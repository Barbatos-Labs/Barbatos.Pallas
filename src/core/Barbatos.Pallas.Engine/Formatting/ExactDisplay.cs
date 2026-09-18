// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using System.Text;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The display forms beyond decimals: fractions, square roots and π (Standard), prime factors, recurring decimals,
/// engineering notation and degrees-minutes-seconds (manual pp. 42-50, 64).
/// </summary>
/// <remarks>
/// <para>
/// An exact value is shown from what it is: its exact form, or the fraction its <see cref="decimal"/> stands for, found
/// within decimal rounding (10⁻²⁵). An approximate value, the result of a function such as sin, is recognized within
/// 10⁻¹³ relative (decision of 18 Sep 2026), and only as a simple form with small denominators, so that a transcendental
/// result is not dressed up as a fraction by coincidence: denominators up to 10⁴, √ forms <c>a√b⌟c</c>, and
/// <c>p⌟qπ</c> with q ≤ 100.
/// </para>
/// <para>
/// The bounds are the calculator's in the <see cref="CalculatorProfile.Standard"/> profile (docs/CALCULATOR-CATALOG.md §4):
/// a fraction whose mixed form needs more than 10 digits and separators is shown as a decimal, and so is a square-root
/// form with a coefficient of 100 or more.
/// </para>
/// </remarks>
internal static class ExactDisplay
{
    private const decimal ExactTolerance = 0.0000000000000000000000001m;

    // A fraction of an exact value is recognized up to this denominator in both profiles; the display bounds, not the
    // search, decide whether it is shown.
    private const long ExactDenominator = 1_000_000_000_000_000;
    private const long ApproximateDenominator = 10_000;
    private const long PiDenominator = 100;

    private static readonly string[] EngineeringSymbols = ["f", "p", "n", "μ", "m", string.Empty, "k", "M", "G", "T", "P", "E"];
    private static readonly char[] Superscripts = ['⁰', '¹', '²', '³', '⁴', '⁵', '⁶', '⁷', '⁸', '⁹'];

    /// <summary>Returns the Standard display, or <see langword="null"/> when the value is best shown as a decimal.</summary>
    public static string? Standard(Value value, CalculatorSettings settings, CalculatorProfile profile, bool allowRoots)
    {
        Bounds bounds = Bounds.For(profile);
        if (value.Form is { } form)
        {
            return allowRoots ? FormText(form, bounds) : null;
        }

        if (TryGetFraction(value, out long numerator, out long denominator))
        {
            return denominator == 1 ? null : FractionText(numerator, denominator, settings.FractionResult == FractionResult.Mixed, bounds);
        }

        if (!allowRoots || value.IsExact || value.Kind != ValueKind.DecimalReal)
        {
            return null;
        }

        return RecognizeRoot(value.ToDecimal(), bounds) ?? RecognizePi(value.ToDouble(), bounds);
    }

    /// <summary>Returns the FORMAT improper or mixed fraction, or <see langword="null"/>.</summary>
    public static string? Fraction(Value value, CalculatorProfile profile, bool mixed)
    {
        return TryGetFraction(value, out long numerator, out long denominator) && denominator != 1
            ? FractionText(numerator, denominator, mixed, Bounds.For(profile))
            : null;
    }

    /// <summary>Returns the prime factorization of a positive integer (p. 45), or <see langword="null"/>.</summary>
    /// <remarks>
    /// The calculator divides by primes below 1000 only: a remaining factor below 1009² = 1018081 is then prime, and a larger
    /// one is shown unfactored in parentheses, as in <c>2036162 = 2×(1018081)</c>. The Extended profile factors completely
    /// (deviation D7).
    /// </remarks>
    public static string? PrimeFactors(Value value, CalculatorProfile profile)
    {
        long limit = profile == CalculatorProfile.Standard ? 10_000_000_000 : 1_000_000_000_000_000;
        if (!value.IsReal || !ValueMath.TryGetInteger(value, out long number) || number < 1 || number >= limit)
        {
            return null;
        }

        if (number == 1)
        {
            return "1";
        }

        // The calculator's primes are those up to 997, the largest below 1000; the rest of the number is one factor.
        List<string> factors = [];
        long remaining = 1;
        foreach ((long prime, int exponent) in IntegerFunctions.PrimeFactors(number))
        {
            if (profile == CalculatorProfile.Extended || prime <= 997)
            {
                factors.Add(Power(prime, exponent));
                continue;
            }

            for (int i = 0; i < exponent; i++)
            {
                remaining *= prime;
            }
        }

        if (remaining > 1)
        {
            factors.Add(remaining < 1_018_081
                ? remaining.ToString(CultureInfo.InvariantCulture)
                : "(" + remaining.ToString(CultureInfo.InvariantCulture) + ")");
        }

        return string.Join('×', factors);
    }

