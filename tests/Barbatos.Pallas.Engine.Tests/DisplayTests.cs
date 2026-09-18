// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// How results are displayed: number formats, fractions, square roots and π, and the FORMAT conversions.
/// </summary>
public sealed class DisplayTests
{
    [Theory]
    // Norm 1 uses exponent form below 10⁻² and from 10¹⁰; Norm 2 below 10⁻⁹ (p. 23).
    [InlineData("1÷200", 1, "5×10^-3")]
    [InlineData("1÷200", 2, "0.005")]
    [InlineData("1÷6", 1, "0.1666666667")]
    [InlineData("12345678901", 1, "1.23456789×10^10")]
    [InlineData("1234567890", 1, "1234567890")]
    public void Norm_SwitchesToExponentFormAtItsBounds(string input, int norm, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = norm == 1 ? NumberFormat.Norm1 : NumberFormat.Norm2,
        }).Should().Be(expected);
    }

    [Theory]
    [InlineData("1÷6", 3, "0.167")]
    [InlineData("1÷6", 0, "0")]
    [InlineData("-1÷1000", 2, "0.00")]
    [InlineData("2.5", 0, "3")]
    public void Fix_RoundsHalfAwayFromZero(string input, int decimals, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Fix(decimals),
        }).Should().Be(expected);
    }

    [Theory]
    [InlineData("1÷6", 3, "1.67×10^-1")]
    [InlineData("1234", 5, "1.2340×10^3")]
    public void Sci_KeepsTheDigitsItIsAskedFor(string input, int digits, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Sci(digits),
        }).Should().Be(expected);
    }

    [Theory]
    [InlineData("2⌟3+1⌟1⌟2", "13⌟6")]
    [InlineData("3÷2", "3⌟2")]
    [InlineData("10⁻¹", "1⌟10")]
    [InlineData("sin(30)", "1⌟2")]
    [InlineData("√(2)×3", "3√(2)")]
    [InlineData("10√(2)+15×3√(3)", "45√(3)+10√(2)")]
    [InlineData("99√(999)", "3129.089165")]
    [InlineData("π÷6", "1⌟6π")]
    [InlineData("sin(45)", "√(2)⌟2")]
    [InlineData("cos(30)", "√(3)⌟2")]
    [InlineData("1+√(2)", "1+√(2)")]
    [InlineData("ln(90)", "4.49980967")]
    public void MathO_ShowsFractionsRootsAndPi(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void MixedFractionSetting_ChangesHowAFractionIsWritten()
    {
        Calculator.Display("13⌟4", settings: settings => settings with { FractionResult = FractionResult.Mixed }).Should().Be("3⌟1⌟4");
        Calculator.Display("13⌟4", settings: settings => settings with { FractionResult = FractionResult.Improper }).Should().Be("13⌟4");
    }

    [Fact]
    public void FractionsTooLongForTheDisplay_AreShownAsDecimals()
    {
        // Ten digits with separators still fit (p. 46); eleven do not.
        Func<CalculatorSettings, CalculatorSettings> lineO = settings => settings with { InputOutput = InputOutput.LineILineO };

        Calculator.Display("1⌟1⌟123456", settings: lineO).Should().Be("123457⌟123456");
        Calculator.Display("1⌟1⌟1234567", settings: lineO).Should().Be("1.00000081");
    }

    [Fact]
    public void LineOutput_ShowsAFractionOnlyForFractionInput()
    {
        Func<CalculatorSettings, CalculatorSettings> lineO = settings => settings with { InputOutput = InputOutput.LineILineO };

        Calculator.Display("2⌟3+1⌟1⌟2", settings: lineO).Should().Be("13⌟6");
        Calculator.Display("3.25", settings: lineO).Should().Be("3.25");
        Calculator.Display("√(2)×3", settings: lineO).Should().Be("4.242640687", "LineO has no square-root forms");
    }

    [Theory]
    [InlineData(FormatTarget.DecimalValue, "0.5235987756")]
    [InlineData(FormatTarget.Standard, "1⌟6π")]
    public void FormatMenu_ConvertsTheDisplayedResult(FormatTarget target, string expected)
    {
        CalculatorSession session = Calculator.Session();
        Calculation calculation = session.Calculate("π÷6");

        session.Format(calculation, target)!.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("1014", "2×3×13²")]
    [InlineData("2036162", "2×(1018081)")]
    [InlineData("1", "1")]
    [InlineData("1024", "2^10")]
    public void PrimeFactor_FollowsTheCalculatorsLimits(string input, string expected)
    {
        CalculatorSession session = Calculator.Session();

        session.Format(session.Calculate(input), FormatTarget.PrimeFactor)!.Text.Should().Be(expected);
    }

    [Fact]
    public void PrimeFactor_IsCompleteInTheExtendedProfile()
    {
        // Deviation D7: 1018081 = 1009², which the calculator leaves unfactored.
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended);

        session.Format(session.Calculate("2036162"), FormatTarget.PrimeFactor)!.Text.Should().Be("2×1009²");
    }

    [Theory]
    [InlineData("-5", null)]
    [InlineData("2.5", null)]
    [InlineData("10000000000", null)]
    public void PrimeFactor_AppliesToPositiveIntegersOnly(string input, string? expected)
    {
        CalculatorSession session = Calculator.Session();

        session.Format(session.Calculate(input), FormatTarget.PrimeFactor)?.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("3.(021)+0.(312)", "3.(3)")]
    [InlineData("1⌟6", "0.1(6)")]
    [InlineData("1⌟4", null)]
    public void RecurringDecimal_IsShownWhenTheFractionRepeats(string input, string? expected)
    {
        CalculatorSession session = Calculator.Session();

        session.Format(session.Calculate(input), FormatTarget.RecurringDecimal)?.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.25", "1°15′0″")]
    [InlineData("-1.25", "-1°15′0″")]
    [InlineData("2°20′30″+0°9′30″", "2°30′0″")]
    public void Sexagesimal_ConvertsDecimalDegrees(string input, string expected)
    {
        CalculatorSession session = Calculator.Session();

        session.Format(session.Calculate(input), FormatTarget.Sexagesimal)!.Text.Should().Be(expected);
    }

    [Fact]
    public void Engineering_UsesExponentsThatAreMultiplesOfThree()
    {
        CalculatorSession session = Calculator.Session();

        session.Format(session.Calculate("1234"), FormatTarget.Engineering)!.Text.Should().Be("1.234×10^3");
        session.Format(session.Calculate("0.005"), FormatTarget.Engineering)!.Text.Should().Be("5×10^-3");
    }

    [Fact]
    public void EngineerSymbol_WritesTheExponentAsASymbol()
    {
        // Manual p. 64: 999k + 25k is 1.024M, and moving the decimal point right shows 1024k.
        CalculatorSession session = Calculator.Session(settings: settings => settings with { EngineerSymbol = true });
        Calculation calculation = session.Calculate("999_k+25_k");

        calculation.Display.Text.Should().Be("1.024M");
        session.FormatEngineering(calculation, shift: -1).Should().Be("1024k");
        session.FormatEngineering(calculation, shift: 0).Should().Be("1.024M");
    }

    [Fact]
    public void DecimalMarkAndDigitSeparator_ChangeOnlyTheText()
    {
        Func<CalculatorSettings, CalculatorSettings> european = settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            DecimalMark = DecimalMark.Comma,
            DigitSeparator = true,
        };

        Calculator.Display("1234567.25", settings: european).Should().Be("1.234.567,25");
        Calculator.Display("1234567.25", settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO, DigitSeparator = true })
            .Should().Be("1,234,567.25");
    }

    [Fact]
    public void Latex_IsWrittenByTheExpressionsPrinter()
    {
        Calculator.Session().Calculate("2⌟3+1⌟1⌟2").Display.Latex.Should().Be(@"\frac{13}{6}");
        Calculator.Session().Calculate("√(2)×3").Display.Latex.Should().Be(@"3 \sqrt{2}");
    }

    [Fact]
    public void AValueCanBeDisplayedOnItsOwn()
    {
        // The variable list shows values in Norm 1 (p. 38).
        Value value = Calculator.Evaluate("3+5");

        PallasEngine.Format(value, CalculatorSettings.Initial with { InputOutput = InputOutput.MathIDecimalO })!.Text.Should().Be("8");
    }

    [Fact]
    public void ApproximateValues_AreNotDressedUpAsFractionsWithLargeDenominators()
    {
        // Recognition from an approximate value is limited to small forms, so a transcendental result stays a decimal.
        Calculator.Display("sinh(1)").Should().Be("1.175201194");
        Calculator.Display("ln(2)").Should().Be("0.6931471806");
        Calculator.Display("√(2)").Should().Be("√(2)");
    }

    [Fact]
    public void BaseN_DisplaysThePatternOfItsNumberMode()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);
        session.Settings = session.Settings with { BaseMode = NumberBase.Bin };

        session.Calculate("Not(1010)").Display.Text.Should().Be("11111111111111111111111111110101");

        session.Settings = session.Settings with { BaseMode = NumberBase.Hex };
        session.Calculate("d255").Display.Text.Should().Be("FF");

        session.Settings = session.Settings with { BaseMode = NumberBase.Oct };
        session.Calculate("d64").Display.Text.Should().Be("100");
    }
}
