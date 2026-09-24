// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using AwesomeAssertions.Equivalency;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The Math Box screens: the menu, Dice Roll and Coin Toss, the Number Line and the Circle (manual pp. 146-161).
/// </summary>
public sealed class MathBoxViewModelTests
{
    private static MathBoxViewModel Screen(CalculatorSession? session = null) => new(session ?? Shell.Session(CalculatorApp.MathBox));

    [Fact]
    public void TheMenuOpensEachTool()
    {
        MathBoxViewModel screen = Screen();
        screen.IsMenu.Should().BeTrue();
        screen.Current.Should().BeNull();

        screen.Open(MathBoxTool.DiceRoll);
        screen.Current.Should().BeSameAs(screen.Dice);
        screen.Open(MathBoxTool.CoinToss);
        screen.Current.Should().BeSameAs(screen.Coins);
        screen.Open(MathBoxTool.NumberLine);
        screen.Current.Should().BeSameAs(screen.NumberLine);
        screen.Open(MathBoxTool.Circle);
        screen.Current.Should().BeSameAs(screen.Circle);
        screen.IsMenu.Should().BeFalse();

        screen.Back();

        screen.IsMenu.Should().BeTrue();
        screen.Tools.Should().Equal(MathBoxTool.DiceRoll, MathBoxTool.CoinToss, MathBoxTool.NumberLine, MathBoxTool.Circle);
    }

    [Fact]
    public void ADiceRollStartsWithOneDieAndFiveAttempts()
    {
        // p. 147: the parameter screen offers Dice 1, Attempts 5, Same Result Off.
        SimulationViewModel dice = Screen().Dice;

        dice.Count.Should().Be(1);
        dice.Attempts[0, 0].Text.Should().Be("5");
        dice.SameResult.Should().Be(SameResult.Off);

        dice.Execute();

        dice.HasResult.Should().BeTrue();
        dice.Rows.Should().HaveCount(5);
        dice.Columns.Should().Equal("A");
    }

    [Fact]
    public void TwoDiceShowTheirSumAndDifference()
    {
        // p. 149: A, B, Sum and Diff.
        SimulationViewModel dice = Screen().Dice;
        dice.Count = 2;

        dice.Execute();

        dice.Columns.Should().Equal("A", "B", "Sum", "Diff");
        foreach (SimulationRow row in dice.Rows)
        {
            int a = int.Parse(row.Cells[0], System.Globalization.CultureInfo.InvariantCulture);
            int b = int.Parse(row.Cells[1], System.Globalization.CultureInfo.InvariantCulture);
            row.Cells[2].Should().Be((a + b).ToString(System.Globalization.CultureInfo.InvariantCulture));
            row.Cells[3].Should().Be(Math.Abs(a - b).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        dice.Rows.Select(row => row.Number).Should().Equal(1, 2, 3, 4, 5);
        dice.Tallies.Should().Equal(SimulationTally.Sum, SimulationTally.Difference);
    }

    [Fact]
    public void ThreeDiceShowTheirSumOnly()
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Count = 3;

        dice.Execute();

        dice.Columns.Should().Equal("A", "B", "C", "Sum");
        dice.Tallies.Should().Equal(SimulationTally.Sum);
    }

    [Fact]
    public void CoinsAreDrawnAsTheCalculatorDrawsThem()
    {
        // p. 153: ● for heads and ○ for tails, and the number of heads for two or three coins.
        SimulationViewModel coins = Screen().Coins;
        coins.Count = 3;

        coins.Execute();

        coins.Columns.Should().Equal("A", "B", "C", SimulationViewModel.Heads);
        coins.Rows.Should().OnlyContain(row => row.Cells.Take(3).All(cell => cell == SimulationViewModel.Heads || cell == SimulationViewModel.Tails));
        coins.Rows.Should().OnlyContain(row => row.Cells[3] == row.Cells.Take(3).Count(cell => cell == SimulationViewModel.Heads).ToString(System.Globalization.CultureInfo.InvariantCulture));
        coins.Tallies.Should().Equal(SimulationTally.Heads);
        coins.OutcomeHeading.Should().Be("Side");
        coins.Frequencies.Select(row => row.Outcome).Should().Equal("●×0", "●×1", "●×2", "●×3");
    }

    [Fact]
    public void OneCoinCountsTailsAndHeads()
    {
        SimulationViewModel coins = Screen().Coins;

        coins.Execute();

        coins.Columns.Should().Equal("A");
        coins.Frequencies.Select(row => row.Outcome).Should().Equal(SimulationViewModel.Tails, SimulationViewModel.Heads);
    }

    [Fact]
    public void ARelativeFrequencyIsShownAsADecimal()
    {
        // p. 150 writes 0.184, where MathI/MathO would write a fraction.
        SimulationViewModel dice = Screen().Dice;

        dice.Execute();

        dice.OutcomeHeading.Should().Be("Sum");
        dice.Frequencies.Should().HaveCount(6);
        dice.Frequencies.Sum(row => row.Frequency).Should().Be(5);
        dice.Frequencies.Should().OnlyContain(row => row.RelativeFrequency == (row.Frequency / 5m).ToString(System.Globalization.CultureInfo.InvariantCulture));
        dice.Selected.Should().Be(dice.Frequencies[0]);
    }

    [Fact]
    public void TheDifferenceOfTwoDiceIsCountedWhenAsked()
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Count = 2;
        dice.Execute();

        dice.Tally = SimulationTally.Difference;

        dice.OutcomeHeading.Should().Be("Diff");
        dice.Frequencies.Select(row => row.Outcome).Should().Equal("0", "1", "2", "3", "4", "5");
    }

