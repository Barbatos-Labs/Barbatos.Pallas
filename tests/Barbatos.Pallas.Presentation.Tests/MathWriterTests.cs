// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The two roads out of the math input: Canonical Linear Syntax for the engine, LaTeX for the screen. The same tree
/// writes both, so what is displayed and what is calculated cannot drift apart.
/// </summary>
public sealed class MathWriterTests
{
    private const string Caret = @"\color{red}{|}";

    private static MathDocument Typed(MathDocument document, string symbols)
    {
        foreach (char character in symbols)
        {
            document = document.Insert(character.ToString());
        }

        return document;
    }

    private static MathDocument Typed(string symbols) => Typed(MathDocument.Empty, symbols);

    [Fact]
    public void AnEmptyInputIsABoxWithTheCursorBeforeIt()
    {
        MathLatexWriter.Write(MathDocument.Empty).Should().Be(Caret + @"\square");
        MathLatexWriter.Write(MathDocument.Empty, caret: false).Should().Be(@"\square");
    }

    [Fact]
    public void TheCursorIsDrawnWhereItStands()
    {
        MathDocument document = Typed("1+2");

        MathLatexWriter.Write(document).Should().Be("1+2" + Caret);
        MathLatexWriter.Write(document.MoveLeft()).Should().Be("1+" + Caret + "2");
        MathLatexWriter.Write(document.MoveToStart()).Should().Be(Caret + "1+2");
    }

