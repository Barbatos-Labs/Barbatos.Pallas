// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Significant-digit rounding, which .NET provides only through format strings.
/// </summary>
internal static class DecimalDigits
{
    // Written out: a loop that builds the table runs once, in static initialization, where mutation testing cannot reach it.
    private static readonly decimal[] PowersOfTen =
    [
        1m,
        10m,
        100m,
        1_000m,
        10_000m,
        100_000m,
        1_000_000m,
        10_000_000m,
        100_000_000m,
        1_000_000_000m,
        10_000_000_000m,
        100_000_000_000m,
        1_000_000_000_000m,
        10_000_000_000_000m,
        100_000_000_000_000m,
        1_000_000_000_000_000m,
        10_000_000_000_000_000m,
        100_000_000_000_000_000m,
        1_000_000_000_000_000_000m,
        10_000_000_000_000_000_000m,
        100_000_000_000_000_000_000m,
        1_000_000_000_000_000_000_000m,
        10_000_000_000_000_000_000_000m,
        100_000_000_000_000_000_000_000m,
        1_000_000_000_000_000_000_000_000m,
        10_000_000_000_000_000_000_000_000m,
        100_000_000_000_000_000_000_000_000m,
        1_000_000_000_000_000_000_000_000_000m,
        10_000_000_000_000_000_000_000_000_000m,
    ];

    /// <summary>Returns ⌊log₁₀|value|⌋ for a nonzero value.</summary>
    public static int Exponent(decimal value)
    {
        decimal magnitude = Math.Abs(value);
        int exponent = 0;
        while (magnitude >= 10m)
        {
            magnitude /= 10m;
            exponent++;
        }

        while (magnitude < 1m)
        {
            magnitude *= 10m;
            exponent--;
        }

        return exponent;
    }

    /// <summary>Returns 10ⁿ for 0 ≤ n ≤ 28.</summary>
    public static decimal PowerOfTen(int exponent) => PowersOfTen[exponent];

    /// <summary>Rounds to a number of significant digits, half away from zero.</summary>
    public static decimal RoundToSignificantDigits(decimal value, int digits)
    {
        if (value == 0m)
        {
            return 0m;
        }

        int decimals = digits - 1 - Exponent(value);
        if (decimals >= 0)
        {
            return Math.Round(value, Math.Min(decimals, 28), MidpointRounding.AwayFromZero);
        }

        decimal factor = PowerOfTen(Math.Min(-decimals, 28));
        return Math.Round(value / factor, 0, MidpointRounding.AwayFromZero) * factor;
    }

    /// <summary>Rounds a <see cref="double"/> to a number of significant digits, through the "E" format.</summary>
    public static double RoundToSignificantDigits(double value, int digits)
    {
        string text = value.ToString("E" + (digits - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        return double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
