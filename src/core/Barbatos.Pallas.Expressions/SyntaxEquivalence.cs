// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Compares syntax trees by structure.
/// </summary>
public static class SyntaxEquivalence
{
    /// <summary>
    /// Determines whether two trees have the same structure, ignoring spans and parentheses.
    /// </summary>
    /// <param name="left">The first tree.</param>
    /// <param name="right">The second tree.</param>
    /// <returns>
    /// <see langword="true"/> when both trees apply the same operators to the same operands in the same order. Parentheses
    /// are looked through, so <c>6÷2(1+2)</c> and <c>6÷(2(1+2))</c> are equivalent (manual p. 29). Numbers compare as
    /// written, so <c>1.5</c> and <c>1.50</c> are not. A missing minute or second part equals <c>0</c>, as printed.
    /// </returns>
    public static bool AreEquivalent(SyntaxNode left, SyntaxNode right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return (Unwrap(left), Unwrap(right)) switch
        {
            (NumberLiteral a, NumberLiteral b) => a.Text == b.Text,
            (BaseLiteral a, BaseLiteral b) => a.Base == b.Base && a.Digits.Text == b.Digits.Text,
            (NameReference a, NameReference b) => Same(a.Symbol, b.Symbol),
            (CellReference a, CellReference b) => a.Text == b.Text,
            (CellRange a, CellRange b) => a.Start.Text == b.Start.Text && a.End.Text == b.End.Text,
            (NegationExpression a, NegationExpression b) => AreEquivalent(a.Operand, b.Operand),
            (PostfixExpression a, PostfixExpression b) => a.Operator == b.Operator && AreEquivalent(a.Operand, b.Operand),
            (SuffixCommandExpression a, SuffixCommandExpression b) => Same(a.Command, b.Command) && AreEquivalent(a.Operand, b.Operand),
            (BinaryExpression a, BinaryExpression b) => a.Operator == b.Operator && AreEquivalent(a.Left, b.Left) && AreEquivalent(a.Right, b.Right),
            (MixedFractionExpression a, MixedFractionExpression b) =>
                AreEquivalent(a.Whole, b.Whole) && AreEquivalent(a.Numerator, b.Numerator) && AreEquivalent(a.Denominator, b.Denominator),
            (SexagesimalExpression a, SexagesimalExpression b) =>
                AreEquivalent(a.Degrees, b.Degrees) && PartText(a.Minutes) == PartText(b.Minutes) && PartText(a.Seconds) == PartText(b.Seconds),
            (FunctionCall a, FunctionCall b) =>
                Same(a.Function, b.Function) && a.Arguments.Length == b.Arguments.Length
                && a.Arguments.Zip(b.Arguments).All(pair => AreEquivalent(pair.First, pair.Second)),
            (RelationChain a, RelationChain b) =>
                a.Operators.SequenceEqual(b.Operators) && a.Operands.Zip(b.Operands).All(pair => AreEquivalent(pair.First, pair.Second)),
            _ => false,
        };
    }

    private static SyntaxNode Unwrap(SyntaxNode node)
    {
        while (node is ParenthesizedExpression parenthesized)
        {
            node = parenthesized.Inner;
        }

        return node;
    }

    private static bool Same(SyntaxSymbol a, SyntaxSymbol b) => a.Kind == b.Kind && a.Text == b.Text;

    private static string PartText(NumberLiteral? part) => part?.Text ?? "0";
}
