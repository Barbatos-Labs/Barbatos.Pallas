// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void ParsingAnExpression()
    {
        ParseResult result = ExpressionParser.Parse("6÷2(1+2)", SyntaxContext.Calculate);

        result.Succeeded.Should().BeTrue();
        SyntaxNode tree = result.Root!;
        tree.ToString().Should().Be("6÷2(1+2)", "an omitted × binds tighter than ÷ (manual p. 29)");
        tree.Should().BeOfType<BinaryExpression>().Which.Operator.Should().Be(BinaryOperator.Divide);
    }

    [Fact]
    public void AnErrorCarriesItsCodeAndSpan()
    {
        ParseResult result = ExpressionParser.Parse("2+", SyntaxContext.Calculate);

        result.Succeeded.Should().BeFalse();
        SyntaxDiagnostic error = result.Diagnostic!.Value;
        error.Code.Should().Be(SyntaxErrorCode.MissingOperand);
        error.Span.Should().Be(new SourceSpan(2, 0));
        error.Span.End.Should().Be(2);
    }

    [Fact]
    public void EachApplicationReadsItsOwnSyntax()
    {
        ExpressionParser.Parse("1F+b101", new SyntaxContext(CalculatorApp.BaseN)).Succeeded.Should().BeTrue();
        ExpressionParser.Parse("Sum(A1:B3)", new SyntaxContext(CalculatorApp.Spreadsheet)).Succeeded.Should().BeTrue();
        ExpressionParser.Parse("2∠45", new SyntaxContext(CalculatorApp.Complex)).Succeeded.Should().BeTrue();

        // Relations are read only when Verify is on (manual p. 73).
        ExpressionParser.Parse("1≤1<1+1", SyntaxContext.Calculate).Succeeded.Should().BeFalse();
        ExpressionParser.Parse("1≤1<1+1", new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void PrintingATreeBack()
    {
        SyntaxNode tree = ExpressionParser.Parse("sqrt(2)*pi", SyntaxContext.Calculate).Root!;

        LinearPrinter.Print(tree, SyntaxContext.Calculate).Should().Be("√(2)×π");
        LatexPrinter.Print(tree).Should().Be(@"\sqrt{2}\times \pi ");
        SyntaxEquivalence.AreEquivalent(tree, ExpressionParser.Parse("√(2)×π", SyntaxContext.Calculate).Root!).Should().BeTrue();
    }

    [Fact]
    public void PrintingOneName()
    {
        SyntaxSymbol avogadro = SyntaxVocabulary.Standard.Symbols.First(symbol => symbol.Text == "@N_A");

        LatexPrinter.Print(avogadro).Should().Be("N_{A}");
    }

    [Fact]
    public void AVocabularyOfOnesOwn()
    {
        SyntaxSymbol beam = SyntaxSymbol.CreateName("beam(", SymbolKind.Function);
        SyntaxVocabulary vocabulary = SyntaxVocabulary.Standard.With(beam);

        ExpressionParser.Parse("2beam(3,4)", SyntaxContext.Calculate, vocabulary).Succeeded.Should().BeTrue();
        ExpressionParser.Parse("2beam(3,4)", SyntaxContext.Calculate).Succeeded.Should().BeFalse("the standard vocabulary has no beam(");
    }
}
