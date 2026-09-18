// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A real number rounded for display: its sign, significant digits and decimal exponent.
/// </summary>
/// <param name="Negative">Whether the number is negative.</param>
/// <param name="Digits">The significant digits, the first nonzero unless the number is zero.</param>
/// <param name="Exponent">The power of ten of the first digit: 1.024 has exponent 0, 1024 has exponent 3.</param>
internal readonly record struct Significand(bool Negative, string Digits, int Exponent)
{
    public bool IsZero => Digits.All(digit => digit == '0');
}

/// <summary>
/// Writes real numbers the way the calculator displays them (Norm, Fix, Sci; manual p. 23), in the output notation of
/// docs/LINEAR-SYNTAX.md §7: <c>1.67×10^-1</c>.
/// </summary>
/// <remarks>
/// Rounding is half away from zero, the display default (docs/PRECISION.md §5), and happens on <see cref="decimal"/>:
/// a <see cref="double"/> outside <see cref="decimal"/>'s range is rounded by the "E" format, which works on its exact
/// binary value.
/// </remarks>
internal static class NumberText
{
    public const string TimesTenTo = "×10^";

    /// <summary>The largest magnitude displayed without an exponent: |x| ≥ 10¹⁰ is written 1×10^10.</summary>
    private const decimal LargestFixed = 10_000_000_000m;

    /// <summary>Returns a value rounded to a number of significant digits.</summary>
    public static Significand Round(Value value, int significantDigits)
    {
        if (value.Kind == ValueKind.DecimalReal)
        {
            decimal d = value.ToDecimal();
            decimal magnitude = DecimalDigits.RoundToSignificantDigits(Math.Abs(d), significantDigits);
            if (magnitude == 0m)
            {
                return new Significand(false, new string('0', significantDigits), 0);
            }

            int exponent = DecimalDigits.Exponent(magnitude);
            decimal mantissa = exponent >= 0 ? magnitude / DecimalDigits.PowerOfTen(exponent) : magnitude * DecimalDigits.PowerOfTen(-exponent);
            string text = mantissa.ToString("F" + (significantDigits - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
            return new Significand(decimal.IsNegative(d), text.Replace(".", string.Empty, StringComparison.Ordinal), exponent);
        }

        double x = value.ToDouble();
        string exponential = Math.Abs(x).ToString("E" + (significantDigits - 1).ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        int marker = exponential.IndexOf('E', StringComparison.Ordinal);
        return new Significand(
            double.IsNegative(x),
            exponential[..marker].Replace(".", string.Empty, StringComparison.Ordinal),
            int.Parse(exponential[(marker + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture));
    }

    /// <summary>Formats a real value in a number format.</summary>
    public static string Format(Value value, NumberFormat format)
    {
        return format.Kind switch
        {
            NumberFormatKind.Fix => FormatFix(value, format.Digits),
            NumberFormatKind.Sci => Exponential(Round(value, format.Digits), trimZeros: false),
            _ => FormatNorm(value, format.Digits),
        };
    }

    /// <summary>Norm: 10 significant digits, trailing zeros removed, in exponent form below 10⁻² (Norm 1) or 10⁻⁹ (Norm 2), or from 10¹⁰.</summary>
    public static string FormatNorm(Value value, int norm)
    {
        Significand rounded = Round(value, 10);
        if (rounded.IsZero)
        {
            return "0";
        }

        int smallestFixedExponent = norm == 2 ? -9 : -2;
        return rounded.Exponent < smallestFixedExponent || rounded.Exponent >= 10
            ? Exponential(rounded, trimZeros: true)
            : Fixed(rounded, trimZeros: true);
    }

    /// <summary>Writes a significand in exponent form: <c>1.024×10^6</c>.</summary>
    public static string Exponential(Significand significand, bool trimZeros)
    {
        StringBuilder text = new();
        if (significand.Negative && !significand.IsZero)
        {
            text.Append('-');
        }

        text.Append(significand.Digits[0]);
        string fraction = significand.Digits[1..];
        if (trimZeros)
        {
            fraction = fraction.TrimEnd('0');
        }

        if (fraction.Length > 0)
        {
            text.Append('.').Append(fraction);
        }

        return text.Append(TimesTenTo).Append(significand.Exponent.ToString(CultureInfo.InvariantCulture)).ToString();
    }

    /// <summary>Writes a significand without an exponent: <c>1024</c>, <c>0.005</c>.</summary>
    public static string Fixed(Significand significand, bool trimZeros)
    {
        string digits = significand.Digits;
        int exponent = significand.Exponent;
        string integer;
        string fraction;
        if (exponent >= 0)
        {
            // ENG shifted far enough (1024000 as 1024000000000×10^-6) needs more digits than were rounded.
            string padded = digits.PadRight(exponent + 1, '0');
            integer = padded[..(exponent + 1)];
            fraction = padded[(exponent + 1)..];
        }
        else
        {
            integer = "0";
            fraction = new string('0', -exponent - 1) + digits;
        }

        if (trimZeros)
        {
            fraction = fraction.TrimEnd('0');
        }

        string text = fraction.Length > 0 ? integer + "." + fraction : integer;
        return significand.Negative && !significand.IsZero ? "-" + text : text;
    }

    /// <summary>Applies the Decimal Mark and Digit Separator settings to a formatted number (pp. 24-25).</summary>
    public static string Localize(string text, CalculatorSettings settings)
    {
        char separator = settings.DecimalMark == DecimalMark.Dot ? ',' : '.';
        char mark = settings.DecimalMark == DecimalMark.Dot ? '.' : ',';
        StringBuilder result = new();
        int index = 0;
        while (index < text.Length)
        {
            int start = index;
            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
            }

            if (index == start)
            {
                result.Append(text[index] == '.' ? mark : text[index]);
                index++;
                continue;
            }

            // Separators group the integer part in threes. An exponent has at most three digits (10^308), so it never
            // gets one.
            string run = text[start..index];
            bool grouped = settings.DigitSeparator && (start == 0 || text[start - 1] != '.');
            for (int i = 0; i < run.Length; i++)
            {
                if (grouped && i > 0 && (run.Length - i) % 3 == 0)
                {
                    result.Append(separator);
                }

                result.Append(run[i]);
            }
        }

        return result.ToString();
    }

    private static string FormatFix(Value value, int decimals)
    {
        if (value.Kind == ValueKind.DecimalReal)
        {
            decimal rounded = Math.Round(value.ToDecimal(), decimals, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded) < LargestFixed)
            {
                string text = rounded.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                return rounded == 0m ? text.TrimStart('-') : text;
            }
        }
        else if (Math.Abs(value.ToDouble()) < 1d)
        {
            // Below 10⁻¹⁴, every Fix format shows zero.
            return decimals == 0 ? "0" : "0." + new string('0', decimals);
        }

        // From 10¹⁰ the display has no room for decimals: exponent form keeps the Fix decimals in the mantissa.
        Significand significand = Round(value, 10);
        return Exponential(Round(value, Math.Min(10, Math.Max(1, decimals + 1))) with { Negative = significand.Negative }, trimZeros: false);
    }
}
