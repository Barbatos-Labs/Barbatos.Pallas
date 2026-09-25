// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The screens of the applications: each one fills in the calculator's own form and asks the engine, and what comes
/// back is the manual's own answer.
/// </summary>
public sealed class ApplicationScreenTests
{
    private static CalculatorSession Session(CalculatorApp app) => Shell.Session(app);

    private static void Fill(ValueGridViewModel grid, params string[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            grid.Cells[index].Text = values[index];
        }
    }

    [Fact]
    public void AMatrixIsTypedInAndUsedByName()
    {
        // The manual's own example (p. 139): MatA × MatB.
        CalculatorSession session = Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        Fill(screen.Grid, "2", "1", "1", "1");
        screen.Store();

        screen.Name = MatrixVariable.MatB;
        Fill(screen.Grid, "2", "-1", "-1", "2");
        screen.Store();

        screen.Calculate.Input.Set(MathDocumentReader.Read("MatA×MatB", CalculatorApp.Matrix));
        screen.Calculate.Execute();

        screen.Calculate.Display!.Text.Should().Be("[[3, 0], [1, 1]]");
        screen.Answer.Should().NotBeNull();
        screen.IsDefined.Should().BeTrue();
    }

    [Fact]
    public void AMatrixComesBackWhenItIsChosenAgain()
    {
        CalculatorSession session = Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        Fill(screen.Grid, "5", "6", "7", "8");
        screen.Store();

        screen.Name = MatrixVariable.MatC;
        screen.Grid[0, 0].Text.Should().Be("0", "a matrix that was never stored is empty");

        screen.Name = MatrixVariable.MatA;
        screen.Grid[0, 0].Display.Should().Be("5");
        screen.Grid[1, 1].Display.Should().Be("8");
    }

    [Fact]
    public void AMatrixIsDeletedAndResized()
    {
        CalculatorSession session = Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        screen.Store();

        screen.Resize(3, 4).Should().BeTrue();
        screen.Resize(5, 1).Should().BeFalse("a matrix of the calculator is at most four by four");
        screen.Grid.Rows.Should().Be(3);
        screen.Grid.Columns.Should().Be(4);

        screen.Delete();
        screen.IsDefined.Should().BeFalse();
        screen.TakeAnswer().Should().BeFalse("nothing has been calculated yet");
        screen.Names.Should().HaveCount(4);
    }

    [Fact]
    public void AnAnswerIsCarriedOnFrom()
    {
        CalculatorSession session = Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        Fill(screen.Grid, "1", "2", "3", "4");
        screen.Store();
        screen.Calculate.Input.Set(MathDocumentReader.Read("MatA+MatA", CalculatorApp.Matrix));
        screen.Calculate.Execute();

        screen.TakeAnswer().Should().BeTrue();

        screen.Grid[0, 0].Display.Should().Be("2");
        screen.Grid[1, 1].Display.Should().Be("8");
    }

    [Fact]
    public void AVectorIsTypedInAndUsedByName()
    {
        // The manual's own example (p. 147): the dot product of (1, 2) and (3, 4).
        CalculatorSession session = Session(CalculatorApp.Vector);
        VectorViewModel screen = new(session);
        screen.Resize(2).Should().BeTrue();
        Fill(screen.Grid, "1", "2");
        screen.Store();

        screen.Name = VectorVariable.VctB;
        screen.Resize(2);
        Fill(screen.Grid, "3", "4");
        screen.Store();

        screen.Calculate.Input.Set(MathDocumentReader.Read("VctA•VctB", CalculatorApp.Vector));
        screen.Calculate.Execute();

        screen.Calculate.Display!.Text.Should().Be("11");
    }

    [Fact]
    public void AVectorIsTwoOrThreeElements()
    {
        CalculatorSession session = Session(CalculatorApp.Vector);
        VectorViewModel screen = new(session);

        screen.Resize(3).Should().BeTrue();
        screen.Resize(4).Should().BeFalse("a vector of the calculator has two or three elements");
        screen.Grid.Columns.Should().Be(3);

        Fill(screen.Grid, "1", "1", "1");
        screen.Store();
        screen.IsDefined.Should().BeTrue();
        screen.Delete();
        screen.IsDefined.Should().BeFalse();
    }

