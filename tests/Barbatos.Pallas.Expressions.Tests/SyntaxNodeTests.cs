// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class SyntaxNodeTests
{
    private static readonly SyntaxContext Verify = new(CalculatorApp.Calculate, AllowRelations: true);

    [Fact]
    public void Constructors_RejectInvalidTrees()
    {
        NumberLiteral one = new("1");
        SyntaxSymbol sin = Symbol("sin(");
        SyntaxSymbol pi = Symbol("π");

        Action[] invalid =
        [
            () => _ = new NumberLiteral(" "),
            () => _ = new CellReference(""),
            () => _ = new BaseLiteral(NumberBase.Hex, null!),
            () => _ = new CellRange(new CellReference("A1"), null!),
            () => _ = new NameReference(sin),
            () => _ = new NameReference(Symbol("+")),
            () => _ = new NegationExpression(null!),
            () => _ = new PostfixExpression(PostfixOperator.Square, null!),
            () => _ = new SuffixCommandExpression(one, pi),
            () => _ = new BinaryExpression(BinaryOperator.Add, one, null!),
            () => _ = new MixedFractionExpression(one, one, null!),
            () => _ = new SexagesimalExpression(one, null, null),
            () => _ = new FunctionCall(pi, [one]),
            () => _ = new FunctionCall(sin, []),
            () => _ = new FunctionCall(sin, default),
            () => _ = new FunctionCall(sin, [null!]),
            () => _ = new ParenthesizedExpression(null!),
            () => _ = new RelationChain([one], [RelationOperator.Equal]),
            () => _ = new RelationChain([one, one], []),
            () => _ = new RelationChain(default, [RelationOperator.Equal]),
            () => _ = new RelationChain([one, null!], [RelationOperator.Equal]),
        ];

        invalid.Should().AllSatisfy(act => act.Should().Throw<ArgumentException>());
    }

    [Fact]
    public void Constructors_StoreCanonicalSymbols()
    {
        NameReference name = new(Symbol("pi"));
        FunctionCall call = new(Symbol("sqrt("), [new NumberLiteral("4")]);
        SuffixCommandExpression conversion = new(new NumberLiteral("5"), Symbol("cm->in"));

        name.Symbol.Text.Should().Be("π");
        call.Function.Text.Should().Be("√(");
        conversion.Command.Text.Should().Be("cm▶in");
        new NumberLiteral("0.(3)").IsRecurring.Should().BeTrue();
        new NumberLiteral("0.3").IsRecurring.Should().BeFalse();
        new BaseLiteral(NumberBase.Bin, new NumberLiteral("101")).Base.Should().Be(NumberBase.Bin);
    }

    [Fact]
    public void ToString_PrintsCanonicalLinearSyntax()
    {
        ExpressionParser.Parse("2*pi<=7", Verify).Root!.ToString().Should().Be("2×π≤7");
    }

    [Theory]
    [InlineData("6÷2(1+2)", "6÷(2(1+2))", true)]
    [InlineData("((1))", "1", true)]
    [InlineData("1.5", "1.50", false)]
    [InlineData("2°20′", "2°20′0″", true)]
    [InlineData("2°20′", "2°20′1″", false)]
    [InlineData("2°30″", "2°0′30″", true)]
    [InlineData("2+3", "2-3", false)]
    [InlineData("2+3", "3+2", false)]
    [InlineData("sin(1)", "cos(1)", false)]
    [InlineData("log(1)", "log(1,1)", false)]
    [InlineData("log(1,2)", "log(1,3)", false)]
    [InlineData("1<2", "1≤2", false)]
    [InlineData("1<2<3", "1<2<4", false)]
    [InlineData("1<2", "1<2", true)]
    [InlineData("-1", "1", false)]
    [InlineData("-1", "-(1)", true)]
    [InlineData("1²", "1³", false)]
    [InlineData("1_k", "1_M", false)]
    [InlineData("1⌟1⌟2", "1⌟1⌟3", false)]
    [InlineData("1⌟1⌟2", "1⌟5⌟2", false)]
    [InlineData("1⌟1⌟2", "4⌟1⌟2", false)]
    [InlineData("2°20′", "3°20′", false)]
    [InlineData("2°20′", "2°21′", false)]
    [InlineData("1⌟1⌟2", "1⌟(1⌟2)", false)]
    [InlineData("A", "B", false)]
    [InlineData("A", "1", false)]
    public void Equivalence_IgnoresParenthesesAndSpans(string left, string right, bool equivalent)
    {
        SyntaxEquivalence.AreEquivalent(Parse(left), Parse(right)).Should().Be(equivalent);
    }

    [Theory]
    [InlineData(CalculatorApp.BaseN, "h1F", "h1F", true)]
    [InlineData(CalculatorApp.BaseN, "h1F", "b1F", false)]
    [InlineData(CalculatorApp.BaseN, "h1F", "h1E", false)]
    [InlineData(CalculatorApp.Spreadsheet, "A1", "A1", true)]
    [InlineData(CalculatorApp.Spreadsheet, "A1", "A2", false)]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:B2)", "Sum(A1:B2)", true)]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:B2)", "Sum(A1:B3)", false)]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:B2)", "Sum(A2:B2)", false)]
    public void Equivalence_ComparesContextSpecificNodes(CalculatorApp app, string left, string right, bool equivalent)
    {
        SyntaxContext context = new(app);
        SyntaxEquivalence.AreEquivalent(ExpressionParser.Parse(left, context).Root!, ExpressionParser.Parse(right, context).Root!).Should().Be(equivalent);
    }

    [Fact]
    public void Equivalence_IsFalseBetweenDifferentKindsOfNode()
    {
        NumberLiteral one = new("1");
        SyntaxNode[] nodes =
        [
            one,
            new BaseLiteral(NumberBase.Hex, one),
            new NameReference(Symbol("x")),
            new CellReference("A1"),
            new CellRange(new CellReference("A1"), new CellReference("A2")),
            new NegationExpression(one),
            new PostfixExpression(PostfixOperator.Square, one),
            new SuffixCommandExpression(one, Symbol("_k")),
            new BinaryExpression(BinaryOperator.Add, one, one),
            new MixedFractionExpression(one, one, one),
            new SexagesimalExpression(one, one, null),
            new FunctionCall(Symbol("sin("), [one]),
            new RelationChain([one, one], [RelationOperator.Equal]),
        ];

        foreach (SyntaxNode left in nodes)
        {
            foreach (SyntaxNode right in nodes)
            {
                SyntaxEquivalence.AreEquivalent(left, right).Should().Be(ReferenceEquals(left, right), "{0} and {1}", left.GetType().Name, right.GetType().Name);
            }
        }
    }

    [Fact]
    public void Equivalence_DistinguishesSymbolsOfTheSameSpellingButDifferentKind()
    {
        NameReference variable = new(Symbol("x"));
        NameReference constant = new(SyntaxSymbol.CreateName("x", SymbolKind.Constant));

        SyntaxEquivalence.AreEquivalent(variable, constant).Should().BeFalse();
    }

    [Fact]
    public void Printers_RejectUndefinedOperators()
    {
        NumberLiteral one = new("1");
        SyntaxNode undefined = new BinaryExpression((BinaryOperator)99, one, one);

        Action linear = () => LinearPrinter.Print(undefined, Verify);
        Action latex = () => LatexPrinter.Print(new NegationExpression(undefined));

        linear.Should().Throw<ArgumentOutOfRangeException>();
        latex.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Equivalence_RejectsNull()
    {
        Action left = () => SyntaxEquivalence.AreEquivalent(null!, new NumberLiteral("1"));
        Action right = () => SyntaxEquivalence.AreEquivalent(new NumberLiteral("1"), null!);

        left.Should().Throw<ArgumentNullException>();
        right.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SourceSpan_Covering_JoinsTwoSpans()
    {
        SourceSpan.Covering(new SourceSpan(2, 3), new SourceSpan(7, 2)).Should().Be(new SourceSpan(2, 7));
        new SourceSpan(2, 3).End.Should().Be(5);
    }

    [Fact]
    public void ParseResult_ExposesTheTextAndOutcome()
    {
        ParseResult success = ExpressionParser.Parse("1", SyntaxContext.Calculate);
        ParseResult failure = ExpressionParser.Parse("1+", SyntaxContext.Calculate);

        (success.Succeeded, success.Diagnostic, success.Text).Should().Be((true, (SyntaxDiagnostic?)null, "1"));
        (failure.Succeeded, failure.Root).Should().Be((false, (SyntaxNode?)null));
    }

    private static SyntaxNode Parse(string text) => ExpressionParser.Parse(text, Verify).Root!;

    private static SyntaxSymbol Symbol(string text) => SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == text);
}
