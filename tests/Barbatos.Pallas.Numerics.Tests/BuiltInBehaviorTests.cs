// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;

namespace Barbatos.Pallas.Numerics.Tests;

/// <summary>
/// Pins the behaviors of the built-in numeric types that docs/PRECISION.md §4 lists and Pallas's design depends on.
/// These tests exercise .NET, not Pallas: if one fails after a runtime update, the design assumption it pins has
/// changed and PRECISION.md needs review.
/// </summary>
public sealed class BuiltInBehaviorTests
{
    [Fact]
    public void Decimal_AddsDecimalFractionsExactly()
    {
        // The reason decimal is the calculator's arithmetic type; 0.1 + 0.2 in double is 0.30000000000000004.
        (0.1m + 0.2m).Should().Be(0.3m);
        (0.1 + 0.2).Should().Be(0.30000000000000004);
    }

    [Fact]
    public void Decimal_RoundsSilentlyAtItsLastDigit()
    {
        (1m / 3m * 3m).Should().Be(0.9999999999999999999999999999m);
        (0.1234567890123456789012345678m + 10m).Should().Be(10.123456789012345678901234568m);
    }

    [Fact]
    public void Decimal_LosesSmallValuesSilently()
    {
        decimal.Parse("6.62607015E-34", NumberStyles.Float, CultureInfo.InvariantCulture).Should().Be(0m);
        decimal.Parse("1.66053906660E-27", NumberStyles.Float, CultureInfo.InvariantCulture).Should().Be(0.0000000000000000000000000017m);
        (new decimal(123456789, 0, 0, false, 28) * 0.00001m).Should().Be(0.0000000000000000000000001235m);
    }

    [Fact]
    public void Decimal_OverflowIsLoud()
    {
        decimal largest = decimal.MaxValue;
        Action doubled = () => _ = largest * 2m;
        Action factorial = () =>
        {
            decimal product = 1m;
            for (int factor = 2; factor <= 28; factor++)
            {
                product *= factor;
            }
        };

        doubled.Should().Throw<OverflowException>();
        factorial.Should().Throw<OverflowException>("28! is about 3.0×10²⁹");
    }

    [Fact]
    public void DecimalFromDouble_RoundsToFifteenSignificantDigits()
    {
        ((decimal)Math.Sin(Math.PI / 6)).Should().Be(0.5m, "0.49999999999999994 loses its binary noise");
        ((decimal)Math.Tan(Math.PI / 4)).Should().Be(1m);
        ((decimal)(0.1 + 0.2)).Should().Be(0.3m);
        ((decimal)Math.Sqrt(2)).Should().Be(1.41421356237310m);
        ((decimal)123456789.123456789).Should().Be(123456789.123457m);
        ((decimal)1.005).Should().Be(1.005m);
        ((decimal)(Math.Sqrt(2) * Math.Sqrt(2))).Should().Be(2m);
        (Math.Sqrt(2) * Math.Sqrt(2)).Should().Be(2.0000000000000004);
    }

    [Fact]
    public void DecimalFromDouble_MayDifferByOneInTheFifteenthDigit()
    {
        // Math.E is 2.71828182845904509…, whose 15 correctly rounded digits are 2.71828182845905.
        ((decimal)Math.E).Should().Be(2.71828182845904m);
    }

    [Fact]
    public void DecimalFromDouble_ThrowsForValuesDecimalCannotHold_AndLosesTinyValues()
    {
        double huge = 1e29;
        double nan = double.NaN;
        Action overflow = () => _ = (decimal)huge;
        Action notANumber = () => _ = (decimal)nan;

        overflow.Should().Throw<OverflowException>();
        notANumber.Should().Throw<OverflowException>();
        ((decimal)6.62607015e-34).Should().Be(0m, "the Planck constant is below decimal's 28 decimal places");
    }

    [Fact]
    public void Constants_AreInTheBcl_AndMathFIsSinglePrecision()
    {
        double.Pi.Should().Be(Math.PI);
        double.E.Should().Be(Math.E);
        double.Tau.Should().Be(Math.Tau);

        // MathF.PI is a float: 3.1415927, 8.7×10⁻⁸ from π. Math.PI and double.Pi are the double-precision constant.
        ((double)MathF.PI - Math.PI).Should().BeApproximately(8.74e-8, 1e-10);
    }

    [Fact]
    public void DoubleSinPiAndCosPi_AreExactAtMultiplesOfARightAngle_WhereSystemMathIsNot()
    {
        double.CosPi(0.5).Should().Be(0);
        double.SinPi(1).Should().Be(0);
        double.IsPositiveInfinity(double.TanPi(0.5)).Should().BeTrue();
        double.SinPi(1.0 / 6).Should().Be(0.49999999999999994, "only multiples of ½ are exact");
    }

    [Fact]
    public void SystemMath_IsNotExactAtSpecialAngles()
    {
        Math.Cos(Math.PI / 2).Should().BeInRange(6.12e-17, 6.13e-17);
        Math.Tan(Math.PI / 2).Should().BeGreaterThan(1.6e16);
    }

    [Fact]
    public void SystemMath_EdgeValuesThatScientificMathHandlesItself()
    {
        Math.Pow(-8, 1.0 / 3).Should().Be(double.NaN);
        Math.Pow(0, 0).Should().Be(1, "IEEE 754's convention, which ScientificMath.Pow does not follow");
        Math.Log(1, 0).Should().Be(0, "-0 rather than NaN, which is why ScientificMath.Log checks base 0 itself");
    }

    [Fact]
    public void Rounding_DoubleIsNotADecimalTie_AndTheDefaultsDisagree()
    {
        Math.Round(1.005, 2, MidpointRounding.AwayFromZero).Should().Be(1);
        Math.Round(1.005m, 2, MidpointRounding.AwayFromZero).Should().Be(1.01m);

        Math.Round(2.5m).Should().Be(2m);
        2.5m.ToString("F0", CultureInfo.InvariantCulture).Should().Be("3");
        2.345m.ToString("F2", CultureInfo.InvariantCulture).Should().Be("2.35");
    }

    [Fact]
    public void BigInteger_HoldsFactorialsDecimalCannot()
    {
        BigInteger product = BigInteger.One;
        for (int factor = 2; factor <= 28; factor++)
        {
            product *= factor;
        }

        product.Should().Be(BigInteger.Parse("304888344611713860501504000000", CultureInfo.InvariantCulture));
    }
}
