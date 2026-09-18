// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Arithmetic: exact in <see cref="decimal"/>, continuing in <see cref="double"/> exactly where <see cref="decimal"/>
/// would lose digits, and refusing what the calculator refuses.
/// </summary>
public sealed class ArithmeticTests
{
    [Fact]
    public void DecimalInput_StaysDecimal()
    {
        // The first promise of docs/PRECISION.md: 0.1 + 0.2 is 0.3, not 0.30000000000000004.
        Value value = Calculator.Evaluate("0.1+0.2");

        value.IsExact.Should().BeTrue();
        value.ToDecimal().Should().Be(0.3m);
    }

    [Theory]
    [InlineData("7×8-4×5", "36")]
    // MathI/MathO displays a result as a fraction whenever one fits the display (p. 47 converts 3.25 in LineI/LineO
    // precisely because MathO would have shown 13⌟4 straight away).
    [InlineData("1.1×1.1", "121⌟100")]
    [InlineData("2÷3×3", "2")]
    [InlineData("-2²", "-4")]
    [InlineData("(-2)²", "4")]
    [InlineData("6÷2(1+2)", "1")]
    [InlineData("(5²)³", "15625")]
    [InlineData("(1+1)^(2+2)", "16")]
    [InlineData("5ˣ√(32)", "2")]
    [InlineData("2^-3", "1⌟8")]
    [InlineData("(5+3)!", "40320")]
    [InlineData("10P4", "5040")]
    [InlineData("10C4", "210")]
    [InlineData("150×20%", "30")]
    [InlineData("660÷880%", "75")]
    [InlineData("GCD(28,35)", "7")]
    [InlineData("LCM(9,15)", "45")]
    [InlineData("Abs(2-7)", "5")]
    [InlineData("Int(-3.5)", "-3")]
    [InlineData("Intg(-3.5)", "-4")]
    public void Examples_MatchTheManual(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void DecimalOverflow_ContinuesInDouble()
    {
        // 7.9×10²⁸ is decimal's limit; the calculation continues rather than failing.
        Value value = Calculator.Evaluate("99999999999999999999999999×9999");

        value.Kind.Should().Be(ValueKind.DoubleReal);
        value.ToDouble().Should().BeApproximately(9.999e29, 1e27);
    }

    [Fact]
    public void SmallProducts_AreComputedInDoubleFromTheOperands()
    {
        // In decimal, 1.23456789×10⁻²⁰ × 10⁻⁵ keeps four significant digits; the engine computes it in double instead.
        Value value = Calculator.Evaluate("1.23456789×10^-20×10^-5");

        value.Kind.Should().Be(ValueKind.DoubleReal);
        value.ToDouble().Should().BeApproximately(1.23456789e-25, 1e-40);
    }

    [Fact]
    public void IntegerPowers_OfExactValues_AreExact()
    {
        Value value = Calculator.Evaluate("1.1^3");

        value.IsExact.Should().BeTrue();
        value.ToDecimal().Should().Be(1.331m);
    }

    [Theory]
    [InlineData("14÷0×2")]
    [InlineData("0^0")]
    [InlineData("0^-1")]
    [InlineData("(-8)^0.5")]
    [InlineData("√(-1)")]
    [InlineData("log(0,2)")]
    [InlineData("log(1,2)")]
    [InlineData("ln(0)")]
    [InlineData("sin⁻¹(2)")]
    [InlineData("tan(90)")]
    [InlineData("(-2)!")]
    [InlineData("2.5!")]
    [InlineData("GCD(1.5,2)")]
    [InlineData("1⌟0")]
    public void OutsideTheDomain_IsAMathError(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void NegativeBase_WithAnOddRootExponent_IsTheRealRoot()
    {
        // Math.Pow(-8, 1.0/3) is NaN; the calculator gives −2 (docs/PRECISION.md §6).
        Calculator.Display("(-8)^(1⌟3)").Should().Be("-2");
        Calculator.Display("3ˣ√(-8)").Should().Be("-2");
    }

    [Fact]
    public void StandardProfile_KeepsTheCalculatorsRange()
    {
        // The range ends at 9.999999999×10⁹⁹ (p. 169): a value that would display as 1×10^100 is already out of it.
        Calculator.Error("9.999999999×10^99×10").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("9.9999999999×10^99").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Evaluate("9.999999999×10^99").Kind.Should().Be(ValueKind.DoubleReal);
    }

    [Fact]
    public void ExtendedProfile_ReachesFurtherThanTheCalculator()
    {
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended);

        session.Calculate("9.9999999999×10^99×10").Error.Should().BeNull();
        session.Calculate("100!").Error.Should().BeNull("the Extended profile computes factorials beyond 69");
        session.Calculate("10^400").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "double has no value that large");
    }

