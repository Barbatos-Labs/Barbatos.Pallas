// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// Reading a calculation back onto the line: what comes out of the history is what went into it, and it can be
/// edited as if it had been typed.
/// </summary>
public sealed class MathDocumentReaderTests
{
    [Theory]
    // Everything the reader knows a structure for, and a few things it does not.
    [InlineData("1+2×3")]
    [InlineData("12⌟5")]
    [InlineData("(1+2)⌟3")]
    [InlineData("3⌟1⌟4")]
    [InlineData("√(2)")]
    [InlineData("3ˣ√(8)")]
    [InlineData("2^(10)")]
    [InlineData("(1⌟2)^(3)")]
    [InlineData("Abs(1-2)")]
    [InlineData("log(2,16)")]
    [InlineData("∫(x²,0,1)")]
    [InlineData("Σ(x+1,1,5)")]
    [InlineData("Π(x,1,5)")]
    [InlineData("d/dx(x³,0.1)")]
    [InlineData("sin(30°)")]
    [InlineData("GCD(28,35)")]
    [InlineData("2°20′30″")]
    [InlineData("5cm▶in")]
    [InlineData("3.(021)")]
    [InlineData("10C4+10P4")]
    [InlineData("5!+150×20%")]
    [InlineData("−5+Ans-PreAns")] // A sign is the calculator's own minus; a subtraction is a hyphen.
    [InlineData("999_k")]
    [InlineData("@N_A")]
    [InlineData("f(2)+g(3)")]
    [InlineData("1⌟3+2⌟3")]
    public void WhatWasCalculatedComesBackAsItWasWritten(string text)
    {
        MathDocument document = MathDocumentReader.Read(text);

        MathLinearWriter.Write(document).Should().Be(text);
        document.Cursor.Depth.Should().Be(0, "the cursor waits at the end of the line");
        document.Cursor.Index.Should().Be(document.Root.Count);
    }

    [Theory]
    [InlineData(CalculatorApp.Complex, "2+3i")]
    [InlineData(CalculatorApp.Complex, "2∠45")]
    [InlineData(CalculatorApp.BaseN, "d10+h1F")]
    [InlineData(CalculatorApp.BaseN, "1010 and 1100")]
    [InlineData(CalculatorApp.Matrix, "Det(MatA)")]
    [InlineData(CalculatorApp.Vector, "VctA•VctB")]
    [InlineData(CalculatorApp.Statistics, "Σxy+x̄")]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:A5)")]
    [InlineData(CalculatorApp.Calculate, "1≤1<1+1")]
    public void EveryApplicationReadsItsOwnSyntaxBack(CalculatorApp app, string text)
    {
        MathDocument document = MathDocumentReader.Read(text, app);

        MathLinearWriter.Write(document).Should().Be(text);
    }

    [Fact]
    public void AnAliasComesBackAsWhatTheCalculatorWrites()
    {
        // The parser reads the keyboard's hyphen; what it prints back is the calculator's own minus sign.
        MathDocument document = MathDocumentReader.Read("-5*2/4");

        MathLinearWriter.Write(document).Should().Be("−5×2÷4");
    }

    [Fact]
    public void AFractionComesBackAsAFractionAndNotAsCharacters()
    {
        MathDocument document = MathDocumentReader.Read("12⌟5");

        document.Root.Count.Should().Be(1);
        document.Root[0].Should().BeOfType<MathStructure>().Which.Kind.Should().Be(MathTemplateKind.Fraction);
        MathLatexWriter.Write(document, caret: false).Should().Be(@"\frac{12}{5}", "so the screen draws it as a fraction");
    }

    [Fact]
    public void AFunctionComesBackAsOneSymbol()
    {
        MathDocument document = MathDocumentReader.Read("sin(30)");

        document.Root.Count.Should().Be(4, "the name and its bracket are one symbol, then 3, 0 and the closing bracket");
        document.Root[0].Should().Be(new MathSymbol("sin("));
    }

    [Fact]
    public void ANumberComesBackDigitByDigit()
    {
        MathDocument document = MathDocumentReader.Read("125");

        document.Root.Count.Should().Be(3, "a number is deleted one digit at a time, as it was typed");
    }

    [Fact]
    public void WhatWasReadBackCanBeEdited()
    {
        MathDocument document = MathDocumentReader.Read("1⌟2");

        document = document.Backspace().Insert("3");

        MathLinearWriter.Write(document).Should().Be("1⌟23", "the cursor stepped into the denominator, as it would after typing");
    }

    [Theory]
    // A line that was never a calculation comes back as it was left.
    [InlineData("1+")]
    [InlineData("sin(")]
    [InlineData("((")]
    [InlineData("×÷")]
    [InlineData("")]
    public void WhatIsNotACalculationComesBackAsCharacters(string text)
    {
        MathDocument document = MathDocumentReader.Read(text);

        MathLinearWriter.Write(document).Should().Be(text);
        document.Root.Count.Should().Be(text.Length);
    }

    [Fact]
    public void TextIsRequired()
    {
        Action act = () => MathDocumentReader.Read(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
