// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Binding strength, the priority levels of manual p. 168 turned around: a larger number binds tighter, and
/// Verify's relations (not on the calculator's list) bind loosest.
/// </summary>
internal static class Precedence
{
    public const int Relation = 1;          // = ≠ < > ≤ ≥
    public const int Or = 2;                // level 13: or xor xnor
    public const int And = 3;               // level 12: and
    public const int Additive = 4;          // level 11: + -
    public const int Multiplicative = 5;    // level 10: × ÷ ÷R
    public const int Dot = 6;               // level 9: •
    public const int Combinatorial = 7;     // level 8: P C ∠
    public const int Implicit = 8;          // level 7: multiplication with the sign omitted
    public const int Conversion = 9;        // level 6: unit conversions, x̂ ŷ x̂₁ x̂₂
    public const int Prefix = 10;           // level 5: negative sign, base prefixes
    public const int Fraction = 11;         // level 4: ⌟
    public const int Postfix = 12;          // level 3: ² ³ ⁻¹ ! ° ʳ ᵍ % ▶t, engineering symbols, ^, ˣ√
    public const int Primary = 13;          // levels 1-2: numbers, names, parentheses, functions

    public static int Of(BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            BinaryOperator.Add or BinaryOperator.Subtract => Additive,
            BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.DivideWithRemainder => Multiplicative,
            BinaryOperator.ImplicitMultiply => Implicit,
            BinaryOperator.Power or BinaryOperator.Root => Postfix,
            BinaryOperator.Fraction => Fraction,
            BinaryOperator.Permutation or BinaryOperator.Combination or BinaryOperator.Polar => Combinatorial,
            BinaryOperator.DotProduct => Dot,
            BinaryOperator.And => And,
            BinaryOperator.Or or BinaryOperator.Xor or BinaryOperator.Xnor => Or,
            _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, "Not a defined BinaryOperator value."),
        };
    }

    public static int Of(PostfixOperator postfixOperator)
    {
        return postfixOperator is PostfixOperator.EstimateX or PostfixOperator.EstimateY or PostfixOperator.EstimateX1 or PostfixOperator.EstimateX2
            ? Conversion
            : Postfix;
    }

    public static int Of(SyntaxNode node)
    {
        return node switch
        {
            BinaryExpression binary => Of(binary.Operator),
            PostfixExpression postfix => Of(postfix.Operator),
            SuffixCommandExpression suffix => suffix.Command.Kind == SymbolKind.EngineeringSymbol ? Postfix : Conversion,
            NegationExpression => Prefix,
            MixedFractionExpression => Fraction,
            SexagesimalExpression => Postfix,
            RelationChain => Relation,
            _ => Primary,
        };
    }
}
