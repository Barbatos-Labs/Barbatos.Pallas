// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The edges of the display: the bounds of each form, the two profiles, and the settings that only change text.
/// </summary>
public sealed class DisplayEdgeTests
{
    private static string Format(string input, FormatTarget target, CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Calculator.Session(profile: profile);
        return session.Format(session.Calculate(input), target)?.Text ?? "<none>";
    }

    [Theory]
    // Manual p. 45: a factor of 1018081 = 1009² or more is left in parentheses, because the calculator divides by
    // primes below 1000 only.
    [InlineData("994009", "997²")]
    [InlineData("999983", "999983")]
    [InlineData("1018081", "(1018081)")]
    [InlineData("2036162", "2×(1018081)")]
    [InlineData("9999999967", "(9999999967)")]
    [InlineData("512", "2^9")]
    [InlineData("6", "2×3")]
    public void PrimeFactors_FollowTheCalculatorsLimits(string input, string expected)
    {
        Format(input, FormatTarget.PrimeFactor).Should().Be(expected);
    }

    [Theory]
    [InlineData("1018081", "1009²")]
    [InlineData("9999999967", "9999999967")]
    [InlineData("1000000000000000", "<none>")]
    public void PrimeFactors_AreCompleteInTheExtendedProfile(string input, string expected)
    {
        Format(input, FormatTarget.PrimeFactor, CalculatorProfile.Extended).Should().Be(expected);
    }

    [Theory]
    [InlineData("1⌟7", "0.(142857)")]
    [InlineData("-1⌟3", "-0.(3)")]
    [InlineData("1⌟6", "0.1(6)")]
    [InlineData("10⌟3", "3.(3)")]
    [InlineData("1⌟97", "<none>")]
    [InlineData("1⌟8", "<none>")]
    [InlineData("5", "<none>")]
    public void RecurringDecimals_HaveTheirOwnBounds(string input, string expected)
    {
        // 1/97 repeats over 96 digits, past the 99 bytes the display holds (p. 46); 1/8 terminates.
        Format(input, FormatTarget.RecurringDecimal).Should().Be(expected);
    }

    [Theory]
    [InlineData("1.25", "1°15′0″")]
    [InlineData("0", "0°0′0″")]
    [InlineData("-0.5", "-0°30′0″")]
    [InlineData("9999999.999", "9999999°59′56.4″")]
    [InlineData("10000000", "<none>")]
    public void Sexagesimal_StopsAtTheDisplayBound(string input, string expected)
    {
        Format(input, FormatTarget.Sexagesimal).Should().Be(expected);
    }

    [Theory]
    [InlineData("1234", "1.234×10^3")]
    [InlineData("0.000005", "5×10^-6")]
    [InlineData("0", "0")]
    [InlineData("999999", "999.999×10^3")]
    public void Engineering_MovesTheExponentToAMultipleOfThree(string input, string expected)
    {
        Format(input, FormatTarget.Engineering).Should().Be(expected);
    }

