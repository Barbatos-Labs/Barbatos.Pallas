// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using NumericsComplex = System.Numerics.Complex;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The edges of the arithmetic: the ends of each range, the paths between decimal and double, and the domains that only
/// a stranger input reaches.
/// </summary>
public sealed class ValueMathEdgeTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian, InputOutput = InputOutput.MathIDecimalO };

    private static CalculatorSession BaseN(NumberBase mode) =>
        Calculator.Session(CalculatorApp.BaseN, settings: settings => settings with { BaseMode = mode });

    private static void ShouldBeNear(NumericsComplex actual, double real, double imaginary)
    {
        actual.Real.Should().BeApproximately(real, 1e-12);
        actual.Imaginary.Should().BeApproximately(imaginary, 1e-12);
    }

    [Theory]
    // Base-N subtracts in 32 bits: FFFFFFFF − 80000000 is −1 − (−2³¹) = 7FFFFFFF, although −(−2³¹) itself is out of range.
    [InlineData(NumberBase.Hex, "FFFFFFFF-80000000", int.MaxValue)]
    [InlineData(NumberBase.Dec, "-2147483647-1", int.MinValue)]
    [InlineData(NumberBase.Hex, "7FFFFFFE+1", int.MaxValue)]
    [InlineData(NumberBase.Hex, "80000001-1", int.MinValue)]
    public void BaseNArithmetic_ReachesBothEndsOfTheRange(NumberBase mode, string input, int expected)
    {
        BaseN(mode).Calculate(input).Result.ToInt32().Should().Be(expected);
    }

    [Theory]
    [InlineData(NumberBase.Dec, "0-(-2147483648)")]
    [InlineData(NumberBase.Hex, "80000000-1")]
    [InlineData(NumberBase.Hex, "7FFFFFFF+1")]
    public void BaseNArithmeticBeyondThirtyTwoBits_IsAMathError(NumberBase mode, string input)
    {
        BaseN(mode).Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void VerifyIsNotAvailableInBaseN()
    {
        // Verify exists in Calculate, Table, Equation and Complex (p. 73).
        CalculatorSession session = BaseN(NumberBase.Dec);
        session.Settings = session.Settings with { Verify = true };

        session.Calculate("1=1").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AnExactFormPlusAnApproximateValue_IsTheirSum()
    {
        // √2 keeps its form; ln 2 has none, so the sum is computed from both doubles.
        Calculator.Display("√(2)+ln(2)").Should().Be("2.107360743");
        Calculator.Display("√(2)+√(3)+√(5)+√(7)+√(11)").Should().Be("11.34470845", "five terms drop the form, not the value");
    }

    [Fact]
    public void DecimalOverflow_ContinuesInDouble()
    {
        const string Max = "79228162514264337593543950335";

        Calculator.Display(Max + "+" + Max).Should().Be("1.58456325×10^29");
        Calculator.Display(Max + "÷0.5").Should().Be("1.58456325×10^29");
        Calculator.Evaluate(Max + "+" + Max).Kind.Should().Be(ValueKind.DoubleReal);
    }

    [Fact]
    public void ADecimalProductBelowTenToTheMinusFourteen_IsRecomputedInDouble()
    {
        Calculator.Display("0.0000001×0.00000001").Should().Be("1×10^-15");
        Calculator.Evaluate("0.0000001×0.00000001").Kind.Should().Be(ValueKind.DoubleReal);
    }

    [Fact]
    public void ASumOfExactlyTenToTheMinusFourteen_StaysExact()
    {
        // 10⁻¹⁴ is the smallest magnitude at which decimal still holds 15 significant digits (PRECISION.md §3).
        Value value = Calculator.Evaluate("0.00000000000002-0.00000000000001");

        value.Kind.Should().Be(ValueKind.DecimalReal);
        value.IsExact.Should().BeTrue();
    }

    [Theory]
    [InlineData("2^0.5", "√(2)")]
    [InlineData("0^0.3", "0")]
    [InlineData("10^2.5", "316.227766")]
    [InlineData("2^100.5", "1.792728671×10^30")]
    [InlineData("2ˣ√(8)", "2√(2)")]
    [InlineData("4ˣ√(0)", "0")]
    [InlineData("3000000000ˣ√(2)", "1")]
    [InlineData("0P0", "1")]
    [InlineData("0C0", "1")]
    [InlineData("2000C1", "2000")]
    [InlineData("2000C1999", "2000")]
    public void PowersRootsAndSelections_AtTheirEdges(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("0^-0.3")]
    [InlineData("10^99.999999995")]
    [InlineData("10^128")]
    [InlineData("(-8)^(10^-20)")]
    [InlineData("(-8)^0.3333333333333")]
    [InlineData("1000000P500000")]
    [InlineData("1000000C500000")]
    public void PowersAndSelectionsOutsideTheirDomain_AreMathErrors(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheDomainOfTenToTheX_IsTheCalculatorsOnly()
    {
        CalculatorSession extended = Calculator.Session(profile: CalculatorProfile.Extended);

        extended.Calculate("10^99.999999995").Error.Should().BeNull();
        extended.Calculate("170P170").Error.Should().BeNull();
        extended.Calculate("171P171").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnApproximateExponentNearAnOddFraction_TakesTheRealRoot()
    {
        // log(10^(1/3)) is 0.333333333333333 to 15 digits: within 10⁻¹³ of 1/3, so (−8) to it is the real cube root.
        Calculator.Display("(-8)^(log(10^(1⌟3)))").Should().Be("-2");
    }

    [Fact]
    public void SquareRootsWrittenAsPowersOrRoots_KeepTheirForm()
    {
        // The display would recognize √2 from its digits anyway; only the form makes the product exactly 2.
        Calculator.Evaluate("2^0.5×2^0.5").IsExact.Should().BeTrue();
        Calculator.Evaluate("2ˣ√(2)×2ˣ√(2)").IsExact.Should().BeTrue();
    }

    [Fact]
    public void ALargeExactExponent_IsRecognizedAsAFraction()
    {
        // 100000/3 as a decimal is off by 3×10⁻²⁴: the tolerance grows with the exponent.
        Calculator.Display("(-1)^(100000⌟3)").Should().Be("1");
    }

    [Fact]
    public void AComplexIndex_IsAMathError()
    {
        Calculator.Error("(1+i)ˣ√(4)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ARootWhoseReciprocalOverflows_IsAMathError()
    {
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("(1×10^-320)ˣ√(2)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnIntegerPowerOfAnApproximateValue_IsItsProduct()
    {
        // An integer power is a product (PRECISION.md §3 sends only other powers to double), so x^n, x² and x×x agree
        // to the last digit: through Math.Pow, sin(1)^2−sin(1)×sin(1) was 4.5×10⁻¹⁹ (25 Sep 2026). The digits the
        // decimal keeps beyond the fifteen of sin 1 do not make it exact.
        Value cube = Calculator.Evaluate("sin(1)^3", settings: Radians);

        cube.IsExact.Should().BeFalse();
        cube.Should().Be(Calculator.Evaluate("sin(1)×sin(1)×sin(1)", settings: Radians));
        Calculator.Evaluate("sin(1)^2−sin(1)×sin(1)", settings: Radians).ToDecimal().Should().Be(0m);
        Calculator.Evaluate("sin(1)^2−sin(1)²", settings: Radians).ToDecimal().Should().Be(0m);
        Calculator.Evaluate("ln(7)^3−ln(7)×ln(7)×ln(7)").ToDecimal().Should().Be(0m);
        Calculator.Evaluate("ln(7)^-2−1÷(ln(7)×ln(7))").ToDecimal().Should().Be(0m, "a negative power is the reciprocal of the product");
    }

    [Fact]
    public void SelectionsOfApproximateIntegers_AreApproximate()
    {
        Calculator.Evaluate("(20sin(30))P2").IsExact.Should().BeFalse();
        Calculator.Evaluate("(20sin(30))C2").IsExact.Should().BeFalse();
        Calculator.Evaluate("10P2").IsExact.Should().BeTrue();
        Calculator.Evaluate("10C2").IsExact.Should().BeTrue();
    }

    [Fact]
    public void AnAngleConvertedToRadians_KeepsPiExact()
    {
        Value value = Calculator.Evaluate("45°÷π", settings: Radians);

        value.IsExact.Should().BeTrue("(π/4)/π is exactly 1/4");
        value.ToDecimal().Should().Be(0.25m);
    }

    [Fact]
    public void TheEndsOfTheCalculatorsRange()
    {
        // 9.9999999995×10⁹⁹ would display as 1×10^100; anything nonzero below 10⁻⁹⁹ is 0 (assumption U18).
        Calculator.Error("9.9999999995×10^99×1").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Display("9.999999999×10^99×1").Should().Be("9.999999999×10^99");
        Calculator.Display("10^-60×10^-60").Should().Be("0");
        Calculator.Display("1×10^-99×1").Should().Be("1×10^-99");
        Calculator.Display("9.99999999995×10^-99÷10").Should().Be("1×10^-99", "it displays as the smallest value in range");
        Calculator.Display("9.999999999×10^-99÷10").Should().Be("0");
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("10^-60×10^-60").Display.Text.Should().Be("1×10^-120");
    }

    [Fact]
    public void PowersOfTenReachTheSmallestValueInRange()
    {
        // Squaring out 10⁹⁹ in double drifted a unit below it, and 10⁻⁹⁹ underflowed to 0; 10⁻¹²⁸ went through 10¹²⁸.
        Calculator.Display("10^-99").Should().Be("1×10^-99");
        Calculator.Display("2×10^-99").Should().Be("2×10^-99");
        Calculator.Display("10^-128").Should().Be("0");
        Calculator.Evaluate("10^-50").ToDouble().Should().Be(1e-50);
        Calculator.Evaluate("10^40").ToDouble().Should().Be(1e40);
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("10^-128").Display.Text.Should().Be("1×10^-128");
        Calculator.Evaluate("1.1^3").ToDecimal().Should().Be(1.331m);
        Calculator.Evaluate("1.1^3").IsExact.Should().BeTrue();
    }

    [Fact]
    public void AComplexResultWithOnePartOutOfRange_IsAMathError()
    {
        Calculator.Error("(10^60+i)×10^60", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ComplexPowersAndRoots()
    {
        ShouldBeNear(Calculator.Evaluate("(-16)^0.25", CalculatorApp.Complex).ToComplex(), Math.Sqrt(2d), Math.Sqrt(2d));
        ShouldBeNear(Calculator.Evaluate("4ˣ√(-16)", CalculatorApp.Complex).ToComplex(), Math.Sqrt(2d), Math.Sqrt(2d));
        ShouldBeNear(Calculator.Evaluate("3ˣ√(i)", CalculatorApp.Complex).ToComplex(), Math.Sqrt(3d) / 2d, 0.5d);
        ShouldBeNear(Calculator.Evaluate("√(i)", CalculatorApp.Complex).ToComplex(), Math.Sqrt(0.5d), Math.Sqrt(0.5d));
        Calculator.Evaluate("√(-4)", CalculatorApp.Complex).ToComplex().Should().Be(new NumericsComplex(0d, 2d));
    }

    [Fact]
    public void IntegerPowersOfComplexNumbers_AreProducts()
    {
        Calculator.Evaluate("i^-1", CalculatorApp.Complex).ToComplex().Should().Be(new NumericsComplex(0d, -1d));
        Calculator.Evaluate("(1+i)^3", CalculatorApp.Complex).ToComplex().Should().Be(new NumericsComplex(-2d, 2d));
        Calculator.Evaluate("i^9999999999", CalculatorApp.Complex).ToComplex().Should().Be(new NumericsComplex(0d, -1d));
        Calculator.Error("i^10000000000", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("0^i", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // An approximate value compares within 10⁻¹³ relative; an exact or exact-form value compares exactly.
    [InlineData("1000sin(90)=1000.00000000005", true)]
    [InlineData("1000sin(90)=1000.0000000002", false)]
    [InlineData("1000sin(90)<1000.0000000002", true)]
    [InlineData("0<10^-20", true)]
    [InlineData("10^-20>0", true)]
    [InlineData("√(2)×√(2)=2", true)]
    [InlineData("√(2)=1.4142135623731", true)]
    [InlineData("√(2)<√(3)", true)]
    [InlineData("1=1.00000000000001", false)]
    public void Verify_ComparesByExactnessAndTolerance(string input, bool expected)
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { Verify = true });

        session.Calculate(input).IsTrue.Should().Be(expected, "'{0}'", input);
    }
}
