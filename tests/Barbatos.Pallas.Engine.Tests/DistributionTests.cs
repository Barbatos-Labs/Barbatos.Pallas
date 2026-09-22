// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using PeterO.Numbers;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Distribution application (manual pp. 95-100): binomial exactly, normal and Poisson against PeterO.Numbers.
/// </summary>
public sealed class DistributionTests
{
    private static readonly EContext Wide = EContext.ForPrecision(50);

    // A result between 10⁻¹⁴ and 7.9×10²⁸ is held to 15 significant digits: half a unit of the 15th is 5×10⁻¹⁵ relative.
    private const double FifteenDigits = 5e-15;

    private static CalculatorSession Session(CalculatorProfile profile = CalculatorProfile.Standard) => Calculator.Session(CalculatorApp.Distribution, profile);

    private static Value Number(decimal value) => Value.FromDecimal(value);

    private static Value[] Numbers(params decimal[] values) => [.. values.Select(Value.FromDecimal)];

    private static DistributionParameters Binomial(decimal trials, decimal probability) => new() { Trials = Number(trials), Probability = Number(probability) };

    private static DistributionParameters Normal(decimal mean, decimal deviation) => new() { Mean = Number(mean), StandardDeviation = Number(deviation) };

    [Fact]
    public void TheBinomialExampleOfTheManual()
    {
        // p. 97: Binomial CD of x = 2, 3, 4, 5 with N = 5 and p = 0.5; a list leaves Ans alone.
        CalculatorSession session = Session();

        IReadOnlyList<Calculation> results = session.CalculateDistribution(DistributionKind.BinomialCD, Binomial(5, 0.5m), Numbers(2, 3, 4, 5));

        results.Select(result => result.Display.Text).Should().Equal("0.5", "0.8125", "0.96875", "1");
        results[1].Result.IsExact.Should().BeTrue();
        results[1].Input.Should().Be("Binomial CD");
        session.Ans.Should().Be(Value.Zero);
    }

    [Fact]
    public void TheNormalExampleOfTheManual()
    {
        // p. 100: Normal PD at x = 36 with μ = 35, σ = 2 is e^(−1/8)/(2√(2π)) = 0.17603266338214973888…; it goes to Ans.
        CalculatorSession session = Session();

        Calculation result = session.CalculateDistribution(DistributionKind.NormalPD, Normal(35, 2) with { X = Number(36) });

        result.Display.Text.Should().Be("0.1760326634");
        Relative(result.Result.ToDouble(), "0.17603266338214973888").Should().BeLessThan(FifteenDigits);
        session.Ans.Should().Be(result.Result);
    }

    [Theory]
    // Exact fractions, displayed as decimals (assumption U23).
    [InlineData(DistributionKind.BinomialPD, 2, 5, "0.5", "0.3125")]
    [InlineData(DistributionKind.BinomialPD, 0, 0, "0.3", "1")]
    [InlineData(DistributionKind.BinomialPD, 0, 4, "0", "1")]
    [InlineData(DistributionKind.BinomialPD, 3, 4, "0", "0")]
    [InlineData(DistributionKind.BinomialPD, 4, 4, "1", "1")]
    [InlineData(DistributionKind.BinomialCD, 3, 4, "1", "0")]
    [InlineData(DistributionKind.BinomialCD, 4, 4, "1", "1")]
    [InlineData(DistributionKind.BinomialPD, 1, 3, "0.2", "0.384")]
    [InlineData(DistributionKind.BinomialCD, 1, 3, "0.2", "0.896")]
    [InlineData(DistributionKind.BinomialCD, 10, 10, "0.37", "1")]
    public void BinomialProbabilitiesAreExact(DistributionKind kind, int x, int trials, string probability, string expected)
    {
        Calculation result = Session().CalculateDistribution(kind, Binomial(trials, decimal.Parse(probability, CultureInfo.InvariantCulture)) with { X = Number(x) });

        result.Display.Text.Should().Be(expected);
        result.Result.IsExact.Should().BeTrue();
    }

