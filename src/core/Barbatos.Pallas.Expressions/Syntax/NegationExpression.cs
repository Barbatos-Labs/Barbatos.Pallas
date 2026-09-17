// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The negative sign <c>-x</c>, priority level 5: <c>-2²</c> is <c>-(2²)</c> (manual p. 169).
/// </summary>
public sealed class NegationExpression : SyntaxNode
{
    /// <summary>Initializes a negation.</summary>
    /// <param name="operand">The negated expression.</param>
    /// <param name="span">Where the sign and operand are.</param>
    public NegationExpression(SyntaxNode operand, SourceSpan span = default)
        : base(span)
    {
        Operand = NotNull(operand, nameof(operand));
    }

    /// <summary>Gets the negated expression.</summary>
    public SyntaxNode Operand { get; }
}
