// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using CsCheck;
using Ratio = (System.Numerics.BigInteger Numerator, System.Numerics.BigInteger Denominator);

namespace Barbatos.Pallas.Statistics.Tests;

/// <summary>
/// Exact sums, variances and fits, checked against known fractions and by cross-multiplying with the data.
/// </summary>
public sealed class ExactSampleTests
{
    // p. 83: x = 1-10 with frequencies 1, 2, 1, 2, 2, 2, 3, 4, 2, 1.
    private static readonly decimal[] ManualX = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly decimal[] ManualFrequencies = [1, 2, 1, 2, 2, 2, 3, 4, 2, 1];

    // p. 84: ten (x, y) pairs.
    private static readonly decimal[] PairX = [1.0m, 1.2m, 1.5m, 1.6m, 1.9m, 2.1m, 2.4m, 2.5m, 2.7m, 3.0m];
    private static readonly decimal[] PairY = [1.0m, 1.1m, 1.2m, 1.3m, 1.4m, 1.5m, 1.6m, 1.7m, 1.8m, 2.0m];

    // Values with two decimals, so 100·v is an integer the tests check exactly, and small integer frequencies.
    private static readonly Gen<decimal> Entry = Gen.Int[-9999, 9999].Select(hundredths => hundredths / 100m);

    private static readonly Gen<(decimal[] X, decimal[] Y, decimal[] F)> Data =
        Gen.Int[1, 12].SelectMany(rows => Gen.Select(Entry.Array[rows], Entry.Array[rows], Gen.Int[0, 4].Array[rows]))
            .Select((x, y, f) => (x, y, f.Select(value => (decimal)value).ToArray()));

    [Fact]
    public void TheOneVariableDataOfTheManual()
    {
        ExactSample sample = ExactSample.FromDecimals(ManualX, [], ManualFrequencies);

        sample.HasY.Should().BeFalse();
        sample.Count.Should().Be((20, 1));
        sample.Sum(1, 0).Should().Be((119, 1));
        sample.Sum(2, 0).Should().Be((837, 1));
        sample.TryGetMean(SampleVariable.X, out Ratio mean).Should().BeTrue();
        mean.Should().Be((119, 20));
        sample.TryGetPopulationVariance(SampleVariable.X, out Ratio population).Should().BeTrue();
        population.Should().Be((2579, 400));
        sample.TryGetSampleVariance(SampleVariable.X, out Ratio variance).Should().BeTrue();
        variance.Should().Be((2579, 380));
    }

    [Fact]
    public void TheTwoVariableDataOfTheManual()
    {
        ExactSample sample = ExactSample.FromDecimals(PairX, PairY, []);

        sample.Sum(1, 0).Should().Be((199, 10));
        sample.Sum(0, 1).Should().Be((73, 5));
        sample.Sum(2, 0).Should().Be((4357, 100));
        sample.Sum(0, 2).Should().Be((556, 25));
        sample.Sum(1, 1).Should().Be((774, 25));
        sample.Sum(3, 0).Should().Be((102451, 1000));
        sample.Sum(2, 1).Should().Be((17811, 250));
        sample.Sum(4, 0).Should().Be((2535541, 10000));
        sample.TryGetMean(SampleVariable.Y, out Ratio mean).Should().BeTrue();
        mean.Should().Be((73, 50));

        // p. 86: a = 10009/19845, b = 1906/3969, r² = 908209/916839.
        sample.TryGetLinearFit(out Ratio intercept, out Ratio slope).Should().BeTrue();
        intercept.Should().Be((10009, 19845));
        slope.Should().Be((1906, 3969));
        sample.TryGetCorrelationSquared(out Ratio square, out int sign).Should().BeTrue();
        square.Should().Be((908209, 916839));
        sign.Should().Be(1);

        // p. 86: a = 0.7028598638, b = 0.2576384379, c = 0.05610274153.
        sample.TryGetQuadraticFit(out Ratio a, out Ratio b, out Ratio c).Should().BeTrue();
        Rounded(a).Should().Be(0.7028598638m);
        Rounded(b).Should().Be(0.2576384379m);
        Rounded(c).Should().Be(0.05610274153m);
    }

    [Fact]
    public void ADecreasingLineHasANegativeCorrelation()
    {
        ExactSample sample = ExactSample.FromDecimals([1, 2, 3], [3, 2, 0], []);

        sample.TryGetCorrelationSquared(out Ratio square, out int sign).Should().BeTrue();
        sign.Should().Be(-1);
        square.Should().Be((27, 28));
    }

