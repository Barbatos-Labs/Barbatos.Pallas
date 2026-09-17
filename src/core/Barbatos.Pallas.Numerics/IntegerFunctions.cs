// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// Exact integer functions on <see cref="BigInteger"/> that the BCL does not provide.
/// </summary>
/// <remarks>
/// The greatest common divisor is already <see cref="BigInteger.GreatestCommonDivisor(BigInteger, BigInteger)"/>, so it is
/// not repeated here. These functions compute exact results of any size; limits such as the calculator's
/// <c>n! &lt; 10¹⁰⁰</c> belong to the caller, which should check them before asking for a result it would reject.
/// </remarks>
public static class IntegerFunctions
{
    /// <summary>Returns <c>n!</c>.</summary>
    /// <param name="n">A non-negative integer.</param>
    /// <returns>The exact factorial; <c>0! = 1</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative.</exception>
    public static BigInteger Factorial(int n)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        BigInteger result = BigInteger.One;
        for (long factor = 2; factor <= n; factor++)
        {
            result *= factor;
        }

        return result;
    }

    /// <summary>Returns the number of ordered selections of <paramref name="r"/> items from <paramref name="n"/>: <c>n! / (n − r)!</c>.</summary>
    /// <param name="n">The number of items; non-negative.</param>
    /// <param name="r">The number selected; between 0 and <paramref name="n"/>.</param>
    /// <returns>The exact number of permutations.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative, or <paramref name="r"/> is outside [0, n].</exception>
    public static BigInteger Permutations(long n, long r)
    {
        ValidateSelection(n, r);

        // Counting steps rather than running a factor up to n: "factor <= n" never ends when n is long.MaxValue.
        BigInteger result = BigInteger.One;
        for (long step = 0; step < r; step++)
        {
            result *= n - step;
        }

        return result;
    }

    /// <summary>Returns the number of unordered selections of <paramref name="r"/> items from <paramref name="n"/>: <c>n! / (r! (n − r)!)</c>.</summary>
    /// <param name="n">The number of items; non-negative.</param>
    /// <param name="r">The number selected; between 0 and <paramref name="n"/>.</param>
    /// <returns>The exact number of combinations.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="n"/> is negative, or <paramref name="r"/> is outside [0, n].</exception>
    public static BigInteger Combinations(long n, long r)
    {
        ValidateSelection(n, r);

        // Multiplying before dividing keeps every intermediate an integer: after step i the value is C(n − k + i, i).
        long k = Math.Min(r, n - r);
        BigInteger result = BigInteger.One;
        for (long i = 1; i <= k; i++)
        {
            result = result * (n - k + i) / i;
        }

        return result;
    }

    /// <summary>Returns the least common multiple.</summary>
    /// <param name="left">The first integer.</param>
    /// <param name="right">The second integer.</param>
    /// <returns>The smallest non-negative common multiple; 0 when either argument is 0.</returns>
    public static BigInteger LeastCommonMultiple(BigInteger left, BigInteger right)
    {
        // The divisor is 0 only when both arguments are; with one zero argument the product below is already 0.
        BigInteger divisor = BigInteger.GreatestCommonDivisor(left, right);
        return divisor.IsZero ? BigInteger.Zero : BigInteger.Abs(left / divisor * right);
    }

    /// <summary>
    /// Returns the prime factorization of a positive integer.
    /// </summary>
    /// <param name="value">A positive integer.</param>
    /// <returns>The prime factors in increasing order with their exponents; empty for 1.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not positive.</exception>
    /// <remarks>
    /// Trial division, which needs at most √value steps: instant for the calculator's 10-digit limit, and a few seconds in
    /// the worst case (a large prime near <see cref="long.MaxValue"/>).
    /// </remarks>
    public static IReadOnlyList<(long Prime, int Exponent)> PrimeFactors(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

        List<(long Prime, int Exponent)> factors = [];
        long remaining = value;
        for (long divisor = 2; divisor <= remaining / divisor; divisor += divisor == 2 ? 1 : 2)
        {
            int exponent = 0;
            while (remaining % divisor == 0)
            {
                remaining /= divisor;
                exponent++;
            }

            if (exponent > 0)
            {
                factors.Add((divisor, exponent));
            }
        }

        if (remaining > 1)
        {
            factors.Add((remaining, 1));
        }

        return factors;
    }

    private static void ValidateSelection(long n, long r)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(n);
        ArgumentOutOfRangeException.ThrowIfNegative(r);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(r, n);
    }
}
