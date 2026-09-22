// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// The Poisson probability e^(−λ)·λ^x/x!.
/// </summary>
/// <remarks>
/// <para>
/// .NET has no Poisson distribution and no log-gamma function. The obvious exp(x·ln λ − λ − ln x!) cancels: at
/// x = λ = 10⁶ the three terms are about 1.4×10⁷ and their sum about −7, so the rounding of each term, 10⁻⁹ absolute,
/// becomes a relative error of 10⁻⁹ in the probability.
/// </para>
/// <para>
/// This is Loader's saddle-point form instead (C. Loader, <i>Fast and Accurate Computation of Binomial Probabilities</i>,
/// 2000): e^(−λ)λ^x/x! = e^(−stirlerr(x) − bd0(x, λ))/√(2πx), where stirlerr(x) = ln x! − ln(√(2πx)(x/e)^x) is a table up to
/// 15 and its Stirling series above, and bd0(x, λ) = x·ln(x/λ) + λ − x is summed as a series where x is near λ, so that
/// nothing large cancels. Measured against PeterO.Numbers at 50 digits (PoissonDistributionTests): a relative error below
/// 10⁻¹⁵·(1 + |ln P|) wherever the result is a normal <see cref="double"/>, at most 8.7×10⁻¹⁶·(1 + |ln P|) in 3,000
/// samples. A small probability is e raised to a large exponent, whose own rounding is 10⁻¹⁶ of it, so no algorithm in
/// <see cref="double"/> does better in the far tail: P(1772; 637.4) = 7.6×10⁻²⁹⁷ is 3.3×10⁻¹³ off.
/// </para>
/// </remarks>
public static class PoissonDistribution
{
    // √(2π).
    private const double SqrtTwoPi = 2.5066282746310002;

    // stirlerr(n) = ln n! − ½·ln(2πn) − n·ln n + n for n = 0 to 15, from PeterO.Numbers at 60 digits; 0 is a placeholder.
    private static readonly double[] StirlingErrors =
    [
        0,
        0.08106146679532726,
        0.0413406959554093,
        0.02767792568499834,
        0.020790672103765093,
        0.016644691189821193,
        0.013876128823070748,
        0.01189670994589177,
        0.010411265261972096,
        0.009255462182712733,
        0.00833056343336287,
        0.007573675487951841,
        0.00694284010720953,
        0.006408994188004207,
        0.0059513701127588475,
        0.005554733551962801,
    ];

    /// <summary>Returns the probability that a Poisson variable with mean <paramref name="mean"/> is <paramref name="x"/>.</summary>
    /// <param name="x">A whole number, 0 or more.</param>
    /// <param name="mean">The mean λ, more than 0.</param>
    /// <returns>e^(−λ)·λ^x/x!; NaN when <paramref name="x"/> is not a whole number of 0 or more, or λ is not positive.</returns>
    public static double Probability(double x, double mean)
    {
        if (!(double.IsInteger(x) && x >= 0d && mean > 0d))
        {
            return double.NaN;
        }

        return x == 0d ? Math.Exp(-mean) : Math.Exp(-StirlingError(x) - Deviance(x, mean)) / (SqrtTwoPi * Math.Sqrt(x));
    }

    /// <summary>ln x! − ln(√(2πx)(x/e)^x) for a whole x of 1 or more.</summary>
    private static double StirlingError(double x)
    {
        if (x <= 15d)
        {
            return StirlingErrors[(int)x];
        }

        // 1/(12x) − 1/(360x³) + 1/(1260x⁵) − 1/(1680x⁷) + 1/(1188x⁹); the next term is below 10⁻¹⁶ of the sum from 16 on.
        double square = x * x;
        return ((1d / 12d) - (((1d / 360d) - (((1d / 1260d) - (((1d / 1680d) - ((1d / 1188d) / square)) / square)) / square)) / square)) / x;
    }

    /// <summary>x·ln(x/λ) + λ − x, which is 0 at x = λ and positive elsewhere, without cancellation near λ.</summary>
    private static double Deviance(double x, double mean)
    {
        // With v = (x − λ)/(x + λ), bd0 = (x − λ)·v + 2x·Σ v^(2j+1)/(2j + 1), whose terms fall by v² ≤ ¼ while x is between
        // λ/3 and 3λ and cancel by at most a third. Outside, the logarithmic form cancels by a factor of 3 at most.
        if (Math.Abs(x - mean) < 0.5d * (x + mean))
        {
            double ratio = (x - mean) / (x + mean);
            double sum = (x - mean) * ratio;
            double power = 2d * x * ratio;
            double square = ratio * ratio;
            for (int j = 1; ; j++)
            {
                power *= square;
                double next = sum + (power / ((2 * j) + 1));
                if (next == sum)
                {
                    return sum;
                }

                sum = next;
            }
        }

        return (x * Math.Log(x / mean)) + mean - x;
    }
}