    /// <summary>Returns the recurring decimal of a fraction (p. 46), or <see langword="null"/> when it terminates or is too long.</summary>
    public static string? RecurringDecimal(Value value, CalculatorProfile profile)
    {
        if (!TryGetFraction(value, out long numerator, out long denominator) || denominator == 1
            || !FitsFraction(numerator, denominator, Bounds.For(profile)))
        {
            return null;
        }

        long whole = Math.Abs(numerator) / denominator;
        long remainder = Math.Abs(numerator) % denominator;
        Dictionary<long, int> seen = [];
        StringBuilder digits = new();
        while (remainder != 0 && !seen.ContainsKey(remainder))
        {
            seen[remainder] = digits.Length;
            remainder *= 10;
            digits.Append((char)('0' + (remainder / denominator)));
            remainder %= denominator;
        }

        if (remainder == 0)
        {
            return null;
        }

        // Data size: one byte per digit, one for the point, three for the recurring marks; at most 99 bytes.
        string integer = whole.ToString(CultureInfo.InvariantCulture);
        if (integer.Length + digits.Length + 1 + 3 > 99)
        {
            return null;
        }

        int start = seen[remainder];
        string text = integer + "." + digits.ToString(0, start) + "(" + digits.ToString(start, digits.Length - start) + ")";
        return long.IsNegative(numerator) ? "-" + text : text;
    }

    /// <summary>Returns engineering notation: an exponent that is a multiple of 3, shifted by <paramref name="shift"/> steps of 3.</summary>
    /// <remarks>With Engineer Symbol on, the exponent is written as a symbol: 1024000 is <c>1.024M</c>, shifted once <c>1024k</c> (p. 64).</remarks>
    public static string Engineering(Value value, CalculatorSettings settings, int shift)
    {
        Significand rounded = NumberText.Round(value, 10);
        if (rounded.IsZero)
        {
            return "0";
        }

        int exponent = (int)Math.Floor(rounded.Exponent / 3d) * 3 + (shift * 3);
        string mantissa = NumberText.Fixed(rounded with { Exponent = rounded.Exponent - exponent }, trimZeros: true);
        int symbol = (exponent / 3) + 5;
        return settings.EngineerSymbol && exponent % 3 == 0 && symbol is >= 0 and < 12
            ? mantissa + EngineeringSymbols[symbol]
            : mantissa + NumberText.TimesTenTo + exponent.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Returns the display with Engineer Symbol on: a symbol when one keeps the mantissa in [1, 1000) (assumption U17).</summary>
    public static string EngineeringSymbol(Value value, CalculatorSettings settings)
    {
        Significand rounded = NumberText.Round(value, 10);
        int exponent = (int)Math.Floor(rounded.Exponent / 3d) * 3;
        return rounded.IsZero || exponent == 0 || exponent < -15 || exponent > 18
            ? NumberText.Format(value, settings.NumberFormat)
            : Engineering(value, settings, shift: 0);
    }

    /// <summary>Returns degrees-minutes-seconds (p. 49), seconds to two decimals, or <see langword="null"/> beyond 9999999°59′59″.</summary>
    public static string? Sexagesimal(Value value)
    {
        // Checked before converting: the seconds of 5×10²⁸° overflow decimal. Rounding can still carry 9999999.99999999°
        // into 10000000°, hence the second check.
        if (value.Kind != ValueKind.DecimalReal || Math.Abs(value.ToDecimal()) >= 10_000_000m)
        {
            return null;
        }

        (bool negative, decimal degrees, decimal minutes, decimal seconds) = Numerics.Sexagesimal.FromDegrees(value.ToDecimal(), 2, MidpointRounding.AwayFromZero);
        if (degrees > 9_999_999m)
        {
            return null;
        }

        string text = string.Create(CultureInfo.InvariantCulture, $"{degrees:0}°{minutes:0}′{Trimmed(seconds)}″");
        return negative && (degrees != 0m || minutes != 0m || seconds != 0m) ? "-" + text : text;
    }

    /// <summary>Recognizes the fraction a real value stands for.</summary>
    internal static bool TryGetFraction(Value value, out long numerator, out long denominator)
    {
        numerator = 0;
        denominator = 1;
        return value.Kind == ValueKind.DecimalReal && (value.IsExact
            ? Fractions.TryFromDecimal(value.ToDecimal(), ExactDenominator, ExactTolerance * Math.Max(1m, Math.Abs(value.ToDecimal())), out numerator, out denominator)
            : Fractions.TryFromDecimal(value.ToDecimal(), ApproximateDenominator, ValueMath.RelativeTolerance * Math.Abs(value.ToDecimal()), out numerator, out denominator));
    }

    private static string? FractionText(long numerator, long denominator, bool mixed, Bounds bounds)
    {
        if (!FitsFraction(numerator, denominator, bounds))
        {
            return null;
        }

        string sign = long.IsNegative(numerator) ? "-" : string.Empty;
        long magnitude = Math.Abs(numerator);
        long whole = Math.DivRem(magnitude, denominator, out long remainder);
        if (mixed && whole != 0)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{sign}{whole}⌟{remainder}⌟{denominator}");
        }

        return string.Create(CultureInfo.InvariantCulture, $"{sign}{magnitude}⌟{denominator}");
    }