    [Fact]
    public void EveryResultIsInLowestTermsWithAPositiveDenominator()
    {
        Data.Sample(data =>
        {
            ExactSample sample = ExactSample.FromDecimals(data.X, data.Y, data.F);
            List<Ratio> results = [sample.Count, sample.Sum(1, 0), sample.Sum(1, 1), sample.Sum(0, 2)];
            if (sample.TryGetMean(SampleVariable.Y, out Ratio mean))
            {
                results.Add(mean);
            }

            if (sample.TryGetLinearFit(out Ratio intercept, out Ratio slope))
            {
                results.Add(intercept);
                results.Add(slope);
            }

            return results.TrueForAll(result => result.Denominator.Sign > 0 && BigInteger.GreatestCommonDivisor(result.Numerator, result.Denominator).IsOne);
        });
    }

    [Fact]
    public void VariancesAreTheTwoPassDefinition()
    {
        // σ² = Σf(v − v̄)²/n and s² = Σf(v − v̄)²/(n − 1), for x and for y, computed here on 100·v.
        Data.Sample(data =>
        {
            ExactSample sample = ExactSample.FromDecimals(data.X, data.Y, data.F);
            return Matches(sample, SampleVariable.X, data.X, data.F) && Matches(sample, SampleVariable.Y, data.Y, data.F);
        });

        static bool Matches(ExactSample sample, SampleVariable variable, decimal[] values, decimal[] frequencies)
        {
            BigInteger[] v = Integers(values, 100);
            BigInteger[] f = Integers(frequencies, 1);
            BigInteger n = f.Aggregate(BigInteger.Zero, (sum, frequency) => sum + frequency);
            BigInteger sum = Enumerable.Range(0, v.Length).Aggregate(BigInteger.Zero, (total, i) => total + (f[i] * v[i]));

            // Σf(n·V − ΣfV)² = 100²·n²·Σf(v − v̄)².
            BigInteger deviations = Enumerable.Range(0, v.Length).Aggregate(BigInteger.Zero, (total, i) => total + (f[i] * BigInteger.Pow((n * v[i]) - sum, 2)));
            bool population = sample.TryGetPopulationVariance(variable, out Ratio populationVariance)
                ? populationVariance.Numerator * n * n * n * 10000 == deviations * populationVariance.Denominator
                : n.IsZero;
            bool sampleVariance = sample.TryGetSampleVariance(variable, out Ratio variance)
                ? variance.Numerator * n * n * (n - 1) * 10000 == deviations * variance.Denominator
                : n <= 1;
            return population && sampleVariance;
        }
    }

    [Fact]
    public void FrequenciesWeighY()
    {
        ExactSample sample = ExactSample.FromDecimals([1, 2], [3, 4], [2, 1]);

        sample.Sum(0, 1).Should().Be((10, 1));
        sample.Sum(0, 2).Should().Be((34, 1));
        sample.Sum(1, 1).Should().Be((14, 1));
    }

    [Fact]
    public void TheLinearFitSatisfiesItsNormalEquations()
    {
        Data.Sample(data =>
        {
            ExactSample sample = ExactSample.FromDecimals(data.X, data.Y, data.F);
            if (!sample.TryGetLinearFit(out Ratio a, out Ratio b))
            {
                return Distinct(data.X, data.F) < 2;
            }

            // Σf·xᵏ·(y − a − bx) = 0 for k = 0, 1, multiplied by 100·a.D·b.D on 100·x and 100·y.
            BigInteger[] x = Integers(data.X, 100);
            BigInteger[] y = Integers(data.Y, 100);
            BigInteger[] f = Integers(data.F, 1);
            return Enumerable.Range(0, 2).All(k => Enumerable.Range(0, x.Length).Aggregate(BigInteger.Zero, (sum, i) =>
                sum + (f[i] * BigInteger.Pow(x[i], k) * ((y[i] * a.Denominator * b.Denominator) - (100 * a.Numerator * b.Denominator) - (b.Numerator * a.Denominator * x[i])))).IsZero);
        });
    }

