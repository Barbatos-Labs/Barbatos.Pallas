// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Statistics;
using Fraction = (System.Numerics.BigInteger Numerator, System.Numerics.BigInteger Denominator);

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The statistic variables of the Statistics application, calculated from its data (manual pp. 83-95).
/// </summary>
/// <remarks>
/// <para>
/// Sums, means, variances and the coefficients of the linear and quadratic regressions are exact
/// (<see cref="ExactSample"/>) and rounded once; σ, s and r are square roots of exact values. A value held as
/// <see cref="double"/> enters at its shortest round-trip digits (<see cref="ScaledValues"/>). The other regressions fit a line to ln x, ln y or 1/x (pp. 94-95), whose values follow the precision rule,
/// and recover a and b through exp.
/// </para>
/// <para>
/// A negative frequency makes every statistic a Math ERROR, a row with frequency 0 is left out, and the quartiles need
/// whole frequencies (assumption U22); the quartiles themselves follow assumption U1 (<see cref="Quartiles"/>).
/// </para>
/// </remarks>
internal sealed class StatisticsCalculator
{
    /// <summary>The most rows of the <see cref="CalculatorProfile.Extended"/> profile.</summary>
    public const int ExtendedRowLimit = 10_000;

    private readonly StatisticsData _data;
    private readonly RegressionModel _model;
    private readonly bool _negativeFrequency;
    private readonly int[] _rows;
    private readonly bool _exact;
    private ExactSample? _sample;
    private (ExactSample? Sample, bool Exact)? _fit;

    public StatisticsCalculator(StatisticsData data, RegressionModel model)
    {
        _data = data;
        _model = model;
        _negativeFrequency = data.Frequencies.Any(frequency => frequency.ToDouble() < 0d);
        _rows = [.. Enumerable.Range(0, data.Rows).Where(row => !data.HasFrequencies || !data.Frequencies[row].IsZero)];
        _exact = _rows.All(row => data.X[row].IsExact && (!data.IsTwoVariable || data.Y[row].IsExact) && (!data.HasFrequencies || data.Frequencies[row].IsExact));
    }

    private ExactSample Sample => _sample ??= Create(Column(_data.X), _data.IsTwoVariable ? Column(_data.Y) : [], Frequencies());

    /// <summary>The most rows the editor holds: 160, 80 or 53 for one, two or three columns (p. 80).</summary>
    public static int RowLimit(CalculatorProfile profile, int columns)
    {
        return profile == CalculatorProfile.Standard
            ? columns switch
            {
                1 => 160,
                2 => 80,
                _ => 53,
            }
            : ExtendedRowLimit;
    }

    public EvalResult Evaluate(Statistic statistic, EvaluationContext context)
    {
        if (_negativeFrequency)
        {
            return ValueMath.MathError;
        }

        return statistic switch
        {
            Statistic.Count => Ratio(Sample.Count, _exact, context),
            Statistic.SumX => Ratio(Sample.Sum(1, 0), _exact, context),
            Statistic.SumX2 => Ratio(Sample.Sum(2, 0), _exact, context),
            Statistic.SumX3 => Ratio(Sample.Sum(3, 0), _exact, context),
            Statistic.SumX4 => Ratio(Sample.Sum(4, 0), _exact, context),
            Statistic.SumY => Ratio(Sample.Sum(0, 1), _exact, context),
            Statistic.SumY2 => Ratio(Sample.Sum(0, 2), _exact, context),
            Statistic.SumXY => Ratio(Sample.Sum(1, 1), _exact, context),
            Statistic.SumX2Y => Ratio(Sample.Sum(2, 1), _exact, context),
            Statistic.MeanX => Sample.TryGetMean(SampleVariable.X, out Fraction meanX) ? Ratio(meanX, _exact, context) : ValueMath.MathError,
            Statistic.MeanY => Sample.TryGetMean(SampleVariable.Y, out Fraction meanY) ? Ratio(meanY, _exact, context) : ValueMath.MathError,
            Statistic.PopulationVarianceX => PopulationVariance(SampleVariable.X, context),
            Statistic.PopulationVarianceY => PopulationVariance(SampleVariable.Y, context),
            Statistic.PopulationDeviationX => Root(PopulationVariance(SampleVariable.X, context), context),
            Statistic.PopulationDeviationY => Root(PopulationVariance(SampleVariable.Y, context), context),
            Statistic.SampleVarianceX => SampleVariance(SampleVariable.X, context),
            Statistic.SampleVarianceY => SampleVariance(SampleVariable.Y, context),
            Statistic.SampleDeviationX => Root(SampleVariance(SampleVariable.X, context), context),
            Statistic.SampleDeviationY => Root(SampleVariance(SampleVariable.Y, context), context),
            Statistic.MinX => Extreme(_data.X, largest: false),
            Statistic.MaxX => Extreme(_data.X, largest: true),
            Statistic.MinY => Extreme(_data.Y, largest: false),
            Statistic.MaxY => Extreme(_data.Y, largest: true),
            Statistic.FirstQuartile => QuartileValue(Quartile.First, context),
            Statistic.Median => QuartileValue(Quartile.Median, context),
            Statistic.ThirdQuartile => QuartileValue(Quartile.Third, context),
            _ => Regression(statistic, context),
        };
    }

