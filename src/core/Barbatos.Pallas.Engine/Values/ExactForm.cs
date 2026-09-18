// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>A transcendental constant a term may be a multiple of.</summary>
internal enum ExactConstant
{
    /// <summary>None: the term is a rational multiple of a square root.</summary>
    None = 0,

    /// <summary>π.</summary>
    Pi = 1,

    /// <summary>e. Only π is a display form; e is kept so that a value written with e reaches the functions as <see cref="double.E"/>.</summary>
    E = 2,
}

/// <summary>
/// One term of an <see cref="ExactForm"/>: <c>Coefficient × √Radicand</c>, times π or e when <see cref="Constant"/> says so.
/// </summary>
/// <param name="Coefficient">The rational coefficient, as a <see cref="decimal"/>.</param>
/// <param name="Radicand">A square-free positive integer; 1 for a term without a square root.</param>
/// <param name="Constant">The transcendental constant the term is a multiple of.</param>
internal readonly record struct ExactTerm(decimal Coefficient, long Radicand, ExactConstant Constant)
{
    public bool IsRational => Radicand == 1 && Constant == ExactConstant.None;

    public bool HasPi => Constant == ExactConstant.Pi;
}

/// <summary>
/// The exact form of a value computed exactly: a short sum of rational multiples of 1, of square roots of integers and of
/// π, such as <c>45√3 + 10√2</c> or <c>⅙π</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is not a number type. Values are still computed with <see cref="decimal"/> and <see cref="double"/>; the form is
/// recorded beside them for two purposes (decision of 18 Sep 2026). The first is display: <c>10√(2)+15×3√(3)</c> is
/// <c>45√(3)+10√(2)</c> on the calculator (manual p. 35), and no search over 15 significant digits can recover a sum of two
/// square roots. The second is exactness: <c>(decimal)Math.Sqrt(2)</c> squared is 2.000000000000014, while the form of
/// <c>√(2)×√(2)</c> is exactly 2.
/// </para>
/// <para>
/// A form is dropped, never guessed, when an operation leaves what it can represent: a product of two multiples of π, a
/// division by a sum of several square roots, more than <see cref="MaxTerms"/> terms, a radicand above
/// <see cref="MaxRadicand"/>, or a <see cref="decimal"/> overflow.
/// </para>
/// </remarks>
internal sealed class ExactForm
{
    /// <summary>The most terms a form keeps; the calculator displays at most two.</summary>
    public const int MaxTerms = 4;

    /// <summary>The largest radicand; factoring it by trial division takes at most 10⁵ steps.</summary>
    public const long MaxRadicand = 10_000_000_000;

    /// <summary>The largest denominator recognized in a coefficient before taking a square root.</summary>
    private const long MaxRootDenominator = 100_000;

    // 10⁻²⁵: a decimal quotient is rounded at its 28th significant digit, so 1/3 × 3 lands within this of 1.
    private const decimal CoefficientTolerance = 0.0000000000000000000000001m;

    private readonly ExactTerm[] _terms;

    private ExactForm(ExactTerm[] terms)
    {
        _terms = terms;
    }

    /// <summary>Gets the form of zero, which has no terms.</summary>
    public static ExactForm Zero { get; } = new([]);

    /// <summary>Gets the form of π.</summary>
    public static ExactForm Pi { get; } = new([new ExactTerm(1m, 1, ExactConstant.Pi)]);

    /// <summary>Gets the form of e.</summary>
    public static ExactForm E { get; } = new([new ExactTerm(1m, 1, ExactConstant.E)]);

    /// <summary>Gets the terms, ordered by multiple of π, then radicand.</summary>
    public ReadOnlySpan<ExactTerm> Terms => _terms;

    /// <summary>Gets whether the form is a rational number: no square root and no π.</summary>
    public bool IsRational => _terms.Length == 0 || (_terms.Length == 1 && _terms[0].IsRational);

    /// <summary>Gets the value of a rational form; meaningful only when <see cref="IsRational"/>.</summary>
    public decimal RationalValue => _terms.Length == 0 ? 0m : _terms[0].Coefficient;

