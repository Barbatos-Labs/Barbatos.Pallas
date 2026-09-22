// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using CsCheck;
using PeterO.Numbers;

namespace Barbatos.Pallas.Numerics.Tests;

/// <summary>
/// The Poisson probability against PeterO.Numbers at 50 digits: exactly e^(−λ)·λ^x/x! for moderate x, and through a
/// 50-digit Stirling series for large x, the two checked against each other.
/// </summary>
public sealed class PoissonDistributionTests
{
    // The exponent of a small probability carries its own rounding: the error grows with |ln P| (PoissonDistribution's remarks).
    private const double Tolerance = 1e-15;

    private static readonly EContext Wide = EContext.ForPrecision(50);

    [Fact]
    public void TheTwoReferencesAgree()
    {
        foreach ((double x, double mean) in (ReadOnlySpan<(double, double)>)[(1000d, 1000d), (1000d, 970.5), (400d, 450.25)])
        {
            Relative(ExactReference(x, mean), StirlingReference(x, mean)).Should().BeLessThan(1e-40);
        }
    }

    [Fact]
    public void ModerateMeans()
    {
        Gen.Double[-2d, 2.7d].SelectMany(exponent => Gen.Double[0d, 3d].Select(spread => (Mean: Math.Pow(10d, exponent), Spread: spread)))
            .Sample(sample =>
            {
                double x = Math.Floor(sample.Mean * sample.Spread);
                return Close(PoissonDistribution.Probability(x, sample.Mean), ExactReference(x, sample.Mean));
            }, iter: 300);
    }

    [Fact]
    public void LargeMeans()
    {
        // Around the mean, where exp(x·ln λ − λ − ln x!) would cancel most.
        Gen.Double[3d, 8d].SelectMany(exponent => Gen.Double[-10d, 10d].Select(deviations => (Mean: Math.Pow(10d, exponent), Deviations: deviations)))
            .Sample(sample =>
            {
                double x = Math.Round(sample.Mean + (sample.Deviations * Math.Sqrt(sample.Mean)));
                return Close(PoissonDistribution.Probability(x, sample.Mean), StirlingReference(x, sample.Mean));
            }, iter: 300);
    }

    [Theory]
    // Both sides of the table and of the near-the-mean series.
    [InlineData(15, 15)]
    [InlineData(16, 16)]
    [InlineData(16, 14.5)]
    [InlineData(16, 17.9)]
    [InlineData(16, 18)]
    [InlineData(1, 0.5)]
    [InlineData(3, 0.001)]
    public void TheMethodsMeet(double x, double mean)
    {
        double probability = PoissonDistribution.Probability(x, mean);
        Relative(probability, ExactReference(x, mean)).Should().BeLessThan(Tolerance * (1d + Math.Abs(Math.Log(probability))));
    }

    [Fact]
    public void SpecialValues()
    {
        PoissonDistribution.Probability(0d, 2d).Should().Be(Math.Exp(-2d));
        PoissonDistribution.Probability(0d, 800d).Should().Be(0d, "e^(−800) underflows");
        PoissonDistribution.Probability(5000d, 1d).Should().Be(0d);
        double.IsNaN(PoissonDistribution.Probability(-1d, 2d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(1.5d, 2d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(double.NaN, 2d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(double.PositiveInfinity, 2d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(1d, 0d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(1d, -2d)).Should().BeTrue();
        double.IsNaN(PoissonDistribution.Probability(1d, double.NaN)).Should().BeTrue();
    }

    [Fact]
    public void TheProbabilitiesAddUpToOne()
    {
        double sum = 0d;
        for (int x = 0; x <= 60; x++)
        {
            sum += PoissonDistribution.Probability(x, 12.5);
        }

        sum.Should().BeApproximately(1d, 1e-15);
    }

    private static bool Close(double actual, EDecimal expected)
    {
        // Below the normal range a double keeps fewer digits.
        return expected.CompareToValue(EDecimal.FromString("1E-300")) < 0
            ? Math.Abs(actual) < 1e-299
            : Relative(actual, expected) <= Tolerance * (1d + Math.Abs(Math.Log(actual)));
    }

    private static double Relative(double actual, EDecimal expected) => Relative(Exact(actual), expected);

    private static double Relative(EDecimal actual, EDecimal expected)
    {
        return actual.Subtract(expected, Wide).Divide(expected, Wide).Abs().ToDouble();
    }

    private static EDecimal Exact(double value) => ERational.FromDouble(value).ToEDecimal(Wide);

    /// <summary>e^(−λ)·λ^x/x! with x! exact.</summary>
    private static EDecimal ExactReference(double x, double mean)
    {
        EDecimal lambda = Exact(mean);
        EInteger factorial = EInteger.One;
        for (int k = 2; k <= (int)x; k++)
        {
            factorial *= k;
        }

        return lambda.Negate().Exp(Wide).Multiply(lambda.Pow(EDecimal.FromInt64((long)x), Wide), Wide).Divide(EDecimal.FromEInteger(factorial), Wide);
    }

    /// <summary>
    /// exp(x·ln λ − λ − ln x!) at 50 digits, with ln x! from its Stirling series, which converges fast from x = 400. Every
    /// logarithm takes a value rounded to 50 digits, where PeterO's defect (Support/Reference.cs) does not show.
    /// </summary>
    private static EDecimal StirlingReference(double x, double mean)
    {
        EDecimal lambda = Exact(mean);
        EDecimal n = EDecimal.FromInt64((long)x);

        // ln x! = x·ln x − x + ½·ln(2πx) + Σ B₂ₖ/(2k(2k − 1)x^(2k−1)).
        string[] bernoulli = ["1/6", "-1/30", "1/42", "-1/30", "5/66", "-691/2730", "7/6", "-3617/510"];
        EDecimal series = EDecimal.Zero;
        for (int k = 1; k <= bernoulli.Length; k++)
        {
            string[] parts = bernoulli[k - 1].Split('/');
            EDecimal b = EDecimal.FromString(parts[0]).Divide(EDecimal.FromString(parts[1]), Wide);
            series = series.Add(b.Divide(EDecimal.FromInt32(2 * k * ((2 * k) - 1)).Multiply(n.Pow(EDecimal.FromInt32((2 * k) - 1), Wide)), Wide), Wide);
        }

        EDecimal twoPi = EDecimal.FromString("6.283185307179586476925286766559005768394338798750211641949889");
        EDecimal logFactorial = n.Multiply(n.Log(Wide)).Subtract(n).Add(twoPi.Multiply(n, Wide).Log(Wide).Divide(EDecimal.FromInt32(2), Wide)).Add(series);
        return n.Multiply(lambda.Log(Wide), Wide).Subtract(lambda).Subtract(logFactorial, Wide).Exp(Wide);
    }
}
