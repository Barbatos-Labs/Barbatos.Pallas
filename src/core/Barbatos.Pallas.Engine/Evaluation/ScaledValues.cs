// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Real values as integers over powers of ten, for the calculations that are exact on <see cref="BigInteger"/>: statistics
/// sums and fits, quartiles, binomial probabilities.
/// </summary>
/// <remarks>
/// A <see cref="decimal"/> is its mantissa over a power of ten. A value held as <see cref="double"/> enters as its
/// shortest round-trip decimal, the digits it was entered or displayed with: 1.6×10⁻¹⁹ is 16/10²⁰, not the 60-digit
/// binary fraction the double is.
/// </remarks>
internal static class ScaledValues
{
    /// <summary>A real value as an integer times 10^−scale; the scale is negative for a large <see cref="double"/>.</summary>
    public static (BigInteger Mantissa, int Scale) Decompose(Value value)
    {
        if (value.Kind == ValueKind.DecimalReal)
        {
            // Exact: the product is the decimal's mantissa, at most 2⁹⁶ − 1, which decimal holds with scale 0.
            decimal number = value.ToDecimal();
            return (new BigInteger(number * DecimalDigits.PowerOfTen(number.Scale)), number.Scale);
        }

        // The shortest text that reads back as the double, such as "1.6E-19" or "1E+30". It is always exponential: a value
        // is held as a double only below 10⁻¹⁴ or beyond 7.9×10²⁸ (the precision rule).
        string[] text = value.ToDouble().ToString("R", CultureInfo.InvariantCulture).Split('E');
        string[] digits = text[0].Split('.');
        int decimals = digits.Length == 2 ? digits[1].Length : 0;
        BigInteger mantissa = BigInteger.Parse(string.Concat(digits), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        return (mantissa, decimals - int.Parse(text[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
    }

    /// <summary>Each value as an integer over one power of ten, 0 or more, shared by all.</summary>
    public static (BigInteger[] Integers, int Scale) Scaled(IReadOnlyList<Value> values)
    {
        (BigInteger Mantissa, int Scale)[] parts = [.. values.Select(Decompose)];
        int scale = 0;
        foreach ((_, int partScale) in parts)
        {
            scale = Math.Max(scale, partScale);
        }

        return ([.. parts.Select(part => part.Mantissa * BigInteger.Pow(10, scale - part.Scale))], scale);
    }

    /// <summary>Whether a real value is a whole number, and which.</summary>
    public static bool TryGetWhole(Value value, out BigInteger whole)
    {
        (BigInteger mantissa, int scale) = Decompose(value);
        whole = BigInteger.DivRem(mantissa * BigInteger.Pow(10, Math.Max(0, -scale)), BigInteger.Pow(10, Math.Max(0, scale)), out BigInteger remainder);
        return remainder.IsZero;
    }
}