    private static EvalResult Ratio(Fraction ratio, bool exact, EvaluationContext context)
    {
        return ValueMath.FromRatio(ratio.Numerator, ratio.Denominator, exact, context);
    }

    private static EvalResult Root(EvalResult variance, EvaluationContext context)
    {
        return variance.Succeeded ? ValueMath.SquareRoot(variance.Value, context) : variance;
    }

    private static ExactSample Create(IReadOnlyList<Value> x, IReadOnlyList<Value> y, IReadOnlyList<Value> frequencies)
    {
        (BigInteger[] xIntegers, int xScale) = ScaledValues.Scaled(x);
        (BigInteger[] yIntegers, int yScale) = ScaledValues.Scaled(y);
        (BigInteger[] frequencyIntegers, int frequencyScale) = ScaledValues.Scaled(frequencies);
        return ExactSample.FromScaledIntegers(xIntegers, xScale, yIntegers, yScale, frequencyIntegers, frequencyScale);
    }

    private Value[] Column(IReadOnlyList<Value> values) => [.. _rows.Select(row => values[row])];

    private Value[] Frequencies() => _data.HasFrequencies ? Column(_data.Frequencies) : [];

    private EvalResult PopulationVariance(SampleVariable variable, EvaluationContext context)
    {
        return Sample.TryGetPopulationVariance(variable, out Fraction variance) ? Ratio(variance, _exact, context) : ValueMath.MathError;
    }

    private EvalResult SampleVariance(SampleVariable variable, EvaluationContext context)
    {
        return Sample.TryGetSampleVariance(variable, out Fraction variance) ? Ratio(variance, _exact, context) : ValueMath.MathError;
    }

    private EvalResult Extreme(IReadOnlyList<Value> values, bool largest)
    {
        if (_rows.Length == 0)
        {
            return ValueMath.MathError;
        }

        Value[] column = Column(values);
        return largest ? column.Max(RealOrder.Instance) : column.Min(RealOrder.Instance);
    }

    private EvalResult QuartileValue(Quartile quartile, EvaluationContext context)
    {
        // The quartiles count values, so a frequency must be a whole number (assumption U22).
        BigInteger[] counts = new BigInteger[_rows.Length];
        for (int i = 0; i < counts.Length; i++)
        {
            if (!_data.HasFrequencies)
            {
                counts[i] = BigInteger.One;
            }
            else if (!ScaledValues.TryGetWhole(_data.Frequencies[_rows[i]], out counts[i]))
            {
                return ValueMath.MathError;
            }
        }

        BigInteger total = counts.Aggregate(BigInteger.Zero, (sum, count) => sum + count);
        if (total.IsZero)
        {
            return ValueMath.MathError;
        }

        int[] sorted = [.. Enumerable.Range(0, _rows.Length).OrderBy(i => _data.X[_rows[i]], RealOrder.Instance)];
        (BigInteger lower, BigInteger upper) = Quartiles.Ranks(total, quartile);
        Value low = ValueAtRank(sorted, counts, lower);
        Value high = ValueAtRank(sorted, counts, upper);

        // (low + high)/2 exactly: the sum itself could leave the range, as 9×10⁹⁹ + 9.5×10⁹⁹ does.
        (BigInteger[] ends, int scale) = ScaledValues.Scaled([low, high]);
        return ValueMath.FromRatio(ends[0] + ends[1], 2 * BigInteger.Pow(10, scale), low.IsExact && high.IsExact, context);
    }

