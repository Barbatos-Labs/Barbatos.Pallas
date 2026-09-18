// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// Trigonometric functions of an angle in a calculator angle unit.
/// </summary>
/// <remarks>
/// <para>
/// .NET provides the functions; this class only chooses among them for an <see cref="AngleUnit"/>, which .NET does not
/// have. Results follow <see cref="double"/> semantics: NaN outside the domain, ±∞ for the tangent of an odd multiple of a
/// right angle.
/// </para>
/// <para>
/// Degrees and gradians use <see cref="double.SinPi"/>, <see cref="double.CosPi"/> and <see cref="double.TanPi"/>, which
/// take the angle as a multiple of π. They are exact at multiples of a right angle, where <c>Math.Cos(Math.PI / 2)</c> is
/// 6.1×10⁻¹⁷ and <c>Math.Tan(Math.PI / 2)</c> is 1.6×10¹⁶. Whole turns are removed first with
/// <see cref="double.Ieee754Remainder"/>, which is exact: dividing 10⁶° by 180 directly loses 6×10⁻¹³ of a half-turn.
/// </para>
/// </remarks>
public static class Trigonometry
{
    /// <summary>Returns the sine of an angle.</summary>
    /// <param name="angle">The angle.</param>
    /// <param name="unit">The unit of <paramref name="angle"/>.</param>
    /// <returns>The sine; exactly 0 or ±1 at multiples of a right angle; NaN for a NaN or infinite angle.</returns>
    public static double Sin(double angle, AngleUnit unit)
    {
        return TryGetHalfTurns(angle, unit, out double halfTurns) ? double.SinPi(halfTurns) : Math.Sin(angle);
    }

    /// <summary>Returns the cosine of an angle.</summary>
    /// <param name="angle">The angle.</param>
    /// <param name="unit">The unit of <paramref name="angle"/>.</param>
    /// <returns>The cosine; exactly 0 or ±1 at multiples of a right angle; NaN for a NaN or infinite angle.</returns>
    public static double Cos(double angle, AngleUnit unit)
    {
        return TryGetHalfTurns(angle, unit, out double halfTurns) ? double.CosPi(halfTurns) : Math.Cos(angle);
    }

    /// <summary>Returns the tangent of an angle.</summary>
    /// <param name="angle">The angle.</param>
    /// <param name="unit">The unit of <paramref name="angle"/>.</param>
    /// <returns>The tangent; exactly 0 at multiples of a straight angle and ±∞ at odd multiples of a right angle.</returns>
    public static double Tan(double angle, AngleUnit unit)
    {
        return TryGetHalfTurns(angle, unit, out double halfTurns) ? double.TanPi(halfTurns) : Math.Tan(angle);
    }

    /// <summary>Returns the angle whose sine is <paramref name="value"/>.</summary>
    /// <param name="value">A value between −1 and 1.</param>
    /// <param name="unit">The unit of the returned angle.</param>
    /// <returns>An angle between −90° and 90°; NaN outside [−1, 1].</returns>
    public static double Asin(double value, AngleUnit unit)
    {
        return unit switch
        {
            AngleUnit.Degree => double.AsinPi(value) * 180d,
            AngleUnit.Radian => Math.Asin(value),
            AngleUnit.Gradian => double.AsinPi(value) * 200d,
            _ => throw UndefinedUnit(unit),
        };
    }

    /// <summary>Returns the angle whose cosine is <paramref name="value"/>.</summary>
    /// <param name="value">A value between −1 and 1.</param>
    /// <param name="unit">The unit of the returned angle.</param>
    /// <returns>An angle between 0° and 180°; NaN outside [−1, 1].</returns>
    public static double Acos(double value, AngleUnit unit)
    {
        return unit switch
        {
            AngleUnit.Degree => double.AcosPi(value) * 180d,
            AngleUnit.Radian => Math.Acos(value),
            AngleUnit.Gradian => double.AcosPi(value) * 200d,
            _ => throw UndefinedUnit(unit),
        };
    }

