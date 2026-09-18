// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A whole bound input: an expression, or one of the forms the calculator allows only on its own.
/// </summary>
/// <param name="Kind">The form of the input.</param>
/// <param name="Operands">The expression; the operands of a Verify chain; the dividend and divisor of ÷R; the two coordinates of Pol( or Rec(.</param>
/// <param name="Relations">The relational operators of a Verify chain; empty otherwise.</param>
/// <param name="Span">The whole input.</param>
internal sealed record BoundStatement(StatementKind Kind, ImmutableArray<BoundNode> Operands, ImmutableArray<RelationOperator> Relations, SourceSpan Span);

/// <summary>The forms of a <see cref="BoundStatement"/>.</summary>
internal enum StatementKind
{
    /// <summary>An expression with one value.</summary>
    Expression,

    /// <summary>A Verify chain such as <c>1≤1&lt;1+1</c>.</summary>
    Verify,

    /// <summary>A division with remainder, <c>5÷R2</c>: quotient to E, remainder to F (p. 56).</summary>
    Remainder,

    /// <summary><c>Pol(x,y)</c>: r to x, θ to y (p. 62).</summary>
    ToPolar,

    /// <summary><c>Rec(r,θ)</c>: x to x, y to y (p. 62).</summary>
    ToRectangular,
}