    [Fact]
    public void TheQuadraticFitSatisfiesItsNormalEquations()
    {
        Data.Sample(data =>
        {
            ExactSample sample = ExactSample.FromDecimals(data.X, data.Y, data.F);
            if (!sample.TryGetQuadraticFit(out Ratio a, out Ratio b, out Ratio c))
            {
                return Distinct(data.X, data.F) < 3;
            }

            // Σf·xᵏ·(y − a − bx − cx²) = 0 for k = 0, 1, 2, multiplied by 100²·a.D·b.D·c.D on 100·x and 100·y.
            BigInteger[] x = Integers(data.X, 100);
            BigInteger[] y = Integers(data.Y, 100);
            BigInteger[] f = Integers(data.F, 1);
            BigInteger all = a.Denominator * b.Denominator * c.Denominator;
            return Enumerable.Range(0, 3).All(k => Enumerable.Range(0, x.Length).Aggregate(BigInteger.Zero, (sum, i) =>
            {
                BigInteger residual = (100 * y[i] * all)
                    - (10000 * a.Numerator * (all / a.Denominator))
                    - (100 * b.Numerator * (all / b.Denominator) * x[i])
                    - (c.Numerator * (all / c.Denominator) * x[i] * x[i]);
                return sum + (f[i] * BigInteger.Pow(x[i], k) * residual);
            }).IsZero);
        });
    }

    [Fact]
    public void TheCorrelationIsTheSlopeTimesTheSpreadRatio()
    {
        // r² = b²·Sxx/Syy, and r has the sign of b.
        Data.Sample(data =>
        {
            ExactSample sample = ExactSample.FromDecimals(data.X, data.Y, data.F);
            if (!sample.TryGetCorrelationSquared(out Ratio square, out int sign))
            {
                return Distinct(data.X, data.F) < 2 || Distinct(data.Y, data.F) < 2;
            }

            sample.TryGetLinearFit(out _, out Ratio slope).Should().BeTrue();
            sample.TryGetPopulationVariance(SampleVariable.X, out Ratio xVariance).Should().BeTrue();
            sample.TryGetPopulationVariance(SampleVariable.Y, out Ratio yVariance).Should().BeTrue();
            return sign == slope.Numerator.Sign
                && square.Numerator * slope.Denominator * slope.Denominator * xVariance.Denominator * yVariance.Numerator
                    == square.Denominator * slope.Numerator * slope.Numerator * xVariance.Numerator * yVariance.Denominator;
        });
    }

    [Fact]
    public void DegenerateData_HasNoMeanVarianceOrFit()
    {
        ExactSample empty = ExactSample.FromDecimals([], [], []);
        empty.Count.Should().Be((0, 1));
        empty.TryGetMean(SampleVariable.X, out _).Should().BeFalse();
        empty.TryGetPopulationVariance(SampleVariable.X, out _).Should().BeFalse();

        ExactSample one = ExactSample.FromDecimals([5], [2], []);
        one.TryGetPopulationVariance(SampleVariable.Y, out Ratio zero).Should().BeTrue();
        zero.Should().Be((0, 1));
        one.TryGetSampleVariance(SampleVariable.X, out _).Should().BeFalse();

        // A vertical line: every x equal, so Sxx is exactly 0.
        ExactSample vertical = ExactSample.FromDecimals([1.23456789012345678901m, 1.23456789012345678901m], [1, 2], []);
        vertical.TryGetLinearFit(out _, out _).Should().BeFalse();
        vertical.TryGetCorrelationSquared(out _, out _).Should().BeFalse();

        ExactSample flat = ExactSample.FromDecimals([1, 2], [3, 3], []);
        flat.TryGetLinearFit(out Ratio intercept, out Ratio slope).Should().BeTrue();
        intercept.Should().Be((3, 1));
        slope.Should().Be((0, 1));
        flat.TryGetCorrelationSquared(out _, out _).Should().BeFalse();
        flat.TryGetQuadraticFit(out _, out _, out _).Should().BeFalse("two distinct x values cannot fix a parabola");

        // Zero frequencies count for nothing.
        ExactSample unweighted = ExactSample.FromDecimals([1, 2, 3], [], [0, 0, 0]);
        unweighted.TryGetMean(SampleVariable.X, out _).Should().BeFalse();
    }

    [Fact]
    public void FrequenciesMayBeFractions()
    {
        // n = 0.5: n − 1 is negative, and so is the sample variance.
        ExactSample sample = ExactSample.FromDecimals([1, 3], [], [0.25m, 0.25m]);

        sample.Count.Should().Be((1, 2));
        sample.TryGetMean(SampleVariable.X, out Ratio mean).Should().BeTrue();
        mean.Should().Be((2, 1));
        sample.TryGetSampleVariance(SampleVariable.X, out Ratio variance).Should().BeTrue();
        variance.Numerator.Sign.Should().Be(-1);
        variance.Denominator.Sign.Should().Be(1);
    }

