// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Solvers;

/// <summary>
/// Exact algebra of a polynomial with <see cref="BigInteger"/> coefficients, ascending: <c>coefficients[i]</c> multiplies
/// x to the power i.
/// </summary>
/// <remarks>
/// A polynomial of the calculator comes from decimal coefficients, so multiplying it by a power of ten makes every
/// coefficient an integer and leaves the roots where they are. Everything here is then exact: the sign at a rational
/// point, the number of distinct real roots (Sturm's theorem), the polynomial without its repeated factors, and division
/// by a rational root. .NET has no polynomial arithmetic of any kind.
/// </remarks>
public static class IntegerPolynomial
{
    /// <summary>Returns the degree: the index of the highest coefficient that is not 0, or −1 for the zero polynomial.</summary>
    /// <param name="coefficients">The coefficients, ascending.</param>
    /// <returns>The degree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    public static int Degree(IReadOnlyList<BigInteger> coefficients)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        for (int i = coefficients.Count - 1; i >= 0; i--)
        {
            if (!coefficients[i].IsZero)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Returns the sign of the polynomial at a rational point, exactly.</summary>
    /// <param name="coefficients">The coefficients, ascending.</param>
    /// <param name="numerator">The numerator of the point.</param>
    /// <param name="denominator">The denominator of the point, positive.</param>
    /// <returns>−1, 0 or 1.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="denominator"/> is not positive.</exception>
    public static int SignAt(IReadOnlyList<BigInteger> coefficients, BigInteger numerator, BigInteger denominator)
    {
        int degree = Degree(coefficients);
        if (denominator.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(denominator), denominator, "A rational point has a positive denominator.");
        }

        // The sum is q to the power n times p(p/q), and q is positive, so the sum has the sign of the value.
        BigInteger sum = BigInteger.Zero;
        BigInteger power = BigInteger.One;
        for (int i = 0; i <= degree; i++)
        {
            sum += coefficients[i] * power * BigInteger.Pow(denominator, degree - i);
            power *= numerator;
        }

        return sum.Sign;
    }

    /// <summary>Returns the derivative.</summary>
    /// <param name="coefficients">The coefficients, ascending.</param>
    /// <returns>The coefficients of the derivative, ascending.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    public static BigInteger[] Derivative(IReadOnlyList<BigInteger> coefficients)
    {
        int degree = Degree(coefficients);
        return degree <= 0 ? [BigInteger.Zero] : [.. Enumerable.Range(1, degree).Select(i => coefficients[i] * i)];
    }

    /// <summary>Returns the polynomial without its repeated factors: p divided by the greatest common divisor of p and its derivative.</summary>
    /// <param name="coefficients">The coefficients, ascending.</param>
    /// <returns>The square-free part: integer coefficients with the same roots, each of them once.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    public static BigInteger[] SquareFree(IReadOnlyList<BigInteger> coefficients)
    {
        BigInteger[] primitive = Primitive(coefficients);
        BigInteger[] common = Gcd(primitive, Derivative(primitive));
        return Degree(common) < 0 ? primitive : Primitive(Divide(primitive, common));
    }

    /// <summary>Returns the number of distinct real roots, by Sturm's theorem.</summary>
    /// <param name="coefficients">The coefficients, ascending.</param>
    /// <returns>The number of distinct real roots; 0 for a constant polynomial.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The chain is p, its derivative, then the negated remainders. The number of roots is the sign changes at minus
    /// infinity less those at plus infinity, where a member has the sign of its leading coefficient, taken at minus
    /// infinity with the sign of (−1) to the power of its degree. The chain is built on the square-free part, so a
    /// repeated root counts once.
    /// </remarks>
    public static int CountRealRoots(IReadOnlyList<BigInteger> coefficients)
    {
        BigInteger[] squareFree = SquareFree(coefficients);
        List<BigInteger[]> chain = [squareFree, Reduced(Derivative(squareFree))];
        while (Degree(chain[^1]) > 0)
        {
            chain.Add(NegatedRemainder(chain[^2], chain[^1]));
        }

        return SignChanges(chain, atNegativeInfinity: true) - SignChanges(chain, atNegativeInfinity: false);
    }

    /// <summary>Divides the polynomial by (denominator·x − numerator), a factor of it.</summary>
    /// <param name="coefficients">The coefficients, ascending, of a polynomial of degree 1 or more.</param>
    /// <param name="numerator">The numerator of the root.</param>
    /// <param name="denominator">The denominator of the root, positive.</param>
    /// <returns>The quotient, ascending, with integer coefficients.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="denominator"/> is not positive.</exception>
    /// <exception cref="ArgumentException">The rational is not a root of the polynomial.</exception>
    /// <remarks>
    /// The quotient of a primitive polynomial by a primitive factor has integer coefficients (the lemma of Gauss), so
    /// the division that builds it, of a coefficient by the denominator, is exact.
    /// </remarks>
    public static BigInteger[] Deflate(IReadOnlyList<BigInteger> coefficients, BigInteger numerator, BigInteger denominator)
    {
        BigInteger[] primitive = Primitive(coefficients);
        int degree = Degree(primitive);
        BigInteger divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        BigInteger p = numerator / divisor;
        BigInteger q = denominator / divisor;
        if (degree < 1 || SignAt(primitive, p, q) != 0)
        {
            throw new ArgumentException("The rational is not a root of the polynomial.", nameof(numerator));
        }

        // From the top: the quotient b of degree n − 1 has q·b[n−1] = a[n] and q·b[i−1] = a[i] + p·b[i].
        BigInteger[] quotient = new BigInteger[degree];
        BigInteger carry = BigInteger.Zero;
        for (int i = degree; i >= 1; i--)
        {
            quotient[i - 1] = (primitive[i] + carry) / q;
            carry = quotient[i - 1] * p;
        }

        return quotient;
    }