    /// <summary>Returns the form of a rational number.</summary>
    public static ExactForm Rational(decimal value)
    {
        return value == 0m ? Zero : new ExactForm([new ExactTerm(value, 1, ExactConstant.None)]);
    }

    /// <summary>Returns the form of the square root of a non-negative rational number, or <see langword="null"/>.</summary>
    /// <param name="value">The rational; never negative, since the square root of a negative number is a Math ERROR or a complex number first.</param>
    /// <remarks>
    /// <c>√(n/d)</c> is written <c>√(n·d)/d</c>, and the squares are taken out of <c>n·d</c>. The rational is first recognized
    /// as a fraction, so that <c>√(1⌟3)</c>, whose decimal is 0.3333…, is <c>√3/3</c>.
    /// </remarks>
    public static ExactForm? SquareRoot(decimal value)
    {
        if (value == 0m)
        {
            return Zero;
        }

        if (!Fractions.TryFromDecimal(value, MaxRootDenominator, CoefficientTolerance * Math.Max(1m, value), out long numerator, out long denominator)
            || numerator > MaxRadicand / denominator)
        {
            return null;
        }

        (long outside, long inside) = ExtractSquares(numerator * denominator);
        return new ExactForm([new ExactTerm(Snap((decimal)outside / denominator), inside, ExactConstant.None)]);
    }

    /// <summary>Returns the sum of two forms, or <see langword="null"/>.</summary>
    public static ExactForm? Add(ExactForm left, ExactForm right)
    {
        List<ExactTerm> terms = [.. left._terms];
        foreach (ExactTerm term in right._terms)
        {
            if (!Accumulate(terms, term))
            {
                return null;
            }
        }

        return Create(terms);
    }

    /// <summary>Returns the negated form.</summary>
    public ExactForm Negate()
    {
        return new ExactForm([.. _terms.Select(term => term with { Coefficient = -term.Coefficient })]);
    }

    /// <summary>Returns the product of two forms, or <see langword="null"/>.</summary>
    public static ExactForm? Multiply(ExactForm left, ExactForm right)
    {
        List<ExactTerm> terms = [];
        foreach (ExactTerm a in left._terms)
        {
            foreach (ExactTerm b in right._terms)
            {
                if ((a.Constant != ExactConstant.None && b.Constant != ExactConstant.None) || !TryMultiply(a, b, out ExactTerm product) || !Accumulate(terms, product))
                {
                    return null;
                }
            }
        }

        return Create(terms);
    }

    /// <summary>Returns the quotient of two forms, or <see langword="null"/>.</summary>
    /// <remarks>
    /// The divisor must be a single term, or a rational plus one square root, whose inverse is again a form:
    /// <c>(a + b√r)⁻¹ = (a − b√r) / (a² − b²r)</c>. Dividing by a multiple of π needs π in every term of the dividend.
    /// </remarks>
    public static ExactForm? Divide(ExactForm left, ExactForm right)
    {
        ExactTerm[] divisor = right._terms;
        if (divisor.Length == 1)
        {
            ExactTerm term = divisor[0];
            ExactForm dividend = left;
            if (term.Constant != ExactConstant.None)
            {
                // π or e cancels only against itself: every term of the dividend must carry the same constant.
                if (left._terms.Any(dividendTerm => dividendTerm.Constant != term.Constant))
                {
                    return null;
                }

                dividend = new ExactForm([.. left._terms.Select(dividendTerm => dividendTerm with { Constant = ExactConstant.None })]);
            }

            decimal scale;
            try
            {
                scale = 1m / (term.Coefficient * term.Radicand);
            }
            catch (OverflowException)
            {
                return null;
            }

            return Multiply(dividend, new ExactForm([new ExactTerm(scale, term.Radicand, ExactConstant.None)]));
        }

        if (divisor.Length == 2 && divisor[0].IsRational && divisor[1].Constant == ExactConstant.None)
        {
            (decimal a, decimal b, long r) = (divisor[0].Coefficient, divisor[1].Coefficient, divisor[1].Radicand);
            decimal norm;
            try
            {
                norm = (a * a) - (b * b * r);
            }
            catch (OverflowException)
            {
                return null;
            }

            ExactForm conjugate = new([new ExactTerm(a, 1, ExactConstant.None), new ExactTerm(-b, r, ExactConstant.None)]);
            ExactForm? numerator = Multiply(left, conjugate);
            return numerator is null || norm == 0m ? null : Divide(numerator, Rational(norm));
        }

        return null;
    }

