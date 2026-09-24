// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Calculates, once, the parts of an expression that are the same at every x: √(2π) in e^(−x²÷2)÷√(2π).
/// </summary>
/// <remarks>
/// <para>
/// A call whose arguments are all constants is replaced by its value: the same operation on the same values, under the
/// settings the compiled expression keeps, is what the evaluator would calculate at every x, exact form included. It
/// is only worth doing for an expression calculated many times (<see cref="CompiledExpression"/>). Measured on 24 Sep
/// 2026: √(2π) cost 2.8 µs at every calculation against 0.9 µs for e^x, which put a graph of the normal density at
/// 21 ms for 2,000 columns.
/// </para>
/// <para>
/// What is not the same at every calculation stays: Ran# and RanInt#( draw a new number each time, and a plugin
/// function is not known to be pure. A call that fails is not folded either, so its error is still raised when it
/// is calculated, with the span it has.
/// </para>
/// </remarks>
internal static class ConstantFolder
{
    public static BoundNode Fold(BoundNode node, EvaluationContext context) => node switch
    {
        BoundCall call => Call(call, context),
        BoundLet let => new BoundLet(let.Slot, Fold(let.Value, context), Fold(let.Body, context), let.Span),
        BoundPluginCall plugin => new BoundPluginCall(plugin.Function, Folded(plugin.Arguments, context), plugin.Span),
        BoundCalculus calculus => new BoundCalculus(
            calculus.Kind,
            calculus.Slot,
            Fold(calculus.Body, context),
            calculus.Derivative is { } derivative ? Fold(derivative, context) : null,
            Folded(calculus.Arguments, context),
            calculus.Span),
        _ => node,
    };

    private static BoundNode Call(BoundCall call, EvaluationContext context)
    {
        ImmutableArray<BoundNode> arguments = Folded(call.Arguments, context);
        if (call.Operation is Operation.Random or Operation.RandomInteger || !arguments.All(argument => argument is BoundConstant))
        {
            return new BoundCall(call.Operation, arguments, call.Span);
        }

        Value[] values = [.. arguments.Select(argument => ((BoundConstant)argument).Value)];
        EvalResult result = Operations.Evaluate(call.Operation, values, context);
        return result.Succeeded ? new BoundConstant(result.Value, call.Span) : new BoundCall(call.Operation, arguments, call.Span);
    }

    private static ImmutableArray<BoundNode> Folded(ImmutableArray<BoundNode> nodes, EvaluationContext context) =>
        [.. nodes.Select(node => Fold(node, context))];
}