    private Value ValueAtRank(int[] sorted, BigInteger[] counts, BigInteger rank)
    {
        // Every count is 1 or more, and the rank is at most their total.
        int index = 0;
        for (BigInteger seen = counts[sorted[0]]; seen < rank; seen += counts[sorted[index]])
        {
            index++;
        }

        return _data.X[_rows[sorted[index]]];
    }

    private EvalResult Regression(Statistic statistic, EvaluationContext context)
    {
        (ExactSample? fit, bool exact) = _fit ??= Fit(context);
        if (fit is null)
        {
            return ValueMath.MathError;
        }

        if (_model == RegressionModel.Quadratic)
        {
            // The binder offers c only here, and r only for the other models.
            return !fit.TryGetQuadraticFit(out Fraction a, out Fraction b, out Fraction c)
                ? ValueMath.MathError
                : Ratio(statistic switch
                {
                    Statistic.A => a,
                    Statistic.B => b,
                    _ => c,
                }, exact, context);
        }

        if (statistic == Statistic.R)
        {
            if (!fit.TryGetCorrelationSquared(out Fraction square, out int sign))
            {
                return ValueMath.MathError;
            }

            // r has the sign of Sxy; 0 when Sxy is.
            EvalResult r = Root(Ratio(square, exact, context), context);
            return r.Succeeded ? ValueMath.Multiply(Value.FromDecimal(sign), r.Value, context) : r;
        }

        if (!fit.TryGetLinearFit(out Fraction intercept, out Fraction slope))
        {
            return ValueMath.MathError;
        }

        // y = a·e^(bx), a·b^x and a·x^b are lines in ln y, whose intercept is ln a; a·b^x's slope is ln b too (p. 94).
        bool exponentialIntercept = _model is RegressionModel.ExponentialE or RegressionModel.ExponentialAB or RegressionModel.Power;
        bool exponentialSlope = _model == RegressionModel.ExponentialAB;
        return statistic == Statistic.A
            ? Exponential(Ratio(intercept, exact, context), exponentialIntercept, context)
            : Exponential(Ratio(slope, exact, context), exponentialSlope, context);
    }

    private static EvalResult Exponential(EvalResult value, bool apply, EvaluationContext context)
    {
        return apply && value.Succeeded ? Operations.Evaluate(Operation.Exp, [value.Value], context) : value;
    }

    /// <summary>
    /// The sample the regression model fits a line or a parabola to: ln x, ln y or 1/x where the model says (p. 86); none
    /// when a value has no logarithm or reciprocal, which is a Math ERROR.
    /// </summary>
    private (ExactSample? Sample, bool Exact) Fit(EvaluationContext context)
    {
        bool logX = _model is RegressionModel.Logarithmic or RegressionModel.Power;
        bool logY = _model is RegressionModel.ExponentialE or RegressionModel.ExponentialAB or RegressionModel.Power;
        Value[] x = Column(_data.X);
        Value[] y = Column(_data.Y);
        for (int i = 0; i < x.Length; i++)
        {
            EvalResult tx = logX ? Operations.Evaluate(Operation.Ln, [x[i]], context)
                : _model == RegressionModel.Inverse ? ValueMath.Divide(Value.One, x[i], context)
                : x[i];
            EvalResult ty = logY ? Operations.Evaluate(Operation.Ln, [y[i]], context) : y[i];
            if (!tx.Succeeded || !ty.Succeeded)
            {
                return default;
            }

            x[i] = tx.Value;
            y[i] = ty.Value;
        }

        // A logarithm makes a value approximate; a reciprocal of an exact decimal is exact, rounded at its 28th digit.
        return (Create(x, y, Frequencies()), _exact && !logX && !logY);
    }
}