    /// <summary>Returns the value of the form in <see cref="double"/>.</summary>
    /// <remarks>
    /// A multiple of π is computed from <see cref="double.Pi"/>, so <c>π÷2</c> is exactly the <see cref="double"/> nearest π/2
    /// and <see cref="Trigonometry"/> recognizes it as a right angle.
    /// </remarks>
    public double ToDouble()
    {
        double sum = 0d;
        foreach (ExactTerm term in _terms)
        {
            double value = (double)term.Coefficient;
            if (term.Radicand != 1)
            {
                value *= Math.Sqrt(term.Radicand);
            }

            value *= term.Constant switch
            {
                ExactConstant.Pi => double.Pi,
                ExactConstant.E => double.E,
                _ => 1d,
            };

            sum += value;
        }

        return sum;
    }

    /// <summary>Rounds a coefficient that lies within decimal rounding of a simple fraction to that fraction.</summary>
    /// <remarks>
    /// <c>√2/3 × 3√2</c> multiplies 0.3333333333333333333333333333 by 3 and gets 1.9999999999999999999999999998; the
    /// form is 2.
    /// </remarks>
    internal static decimal Snap(decimal coefficient)
    {
        decimal tolerance = CoefficientTolerance * Math.Max(1m, Math.Abs(coefficient));
        if (Fractions.TryFromDecimal(coefficient, MaxRootDenominator, tolerance, out long numerator, out long denominator))
        {
            decimal snapped = (decimal)numerator / denominator;
            if (Math.Abs(snapped - coefficient) <= tolerance)
            {
                return snapped;
            }
        }

        return coefficient;
    }

    private static bool TryMultiply(ExactTerm a, ExactTerm b, out ExactTerm product)
    {
        // Both radicands are square-free, so with g = gcd(a, b) the product is g² × (a/g)(b/g), and (a/g)(b/g) is square-free.
        long gcd = (long)BigInteger.GreatestCommonDivisor(a.Radicand, b.Radicand);
        long left = a.Radicand / gcd;
        long right = b.Radicand / gcd;
        try
        {
            if (left > MaxRadicand / right)
            {
                product = default;
                return false;
            }

            product = new ExactTerm(Snap(a.Coefficient * b.Coefficient * gcd), left * right, a.Constant != ExactConstant.None ? a.Constant : b.Constant);
            return true;
        }
        catch (OverflowException)
        {
            product = default;
            return false;
        }
    }

    private static bool Accumulate(List<ExactTerm> terms, ExactTerm term)
    {
        int index = terms.FindIndex(existing => existing.Radicand == term.Radicand && existing.Constant == term.Constant);
        if (index < 0)
        {
            terms.Add(term);
            return true;
        }

        try
        {
            terms[index] = terms[index] with { Coefficient = terms[index].Coefficient + term.Coefficient };
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static ExactForm? Create(List<ExactTerm> terms)
    {
        terms.RemoveAll(term => term.Coefficient == 0m);
        if (terms.Count > MaxTerms)
        {
            return null;
        }

        terms.Sort(static (a, b) => a.Constant != b.Constant ? a.Constant.CompareTo(b.Constant) : a.Radicand.CompareTo(b.Radicand));
        return new ExactForm([.. terms]);
    }

    private static (long Outside, long Inside) ExtractSquares(long value)
    {
        long outside = 1;
        long inside = 1;
        foreach ((long prime, int exponent) in IntegerFunctions.PrimeFactors(value))
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

        return (outside, inside);
    }
}
