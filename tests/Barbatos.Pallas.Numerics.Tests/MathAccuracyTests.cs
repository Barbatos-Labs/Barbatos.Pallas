// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Pallas.Numerics.Tests.Support;
using CsCheck;

namespace Barbatos.Pallas.Numerics.Tests;

/// <summary>
/// Pins the accuracy docs/PRECISION.md states for System.Math and Trigonometry: a relative error of at most 5×10⁻¹⁵,
/// checked against 40-digit references that share no code with System.Math.
/// </summary>
/// <remarks>
/// Pallas does not wrap System.Math; the engine calls it directly. These tests still belong here, because Pallas's
/// accuracy claim rests on them. CI runs them on Windows x64 (UCRT) only: the application ships on Windows alone, and
/// the Linux jobs were dropped with it (docs/PRECISION.md I5).
/// </remarks>
public sealed class MathAccuracyTests
{
    private const string Tolerance = "5E-15";

    [Fact]
    public void ManualExamples_AreReproducedByBuiltInsAndDecimalFormatting()
    {
        // Values the manual prints, from System.Math and System.Double, shown with 10 significant digits by the
        // built-in (decimal) conversion and "G10" format.
        Display(Math.Sinh(1)).Should().Be("1.175201194");                  // p. 63
        Display(Math.Log(90)).Should().Be("4.49980967");                   // p. 57
        Display(Math.Exp(1)).Should().Be("2.718281828");
        Math.Log10(1000).Should().Be(3);                                   // p. 57
        Math.Log(16, 2).Should().Be(4);                                    // p. 57
        Display(Math.Sqrt(2) * 3).Should().Be("4.242640687");              // p. 33
        Display(99 * Math.Sqrt(999)).Should().Be("3129.089165");           // p. 35
        double.RootN(32, 5).Should().Be(2);                                // p. 34
        Math.Pow(Math.Pow(5, 2), 3).Should().Be(15625);                    // p. 33
        ((decimal)double.Hypot(Math.Sqrt(2), Math.Sqrt(2))).Should().Be(2m);          // p. 62, Pol(√2, √2)
        ((decimal)(double.Atan2Pi(Math.Sqrt(2), Math.Sqrt(2)) * 180)).Should().Be(45m);
        ((decimal)(Math.Sqrt(2) * Trigonometry.Cos(45, AngleUnit.Degree))).Should().Be(1m); // p. 62, Rec(√2, 45°)
    }

    [Fact]
    public void Exp_IsAccurateToFifteenDigits()
    {
        Gen.Double[-700, 700].Sample(x => Reference.Close(Math.Exp(x), Reference.Exp(x), Tolerance), iter: 2_000);
    }

    [Fact]
    public void Ln_IsAccurateToFifteenDigits()
    {
        Gen.Double[1e-300, 1e300].Sample(x => Reference.Close(Math.Log(x), Reference.Ln(x), Tolerance), iter: 2_000);
    }

    [Fact]
    public void Log10_IsAccurateToFifteenDigits()
    {
        Gen.Double[1e-300, 1e300].Sample(x => Reference.Close(Math.Log10(x), Reference.Log10(x), Tolerance), iter: 2_000);
    }

    [Fact]
    public void Sqrt_IsAccurateToFifteenDigits()
    {
        Gen.Double[0, 1e300].Sample(x => Reference.Close(Math.Sqrt(x), Reference.Sqrt(x), Tolerance), iter: 2_000);
    }

    [Fact]
    public void Pow_IsAccurateToFifteenDigits()
    {
        Gen.Select(Gen.Double[0.001, 1000], Gen.Double[-50, 50])
            .Sample((x, y) => Reference.Close(Math.Pow(x, y), Reference.Pow(x, y), Tolerance), iter: 2_000);
    }

    [Fact]
    public void Trigonometry_InDegreesAndGradians_IsAccurateAtAnyMagnitude()
    {
        // Converting a large angle to radians in double loses digits (10⁶° is 17453.29… rad, whose ulp is 3.6×10⁻¹²), so
        // Trigonometry first removes whole turns, which is exact for degrees and gradians.
        Gen.Double[-1e6, 1e6].Sample(angle =>
            Reference.Close(Trigonometry.Sin(angle, AngleUnit.Degree), Reference.SinOfTurns(angle, 360), Tolerance)
            && Reference.Close(Trigonometry.Cos(angle, AngleUnit.Degree), Reference.CosOfTurns(angle, 360), Tolerance)
            && Reference.Close(Trigonometry.Sin(angle, AngleUnit.Gradian), Reference.SinOfTurns(angle, 400), Tolerance)
            && Reference.Close(Trigonometry.Cos(angle, AngleUnit.Gradian), Reference.CosOfTurns(angle, 400), Tolerance), iter: 2_000);
    }

    [Fact]
    public void SinAndCos_AreAccurateToFifteenDigits()
    {
        // Absolute tolerance near zero crossings, where a relative bound would demand more than a double can hold.
        Gen.Double[-10, 10].Sample(x =>
            Reference.Close(Trigonometry.Sin(x, AngleUnit.Radian), Reference.Sin(x), Tolerance)
            && Reference.Close(Trigonometry.Cos(x, AngleUnit.Radian), Reference.Cos(x), Tolerance), iter: 2_000);
    }

    private static string Display(double value) => ((decimal)value).ToString("G10", CultureInfo.InvariantCulture);
}
