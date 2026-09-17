// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions.Tests.Support;
using CsCheck;

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class LinearPrinterTests
{
    private static readonly SyntaxContext Calculate = new(CalculatorApp.Calculate, AllowRelations: true);

    [Theory]
    [InlineData("2*3/4", "2×3÷4")]
    [InlineData("2−3", "2-3")]
    [InlineData("sqrt(4)+asin(1)", "√(4)+sin⁻¹(1)")]
    [InlineData("pi", "π")]
    [InlineData("5cm->in", "5cm▶in")]
    [InlineData("5 n mile->m", "5n mile▶m")]
    [InlineData("@epsilon_0+@ε₀", "@ε_0+@ε_0")]
    [InlineData("3_u", "3_μ")]
    [InlineData("1<=2", "1≤2")]
    [InlineData("root(5,32)", "5ˣ√(32)")]
    [InlineData("diff(sin(x),1)+sum(x,1,5)", "d/dx(sin(x),1)+Σ(x,1,5)")]
    [InlineData("2°30\"", "2°0′30″")]
    [InlineData("2°20'", "2°20′0″")]
    [InlineData("sin(30", "sin(30)")]
    [InlineData("(1+2", "(1+2)")]
    [InlineData("  2 +  3 ", "2+3")]
    public void Print_UsesCanonicalSpellings(string input, string expected)
    {
        Print(input, Calculate).Should().Be(expected);
    }

    [Theory]
    [InlineData("1010and1100", "1010 and 1100")]
    [InlineData("h1F xor b101", "h1F xor b101")]
    public void Print_SeparatesLogicOperatorsWithSpaces(string input, string expected)
    {
        Print(input, new SyntaxContext(CalculatorApp.BaseN)).Should().Be(expected);
    }

    [Theory]
    [InlineData("6÷2(1+2)")]
    [InlineData("(3×6)<(2+6)×2")]
    [InlineData("((2))")]
    [InlineData("-2²")]
    [InlineData("(-2)²")]
    [InlineData("2⌟3+1⌟1⌟2")]
    [InlineData("3.(021)+0.(312)")]
    [InlineData("2+3=5≠2+5=8")]
    public void Print_ReproducesCanonicalInput(string input)
    {
        Print(input, Calculate).Should().Be(input);
    }

    public static TheoryData<string, SyntaxNode> ConstructedTrees => new()
    {
        { "(1+2)×3", Binary(BinaryOperator.Multiply, Binary(BinaryOperator.Add, Number("1"), Number("2")), Number("3")) },
        { "1-(2-3)", Binary(BinaryOperator.Subtract, Number("1"), Binary(BinaryOperator.Subtract, Number("2"), Number("3"))) },
        { "1-2-3", Binary(BinaryOperator.Subtract, Binary(BinaryOperator.Subtract, Number("1"), Number("2")), Number("3")) },
        { "2^3^2", Binary(BinaryOperator.Power, Binary(BinaryOperator.Power, Number("2"), Number("3")), Number("2")) },
        { "2^(3^2)", Binary(BinaryOperator.Power, Number("2"), Binary(BinaryOperator.Power, Number("3"), Number("2"))) },
        { "2(3)", Binary(BinaryOperator.ImplicitMultiply, Number("2"), Number("3")) },
        { "2(-3)", Binary(BinaryOperator.ImplicitMultiply, Number("2"), new NegationExpression(Number("3"))) },
        { "(2C)3", Binary(BinaryOperator.ImplicitMultiply, Binary(BinaryOperator.ImplicitMultiply, Number("2"), Name("C")), Number("3")) },
        { "2C(-3)", Binary(BinaryOperator.Combination, Number("2"), new NegationExpression(Number("3"))) },
        { "2.5 (3)", Binary(BinaryOperator.ImplicitMultiply, Number("2.5"), new ParenthesizedExpression(Number("3"))) },
        { "(1⌟2)⌟3", Binary(BinaryOperator.Fraction, Binary(BinaryOperator.Fraction, Number("1"), Number("2")), Number("3")) },
        { "--2", new NegationExpression(new NegationExpression(Number("2"))) },
        { "-(2π)", new NegationExpression(Binary(BinaryOperator.ImplicitMultiply, Number("2"), Name("π"))) },
        { "(-2)²", new PostfixExpression(PostfixOperator.Square, new NegationExpression(Number("2"))) },
        { "2^3ˣ√(8)", Binary(BinaryOperator.Root, Binary(BinaryOperator.Power, Number("2"), Number("3")), Number("8")) },
        { "(2π)cm▶in", new SuffixCommandExpression(Binary(BinaryOperator.ImplicitMultiply, Number("2"), Name("π")), Symbol("cm▶in")) },
        { "1⌟1⌟2", new MixedFractionExpression(Number("1"), Number("1"), Number("2")) },
        { "2°0′30″", new SexagesimalExpression(Number("2"), null, Number("30")) },
        { "sin((1+2))", new FunctionCall(Symbol("sin("), [new ParenthesizedExpression(Binary(BinaryOperator.Add, Number("1"), Number("2")))]) },
        { "Abs(1,2)", new FunctionCall(Symbol("Abs("), [Number("1"), Number("2")]) },
    };

    [Theory]
    [MemberData(nameof(ConstructedTrees))]
    public void Print_ParenthesizesConstructedTreesAsTheGrammarRequires(string expected, SyntaxNode tree)
    {
        LinearPrinter.Print(tree, Calculate).Should().Be(expected);
        SyntaxEquivalence.AreEquivalent(ExpressionParser.Parse(expected, Calculate).Root!, tree).Should().BeTrue();
    }

    [Fact]
    public void Print_ParsesBackToAnEquivalentTree_InEveryApplication()
    {
        foreach (SyntaxContext context in SyntaxTrees.Contexts)
        {
            SyntaxTrees.For(context).Sample(tree =>
            {
                string printed = LinearPrinter.Print(tree, context);
                ParseResult reparsed = ExpressionParser.Parse(printed, context);
                return reparsed.Succeeded && SyntaxEquivalence.AreEquivalent(tree, reparsed.Root);
            }, iter: 3_000, print: tree => $"{context.App}: {LinearPrinter.Print(tree, context)}");
        }
    }

    [Fact]
    public void Print_IsIdempotent()
    {
        foreach (SyntaxContext context in SyntaxTrees.Contexts)
        {
            SyntaxTrees.For(context).Sample(tree =>
            {
                string printed = LinearPrinter.Print(tree, context);
                return LinearPrinter.Print(ExpressionParser.Parse(printed, context).Root!, context) == printed;
            }, iter: 1_000, print: tree => $"{context.App}: {LinearPrinter.Print(tree, context)}");
        }
    }

    [Fact]
    public void Print_WithAPluginVocabulary_SeparatesTokensThatWouldMerge()
    {
        SyntaxSymbol ab = SyntaxSymbol.CreateName("AB", SyntaxSymbol.CreateName("AB", SymbolKind.Constant).Kind);
        SyntaxVocabulary vocabulary = SyntaxVocabulary.Standard.With(ab);
        SyntaxNode tree = Binary(BinaryOperator.ImplicitMultiply, Name("A"), Name("B"));

        string printed = LinearPrinter.Print(tree, Calculate, vocabulary);

        printed.Should().Be("A B");
        SyntaxEquivalence.AreEquivalent(ExpressionParser.Parse(printed, Calculate, vocabulary).Root!, tree).Should().BeTrue();
    }

    [Fact]
    public void Print_RejectsNull()
    {
        Action act = () => LinearPrinter.Print(null!, Calculate);
        act.Should().Throw<ArgumentNullException>();
    }

    private static string Print(string input, SyntaxContext context)
    {
        ParseResult result = ExpressionParser.Parse(input, context);
        result.Succeeded.Should().BeTrue("'{0}' should parse, but failed with {1}", input, result.Diagnostic);
        return LinearPrinter.Print(result.Root!, context);
    }

    private static NumberLiteral Number(string text) => new(text);

    private static SyntaxSymbol Symbol(string text) => SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == text);

    private static NameReference Name(string text) => new(Symbol(text));

    private static BinaryExpression Binary(BinaryOperator binaryOperator, SyntaxNode left, SyntaxNode right) => new(binaryOperator, left, right);
}
