// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// An operator between two operands: <c>a+b</c>, <c>2π</c>, <c>2⌟3</c>, <c>10C4</c>, <c>5ˣ√(32)</c>.
/// </summary>
public sealed class BinaryExpression : SyntaxNode
{
    /// <summary>Initializes a binary expression.</summary>
    /// <param name="binaryOperator">The operator.</param>
    /// <param name="left">The left operand; for <see cref="BinaryOperator.Root"/>, the index.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="span">Where the expression is.</param>
    public BinaryExpression(BinaryOperator binaryOperator, SyntaxNode left, SyntaxNode right, SourceSpan span = default)
        : base(span)
    {
        Operator = binaryOperator;
        Left = NotNull(left, nameof(left));
        Right = NotNull(right, nameof(right));
    }

    /// <summary>Gets the operator.</summary>
    public BinaryOperator Operator { get; }

    /// <summary>Gets the left operand.</summary>
    public SyntaxNode Left { get; }

    /// <summary>Gets the right operand.</summary>
    public SyntaxNode Right { get; }
}