    [Fact]
    public void TheCursorIsDrawnInsideTheSlotItIsIn()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction);
        document = Typed(document, "2");

        MathLatexWriter.Write(document).Should().Be(@"\frac{1}{2" + Caret + "}");
        MathLatexWriter.Write(document.MoveUp()).Should().Be(@"\frac{1" + Caret + "}{2}");
        MathLatexWriter.Write(document.MoveToEnd()).Should().Be(@"\frac{1}{2}" + Caret);
    }

    [Fact]
    public void AnEmptySlotIsABox()
    {
        MathDocument document = MathDocument.Empty.Insert(MathTemplateKind.Fraction);

        MathLatexWriter.Write(document).Should().Be(@"\frac{" + Caret + @"\square}{\square}");
    }

    [Fact]
    public void OneCursorIsDrawnWhereverItIs()
    {
        MathDocument document = Typed("2").Insert(MathTemplateKind.Power).Insert(MathTemplateKind.Fraction);
        document = Typed(document, "1");

        string latex = MathLatexWriter.Write(document);

        latex.Should().Be(@"{2}^{\frac{1" + Caret + @"}{\square}}");
        MathLatexWriter.Write(document, caret: false).Should().NotContain(Caret);
    }

    [Theory]
    // Every template, as the engine reads it and as the screen draws it.
    [InlineData(MathTemplateKind.Parentheses, "(1)", @"\left(1\right)")]
    [InlineData(MathTemplateKind.Fraction, "1⌟", @"\frac{1}{\square}")]
    [InlineData(MathTemplateKind.MixedFraction, "1⌟⌟", @"1\frac{\square}{\square}")]
    [InlineData(MathTemplateKind.SquareRoot, "√(1)", @"\sqrt{1}")]
    [InlineData(MathTemplateKind.Root, "1ˣ√()", @"\sqrt[1]{\square}")]
    [InlineData(MathTemplateKind.Power, "1^()", @"{1}^{\square}")]
    [InlineData(MathTemplateKind.Abs, "Abs(1)", @"\left|1\right|")]
    [InlineData(MathTemplateKind.LogBase, "log(1,)", @"\log_{1}\left(\square\right)")]
    [InlineData(MathTemplateKind.Integral, "∫(1,,)", @"\int_{\square}^{\square} 1\,dx")]
    [InlineData(MathTemplateKind.Sum, "Σ(1,,)", @"\sum_{x=\square}^{\square} 1")]
    [InlineData(MathTemplateKind.Product, "Π(1,,)", @"\prod_{x=\square}^{\square} 1")]
    [InlineData(MathTemplateKind.Derivative, @"d/dx(1,)", @"\left.\frac{d}{dx}\left(1\right)\right|_{x=\square}")]
    public void EveryTemplateIsWrittenBothWays(MathTemplateKind kind, string linear, string latex)
    {
        MathDocument document = MathDocument.Empty.Insert(kind).Insert("1");

        MathLinearWriter.Write(document).Should().Be(linear);
        MathLatexWriter.Write(document, caret: false).Should().Be(latex);
    }

    [Theory]
    [InlineData("sin(", @"\sin(")]
    [InlineData("×", @"\times ")]
    [InlineData("÷", @"\div ")]
    [InlineData("−", "-")]
    [InlineData("π", @"\pi ")]
    [InlineData("²", "{}^{2}", "a square with nothing to square still draws")]
    [InlineData("⁻¹", "{}^{-1}", "and TeX refuses a script without a base")]
    [InlineData("°", @"{}^{\circ}", "and so does a degree sign")]
    [InlineData("%", @"\%")]
    [InlineData("Ran#", @"\text{Ran\#}")]
    [InlineData("Ans", @"\mathrm{Ans}")]
    [InlineData("MatA", @"\mathrm{MatA}")]
    [InlineData("Rnd(", @"\mathrm{Rnd}(")]
    [InlineData("A", "A")]
    [InlineData("7", "7")]
    [InlineData("+", "+")]
    [InlineData("$A$1", @"\text{\$}A\text{\$}1")]
    [InlineData("σx", @"\sigma x", "WpfMath has no σ as a character, only as a command")]
    [InlineData("Σy", @"\Sigma y")]
    [InlineData("▶t", @"\blacktriangleright t")]
    [InlineData("σ²x", @"\sigma ^{2}x", "a statistic name with a square in it, as the CATALOG will type")]
    [InlineData("x̂₁", @"\hat{x}_{1}")]
    [InlineData("(", "(", "a lone bracket, as a line read back as characters has, is not the start of a function")]
    [InlineData("beamlength", @"\mathrm{beamlength}", "a name the standard vocabulary has not - a plugin's - is upright")]
    [InlineData("′", "{}'", "a minute mark with nothing before it is a script without a base, which TeX refuses")]
    [InlineData("″", "{}''")]
    [InlineData("@c", "c", "a constant is drawn by its name, as the history draws it")]
    [InlineData("@N_A", "N_{A}")]
    [InlineData("_k", @"\mathrm{k}")]
    [InlineData("÷R", @"\div_{\mathrm{R}} ")]
    [InlineData("and", @"\;\mathrm{and}\;", "a logic operator is a word between two numbers, spaced as the history spaces it")]
    [InlineData("xnor", @"\;\mathrm{xnor}\;")]
    public void ASymbolIsDrawnAsTheCalculatorWritesIt(string symbol, string latex, string? because = null)
    {
        MathLatexWriter.Write(MathDocument.Empty.Insert(symbol), caret: false).Should().Be(latex, because ?? string.Empty);
    }

    [Fact]
    public void EveryNameIsDrawnOnTheLineAsTheHistoryDrawsIt()
    {
        foreach (SyntaxSymbol name in SyntaxVocabulary.Standard.Symbols.Where(symbol => ReferenceEquals(symbol.Canonical, symbol)
            && symbol.Kind is SymbolKind.Constant or SymbolKind.ScientificConstant or SymbolKind.Variable or SymbolKind.Memory
                or SymbolKind.MatrixVariable or SymbolKind.VectorVariable or SymbolKind.StatisticsVariable
                or SymbolKind.EngineeringSymbol or SymbolKind.UnitConversion))
        {
            MathLatexWriter.Write(MathDocument.Empty.Insert(name.Text), caret: false)
                .Should().Be(LatexPrinter.Print(name), "'{0}' is one name, drawn one way", name.Text);
        }
    }

    [Theory]
    [InlineData("A", @"\mathrm{A}")]
    [InlineData("F", @"\mathrm{F}")]
    [InlineData("x", "x", "x is a variable in Base-N too")]
    public void InBaseNTheLettersOfANumberAreDrawnUprightAsDigitsAre(string symbol, string latex, string? because = null)
    {
        MathLatexWriter.Write(MathDocument.Empty.Insert(symbol), caret: false, CalculatorApp.BaseN).Should().Be(latex, because ?? string.Empty);
        MathLatexWriter.Write(MathDocument.Empty.Insert("A"), caret: false, CalculatorApp.Calculate).Should().Be("A", "elsewhere A is the variable");
    }

    [Fact]
    public void TheLineIsDrawnForItsApplication()
    {
        MathInputViewModel input = new() { App = CalculatorApp.BaseN };
        input.Press(KeyId.HexA);
        input.Latex.Should().Contain(@"\mathrm{A}");

        List<string?> changed = input.Changes();
        input.App = CalculatorApp.Calculate;

        changed.Should().Contain(nameof(MathInputViewModel.Latex), "the same letter is drawn another way in another application");
    }

    [Fact]
    public void ASlotIsBracketedOnlyWhereItHasToBe()
    {
        MathDocument twelve = Typed("12").Insert(MathTemplateKind.Fraction).Insert("5");
        MathDocument sum = MathDocument.Empty.Insert(MathTemplateKind.Fraction);
        sum = Typed(sum, "1+2").MoveDown();
        sum = Typed(sum, "3");
        MathDocument nested = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveToEnd().Insert(MathTemplateKind.Power).Insert("3");
        MathDocument root = MathDocument.Empty.Insert(MathTemplateKind.SquareRoot).Insert("2").MoveToEnd().Insert(MathTemplateKind.Fraction).Insert("3");

        MathLinearWriter.Write(twelve).Should().Be("12⌟5", "a number needs no brackets");
        MathLinearWriter.Write(sum).Should().Be("(1+2)⌟3", "a sum does");
        MathLinearWriter.Write(nested).Should().Be("(1⌟2)^(3)", "and so does a fraction, which would otherwise read as a mixed fraction");
        MathLinearWriter.Write(root).Should().Be("√(2)⌟3", "a root carries its own brackets");
    }

    [Theory]
    // What the editor writes, the parser reads back as the same thing.
    [InlineData("1+2×3")]
    [InlineData("sin(30)")]
    [InlineData("Abs(1-2)")]
    public void WhatIsTypedIsWhatTheEngineReads(string text)
    {
        MathDocument document = MathDocument.Empty;
        foreach (string symbol in Symbols(text))
        {
            document = document.Insert(symbol);
        }

        string linear = MathLinearWriter.Write(document);

        linear.Should().Be(text);
        ExpressionParser.Parse(linear, SyntaxContext.Calculate).Root.Should().NotBeNull();
    }

    [Fact]
    public void AStructuredInputParses()
    {
        // 1 + 2/3 raised to the fourth, with a square root inside: the shape the linear form has to survive.
        MathDocument document = Typed("1+");
        document = Typed(document.Insert(MathTemplateKind.Fraction), "2").MoveDown();
        document = Typed(document, "3").MoveToEnd();
        document = Typed(document.Insert(MathTemplateKind.Power), "4").MoveToEnd();
        document = Typed(document.Insert("×").Insert(MathTemplateKind.SquareRoot), "9").MoveToEnd();

        string linear = MathLinearWriter.Write(document);

        linear.Should().Be("1+(2⌟3)^(4)×√(9)");
        ExpressionParser.Parse(linear, SyntaxContext.Calculate).Root.Should().NotBeNull();
    }

    [Fact]
    public void ADocumentIsRequired()
    {
        Action linear = () => MathLinearWriter.Write((MathDocument)null!);
        Action row = () => MathLinearWriter.Write((MathRow)null!);
        Action latex = () => MathLatexWriter.Write(null!);
        Action latexWithout = () => MathLatexWriter.Write(null!, caret: false);
        Action latexOfAnApplication = () => MathLatexWriter.Write(null!, caret: false, CalculatorApp.BaseN);

        latexOfAnApplication.Should().Throw<ArgumentNullException>().WithParameterName("document");

        linear.Should().Throw<ArgumentNullException>();
        row.Should().Throw<ArgumentNullException>();
        latex.Should().Throw<ArgumentNullException>();
        latexWithout.Should().Throw<ArgumentNullException>();
    }

    private static IEnumerable<string> Symbols(string text)
    {
        // "sin(30)" is the symbols sin(, 3, 0, ) - a name and its bracket arrive together, as one key.
        for (int index = 0; index < text.Length; index++)
        {
            int open = text.IndexOf('(', index);
            if (open > index && char.IsAsciiLetter(text[index]))
            {
                yield return text[index..(open + 1)];
                index = open;
                continue;
            }

            yield return text[index].ToString();
        }
    }
}