    /// <summary>Returns the angle whose tangent is <paramref name="value"/>.</summary>
    /// <param name="value">The value.</param>
    /// <param name="unit">The unit of the returned angle.</param>
    /// <returns>An angle between −90° and 90°.</returns>
    public static double Atan(double value, AngleUnit unit)
    {
        return unit switch
        {
            AngleUnit.Degree => double.AtanPi(value) * 180d,
            AngleUnit.Radian => Math.Atan(value),
            AngleUnit.Gradian => double.AtanPi(value) * 200d,
            _ => throw UndefinedUnit(unit),
        };
    }

    /// <summary>Returns the angle of the point (<paramref name="x"/>, <paramref name="y"/>) from the positive x-axis.</summary>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="unit">The unit of the returned angle.</param>
    /// <returns>
    /// An angle in (−180°, 180°], the range the calculator displays for Pol( and complex arguments (pp. 62, 126); −180°
    /// only for a negative zero <paramref name="y"/>.
    /// </returns>
    public static double Atan2(double y, double x, AngleUnit unit)
    {
        return unit switch
        {
            AngleUnit.Degree => double.Atan2Pi(y, x) * 180d,
            AngleUnit.Radian => Math.Atan2(y, x),
            AngleUnit.Gradian => double.Atan2Pi(y, x) * 200d,
            _ => throw UndefinedUnit(unit),
        };
    }

    /// <summary>Converts an angle from one unit to another.</summary>
    /// <param name="angle">The angle.</param>
    /// <param name="from">The unit of <paramref name="angle"/>.</param>
    /// <param name="to">The unit of the result.</param>
    /// <returns>The same angle in <paramref name="to"/>.</returns>
    /// <remarks>
    /// Degrees and radians convert with <see cref="double.DegreesToRadians"/> and <see cref="double.RadiansToDegrees"/>.
    /// .NET has no gradians; 200 gradians are 180 degrees.
    /// </remarks>
    public static double ConvertAngle(double angle, AngleUnit from, AngleUnit to)
    {
        if (from == to)
        {
            return angle;
        }

        double degrees = from switch
        {
            AngleUnit.Degree => angle,
            AngleUnit.Radian => double.RadiansToDegrees(angle),
            AngleUnit.Gradian => angle * 180d / 200d,
            _ => throw UndefinedUnit(from),
        };

        return to switch
        {
            AngleUnit.Degree => degrees,
            AngleUnit.Radian => double.DegreesToRadians(degrees),
            AngleUnit.Gradian => degrees * 200d / 180d,
            _ => throw UndefinedUnit(to),
        };
    }

    /// <summary>
    /// Expresses an angle as a multiple of π (half-turns), when that can be done without losing accuracy.
    /// </summary>
    /// <remarks>
    /// A radian angle is a multiple of π only approximately, so the ...Pi functions are used only when
    /// <c>angle / π</c> lands exactly on a multiple of ½ (a right angle): <c>Math.PI / 2</c> and <c>3 * Math.PI / 2</c> do.
    /// Otherwise <see cref="Math"/> takes the radians directly. <c>double.SinPi(13 * Math.PI / 6 / Math.PI)</c> was
    /// measured at 0.5000000000000008 where <c>Math.Sin(13 * Math.PI / 6)</c> is 0.5. And 3.14159265358979, π typed with
    /// 15 digits, keeps its sine of 3.2×10⁻¹⁵, as the calculator shows.
    /// </remarks>
    private static bool TryGetHalfTurns(double angle, AngleUnit unit, out double halfTurns)
    {
        switch (unit)
        {
            case AngleUnit.Degree:
                halfTurns = double.Ieee754Remainder(angle, 360d) / 180d;
                return true;
            case AngleUnit.Gradian:
                halfTurns = double.Ieee754Remainder(angle, 400d) / 200d;
                return true;
            case AngleUnit.Radian:
                halfTurns = angle / double.Pi;
                return double.IsInteger(2d * halfTurns);
            default:
                throw UndefinedUnit(unit);
        }
    }

    private static ArgumentOutOfRangeException UndefinedUnit(AngleUnit unit)
    {
        return new ArgumentOutOfRangeException(nameof(unit), unit, "Not a defined AngleUnit value.");
    }
}
