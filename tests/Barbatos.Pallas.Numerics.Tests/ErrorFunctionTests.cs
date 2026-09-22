// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Pallas.Numerics.Tests.Support;
using CsCheck;
using PeterO.Numbers;

namespace Barbatos.Pallas.Numerics.Tests;

/// <summary>
/// erf, erfc and the inverse of erfc against 50-digit PeterO.Numbers references.
/// </summary>
public sealed class ErrorFunctionTests
{
    // Measured on 18 Sep 2026: at most 1.2×10⁻¹⁵ for erf and 5.4×10⁻¹⁶ for erfc (ErrorFunction's remarks).
    private const double Tolerance = 2e-15;

    [Fact]
    public void ReferenceAgreesWithPublishedDigitsAndWithItself()
    {
        Digits(ErrorFunctionReference.Erf(1d)).Should().Be("0.842700792949714869341220635083");
        Digits(ErrorFunctionReference.Erfc(3d)).Should().Be("0.0000220904969985854413727761295823");
        foreach (double x in (double[])[3d, 4d, 5d])
        {
            EDecimal series = EDecimal.One.Subtract(ErrorFunctionReference.Erf(x), EContext.ForPrecision(50));
            series.Subtract(ErrorFunctionReference.ContinuedFraction(x)).Divide(series, EContext.ForPrecision(40)).Abs()
                .CompareToValue(EDecimal.FromString("1E-25")).Should().BeNegative();
        }
    }

    [Fact]
    public void Erf_IsAccurate()
    {
        Gen.Double[-6d, 6d].Sample(x => ErrorFunctionReference.CloseRelative(ErrorFunction.Erf(x), ErrorFunctionReference.Erf(x), Tolerance), iter: 300);
    }

    [Fact]
    public void Erfc_IsAccurateWhereverItIsANormalDouble()
    {
        // erfc(26.5) = 4.1×10⁻³⁰⁷; beyond about 26.55 the result is subnormal and keeps fewer digits.
        Gen.Double[-6d, 26.5d].Sample(x => ErrorFunctionReference.CloseRelative(ErrorFunction.Erfc(x), ErrorFunctionReference.Erfc(x), Tolerance), iter: 300);
    }

    [Theory]
    // Both sides of every change of method, and the far tail.
    [InlineData(0.999999999)]
    [InlineData(1d)]
    [InlineData(1.000000001)]
    [InlineData(-1d)]
    [InlineData(-1.000000001)]
    [InlineData(1e-300)]
    [InlineData(0.5)]
    [InlineData(7d)]
    [InlineData(26d)]
    public void TheMethodsMeet(double x)
    {
        ErrorFunctionReference.RelativeError(ErrorFunction.Erfc(x), ErrorFunctionReference.Erfc(x)).Should().BeLessThan(Tolerance);
        if (Math.Abs(x) <= 6d)
        {
            ErrorFunctionReference.RelativeError(ErrorFunction.Erf(x), ErrorFunctionReference.Erf(x)).Should().BeLessThan(Tolerance);
        }
    }

    [Fact]
    public void SpecialValues()
    {
        ErrorFunction.Erf(0d).Should().Be(0d);
        double.IsNegative(ErrorFunction.Erf(-0d)).Should().BeTrue();
        ErrorFunction.Erf(double.PositiveInfinity).Should().Be(1d);
        ErrorFunction.Erf(double.NegativeInfinity).Should().Be(-1d);
        ErrorFunction.Erf(30d).Should().Be(1d);
        double.IsNaN(ErrorFunction.Erf(double.NaN)).Should().BeTrue();

        ErrorFunction.Erfc(0d).Should().Be(1d);
        ErrorFunction.Erfc(double.PositiveInfinity).Should().Be(0d);
        ErrorFunction.Erfc(double.NegativeInfinity).Should().Be(2d);
        ErrorFunction.Erfc(-30d).Should().Be(2d);
        ErrorFunction.Erfc(27.3).Should().Be(0d);
        ErrorFunction.Erfc(27d).Should().BePositive("erfc(27) = 5.2×10⁻³¹⁹ is a subnormal double");
        double.IsNaN(ErrorFunction.Erfc(double.NaN)).Should().BeTrue();
    }

    [Fact]
    public void ErfIsOddAndErfcItsComplement()
    {
        Gen.Double[-30d, 30d].Sample(x =>
            ErrorFunction.Erf(-x) == -ErrorFunction.Erf(x)
            && Math.Abs(ErrorFunction.Erf(x) + ErrorFunction.Erfc(x) - 1d) <= 2.3e-16
            && Math.Abs(ErrorFunction.Erfc(x) + ErrorFunction.Erfc(-x) - 2d) <= 4.5e-16);
    }

    [Fact]
    public void InverseErfc_IsTheInverse()
    {
        // If x is off by ε relative, erfc(x) is off by about 2x²·ε, so the round trip is judged on that scale. Above 1 the
        // inverse is −erfc⁻¹(2 − y) by construction (InverseErfc_SpecialValues).
        Gen<double> values = Gen.OneOf(Gen.Double[0d, 1d], Gen.Double[-300d, 0d].Select(exponent => Math.Pow(10d, exponent)));
        values.Sample(y =>
        {
            double x = ErrorFunction.InverseErfc(y);
            return y == 0d
                ? double.IsPositiveInfinity(x)
                : ErrorFunctionReference.RelativeError(y, ErrorFunctionReference.Erfc(x)) <= 5e-15 * Math.Max(1d, 2d * x * x);
        }, iter: 300);
    }

    [Fact]
    public void InverseErfc_SpecialValues()
    {
        // erfc⁻¹(0.5) = 0.47693627620446987338…
        ErrorFunctionReference.RelativeError(ErrorFunction.InverseErfc(0.5), EDecimal.FromString("0.4769362762044698733814183536431305598090"))
            .Should().BeLessThan(3e-16);
        ErrorFunction.InverseErfc(1.5).Should().Be(-ErrorFunction.InverseErfc(0.5));
        ErrorFunction.InverseErfc(1d).Should().Be(0d);
        double.IsNegative(ErrorFunction.InverseErfc(1d)).Should().BeFalse();
        ErrorFunction.InverseErfc(0d).Should().Be(double.PositiveInfinity);
        ErrorFunction.InverseErfc(2d).Should().Be(double.NegativeInfinity);
        ErrorFunction.InverseErfc(double.Epsilon).Should().BeApproximately(27.2132932, 1e-6);
        double.IsNaN(ErrorFunction.InverseErfc(-0.1)).Should().BeTrue();
        double.IsNaN(ErrorFunction.InverseErfc(2.1)).Should().BeTrue();
        double.IsNaN(ErrorFunction.InverseErfc(double.NaN)).Should().BeTrue();
    }

    [Fact]
    public void TheNormalDistributionOfTheManual()
    {
        // p. 91: P(t) for t = −79/√2579 is 0.05990013396322900464…; P(t) = erfc(−t/√2)/2.
        double t = -79d / Math.Sqrt(2579d);
        double p = ErrorFunction.Erfc(-t / Math.Sqrt(2d)) / 2d;

        ErrorFunctionReference.RelativeError(p, EDecimal.FromString("0.05990013396322900464")).Should().BeLessThan(5e-15);
    }

    private static string Digits(EDecimal value)
    {
        return value.RoundToPrecision(EContext.ForPrecision(30)).ToPlainString().ToString(CultureInfo.InvariantCulture);
    }
}