    /// <summary>Whether a fraction fits the display: its mixed form uses at most 10 digits and separators (pp. 46, 171).</summary>
    private static bool FitsFraction(long numerator, long denominator, Bounds bounds)
    {
        long magnitude = Math.Abs(numerator);
        long whole = magnitude / denominator;
        long remainder = magnitude % denominator;
        int length = remainder == 0
            ? Digits(whole)
            : whole == 0
                ? Digits(remainder) + 1 + Digits(denominator)
                : Digits(whole) + Digits(remainder) + Digits(denominator) + 2;
        return length <= bounds.FractionLength;
    }

    private static string? FormText(ExactForm form, Bounds bounds)
    {
        ReadOnlySpan<ExactTerm> terms = form.Terms;
        if (terms.Length is 0 or > 2)
        {
            return null;
        }

        // e has a form so that it reaches the functions exactly, but the calculator shows no e forms.
        foreach (ExactTerm term in terms)
        {
            if (term.Constant == ExactConstant.E)
            {
                return null;
            }
        }

        // A multiple of π stands alone: p⌟qπ, with |x| < 10⁶. π under a square root, or beside one, has no form.
        if (terms[0].HasPi || (terms.Length == 2 && terms[1].HasPi))
        {
            if (terms.Length != 1 || terms[0].Radicand != 1 || !TryCoefficient(terms[0].Coefficient, out long p, out long q)
                || Math.Abs(form.ToDouble()) >= 1e6 || !FitsFraction(p, q, bounds))
            {
                return null;
            }

            string coefficient = q == 1
                ? (p == 1 ? string.Empty : p == -1 ? "-" : p.ToString(CultureInfo.InvariantCulture))
                : string.Create(CultureInfo.InvariantCulture, $"{p}⌟{q}");
            return coefficient + "π";
        }

        // The rational part first, then square roots by decreasing radicand (assumption U15).
        List<(long Numerator, long Denominator, long Radicand)> parts = [];
        foreach (ExactTerm term in terms)
        {
            if (!TryCoefficient(term.Coefficient, out long p, out long q))
            {
                return null;
            }

            // A rational part has radicand 1, within every bound.
            if (Math.Abs(p) >= bounds.Coefficient || q >= bounds.Coefficient || term.Radicand >= bounds.Radicand)
            {
                return null;
            }

            parts.Add((p, q, term.Radicand));
        }

        parts.Sort(static (a, b) => a.Radicand == 1 || b.Radicand == 1 ? a.Radicand.CompareTo(b.Radicand) : b.Radicand.CompareTo(a.Radicand));

        StringBuilder text = new();
        foreach ((long p, long q, long radicand) in parts)
        {
            if (long.IsNegative(p))
            {
                text.Append('-');
            }
            else if (text.Length > 0)
            {
                text.Append('+');
            }

            long magnitude = Math.Abs(p);
            if (radicand == 1)
            {
                text.Append(q == 1
                    ? magnitude.ToString(CultureInfo.InvariantCulture)
                    : string.Create(CultureInfo.InvariantCulture, $"{magnitude}⌟{q}"));
                continue;
            }

            if (magnitude != 1)
            {
                text.Append(magnitude.ToString(CultureInfo.InvariantCulture));
            }

            text.Append("√(").Append(radicand.ToString(CultureInfo.InvariantCulture)).Append(')');
            if (q != 1)
            {
                text.Append('⌟').Append(q.ToString(CultureInfo.InvariantCulture));
            }
        }

        return text.ToString();
    }

