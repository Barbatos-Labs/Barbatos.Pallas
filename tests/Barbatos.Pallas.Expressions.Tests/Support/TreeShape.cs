// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text;

namespace Barbatos.Pallas.Expressions.Tests.Support;

/// <summary>
/// Writes a tree as a compact prefix form, so a test states the structure it expects in one line:
/// <c>6÷2(1+2)</c> is <c>(÷ 6 (· 2 [(+ 1 2)]))</c>, where <c>·</c> is implicit multiplication and <c>[…]</c> parentheses.
/// </summary>
internal static class TreeShape
{
    public static string Of(SyntaxNode node)
    {
        StringBuilder shape = new();
        Write(shape, node);
        return shape.ToString();
    }

    public static string Parse(string text, SyntaxContext context)
    {
        ParseResult result = ExpressionParser.Parse(text, context);
        result.Succeeded.Should().BeTrue("'{0}' should parse, but failed with {1}", text, result.Diagnostic);
        return Of(result.Root!);
    }

    public static string Parse(string text) => Parse(text, new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true));

    private static void Write(StringBuilder shape, SyntaxNode node)
    {
        switch (node)
        {
            case NumberLiteral number:
                shape.Append(number.Text);
                break;
            case BaseLiteral literal:
                shape.Append(literal.Base).Append(':').Append(literal.Digits.Text);
                break;
            case NameReference name:
                shape.Append(name.Symbol.Text);
                break;
            case CellReference cell:
                shape.Append(cell.Text);
                break;
            case CellRange range:
                shape.Append(range.Start.Text).Append(':').Append(range.End.Text);
                break;
            case NegationExpression negation:
                Group(shape, "neg", negation.Operand);
                break;
            case PostfixExpression postfix:
                Group(shape, Name(postfix.Operator), postfix.Operand);
                break;
            case SuffixCommandExpression suffix:
                Group(shape, suffix.Command.Text, suffix.Operand);
                break;
            case BinaryExpression binary:
                Group(shape, Name(binary.Operator), binary.Left, binary.Right);
                break;
            case MixedFractionExpression mixed:
                Group(shape, "mixed", mixed.Whole, mixed.Numerator, mixed.Denominator);
                break;
            case SexagesimalExpression sexagesimal:
                shape.Append("(dms ");
                Write(shape, sexagesimal.Degrees);
                shape.Append(' ').Append(sexagesimal.Minutes?.Text ?? "_").Append(' ').Append(sexagesimal.Seconds?.Text ?? "_").Append(')');
                break;
            case FunctionCall call:
                Group(shape, call.Function.Text, [.. call.Arguments]);
                break;
            case ParenthesizedExpression parenthesized:
                shape.Append('[');
                Write(shape, parenthesized.Inner);
                shape.Append(']');
                break;
            case RelationChain chain:
                shape.Append("(chain ");
                Write(shape, chain.Operands[0]);
                for (int index = 0; index < chain.Operators.Length; index++)
                {
                    shape.Append(' ').Append(Name(chain.Operators[index])).Append(' ');
                    Write(shape, chain.Operands[index + 1]);
                }

                shape.Append(')');
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(node), node.GetType().Name, "Unknown node type.");
        }
    }

    private static void Group(StringBuilder shape, string name, params SyntaxNode[] children)
    {
        shape.Append('(').Append(name);
        foreach (SyntaxNode child in children)
        {
            shape.Append(' ');
            Write(shape, child);
        }

        shape.Append(')');
    }

    private static string Name(BinaryOperator binaryOperator) => binaryOperator switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "×",
        BinaryOperator.Divide => "÷",
        BinaryOperator.DivideWithRemainder => "÷R",
        BinaryOperator.ImplicitMultiply => "·",
        BinaryOperator.Power => "^",
        BinaryOperator.Root => "root",
        BinaryOperator.Fraction => "⌟",
        BinaryOperator.Permutation => "P",
        BinaryOperator.Combination => "C",
        BinaryOperator.Polar => "∠",
        BinaryOperator.DotProduct => "•",
        _ => binaryOperator.ToString().ToLowerInvariant(),
    };

    private static string Name(PostfixOperator postfixOperator) => postfixOperator switch
    {
        PostfixOperator.Square => "²",
        PostfixOperator.Cube => "³",
        PostfixOperator.Reciprocal => "⁻¹",
        PostfixOperator.Factorial => "!",
        PostfixOperator.Percent => "%",
        PostfixOperator.Degrees => "°",
        PostfixOperator.Radians => "ʳ",
        PostfixOperator.Gradians => "ᵍ",
        PostfixOperator.StandardizedVariate => "▶t",
        PostfixOperator.EstimateX => "x̂",
        PostfixOperator.EstimateY => "ŷ",
        PostfixOperator.EstimateX1 => "x̂₁",
        _ => "x̂₂",
    };

    private static string Name(RelationOperator relationOperator) => relationOperator switch
    {
        RelationOperator.Equal => "=",
        RelationOperator.NotEqual => "≠",
        RelationOperator.Less => "<",
        RelationOperator.Greater => ">",
        RelationOperator.LessOrEqual => "≤",
        _ => "≥",
    };
}
