// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// The error function, its complement and the inverse of the complement.
/// </summary>
/// <remarks>
/// <para>
/// The normal distribution of the Statistics and Distribution applications is the complementary error function:
/// Φ(t) = erfc(−t/√2)/2, and the inverse normal is the inverse of erfc. .NET has neither function nor the inverse.
/// </para>
/// <para>
/// Below 1, erf is its Maclaurin series, whose 19 terms reach <see cref="double"/> precision there. From 1 up,
/// erfc(x) = e^(−x²)·x·K(x²)/√π, where K is the continued fraction of the incomplete gamma function Γ(½, x²), evaluated
/// backward from a depth of 120; the other function is then the difference from 1, which loses nothing because erfc is at
/// most 0.16 there. The same fraction evaluated forward (Lentz) was measured 6.5×10⁻¹⁵ off near x = 1, backward 5.3×10⁻¹⁶
/// (18 Sep 2026). e^(−x²) is taken as e^(−h²)·e^(−(x−h)(x+h)), with h the sixteenths of x, whose square is exact:
/// <c>Math.Exp(-x * x)</c> carries the rounding of x², x²·2⁻⁵³ relative, which is 5.5×10⁻¹⁵ at x = 7.
/// </para>
/// <para>
/// The accuracy is measured against PeterO.Numbers series at 50 digits (ErrorFunctionTests): a relative error below
/// 2×10⁻¹⁵ for erf and erfc wherever erfc is a normal <see cref="double"/>.
/// </para>
/// </remarks>
public static class ErrorFunction
{
    private const double SqrtPi = 1.7724538509055160;

    private const int Depth = 120;

    // 2/√π · (−1)ⁿ / (n!·(2n + 1)) for n = 0 to 18, from PeterO.Numbers at 60 digits.
    private static readonly double[] MaclaurinCoefficients =
    [
        1.1283791670955126,
        -0.37612638903183754,
        0.11283791670955126,
        -0.026866170645131252,
        0.005223977625442188,
        -0.0008548327023450853,
        0.00012055332981789664,
        -1.492565035840625E-05,
        1.6462114365889248E-06,
        -1.6365844691234924E-07,
        1.4807192815879218E-08,
        -1.2290555301717928E-09,
        9.422759064650411E-11,
        -6.7113668551641105E-12,
        4.4632242632864775E-13,
        -2.7835162072109215E-14,
        1.6342614095367152E-15,
        -9.063970842808673E-17,
        4.763348040515068E-18,
    ];

    /// <summary>Returns the error function of a value.</summary>
    /// <param name="x">The value.</param>
    /// <returns>erf(x), between −1 and 1; ±1 at ±∞; NaN for NaN.</returns>
    public static double Erf(double x)
    {
        return Math.Abs(x) < 1d ? Maclaurin(x) : double.CopySign(1d - UpperTail(Math.Abs(x)), x);
    }

    /// <summary>Returns the complementary error function of a value, 1 − erf(x), without the cancellation of the difference.</summary>
    /// <param name="x">The value.</param>
    /// <returns>erfc(x), between 0 and 2; 0 at +∞ and 2 at −∞; NaN for NaN.</returns>
    public static double Erfc(double x)
    {
        if (x >= 1d)
        {
            return UpperTail(x);
        }

        return x > -1d ? 1d - Maclaurin(x) : 2d - UpperTail(-x);
    }

    /// <summary>Returns the value whose complementary error function is <paramref name="value"/>.</summary>
    /// <param name="value">A value between 0 and 2.</param>
    /// <returns>x with erfc(x) = <paramref name="value"/>; +∞ at 0 and −∞ at 2; NaN outside [0, 2].</returns>
    /// <remarks>
    /// <para>
    /// ln erfc is concave and decreasing, and erfc(x) ≤ e^(−x²) for x ≥ 0. Newton's method on ln erfc(x) = ln y, started at
    /// x = √(−ln y), which is at or beyond the root, therefore decreases to the root without overshooting it; it stops when a
    /// step no longer decreases x. Working on the logarithm keeps the tail in range: erfc(26) is 5.7×10⁻²⁹⁶.
    /// </para>
    /// <para>
    /// The ends need no case of their own: outside [0, 2] the logarithm is NaN, and so is the step, which stops the loop at
    /// once; at 0 the start is +∞, where the step is NaN too.
    /// </para>
    /// </remarks>
    public static double InverseErfc(double value)
    {
        if (value > 1d)
        {
            return -InverseErfc(2d - value);
        }

        double target = Math.Log(value);
        double x = Math.Sqrt(0d - target);
        for (;;)
        {
            (double logarithm, double slope) = LogErfc(x);
            double next = x - ((logarithm - target) / slope);
            if (!(next < x))
            {
                return x;
            }

            x = next;
        }
    }

    private static double Maclaurin(double x)
    {
        double square = x * x;
        double sum = 0d;
        for (int n = MaclaurinCoefficients.Length - 1; n >= 0; n--)
        {
            sum = (sum * square) + MaclaurinCoefficients[n];
        }

        return x * sum;
    }

    /// <summary>erfc(x) for x ≥ 1, or NaN.</summary>
    /// <remarks>e^(−x²) underflows to 0 from x = 27.3, which makes the result 0 up to +∞, where it would be 0·∞.</remarks>
    private static double UpperTail(double x)
    {
        return double.IsPositiveInfinity(x) ? 0d : ExpOfMinusSquare(x) * x * ContinuedFraction(x * x) / SqrtPi;
    }

    /// <summary>
    /// K(z) = 1 / (z + ½ − (1·½) / (z + 2½ − (2·1½) / (z + 4½ − …))), so that Γ(½, z) = e^(−z)·√z·K(z).
    /// </summary>
    /// <remarks>Cut at depth 120, which is converged from z = 1 up (measured); the part beyond is taken as infinite.</remarks>
    private static double ContinuedFraction(double z)
    {
        double tail = double.PositiveInfinity;
        for (int k = Depth; k >= 1; k--)
        {
            tail = z + ((2 * (k - 1)) + 0.5d) - (k * (k - 0.5d) / tail);
        }

        return 1d / tail;
    }

    private static double ExpOfMinusSquare(double x)
    {
        double sixteenths = Math.Truncate(x * 16d) / 16d;
        return Math.Exp(-sixteenths * sixteenths) * Math.Exp(-(x - sixteenths) * (x + sixteenths));
    }

    /// <summary>ln erfc(x) and its derivative, for x ≥ 0.</summary>
    private static (double Logarithm, double Slope) LogErfc(double x)
    {
        if (x >= 1d)
        {
            // ln(e^(−x²)·x·K/√π), which stays finite where erfc itself underflows.
            double fraction = ContinuedFraction(x * x);
            return ((-x * x) + Math.Log(x * fraction / SqrtPi), -2d / (x * fraction));
        }

        double complement = 1d - Maclaurin(x);
        return (Math.Log(complement), -MaclaurinCoefficients[0] * ExpOfMinusSquare(x) / complement);
    }
}
