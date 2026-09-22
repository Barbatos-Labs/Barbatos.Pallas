// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Statistics;

/// <summary>
/// Exact sums, means, variances, least-squares coefficients and correlation of one- or two-variable data with frequencies.
/// </summary>
/// <remarks>
/// <para>
/// .NET has no statistics, and the one-pass formulas the calculator documents (manual pp. 93-95) cancel:
/// Sxx = Σx² − (Σx)²/n of equal 20-digit decimals comes out as about 10⁻²⁷ instead of 0 in <see cref="decimal"/>, and the
/// slope Sxy/Sxx of a vertical line becomes a huge number instead of a Math ERROR. Each value here is an integer over a
/// power of ten, like a <see cref="decimal"/>, so every column is scaled to integers once and every sum is exact on
/// <see cref="BigInteger"/>: Sxx is 0 exactly when the x values are all equal, and each result is a fraction of integers
/// that the caller rounds once.
/// </para>
/// <para>
/// There is no rational number type: a result is its numerator and a positive denominator, in lowest terms. The linear and
/// quadratic fits solve their normal equations by Cramer's rule on the integer sums.
/// </para>
/// </remarks>
public sealed class ExactSample
{
    // Σ f·x^k for k = 0..4, Σ f·x^k·y for k = 0..2, and Σ f·y² — all on the scaled integers.
    private readonly BigInteger[] _xSums = new BigInteger[5];
    private readonly BigInteger[] _xySums = new BigInteger[3];
    private readonly BigInteger _ySquares;
    private readonly int _xScale;
    private readonly int _yScale;
    private readonly int _frequencyScale;

    private ExactSample(ReadOnlySpan<BigInteger> x, int xScale, ReadOnlySpan<BigInteger> y, int yScale, ReadOnlySpan<BigInteger> frequencies, int frequencyScale)
    {
        _xScale = xScale;
        _yScale = yScale;
        _frequencyScale = frequencyScale;
        HasY = !y.IsEmpty;
        BigInteger one = BigInteger.Pow(10, frequencyScale);
        for (int row = 0; row < x.Length; row++)
        {
            BigInteger power = frequencies.IsEmpty ? one : frequencies[row];
            BigInteger yValue = HasY ? y[row] : BigInteger.Zero;
            for (int k = 0; k < _xSums.Length; k++)
            {
                _xSums[k] += power;
                if (k < _xySums.Length)
                {
                    _xySums[k] += power * yValue;
                }

                power *= x[row];
            }

            _ySquares += (frequencies.IsEmpty ? one : frequencies[row]) * yValue * yValue;
        }
    }

    /// <summary>Gets whether the sample has a second variable, y.</summary>
    public bool HasY { get; }

    /// <summary>Gets n, the sum of the frequencies.</summary>
    public (BigInteger Numerator, BigInteger Denominator) Count => Lowest(_xSums[0], BigInteger.Pow(10, _frequencyScale));

    /// <summary>Creates a sample of decimals.</summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values, one per x value; empty for one variable.</param>
    /// <param name="frequencies">The frequency of each row, 0 or more; empty when every row counts once.</param>
    /// <returns>The sample.</returns>
    /// <exception cref="ArgumentException"><paramref name="y"/> or <paramref name="frequencies"/> has the wrong length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A frequency is negative.</exception>
    public static ExactSample FromDecimals(ReadOnlySpan<decimal> x, ReadOnlySpan<decimal> y, ReadOnlySpan<decimal> frequencies)
    {
        (BigInteger[] xIntegers, int xScale) = Scaled(x);
        (BigInteger[] yIntegers, int yScale) = Scaled(y);
        (BigInteger[] frequencyIntegers, int frequencyScale) = Scaled(frequencies);
        return FromScaledIntegers(xIntegers, xScale, yIntegers, yScale, frequencyIntegers, frequencyScale);
    }

