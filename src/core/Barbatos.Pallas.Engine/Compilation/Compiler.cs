// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Lowers a bound statement to postfix stack-machine code.
/// </summary>
/// <remarks>
/// Each operand of the statement becomes segment 0, 1, … in order. Calculus bodies get segments of their own, because
/// Σ, Π and ∫ evaluate them many times.
/// </remarks>
internal sealed class Compiler
{
    private readonly List<Segment?> _segments = [];
    private readonly List<Value> _constants = [];
    private readonly List<IMathFunction> _plugins = [];

    private Compiler()
    {
    }

    public static CompiledProgram Compile(BoundStatement statement, int slotCount)
    {
        Compiler compiler = new();
        foreach (BoundNode operand in statement.Operands)
        {
            compiler._segments.Add(null);
        }

        for (int i = 0; i < statement.Operands.Length; i++)
        {
            compiler._segments[i] = compiler.CompileSegment(statement.Operands[i]);
        }

        return new CompiledProgram(statement, [.. compiler._segments.Select(segment => segment!)], [.. compiler._constants], [.. compiler._plugins], slotCount);
    }

    private Segment CompileSegment(BoundNode node)
    {
        ImmutableArray<Instruction>.Builder code = ImmutableArray.CreateBuilder<Instruction>();
        int depth = 0;
        int maxStack = 0;
        Emit(node, code, ref depth, ref maxStack);

        // Every node leaves exactly one value; anything else is a compiler defect, not a calculator error.
        if (depth != 1)
        {
            throw new InvalidOperationException($"The compiled segment leaves {depth} values on the stack.");
        }

        return new Segment(code.ToImmutable(), maxStack);
    }

    private void Emit(BoundNode node, ImmutableArray<Instruction>.Builder code, ref int depth, ref int maxStack)
    {
        switch (node)
        {
            case BoundConstant constant:
                code.Add(new Instruction(OpCode.Push, AddConstant(constant.Value), 0, 0, 0, node.Span));
                Push(ref depth, ref maxStack);
                break;
            case BoundMemory memory:
                code.Add(new Instruction(OpCode.Load, (int)memory.Slot, 0, 0, 0, node.Span));
                Push(ref depth, ref maxStack);
                break;
            case BoundLocal local:
                code.Add(new Instruction(OpCode.LoadLocal, local.Slot, 0, 0, 0, node.Span));
                Push(ref depth, ref maxStack);
                break;
            case BoundFailure failure:
                code.Add(new Instruction(OpCode.Fail, (int)failure.Kind, 0, 0, 0, node.Span));
                Push(ref depth, ref maxStack);
                break;
            case BoundCall call:
                foreach (BoundNode argument in call.Arguments)
                {
                    Emit(argument, code, ref depth, ref maxStack);
                }

                code.Add(new Instruction(OpCode.Call, (int)call.Operation, call.Arguments.Length, 0, 0, node.Span));
                depth -= call.Arguments.Length;
                Push(ref depth, ref maxStack);
                break;
            case BoundPluginCall plugin:
                foreach (BoundNode argument in plugin.Arguments)
                {
                    Emit(argument, code, ref depth, ref maxStack);
                }

                code.Add(new Instruction(OpCode.CallPlugin, _plugins.Count, plugin.Arguments.Length, 0, 0, node.Span));
                _plugins.Add(plugin.Function);
                depth -= plugin.Arguments.Length;
                Push(ref depth, ref maxStack);
                break;
            case BoundLet let:
                Emit(let.Value, code, ref depth, ref maxStack);
                code.Add(new Instruction(OpCode.StoreLocal, let.Slot, 0, 0, 0, node.Span));
                depth--;
                Emit(let.Body, code, ref depth, ref maxStack);
                break;
            case BoundCalculus calculus:
                foreach (BoundNode argument in calculus.Arguments)
                {
                    Emit(argument, code, ref depth, ref maxStack);
                }

                // Reserve the body's segment, and the derivative's right after it, before compiling either.
                int body = _segments.Count;
                _segments.Add(null);
                if (calculus.Derivative is not null)
                {
                    _segments.Add(null);
                }

                _segments[body] = CompileSegment(calculus.Body);
                if (calculus.Derivative is not null)
                {
                    _segments[body + 1] = CompileSegment(calculus.Derivative);
                }

                code.Add(new Instruction(OpCode.Calculus, (int)calculus.Kind, calculus.Arguments.Length, body, calculus.Slot, node.Span));
                depth -= calculus.Arguments.Length;
                Push(ref depth, ref maxStack);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(node), node.GetType().Name, "Not a bound node the compiler knows.");
        }
    }

    private int AddConstant(Value value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    private static void Push(ref int depth, ref int maxStack)
    {
        depth++;
        maxStack = Math.Max(maxStack, depth);
    }
}