    [Fact]
    public void ABinomialProbabilityOfAThousandTrials()
    {
        // C(1000, 500)/2¹⁰⁰⁰, computed here with BigInteger alone.
        BigInteger choose = BigInteger.One;
        for (int k = 0; k < 500; k++)
        {
            choose = choose * (1000 - k) / (k + 1);
        }

        BigInteger digits = choose * BigInteger.Pow(10, 28) / BigInteger.Pow(2, 1000);
        decimal expected = (decimal)digits / 10_000_000_000_000_000_000_000_000_000m;

        Value result = Session().CalculateDistribution(DistributionKind.BinomialPD, Binomial(1000, 0.5m) with { X = Number(500) }).Result;

        Math.Abs(result.ToDecimal() - expected).Should().BeLessThanOrEqualTo(1e-28m, "the digits here are truncated, the result's rounded");
    }

    [Fact]
    public void ABinomialOfAThirdIsRoundedOnce()
    {
        // p = 1/3 as a 28-digit decimal: P(X = 1; N = 3) = 3p(1 − p)² = 4/9 to 27 digits.
        Value third = Calculator.Evaluate("1÷3");
        Value result = Session().CalculateDistribution(DistributionKind.BinomialPD, new DistributionParameters { X = Number(1), Trials = Number(3), Probability = third }).Result;

        Math.Abs(result.ToDecimal() - (4m / 9m)).Should().BeLessThan(1e-26m);
    }

