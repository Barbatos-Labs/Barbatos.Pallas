// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Complex application (manual pp. 125-129).
/// </summary>
public sealed class ComplexTests
{
    private static string Display(string input, Func<CalculatorSettings, CalculatorSettings>? settings = null)
    {
        return Calculator.Display(input, CalculatorApp.Complex, settings);
    }

    [Theory]
    [InlineData("(1+i)^4+(1-i)^2", "-4-2i")]
    [InlineData("(2+3i)+(1-i)", "3+2i")]
    [InlineData("(2+3i)×(2-3i)", "13")]
    [InlineData("Conjg(2+3i)", "2-3i")]
    [InlineData("ReP(2+3i)", "2")]
    [InlineData("ImP(2+3i)", "3")]
    [InlineData("Abs(1+i)", "√(2)")]
    [InlineData("Arg(1+i)", "45")]
    [InlineData("i²", "-1")]
    [InlineData("1÷i", "-i")]
    [InlineData("√(-4)", "2i")]
    public void Examples_MatchTheManual(string input, string expected)
    {
        Display(input).Should().Be(expected);
    }

    [Fact]
    public void IntegerPowers_AreProducts_NotPolarRotations()
    {
        // Complex.Pow(i, 2) was measured at −1 + 1.2×10⁻¹⁶i, which would make i² = −1 false (decision of 18 Sep 2026).
        Calculator.Evaluate("i²", CalculatorApp.Complex).ToComplex().Imaginary.Should().Be(0d);
        Calculator.Evaluate("(1+i)^4", CalculatorApp.Complex).ToComplex().Should().Be(new System.Numerics.Complex(-4d, 0d));
    }

    [Fact]
    public void PolarInput_UsesTheAngleUnitExactly()
    {
        // Manual p. 126: 2∠45 is √2 + √2i; and 2∠90 is exactly 2i, where cos(π/2) is 6.1×10⁻¹⁷.
        Display("2∠45").Should().Be("√(2)+√(2)i");
        Display("2∠90").Should().Be("2i");
    }

    [Fact]
    public void PolarDisplay_ShowsModulusAndArgument()
    {
        Func<CalculatorSettings, CalculatorSettings> polar = settings => settings with { ComplexResult = ComplexResult.Polar };

        Display("√(2)+√(2)i", polar).Should().Be("2∠45");
        Display("-1", polar).Should().Be("-1", "a real result is not written in polar form");
    }

    [Fact]
    public void FormatMenu_ConvertsBetweenCoordinates()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);
        Calculation calculation = session.Calculate("√(2)+√(2)i");

        session.Format(calculation, FormatTarget.Polar)!.Text.Should().Be("2∠45");
        session.Format(calculation, FormatTarget.Rectangular)!.Text.Should().Be("√(2)+√(2)i");
    }

    [Fact]
    public void ConversionsThatDoNotApplyToAComplexNumber_ReturnNothing()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);
        Calculation calculation = session.Calculate("2+3i");

        session.Format(calculation, FormatTarget.PrimeFactor).Should().BeNull();
        session.Format(calculation, FormatTarget.Sexagesimal).Should().BeNull();
        session.Format(calculation, FormatTarget.MixedFraction).Should().BeNull();
    }

    [Fact]
    public void ArgumentsAreInTheHalfOpenTurn()
    {
        // θ is displayed in (−180°, 180°] (p. 126).
        Display("Arg(-1)").Should().Be("180");
        Display("Arg(-i)").Should().Be("-90");
    }

    [Fact]
    public void ComplexValues_AreNotAllowedInCalculate()
    {
        // The imaginary unit is not even a name there (p. 129), and a complex result cannot reach Calculate.
        Calculator.Error("2+3i").SyntaxCode.Should().Be(SyntaxErrorCode.UnexpectedCharacter);
    }

    [Fact]
    public void PercentIsNotAvailableInComplex()
    {
        // Manual p. 57: % cannot be entered in the Complex application.
        Calculator.Error("50%", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void RealCalculationsInComplex_StayExact()
    {
        Calculator.Evaluate("0.1+0.2", CalculatorApp.Complex).IsExact.Should().BeTrue();
        Display("1⌟3+1⌟6").Should().Be("1⌟2");
    }

    [Fact]
    public void ComplexPowers_BeyondIntegers_UseThePrincipalValue()
    {
        Calculator.Evaluate("i^0.5", CalculatorApp.Complex).ToComplex().Real.Should().BeApproximately(0.7071067811865476d, 1e-14);
    }

    [Fact]
    public void LargeIntegerPowers_AreRefused()
    {
        // |n| < 10¹⁰ for (a+bi)ⁿ (p. 126).
        Calculator.Error("(1+i)^10000000000", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }
}