    [Fact]
    public void EveryBitOfADecimalIsRead()
    {
        BigInteger largest = BigInteger.Pow(2, 96) - 1;

        ExactSample.FromDecimals([decimal.MaxValue], [], []).Sum(1, 0).Should().Be((largest, 1));
        ExactSample.FromDecimals([decimal.MinValue], [], []).Sum(1, 0).Should().Be((-largest, 1));
        ExactSample.FromDecimals([1099511627776.5m], [], []).Sum(1, 0).Should().Be((new BigInteger(2199023255553), 2));
    }

    [Fact]
    public void ScaledIntegersAreValuesOverPowersOfTen()
    {
        // x = 1.5, 2.5; y = 3, 5; frequencies 0.5, 1.5: Σfxy = 0.75·3 + 3.75·5 = 21.
        ExactSample sample = ExactSample.FromScaledIntegers([15, 25], 1, [3, 5], 0, [5, 15], 1);

        sample.Count.Should().Be((2, 1));
        sample.Sum(1, 1).Should().Be((21, 1));
        sample.TryGetLinearFit(out Ratio intercept, out Ratio slope).Should().BeTrue();
        intercept.Should().Be((0, 1));
        slope.Should().Be((2, 1));
    }

    [Fact]
    public void ArgumentsAreChecked()
    {
        Action shortY = () => ExactSample.FromDecimals([1, 2], [1], []);
        Action shortFrequencies = () => ExactSample.FromDecimals([1, 2], [], [1]);
        Action negativeFrequency = () => ExactSample.FromDecimals([1], [], [-1]);
        Action negativeXScale = () => ExactSample.FromScaledIntegers([1], -1, [], 0, [], 0);
        Action negativeYScale = () => ExactSample.FromScaledIntegers([1], 0, [], -1, [], 0);
        Action negativeFrequencyScale = () => ExactSample.FromScaledIntegers([1], 0, [], 0, [], -1);

        shortY.Should().Throw<ArgumentException>().WithParameterName("y");
        shortFrequencies.Should().Throw<ArgumentException>().WithParameterName("frequencies");
        negativeFrequency.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("frequencies");
        negativeXScale.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("xScale");
        negativeYScale.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("yScale");
        negativeFrequencyScale.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("frequencyScale");
    }

    [Theory]
    [InlineData(-1, 0, "xPower")]
    [InlineData(5, 0, "xPower")]
    [InlineData(3, 1, "xPower")]
    [InlineData(1, 2, "xPower")]
    [InlineData(0, 3, "yPower")]
    [InlineData(0, -1, "yPower")]
    public void OnlyTheCalculatorsSumsExist(int xPower, int yPower, string parameter)
    {
        ExactSample sample = ExactSample.FromDecimals([1], [2], []);

        Action sum = () => sample.Sum(xPower, yPower);

        sum.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(parameter);
    }

    [Fact]
    public void AOneVariableSampleHasNoY()
    {
        ExactSample sample = ExactSample.FromDecimals([1, 2], [], []);

        sample.Invoking(s => s.Sum(0, 1)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("yPower");
        sample.Invoking(s => s.TryGetMean(SampleVariable.Y, out _)).Should().Throw<InvalidOperationException>();
        sample.Invoking(s => s.TryGetLinearFit(out _, out _)).Should().Throw<InvalidOperationException>();
        sample.Invoking(s => s.TryGetQuadraticFit(out _, out _, out _)).Should().Throw<InvalidOperationException>();
        sample.Invoking(s => s.TryGetCorrelationSquared(out _, out _)).Should().Throw<InvalidOperationException>();
    }

    private static decimal Rounded(Ratio value)
    {
        return decimal.Round((decimal)value.Numerator / (decimal)value.Denominator, 10, MidpointRounding.AwayFromZero) is decimal rounded
            && Math.Abs(rounded) < 0.1m
            ? decimal.Round((decimal)value.Numerator / (decimal)value.Denominator, 11, MidpointRounding.AwayFromZero)
            : rounded;
    }

    private static BigInteger[] Integers(decimal[] values, int factor) => [.. values.Select(value => (BigInteger)(value * factor))];

    private static int Distinct(decimal[] values, decimal[] frequencies)
    {
        return values.Where((_, i) => frequencies[i] != 0m).Distinct().Count();
    }
}