    [Fact]
    public void StandardProfile_StopsFactorialsAt69()
    {
        Calculator.Session().Calculate("69!").Error.Should().BeNull();
        Calculator.Error("70!").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void LargeIntegerFunctions_AreExactWhileDecimalHoldsThem()
    {
        // 27! is the largest factorial decimal can hold exactly; beyond it the value is a double.
        Value exact = Calculator.Evaluate("27!");
        Value approximate = Calculator.Evaluate("28!");

        exact.IsExact.Should().BeTrue();
        exact.ToDecimal().Should().Be(10888869450418352160768000000m);
        approximate.Kind.Should().Be(ValueKind.DoubleReal);
    }

    [Fact]
    public void SelectionsTooLargeToDisplay_AreRefusedWithoutComputing()
    {
        // nPr with r ≥ 70 is at least 70!, beyond the calculator's range: the answer must come back quickly.
        Calculator.Error("1000000000P1000000").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("1000000000C1000000").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void Rnd_RoundsToTheDisplayFormat()
    {
        // Manual p. 61: with Fix 3, 10÷3×3 displays 10.000 while Rnd(10÷3)×3 is 9.999.
        Func<CalculatorSettings, CalculatorSettings> fix3 = settings => settings with { InputOutput = InputOutput.MathIDecimalO, NumberFormat = NumberFormat.Fix(3) };

        Calculator.Display("10÷3×3", settings: fix3).Should().Be("10.000");
        Calculator.Display("Rnd(10÷3)×3", settings: fix3).Should().Be("9.999");
    }

    [Fact]
    public void Percent_IsAHundredth()
    {
        Calculator.Display("3500-3500×25%").Should().Be("2625");
    }

    [Fact]
    public void AngleUnits_ConvertExactly()
    {
        // (π÷2)ʳ is 90° (p. 61), and 90° is π/2 in radians: both exact through the π form.
        Calculator.Display("(π÷2)ʳ").Should().Be("90");
        Calculator.Display("90°", settings: settings => settings with { AngleUnit = Numerics.AngleUnit.Radian }).Should().Be("1⌟2π");
        Calculator.Display("100ᵍ", settings: settings => settings with { AngleUnit = Numerics.AngleUnit.Degree }).Should().Be("90");
    }

    [Fact]
    public void Sexagesimal_AddsExactly()
    {
        // 2°20′30″ + 0°9′30″ = 2°30′0″ = 2.5 (pp. 49-50).
        Calculation calculation = Calculator.Session().Calculate("2°20′30″+0°9′30″");

        calculation.Display.Text.Should().Be("2°30′0″");
        calculation.Result.ToDecimal().Should().Be(2.5m);
    }

    [Fact]
    public void EngineeringSymbols_ScaleExactly()
    {
        Calculator.Evaluate("999_k+25_k").ToDecimal().Should().Be(1024000m);
        Calculator.Evaluate("5_m").ToDecimal().Should().Be(0.005m);
        Calculator.Evaluate("5_f").Kind.Should().Be(ValueKind.DoubleReal, "5×10⁻¹⁵ is below decimal's precise range");
    }
}
