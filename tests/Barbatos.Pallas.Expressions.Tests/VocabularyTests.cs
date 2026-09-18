// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class VocabularyTests
{
    private static IEnumerable<SyntaxSymbol> Canonical => SyntaxVocabulary.Standard.Symbols.Where(symbol => ReferenceEquals(symbol, symbol.Canonical));

    [Theory]
    // The CATALOG of the reference calculator (pp. 64-66): 47 scientific constants, 40 unit conversions, 11 engineering symbols.
    [InlineData(SymbolKind.ScientificConstant, 47)]
    [InlineData(SymbolKind.UnitConversion, 40)]
    [InlineData(SymbolKind.EngineeringSymbol, 11)]
    [InlineData(SymbolKind.RelationOperator, 6)]
    public void Standard_HasTheCatalogsCounts(SymbolKind kind, int count)
    {
        Canonical.Count(symbol => symbol.Kind == kind).Should().Be(count);
    }

    [Fact]
    public void Standard_SpellingsAreUnique_AndAliasesPointAtCanonicalSymbols()
    {
        SyntaxVocabulary.Standard.Symbols.Select(symbol => symbol.Text).Should().OnlyHaveUniqueItems();
        SyntaxVocabulary.Standard.Symbols.Should().AllSatisfy(symbol =>
        {
            symbol.Canonical.Canonical.Should().BeSameAs(symbol.Canonical);
            symbol.Kind.Should().Be(symbol.Canonical.Kind);
            symbol.ToString().Should().Be(symbol.Text);
        });
    }

    [Fact]
    public void Operators_CarryTheirOperation()
    {
        SyntaxSymbol plus = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "+");
        SyntaxSymbol square = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "²");
        SyntaxSymbol lessOrEqual = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "<=");

        (plus.BinaryOperator, plus.PostfixOperator, plus.RelationOperator).Should().Be((BinaryOperator.Add, (PostfixOperator?)null, (RelationOperator?)null));
        square.PostfixOperator.Should().Be(PostfixOperator.Square);
        lessOrEqual.RelationOperator.Should().Be(RelationOperator.LessOrEqual);
    }

    [Theory]
    [InlineData("i", CalculatorApp.Calculate, false)]
    [InlineData("i", CalculatorApp.Complex, true)]
    [InlineData("sin(", CalculatorApp.BaseN, false)]
    [InlineData("sin(", CalculatorApp.Complex, true)]
    [InlineData("and", CalculatorApp.BaseN, true)]
    [InlineData("and", CalculatorApp.Calculate, false)]
    [InlineData("MatA", CalculatorApp.Vector, false)]
    [InlineData("x̄", CalculatorApp.Statistics, true)]
    public void Symbols_AreAvailableInTheirApplications(string text, CalculatorApp app, bool available)
    {
        SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == text).IsAvailableIn(app).Should().Be(available);
    }

    [Fact]
    public void CreateName_NormalizesSpellings_AndKeepsApplications()
    {
        SyntaxSymbol symbol = SyntaxSymbol.CreateName("y\u0304bar", SymbolKind.Variable, CalculatorApp.Statistics, CalculatorApp.Table);

        symbol.Text.Should().Be("ȳbar");
        symbol.Applications.Should().Equal(CalculatorApp.Statistics, CalculatorApp.Table);
        symbol.Canonical.Should().BeSameAs(symbol);
    }

    [Theory]
    [InlineData("beam", SymbolKind.Function)]
    [InlineData("(", SymbolKind.Function)]
    [InlineData("h", SymbolKind.ScientificConstant)]
    [InlineData("@", SymbolKind.ScientificConstant)]
    [InlineData("k", SymbolKind.EngineeringSymbol)]
    [InlineData("_", SymbolKind.EngineeringSymbol)]
    [InlineData("cm-in", SymbolKind.UnitConversion)]
    [InlineData("x(", SymbolKind.Variable)]
    [InlineData("2x", SymbolKind.Variable)]
    [InlineData(".x", SymbolKind.Variable)]
    [InlineData(" x", SymbolKind.Variable)]
    [InlineData("x ", SymbolKind.Variable)]
    [InlineData("", SymbolKind.Variable)]
    public void CreateName_RejectsSpellingsTheLexerCouldNotMatch(string text, SymbolKind kind)
    {
        Action act = () => SyntaxSymbol.CreateName(text, kind);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(SymbolKind.BinaryOperator)]
    [InlineData(SymbolKind.OpenParenthesis)]
    [InlineData((SymbolKind)(-1))]
    public void CreateName_RejectsGrammarKinds(SymbolKind kind)
    {
        Action act = () => SyntaxSymbol.CreateName("x", kind);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("_kilo", SymbolKind.EngineeringSymbol)]
    [InlineData("@g_0", SymbolKind.ScientificConstant)]
    [InlineData("ly->km", SymbolKind.UnitConversion)]
    [InlineData("ly▶km", SymbolKind.UnitConversion)]
    [InlineData("tau", SymbolKind.Constant)]
    public void CreateName_AcceptsEachKindsSpelling(string text, SymbolKind kind)
    {
        SyntaxSymbol.CreateName(text, kind).Kind.Should().Be(kind);
    }

    [Fact]
    public void CreateAlias_ValidatesNames_ButNotGrammar()
    {
        SyntaxSymbol beam = SyntaxSymbol.CreateName("beam(", SymbolKind.Function);
        SyntaxSymbol plus = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "+");

        SyntaxSymbol conversion = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "cm▶in");

        Action badName = () => beam.CreateAlias("girder");
        Action empty = () => beam.CreateAlias(" ");
        Action emptyOperator = () => plus.CreateAlias(" ");
        Action badConversion = () => conversion.CreateAlias("cm to in");

        badName.Should().Throw<ArgumentException>();
        empty.Should().Throw<ArgumentException>();
        emptyOperator.Should().Throw<ArgumentException>();
        badConversion.Should().Throw<ArgumentException>();
        conversion.CreateAlias("cm=>in▶").Canonical.Should().BeSameAs(conversion);
        plus.CreateAlias("plus").Canonical.Should().BeSameAs(plus);
        beam.CreateAlias("girder(").CreateAlias("span(").Canonical.Should().BeSameAs(beam);
    }

    [Fact]
    public void With_AddsNames_AndLeavesTheOriginalUnchanged()
    {
        SyntaxSymbol beam = SyntaxSymbol.CreateName("beam(", SymbolKind.Function);

        SyntaxVocabulary extended = SyntaxVocabulary.Standard.With(beam, SyntaxSymbol.CreateName("ly▶km", SymbolKind.UnitConversion));

        extended.Symbols.Should().Contain(beam);
        SyntaxVocabulary.Standard.Symbols.Should().NotContain(beam);
        extended.Symbols.Length.Should().Be(SyntaxVocabulary.Standard.Symbols.Length + 2);
    }

    [Fact]
    public void With_RejectsDuplicatesOperatorsAndNulls()
    {
        SyntaxSymbol plus = SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == "+");

        Action duplicate = () => SyntaxVocabulary.Standard.With(SyntaxSymbol.CreateName("sin(", SymbolKind.Function));
        Action twice = () => SyntaxVocabulary.Standard.With(SyntaxSymbol.CreateName("beam(", SymbolKind.Function), SyntaxSymbol.CreateName("beam(", SymbolKind.Function));
        Action grammar = () => SyntaxVocabulary.Standard.With(plus.CreateAlias("plus"));
        Action nullList = () => SyntaxVocabulary.Standard.With((IEnumerable<SyntaxSymbol>)null!);
        Action nullItem = () => SyntaxVocabulary.Standard.With([null!]);

        duplicate.Should().Throw<ArgumentException>();
        twice.Should().Throw<ArgumentException>();
        grammar.Should().Throw<ArgumentException>();
        nullList.Should().Throw<ArgumentNullException>();
        nullItem.Should().Throw<ArgumentNullException>();
    }
}
