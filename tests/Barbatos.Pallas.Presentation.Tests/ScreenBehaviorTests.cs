// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// What a screen does with what it is given: a setting that is already set, an answer that is not there, a size that
/// is out of range, and a sheet that waits to be calculated again.
/// </summary>
public sealed class ScreenBehaviorTests
{
    [Fact]
    public void TheBaseIsSetOnceAndSaysSo()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.BaseN);
        BaseNViewModel screen = new(session);
        List<string?> changed = screen.Changes();

        screen.Mode = NumberBase.Hex;

        changed.Should().ContainSingle().Which.Should().Be(nameof(BaseNViewModel.Mode));
        session.Settings.BaseMode.Should().Be(NumberBase.Hex);

        changed.Clear();
        screen.Mode = NumberBase.Hex;

        changed.Should().BeEmpty("the calculator was already counting in that base");
    }

    [Fact]
    public void ADistributionThatFailsShowsNoNumberForThatValue()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        screen.Arguments[0, 0].Text = "5";
        screen.Arguments[0, 1].Text = "0.5";
        screen.SetValueCount(2);
        screen.Values[0, 0].Text = "1";
        screen.Values[1, 0].Text = "-1";

        screen.Execute();

        screen.Results.Should().HaveCount(2);
        screen.Results[0].Text.Should().NotBeEmpty();
        screen.Results[1].Text.Should().BeEmpty("x below zero is no number of successes");
        screen.ErrorKey.Should().NotBeNull();
    }

    [Fact]
    public void ADistributionThatTakesOneValueDropsTheRestWhenItIsChosen()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        screen.SetValueCount(3);
        screen.Values.Rows.Should().Be(3, "a binomial is calculated for as many values as are typed");

        screen.Kind = DistributionKind.NormalPD;

        screen.TakesOneValue.Should().BeTrue("the normal distributions take one x (p. 96)");
        screen.Values.Rows.Should().Be(1);
        screen.Arguments.Columns.Should().Be(2);
    }

    [Fact]
    public void SimultaneousEquationsThatEveryPairSatisfiesShowNoNumbers()
    {
        // x + y = 1 and 2x + 2y = 2 are the same equation twice (p. 116).
        EquationViewModel screen = new(Shell.Session(CalculatorApp.Equation));
        screen.Grid[0, 0].Text = "1";
        screen.Grid[0, 1].Text = "1";
        screen.Grid[0, 2].Text = "1";
        screen.Grid[1, 0].Text = "2";
        screen.Grid[1, 1].Text = "2";
        screen.Grid[1, 2].Text = "2";

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.InfiniteSolutions);
        screen.HasSolution.Should().BeTrue("there is an answer to show, even with no numbers in it");
        screen.Solutions.Should().BeEmpty();
    }

    [Fact]
    public void AComplexRootIsWrittenWithTheSignOfItsImaginaryPart()
    {
        // x² + 1 = 0 has the roots i and -i (p. 118).
        EquationViewModel screen = new(Shell.Session(CalculatorApp.Equation));
        screen.Kind = EquationKind.Polynomial;
        screen.Grid[0, 0].Text = "1";
        screen.Grid[0, 1].Text = "0";
        screen.Grid[0, 2].Text = "1";

        screen.Solve();

        screen.Solutions[0].Name.Should().Be("x₁");
        screen.Solutions[0].Text.Should().Be("0+1i");
        screen.Solutions[1].Name.Should().Be("x₂");
        screen.Solutions[1].Text.Should().Be("0-1i", "the other root is below the real line, and a minus is written once");
        screen.Solutions[2].Text.Should().Be("0, 1", "the lowest the parabola goes is x = 0, y = 1");
    }

    [Fact]
    public void ADegreeOutOfRangeIsBroughtBackToOneThereIs()
    {
        InequalityViewModel screen = new(Shell.Session(CalculatorApp.Inequality));

        screen.Degree = 9;

        screen.Degree.Should().Be(4, "the calculator solves up to the fourth degree (p. 121)");
        screen.Grid.Columns.Should().Be(5);

        screen.Degree = 0;

        screen.Degree.Should().Be(2);
        screen.Grid.Columns.Should().Be(3);
    }

    [Fact]
    public void AnInequalityOfAnotherDegreeIsStartedAgain()
    {
        InequalityViewModel screen = new(Shell.Session(CalculatorApp.Inequality));
        screen.Grid[0, 0].Text = "1";
        screen.Grid[0, 1].Text = "-2";
        screen.Grid[0, 2].Text = "-3";
        screen.Solve();
        screen.Intervals.Should().NotBeEmpty();

        screen.Degree = 3;

        screen.Grid.Columns.Should().Be(4);
        screen.Grid[0, 0].Value.Should().Be(Value.Zero, "the coefficients of a cubic are not those of a quadratic");
        screen.Intervals.Should().BeEmpty();
        screen.Text.Should().BeEmpty();
        screen.Outcome.Should().BeNull();
        screen.HasSolution.Should().BeFalse();
    }

    [Fact]
    public void TheIntervalsOfAnInequalityAreNumberedFromOne()
    {
        // x² - 2x - 3 > 0 holds for x < -1 and for 3 < x (p. 122).
        InequalityViewModel screen = new(Shell.Session(CalculatorApp.Inequality));
        screen.Grid[0, 0].Text = "1";
        screen.Grid[0, 1].Text = "-2";
        screen.Grid[0, 2].Text = "-3";

        screen.Solve();

        screen.Intervals.Select(interval => interval.Name).Should().Equal("1", "2");
        screen.Intervals.Should().AllSatisfy(interval => interval.Text.Should().NotBeEmpty());
        screen.Text.Should().NotBeEmpty();
    }

    [Fact]
    public void ARatioScreenIsEmptied()
    {
        RatioViewModel screen = new(Shell.Session(CalculatorApp.Ratio));
        screen.Grid[0, 0].Text = "3";
        screen.Grid[0, 1].Text = "8";
        screen.Grid[0, 2].Text = "12";
        screen.Solve();
        screen.HasSolution.Should().BeTrue();

        screen.Clear();

        screen.Grid[0, 0].Value.Should().Be(Value.Zero);
        screen.Grid[0, 2].Value.Should().Be(Value.Zero);
        screen.Solution.Should().BeNull();
        screen.HasSolution.Should().BeFalse();
    }

    [Fact]
    public void AMatrixScreenOpensOnWhatTheSessionAlreadyHolds()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Matrix);
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { Value.FromDecimal(7), Value.FromDecimal(8) } }));

        MatrixViewModel screen = new(session);

        screen.Grid.Rows.Should().Be(1);
        screen.Grid.Columns.Should().Be(2);
        screen.Grid[0, 0].Display.Should().Be("7", "the matrix that is there is the one on the screen");
        screen.Grid[0, 1].Display.Should().Be("8");
        screen.IsDefined.Should().BeTrue();
    }

    [Fact]
    public void AMatrixThatIsDeletedIsOffTheScreenAsWell()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        screen.Grid[0, 0].Text = "5";
        screen.Store();

        screen.Delete();

        screen.Grid[0, 0].Value.Should().Be(Value.Zero);
        screen.IsDefined.Should().BeFalse();
        session.GetMatrix(MatrixVariable.MatA).Should().BeNull();
    }

    [Fact]
    public void ASheetSaysWhenItStopsCalculatingByItself()
    {
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet));
        List<string?> changed = screen.Changes();

        screen.AutoCalculate = false;

        changed.Should().Contain(nameof(SpreadsheetViewModel.AutoCalculate));
        screen.AutoCalculate.Should().BeFalse();
    }

    [Fact]
    public void ASheetThatIsNotCalculatingByItselfShowsTheOldValueUntilItIs()
    {
        // A1 = 1, and with Auto Calc off a formula typed after it waits to be calculated (p. 104).
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet));
        screen.Selected = new CellAddress(0, 0);
        screen.Input = "1";
        screen.Commit();

        screen.AutoCalculate = false;
        screen.Selected = new CellAddress(0, 1);
        screen.Input = "=A1+100";
        screen.Commit();

        screen.Editing.Input.Should().Be("=A1+100", "the formula is in the cell");
        screen.Editing.Text.Should().BeEmpty("but nothing has been calculated");

        screen.Recalculate();

        screen.Editing.Text.Should().Be("101", "and now it has");
    }
}
