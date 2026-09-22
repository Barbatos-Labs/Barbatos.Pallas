// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Solvers;

/// <summary>
/// The roots of a polynomial: all of them at once numerically, and the rational a numeric root stands for when it is
/// one.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Find"/> is the method of Aberth: every root is refined at once by a Newton step corrected for the pull of
/// the other roots, which converges from the same starting circle for any polynomial and does not divide a root out, so
/// no root carries the error of the ones found before it. A calculator solves polynomials of degree 2 to 4, where the
/// iteration settles in a few dozen steps.
/// </para>
/// <para>
/// A root that is rational is recovered exactly rather than left as a decimal: <see cref="TryGetRational"/> reads the
/// continued fraction of the approximation, whose convergents are the only rationals a double can stand for, and
/// <see cref="IntegerPolynomial.SignAt"/> says which of them is a root. That, with
/// <see cref="IntegerPolynomial.Deflate"/>, is what lets a cubic or a quartic that factors over the rationals keep the
/// exact roots of the quadratic that is left.
/// </para>
/// </remarks>
public static class PolynomialRoots
{
    /// <summary>The largest denominator a double can determine: below it, a convergent nearer than 2⁻⁵³ is the rational itself.</summary>
    private const double DenominatorLimit = 1e12;

    /// <summary>Sweeps of the iteration: it settles a simple root in a few dozen and a repeated one in a few hundred.</summary>
    /// <remarks>A fixed number of sweeps, because a sweep of a root that has settled moves it by nothing.</remarks>
    private const int Sweeps = 2000;

    /// <summary>Returns every complex root of the polynomial, by the method of Aberth.</summary>
    /// <param name="coefficients">The coefficients, ascending; <c>coefficients[i]</c> multiplies x to the power i.</param>
    /// <returns>The roots, as many as the degree, in no particular order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The polynomial is constant, or a coefficient is not finite.</exception>
    public static Complex[] Find(IReadOnlyList<double> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        int degree = -1;
        for (int i = coefficients.Count - 1; i >= 0; i--)
        {
            if (!double.IsFinite(coefficients[i]))
            {
                throw new ArgumentException("A coefficient is not a finite number.", nameof(coefficients));
            }

            degree = degree < 0 && coefficients[i] != 0d ? i : degree;
        }

        if (degree < 1)
        {
            throw new ArgumentException("A polynomial has roots from degree 1 on.", nameof(coefficients));
        }

        // p(s·w) has the roots of p divided by s. Taking s as the geometric mean of the root magnitudes, |a₀/aₙ| to the
        // power 1/n, puts them around the unit circle, where a term of Horner's scheme cannot overflow; the coefficients
        // are built through their logarithms, and divided by the largest, for the same reason.
        double[] scaled = Scaled(coefficients, degree, out double scale);

        // The roots lie inside the circle of Cauchy, and starting on it at angles that are not symmetric about the real
        // axis keeps two of them from meeting on it and iterating as one.
        double radius = 1d;
        for (int i = 0; i < degree; i++)
        {
            radius = Math.Max(radius, 1d + (Math.Abs(scaled[i]) / Math.Abs(scaled[degree])));
        }

        Complex[] roots = new Complex[degree];
        for (int k = 0; k < degree; k++)
        {
            roots[k] = Complex.FromPolarCoordinates(radius, (2d * double.Pi * k / degree) + 0.5d);
        }

        for (int sweep = 0; sweep < Sweeps; sweep++)
        {
            Refine(scaled, roots, degree);
        }

        return [.. roots.Select(root => root * scale)];
    }

    /// <summary>The polynomial in the variable x/s, its coefficients divided by the largest of them; s comes back as the scale.</summary>
    private static double[] Scaled(IReadOnlyList<double> coefficients, int degree, out double scale)
    {
        double logarithm = coefficients[0] == 0d ? 0d : Math.Log(Math.Abs(coefficients[0] / coefficients[degree])) / degree;
        double largest = double.NegativeInfinity;
        double[] logarithms = new double[degree + 1];
        for (int i = 0; i <= degree; i++)
        {
            logarithms[i] = Math.Log(Math.Abs(coefficients[i])) + (i * logarithm);
            largest = Math.Max(largest, logarithms[i]);
        }

        scale = Math.Exp(logarithm);
        return [.. Enumerable.Range(0, degree + 1).Select(i => double.CopySign(Math.Exp(logarithms[i] - largest), coefficients[i]))];
    }

    /// <summary>Returns the rational the approximation stands for, when that rational is a root of the polynomial.</summary>
    /// <param name="approximation">An approximation of a real root.</param>
    /// <param name="coefficients">The coefficients of the polynomial, ascending.</param>
    /// <param name="numerator">The numerator of the root, on success.</param>
    /// <param name="denominator">The denominator of the root, positive, on success.</param>
    /// <returns><see langword="true"/> when a convergent of the approximation is a root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    public static bool TryGetRational(double approximation, IReadOnlyList<BigInteger> coefficients, out BigInteger numerator, out BigInteger denominator)
    {
        ArgumentNullException.ThrowIfNull(coefficients);

        // The convergents of the continued fraction: hᵢ = aᵢ·hᵢ₋₁ + hᵢ₋₂ over kᵢ = aᵢ·kᵢ₋₁ + kᵢ₋₂, started at 1/0 and 0/1.
        numerator = BigInteger.One;
        denominator = BigInteger.Zero;
        BigInteger previousNumerator = BigInteger.Zero;
        BigInteger previousDenominator = BigInteger.One;
        double rest = approximation;
        while (double.IsFinite(rest) && (double)denominator <= DenominatorLimit)
        {
            double whole = Math.Floor(rest);
            BigInteger term = new(whole);
            (numerator, previousNumerator) = ((term * numerator) + previousNumerator, numerator);
            (denominator, previousDenominator) = ((term * denominator) + previousDenominator, denominator);
            // The denominators of the convergents are positive: the first is 1, and every term after the first is 1 or more.
            if (IntegerPolynomial.SignAt(coefficients, numerator, denominator) == 0)
            {
                return true;
            }

            rest = 1d / (rest - whole);
        }

        numerator = BigInteger.Zero;
        denominator = BigInteger.One;
        return false;
    }

    /// <summary>One sweep of Aberth over every root.</summary>
    private static void Refine(IReadOnlyList<double> coefficients, Complex[] roots, int degree)
    {
        for (int k = 0; k < degree; k++)
        {
            (Complex value, Complex slope) = EvaluateWithDerivative(coefficients, degree, roots[k]);
            Complex ratio = value / slope;
            Complex pull = Complex.Zero;
            for (int j = 0; j < degree; j++)
            {
                pull += j == k ? Complex.Zero : Complex.One / (roots[k] - roots[j]);
            }

            // A step that is not finite is a root the sweep cannot improve: two roots on top of each other, or a
            // derivative of 0. It is left where it is, for the pull of the other roots to move it next time.
            Complex step = ratio / (Complex.One - (ratio * pull));
            roots[k] -= Complex.IsFinite(step) ? step : Complex.Zero;
        }
    }

    /// <summary>The value and the derivative at a point, by the scheme of Horner.</summary>
    private static (Complex Value, Complex Slope) EvaluateWithDerivative(IReadOnlyList<double> coefficients, int degree, Complex point)
    {
        Complex value = coefficients[degree];
        Complex slope = Complex.Zero;
        for (int i = degree - 1; i >= 0; i--)
        {
            slope = (slope * point) + value;
            value = (value * point) + coefficients[i];
        }

        return (value, slope);
    }
}