    [Fact]
    public void SimultaneousEquationsAreSolvedFromTheirCoefficients()
    {
        // x + 2y = 3, 4x + 5y = 6 (p. 113).
        CalculatorSession session = Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        Fill(screen.Grid, "1", "2", "3", "4", "5", "6");

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.Solved);
        screen.Solutions.Select(line => line.Name).Should().Equal("x", "y");
        screen.Solutions[0].Text.Should().Be("-1");
        screen.Solutions[1].Text.Should().Be("2");
        screen.ErrorKey.Should().BeEmpty();
    }

    [Fact]
    public void APolynomialIsSolvedFromItsCoefficients()
    {
        // x² - 3x + 2 = 0.
        CalculatorSession session = Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        screen.Kind = EquationKind.Polynomial;
        Fill(screen.Grid, "1", "-3", "2");

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.Solved);
        screen.Solutions.Should().HaveCountGreaterThanOrEqualTo(2);
        screen.Solutions[0].Text.Should().Be("2");
        screen.Solutions[1].Text.Should().Be("1");
    }

    [Fact]
    public void TheSizeOfTheCoefficientsFollowsTheKind()
    {
        CalculatorSession session = Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);

        screen.Grid.Rows.Should().Be(2, "two equations in two unknowns");
        screen.Grid.Columns.Should().Be(3, "two coefficients and a constant");

        screen.Size = 3;
        screen.Grid.Rows.Should().Be(3);
        screen.Grid.Columns.Should().Be(4);

        screen.Kind = EquationKind.Polynomial;
        screen.Grid.Rows.Should().Be(1, "a polynomial is one row of coefficients");
        screen.Grid.Columns.Should().Be(4, "a cubic has four");

        screen.Size = 9;
        screen.Size.Should().Be(4, "the calculator solves up to four unknowns, and a polynomial of degree four");
    }

    [Fact]
    public void AnEquationWithNoSolutionSaysSo()
    {
        CalculatorSession session = Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        Fill(screen.Grid, "1", "1", "1", "1", "1", "2");

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.NoSolution);
        screen.Solutions.Should().BeEmpty();
        screen.HasSolution.Should().BeTrue("the screen has something to say, even if it is that there is nothing");
    }

    [Fact]
    public void AnInequalityIsSolvedAndWrittenAsTheCalculatorWritesIt()
    {
        // x² - 2x - 3 > 0 (p. 122).
        CalculatorSession session = Session(CalculatorApp.Inequality);
        InequalityViewModel screen = new(session);
        Fill(screen.Grid, "1", "-2", "-3");
        screen.Relation = RelationOperator.Greater;

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.Solved);
        screen.Text.Should().NotBeEmpty();
        screen.Intervals.Should().HaveCount(2);
        screen.HasSolution.Should().BeTrue();
        screen.ErrorKey.Should().BeNull();
    }

    [Fact]
    public void TheDegreeOfAnInequalityIsTwoToFour()
    {
        CalculatorSession session = Session(CalculatorApp.Inequality);
        InequalityViewModel screen = new(session);

        screen.Grid.Columns.Should().Be(3);
        screen.Degree = 4;
        screen.Grid.Columns.Should().Be(5);
        screen.Degree = 1;
        screen.Degree.Should().Be(2);
        screen.Relations.Should().HaveCount(4);
    }

    [Fact]
    public void ARatioIsSolvedForX()
    {
        // 3:8 = X:12 (p. 133).
        CalculatorSession session = Session(CalculatorApp.Ratio);
        RatioViewModel screen = new(session);
        Fill(screen.Grid, "3", "8", "12");

        screen.Solve();

        screen.HasSolution.Should().BeTrue();
        screen.Solution!.Display.Text.Should().Be("9⌟2", "in MathI/MathO the calculator answers with a fraction");
        screen.HasError.Should().BeFalse();
        screen.ErrorKey.Should().BeNull();
    }

    [Fact]
    public void ARatioThatCannotBeSolvedSaysWhy()
    {
        CalculatorSession session = Session(CalculatorApp.Ratio);
        RatioViewModel screen = new(session);
        Fill(screen.Grid, "3", "0", "12");

        screen.Solve();

        screen.HasError.Should().BeTrue();
        screen.ErrorKey.Should().Be("error.MathError");
        screen.Clear();
        screen.Solution.Should().BeNull();
        screen.Forms.Should().HaveCount(2);
    }

    [Fact]
    public void ADistributionIsCalculatedForEveryValueAtOnce()
    {
        // Binomial PD with N = 5, p = 0.5, for x = 0, 1, 2 (p. 97).
        CalculatorSession session = Session(CalculatorApp.Distribution);
        DistributionViewModel screen = new(session);
        screen.Kind = DistributionKind.BinomialPD;
        Fill(screen.Arguments, "5", "0.5");
        screen.SetValueCount(3);
        Fill(screen.Values, "0", "1", "2");

        screen.Execute();

        screen.Results.Should().HaveCount(3);
        screen.Results[0].Text.Should().Be("0.03125");
        screen.Results[1].Text.Should().Be("0.15625");
        screen.Results[2].Text.Should().Be("0.3125");
        screen.ErrorKey.Should().BeNull();
    }

    [Fact]
    public void EveryDistributionAsksForItsOwnParameters()
    {
        CalculatorSession session = Session(CalculatorApp.Distribution);
        DistributionViewModel screen = new(session);

        screen.Parameters.Should().Equal("distribution.trials", "distribution.probability");
        screen.Kind = DistributionKind.NormalCD;
        screen.Parameters.Should().HaveCount(4);
        screen.Arguments.Columns.Should().Be(4, "the grid follows what the distribution asks for");
        screen.Kind = DistributionKind.PoissonPD;
        screen.Parameters.Should().Equal(["distribution.lambda"], "a Poisson has a mean of its own, λ");
        screen.Kinds.Should().HaveCount(Enum.GetValues<DistributionKind>().Length);
    }

    [Fact]
    public void ATableIsGeneratedFromTheFunctionsAndTheRange()
    {
        // f(x) = x² from 1 to 5, step 1 (p. 108).
        CalculatorSession session = Session(CalculatorApp.Table);
        TableViewModel screen = new(session);
        screen.Type = TableType.FunctionF;
        screen.FunctionF = "x²";

        screen.Generate();

        screen.Rows.Should().HaveCount(5);
        screen.Rows[0].X.Text.Should().Be("1");
        screen.Rows[4].F!.Text.Should().Be("25");
        screen.ErrorKey.Should().BeNull();
        screen.RowLimit.Should().Be(45, "a table of one function holds more rows");
    }

    [Fact]
    public void ARowOfATableIsChangedAndRemoved()
    {
        CalculatorSession session = Session(CalculatorApp.Table);
        TableViewModel screen = new(session);
        screen.Type = TableType.FunctionF;
        screen.FunctionF = "x²";
        screen.Generate();

        screen.SetX(0, Value.FromDecimal(10)).Should().BeTrue();
        screen.Rows[0].F!.Text.Should().Be("100");

        screen.Verify(0, TableFunction.F, "100").Should().BeTrue();
        screen.Verify(0, TableFunction.F, "99").Should().BeFalse();

        screen.RemoveRow(0).Should().BeTrue();
        screen.Rows.Should().HaveCount(4);
        screen.RemoveRow(9).Should().BeFalse();
        screen.SetX(9, Value.One).Should().BeFalse();
    }

    [Fact]
    public void AFunctionThatIsNotSyntaxSaysSo()
    {
        CalculatorSession session = Session(CalculatorApp.Table);
        TableViewModel screen = new(session);
        screen.Type = TableType.FunctionF;
        screen.FunctionF = "x²+";

        screen.Generate();

        screen.ErrorKey.Should().Be("error.SyntaxError");
        screen.Rows.Should().BeEmpty();
    }

    [Fact]
    public void ASheetHoldsConstantsAndFormulas()
    {
        // A1 = 1, A2 = 2, A3 = Sum(A1:A2) (p. 103).
        CalculatorSession session = Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);

        screen.Selected = new CellAddress(0, 0);
        screen.Input = "1";
        screen.Commit();
        screen.Selected = new CellAddress(0, 1);
        screen.Input = "2";
        screen.Commit();
        screen.Selected = new CellAddress(0, 2);
        screen.Input = "=Sum(A1:A2)";
        screen.Commit();

        screen.Editing.Text.Should().Be("3");
        screen.Editing.Input.Should().Be("=Sum(A1:A2)");
        screen.ErrorKey.Should().BeNull();
        screen.UsedBytes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ACellThatReadsItselfIsRefused()
    {
        CalculatorSession session = Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);
        screen.Selected = new CellAddress(0, 0);
        screen.Input = "=A1+1";

        screen.Commit();

        screen.Editing.ErrorKey.Should().Be("error.CircularError");
        screen.Editing.Text.Should().BeEmpty("the error is shown in the place of its value (U34)");
    }

    [Fact]
    public void ASheetIsCopiedFilledAndEmptied()
    {
        CalculatorSession session = Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);
        screen.Selected = new CellAddress(0, 0);
        screen.Input = "5";
        screen.Commit();

        screen.Copy(new CellAddress(0, 0), new CellAddress(1, 0));
        screen.ErrorKey.Should().BeNull();
        screen.Cells.Single(cell => cell.Address == new CellAddress(1, 0)).Text.Should().Be("5");
        screen.Fill("7", new CellAddress(2, 0), new CellAddress(2, 2));
        screen.ErrorKey.Should().BeNull();
        screen.Cells.Count(cell => cell.Text == "7").Should().Be(3);

        screen.AutoCalculate = false;
        screen.AutoCalculate.Should().BeFalse();
        screen.Recalculate();

        screen.ClearCell();
        screen.Editing.Input.Should().BeEmpty();
        screen.ClearAll();
        screen.Cells.Should().OnlyContain(cell => cell.Input.Length == 0);
        screen.UsedBytes.Should().Be(0);
    }

    [Fact]
    public void StatisticsAreWorkedOutFromTheDataOnTheScreen()
    {
        // The manual's own one-variable example (p. 79).
        CalculatorSession session = Session(CalculatorApp.Statistics);
        StatisticsViewModel screen = new(session);
        Fill(screen.Data, "1", "2", "3", "4");

        screen.Apply();

        screen.Summary.Should().NotBeEmpty();
        screen.Summary.First(line => line.Name == "n").Text.Should().Be("4");
        screen.Summary.First(line => line.Name == "x̄").Text.Should().Be("2.5");
        screen.Summary.First(line => line.Name == "Σx").Text.Should().Be("10");
    }

    [Fact]
    public void TwoVariableDataAreFittedWithARegression()
    {
        CalculatorSession session = Session(CalculatorApp.Statistics);
        StatisticsViewModel screen = new(session);
        screen.IsTwoVariable = true;
        screen.Data.Resize(3, 2);
        Fill(screen.Data, "1", "2", "2", "4", "3", "6");

        screen.Regression = RegressionModel.Linear;
        screen.Apply();

        screen.Summary.First(line => line.Name == "b").Text.Should().Be("2", "y is exactly twice x");
        screen.Summary.First(line => line.Name == "r").Text.Should().Be("1");
        session.Regression.Should().Be(RegressionModel.Linear);
    }

    [Fact]
    public void TheColumnsOfTheDataFollowWhatIsShown()
    {
        CalculatorSession session = Session(CalculatorApp.Statistics);
        StatisticsViewModel screen = new(session);

        screen.Data.Columns.Should().Be(1);
        screen.IsTwoVariable = true;
        screen.Data.Columns.Should().Be(2);
        screen.HasFrequencies = true;
        screen.Data.Columns.Should().Be(3);

        screen.AddRow();
        screen.Data.Rows.Should().Be(5);
        screen.RemoveRow();
        screen.Data.Rows.Should().Be(4);

        screen.Clear();
        screen.Summary.Should().BeEmpty();
        session.StatisticsData.Rows.Should().Be(0);
    }

    [Fact]
    public void TheBaseNScreenCountsInTheBaseItIsSetTo()
    {
        CalculatorSession session = Session(CalculatorApp.BaseN);
        BaseNViewModel screen = new(session);

        screen.Mode.Should().Be(NumberBase.Dec);
        screen.Calculate.Input.Set(MathDocumentReader.Read("1111", CalculatorApp.BaseN));
        screen.Calculate.Execute();
        screen.Calculate.Display!.Text.Should().Be("1111");

        screen.Mode = NumberBase.Bin;
        session.Settings.BaseMode.Should().Be(NumberBase.Bin);
        screen.Calculate.Execute();
        screen.Calculate.Display!.Text.Should().Be("1111", "the same digits, now binary");
        screen.Calculate.Calculation!.Result.ToInt32().Should().Be(15);
        screen.Bases.Should().HaveCount(4);
    }

    [Fact]
    public void EveryScreenRefusesToBeBuiltWithoutASession()
    {
        Action matrix = () => _ = new MatrixViewModel(null!);
        Action vector = () => _ = new VectorViewModel(null!);
        Action equation = () => _ = new EquationViewModel(null!);
        Action inequality = () => _ = new InequalityViewModel(null!);
        Action ratio = () => _ = new RatioViewModel(null!);
        Action distribution = () => _ = new DistributionViewModel(null!);
        Action table = () => _ = new TableViewModel(null!);
        Action sheet = () => _ = new SpreadsheetViewModel(null!);
        Action statistics = () => _ = new StatisticsViewModel(null!);
        Action baseN = () => _ = new BaseNViewModel(null!);

        foreach (Action act in new[] { matrix, vector, equation, inequality, ratio, distribution, table, sheet, statistics, baseN })
        {
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
