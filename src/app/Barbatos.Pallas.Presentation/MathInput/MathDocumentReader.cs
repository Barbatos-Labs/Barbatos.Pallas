// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Reads Canonical Linear Syntax back onto the line, which is how a calculation comes out of the history.
/// </summary>
/// <remarks>
/// <para>
/// The text is read by the engine's own parser and the tree is turned into structures, so there is no second parser
/// to keep in step with the first. What has no structure of its own - a sexagesimal angle, a unit conversion, a
/// Base-N literal, the relation of a comparison - is put back as the characters it was written with, which reads the
/// same and can still be edited; what is on either side of a relation keeps its structures.
/// </para>
/// <para>
/// Text the parser cannot read at all becomes its characters too: a line that was stored while it was still being
/// typed comes back as it was left.
/// </para>
/// </remarks>
public static class MathDocumentReader
{
    /// <summary>Reads text onto a line.</summary>
    /// <param name="text">The Canonical Linear Syntax.</param>
    /// <param name="app">The application whose syntax it is.</param>
    /// <returns>The input, with the cursor at its end.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static MathDocument Read(string text, CalculatorApp app = CalculatorApp.Calculate)
    {
        ArgumentNullException.ThrowIfNull(text);
        SyntaxContext context = new(app, AllowRelations: true);
        ParseResult result = ExpressionParser.Parse(text, context);
        MathRow row = result.Root is null ? Characters(text) : new MathRow([.. Elements(result.Root, context)]);
        return MathDocument.Of(row);
    }

    private static IEnumerable<MathElement> Elements(SyntaxNode node, SyntaxContext context)
    {
        switch (node)
        {
            case NumberLiteral number:
                return Characters(number.Text).Elements;
            case NameReference name:
                return [new MathSymbol(name.Symbol.Text)];
            case ParenthesizedExpression parenthesized:
                return [Structure(MathTemplateKind.Parentheses, context, parenthesized.Inner)];
            case MixedFractionExpression mixed:
                return [Structure(MathTemplateKind.MixedFraction, context, mixed.Whole, mixed.Numerator, mixed.Denominator)];
            case NegationExpression negation:
                return [new MathSymbol("−"), .. Elements(negation.Operand, context)];
            case BinaryExpression binary:
                return Binary(binary, context);
            case FunctionCall call:
                return Call(call, context);
            case RelationChain chain:
                // A comparison of Verify: each side keeps its structures - √(4)=2 comes back with its square root -
                // and the relations between them are their symbols. Before, the whole chain came back as characters
                // (found by a test of AllowRelations, which nothing could tell was on).
                return chain.Operands.SelectMany((operand, index) => index == 0
                    ? Elements(operand, context)
                    : [new MathSymbol(SpellingOf(chain.Operators[index - 1])), .. Elements(operand, context)]);
            case PostfixExpression postfix:
                // The operator as the vocabulary spells it, after its operand. Cutting the operand's length off the
                // printed node was wrong where the printer brackets the operand: (Σx x̂)° prints as (Σxx̂)°, and the
                // cut came one character early (found by the random-key property, 23 Sep 2026).
                return [.. Elements(postfix.Operand, context), .. Characters(SpellingOf(postfix.Operator)).Elements];
            default:
                return Characters(Print(node, context)).Elements;
        }
    }

    private static IEnumerable<MathElement> Binary(BinaryExpression binary, SyntaxContext context)
    {
        switch (binary.Operator)
        {
            case BinaryOperator.Fraction:
                return [Structure(MathTemplateKind.Fraction, context, binary.Left, binary.Right)];
            case BinaryOperator.Root:
                return [Structure(MathTemplateKind.Root, context, binary.Left, Unwrap(binary.Right))];
            case BinaryOperator.Power:
                return [Structure(MathTemplateKind.Power, context, binary.Left, Unwrap(binary.Right))];
            case BinaryOperator.ImplicitMultiply:
                return [.. Elements(binary.Left, context), .. Elements(binary.Right, context)];
            default:
                string text = Print(binary, context);
                string left = Print(binary.Left, context);
                string right = Print(binary.Right, context);

                // What is between the two operands is the operator as it was written, spaces and all.
                string between = text.Length >= left.Length + right.Length
                    ? text[left.Length..^right.Length]
                    : text;
                return [.. Elements(binary.Left, context), .. Characters(between).Elements, .. Elements(binary.Right, context)];
        }
    }

    private static List<MathElement> Call(FunctionCall call, SyntaxContext context)
    {
        string name = call.Function.Text;
        MathTemplateKind? kind = (name, call.Arguments.Length) switch
        {
            ("√(", 1) => MathTemplateKind.SquareRoot,
            ("Abs(", 1) => MathTemplateKind.Abs,
            ("log(", 2) => MathTemplateKind.LogBase,
            ("∫(", 3) => MathTemplateKind.Integral,
            ("Σ(", 3) => MathTemplateKind.Sum,
            ("Π(", 3) => MathTemplateKind.Product,
            ("d/dx(", 2) => MathTemplateKind.Derivative,
            _ => null,
        };

        if (kind is { } template)
        {
            return [Structure(template, context, [.. call.Arguments])];
        }


        List<MathElement> elements = [new MathSymbol(name)];
        for (int index = 0; index < call.Arguments.Length; index++)
        {
            if (index > 0)
            {
                elements.Add(new MathSymbol(","));
            }

            elements.AddRange(Elements(call.Arguments[index], context));
        }

        elements.Add(new MathSymbol(")"));
        return elements;
    }

    private static MathStructure Structure(MathTemplateKind kind, SyntaxContext context, params SyntaxNode[] slots)
    {
        // A slot keeps the brackets it was written with: ((7)) is two of them, and reading it back as one would be
        // a different tree, even if it is the same number.
        ImmutableArray<MathRow> rows = [.. slots.Select(slot => new MathRow([.. Elements(slot, context)]))];
        return new MathStructure(kind, rows);
    }

    private static SyntaxNode Unwrap(SyntaxNode node) => node is ParenthesizedExpression parenthesized ? parenthesized.Inner : node;

    private static string Print(SyntaxNode node, SyntaxContext context) => LinearPrinter.Print(node, context);

    /// <summary>How the vocabulary that read a postfix operator spells it: the canonical spelling, never an alias.</summary>
    private static string SpellingOf(PostfixOperator postfix) =>
        SyntaxVocabulary.Standard.Symbols.First(symbol => symbol.PostfixOperator == postfix).Canonical.Text;

    /// <summary>How the vocabulary spells a relation.</summary>
    private static string SpellingOf(RelationOperator relation) =>
        SyntaxVocabulary.Standard.Symbols.First(symbol => symbol.RelationOperator == relation).Canonical.Text;

    private static MathRow Characters(string text) => new([.. text.Select(character => (MathElement)new MathSymbol(character.ToString()))]);
}
