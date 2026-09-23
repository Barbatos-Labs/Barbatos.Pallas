// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Math Box application (manual pp. 146-161): Dice Roll and Coin Toss, Number Line and Circle.
/// </summary>
public sealed class MathBoxTests
{
    private static CalculatorSession Session(AngleUnit unit = AngleUnit.Degree, int? seed = 880) =>
        Calculator.Session(CalculatorApp.MathBox, settings: settings => settings with { AngleUnit = unit }, randomSeed: seed);

    private static Value Number(decimal value) => Value.FromDecimal(value);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DiceLandOnOneToSix(int dice)
    {
        Simulation roll = Session().Simulate(SimulationKind.DiceRoll, dice, Number(250));

        roll.Succeeded.Should().BeTrue();
        roll.Attempts.Should().HaveCount(250).And.OnlyContain(attempt => attempt.Length == dice);
        roll.Attempts.SelectMany(attempt => attempt).Should().OnlyContain(face => face >= 1 && face <= 6);
        roll.Attempts.SelectMany(attempt => attempt).Distinct().Should().HaveCount(6, "250 throws of a fair die show every face");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CoinsLandHeadsOrTails(int coins)
    {
        Simulation toss = Session().Simulate(SimulationKind.CoinToss, coins, Number(100));

        toss.Attempts.Should().HaveCount(100).And.OnlyContain(attempt => attempt.Length == coins);
        toss.Attempts.SelectMany(attempt => attempt).Distinct().Order().Should().Equal(0, 1);
    }

    [Fact]
    public void TheSumAndTheDifferenceOfTwoDiceAreTheListColumns()
    {
        // p. 149: A and B, then Sum and Diff.
        Simulation roll = Session().Simulate(SimulationKind.DiceRoll, 2, Number(50));

        for (int attempt = 0; attempt < 50; attempt++)
        {
            (int a, int b) = (roll.Attempts[attempt][0], roll.Attempts[attempt][1]);
            roll.Sum(attempt).Should().Be(a + b);
            roll.Difference(attempt).Should().Be(Math.Abs(a - b));
        }
    }

    [Fact]
    public void OnlyTwoDiceHaveADifference()
    {
        Simulation three = Session().Simulate(SimulationKind.DiceRoll, 3, Number(5));
        Simulation coins = Session().Simulate(SimulationKind.CoinToss, 2, Number(5));

        ((Action)(() => three.Difference(0))).Should().Throw<InvalidOperationException>();
        ((Action)(() => coins.Difference(0))).Should().Throw<InvalidOperationException>();
        ((Action)(() => three.Frequencies(SimulationTally.Difference))).Should().Throw<ArgumentException>().WithParameterName("tally");
    }

    [Theory]
    [InlineData(SimulationKind.DiceRoll, 1, SimulationTally.Sum, 1, 6)]
    [InlineData(SimulationKind.DiceRoll, 2, SimulationTally.Sum, 2, 12)]
    [InlineData(SimulationKind.DiceRoll, 3, SimulationTally.Sum, 3, 18)]
    [InlineData(SimulationKind.DiceRoll, 2, SimulationTally.Difference, 0, 5)]
    [InlineData(SimulationKind.CoinToss, 1, SimulationTally.Heads, 0, 1)]
    [InlineData(SimulationKind.CoinToss, 3, SimulationTally.Heads, 0, 3)]
    public void TheRelativeFreqScreenCountsEveryOutcome(SimulationKind kind, int count, SimulationTally tally, int lowest, int highest)
    {
        // pp. 150, 153: one row per outcome, and the frequencies add up to the attempts.
        Simulation simulation = Session().Simulate(kind, count, Number(250));

        SimulationFrequency[] rows = [.. simulation.Frequencies(tally)];

        rows.Select(row => row.Outcome).Should().Equal(Enumerable.Range(lowest, highest - lowest + 1));
        rows.Sum(row => row.Frequency).Should().Be(250);
        rows.Should().OnlyContain(row => row.RelativeFrequency.ToDecimal() == row.Frequency / 250m);
    }

    [Fact]
    public void ARelativeFrequencyIsADecimal()
    {
        // p. 150 shows 46 of 250 as 0.184, not as the fraction 23⌟125.
        Simulation roll = Session().Simulate(SimulationKind.DiceRoll, 1, Number(250));

        SimulationFrequency row = roll.Frequencies(SimulationTally.Sum).First(frequency => frequency.Frequency > 0);
        CalculatorSettings decimalOutput = CalculatorSettings.Initial with { InputOutput = InputOutput.MathIDecimalO };

        row.RelativeFrequency.ToDecimal().Should().Be(row.Frequency / 250m);
        PallasEngine.Format(row.RelativeFrequency, decimalOutput)!.Text.Should()
            .Be((row.Frequency / 250m).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ACoinCountsItsHeads()
    {
        Simulation toss = Session().Simulate(SimulationKind.CoinToss, 3, Number(20));

        for (int attempt = 0; attempt < 20; attempt++)
        {
            toss.Sum(attempt).Should().Be(toss.Attempts[attempt].Count(side => side == 1));
        }

        ((Action)(() => toss.Frequencies(SimulationTally.Sum))).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("251")]
    [InlineData("2.5")]
    [InlineData("-1")]
    [InlineData("10^30")]
    public void AttemptsOutsideOneTo250AreARangeError(string attempts)
    {
        // p. 164: a whole number from 1 to 250.
        Value value = Calculator.Evaluate(attempts);

        Simulation roll = Session().Simulate(SimulationKind.DiceRoll, 1, value);

        roll.Succeeded.Should().BeFalse();
        roll.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
        roll.Attempts.Should().BeEmpty();
        ((Action)(() => roll.Frequencies(SimulationTally.Sum))).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AComplexNumberOfAttemptsIsARangeError()
    {
        Value complex = Calculator.Evaluate("2+3i", CalculatorApp.Complex);

        Session().Simulate(SimulationKind.CoinToss, 1, complex).Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(250)]
    public void OneAnd250AttemptsAreAllowed(int attempts)
    {
        Session().Simulate(SimulationKind.CoinToss, 2, Number(attempts)).Attempts.Should().HaveCount(attempts);
    }

    [Theory]
    [InlineData(SameResult.First)]
    [InlineData(SameResult.Second)]
    [InlineData(SameResult.Third)]
    public void APresetGivesTheSameResultToEveryone(SameResult preset)
    {
        // p. 150: every calculator of a class shows one result - whatever each session did before.
        CalculatorSession first = Session(seed: 1);
        CalculatorSession second = Session(seed: null);
        second.Evaluate("Ran#");

        Simulation one = first.Simulate(SimulationKind.DiceRoll, 2, Number(100), preset);
        Simulation other = second.Simulate(SimulationKind.DiceRoll, 2, Number(100), preset);

        other.Attempts.Select(attempt => string.Join(",", attempt)).Should().Equal(one.Attempts.Select(attempt => string.Join(",", attempt)));
    }

    [Theory]
    [InlineData(SameResult.First, "2,1,3,5,4,3,3,6,1,4")]
    [InlineData(SameResult.Second, "5,3,1,6,1,2,5,3,2,1")]
    [InlineData(SameResult.Third, "2,5,6,2,4,2,2,6,3,3")]
    public void APresetIsTheSameResultOnEveryVersion(SameResult preset, string faces)
    {
        // The promise of p. 150 holds between copies only while the results never change: a new order of drawing, or a
        // .NET whose seeded Random drew differently, would fail here rather than give a class two results. Measured
        // 24 Sep 2026, and the same from .NET Framework's Random, which shares the seeded algorithm.
        Simulation roll = Session().Simulate(SimulationKind.DiceRoll, 1, Number(10), preset);

        string.Join(",", roll.Attempts.Select(attempt => attempt[0])).Should().Be(faces);
    }

    [Fact]
    public void APresetThrowsCoinsInTheSameOrder()
    {
        Simulation toss = Session().Simulate(SimulationKind.CoinToss, 3, Number(4), SameResult.First);

        string.Join(" ", toss.Attempts.Select(attempt => string.Concat(attempt))).Should().Be("000 110 010 100");
    }

    [Fact]
    public void ThePresetsAreThreeDifferentResults()
    {
        CalculatorSession session = Session();

        string[] results =
        [
            .. ((SameResult[])[SameResult.First, SameResult.Second, SameResult.Third])
                .Select(preset => string.Join(";", session.Simulate(SimulationKind.DiceRoll, 1, Number(30), preset).Attempts.Select(attempt => attempt[0]))),
        ];

        results.Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void WithoutAPresetEveryRollIsANewOne()
    {
        CalculatorSession session = Session();

        string first = string.Join(";", session.Simulate(SimulationKind.DiceRoll, 1, Number(30)).Attempts.Select(attempt => attempt[0]));
        string second = string.Join(";", session.Simulate(SimulationKind.DiceRoll, 1, Number(30)).Attempts.Select(attempt => attempt[0]));

        second.Should().NotBe(first);
    }

    [Fact]
    public void APresetLeavesTheSessionsOwnRandomNumbersAlone()
    {
        CalculatorSession plain = Session(seed: 7);
        CalculatorSession preset = Session(seed: 7);

        preset.Simulate(SimulationKind.DiceRoll, 3, Number(250), SameResult.First);

        preset.Evaluate("Ran#").Result.Should().Be(plain.Evaluate("Ran#").Result);
    }

    [Fact]
    public void ASimulationIsNotAnAnswer()
    {
        CalculatorSession session = Session();
        session.Calculate("7");

        session.Simulate(SimulationKind.DiceRoll, 1, Number(10));

        session.Ans.ToDecimal().Should().Be(7m);
        session.History.Should().ContainSingle();
    }

    [Fact]
    public void ASimulationNeedsMathBoxAndItsOwnArguments()
    {
        CalculatorSession session = Session();

        ((Action)(() => Calculator.Session().Simulate(SimulationKind.DiceRoll, 1, Number(5)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => session.Simulate(SimulationKind.DiceRoll, 0, Number(5)))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
        ((Action)(() => session.Simulate(SimulationKind.DiceRoll, 4, Number(5)))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
        ((Action)(() => session.Simulate((SimulationKind)9, 1, Number(5)))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("kind");
        ((Action)(() => session.Simulate(SimulationKind.CoinToss, 1, Number(5), (SameResult)9))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sameResult");
    }

    [Theory]
    [InlineData(NumberLineForm.Less, false, false)]
    [InlineData(NumberLineForm.LessOrEqual, false, true)]
    [InlineData(NumberLineForm.Greater, false, false)]
    [InlineData(NumberLineForm.GreaterOrEqual, true, false)]
    public void AnExpressionWithOneBoundGoesOnToOneSide(NumberLineForm form, bool lowerIncluded, bool upperIncluded)
    {
        // p. 155: an arrow for the side with no bound, a filled dot for a bound that is part of the set.
        NumberLineAxis axis = NumberLine.Define(form, Number(-1.5m));

        axis.Succeeded.Should().BeTrue();
        bool left = form is NumberLineForm.Less or NumberLineForm.LessOrEqual;
        axis.Lower.Should().Be(left ? null : Number(-1.5m));
        axis.Upper.Should().Be(left ? Number(-1.5m) : null);
        axis.LowerIncluded.Should().Be(lowerIncluded);
        axis.UpperIncluded.Should().Be(upperIncluded);
        axis.B.Should().BeNull();
    }

    [Fact]
    public void AnEqualityIsOnePoint()
    {
        NumberLineAxis axis = NumberLine.Define(NumberLineForm.Equal, Number(2));

        axis.Lower.Should().Be(Number(2));
        axis.Upper.Should().Be(Number(2));
        (axis.LowerIncluded && axis.UpperIncluded).Should().BeTrue();
    }

    [Theory]
    [InlineData(NumberLineForm.Between, false, false)]
    [InlineData(NumberLineForm.FromIncluded, true, false)]
    [InlineData(NumberLineForm.ToIncluded, false, true)]
    [InlineData(NumberLineForm.BetweenIncluded, true, true)]
    public void AnExpressionWithTwoBoundsIsASegment(NumberLineForm form, bool lowerIncluded, bool upperIncluded)
    {
        NumberLineAxis axis = NumberLine.Define(form, Number(-2m), Number(-0.5m));

        axis.Lower.Should().Be(Number(-2m));
        axis.Upper.Should().Be(Number(-0.5m));
        axis.LowerIncluded.Should().Be(lowerIncluded);
        axis.UpperIncluded.Should().Be(upperIncluded);
    }

    [Theory]
    [InlineData("10", "5")]
    [InlineData("5", "5")]
    [InlineData("-10000000001", "0")]
    [InlineData("0", "10000000001")]
    [InlineData("0", "10^30")]
    public void BoundsOutOfRangeOrInTheWrongOrderAreARangeError(string a, string b)
    {
        // p. 165: 10<x≤5, and a or b beyond ±10¹⁰ (p. 153).
        NumberLineAxis axis = NumberLine.Define(NumberLineForm.ToIncluded, Calculator.Evaluate(a), Calculator.Evaluate(b));

        axis.Succeeded.Should().BeFalse();
        axis.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }

    [Theory]
    [InlineData("10000000000")]
    [InlineData("-10000000000")]
    public void TenToTheTenIsInRange(string a)
    {
        NumberLine.Define(NumberLineForm.Greater, Calculator.Evaluate(a)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AnExpressionHasTheBoundsOfItsForm()
    {
        ((Action)(() => NumberLine.Define(NumberLineForm.Less, Number(1), Number(2)))).Should().Throw<ArgumentException>().WithParameterName("b");
        ((Action)(() => NumberLine.Define(NumberLineForm.Between, Number(1)))).Should().Throw<ArgumentException>().WithParameterName("b");
        ((Action)(() => NumberLine.Define((NumberLineForm)99, Number(1)))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("form");
    }

    [Fact]
    public void TheViewOfTheManualsExample()
    {
        // p. 156: x≤-1.5, x>-1.0 and -2.0<x≤-0.5 are drawn with Scale 0.2 and Center -1.2, from -2.8 to 0.4.
        NumberLineAxis[] axes =
        [
            NumberLine.Define(NumberLineForm.LessOrEqual, Number(-1.5m)),
            NumberLine.Define(NumberLineForm.Greater, Number(-1.0m)),
            NumberLine.Define(NumberLineForm.ToIncluded, Number(-2.0m), Number(-0.5m)),
        ];

        NumberLineView view = NumberLine.Fit(axes);

        view.Scale.Should().Be(0.2m);
        view.Center.Should().Be(-1.2m);
        view.Minimum.Should().Be(-2.8m);
        view.Maximum.Should().Be(0.4m);
    }

    [Fact]
    public void AViewSetByHand()
    {
        // p. 157: Scale 1 and Center 2 draw the axis from -6 to 10.
        NumberLineView view = NumberLine.View(Number(2), Number(1));

        view.Succeeded.Should().BeTrue();
        (view.Minimum, view.Maximum).Should().Be((-6m, 10m));
    }

    [Theory]
    [InlineData("0", "0.00000000001")]
    [InlineData("0", "10000000001")]
    [InlineData("0", "-1")]
    [InlineData("10000000001", "1")]
    [InlineData("0", "10^-20")]
    public void AViewOutOfRangeIsARangeError(string center, string scale)
    {
        // p. 157: 10⁻¹⁰ ≤ Scale ≤ 10¹⁰ and -10¹⁰ ≤ Center ≤ 10¹⁰.
        NumberLine.View(Calculator.Evaluate(center), Calculator.Evaluate(scale)).Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }

    [Theory]
    [InlineData("0.0000000001")]
    [InlineData("10000000000")]
    public void TheEndsOfTheScaleAreInRange(string scale)
    {
        NumberLine.View(Number(0), Calculator.Evaluate(scale)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AViewFitsEveryBoundItIsGiven()
    {
        // Wherever the bounds are, the fitted view shows them all, and its ticks are a nice step.
        decimal[][] cases = [[-1.5m], [0m], [3m, 7m], [-10000000000m, 10000000000m], [0.00012m, 0.0003m], [99m, 101m]];
        foreach (decimal[] bounds in cases)
        {
            NumberLineAxis[] axes = [.. bounds.Select(bound => NumberLine.Define(NumberLineForm.Equal, Number(bound)))];

            NumberLineView view = NumberLine.Fit(axes);

            bounds.Should().OnlyContain(bound => bound >= view.Minimum && bound <= view.Maximum, "the view of {0} shows it", string.Join(", ", bounds));
            decimal mantissa = view.Scale / (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)view.Scale)));
            mantissa.Should().BeOneOf(1m, 2m, 5m);
            (view.Center / view.Scale).Should().Be(decimal.Truncate(view.Center / view.Scale), "the center is on a tick");
        }
    }

    [Theory]
    [InlineData("-1.5", "0.2", "-1.6")]
    [InlineData("0", "0.2", "0")]
    [InlineData("1000", "200", "1000")]
    [InlineData("0.3", "0.2", "0.4")]
    public void ASingleBoundSpansItsDistanceFromZeroOrOne(string bound, string scale, string center)
    {
        // Assumption U29: one bound spans the larger of |a| and 1, over eight ticks; the center is rounded to a tick.
        NumberLineView view = NumberLine.Fit([NumberLine.Define(NumberLineForm.Equal, Calculator.Evaluate(bound))]);

        view.Scale.Should().Be(decimal.Parse(scale, System.Globalization.CultureInfo.InvariantCulture));
        view.Center.Should().Be(decimal.Parse(center, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ASpanOfExactlyEightTicksTakesThatScale()
    {
        // 0 to 1.6 is eight ticks of 0.2 exactly: 0.2 is the smallest scale that spans it, not the next one.
        NumberLineView view = NumberLine.Fit([NumberLine.Define(NumberLineForm.BetweenIncluded, Number(0m), Number(1.6m))]);

        view.Scale.Should().Be(0.2m);
    }

    [Fact]
    public void BoundsAreComparedAsTheDecimalsTheyAre()
    {
        // Seventeen significant digits apart: equal as doubles, and still a below b.
        NumberLine.Define(NumberLineForm.Between, Number(1.0000000000000001m), Number(1.0000000000000002m)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void AViewLeavesOutWhatFailedAndStartsAtZero()
    {
        NumberLineAxis failed = NumberLine.Define(NumberLineForm.Between, Number(5), Number(1));

        NumberLine.Fit([failed]).Should().Be(new NumberLineView(0m, 1m));
        NumberLine.Fit([]).Should().Be(new NumberLineView(0m, 1m));
        ((Action)(() => NumberLine.Fit(null!))).Should().Throw<ArgumentNullException>().WithParameterName("axes");
    }

    [Theory]
    [InlineData("45", "√(2)⌟2", "√(2)⌟2", "1")]
    [InlineData("30", "1⌟2", "√(3)⌟2", "√(3)⌟3")]
    [InlineData("135", "√(2)⌟2", "-√(2)⌟2", "-1")]
    public void AnAngleOnTheUnitCircleHasItsTrigonometricValues(string angle, string sine, string cosine, string tangent)
    {
        // p. 160: sin 45 = √2/2 and cos 30 = √3/2 under MathI/MathO.
        CircleAngle drawn = Session().CircleAngle(CircleKind.UnitCircle, Calculator.Evaluate(angle));

        drawn.Succeeded.Should().BeTrue();
        drawn.Sine!.Display.Text.Should().Be(sine);
        drawn.Cosine!.Display.Text.Should().Be(cosine);
        drawn.Tangent!.Display.Text.Should().Be(tangent);
        drawn.Sine.Input.Should().Be("sin(" + angle + ")");
    }

    [Fact]
    public void AnAngleWithoutATangentIsStillDrawn()
    {
        CircleAngle drawn = Session().CircleAngle(CircleKind.HalfCircle, Number(90));

        drawn.Succeeded.Should().BeTrue();
        drawn.Sine!.Display.Text.Should().Be("1");
        drawn.Tangent!.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnAngleInRadiansKeepsPi()
    {
        CalculatorSession session = Session(AngleUnit.Radian);
        Value angle = Calculator.Evaluate("π÷6", settings: settings => settings with { AngleUnit = AngleUnit.Radian });

        CircleAngle drawn = session.CircleAngle(CircleKind.HalfCircle, angle);

        drawn.Sine!.Display.Text.Should().Be("1⌟2");
        drawn.Cosine!.Display.Text.Should().Be("√(3)⌟2");
    }

    [Theory]
    [InlineData(CircleKind.UnitCircle, AngleUnit.Degree, "-9999.99", true)]
    [InlineData(CircleKind.UnitCircle, AngleUnit.Degree, "10000", false)]
    [InlineData(CircleKind.UnitCircle, AngleUnit.Radian, "-10000", false)]
    [InlineData(CircleKind.UnitCircle, AngleUnit.Gradian, "9999", true)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Degree, "0", true)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Degree, "180", true)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Degree, "180.0001", false)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Degree, "-1", false)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Radian, "π", true)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Radian, "3.2", false)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Gradian, "200", true)]
    [InlineData(CircleKind.HalfCircle, AngleUnit.Gradian, "201", false)]
    public void EachCircleHasItsRangeOfAngles(CircleKind kind, AngleUnit unit, string angle, bool drawn)
    {
        // p. 159.
        CircleAngle result = Session(unit).CircleAngle(kind, Calculator.Evaluate(angle));

        result.Succeeded.Should().Be(drawn);
        if (!drawn)
        {
            result.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
            result.Sine.Should().BeNull();
            result.Cosine.Should().BeNull();
            result.Tangent.Should().BeNull();
        }
    }

    [Fact]
    public void ACircleIsNotAnAnswer()
    {
        CalculatorSession session = Session();
        session.Calculate("7");

        session.CircleAngle(CircleKind.UnitCircle, Number(30));
        session.Clock(3);

        session.Ans.ToDecimal().Should().Be(7m);
        session.History.Should().ContainSingle();
    }

    [Theory]
    [InlineData(3, "90", "270")]
    [InlineData(9, "90", "270")]
    [InlineData(6, "180", "180")]
    [InlineData(12, "0", "360")]
    [InlineData(1, "30", "330")]
    public void TheClockGivesTheAnglesBetweenItsHands(int hour, string smaller, string larger)
    {
        // p. 161: at 3:00, θ1 = 90 and θ2 = 270.
        ClockAngles clock = Session().Clock(hour);

        clock.Hour.Should().Be(hour);
        clock.Smaller.Display.Text.Should().Be(smaller);
        clock.Larger.Display.Text.Should().Be(larger);
    }

    [Theory]
    [InlineData(AngleUnit.Radian, "1⌟2π", "3⌟2π")]
    [InlineData(AngleUnit.Gradian, "100", "300")]
    public void TheClockSpeaksTheAngleUnitOfTheSession(AngleUnit unit, string smaller, string larger)
    {
        // p. 161: in radians under MathO the angles are shown with π.
        ClockAngles clock = Session(unit).Clock(3);

        clock.Smaller.Display.Text.Should().Be(smaller);
        clock.Larger.Display.Text.Should().Be(larger);
    }

    [Fact]
    public void TheCircleNeedsMathBoxAndItsOwnArguments()
    {
        CalculatorSession session = Session();

        ((Action)(() => Calculator.Session().CircleAngle(CircleKind.UnitCircle, Number(1)))).Should().Throw<InvalidOperationException>();
        ((Action)(() => Calculator.Session().Clock(3))).Should().Throw<InvalidOperationException>();
        ((Action)(() => session.CircleAngle((CircleKind)9, Number(1)))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("kind");
        ((Action)(() => session.Clock(0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("hour");
        ((Action)(() => session.Clock(13))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("hour");
    }

    [Fact]
    public void MathBoxCalculatesAsCalculateDoes()
    {
        // The Circle cases of the conformance data calculate sin(45) in Math Box.
        Session().Calculate("sin(45)").Display.Text.Should().Be("√(2)⌟2");
    }
}
