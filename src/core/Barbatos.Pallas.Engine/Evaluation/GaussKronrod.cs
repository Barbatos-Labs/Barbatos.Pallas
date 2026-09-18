// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Adaptive Gauss–Kronrod 7/15 quadrature on <see cref="double"/>, the method the reference calculator names for ∫ (p. 52).
/// </summary>
/// <remarks>
/// <para>
/// .NET has no numerical integration. The 15-point Kronrod rule integrates polynomials of degree 22 exactly and the
/// embedded 7-point Gauss rule those of degree 13; their difference estimates the error of each interval. The interval
/// with the largest estimate is bisected until the total estimate meets the target, relative to ∫|f| so that an integral
/// of 0 (an odd function over a symmetric interval) has a meaningful target too.
/// </para>
/// <para>
/// Nodes and weights are those of QUADPACK's QK15 (Piessens et al., 1983). The integrand is never evaluated at the end
/// points, so <c>∫(ln(x),0,1)</c> is computed.
/// </para>
/// </remarks>
internal static class GaussKronrod
{
    /// <summary>The most bisections of one integral.</summary>
    public const int MaxSubdivisions = 500;

    private static readonly double[] KronrodNodes =
    [
        0.991455371120812639206854697526329,
        0.949107912342758524526189684047851,
        0.864864423359769072789712788640926,
        0.741531185599394439863864773280788,
        0.586087235467691130294144845693013,
        0.405845151377397166906606412076961,
        0.207784955007898467600689403773245,
        0d,
    ];

    private static readonly double[] KronrodWeights =
    [
        0.022935322010529224963732008058970,
        0.063092092629978553290700663189204,
        0.104790010322250183839876322541518,
        0.140653259715525918745189590510238,
        0.169004726639267902826583426598550,
        0.190350578064785409913256402421014,
        0.204432940075298892414161999234649,
        0.209482141084727828012999174891714,
    ];

    // The Gauss weights of the nodes KronrodNodes[1], [3], [5] and [7].
    private static readonly double[] GaussWeights =
    [
        0.129484966168869693270611432679082,
        0.279705391489276667901467771423780,
        0.381830050505118944950369775488975,
        0.417959183673469387755102040816327,
    ];

    /// <summary>The integrand: returns <see langword="false"/> with an error when it cannot be evaluated at a point.</summary>
    public delegate bool Integrand(double x, out double value, out CalcErrorKind error);

    /// <summary>Integrates <paramref name="integrand"/> from <paramref name="a"/> to <paramref name="b"/>.</summary>
    /// <param name="integrand">The function.</param>
    /// <param name="a">The lower bound.</param>
    /// <param name="b">The upper bound; may be below <paramref name="a"/>.</param>
    /// <param name="target">The relative error to refine to.</param>
    /// <param name="acceptable">The relative error still accepted when refinement stops.</param>
    /// <param name="context">The budget: each of the 15 evaluations counts as an iteration.</param>
    /// <returns>The integral and its error estimate, or the error that stopped it.</returns>
    public static (double Result, double ErrorEstimate, CalcErrorKind? Error) Integrate(
        Integrand integrand,
        double a,
        double b,
        double target,
        double acceptable,
        EvaluationContext context)
    {
        if (a == b)
        {
            return (0d, 0d, null);
        }

        List<Interval> intervals = [];
        if (!TryRule(integrand, a, b, context, out Interval first, out CalcErrorKind? error))
        {
            return (0d, 0d, error);
        }

        intervals.Add(first);
        for (int subdivision = 0; subdivision < MaxSubdivisions && Total(intervals, interval => interval.Error) > target * Total(intervals, interval => interval.Magnitude); subdivision++)
        {
            int worst = 0;
            for (int i = 1; i < intervals.Count; i++)
            {
                if (intervals[i].Error > intervals[worst].Error)
                {
                    worst = i;
                }
            }

            Interval interval = intervals[worst];
            double middle = interval.Lower + ((interval.Upper - interval.Lower) / 2d);

            // Rounding has made the interval as small as it can be: further bisection cannot help.
            if (middle <= Math.Min(interval.Lower, interval.Upper) || middle >= Math.Max(interval.Lower, interval.Upper))
            {
                break;
            }

            if (!TryRule(integrand, interval.Lower, middle, context, out Interval left, out error)
                || !TryRule(integrand, middle, interval.Upper, context, out Interval right, out error))
            {
                return (0d, 0d, error);
            }

            intervals[worst] = left;
            intervals.Add(right);
        }

        double result = Total(intervals, interval => interval.Result);
        double estimate = Total(intervals, interval => interval.Error);
        double magnitude = Total(intervals, interval => interval.Magnitude);
        return estimate <= acceptable * magnitude
            ? (result, estimate, null)
            : (result, estimate, CalcErrorKind.TimeOut);
    }

    private static bool TryRule(Integrand integrand, double lower, double upper, EvaluationContext context, out Interval interval, out CalcErrorKind? error)
    {
        interval = default;
        error = null;
        if (!context.TryIterate(15))
        {
            error = CalcErrorKind.TimeOut;
            return false;
        }

        double center = (lower + upper) / 2d;
        double halfLength = (upper - lower) / 2d;
        double kronrod = 0d;
        double gauss = 0d;
        double magnitude = 0d;
        for (int i = 0; i < KronrodNodes.Length; i++)
        {
            double offset = halfLength * KronrodNodes[i];
            if (!integrand(center - offset, out double low, out CalcErrorKind lowError))
            {
                error = lowError;
                return false;
            }

            double sum = low;
            double absolute = Math.Abs(low);
            if (i < KronrodNodes.Length - 1)
            {
                if (!integrand(center + offset, out double high, out CalcErrorKind highError))
                {
                    error = highError;
                    return false;
                }

                sum += high;
                absolute += Math.Abs(high);
            }

            kronrod += KronrodWeights[i] * sum;
            magnitude += KronrodWeights[i] * absolute;
            if (i % 2 == 1)
            {
                gauss += GaussWeights[i / 2] * sum;
            }
        }

        double scale = Math.Abs(halfLength);
        interval = new Interval(lower, upper, kronrod * halfLength, Math.Abs((kronrod - gauss) * halfLength), magnitude * scale);
        return double.IsFinite(interval.Result) || SetMathError(out error);
    }

    private static double Total(List<Interval> intervals, Func<Interval, double> part)
    {
        double sum = 0d;
        foreach (Interval interval in intervals)
        {
            sum += part(interval);
        }

        return sum;
    }

    private static bool SetMathError(out CalcErrorKind? error)
    {
        error = CalcErrorKind.MathError;
        return false;
    }

    private readonly record struct Interval(double Lower, double Upper, double Result, double Error, double Magnitude);
}
