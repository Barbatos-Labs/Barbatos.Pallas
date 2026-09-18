// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The display forms at their bounds (p. 35, pp. 42-50, p. 64): what fits, what falls back to a decimal, and the signs.
/// </summary>
public sealed class DisplayFormTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian };

    private static string? Format(string input, FormatTarget target, CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Calculator.Session(profile: profile);
        return session.Format(session.Calculate(input), target)?.Text;
    }

    [Theory]
    // ±a√b⌟c ± d√e⌟f with a, c, d, f < 100 and b, e < 1000 (p. 35); beyond them, a decimal.
    [InlineData("99√(2)", "99√(2)")]
    [InlineData("100√(2)", "141.4213562")]
    [InlineData("√(2)÷99", "√(2)⌟99")]
    [InlineData("√(2)÷100", "0.01414213562")]
    [InlineData("√(997)", "√(997)")]
    [InlineData("√(1001)", "31.63858404")]
    [InlineData("99+√(2)", "99+√(2)")]
    [InlineData("100+√(2)", "101.4142136")]
    [InlineData("1⌟2+√(2)", "1⌟2+√(2)")]
    [InlineData("1⌟100+√(2)", "1.424213562")]
    [InlineData("1-√(2)", "1-√(2)")]
    [InlineData("-1-√(2)", "-1-√(2)")]
    [InlineData("-√(2)", "-√(2)")]
    public void SquareRootForms_FollowTheDisplayBounds(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    // A fraction whose mixed form needs more than 10 digits and separators is shown as a decimal (pp. 46, 171).
    [InlineData("1234⌟56789", "1234⌟56789")]
    [InlineData("12345⌟67891", "0.1818355894")]
    [InlineData("9999⌟7", "9999⌟7")]
    public void Fractions_FollowTheDisplayBounds(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void TheExtendedProfile_ShowsLargerCoefficients()
    {
        // 9999/7 as a decimal is off by 4×10⁻²⁵, beyond a fixed 10⁻²⁵: the tolerance grows with the coefficient.
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("9999÷7×√(2)").Display.Text.Should().Be("9999√(2)⌟7");
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("1⌟10000000007").Display.Text.Should().Be("1⌟10000000007");
    }

    [Theory]
    // An approximate value is recognized as a simple √ or π form; an exact decimal is shown as it was entered.
    [InlineData("sin(-45)", "-√(2)⌟2")]
    [InlineData("3sin(60)", "3√(3)⌟2")]
    [InlineData("1.4142135623731", "1.414213562")]
    public void ApproximateValues_AreRecognizedAsRoots(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("-cos⁻¹(-1)", "-π")]
    [InlineData("2cos⁻¹(-1)", "2π")]
    [InlineData("cos⁻¹(0)", "1⌟2π")]
    [InlineData("1000000cos⁻¹(-1)", "3141592.654")]
    public void ApproximateValues_AreRecognizedAsMultiplesOfPi(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Fact]
    public void LineOutput_ShowsNoRootsEvenForFractionInput()
    {
        Calculator.Display("sin(45)×1⌟1", settings: settings => settings with { InputOutput = InputOutput.LineILineO }).Should().Be("0.7071067812");
    }

    [Theory]
    // The calculator divides by primes below 1000; what remains is one factor, in parentheses from 1009² on (p. 45).
    [InlineData("2036162", "2×(1018081)")]
    [InlineData("1018081", "(1018081)")]
    [InlineData("1005973", "997×1009")]
    [InlineData("1024", "2^10")]
    [InlineData("1", "1")]
    public void PrimeFactors_FollowTheCalculator(string input, string expected)
    {
        Format(input, FormatTarget.PrimeFactor).Should().Be(expected);
    }

    [Fact]
    public void TheExtendedProfile_FactorsCompletelyAndFurther()
    {
        Format("10000000000", FormatTarget.PrimeFactor, CalculatorProfile.Extended).Should().Be("2^10×5^10");
        Format("1018081", FormatTarget.PrimeFactor, CalculatorProfile.Extended).Should().Be("1009²");
        Format("10000000000", FormatTarget.PrimeFactor).Should().BeNull("the calculator factors numbers of at most 10 digits");
    }

    [Theory]
    [InlineData("-1⌟3", FormatTarget.RecurringDecimal, "-0.(3)")]
    [InlineData("7⌟3", FormatTarget.MixedFraction, "2⌟1⌟3")]
    [InlineData("-7⌟3", FormatTarget.MixedFraction, "-2⌟1⌟3")]
    [InlineData("1⌟3", FormatTarget.MixedFraction, "1⌟3")]
    [InlineData("-0.5", FormatTarget.Sexagesimal, "-0°30′0″")]
    [InlineData("-0.0001", FormatTarget.Sexagesimal, "-0°0′0.36″")]
    [InlineData("-0.000001", FormatTarget.Sexagesimal, "0°0′0″")]
    public void FormatConversions_KeepTheirSign(string input, FormatTarget target, string expected)
    {
        Format(input, target).Should().Be(expected);
    }

    [Fact]
    public void AHugeSexagesimalValue_IsDisplayedAsADecimal()
    {
        // Found by the display-and-reparse property: the seconds of 5×10²⁸° overflowed decimal while being displayed.
        Calculator.Session().Calculate("60772²³°10′10″").Succeeded.Should().BeTrue();
        Format("10000000", FormatTarget.Sexagesimal).Should().BeNull();
        Format("9999999.5", FormatTarget.Sexagesimal).Should().Be("9999999°30′0″");
    }

    [Theory]
    [InlineData("5", FormatTarget.ImproperFraction)]
    [InlineData("10^30", FormatTarget.Sexagesimal)]
    [InlineData("10^30", FormatTarget.ImproperFraction)]
    public void FormatConversionsThatDoNotApply_ReturnNothing(string input, FormatTarget target)
    {
        Format(input, target).Should().BeNull();
    }

    [Theory]
    // With Engineer Symbol on, ENG shows f to E; beyond them the exponent (p. 64).
    [InlineData("10^-18", "1×10^-18")]
    [InlineData("10^21", "1×10^21")]
    [InlineData("10^18", "1E")]
    [InlineData("10^-15", "1f")]
    public void EngineeringWithSymbols_EndsAtFAndE(string input, string expected)
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { EngineerSymbol = true });

        session.FormatEngineering(session.Calculate(input), 0).Should().Be(expected);
    }

    [Theory]
    // A result is shown with a symbol only when one keeps the mantissa in [1, 1000); otherwise in the number format.
    [InlineData("1.5×10^-16", "1.5×10^-16")]
    [InlineData("1.5×10^22", "1.5×10^22")]
    [InlineData("1500", "1.5k")]
    public void EngineerSymbolDisplay_FallsBackToTheNumberFormat(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { EngineerSymbol = true, InputOutput = InputOutput.MathIDecimalO })
            .Should().Be(expected);
    }

    [Theory]
    [InlineData("50", "50.00")]
    [InlineData("0", "0.00")]
    public void EngineerSymbolDisplay_KeepsFixForValuesWithoutASymbol(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            EngineerSymbol = true,
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Fix(2),
        }).Should().Be(expected);
    }
}
