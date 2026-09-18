// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// Conversions between decimal degrees and degrees-minutes-seconds, in <see cref="decimal"/>.
/// </summary>
public static class Sexagesimal
{
    // An int, not a decimal: a const decimal compiles to a static field set in a static constructor, which uses of the
    // inlined value never run.
    private const int SecondsPerDegree = 3600;

    /// <summary>
    /// Converts degrees, minutes and seconds to decimal degrees.
    /// </summary>
    /// <param name="degrees">The degrees.</param>
    /// <param name="minutes">The minutes.</param>
    /// <param name="seconds">The seconds.</param>
    /// <returns><c>degrees + minutes/60 + seconds/3600</c>. Negate the result for a negative angle.</returns>
    /// <remarks>
    /// Computed as one division of the total seconds by 3600, rather than as a sum of two divisions, so each of the
    /// 28 decimal places is rounded only once: 2°20′30″ + 0°9′30″ comes out as exactly 2.5.
    /// </remarks>
    public static decimal ToDegrees(decimal degrees, decimal minutes, decimal seconds)
    {
        return ((degrees * SecondsPerDegree) + (minutes * 60m) + seconds) / SecondsPerDegree;
    }

    /// <summary>
    /// Splits decimal degrees into degrees, minutes and seconds, rounding the seconds and carrying any overflow.
    /// </summary>
    /// <param name="value">The angle in decimal degrees.</param>
    /// <param name="secondsDecimals">The number of decimal places to keep in the seconds, from 0 to 28.</param>
    /// <param name="mode">How the seconds are rounded.</param>
    /// <returns>
    /// The components of <c>|value|</c> with the sign carried by <c>Negative</c>. A rounded 60″ carries into the minutes and a
    /// resulting 60′ into the degrees, so 2.4999999999999999999999999999° becomes 2°30′0″ rather than 2°29′60″.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="secondsDecimals"/> is outside 0-28.</exception>
    /// <exception cref="OverflowException">|<paramref name="value"/>| × 3600 is beyond <see cref="decimal"/>, from about 2.2×10²⁵°.</exception>
    public static (bool Negative, decimal Degrees, decimal Minutes, decimal Seconds) FromDegrees(decimal value, int secondsDecimals, MidpointRounding mode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(secondsDecimals);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(secondsDecimals, 28);

        decimal totalSeconds = Math.Round(Math.Abs(value) * SecondsPerDegree, secondsDecimals, mode);
        decimal degrees = Math.Floor(totalSeconds / SecondsPerDegree);
        decimal remainder = totalSeconds - (degrees * SecondsPerDegree);
        decimal minutes = Math.Floor(remainder / 60m);
        decimal seconds = remainder - (minutes * 60m);

        return (value < 0m, degrees, minutes, seconds);
    }
}
