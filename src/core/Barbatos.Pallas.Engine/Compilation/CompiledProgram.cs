// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>The instructions of the evaluator's stack machine.</summary>
internal enum OpCode : byte
{
    /// <summary>Pushes constant <c>A</c>.</summary>
    Push,

    /// <summary>Pushes memory <c>A</c> (a <see cref="MemorySlot"/>).</summary>
    Load,

    /// <summary>Pushes local slot <c>A</c>.</summary>
    LoadLocal,

    /// <summary>Pops into local slot <c>A</c>.</summary>
    StoreLocal,

    /// <summary>Pops <c>B</c> arguments and pushes the result of operation <c>A</c>.</summary>
    Call,

    /// <summary>Pops <c>B</c> arguments and pushes the result of plugin function <c>A</c>.</summary>
    CallPlugin,

    /// <summary>Pops <c>B</c> bounds and pushes the calculus of kind <c>A</c> over the body in segment <c>C</c> (its derivative in <c>C + 1</c>), with its variable in slot <c>D</c>.</summary>
    Calculus,

    /// <summary>Fails with the error kind <c>A</c>.</summary>
    Fail,
}

/// <summary>One instruction: an opcode, its operands and the span to report an error at.</summary>
internal readonly record struct Instruction(OpCode Code, int A, int B, int C, int D, SourceSpan Span);

/// <summary>A sequence of instructions evaluated with its own stack.</summary>
/// <param name="Code">The instructions.</param>
/// <param name="MaxStack">The deepest the stack gets.</param>
internal sealed record Segment(ImmutableArray<Instruction> Code, int MaxStack);

/// <summary>
/// A bound statement lowered to stack-machine segments (docs/ARCHITECTURE.md §5): the operands of the statement first,
/// then the bodies of Σ, Π, ∫ and d/dx, which are evaluated repeatedly.
/// </summary>
internal sealed class CompiledProgram(
    BoundStatement statement,
    ImmutableArray<Segment> segments,
    ImmutableArray<Value> constants,
    ImmutableArray<IMathFunction> plugins,
    int slotCount)
{
    public BoundStatement Statement { get; } = statement;

    public ImmutableArray<Segment> Segments { get; } = segments;

    public ImmutableArray<Value> Constants { get; } = constants;

    public ImmutableArray<IMathFunction> Plugins { get; } = plugins;

    public int SlotCount { get; } = slotCount;
}
