// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What the input suggests about how its result is displayed.
/// </summary>
[Flags]
internal enum DisplayHints
{
    None = 0,

    /// <summary>The input contains a fraction: LineO then displays a fraction result as a fraction.</summary>
    FractionInput = 1,

    /// <summary>The input is a sum or difference of degrees-minutes-seconds: the result is displayed in them too.</summary>
    Sexagesimal = 2,
}

/// <summary>Reads <see cref="DisplayHints"/> from a syntax tree.</summary>
internal static class DisplayHintReader
{
    public static DisplayHints Read(SyntaxNode root)
    {
        return (ContainsFraction(root) ? DisplayHints.FractionInput : DisplayHints.None)
            | (IsSexagesimalSum(root) ? DisplayHints.Sexagesimal : DisplayHints.None);
    }

    private static bool ContainsFraction(SyntaxNode node)
    {
        return node switch
        {
            MixedFractionExpression => true,
            BinaryExpression binary => binary.Operator == BinaryOperator.Fraction || ContainsFraction(binary.Left) || ContainsFraction(binary.Right),
            NegationExpression negation => ContainsFraction(negation.Operand),
            PostfixExpression postfix => ContainsFraction(postfix.Operand),
            ParenthesizedExpression parenthesized => ContainsFraction(parenthesized.Inner),
            FunctionCall call => call.Arguments.Any(ContainsFraction),
            SuffixCommandExpression suffix => ContainsFraction(suffix.Operand),
            _ => false,
        };
    }

    private static bool IsSexagesimalSum(SyntaxNode node)
    {
        return node switch
        {
            SexagesimalExpression => true,
            BinaryExpression { Operator: BinaryOperator.Add or BinaryOperator.Subtract } binary => IsSexagesimalSum(binary.Left) && IsSexagesimalSum(binary.Right),
            NegationExpression negation => IsSexagesimalSum(negation.Operand),
            ParenthesizedExpression parenthesized => IsSexagesimalSum(parenthesized.Inner),
            _ => false,
        };
    }
}
