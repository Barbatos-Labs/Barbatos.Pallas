// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A node of a bound expression: names resolved, arities checked, and every operation one of the engine's.
/// </summary>
/// <remarks>
/// Bound trees are immutable and may share subtrees, which the derivative relies on: the derivative of <c>u×v</c> refers to
/// <c>u</c> and <c>v</c> without copying them.
/// </remarks>
internal abstract class BoundNode(SourceSpan span)
{
    /// <summary>Gets the part of the input an error in this node is reported at.</summary>
    public SourceSpan Span { get; } = span;
}

/// <summary>A constant value.</summary>
internal sealed class BoundConstant(Value value, SourceSpan span) : BoundNode(span)
{
    public Value Value { get; } = value;
}

/// <summary>A calculator memory: A-F, x, y, z (<see cref="MemoryVariable"/>), Ans or PreAns.</summary>
internal sealed class BoundMemory(MemorySlot slot, SourceSpan span) : BoundNode(span)
{
    public MemorySlot Slot { get; } = slot;
}

/// <summary>A statistic variable of the Statistics application, calculated from its data when the input runs.</summary>
internal sealed class BoundStatistic(Statistic statistic, SourceSpan span) : BoundNode(span)
{
    public Statistic Statistic { get; } = statistic;
}

/// <summary>Cells of the Spreadsheet application: one cell, or a range through Min(, Max(, Mean( or Sum( (p. 105).</summary>
internal sealed class BoundCells(CellSelection selection, SourceSpan span) : BoundNode(span)
{
    public CellSelection Selection { get; } = selection;
}

/// <summary>The variable of a Σ, Π, ∫ or d/dx body, or the x of f(x) and g(x).</summary>
internal sealed class BoundLocal(int slot, SourceSpan span) : BoundNode(span)
{
    public int Slot { get; } = slot;
}

/// <summary>A built-in operation.</summary>
internal sealed class BoundCall(Operation operation, ImmutableArray<BoundNode> arguments, SourceSpan span) : BoundNode(span)
{
    public Operation Operation { get; } = operation;

    public ImmutableArray<BoundNode> Arguments { get; } = arguments;
}

/// <summary>A plugin function.</summary>
internal sealed class BoundPluginCall(IMathFunction function, ImmutableArray<BoundNode> arguments, SourceSpan span) : BoundNode(span)
{
    public IMathFunction Function { get; } = function;

    public ImmutableArray<BoundNode> Arguments { get; } = arguments;
}

/// <summary>Evaluates <see cref="Value"/> into a local slot, then <see cref="Body"/>: the call of f(x) or g(x).</summary>
internal sealed class BoundLet(int slot, BoundNode value, BoundNode body, SourceSpan span) : BoundNode(span)
{
    public int Slot { get; } = slot;

    public BoundNode Value { get; } = value;

    public BoundNode Body { get; } = body;
}

/// <summary>Σ, Π, ∫ or d/dx over a body in <see cref="Slot"/>.</summary>
internal sealed class BoundCalculus(
    CalculusKind kind,
    int slot,
    BoundNode body,
    BoundNode? derivative,
    ImmutableArray<BoundNode> arguments,
    SourceSpan span) : BoundNode(span)
{
    public CalculusKind Kind { get; } = kind;

    public int Slot { get; } = slot;

    public BoundNode Body { get; } = body;

    /// <summary>Gets the derivative of the body for d/dx; <see langword="null"/> otherwise.</summary>
    public BoundNode? Derivative { get; } = derivative;

    /// <summary>Gets the bounds: the point and optional tol for d/dx; a, b and optional tol for ∫; a and b for Σ and Π.</summary>
    public ImmutableArray<BoundNode> Arguments { get; } = arguments;
}

/// <summary>An error raised when evaluated: the derivative of a function that has none.</summary>
internal sealed class BoundFailure(CalcErrorKind kind, SourceSpan span) : BoundNode(span)
{
    public CalcErrorKind Kind { get; } = kind;
}

/// <summary>The kinds of <see cref="BoundCalculus"/>.</summary>
internal enum CalculusKind
{
    Derivative,
    Integral,
    Summation,
    Product,
}

/// <summary>The memories an expression can read.</summary>
internal enum MemorySlot
{
    A,
    B,
    C,
    D,
    E,
    F,
    X,
    Y,
    Z,
    Ans,
    PreAns,
    MatA,
    MatB,
    MatC,
    MatD,
    MatAns,
    VctA,
    VctB,
    VctC,
    VctD,
    VctAns,
}
