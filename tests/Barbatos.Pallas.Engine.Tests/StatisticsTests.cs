// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Statistics application (manual pp. 79-95), with the data of Examples 3 and 4.
/// </summary>
public sealed class StatisticsTests
{
    private static Value[] Values(params decimal[] values) => [.. values.Select(Value.FromDecimal)];

    // p. 83: x = 1-10 with frequencies 1, 2, 1, 2, 2, 2, 3, 4, 2, 1.
    private static StatisticsData OneVariable() => new(Values(1, 2, 3, 4, 5, 6, 7, 8, 9, 10), frequencies: Values(1, 2, 1, 2, 2, 2, 3, 4, 2, 1));

    // p. 84.
    private static StatisticsData TwoVariable() => new(
        Values(1.0m, 1.2m, 1.5m, 1.6m, 1.9m, 2.1m, 2.4m, 2.5m, 2.7m, 3.0m),
        Values(1.0m, 1.1m, 1.2m, 1.3m, 1.4m, 1.5m, 1.6m, 1.7m, 1.8m, 2.0m));

    private static CalculatorSession Session(StatisticsData data, RegressionModel regression = RegressionModel.Linear, CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Statistics, profile);
        session.SetStatisticsData(data);
        session.Regression = regression;
        return session;
    }

    [Theory]
    // The 1-Var Results screen of p. 84.
    [InlineData("n", "20")]
    [InlineData("Σx", "119")]
    [InlineData("Σx²", "837")]
    [InlineData("x̄", "5.95")]
    [InlineData("σ²x", "6.4475")]
    [InlineData("σx", "2.539192785")]
    [InlineData("s²x", "6.786842105")]
    [InlineData("sx", "2.605156829")]
    [InlineData("min(x)", "1")]
    [InlineData("max(x)", "10")]
    [InlineData("Q1", "4")]
    [InlineData("Med", "6.5")]
    [InlineData("Q3", "8")]
    [InlineData("x̄×2", "11.9")]
    public void OneVariableResultsOfTheManual(string input, string expected)
    {
        Session(OneVariable()).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // The 2-Var Results screen of p. 85.
    [InlineData("Σx", "19.9")]
    [InlineData("Σy", "14.6")]
    [InlineData("Σx²", "43.57")]
    [InlineData("Σy²", "22.24")]
    [InlineData("Σxy", "30.96")]
    [InlineData("Σx³", "102.451")]
    [InlineData("Σx²y", "71.244")]
    [InlineData("Σx⁴", "253.5541")]
    [InlineData("x̄", "1.99")]
    [InlineData("ȳ", "1.46")]
    [InlineData("σ²y", "0.0924")]
    [InlineData("σx", "0.63")]
    [InlineData("σy", "0.3039736831")]
    [InlineData("s²y", "0.1026666667")]
    [InlineData("sx", "0.6640783086")]
    [InlineData("sy", "0.3204163958")]
    [InlineData("min(x)", "1")]
    [InlineData("max(x)", "3")]
    [InlineData("min(y)", "1")]
    [InlineData("max(y)", "2")]
    [InlineData("n", "10")]
    public void TwoVariableResultsOfTheManual(string input, string expected)
    {
        Session(TwoVariable()).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // pp. 86, 89 and 92.
    [InlineData(RegressionModel.Linear, "a", "0.5043587805")]
    [InlineData(RegressionModel.Linear, "b", "0.4802217183")]
    [InlineData(RegressionModel.Linear, "r", "0.9952824846")]
    [InlineData(RegressionModel.Linear, "5.5ŷ", "3.145578231")]
    [InlineData(RegressionModel.Quadratic, "a", "0.7028598638")]
    [InlineData(RegressionModel.Quadratic, "b", "0.2576384379")]
    [InlineData(RegressionModel.Quadratic, "c", "0.05610274153")]
    public void RegressionsOfTheManual(RegressionModel regression, string input, string expected)
    {
        Session(TwoVariable(), regression).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Fact]
    public void TheNormalDistributionOfTheManual()
    {
        // p. 92: 2▶t is −1.555612486 and P(t) 0.05990013396, with Ans holding t unrounded.
        CalculatorSession session = Session(OneVariable());

        session.Calculate("2▶t").Display.Text.Should().Be("-1.555612486");
        session.Calculate("P(Ans)").Display.Text.Should().Be("0.05990013396");
        session.Calculate("R(2▶t)").Display.Text.Should().Be("0.940099866");
        session.Calculate("Q(2▶t)").Display.Text.Should().Be("0.440099866", "Q(t) is the area between 0 and |t| (assumption U4)");
    }

    [Theory]
    // Φ(1.96) = 0.97500210485177952…; Q and R are its complements.
    [InlineData("P(1.96)", "0.9750021049")]
    [InlineData("R(1.96)", "0.02499789515")]
    [InlineData("Q(1.96)", "0.4750021049")]
    [InlineData("P(0)", "0.5")]
    [InlineData("Q(0)", "0")]
    [InlineData("R(0)", "0.5")]
    [InlineData("P(-40)", "0")]
    public void NormalDistributionValues(string input, string expected)
    {
        Session(OneVariable()).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // Data made from each model exactly; the fit gives the model back.
    [InlineData(RegressionModel.ExponentialAB, "1,2,3", "6,12,24", "a", "3")]
    [InlineData(RegressionModel.ExponentialAB, "1,2,3", "6,12,24", "b", "2")]
    [InlineData(RegressionModel.ExponentialAB, "1,2,3", "6,12,24", "r", "1")]
    [InlineData(RegressionModel.ExponentialAB, "1,2,3", "6,12,24", "4ŷ", "48")]
    [InlineData(RegressionModel.ExponentialAB, "1,2,3", "6,12,24", "48x̂", "4")]
    [InlineData(RegressionModel.Power, "1,2,3", "2,16,54", "a", "2")]
    [InlineData(RegressionModel.Power, "1,2,3", "2,16,54", "b", "3")]
    [InlineData(RegressionModel.Power, "1,2,3", "2,16,54", "4ŷ", "128")]
    [InlineData(RegressionModel.Power, "1,2,3", "2,16,54", "128x̂", "4")]
    [InlineData(RegressionModel.Inverse, "1,2,4", "3,2,1.5", "a", "1")]
    [InlineData(RegressionModel.Inverse, "1,2,4", "3,2,1.5", "b", "2")]
    [InlineData(RegressionModel.Inverse, "1,2,4", "3,2,1.5", "r", "1")]
    [InlineData(RegressionModel.Inverse, "1,2,4", "3,2,1.5", "8ŷ", "1.25")]
    [InlineData(RegressionModel.Inverse, "1,2,4", "3,2,1.5", "1.25x̂", "8")]
    [InlineData(RegressionModel.Linear, "1,2,3", "5,3,1", "r", "-1")]
    [InlineData(RegressionModel.Linear, "1,2,3", "5,3,1", "1x̂", "3")]
    [InlineData(RegressionModel.Quadratic, "-1,0,1,2", "1,0,1,4", "c", "1")]
    [InlineData(RegressionModel.Quadratic, "-1,0,1,2", "1,0,1,4", "3ŷ", "9")]
    [InlineData(RegressionModel.Quadratic, "-1,0,1,2", "1,0,1,4", "4x̂₁", "2")]
    [InlineData(RegressionModel.Quadratic, "-1,0,1,2", "1,0,1,4", "4x̂₂", "-2")]
    public void TheOtherRegressions(RegressionModel regression, string x, string y, string input, string expected)
    {
        CalculatorSession session = Session(new StatisticsData(Parse(x), Parse(y)), regression);

        session.Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // y = 1 + 2·ln x and y = 2·e^(x/2), sampled at values that carry ln and exp.
    [InlineData(RegressionModel.Logarithmic, "1|e|e^(2)", "1|3|5", "a", "1")]
    [InlineData(RegressionModel.Logarithmic, "1|e|e^(2)", "1|3|5", "b", "2")]
    [InlineData(RegressionModel.Logarithmic, "1|e|e^(2)", "1|3|5", "3ŷ", "3.197224577")]
    [InlineData(RegressionModel.Logarithmic, "1|e|e^(2)", "1|3|5", "5x̂", "7.389056099")]
    [InlineData(RegressionModel.ExponentialE, "0|2|4", "2|2e|2e^(2)", "a", "2")]
    [InlineData(RegressionModel.ExponentialE, "0|2|4", "2|2e|2e^(2)", "b", "0.5")]
    [InlineData(RegressionModel.ExponentialE, "0|2|4", "2|2e|2e^(2)", "6ŷ", "40.17107385")]
    [InlineData(RegressionModel.ExponentialE, "0|2|4", "2|2e|2e^(2)", "2x̂", "0")]
    public void RegressionsThroughLogarithms(RegressionModel regression, string x, string y, string input, string expected)
    {
        CalculatorSession session = Session(new StatisticsData(Evaluated(x), Evaluated(y)), regression);

        session.Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // p. 90: the one-variable statistics, the two-variable ones and each regression's own.
    [InlineData(false, RegressionModel.Linear, "Σy")]
    [InlineData(false, RegressionModel.Linear, "a")]
    [InlineData(false, RegressionModel.Linear, "5ŷ")]
    [InlineData(false, RegressionModel.Linear, "min(y)")]
    [InlineData(true, RegressionModel.Linear, "Q1")]
    [InlineData(true, RegressionModel.Linear, "Med")]
    [InlineData(true, RegressionModel.Linear, "2▶t")]
    [InlineData(true, RegressionModel.Linear, "P(1)")]
    [InlineData(true, RegressionModel.Linear, "Q(1)")]
    [InlineData(true, RegressionModel.Linear, "R(1)")]
    [InlineData(true, RegressionModel.Linear, "c")]
    [InlineData(true, RegressionModel.Linear, "5x̂₁")]
    [InlineData(true, RegressionModel.Quadratic, "r")]
    [InlineData(true, RegressionModel.Quadratic, "5x̂")]
    public void AStatisticTheDataDoNotOffer_IsASyntaxError(bool twoVariable, RegressionModel regression, string input)
    {
        CalculatorSession session = Session(twoVariable ? TwoVariable() : OneVariable(), regression);

        session.Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void StatisticsAreNamesOfTheStatisticsApplicationOnly()
    {
        Calculator.Error("Σx", CalculatorApp.Calculate).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("P(1)", CalculatorApp.Calculate).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void DegenerateData()
    {
        CalculatorSession empty = Session(StatisticsData.Empty);
        empty.Calculate("n").Display.Text.Should().Be("0");
        empty.Calculate("Σx").Display.Text.Should().Be("0");
        empty.Calculate("x̄").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        empty.Calculate("σx").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        empty.Calculate("min(x)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        empty.Calculate("Med").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        CalculatorSession one = Session(new StatisticsData(Values(5)));
        one.Calculate("σx").Display.Text.Should().Be("0");
        one.Calculate("sx").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        one.Calculate("Q1").Display.Text.Should().Be("5");
        one.Calculate("Q3").Display.Text.Should().Be("5");

        // Every x equal: Sxx is exactly 0, even for 20-digit values.
        CalculatorSession vertical = Session(new StatisticsData(Values(1.2345678901234567890m, 1.2345678901234567890m), Values(1, 2)));
        vertical.Calculate("b").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        vertical.Calculate("r").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        CalculatorSession line = Session(new StatisticsData(Values(1, 2), Values(1, 2)), RegressionModel.Quadratic);
        line.Calculate("a").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "two points do not fix a parabola");
    }

    [Fact]
    public void TheFrequencyRulesOfAssumptionU22()
    {
        // A negative frequency is a Math ERROR for every statistic.
        CalculatorSession negative = Session(new StatisticsData(Values(1, 2), frequencies: Values(1, -1)));
        negative.Calculate("n").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        negative.Calculate("max(x)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        // A fraction is a weight, but quartiles count values.
        CalculatorSession fraction = Session(new StatisticsData(Values(1, 3), frequencies: Values(0.5m, 1.5m)));
        fraction.Calculate("n").Display.Text.Should().Be("2");
        fraction.Calculate("x̄").Display.Text.Should().Be("2.5");
        fraction.Calculate("Med").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        // A row with frequency 0 is left out, of the extremes and the quartiles too.
        CalculatorSession zero = Session(new StatisticsData(Values(100, 2, 4, -100), frequencies: Values(0, 1, 3, 0)));
        zero.Calculate("n").Display.Text.Should().Be("4");
        zero.Calculate("min(x)").Display.Text.Should().Be("2");
        zero.Calculate("max(x)").Display.Text.Should().Be("4");
        zero.Calculate("Q1").Display.Text.Should().Be("3");
        zero.Calculate("Med").Display.Text.Should().Be("4");

        // A zero-frequency row with x ≤ 0 does not reach ln.
        CalculatorSession power = Session(new StatisticsData(Values(-1, 1, 2), Values(5, 2, 16), Values(0, 1, 1)), RegressionModel.Power);
        power.Calculate("b").Display.Text.Should().Be("3");
    }

    [Fact]
    public void WholeFrequenciesAreCountedHoweverTheyAreHeld()
    {
        // 2.0 is 2; 1 + 10⁻²⁰ is not whole, although it rounds to 1 as a double; 10³⁰ held as a double is.
        Session(new StatisticsData(Values(1, 2), frequencies: Values(2.0m, 2.0m))).Calculate("Q1").Display.Text.Should().Be("1");
        Session(new StatisticsData(Values(1, 2), frequencies: Values(1.00000000000000000001m, 1))).Calculate("Med").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session(new StatisticsData(Values(1, 2), frequencies: [Value.FromDouble(1e30), Value.One])).Calculate("Q3").Display.Text.Should().Be("1");
        Session(new StatisticsData(Values(1, 2), frequencies: [Value.FromDouble(1e-20), Value.One])).Calculate("Med").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AQuartileIsTheExactMidpoint()
    {
        // (9×10⁹⁹ + 9.5×10⁹⁹)/2 is in range although the sum is not; 10⁻⁹⁹ stays 10⁻⁹⁹.
        Session(new StatisticsData([Value.FromDouble(9e99), Value.FromDouble(9.5e99)])).Calculate("Med").Display.Text.Should().Be("9.25×10^99");
        Session(new StatisticsData([Value.FromDouble(1e-99), Value.FromDouble(1e-99)])).Calculate("Med").Display.Text.Should().Be("1×10^-99");
        Session(new StatisticsData(Values(1, 2))).Calculate("Med").Result.IsExact.Should().BeTrue();
        Session(new StatisticsData([Value.One, Calculator.Evaluate("√(2)")])).Calculate("Med").Result.IsExact.Should().BeFalse();
    }

    [Fact]
    public void ExtremesCompareDecimalsExactly()
    {
        // The two values are one double apart at most: as doubles they are equal.
        CalculatorSession session = Session(new StatisticsData(Values(1.0000000000000000000000000001m, 1m)));

        session.Calculate("min(x)").Result.ToDecimal().Should().Be(1m);
        session.Calculate("max(x)").Result.ToDecimal().Should().Be(1.0000000000000000000000000001m);
    }

    [Fact]
    public void AResultIsExactWhenEveryValueIsAndNoLogarithmIsTaken()
    {
        Value root = Calculator.Evaluate("√(2)");

        Session(new StatisticsData([Value.One, root])).Calculate("Σx").Result.IsExact.Should().BeFalse();
        Session(new StatisticsData(Values(1, 2), [Value.One, root])).Calculate("n").Result.IsExact.Should().BeFalse();
        Session(new StatisticsData(Values(1, 2), frequencies: [Value.One, root])).Calculate("n").Result.IsExact.Should().BeFalse();
        Session(new StatisticsData(Values(1, 2), Values(3, 5))).Calculate("a").Result.IsExact.Should().BeTrue();
        Session(new StatisticsData(Values(1, 2, 4), Values(3, 2, 1.5m)), RegressionModel.Inverse).Calculate("b").Result.IsExact.Should().BeTrue();
        Session(new StatisticsData(Values(1, 2, 3), Values(2, 16, 54)), RegressionModel.Power).Calculate("b").Result.IsExact.Should().BeFalse();
        Session(new StatisticsData(Values(1, 2, 3), Values(2, 16, 54)), RegressionModel.ExponentialE).Calculate("b").Result.IsExact.Should().BeFalse();
        Session(new StatisticsData(Values(1, 2, 3), Values(2, 16, 54)), RegressionModel.Logarithmic).Calculate("b").Result.IsExact.Should().BeFalse();
    }

    [Theory]
    // A value without a logarithm or a reciprocal leaves the model without a fit.
    [InlineData(RegressionModel.Logarithmic, "0,1,2", "1,2,3", "a")]
    [InlineData(RegressionModel.ExponentialE, "0,1,2", "-1,2,3", "b")]
    [InlineData(RegressionModel.Power, "1,2,3", "1,2,-3", "r")]
    [InlineData(RegressionModel.Inverse, "0,1,2", "1,2,3", "a")]
    public void ARegressionOfValuesOutsideItsDomain_IsAMathError(RegressionModel regression, string x, string y, string input)
    {
        Session(new StatisticsData(Parse(x), Parse(y)), regression).Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // Estimates and ▶t are decimals too, where MathI/MathO would show 1⌟2 (assumption U23).
    [InlineData(RegressionModel.Linear, "1x̂", "0.5")]
    [InlineData(RegressionModel.Linear, "0.25ŷ", "0.5")]
    [InlineData(RegressionModel.Quadratic, "0.25x̂₁", "0.5")]
    [InlineData(RegressionModel.Quadratic, "0.25x̂₂", "-0.5")]
    public void EstimatesAreDecimals(RegressionModel regression, string input, string expected)
    {
        StatisticsData data = regression == RegressionModel.Linear
            ? new StatisticsData(Values(1, 2, 3), Values(2, 4, 6))
            : new StatisticsData(Values(-1, 0, 1, 2), Values(1, 0, 1, 4));

        Session(data, regression).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Fact]
    public void AStandardizedVariateIsADecimal()
    {
        Session(new StatisticsData(Values(1, 3))).Calculate("2.5▶t").Display.Text.Should().Be("0.5");
    }

    [Fact]
    public void UncorrelatedDataHaveRZero()
    {
        Session(new StatisticsData(Values(1, 2, 3), Values(1, 3, 1))).Calculate("r").Display.Text.Should().Be("0");
    }

    [Fact]
    public void AFrequencyOfAMillionMillionMillionIsCounted()
    {
        // Quartiles walk the cumulative frequencies on BigInteger.
        CalculatorSession session = Session(new StatisticsData(Values(1, 2), frequencies: Values(1_000_000_000_000_000_000m, 3_000_000_000_000_000_000m)));

        session.Calculate("Q1").Display.Text.Should().Be("1.5");
        session.Calculate("Med").Display.Text.Should().Be("2");
    }

    [Fact]
    public void ValuesHeldAsDoubleAreTakenAtTheirDigits()
    {
        // 1.6×10⁻¹⁹ is below decimal's range; its statistics are still exact from its digits.
        CalculatorSession session = Session(new StatisticsData([Value.FromDouble(1.6e-19), Value.FromDouble(3.2e-19), Value.FromDouble(4.8e-19)]));

        session.Calculate("x̄").Display.Text.Should().Be("3.2×10^-19");
        session.Calculate("σ²x").Display.Text.Should().Be("1.706666667×10^-38");
        session.Calculate("Σx").Result.ToDouble().Should().Be(9.6e-19);

        CalculatorSession large = Session(new StatisticsData([Value.FromDouble(1e30), Value.FromDouble(3e30)], [Value.FromDouble(2e30), Value.FromDouble(4e30)]));
        large.Calculate("b").Display.Text.Should().Be("1");
        large.Calculate("a").Display.Text.Should().Be("1×10^30");
    }

    [Fact]
    public void ExactDataGiveExactResults()
    {
        CalculatorSession session = Session(OneVariable());

        Calculation mean = session.Calculate("x̄");
        mean.Result.IsExact.Should().BeTrue();
        session.Format(mean, FormatTarget.ImproperFraction)!.Text.Should().Be("119⌟20", "FORMAT still converts a statistic");
        session.Calculate("σx").Result.IsExact.Should().BeFalse();
    }

    [Fact]
    public void AStatisticIsAConstantToCalculus()
    {
        CalculatorSession session = Session(OneVariable());

        session.Calculate("d/dx(x̄×x,1)").Display.Text.Should().Be("5.95");
        session.Calculate("Σ(x̄,1,4)").Display.Text.Should().Be("23.8");
    }

    [Fact]
    public void ChangingTheDataOrTheRegressionRecalculates()
    {
        CalculatorSession session = Session(TwoVariable());
        session.Calculate("b").Display.Text.Should().Be("0.4802217183");

        session.Regression = RegressionModel.Quadratic;
        session.Calculate("b").Display.Text.Should().Be("0.2576384379");

        session.SetStatisticsData(new StatisticsData(Values(1, 2, 3), Values(2, 4, 6)));
        session.Regression = RegressionModel.Linear;
        session.Calculate("b").Display.Text.Should().Be("2");

        session.SwitchApp(CalculatorApp.Calculate);
        session.SwitchApp(CalculatorApp.Statistics);
        session.StatisticsData.Should().Be(new StatisticsData(Values(1, 2, 3), Values(2, 4, 6)), "the data stay until replaced");
    }

    [Theory]
    // p. 80: 160 rows of one column, 80 of two, 53 of three.
    [InlineData(false, false, 160)]
    [InlineData(true, false, 80)]
    [InlineData(false, true, 80)]
    [InlineData(true, true, 53)]
    public void TheStandardProfileHoldsTheEditorsRows(bool twoVariable, bool frequencies, int limit)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Statistics);
        StatisticsData Rows(int count) => new(Values([.. Enumerable.Repeat(1m, count)]), twoVariable ? Values([.. Enumerable.Repeat(1m, count)]) : null, frequencies ? Values([.. Enumerable.Repeat(1m, count)]) : null);

        session.SetStatisticsData(Rows(limit));
        session.StatisticsData.Rows.Should().Be(limit);
        session.Invoking(s => s.SetStatisticsData(Rows(limit + 1))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("data");
        Calculator.Session(CalculatorApp.Statistics, CalculatorProfile.Extended).Invoking(s => s.SetStatisticsData(Rows(10_000))).Should().NotThrow();
        Calculator.Session(CalculatorApp.Statistics, CalculatorProfile.Extended).Invoking(s => s.SetStatisticsData(Rows(10_001))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TheSessionChecksItsArguments()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Statistics);

        session.StatisticsData.Should().BeSameAs(StatisticsData.Empty);
        session.Regression.Should().Be(RegressionModel.Linear);
        session.Invoking(s => s.SetStatisticsData(null!)).Should().Throw<ArgumentNullException>().WithParameterName("data");
        session.Invoking(s => s.Regression = (RegressionModel)7).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }

    [Fact]
    public void SortingKeepsRowsTogether()
    {
        // p. 83: sort by x ascending, then by y descending.
        StatisticsData data = new(Values(170, 179, 173), Values(66, 75, 68));

        data.Sort(StatisticsColumn.X).ToString().Should().Be("[[170, 66], [173, 68], [179, 75]]");
        data.Sort(StatisticsColumn.X).Sort(StatisticsColumn.Y, descending: true).ToString().Should().Be("[[179, 75], [173, 68], [170, 66]]");

        StatisticsData weighted = new(Values(3, 1, 2), frequencies: Values(1, 3, 2));
        weighted.Sort(StatisticsColumn.Frequency).ToString().Should().Be("[[3, 1], [2, 2], [1, 3]]");

        // Equal values keep their order; decimals and doubles compare as numbers.
        StatisticsData mixed = new([Value.FromDecimal(2m), Value.FromDouble(1e-20), Value.FromDecimal(2m), Value.FromDouble(1e30)], Values(1, 2, 3, 4));
        mixed.Sort(StatisticsColumn.X).Y.Should().Equal(Values(2, 1, 3, 4));
        mixed.Sort(StatisticsColumn.X, descending: true).Y.Should().Equal(Values(4, 1, 3, 2));
    }

    [Fact]
    public void StatisticsDataChecksItsColumns()
    {
        Action none = () => _ = new StatisticsData(null!);
        Action shortY = () => _ = new StatisticsData(Values(1, 2), Values(1));
        Action yWithoutX = () => _ = new StatisticsData([], Values(1));
        Action shortFrequencies = () => _ = new StatisticsData(Values(1, 2), frequencies: Values(1));
        Action complex = () => _ = new StatisticsData([Value.FromComplex(System.Numerics.Complex.ImaginaryOne)]);
        Action matrix = () => _ = new StatisticsData(Values(1), [Value.FromMatrix(new MatrixValue(new[,] { { Value.One } }))]);
        Action noY = () => _ = new StatisticsData(Values(1)).Sort(StatisticsColumn.Y);
        Action noFrequency = () => _ = new StatisticsData(Values(1), Values(1)).Sort(StatisticsColumn.Frequency);

        none.Should().Throw<ArgumentNullException>().WithParameterName("x");
        shortY.Should().Throw<ArgumentException>().WithParameterName("y");
        yWithoutX.Should().Throw<ArgumentException>().WithParameterName("y");
        shortFrequencies.Should().Throw<ArgumentException>().WithParameterName("frequencies");
        complex.Should().Throw<ArgumentException>().WithParameterName("x");
        matrix.Should().Throw<ArgumentException>().WithParameterName("y");
        noY.Should().Throw<ArgumentException>().WithParameterName("column");
        noFrequency.Should().Throw<ArgumentException>().WithParameterName("column");
    }

    [Fact]
    public void StatisticsDataIsAValue()
    {
        StatisticsData data = new(Values(1, 2), Values(3, 4), Values(1, 1));

        data.IsTwoVariable.Should().BeTrue();
        data.HasFrequencies.Should().BeTrue();
        data.Rows.Should().Be(2);
        data.Columns.Should().Be(3);
        data.ToString().Should().Be("[[1, 3, 1], [2, 4, 1]]");
        data.Should().Be(new StatisticsData(Values(1, 2), Values(3, 4), Values(1, 1)));
        data.GetHashCode().Should().Be(new StatisticsData(Values(1, 2), Values(3, 4), Values(1, 1)).GetHashCode());
        data.Should().NotBe(new StatisticsData(Values(1, 2), Values(3, 5), Values(1, 1)));
        data.GetHashCode().Should().NotBe(new StatisticsData(Values(1, 2), Values(3, 5), Values(1, 1)).GetHashCode());
        data.Should().NotBe(new StatisticsData(Values(1, 2), Values(3, 4)));
        new StatisticsData(Values(1, 2), frequencies: Values(1, 2)).GetHashCode().Should().NotBe(new StatisticsData(Values(1, 2), frequencies: Values(2, 1)).GetHashCode());
        new StatisticsData([]).Should().NotBe(new StatisticsData([], []), "empty two-variable data are not empty one-variable data");
        new StatisticsData([], []).GetHashCode().Should().NotBe(new StatisticsData([]).GetHashCode());
        new StatisticsData(Values(1)).Should().NotBe(new StatisticsData(Values(1), frequencies: Values(1)));
        data.Equals((object?)null).Should().BeFalse();
        StatisticsData.Empty.Columns.Should().Be(1);
    }

    private static Value[] Parse(string values) => Values([.. values.Split(',').Select(value => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture))]);

    private static Value[] Evaluated(string inputs) => [.. inputs.Split('|').Select(input => Calculator.Evaluate(input))];
}