    /// <summary>The next member of Sturm's chain: minus the remainder of the two before it.</summary>
    private static BigInteger[] NegatedRemainder(BigInteger[] a, BigInteger[] b)
    {
        return Reduced([.. Remainder(a, b).Select(BigInteger.Negate)]);
    }

    /// <summary>The greatest common divisor over the rationals, with integer coefficients (a primitive remainder chain).</summary>
    private static BigInteger[] Gcd(BigInteger[] first, BigInteger[] second)
    {
        BigInteger[] a = Primitive(first);
        BigInteger[] b = Primitive(second);
        while (Degree(b) >= 0)
        {
            BigInteger[] remainder = Remainder(a, b);
            a = b;
            b = Primitive(remainder);
        }

        return a;
    }

    /// <summary>The remainder of a divided by b, times a square, so that it keeps the sign the true remainder has.</summary>
    /// <remarks>
    /// Dividing would make fractions, so each step multiplies the dividend by the leading coefficient of the divisor
    /// instead. Squaring that multiplier costs nothing at these degrees and keeps the result a positive multiple of the
    /// remainder, which Sturm's chain needs: a negative one would turn a member's signs around and lose a root.
    /// </remarks>
    private static BigInteger[] Remainder(BigInteger[] a, BigInteger[] b)
    {
        int divisorDegree = Degree(b);
        BigInteger leading = b[divisorDegree];
        BigInteger[] rest = [.. a];
        for (int degree = Degree(rest); degree >= divisorDegree; degree = Degree(rest))
        {
            BigInteger factor = rest[degree];
            for (int i = 0; i <= degree; i++)
            {
                rest[i] *= leading * leading;
            }

            for (int i = 0; i <= divisorDegree; i++)
            {
                rest[degree - divisorDegree + i] -= factor * leading * b[i];
            }
        }

        return rest;
    }

    /// <summary>The quotient of a by b, which divides it.</summary>
    private static BigInteger[] Divide(BigInteger[] a, BigInteger[] b)
    {
        int divisorDegree = Degree(b);
        BigInteger[] rest = [.. a];
        BigInteger[] quotient = new BigInteger[Degree(a) + 1];
        for (int degree = Degree(rest); degree >= divisorDegree; degree = Degree(rest))
        {
            BigInteger factor = rest[degree] / b[divisorDegree];
            quotient[degree - divisorDegree] = factor;
            for (int i = 0; i <= divisorDegree; i++)
            {
                rest[degree - divisorDegree + i] -= factor * b[i];
            }
        }

        return quotient;
    }

    /// <summary>The coefficients divided by their common factor, with a positive leading coefficient.</summary>
    private static BigInteger[] Primitive(IReadOnlyList<BigInteger> coefficients)
    {
        BigInteger[] reduced = Reduced(coefficients);
        int degree = Degree(reduced);
        return degree >= 0 && reduced[degree].Sign < 0 ? [.. reduced.Select(BigInteger.Negate)] : reduced;
    }

    /// <summary>The coefficients divided by their common factor, keeping the sign the polynomial has; the zero polynomial has none.</summary>
    private static BigInteger[] Reduced(IReadOnlyList<BigInteger> coefficients)
    {
        int degree = Degree(coefficients);
        BigInteger divisor = BigInteger.Zero;
        for (int i = 0; i <= degree; i++)
        {
            divisor = BigInteger.GreatestCommonDivisor(divisor, coefficients[i]);
        }

        return [.. Enumerable.Range(0, degree + 1).Select(i => coefficients[i] / divisor)];
    }

    /// <summary>The sign changes along the chain at minus infinity or at plus infinity, where each member has the sign of its leading term.</summary>
    private static int SignChanges(List<BigInteger[]> chain, bool atNegativeInfinity)
    {
        int changes = 0;
        int previous = 0;
        foreach (BigInteger[] member in chain)
        {
            int degree = Degree(member);
            if (degree < 0)
            {
                continue;
            }

            int sign = atNegativeInfinity && degree % 2 != 0 ? -member[degree].Sign : member[degree].Sign;
            if (previous != 0 && sign != previous)
            {
                changes++;
            }

            previous = sign;
        }

        return changes;
    }
}
