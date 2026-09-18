// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Numerics.Tests.Support;

namespace Barbatos.Pallas.Numerics.Tests;

public sealed class TrigonometryTests
{
    // The same multiple of a right angle in each unit: (right angles, degrees, radians, gradians).
    public static TheoryData<int, double, double, double> RightAngles => new()
    {
        { 0, 0, 0, 0 },
        { 1, 90, Math.PI / 2, 100 },
        { 2, 180, Math.PI, 200 },
        { 3, 270, 3 * Math.PI / 2, 300 },
        { -1, -90, -Math.PI / 2, -100 },
        { 5, 450, 5 * Math.PI / 2, 500 },
        { 80, 7200, 40 * Math.PI, 8000 },
    };

    [Theory]
    [MemberData(nameof(RightAngles))]
    public void MultiplesOfARightAngle_AreExactInEveryUnit(int rightAngles, double degrees, double radians, double gradians)
    {
        int quadrant = ((rightAngles % 4) + 4) % 4;
        double sin = quadrant switch { 1 => 1, 3 => -1, _ => 0 };
        double cos = quadrant switch { 0 => 1, 2 => -1, _ => 0 };

        foreach ((double angle, AngleUnit unit) in new[] { (degrees, AngleUnit.Degree), (radians, AngleUnit.Radian), (gradians, AngleUnit.Gradian) })
        {
            // Math.Cos(Math.PI / 2) is 6.1E-17; double.CosPi(0.5) is 0.
            Trigonometry.Sin(angle, unit).Should().Be(sin, "sin {0} {1}", angle, unit);
            Trigonometry.Cos(angle, unit).Should().Be(cos, "cos {0} {1}", angle, unit);

            double tan = Trigonometry.Tan(angle, unit);
            if (quadrant % 2 == 0)
            {
                tan.Should().Be(0, "tan {0} {1}", angle, unit);
            }
            else
            {
                // The calculator's Math ERROR; Math.Tan(Math.PI / 2) would be 1.6E16 instead.
                double.IsInfinity(tan).Should().BeTrue("tan {0} {1} is undefined, found {2}", angle, unit, tan);
            }
        }
    }

    public static TheoryData<double, AngleUnit, string, decimal> RationalValues => new()
    {
        // Manual p. 61: sin 30° = 1/2.
        { 30, AngleUnit.Degree, "sin", 0.5m },
        { 150, AngleUnit.Degree, "sin", 0.5m },
        { 210, AngleUnit.Degree, "sin", -0.5m },
        { -30, AngleUnit.Degree, "sin", -0.5m },
        { 60, AngleUnit.Degree, "cos", 0.5m },
        { 240, AngleUnit.Degree, "cos", -0.5m },
        { 45, AngleUnit.Degree, "tan", 1m },
        { 315, AngleUnit.Degree, "tan", -1m },
        { 405, AngleUnit.Degree, "tan", 1m },
        { 50, AngleUnit.Gradian, "tan", 1m },
        { 150, AngleUnit.Gradian, "tan", -1m },
        { Math.PI / 6, AngleUnit.Radian, "sin", 0.5m },
        { 13 * Math.PI / 6, AngleUnit.Radian, "sin", 0.5m },
        { Math.PI / 3, AngleUnit.Radian, "cos", 0.5m },
        { Math.PI / 4, AngleUnit.Radian, "tan", 1m },
    };

    [Theory]
    [MemberData(nameof(RationalValues))]
    public void RationalValues_AreExactAfterTheBuiltInConversionToDecimal(double angle, AngleUnit unit, string function, decimal expected)
    {
        // sin 30° is 0.49999999999999994 in double; (decimal) keeps 15 significant digits, which makes it 0.5.
        double value = function switch
        {
            "sin" => Trigonometry.Sin(angle, unit),
            "cos" => Trigonometry.Cos(angle, unit),
            _ => Trigonometry.Tan(angle, unit),
        };

        ((decimal)value).Should().Be(expected);
    }

    [Fact]
    public void EveryMultipleOfFifteenDegrees_MatchesTheReference()
    {
        // The reference is a 40-digit Taylor series, not System.Math: Math.Tan(-285 * Math.PI / 180) is itself
        // 5.9×10⁻¹⁵ away from tan 75° = 2 + √3.
        for (int degrees = -360; degrees < 720; degrees += 15)
        {
            Reference.Close(Trigonometry.Sin(degrees, AngleUnit.Degree), Reference.SinOfTurns(degrees, 360), "2E-15").Should().BeTrue("sin {0}°", degrees);
            Reference.Close(Trigonometry.Cos(degrees, AngleUnit.Degree), Reference.CosOfTurns(degrees, 360), "2E-15").Should().BeTrue("cos {0}°", degrees);
            if (degrees % 90 != 0 || degrees % 180 == 0)
            {
                Reference.Close(Trigonometry.Tan(degrees, AngleUnit.Degree), Reference.TanOfTurns(degrees, 360), "2E-15").Should().BeTrue("tan {0}°", degrees);
            }
        }
    }

    [Fact]
    public void RadianAngles_OffAMultipleOfARightAngle_UseSystemMath()
    {
        Trigonometry.Sin(1, AngleUnit.Radian).Should().Be(Math.Sin(1));
        Trigonometry.Cos(1, AngleUnit.Radian).Should().Be(Math.Cos(1));
        Trigonometry.Tan(1, AngleUnit.Radian).Should().Be(Math.Tan(1));
        Trigonometry.Sin(1e-300, AngleUnit.Radian).Should().Be(1e-300);

        // π typed with 15 digits is 3.2×10⁻¹⁵ short of π; its sine is not 0, and the calculator does not show 0 either.
        Trigonometry.Sin(3.14159265358979, AngleUnit.Radian).Should().BeGreaterThan(3e-15);
    }

