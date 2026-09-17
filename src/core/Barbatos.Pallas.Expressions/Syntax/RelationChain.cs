// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A Verify expression: operands joined by relational operators, <c>1≤1&lt;1+1</c> or <c>2+3=5≠2+5=8</c>.
/// </summary>
public sealed class RelationChain : SyntaxNode
{
    /// <summary>Initializes a chain.</summary>
    /// <param name="operands">The operands, one more than the operators.</param>
    /// <param name="operators">The operators, at least one.</param>
    /// <param name="span">Where the chain is.</param>
    /// <exception cref="ArgumentException">The counts do not match, or an operand is null.</exception>
    public RelationChain(ImmutableArray<SyntaxNode> operands, ImmutableArray<RelationOperator> operators, SourceSpan span = default)
        : base(span)
    {
        if (operators.IsDefaultOrEmpty || operands.IsDefault || operands.Length != operators.Length + 1 || operands.Any(operand => operand is null))
        {
            throw new ArgumentException("A relation chain has at least one operator and exactly one more operand, none null.", nameof(operands));
        }

        Operands = operands;
        Operators = operators;
    }

    /// <summary>Gets the operands.</summary>
    public ImmutableArray<SyntaxNode> Operands { get; }

    /// <summary>Gets the operators; <c>Operators[i]</c> stands between <c>Operands[i]</c> and <c>Operands[i + 1]</c>.</summary>
    public ImmutableArray<RelationOperator> Operators { get; }
}
