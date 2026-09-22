// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The calculations of the Distribution application (manual pp. 95-100).
/// </summary>
/// <remarks>
/// <para>
/// A binomial probability of a decimal p = m/10^s is a fraction, Σ C(N, k)·m^k·(10^s − m)^(N−k) / 10^(sN), summed on
/// <see cref="BigInteger"/> and rounded once. Its size grows with N·s, so the work counts against the budget, estimated
/// before it starts; a budget that runs out is Time Out, which the calculator also reports for Distribution (p. 165).
/// </para>
/// <para>
/// The normal distribution is <see cref="ErrorFunction"/>. Normal CD subtracts the two tails on the side where they are
/// small, so that an interval far out in a tail keeps its digits. The Poisson probability is Loader's saddle-point form
/// (<see cref="PoissonDistribution"/>); Poisson CD adds the terms from x towards the side where they vanish, and takes the
/// complement above the mean.
/// </para>
/// <para>
/// A parameter outside its domain is a Math ERROR: x and N whole with 0 ≤ x ≤ N, 0 ≤ p ≤ 1, σ &gt; 0, λ &gt; 0, a whole
/// x ≥ 0 for Poisson, Lower ≤ Upper, and 0 &lt; Area &lt; 1 (0 and 1 give infinite x). The manual states only p, σ and Area
/// (p. 98); the rest is assumption U24.
/// </para>
/// </remarks>
internal static class DistributionMath
{
    private const double SqrtTwoPi = 2.5066282746310002;

    public static EvalResult Evaluate(DistributionKind kind, DistributionParameters parameters, Value x, EvaluationContext context)
    {
        return kind switch
        {
            DistributionKind.BinomialPD or DistributionKind.BinomialCD => Binomial(x, parameters.Trials, parameters.Probability, kind == DistributionKind.BinomialCD, context),
            DistributionKind.NormalPD => NormalDensity(x, parameters.Mean, parameters.StandardDeviation, context),
            DistributionKind.NormalCD => NormalInterval(parameters.Lower, parameters.Upper, parameters.Mean, parameters.StandardDeviation, context),
            DistributionKind.InverseNormal => InverseNormal(parameters.Area, parameters.Mean, parameters.StandardDeviation, context),
            _ => Poisson(x, parameters.Lambda, kind == DistributionKind.PoissonCD, context),
        };
    }

    private static EvalResult Binomial(Value x, Value trials, Value probability, bool cumulative, EvaluationContext context)
    {
        // p = m/D with D = 10^s, compared exactly: as a double, 1 + 10⁻²⁸ would pass for 1.
        (BigInteger[] integers, int scale) = ScaledValues.Scaled([probability]);
        BigInteger m = integers[0];
        BigInteger d = BigInteger.Pow(10, scale);
        if (!ScaledValues.TryGetWhole(x, out BigInteger successes) || !ScaledValues.TryGetWhole(trials, out BigInteger n)
            || successes.Sign < 0 || successes > n || m.Sign < 0 || m > d)
        {
            return ValueMath.MathError;
        }

        // The fraction has about N·(s + 1) digits, and the work grows as its square. Beyond 10⁹ digits nothing finishes, and
        // the estimate would overflow the budget's count; below, N fits an int.
        BigInteger q = d - m;
        double digits = (double)n * (scale + 1);
        if (digits > 1e9 || !context.TryIterate((long)(digits * digits / 100d)))
        {
            return EvalResult.Failure(CalcErrorKind.TimeOut);
        }

        // PD: C(N, x)·m^x·q^(N−x). CD: q^(N−x)·Σ C(N, k)·m^k·q^(x−k), by Horner, with C(N, k)·m^k built term by term.
        int top = (int)successes;
        int count = (int)n;
        BigInteger term = BigInteger.One;
        BigInteger sum = BigInteger.Zero;
        for (int k = 0; k <= top; k++)
        {
            if (!context.TryIterate())
            {
                return EvalResult.Failure(CalcErrorKind.TimeOut);
            }

            sum = cumulative ? (sum * q) + term : term;
            term = term * m * (count - k) / (k + 1);
        }

        BigInteger numerator = sum * BigInteger.Pow(q, count - top);
        bool exact = x.IsExact && trials.IsExact && probability.IsExact;
        return ValueMath.FromRatio(numerator, BigInteger.Pow(d, count), exact, context);
    }