    [Theory]
    // x and N whole, 0 ≤ x ≤ N, 0 ≤ p ≤ 1 (assumption U24).
    [InlineData("7", "5", "0.5")]
    [InlineData("-1", "5", "0.5")]
    [InlineData("2.5", "5", "0.5")]
    [InlineData("2", "5.5", "0.5")]
    [InlineData("2", "5", "1.5")]
    [InlineData("2", "5", "-0.1")]
    [InlineData("2", "5", "1.0000000000000000000000000001")]
    public void ABinomialOutsideItsDomain_IsAMathError(string x, string trials, string probability)
    {
        DistributionParameters parameters = new()
        {
            X = Number(decimal.Parse(x, CultureInfo.InvariantCulture)),
            Trials = Number(decimal.Parse(trials, CultureInfo.InvariantCulture)),
            Probability = Number(decimal.Parse(probability, CultureInfo.InvariantCulture)),
        };

        Session().CalculateDistribution(DistributionKind.BinomialPD, parameters).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ALongBinomialIsATimeOut()
    {
        // About 2×10⁶ digits: the estimate exceeds the budget before any work starts.
        Session().CalculateDistribution(DistributionKind.BinomialCD, Binomial(1_000_000, 0.5m) with { X = Number(500_000) })
            .Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);

        CalculatorSession small = Session();
        small.Budget = new EngineBudget(1_000, TimeSpan.FromSeconds(10));
        small.CalculateDistribution(DistributionKind.BinomialCD, Binomial(200, 0.5m) with { X = Number(100) })
            .Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut, "the terms count against the budget too");
    }

    [Theory]
    // Φ(1) − Φ(−1) = 0.68268949213708589717…; Φ(∞) − Φ(0.5) = 0.30853753872598689637…
    [InlineData("-1", "1", "0", "1", "0.68268949213708589717")]
    [InlineData("36", "1E99", "35", "2", "0.30853753872598689637")]
    [InlineData("-1E99", "35", "35", "2", "0.5")]
    public void NormalIntervals(string lower, string upper, string mean, string deviation, string expected)
    {
        DistributionParameters parameters = new()
        {
            Lower = Calculator.Evaluate(lower.Replace("E", "×10^", StringComparison.Ordinal)),
            Upper = Calculator.Evaluate(upper.Replace("E", "×10^", StringComparison.Ordinal)),
            Mean = Number(decimal.Parse(mean, CultureInfo.InvariantCulture)),
            StandardDeviation = Number(decimal.Parse(deviation, CultureInfo.InvariantCulture)),
        };

        Relative(Session().CalculateDistribution(DistributionKind.NormalCD, parameters).Result.ToDouble(), expected).Should().BeLessThan(FifteenDigits);
    }

    [Theory]
    // Φ(11) − Φ(10) is 7.6×10⁻²⁴: the difference of two numbers within 10⁻²³ of 1 would be 0.
    [InlineData("10", "11")]
    [InlineData("-11", "-10")]
    public void AnIntervalFarInATailKeepsItsDigits(string lower, string upper)
    {
        DistributionParameters parameters = Normal(0, 1) with
        {
            Lower = Number(decimal.Parse(lower, CultureInfo.InvariantCulture)),
            Upper = Number(decimal.Parse(upper, CultureInfo.InvariantCulture)),
        };

        double result = Session().CalculateDistribution(DistributionKind.NormalCD, parameters).Result.ToDouble();

        // Q(10) − Q(11) at 50 digits, from erfc's continued fraction in PeterO.
        Relative(result, UpperTail(10).Subtract(UpperTail(11), Wide)).Should().BeLessThan(1e-14);
    }

    [Theory]
    // Φ⁻¹(0.975) = 1.95996398454005423552…; Φ⁻¹(0.5) = 0.
    [InlineData("0.975", "0", "1", "1.95996398454005423552")]
    [InlineData("0.025", "0", "1", "-1.95996398454005423552")]
    [InlineData("0.5", "35", "2", "35")]
    public void InverseNormalValues(string area, string mean, string deviation, string expected)
    {
        CalculatorSession session = Session();
        DistributionParameters parameters = new()
        {
            Area = Number(decimal.Parse(area, CultureInfo.InvariantCulture)),
            Mean = Number(decimal.Parse(mean, CultureInfo.InvariantCulture)),
            StandardDeviation = Number(decimal.Parse(deviation, CultureInfo.InvariantCulture)),
        };

        Calculation result = session.CalculateDistribution(DistributionKind.InverseNormal, parameters);

        Relative(result.Result.ToDouble(), expected).Should().BeLessThan(FifteenDigits);
        session.Ans.Should().Be(result.Result);
    }

    [Theory]
    // σ > 0, Lower ≤ Upper, 0 < Area < 1: the ends of Area give an infinite x (assumption U24).
    [InlineData(DistributionKind.NormalPD, "0", "0", "0", "0.5")]
    [InlineData(DistributionKind.NormalPD, "-1", "0", "0", "0.5")]
    [InlineData(DistributionKind.NormalCD, "1", "2", "1", "0.5")]
    [InlineData(DistributionKind.NormalCD, "1", "-1", "-2", "0.5")]
    [InlineData(DistributionKind.NormalCD, "0", "-1", "2", "0.5")]
    [InlineData(DistributionKind.NormalCD, "-1", "-1", "2", "0.5")]
    [InlineData(DistributionKind.InverseNormal, "1", "0", "0", "0")]
    [InlineData(DistributionKind.InverseNormal, "1", "0", "0", "1")]
    [InlineData(DistributionKind.InverseNormal, "1", "0", "0", "1.5")]
    [InlineData(DistributionKind.InverseNormal, "0", "0", "0", "0.5")]
    public void ANormalOutsideItsDomain_IsAMathError(DistributionKind kind, string deviation, string lower, string upper, string area)
    {
        DistributionParameters parameters = new()
        {
            StandardDeviation = Number(decimal.Parse(deviation, CultureInfo.InvariantCulture)),
            Lower = Number(decimal.Parse(lower, CultureInfo.InvariantCulture)),
            Upper = Number(decimal.Parse(upper, CultureInfo.InvariantCulture)),
            Area = Number(decimal.Parse(area, CultureInfo.InvariantCulture)),
        };

        Session().CalculateDistribution(kind, parameters).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnEmptyIntervalHasProbabilityZero()
    {
        Session().CalculateDistribution(DistributionKind.NormalCD, Normal(0, 1) with { Lower = Number(1.5m), Upper = Number(1.5m) }).Display.Text.Should().Be("0");
    }

    [Fact]
    public void ABoundBeyondTheRange_IsAMathError()
    {
        // A bound 1.8×10¹⁰⁰ from the mean leaves the Standard range, whichever bound it is; the other is at the mean.
        Value large = Value.FromDouble(9e99);
        Value small = Value.FromDouble(-9e99);
        DistributionParameters parameters = new() { Lower = small, Upper = large, StandardDeviation = Number(1) };

        Session().CalculateDistribution(DistributionKind.NormalCD, parameters with { Mean = large }).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().CalculateDistribution(DistributionKind.NormalCD, parameters with { Mean = small }).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // Far from the mean the density is 0, not an error: e^(−z²/2) underflows from |z| = 40, and z² = 10¹²⁰ would leave the range.
    [InlineData("40")]
    [InlineData("1E60")]
    [InlineData("0.00000000000000000001")]
    public void DensitiesFarOutAndAtTheMean(string x)
    {
        Value value = Calculator.Evaluate(x.Replace("E", "×10^", StringComparison.Ordinal));
        string expected = x.StartsWith("0.", StringComparison.Ordinal) ? "0.3989422804" : "0";

        Session().CalculateDistribution(DistributionKind.NormalPD, Normal(0, 1) with { X = value }).Display.Text.Should().Be(expected);
    }

    [Fact]
    public void InverseNormalScalesByTheDeviation()
    {
        // 10 + 2·Φ⁻¹(0.975) = 13.9199279690801084710…
        Value result = Session().CalculateDistribution(DistributionKind.InverseNormal, Normal(10, 2) with { Area = Number(0.975m) }).Result;

        Relative(result.ToDouble(), "13.91992796908010847104").Should().BeLessThan(FifteenDigits);
    }

    [Fact]
    public void AnApproximateParameterMakesAnApproximateBinomial()
    {
        Value half = Value.FromDouble(0.5);

        Session().CalculateDistribution(DistributionKind.BinomialPD, new DistributionParameters { X = Number(2), Trials = Number(5), Probability = half }).Result.IsExact.Should().BeFalse();
        Session().CalculateDistribution(DistributionKind.BinomialPD, new DistributionParameters { X = Value.FromDouble(2), Trials = Number(5), Probability = Number(0.5m) }).Result.IsExact.Should().BeFalse();
        Session().CalculateDistribution(DistributionKind.BinomialPD, new DistributionParameters { X = Number(2), Trials = Value.FromDouble(5), Probability = Number(0.5m) }).Result.IsExact.Should().BeFalse();
    }

    [Fact]
    public void ADensityFarOutKeepsItsDigits()
    {
        // e^(−453.005)/√(2π) = 7.9×10⁻¹⁹⁸, in the Extended range: z²/2 = 453.005 is split into 453 and 0.005 before exp,
        // where exp of 453.005 rounded to double would be 5×10⁻¹⁴ off.
        double result = Session(CalculatorProfile.Extended).CalculateDistribution(DistributionKind.NormalPD, Normal(0, 1) with { X = Number(30.1m) }).Result.ToDouble();

        Relative(result, EDecimal.FromString("-453.005").Exp(Wide).Divide(TwoPi.Sqrt(Wide), Wide)).Should().BeLessThan(2e-15);
    }

    [Theory]
    [InlineData(DistributionKind.PoissonPD, 2, "1")]
    [InlineData(DistributionKind.PoissonPD, 0, "3")]
    [InlineData(DistributionKind.PoissonPD, 1000, "1000")]
    [InlineData(DistributionKind.PoissonCD, 2, "1")]
    [InlineData(DistributionKind.PoissonCD, 0, "50")]
    [InlineData(DistributionKind.PoissonCD, 30, "10")]
    [InlineData(DistributionKind.PoissonCD, 1000, "1000")]
    [InlineData(DistributionKind.PoissonCD, 990, "1000.5")]
    [InlineData(DistributionKind.PoissonCD, 1100, "1000")]
    [InlineData(DistributionKind.PoissonCD, 50, "2")]
    [InlineData(DistributionKind.PoissonCD, 3, "3.5")]
    public void PoissonProbabilities(DistributionKind kind, int x, string lambda)
    {
        decimal mean = decimal.Parse(lambda, CultureInfo.InvariantCulture);
        double result = Session().CalculateDistribution(kind, new DistributionParameters { X = Number(x), Lambda = Number(mean) }).Result.ToDouble();

        Relative(result, PoissonReference(x, mean, kind == DistributionKind.PoissonCD)).Should().BeLessThan(1e-14);
    }

    [Theory]
    // x whole and 0 or more, λ > 0 (assumption U24).
    [InlineData("-1", "2")]
    [InlineData("1.5", "2")]
    [InlineData("1", "0")]
    [InlineData("1", "-2")]
    public void APoissonOutsideItsDomain_IsAMathError(string x, string lambda)
    {
        DistributionParameters parameters = new() { X = Number(decimal.Parse(x, CultureInfo.InvariantCulture)), Lambda = Number(decimal.Parse(lambda, CultureInfo.InvariantCulture)) };

        Session().CalculateDistribution(DistributionKind.PoissonPD, parameters).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().CalculateDistribution(DistributionKind.PoissonCD, parameters).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ALongPoissonSumIsATimeOut()
    {
        CalculatorSession session = Session();
        session.Budget = new EngineBudget(1_000, TimeSpan.FromSeconds(10));

        session.CalculateDistribution(DistributionKind.PoissonCD, new DistributionParameters { X = Number(1_000_000), Lambda = Number(1_000_000) })
            .Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void AListReportsEachValueOnItsOwn()
    {
        // p. 97: "ERROR" in the P column for a value outside the domain, the others calculated.
        IReadOnlyList<Calculation> results = Session().CalculateDistribution(DistributionKind.BinomialPD, Binomial(5, 0.5m), Numbers(2, 7, 5));

        results[0].Display.Text.Should().Be("0.3125");
        results[1].Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        results[1].Display.Text.Should().BeEmpty();
        results[2].Display.Text.Should().Be("0.03125");
        results.Should().OnlyContain(result => result.Input == "Binomial PD");
    }

    [Fact]
    public void AFailedCalculationLeavesAnsAlone()
    {
        CalculatorSession session = Session();
        session.CalculateDistribution(DistributionKind.PoissonPD, new DistributionParameters { X = Number(2), Lambda = Number(1) });
        Value ans = session.Ans;

        session.CalculateDistribution(DistributionKind.PoissonPD, new DistributionParameters { X = Number(-2), Lambda = Number(1) });

        session.Ans.Should().Be(ans);
    }

    [Fact]
    public void AComplexParameter_IsAMathError()
    {
        DistributionParameters parameters = Binomial(5, 0.5m) with { X = Value.FromComplex(System.Numerics.Complex.ImaginaryOne) };

        Session().CalculateDistribution(DistributionKind.BinomialPD, parameters).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().CalculateDistribution(DistributionKind.NormalCD, Normal(0, 1) with { X = Value.FromComplex(System.Numerics.Complex.ImaginaryOne), Upper = Number(1) })
            .Succeeded.Should().BeTrue("Normal CD does not read x");
    }

    [Fact]
    public void ParameterFieldsAreExpressionsOfTheDistributionApplication()
    {
        // A field takes an expression (p. 52 lists Distribution among the applications of d/dx).
        CalculatorSession session = Session();

        Value p = session.Calculate("d/dx(x²,0.25)").Result;
        session.CalculateDistribution(DistributionKind.BinomialPD, new DistributionParameters { X = Number(1), Trials = Number(2), Probability = p }).Display.Text.Should().Be("0.5");
    }

    [Fact]
    public void TheSessionChecksItsArguments()
    {
        CalculatorSession session = Session();
        DistributionParameters parameters = Binomial(5, 0.5m);

        session.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialPD, null!)).Should().Throw<ArgumentNullException>().WithParameterName("parameters");
        session.Invoking(s => s.CalculateDistribution((DistributionKind)7, parameters)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("kind");
        session.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialPD, parameters, null!)).Should().Throw<ArgumentNullException>().WithParameterName("list");
        session.Invoking(s => s.CalculateDistribution(DistributionKind.NormalPD, parameters, Numbers(1))).Should().Throw<ArgumentException>().WithParameterName("kind");
        session.Invoking(s => s.CalculateDistribution(DistributionKind.NormalCD, parameters, Numbers(1))).Should().Throw<ArgumentException>().WithParameterName("kind");
        session.Invoking(s => s.CalculateDistribution(DistributionKind.InverseNormal, parameters, Numbers(1))).Should().Throw<ArgumentException>().WithParameterName("kind");
        session.Invoking(s => s.CalculateDistribution(DistributionKind.PoissonPD, parameters, Numbers([.. Enumerable.Repeat(1m, 46)])))
            .Should().Throw<ArgumentOutOfRangeException>().WithParameterName("list");
        session.CalculateDistribution(DistributionKind.PoissonPD, parameters with { Lambda = Number(1) }, Numbers([.. Enumerable.Repeat(1m, 45)])).Should().HaveCount(45);
        Session(CalculatorProfile.Extended).CalculateDistribution(DistributionKind.PoissonPD, parameters with { Lambda = Number(1) }, Numbers([.. Enumerable.Repeat(1m, 46)])).Should().HaveCount(46);

        session.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialPD, null!, Numbers(1))).Should().Throw<ArgumentNullException>().WithParameterName("parameters");

        CalculatorSession calculate = Calculator.Session();
        calculate.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialPD, parameters)).Should().Throw<InvalidOperationException>();
        calculate.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialPD, parameters, Numbers(1))).Should().Throw<InvalidOperationException>();
    }

    private static readonly EDecimal TwoPi = EDecimal.FromString("6.283185307179586476925286766559005768394338798750211641949889");

    private static double Relative(double actual, string expected) => Relative(actual, EDecimal.FromString(expected));

    private static double Relative(double actual, EDecimal expected)
    {
        EDecimal difference = ERational.FromDouble(actual).ToEDecimal(Wide).Subtract(expected, Wide);
        return expected.IsZero ? difference.Abs().ToDouble() : difference.Divide(expected, Wide).Abs().ToDouble();
    }

    /// <summary>Q(z) = erfc(z/√2)/2 by the continued fraction of Γ(½, z²/2), forward, at 50 digits.</summary>
    private static EDecimal UpperTail(int z)
    {
        EDecimal x = EDecimal.FromInt32(z).Divide(EDecimal.FromString("1.414213562373095048801688724209698078569671875376948073176680"), Wide);
        EDecimal square = x.Multiply(x, Wide);
        EDecimal half = EDecimal.FromString("0.5");
        EDecimal b = square.Add(half);
        EDecimal c = EDecimal.FromString("1E200");
        EDecimal d = EDecimal.One.Divide(b, Wide);
        EDecimal fraction = d;
        for (int i = 1; i < 10_000; i++)
        {
            EDecimal numerator = EDecimal.FromInt32(i).Multiply(EDecimal.FromInt32(i).Subtract(half)).Negate();
            b = b.Add(EDecimal.FromInt32(2));
            d = EDecimal.One.Divide(numerator.Multiply(d, Wide).Add(b, Wide), Wide);
            c = b.Add(numerator.Divide(c, Wide), Wide);
            EDecimal step = d.Multiply(c, Wide);
            fraction = fraction.Multiply(step, Wide);
            if (step.Subtract(EDecimal.One).Abs().CompareToValue(EDecimal.FromString("1E-45")) < 0)
            {
                break;
            }
        }

        EDecimal sqrtPi = EDecimal.FromString("1.772453850905516027298167483341145182797549456122387128213808");
        return square.Negate().Exp(Wide).Multiply(x, Wide).Multiply(fraction, Wide).Divide(sqrtPi, Wide).Divide(EDecimal.FromInt32(2), Wide);
    }

    /// <summary>e^(−λ)·λ^x/x!, or its sum from 0 to x, at 50 digits.</summary>
    private static EDecimal PoissonReference(int x, decimal mean, bool cumulative)
    {
        EDecimal lambda = EDecimal.FromDecimal(mean);
        EDecimal term = lambda.Negate().Exp(Wide);
        EDecimal sum = term;
        for (int k = 1; k <= x; k++)
        {
            term = term.Multiply(lambda, Wide).Divide(EDecimal.FromInt32(k), Wide);
            sum = sum.Add(term, Wide);
        }

        return cumulative ? sum : term;
    }
}