    private static bool TryCoefficient(decimal coefficient, out long numerator, out long denominator)
    {
        return Fractions.TryFromDecimal(coefficient, 1_000_000, ExactTolerance * Math.Max(1m, Math.Abs(coefficient)), out numerator, out denominator);
    }

    /// <summary>Recognizes <c>a√b⌟c</c> from an approximate value through its square, a fraction with a small denominator.</summary>
    private static string? RecognizeRoot(decimal value, Bounds bounds)
    {
        decimal square;
        try
        {
            square = value * value;
        }
        catch (OverflowException)
        {
            return null;
        }

        // A zero value was already shown as a fraction, so p is never 0.
        if (square >= 10_000_000m
            || !Fractions.TryFromDecimal(square, ApproximateDenominator, 2m * ValueMath.RelativeTolerance * square, out long p, out long q))
        {
            return null;
        }

        // √(p/q) = √(p·q)/q, with the squares taken out of p·q.
        long outside = 1;
        long inside = 1;
        foreach ((long prime, int exponent) in IntegerFunctions.PrimeFactors(p * q))
        {
            for (int i = 0; i < exponent / 2; i++)
            {
                outside *= prime;
            }

            if (exponent % 2 == 1)
            {
                inside *= prime;
            }
        }

        long divisor = (long)BigInteger.GreatestCommonDivisor(outside, q);
        long a = outside / divisor;
        long c = q / divisor;
        if (inside == 1 || a >= bounds.Coefficient || c >= bounds.Coefficient || inside >= bounds.Radicand)
        {
            return null;
        }

        string text = (a == 1 ? string.Empty : a.ToString(CultureInfo.InvariantCulture))
            + "√(" + inside.ToString(CultureInfo.InvariantCulture) + ")"
            + (c == 1 ? string.Empty : "⌟" + c.ToString(CultureInfo.InvariantCulture));
        return decimal.IsNegative(value) ? "-" + text : text;
    }

    /// <summary>Recognizes <c>p⌟qπ</c> from an approximate value.</summary>
    private static string? RecognizePi(double value, Bounds bounds)
    {
        if (Math.Abs(value) >= 1e6)
        {
            return null;
        }

        decimal multiple = (decimal)(value / double.Pi);
        if (!Fractions.TryFromDecimal(multiple, PiDenominator, ValueMath.RelativeTolerance * Math.Abs(multiple), out long p, out long q)
            || !FitsFraction(p, q, bounds))
        {
            return null;
        }

        string coefficient = q == 1
            ? (p == 1 ? string.Empty : p == -1 ? "-" : p.ToString(CultureInfo.InvariantCulture))
            : string.Create(CultureInfo.InvariantCulture, $"{p}⌟{q}");
        return coefficient + "π";
    }

    private static string Power(long prime, int exponent)
    {
        string text = prime.ToString(CultureInfo.InvariantCulture);
        return exponent switch
        {
            1 => text,
            2 or 3 => text + Superscripts[exponent],
            _ => text + "^" + exponent.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static string Trimmed(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static int Digits(long value) => value.ToString(CultureInfo.InvariantCulture).Length;

    /// <summary>The display bounds of a profile.</summary>
    private readonly record struct Bounds(int FractionLength, long Coefficient, long Radicand)
    {
        public static Bounds For(CalculatorProfile profile)
        {
            // Standard: a, c, d, f < 100 and b, e < 1000 in ±a√b⌟c ± d√e⌟f (p. 35). Extended: wider.
            return profile == CalculatorProfile.Standard ? new Bounds(10, 100, 1000) : new Bounds(20, 10_000, 1_000_000);
        }
    }
}