    [Fact]
    public void NonFiniteAngles_GiveNaN()
    {
        Trigonometry.Sin(double.NaN, AngleUnit.Radian).Should().Be(double.NaN);
        Trigonometry.Cos(double.PositiveInfinity, AngleUnit.Degree).Should().Be(double.NaN);
        Trigonometry.Tan(double.NegativeInfinity, AngleUnit.Gradian).Should().Be(double.NaN);
    }

    [Fact]
    public void InverseFunctions_ReturnAnglesInTheRequestedUnit()
    {
        ((decimal)Trigonometry.Asin(0.5, AngleUnit.Degree)).Should().Be(30m);
        Trigonometry.Acos(0, AngleUnit.Degree).Should().Be(90);
        Trigonometry.Atan(1, AngleUnit.Degree).Should().Be(45);
        Trigonometry.Asin(-1, AngleUnit.Gradian).Should().Be(-100);
        Trigonometry.Acos(-1, AngleUnit.Gradian).Should().Be(200);
        Trigonometry.Atan(1, AngleUnit.Gradian).Should().Be(50);
        Trigonometry.Asin(1, AngleUnit.Radian).Should().Be(Math.PI / 2);
        Trigonometry.Acos(0.5, AngleUnit.Radian).Should().Be(Math.Acos(0.5));
        Trigonometry.Atan(2, AngleUnit.Radian).Should().Be(Math.Atan(2));

        Trigonometry.Asin(2, AngleUnit.Degree).Should().Be(double.NaN);
        Trigonometry.Acos(-1.5, AngleUnit.Radian).Should().Be(double.NaN);
    }

    [Fact]
    public void ConvertAngle_BetweenEveryPairOfUnits()
    {
        // Manual p. 61: (π/2)ʳ in Degree mode is 90.
        Trigonometry.ConvertAngle(Math.PI / 2, AngleUnit.Radian, AngleUnit.Degree).Should().Be(90);
        Trigonometry.ConvertAngle(90, AngleUnit.Degree, AngleUnit.Radian).Should().Be(Math.PI / 2);
        Trigonometry.ConvertAngle(90, AngleUnit.Degree, AngleUnit.Gradian).Should().Be(100);
        Trigonometry.ConvertAngle(100, AngleUnit.Gradian, AngleUnit.Degree).Should().Be(90);
        Trigonometry.ConvertAngle(100, AngleUnit.Gradian, AngleUnit.Radian).Should().Be(Math.PI / 2);
        Trigonometry.ConvertAngle(Math.PI, AngleUnit.Radian, AngleUnit.Gradian).Should().Be(200);

        // The same unit returns the angle untouched. A round trip through degrees would not: 0.3 rad comes back as
        // 0.29999999999999993, and 0.7 gradians as 0.6999999999999998.
        Trigonometry.ConvertAngle(0.3, AngleUnit.Radian, AngleUnit.Radian).Should().Be(0.3);
        Trigonometry.ConvertAngle(0.7, AngleUnit.Gradian, AngleUnit.Gradian).Should().Be(0.7);
        Trigonometry.ConvertAngle(123.456, AngleUnit.Degree, AngleUnit.Degree).Should().Be(123.456);
    }

    [Theory]
    // The angle of a point, in the range the calculator displays for Pol( and complex arguments: (−180°, 180°].
    [InlineData(1d, 1d, AngleUnit.Degree, 45d)]
    [InlineData(1d, 0d, AngleUnit.Degree, 90d)]
    [InlineData(0d, 1d, AngleUnit.Degree, 0d)]
    [InlineData(0d, -1d, AngleUnit.Degree, 180d)]
    [InlineData(-1d, 0d, AngleUnit.Degree, -90d)]
    [InlineData(-1d, -1d, AngleUnit.Degree, -135d)]
    [InlineData(1d, 1d, AngleUnit.Gradian, 50d)]
    [InlineData(0d, -2d, AngleUnit.Gradian, 200d)]
    public void Atan2_IsExactAtMultiplesOfARightAngle(double y, double x, AngleUnit unit, double expected)
    {
        Trigonometry.Atan2(y, x, unit).Should().Be(expected);
    }

    [Fact]
    public void Atan2_InRadians_IsSystemMath()
    {
        Trigonometry.Atan2(2, 3, AngleUnit.Radian).Should().Be(Math.Atan2(2, 3));
        Trigonometry.Atan2(0, 0, AngleUnit.Degree).Should().Be(0);
    }

    [Fact]
    public void UndefinedUnits_AreRejected()
    {
        const AngleUnit undefined = (AngleUnit)9;
        Action[] sinCosTan = [() => Trigonometry.Sin(1, undefined), () => Trigonometry.Cos(1, undefined), () => Trigonometry.Tan(1, undefined)];
        Action[] inverse = [() => Trigonometry.Asin(0.5, undefined), () => Trigonometry.Acos(0.5, undefined), () => Trigonometry.Atan(0.5, undefined)];
        Action from = () => Trigonometry.ConvertAngle(1, undefined, AngleUnit.Degree);
        Action to = () => Trigonometry.ConvertAngle(1, AngleUnit.Degree, undefined);
        Action atan2 = () => Trigonometry.Atan2(1, 1, undefined);

        sinCosTan.Should().AllSatisfy(act => act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unit"));
        inverse.Should().AllSatisfy(act => act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unit"));
        from.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unit");
        to.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unit");
        atan2.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("unit");
    }
}
