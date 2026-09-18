// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Numerics;
using PeterO.Numbers;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The numerical integrator, checked against PeterO.Numbers references that share none of its code
/// (docs/PRECISION.md §12).
/// </summary>
public sealed class AccuracyTests
{
    private static readonly EContext Working = EContext.ForPrecision(50).WithRounding(ERounding.HalfEven);

    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian, InputOutput = InputOutput.MathIDecimalO };

    public static TheoryData<string, string> Integrals => new()
    {
        // ∫ x² dx from 0 to 3 = 9; ∫ 1/x dx from 1 to 2 = ln 2; ∫ eˣ dx from 0 to 1 = e − 1;
        // ∫ sin x dx from 0 to π/2 = 1; ∫ √x dx from 0 to 1 = 2/3; ∫ 1/(1+x²) dx from 0 to 1 = π/4.
        { "∫(x²,0,3)", "9" },
        { "∫(1⌟x,1,2)", "ln2" },
        { "∫(e^(x),0,1)", "e-1" },
        { "∫(sin(x),0,π÷2)", "1" },
        { "∫(√(x),0,1)", "2/3" },
        { "∫(1⌟(1+x²),0,1)", "pi/4" },
        // [x⁵/5 − x² + x] from −1 to 2 = (6.4 − 4 + 2) − (−0.2 − 1 − 1) = 6.6.
        { "∫(x^4-2x+1,-1,2)", "6.6" },
        { "∫(ln(x),1,e)", "1" },
    };

    [Theory]
    [MemberData(nameof(Integrals))]
    public void Integrals_AgreeWithA50DigitReference(string input, string reference)
    {
        double result = Calculator.Evaluate(input, settings: Radians).ToDouble();
        EDecimal expected = Expected(reference);

        EDecimal difference = EDecimal.FromDouble(result).Subtract(expected, Working).Abs();
        EDecimal scale = EDecimal.Max(expected.Abs(), EDecimal.One, Working);
        difference.Divide(scale, Working).CompareTo(EDecimal.FromString("1e-12")).Should()
            .BeLessThan(0, "{0} = {1}, but the engine computed {2}", input, expected, result);
    }

    [Fact]
    public void TheErrorEstimate_CoversTheRealError()
    {
        // Invariant I7: the estimate the engine reports must not be optimistic.
        Calculation calculation = Calculator.Session(settings: Radians).Calculate("∫(sin(x),0,π)");

        double error = Math.Abs(calculation.Integrals[0].Result - 2d);
        error.Should().BeLessThanOrEqualTo(Math.Max(calculation.Integrals[0].ErrorEstimate, 1e-15));
    }

    [Fact]
    public void ADifficultIntegrand_IsStillWithinItsEstimate()
    {
        // √x has an infinite derivative at 0: the estimate grows, and the result stays inside it.
        Calculation calculation = Calculator.Session(settings: Radians).Calculate("∫(√(x),0,1)");

        double error = Math.Abs(calculation.Integrals[0].Result - (2d / 3d));
        error.Should().BeLessThanOrEqualTo(Math.Max(calculation.Integrals[0].ErrorEstimate, 1e-15));
    }

    [Theory]
    // The engine's own functions come from System.Math; these check that the engine passes values through unharmed.
    [InlineData("ln(90)", "ln90")]
    [InlineData("√(2)", "sqrt2")]
    [InlineData("sinh(1)", "sinh1")]
    [InlineData("e^(1)", "e")]
    public void Functions_KeepAbout15SignificantDigits(string input, string reference)
    {
        double result = Calculator.Evaluate(input, settings: Radians).ToDouble();
        EDecimal expected = Expected(reference);

        EDecimal difference = EDecimal.FromDouble(result).Subtract(expected, Working).Abs();
        difference.Divide(expected.Abs(), Working).CompareTo(EDecimal.FromString("1e-14")).Should()
            .BeLessThan(0, "{0} = {1}, but the engine computed {2}", input, expected, result);
    }

    private static EDecimal Expected(string reference)
    {
        EDecimal pi = EDecimal.PI(Working);
        EDecimal e = EDecimal.One.Exp(Working);
        return reference switch
        {
            "ln2" => EDecimal.FromInt32(2).Log(Working),
            "ln90" => EDecimal.FromInt32(90).Log(Working),
            "e-1" => e.Subtract(EDecimal.One, Working),
            "e" => e,
            "2/3" => EDecimal.FromInt32(2).Divide(EDecimal.FromInt32(3), Working),
            "pi/4" => pi.Divide(EDecimal.FromInt32(4), Working),
            "sqrt2" => EDecimal.FromInt32(2).Sqrt(Working),
            "sinh1" => e.Subtract(EDecimal.One.Divide(e, Working), Working).Divide(EDecimal.FromInt32(2), Working),
            _ => EDecimal.FromString(reference),
        };
    }
}