    private static EvalResult NormalDensity(Value x, Value mean, Value deviation, EvaluationContext context)
    {
        if (!TryStandardize(x, mean, deviation, context, out EvalResult z))
        {
            return z;
        }

        // e^(−z²/2) with z²/2 in decimal, split into a whole part and a fraction, so that neither carries the rounding of the
        // other: exp of a rounded exponent w is off by w·2⁻⁵³. A z held as double is below 10⁻¹⁴, where the exponent is 0
        // for double, or beyond 7.9×10²⁸, where the density is 0; so is it from |z| = 40 on, where e^(−800) underflows.
        double standardized = z.Value.ToDouble();
        double exponent = Math.Exp(-standardized * standardized / 2d);
        if (z.Value.Kind == ValueKind.DecimalReal && Math.Abs(standardized) < 40d)
        {
            decimal w = z.Value.ToDecimal() * z.Value.ToDecimal() / 2m;
            decimal whole = decimal.Truncate(w);
            exponent = Math.Exp(-(double)whole) * Math.Exp(-(double)(w - whole));
        }

        return ValueMath.Real(exponent / (deviation.ToDouble() * SqrtTwoPi), null, context);
    }

    private static EvalResult NormalInterval(Value lower, Value upper, Value mean, Value deviation, EvaluationContext context)
    {
        if (!TryStandardize(lower, mean, deviation, context, out EvalResult low))
        {
            return low;
        }

        if (!TryStandardize(upper, mean, deviation, context, out EvalResult high))
        {
            return high;
        }

        double a = low.Value.ToDouble() / Math.Sqrt(2d);
        double b = high.Value.ToDouble() / Math.Sqrt(2d);
        if (a > b)
        {
            return ValueMath.MathError;
        }

        // Φ(b) − Φ(a) from the tails that are small on that side: erfc(a) − erfc(b) from 0 up, erfc(−b) − erfc(−a) below,
        // which also serves an interval across 0, where erfc(−b) is at least 1 and nothing cancels.
        double probability = a >= 0d ? (ErrorFunction.Erfc(a) - ErrorFunction.Erfc(b)) / 2d : (ErrorFunction.Erfc(-b) - ErrorFunction.Erfc(-a)) / 2d;
        return ValueMath.Real(probability, null, context);
    }

    private static EvalResult InverseNormal(Value area, Value mean, Value deviation, EvaluationContext context)
    {
        if (!(deviation.ToDouble() > 0d))
        {
            return ValueMath.MathError;
        }

        // Φ⁻¹(a) = −√2·erfc⁻¹(2a): infinite at 0 and 1 and NaN outside [0, 1], both of which Real makes a Math ERROR.
        EvalResult t = ValueMath.Real(-Math.Sqrt(2d) * ErrorFunction.InverseErfc(2d * area.ToDouble()), null, context);
        EvalResult scaled = t.Succeeded ? ValueMath.Multiply(deviation, t.Value, context) : t;
        return scaled.Succeeded ? ValueMath.Add(mean, scaled.Value, context) : scaled;
    }

    private static EvalResult Poisson(Value x, Value lambda, bool cumulative, EvaluationContext context)
    {
        double mean = lambda.ToDouble();
        if (!ScaledValues.TryGetWhole(x, out BigInteger whole) || whole.Sign < 0 || !(mean > 0d))
        {
            return ValueMath.MathError;
        }

        double k = (double)whole;
        if (!cumulative)
        {
            return ValueMath.Real(PoissonDistribution.Probability(k, mean), null, context);
        }

        // At or below the mean the terms shrink towards 0; above it, the terms above x shrink and P = 1 − their sum. The
        // terms left out after term t at k are at most t·λ/|λ − k| (a geometric bound), kept below 10⁻¹⁷ of the sum.
        bool below = k <= mean;
        double sum = 0d;
        for (double j = below ? k : k + 1d; j >= 0d; j += below ? -1d : 1d)
        {
            if (!context.TryIterate())
            {
                return EvalResult.Failure(CalcErrorKind.TimeOut);
            }

            double term = PoissonDistribution.Probability(j, mean);
            sum += term;
            if (term * mean <= Math.Abs(mean - j) * sum * 1e-17)
            {
                break;
            }
        }

        return ValueMath.Real(below ? sum : 1d - sum, null, context);
    }

    /// <summary>z = (x − μ)/σ; a Math ERROR unless σ &gt; 0.</summary>
    private static bool TryStandardize(Value x, Value mean, Value deviation, EvaluationContext context, out EvalResult z)
    {
        if (!(deviation.ToDouble() > 0d))
        {
            z = ValueMath.MathError;
            return false;
        }

        EvalResult difference = ValueMath.Subtract(x, mean, context);
        z = difference.Succeeded ? ValueMath.Divide(difference.Value, deviation, context) : difference;
        return z.Succeeded;
    }
}
