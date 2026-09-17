// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// An operator after its operand: <c>x²</c>, <c>5!</c>, <c>20%</c>, <c>30°</c>, <c>5.5ŷ</c>.
/// </summary>
public sealed class PostfixExpression : SyntaxNode
{
    /// <summary>Initializes a postfix expression.</summary>
    /// <param name="postfixOperator">The operator.</param>
    /// <param name="operand">The operand.</param>
    /// <param name="span">Where the operand and operator are.</param>
    public PostfixExpression(PostfixOperator postfixOperator, SyntaxNode operand, SourceSpan span = default)
        : base(span)
    {
        Operator = postfixOperator;
        Operand = NotNull(operand, nameof(operand));
    }

    /// <summary>Gets the operator.</summary>
    public PostfixOperator Operator { get; }

    /// <summary>Gets the operand.</summary>
    public SyntaxNode Operand { get; }
}