    [Fact]
    public void ARelativeFrequencyIsStoredInAVariable()
    {
        // p. 149: [A=] > [Store] puts the highlighted Rel Fr in A.
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        SimulationViewModel dice = Screen(session).Dice;
        dice.Execute();
        dice.Selected = dice.Frequencies[2];

        dice.StoreIn(MemoryVariable.A);

        session.GetVariable(MemoryVariable.A).Should().Be(dice.Frequencies[2].Value);
        dice.Stored.Should().Be(MemoryVariable.A);
    }

    [Fact]
    public void NothingIsStoredBeforeASimulation()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        session.SetVariable(MemoryVariable.B, Value.FromDecimal(7m));

        Screen(session).Dice.StoreIn(MemoryVariable.B);

        session.GetVariable(MemoryVariable.B).ToDecimal().Should().Be(7m);
    }

    [Theory]
    [InlineData("251", "error.RangeError")]
    [InlineData("0", "error.RangeError")]
    [InlineData("2.5", "error.RangeError")]
    [InlineData("5+", "error.SyntaxError")]
    public void AttemptsThatAreNotOneTo250AreAnError(string attempts, string errorKey)
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Attempts[0, 0].Text = attempts;

        dice.Execute();

        dice.HasResult.Should().BeFalse();
        dice.ErrorKey.Should().Be(errorKey);
        dice.Rows.Should().BeEmpty();
        dice.Frequencies.Should().BeEmpty();
    }

    [Fact]
    public void AttemptsAreCalculatedAsTyped()
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Attempts[0, 0].Text = "2×10";

        dice.Execute();

        dice.Rows.Should().HaveCount(20);
    }

    [Fact]
    public void APresetShowsEveryoneTheSameAttempts()
    {
        // p. 150: two calculators - two sessions - with #1 show one result.
        SimulationViewModel first = Screen(Shell.Session(CalculatorApp.MathBox)).Dice;
        SimulationViewModel second = Screen(PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.MathBox)).Dice;
        first.SameResult = second.SameResult = SameResult.First;

        first.Execute();
        second.Execute();

        second.Rows.Should().BeEquivalentTo(first.Rows, options => options.WithStrictOrdering());
    }

    [Fact]
    public void ChangingAParameterClearsTheResult()
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Execute();

        dice.Count = 2;

        dice.HasResult.Should().BeFalse();
        dice.Rows.Should().BeEmpty();

        dice.Execute();
        dice.SameResult = SameResult.Second;
        dice.HasResult.Should().BeFalse();
    }

    [Fact]
    public void TheManualsNumberLines()
    {
        // p. 154: x≤-1.5 on A, x>-1.0 on B and -2.0<x≤-0.5 on C, drawn with Scale 0.2 and Center -1.2 (p. 156).
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.LessOrEqual, "-1.5");
        Register(line.Entries[1], NumberLineForm.Greater, "-1.0");
        Register(line.Entries[2], NumberLineForm.ToIncluded, "-2.0", "-0.5");

        line.Execute();

        line.IsDrawn.Should().BeTrue();
        line.ErrorKey.Should().BeNull();
        line.Axes.Should().HaveCount(3);
        (line.View.Scale, line.View.Center).Should().Be((0.2m, -1.2m));
        line.SelectedText.Should().Be("A:x≤-1.5");
        line.ScaleLabels.Should().Equal("-2.8", "-1.2", "0.4");
        (line.Window[0, 0].Text, line.Window[0, 1].Text).Should().Be(("0.2", "-1.2"), "the View-Window shows the view in effect, in decimals (p. 156)");

        // Across the view from -2.8 to 0.4: -1.5 is 13/32 of the way, -1.0 is 9/16, -2.0 is 1/4 and -0.5 is 23/32.
        line.Bars.Should().HaveCount(3);
        line.Bars[0].Should().BeEquivalentTo(new { Lower = (double?)null, Upper = 13d / 32, UpperIncluded = true }, Nearly);
        line.Bars[1].Should().BeEquivalentTo(new { Lower = 9d / 16, LowerIncluded = false, Upper = (double?)null }, Nearly);
        line.Bars[2].Should().BeEquivalentTo(new NumberLineBar(1d / 4, false, 23d / 32, true), Nearly);

        line.Next();
        line.SelectedText.Should().Be("B:x>-1.0", "a bound is written as it was typed (p. 154)");
        line.Next();
        line.SelectedText.Should().Be("C:-2.0<x≤-0.5");
        line.Next();
        line.SelectedText.Should().Be("A:x≤-1.5", "▼ from the last axis comes back to the first");
        line.Previous();
        line.SelectedText.Should().Be("C:-2.0<x≤-0.5");
    }

    [Fact]
    public void AnAxisLeftEmptyIsNotDrawn()
    {
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[1], NumberLineForm.Equal, "2");

        line.Execute();

        line.Axes.Should().ContainSingle();
        line.SelectedText.Should().Be("B:x=2");
    }

    [Fact]
    public void AViewWindowSetByHandStaysUntilViewReset()
    {
        // pp. 156-157: Scale 1 and Center 2 show the axis from -6 to 10; View-Reset goes back to the fitted view.
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.LessOrEqual, "-1.5");
        line.Execute();
        line.Window[0, 0].Text = "1";
        line.Window[0, 1].Text = "2";

        line.ApplyWindow();
        line.Execute();

        (line.View.Minimum, line.View.Maximum).Should().Be((-6m, 10m));

        line.ResetWindow();

        line.View.Should().Be(NumberLine.Fit(line.Axes));
        line.Window[0, 0].Value.ToDecimal().Should().Be(line.View.Scale);
        line.Window[0, 1].Value.ToDecimal().Should().Be(line.View.Center);
    }

    [Fact]
    public void AViewTooWideForDecimalsIsWrittenAsAPowerOfTen()
    {
        // The widest view there is: a bound of 10^10 fits a scale of 2×10^9, which the cell reads back as it is written.
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.Less, "1×10^10");

        line.Execute();

        line.View.Scale.Should().Be(2000000000m);
        line.Window[0, 0].HasError.Should().BeFalse();
        line.Window[0, 0].Value.ToDecimal().Should().Be(line.View.Scale);
        line.Window[0, 1].Value.ToDecimal().Should().Be(line.View.Center);
    }

    [Theory]
    [InlineData("0", "2", "error.RangeError")]
    [InlineData("1", "1+", "error.SyntaxError")]
    public void AViewWindowOutOfRangeIsAnError(string scale, string center, string errorKey)
    {
        NumberLineViewModel line = Screen().NumberLine;
        (line.Window[0, 0].Value.ToDecimal(), line.Window[0, 1].Value.ToDecimal()).Should().Be((1m, 0m), "nothing drawn is the view of 0 with Scale 1");
        line.Window[0, 0].Text = scale;
        line.Window[0, 1].Text = center;

        line.ApplyWindow();

        line.ErrorKey.Should().Be(errorKey);
        line.View.Should().Be(new NumberLineView(0m, 1m));
    }

    [Theory]
    [InlineData("10", "5", "error.RangeError")]
    [InlineData("1+", "5", "error.SyntaxError")]
    public void AnExpressionOutOfRangeIsAnErrorAndNothingIsDrawn(string a, string b, string errorKey)
    {
        // p. 165: 10<x≤5 is out of range.
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.Equal, "0");
        Register(line.Entries[1], NumberLineForm.ToIncluded, a, b);

        line.Execute();

        line.ErrorKey.Should().Be(errorKey);
        line.IsDrawn.Should().BeFalse();
    }

    [Fact]
    public void ANewAngleUnitClearsEveryExpression()
    {
        // p. 155: changing the Angle Unit in SETTINGS deletes every registered expression.
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        NumberLineViewModel line = Screen(session).NumberLine;
        Register(line.Entries[0], NumberLineForm.Less, "3");
        line.Execute();

        line.Refresh();
        line.IsDrawn.Should().BeTrue("the settings are as they were");

        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };
        line.Refresh();

        line.IsDrawn.Should().BeFalse();
        line.Entries.Should().OnlyContain(entry => entry.Form == null);
    }

    [Fact]
    public void AnExpressionIsDeleted()
    {
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.Between, "1", "2");

        line.Entries[0].Clear();

        line.Entries[0].Form.Should().BeNull();
        line.Entries[0].Bounds[0, 0].Text.Should().Be("0");
        line.Entries[0].Written().Should().BeEmpty();
        ((Action)(() => line.Entries[0].Define())).Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(NumberLineForm.Less, "x<a")]
    [InlineData(NumberLineForm.LessOrEqual, "x≤a")]
    [InlineData(NumberLineForm.Equal, "x=a")]
    [InlineData(NumberLineForm.Greater, "x>a")]
    [InlineData(NumberLineForm.GreaterOrEqual, "x≥a")]
    [InlineData(NumberLineForm.Between, "a<x<b")]
    [InlineData(NumberLineForm.FromIncluded, "a≤x<b")]
    [InlineData(NumberLineForm.ToIncluded, "a<x≤b")]
    [InlineData(NumberLineForm.BetweenIncluded, "a≤x≤b")]
    public void EveryFormIsWrittenAsTheCalculatorsListWritesIt(NumberLineForm form, string text)
    {
        NumberLineViewModel.FormText(form).Should().Be(text);
        Screen().NumberLine.Entries[0].Forms.Should().Contain(new NumberLineFormChoice(form, text));
    }

    [Fact]
    public void ABoundIsWrittenAsTyped()
    {
        // The a of abs( and the b of the bound are not the form's a and b.
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[2], NumberLineForm.BetweenIncluded, " abs(-1) ", "b");

        line.Entries[2].Written().Should().Be("abs(-1)≤x≤b");
        NumberLineViewModel.Write(NumberLineForm.Less, "tan(45)", "ignored").Should().Be("x<tan(45)");
    }

    [Fact]
    public void AnAngleOnTheUnitCircle()
    {
        // pp. 157, 160: θ1 = 45 gives sin √2/2, cos √2/2 and tan 1.
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "45";

        circle.Execute();

        circle.IsDrawn.Should().BeTrue();
        circle.Lines.Select(line => (line.Name, line.Text)).Should().Equal(
            ("sinθ1", "√(2)⌟2"), ("cosθ1", "√(2)⌟2"), ("tanθ1", "1"), ("θ1", "45"));
        circle.Lines.Should().OnlyContain(line => line.Latex.Length > 0, "every value is drawn by WpfMath");
        circle.FirstDirection.Should().Be(45d);
        circle.SecondDirection.Should().BeNull();
    }

    [Fact]
    public void TwoAnglesAreSelectedInTurn()
    {
        // p. 160: ▲ and ▼ choose between θ1 and θ2.
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "45";
        circle.Angles[0, 1].Text = "135";
        circle.Execute();

        circle.Down();
        circle.Lines[0].Name.Should().Be("sinθ2");
        circle.Lines[1].Text.Should().Be("-√(2)⌟2");

        circle.Up();
        circle.Lines[0].Name.Should().Be("sinθ1");
        circle.SecondDirection.Should().Be(135d);
    }

    [Fact]
    public void θ2AloneIsDrawnAlone()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 1].Text = "30";

        circle.Execute();

        circle.First.Should().BeNull();
        circle.Selected.Should().Be(1);
        circle.Lines[0].Should().Be(circle.Lines[0] with { Name = "sinθ2", Text = "1⌟2" });
        circle.Up();
        circle.Selected.Should().Be(1, "there is no θ1 to select");
    }

    [Fact]
    public void NothingTypedIsNothingDrawn()
    {
        CircleViewModel circle = Screen().Circle;

        circle.Execute();

        circle.IsDrawn.Should().BeFalse();
        circle.Lines.Should().BeEmpty();
        circle.Down();
        circle.Selected.Should().Be(0);
    }

    [Fact]
    public void AValueThatDoesNotExistSaysSo()
    {
        // Assumption U30: tan 90° is a Math ERROR, and the angle is still drawn.
        CircleViewModel circle = Screen().Circle;
        circle.Screen = CircleScreen.HalfCircle;
        circle.Angles[0, 0].Text = "90";

        circle.Execute();

        circle.Lines[2].ErrorKey.Should().Be("error.MathError");
        circle.Lines[0].Text.Should().Be("1");
    }

    [Theory]
    [InlineData(CircleScreen.HalfCircle, "181", "error.RangeError")]
    [InlineData(CircleScreen.UnitCircle, "10000", "error.RangeError")]
    [InlineData(CircleScreen.UnitCircle, "4×", "error.SyntaxError")]
    public void AnAngleOutOfItsCircleIsAnError(CircleScreen type, string angle, string errorKey)
    {
        // p. 159.
        CircleViewModel circle = Screen().Circle;
        circle.Screen = type;
        circle.Angles[0, 0].Text = "30";
        circle.Angles[0, 1].Text = angle;

        circle.Execute();

        circle.ErrorKey.Should().Be(errorKey);
        circle.IsDrawn.Should().BeFalse("an error draws neither angle");
    }

    [Fact]
    public void TheClockStartsAtTwelveAndTurnsHourByHour()
    {
        // p. 161: ▲ moves the hour hand on by an hour, ▼ back.
        CircleViewModel circle = Screen().Circle;
        circle.Screen = CircleScreen.Clock;

        circle.IsClock.Should().BeTrue();
        circle.ClockText.Should().Be("12:00");
        circle.Lines.Select(line => line.Text).Should().Equal("0", "360");

        circle.Up();
        circle.ClockText.Should().Be("1:00");
        circle.Up();
        circle.Up();
        circle.ClockText.Should().Be("3:00");
        circle.Lines.Select(line => (line.Name, line.Text)).Should().Equal(("θ1", "90"), ("θ2", "270"));

        circle.Down();
        circle.Down();
        circle.Down();
        circle.ClockText.Should().Be("12:00");
        circle.Down();
        circle.ClockText.Should().Be("11:00");
    }

    [Fact]
    public void AnotherTypeClearsTheDrawing()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "45";
        circle.Execute();

        circle.Screen = CircleScreen.HalfCircle;

        circle.IsDrawn.Should().BeFalse();
        circle.Execute();
        circle.IsDrawn.Should().BeTrue("the angle typed is still there");
    }

    [Fact]
    public void TheCircleFollowsTheAngleUnit()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        CircleViewModel circle = Screen(session).Circle;
        circle.Angles[0, 0].Text = "π÷6";
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        circle.Execute();

        circle.Lines[0].Text.Should().Be("1⌟2");
        circle.FirstDirection.Should().BeApproximately(30d, 1e-9);
        circle.Screen = CircleScreen.Clock;
        circle.Up();
        circle.Up();
        circle.Up();
        circle.Refresh();
        circle.Lines[0].Text.Should().Be("1⌟2π");
    }

    [Fact]
    public void TheShellBuildsMathBoxOnceAndRefreshesItOnTheWayIn()
    {
        CalculatorShellViewModel shell = Shell.Create();
        shell.Open(CalculatorApps.Of(CalculatorApp.MathBox));
        MathBoxViewModel screen = shell.MathBox;
        Register(screen.NumberLine.Entries[0], NumberLineForm.Less, "1");
        screen.NumberLine.Execute();

        shell.Settings.AngleUnit = AngleUnit.Gradian;
        shell.Open(CalculatorApps.Of(CalculatorApp.MathBox));

        shell.MathBox.Should().BeSameAs(screen);
        screen.NumberLine.IsDrawn.Should().BeFalse("a new angle unit clears the number lines (p. 155)");
    }

    [Fact]
    public void ASessionIsRequired()
    {
        ((Action)(() => _ = new MathBoxViewModel(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _ = new SimulationViewModel(null!, SimulationKind.DiceRoll))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _ = new NumberLineViewModel(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => _ = new CircleViewModel(null!))).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TwoCoinsCountTheirHeads()
    {
        SimulationViewModel coins = Screen().Coins;
        coins.Count = 2;

        coins.Execute();

        coins.Columns.Should().Equal("A", "B", SimulationViewModel.Heads);
        coins.Rows.Should().OnlyContain(row => row.Cells.Length == 3
            && row.Cells[2] == row.Cells.Take(2).Count(cell => cell == SimulationViewModel.Heads).ToString(System.Globalization.CultureInfo.InvariantCulture));
        coins.Frequencies.Select(row => row.Outcome).Should().Equal("●×0", "●×1", "●×2");
    }

    [Fact]
    public void OneCoinIsOneCell()
    {
        SimulationViewModel coins = Screen().Coins;

        coins.Execute();

        coins.Rows.Should().OnlyContain(row => row.Cells.Length == 1, "one coin has no heads to count");
    }

    [Fact]
    public void AnErrorIsNamedEachTimeItIsExecuted()
    {
        SimulationViewModel dice = Screen().Dice;
        dice.Attempts[0, 0].Text = "5+";
        dice.Execute();
        List<string?> changed = dice.Changes();

        dice.Execute();

        changed.Should().Contain(nameof(SimulationViewModel.ErrorKey));
        dice.ErrorKey.Should().Be("error.SyntaxError", "the cell's own error comes before the Range ERROR of its zero");
    }

    [Fact]
    public void ANewNumberFormatKeepsTheSelectedRow()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        MathBoxViewModel box = Screen(session);
        box.Dice.Execute();
        SimulationFrequencyRow chosen = box.Dice.Frequencies.Last(row => row.Frequency is > 0 and < 5);
        box.Dice.Selected = chosen;
        List<string?> dice = box.Dice.Changes();
        List<string?> coins = box.Coins.Changes();

        session.Settings = session.Settings with { DecimalMark = DecimalMark.Comma };
        box.Refresh();

        box.Dice.Selected!.Outcome.Should().Be(chosen.Outcome);
        box.Dice.Selected.RelativeFrequency.Should().Be(chosen.RelativeFrequency.Replace('.', ','));
        dice.Should().Contain(nameof(SimulationViewModel.Frequencies));
        coins.Should().Contain(nameof(SimulationViewModel.Frequencies));
    }

    [Fact]
    public void ADifferenceStoredIsADifferenceShown()
    {
        // Showing the differences shows another list: the row a store takes is one of them, not a sum left selected.
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        SimulationViewModel dice = Screen(session).Dice;
        dice.Count = 2;
        dice.Execute();
        dice.Selected = dice.Frequencies[3];

        dice.Tally = SimulationTally.Difference;
        dice.StoreIn(MemoryVariable.C);

        dice.Selected.Should().Be(dice.Frequencies[0]);
        session.GetVariable(MemoryVariable.C).Should().Be(dice.Frequencies[0].Value);
    }

    [Fact]
    public void RefreshingTheClockRewritesItsAngles()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Screen = CircleScreen.Clock;
        List<string?> changed = circle.Changes();

        circle.Refresh();

        changed.Should().Contain([nameof(CircleViewModel.Clock), nameof(CircleViewModel.Lines)]);
    }

    [Fact]
    public void NothingDrawnHasNothingToSelect()
    {
        NumberLineViewModel line = Screen().NumberLine;

        line.Next();
        line.Previous();

        line.Selected.Should().Be(0);
        line.SelectedText.Should().BeEmpty();
        line.Bars.Should().BeEmpty();
    }

    [Fact]
    public void ClearingTheNumberLineGoesBackToTheFirstView()
    {
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.LessOrEqual, "-1.5");
        line.Execute();

        line.Clear();

        line.View.Should().Be(new NumberLineView(0m, 1m));
        (line.Window[0, 0].Text, line.Window[0, 1].Text).Should().Be(("1", "0"));
        line.SelectedText.Should().BeEmpty();
        line.Entries[0].Form.Should().BeNull();
    }

    [Fact]
    public void AnExpressionBetweenTwoBoundsHasBoth()
    {
        NumberLineViewModel line = Screen().NumberLine;
        Register(line.Entries[0], NumberLineForm.Between, "1", "2");
        line.Entries[1].Form = NumberLineForm.GreaterOrEqual;

        line.Entries[0].HasUpperBound.Should().BeTrue();
        line.Entries[1].HasUpperBound.Should().BeFalse();
        line.Execute();

        line.SelectedText.Should().Be("A:1<x<2");
        line.Bars[0].Should().Match<NumberLineBar>(bar => bar.Lower != null && bar.Upper != null && !bar.LowerIncluded && !bar.UpperIncluded);
    }

    [Fact]
    public void RefreshingTheNumberLineRelabelsItsAxis()
    {
        NumberLineViewModel line = Screen().NumberLine;
        List<string?> changed = line.Changes();

        line.Refresh();

        changed.Should().Contain(nameof(NumberLineViewModel.ScaleLabels));
    }

    [Fact]
    public void TheUnitCircleTakesAnAngleTheHalfCircleDoesNot()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "270";

        circle.Execute();

        circle.Lines[0].Text.Should().Be("-1");
        circle.FirstDirection.Should().Be(270d);
    }

    [Theory]
    [InlineData("4×", "181", "error.SyntaxError")]
    [InlineData("181", "4×", "error.RangeError")]
    public void TheFirstErrorIsTheOneNamed(string first, string second, string errorKey)
    {
        CircleViewModel circle = Screen().Circle;
        circle.Screen = CircleScreen.HalfCircle;
        circle.Angles[0, 0].Text = first;
        circle.Angles[0, 1].Text = second;

        circle.Execute();

        circle.ErrorKey.Should().Be(errorKey);
    }

    [Fact]
    public void TheClockDrawsNoAngleThatWasTyped()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "45";
        circle.Screen = CircleScreen.Clock;

        circle.Execute();

        circle.IsDrawn.Should().BeFalse();
        circle.Lines.Select(line => line.Name).Should().Equal("θ1", "θ2");
    }

    [Fact]
    public void ANewNumberFormatRedrawsTheCircle()
    {
        // The same angle, written as the Input/Output setting in effect when the application is opened again.
        CalculatorSession session = Shell.Session(CalculatorApp.MathBox);
        MathBoxViewModel box = Screen(session);
        box.Circle.Angles[0, 0].Text = "45";
        box.Circle.Execute();
        List<string?> changed = box.Circle.Changes();

        session.Settings = session.Settings with { InputOutput = InputOutput.LineIDecimalO };
        box.Refresh();

        box.Circle.Lines[0].Text.Should().Be("0.7071067812");
        changed.Should().Contain([nameof(CircleViewModel.Clock), nameof(CircleViewModel.Lines)]);
    }

    [Fact]
    public void AnAngleTypedButNotExecutedIsNotDrawnByARefresh()
    {
        CircleViewModel circle = Screen().Circle;
        circle.Angles[0, 0].Text = "45";

        circle.Refresh();

        circle.IsDrawn.Should().BeFalse();
    }

    // A position across the view is a pixel, not a result: -1.5 + 2.8 is not 1.3 in binary.
    private static EquivalencyOptions<T> Nearly<T>(EquivalencyOptions<T> options) =>
        options.Using<double>(context => context.Subject.Should().BeApproximately(context.Expectation, 1e-12)).WhenTypeIs<double>();

    private static void Register(NumberLineEntryViewModel entry, NumberLineForm form, string a, string? b = null)
    {
        entry.Form = form;
        entry.Bounds[0, 0].Text = a;
        if (b is not null)
        {
            entry.Bounds[0, 1].Text = b;
        }
    }
}