    /// <summary>Creates a sample of values each given as an integer over a power of ten, one power per column.</summary>
    /// <param name="x">The x values times 10^<paramref name="xScale"/>.</param>
    /// <param name="xScale">The power of ten of the x values, 0 or more.</param>
    /// <param name="y">The y values times 10^<paramref name="yScale"/>, one per x value; empty for one variable.</param>
    /// <param name="yScale">The power of ten of the y values, 0 or more.</param>
    /// <param name="frequencies">The frequencies times 10^<paramref name="frequencyScale"/>, 0 or more; empty when every row counts once.</param>
    /// <param name="frequencyScale">The power of ten of the frequencies, 0 or more.</param>
    /// <returns>The sample.</returns>
    /// <exception cref="ArgumentException"><paramref name="y"/> or <paramref name="frequencies"/> has the wrong length.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A scale or a frequency is negative.</exception>
    public static ExactSample FromScaledIntegers(ReadOnlySpan<BigInteger> x, int xScale, ReadOnlySpan<BigInteger> y, int yScale, ReadOnlySpan<BigInteger> frequencies, int frequencyScale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(xScale);
        ArgumentOutOfRangeException.ThrowIfNegative(yScale);
        ArgumentOutOfRangeException.ThrowIfNegative(frequencyScale);
        RequireLength(y, x.Length, nameof(y));
        RequireLength(frequencies, x.Length, nameof(frequencies));
        foreach (BigInteger frequency in frequencies)
        {
            if (frequency.Sign < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frequencies), frequency, "A frequency is 0 or more.");
            }
        }

        return new ExactSample(x, xScale, y, yScale, frequencies, frequencyScale);
    }

    /// <summary>Returns a sum Σ f·x^i·y^j: Σx to Σx⁴, Σy, Σxy, Σx²y and Σy², or n for i = j = 0.</summary>
    /// <param name="xPower">The power of x: 0 to 4 with no y, 0 to 2 with y, 0 with y².</param>
    /// <param name="yPower">The power of y: 0, 1 or 2; 1 and 2 need <see cref="HasY"/>.</param>
    /// <returns>The sum, in lowest terms.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sum is not one of those.</exception>
    public (BigInteger Numerator, BigInteger Denominator) Sum(int xPower, int yPower)
    {
        int highest = yPower switch
        {
            0 => 4,
            1 => 2,
            _ => 0,
        };
        ArgumentOutOfRangeException.ThrowIfNegative(xPower);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(xPower, highest);
        if (yPower is < 0 or > 2 || (yPower > 0 && !HasY))
        {
            throw new ArgumentOutOfRangeException(nameof(yPower), yPower, "Σy needs y, and y is summed to the power 2 at most.");
        }

        BigInteger sum = yPower switch
        {
            0 => _xSums[xPower],
            1 => _xySums[xPower],
            _ => _ySquares,
        };
        return Lowest(sum, BigInteger.Pow(10, _frequencyScale + (xPower * _xScale) + (yPower * _yScale)));
    }

    /// <summary>Returns the mean of a variable, Σx/n.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="mean">The mean, in lowest terms.</param>
    /// <returns><see langword="false"/> when n is 0.</returns>
    public bool TryGetMean(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) mean)
    {
        (BigInteger first, _, int scale) = Moments(variable);
        return TryDivide(first, _xSums[0] * BigInteger.Pow(10, scale), out mean);
    }

    /// <summary>Returns the population variance of a variable, σ² = Σ(x − x̄)²/n = (nΣx² − (Σx)²)/n².</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="variance">The variance, in lowest terms.</param>
    /// <returns><see langword="false"/> when n is 0.</returns>
    public bool TryGetPopulationVariance(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) variance)
    {
        (BigInteger first, BigInteger second, int scale) = Moments(variable);
        BigInteger n = _xSums[0];
        return TryDivide((n * second) - (first * first), n * n * BigInteger.Pow(10, 2 * scale), out variance);
    }

    /// <summary>Returns the sample variance of a variable, s² = Σ(x − x̄)²/(n − 1) = (nΣx² − (Σx)²)/(n(n − 1)).</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="variance">The variance, in lowest terms; negative when n is between 0 and 1.</param>
    /// <returns><see langword="false"/> when n is 0 or 1.</returns>
    public bool TryGetSampleVariance(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) variance)
    {
        (BigInteger first, BigInteger second, int scale) = Moments(variable);
        BigInteger n = _xSums[0];
        BigInteger nMinusOne = n - BigInteger.Pow(10, _frequencyScale);
        return TryDivide((n * second) - (first * first), n * nMinusOne * BigInteger.Pow(10, 2 * scale), out variance);
    }

    /// <summary>Returns the least-squares line y = a + bx.</summary>
    /// <param name="intercept">a, in lowest terms.</param>
    /// <param name="slope">b, in lowest terms.</param>
    /// <returns><see langword="false"/> when the x values are all equal, or n is 0.</returns>
    /// <exception cref="InvalidOperationException">The sample has no y.</exception>
    public bool TryGetLinearFit(out (BigInteger Numerator, BigInteger Denominator) intercept, out (BigInteger Numerator, BigInteger Denominator) slope)
    {
        RequireY();
        BigInteger sxx = Spread(_xSums[0], _xSums[1], _xSums[2]);
        BigInteger yScale = BigInteger.Pow(10, _yScale);
        slope = default;
        return TryDivide((_xySums[0] * _xSums[2]) - (_xSums[1] * _xySums[1]), sxx * yScale, out intercept)
            && TryDivide(CrossSpread() * BigInteger.Pow(10, _xScale), sxx * yScale, out slope);
    }

    /// <summary>Returns the least-squares parabola y = a + bx + cx².</summary>
    /// <param name="a">a, in lowest terms.</param>
    /// <param name="b">b, in lowest terms.</param>
    /// <param name="c">c, in lowest terms.</param>
    /// <returns><see langword="false"/> when there are fewer than three distinct x values.</returns>
    /// <exception cref="InvalidOperationException">The sample has no y.</exception>
    public bool TryGetQuadraticFit(
        out (BigInteger Numerator, BigInteger Denominator) a,
        out (BigInteger Numerator, BigInteger Denominator) b,
        out (BigInteger Numerator, BigInteger Denominator) c)
    {
        RequireY();
        BigInteger[] s = _xSums;
        BigInteger[] t = _xySums;
        BigInteger determinant = Determinant(s[0], s[1], s[2], s[1], s[2], s[3], s[2], s[3], s[4]);
        BigInteger denominator = determinant * BigInteger.Pow(10, _yScale);
        b = default;
        c = default;
        return TryDivide(Determinant(t[0], s[1], s[2], t[1], s[2], s[3], t[2], s[3], s[4]), denominator, out a)
            && TryDivide(Determinant(s[0], t[0], s[2], s[1], t[1], s[3], s[2], t[2], s[4]) * BigInteger.Pow(10, _xScale), denominator, out b)
            && TryDivide(Determinant(s[0], s[1], t[0], s[1], s[2], t[1], s[2], s[3], t[2]) * BigInteger.Pow(10, 2 * _xScale), denominator, out c);
    }

    /// <summary>Returns the square of the correlation coefficient r = Sxy/√(Sxx·Syy), and its sign.</summary>
    /// <param name="square">r², in lowest terms.</param>
    /// <param name="sign">The sign of r: −1, 0 or 1.</param>
    /// <returns><see langword="false"/> when the x values or the y values are all equal, or n is 0.</returns>
    /// <exception cref="InvalidOperationException">The sample has no y.</exception>
    /// <remarks>r itself is irrational in general, so the exact result is r², and the caller takes the square root once.</remarks>
    public bool TryGetCorrelationSquared(out (BigInteger Numerator, BigInteger Denominator) square, out int sign)
    {
        RequireY();
        BigInteger cross = CrossSpread();
        sign = cross.Sign;
        return TryDivide(cross * cross, Spread(_xSums[0], _xSums[1], _xSums[2]) * Spread(_xSums[0], _xySums[0], _ySquares), out square);
    }

    private static (BigInteger[] Integers, int Scale) Scaled(ReadOnlySpan<decimal> values)
    {
        int scale = 0;
        foreach (decimal value in values)
        {
            scale = Math.Max(scale, value.Scale);
        }

        BigInteger[] integers = new BigInteger[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            integers[i] = Mantissa(values[i]) * BigInteger.Pow(10, scale - values[i].Scale);
        }

        return (integers, scale);
    }

    /// <summary>The integer a <see cref="decimal"/> is, before its power of ten: 2.50 is 250.</summary>
    private static BigInteger Mantissa(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        _ = decimal.GetBits(value, bits);
        BigInteger magnitude = ((BigInteger)(uint)bits[2] << 64) | ((BigInteger)(uint)bits[1] << 32) | (uint)bits[0];
        return decimal.IsNegative(value) ? -magnitude : magnitude;
    }

    private static void RequireLength<T>(ReadOnlySpan<T> values, int length, string name)
    {
        if (!values.IsEmpty && values.Length != length)
        {
            throw new ArgumentException("There must be one value per x value, or none.", name);
        }
    }

    /// <summary>nΣv² − (Σv)², which is n² times the population variance and 0 exactly when every v is equal.</summary>
    private static BigInteger Spread(BigInteger n, BigInteger first, BigInteger second) => (n * second) - (first * first);

    private static BigInteger Determinant(
        BigInteger a, BigInteger b, BigInteger c,
        BigInteger d, BigInteger e, BigInteger f,
        BigInteger g, BigInteger h, BigInteger i)
    {
        return (a * ((e * i) - (f * h))) - (b * ((d * i) - (f * g))) + (c * ((d * h) - (e * g)));
    }

    private static bool TryDivide(BigInteger numerator, BigInteger denominator, out (BigInteger Numerator, BigInteger Denominator) quotient)
    {
        if (denominator.IsZero)
        {
            quotient = default;
            return false;
        }

        quotient = Lowest(numerator, denominator);
        return true;
    }

    private static (BigInteger Numerator, BigInteger Denominator) Lowest(BigInteger numerator, BigInteger denominator)
    {
        BigInteger divisor = BigInteger.CopySign(BigInteger.GreatestCommonDivisor(numerator, denominator), denominator);
        return (numerator / divisor, denominator / divisor);
    }

    /// <summary>nΣxy − ΣxΣy.</summary>
    private BigInteger CrossSpread() => (_xSums[0] * _xySums[1]) - (_xSums[1] * _xySums[0]);

    private (BigInteger First, BigInteger Second, int Scale) Moments(SampleVariable variable)
    {
        if (variable == SampleVariable.X)
        {
            return (_xSums[1], _xSums[2], _xScale);
        }

        RequireY();
        return (_xySums[0], _ySquares, _yScale);
    }

    private void RequireY()
    {
        if (!HasY)
        {
            throw new InvalidOperationException("A one-variable sample has no y.");
        }
    }
}