    [Theory]
    // With Engineer Symbol on, an exponent between f (10⁻¹⁵) and E (10¹⁸) is written as its symbol (p. 64).
    [InlineData("1500", "1.5k")]
    [InlineData("0.0015", "1.5m")]
    [InlineData("1.5×10^18", "1.5E")]
    [InlineData("1.5×10^-15", "1.5f")]
    [InlineData("1.5×10^21", "1.5×10^21")]
    [InlineData("1.5×10^-18", "1.5×10^-18")]
    [InlineData("1.5", "1.5")]
    [InlineData("0", "0")]
    public void EngineerSymbol_HasTheRangeOfItsSymbols(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            EngineerSymbol = true,
        }).Should().Be(expected);
    }

    [Fact]
    public void EngineeringShifts_MoveTheDecimalPointByThree()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { EngineerSymbol = true });
        Calculation calculation = session.Calculate("1234000");

        session.FormatEngineering(calculation, shift: 0).Should().Be("1.234M");
        session.FormatEngineering(calculation, shift: -1).Should().Be("1234k");
        session.FormatEngineering(calculation, shift: 1).Should().Be("0.001234G");
        session.FormatEngineering(session.Calculate("1÷0"), shift: 0).Should().BeNull();
    }

    [Theory]
    // The display bounds of the calculator: a fraction whose mixed form needs more than ten digits and separators,
    // and a square-root form with a coefficient of 100 or more, are shown as decimals (p. 35 and p. 46).
    [InlineData("1⌟1⌟123456", CalculatorProfile.Standard, "123457⌟123456")]
    [InlineData("1⌟1⌟1234567", CalculatorProfile.Standard, "1.00000081")]
    [InlineData("1⌟1⌟1234567", CalculatorProfile.Extended, "1234568⌟1234567")]
    [InlineData("99√(999)", CalculatorProfile.Standard, "3129.089165")]
    [InlineData("99√(999)", CalculatorProfile.Extended, "297√(111)")]
    [InlineData("√(1000)", CalculatorProfile.Standard, "10√(10)")]
    [InlineData("√(20000)", CalculatorProfile.Standard, "141.4213562")]
    [InlineData("√(20000)", CalculatorProfile.Extended, "100√(2)")]
    public void DisplayBounds_DependOnTheProfile(string input, CalculatorProfile profile, string expected)
    {
        Calculator.Session(profile: profile).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("√(2)", "√(2)")]
    [InlineData("-√(2)", "-√(2)")]
    [InlineData("√(2)÷2", "√(2)⌟2")]
    [InlineData("3√(2)÷4", "3√(2)⌟4")]
    [InlineData("1+√(2)", "1+√(2)")]
    [InlineData("1-√(2)", "1-√(2)")]
    [InlineData("-1-√(2)", "-1-√(2)")]
    [InlineData("√(2)-√(3)", "-√(3)+√(2)")]
    [InlineData("π", "π")]
    [InlineData("-π", "-π")]
    [InlineData("2π", "2π")]
    [InlineData("π÷6", "1⌟6π")]
    [InlineData("-π÷6", "-1⌟6π")]
    [InlineData("1000000π", "3141592.654")]
    public void ExactForms_AreWrittenTheWayTheCalculatorWritesThem(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    // Recognized from the value of a function result rather than from a form.
    [InlineData("sin(45)", "√(2)⌟2")]
    [InlineData("sin(60)", "√(3)⌟2")]
    [InlineData("cos⁻¹(-1)", "180")]
    [InlineData("tan(45)", "1")]
    [InlineData("sin(30)", "1⌟2")]
    public void FormsAreRecognizedFromApproximateValuesToo(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void PiIsRecognizedFromAValueInRadians()
    {
        Calculator.Display("cos⁻¹(-1)", settings: settings => settings with { AngleUnit = Numerics.AngleUnit.Radian }).Should().Be("π");
        Calculator.Display("sin⁻¹(1)", settings: settings => settings with { AngleUnit = Numerics.AngleUnit.Radian }).Should().Be("1⌟2π");
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("-0.000001", "-1×10^-6")]
    [InlineData("1000000000", "1000000000")]
    [InlineData("10000000000", "1×10^10")]
    [InlineData("0.01", "0.01")]
    [InlineData("0.001", "1×10^-3")]
    [InlineData("123456789012", "1.23456789×10^11")]
    public void Norm1_SwitchesFormAtItsBounds(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO }).Should().Be(expected);
    }

    [Theory]
    [InlineData("0.001", "0.001")]
    [InlineData("0.0000000001", "1×10^-10")]
    [InlineData("0.000000001", "0.000000001")]
    public void Norm2_KeepsSmallerNumbersInPlace(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Norm2,
        }).Should().Be(expected);
    }

    [Theory]
    [InlineData(DecimalMark.Dot, false, "1234567.5")]
    [InlineData(DecimalMark.Dot, true, "1,234,567.5")]
    [InlineData(DecimalMark.Comma, false, "1234567,5")]
    [InlineData(DecimalMark.Comma, true, "1.234.567,5")]
    public void DecimalMarkAndSeparator_ChangeTextOnly(DecimalMark mark, bool separator, string expected)
    {
        Calculator.Display("1234567.5", settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            DecimalMark = mark,
            DigitSeparator = separator,
        }).Should().Be(expected);
    }

    [Fact]
    public void TheDigitSeparator_LeavesExponentsAlone()
    {
        Calculator.Display("1.234567×10^12", settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            DigitSeparator = true,
        }).Should().Be("1.234567×10^12");
    }

    [Fact]
    public void SeveralResults_AreSeparatedByTheDecimalMarksPartner()
    {
        // With a comma decimal mark the separator between results is a semicolon (pp. 24-25).
        CalculatorSession session = Calculator.Session(settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            DecimalMark = DecimalMark.Comma,
        });

        session.Calculate("5÷R2").Display.Text.Should().Be("2; R=1");
        session.Calculate("Pol(1,1)").Display.Text.Should().Be("r=1,414213562; θ=45");
    }

    [Fact]
    public void FixAndSciHaveTheirOwnEdges()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Fix(9),
        });

        session.Calculate("1÷3").Display.Text.Should().Be("0.333333333");
        session.Calculate("12345678901").Display.Text.Should().Be("1.234567890×10^10", "Fix has no room past ten digits");

        session.Settings = session.Settings with { NumberFormat = NumberFormat.Sci(10) };
        session.Calculate("1÷3").Display.Text.Should().Be("3.333333333×10^-1");

        session.Settings = session.Settings with { NumberFormat = NumberFormat.Sci(1) };
        session.Calculate("0.0000001234").Display.Text.Should().Be("1×10^-7");
    }

    [Fact]
    public void ADoubleValueIsStillRoundedForTheDisplay()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Fix(3),
        });

        session.Calculate("10^-20").Display.Text.Should().Be("0.000");
        session.Calculate("10^30").Display.Text.Should().Be("1.000×10^30");
    }
}
