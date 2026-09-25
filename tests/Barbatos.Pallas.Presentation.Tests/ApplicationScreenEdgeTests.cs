// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The paths through the screens that the manual's own examples do not take: every distribution, every way a form
/// is emptied, and what a screen does when what it is asked for is not there.
/// </summary>
public sealed class ApplicationScreenEdgeTests
{
    private static void Fill(ValueGridViewModel grid, params string[] values)
    {
        for (int index = 0; index < values.Length; index++)
        {
            grid.Cells[index].Text = values[index];
        }
    }

    [Theory]
    // Every distribution, with the parameters it asks for, against the values the manual gives (pp. 96-101).
    [InlineData(DistributionKind.BinomialCD, "5,0.5", "2", "0.5")]
    [InlineData(DistributionKind.NormalPD, "0,1", "0", "0.39894228")]
    [InlineData(DistributionKind.NormalCD, "-1,1,0,1", "0", "0.68268949")]
    [InlineData(DistributionKind.InverseNormal, "0.5,0,1", "0", "0")]
    [InlineData(DistributionKind.PoissonPD, "2", "1", "0.27067057")]
    [InlineData(DistributionKind.PoissonCD, "2", "1", "0.40600585")]
    public void EveryDistributionTakesItsOwnParameters(DistributionKind kind, string arguments, string x, string expected)
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        screen.Kind = kind;
        Fill(screen.Arguments, arguments.Split(','));
        Fill(screen.Values, x);

        screen.Execute();

        screen.Results.Should().ContainSingle();
        screen.Results[0].Text.Should().StartWith(expected[..Math.Min(expected.Length, 6)]);
        screen.ErrorKey.Should().BeNull();
    }

    [Fact]
    public void ADistributionThatCannotBeCalculatedSaysSo()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        screen.Kind = DistributionKind.BinomialPD;
        Fill(screen.Arguments, "5", "2");
        Fill(screen.Values, "1");

        screen.Execute();

        screen.ErrorKey.Should().NotBeNull("a probability above one is not a probability");
    }

    [Fact]
    public void ADistributionScreenIsEmptied()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        Fill(screen.Arguments, "5", "0.5");
        screen.SetValueCount(2);
        Fill(screen.Values, "1", "2");
        screen.Execute();

        screen.Clear();

        screen.Results.Should().BeEmpty();
        screen.Values[0, 0].Value.Should().Be(Value.Zero);
        screen.Arguments[0, 0].Value.Should().Be(Value.Zero);
        screen.ErrorKey.Should().BeNull();
        screen.SetValueCount(0);
        screen.Values.Rows.Should().Be(1, "there is always a value to calculate for");
    }

    [Fact]
    public void AVectorComesBackWhenItIsChosenAgain()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Vector);
        VectorViewModel screen = new(session);
        screen.Resize(2);
        Fill(screen.Grid, "3", "4");
        screen.Store();

        screen.Name = VectorVariable.VctD;
        screen.Grid[0, 0].Value.Should().Be(Value.Zero);

        screen.Name = VectorVariable.VctA;
        screen.Grid.Columns.Should().Be(2, "the vector that was stored had two elements");
        screen.Grid[0, 1].Display.Should().Be("4");
    }

    [Fact]
    public void AVectorAnswerIsCarriedOnFrom()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Vector);
        VectorViewModel screen = new(session);
        screen.Resize(3);
        Fill(screen.Grid, "1", "2", "3");
        screen.Store();
        screen.Calculate.Input.Set(MathDocumentReader.Read("VctA+VctA", CalculatorApp.Vector));
        screen.Calculate.Execute();

        screen.TakeAnswer().Should().BeTrue();

        screen.Grid.Columns.Should().Be(3);
        screen.Grid[0, 2].Display.Should().Be("6");
        screen.Answer.Should().NotBeNull();
    }

    [Fact]
    public void AVectorWithoutAnAnswerTakesNothing()
    {
        VectorViewModel screen = new(Shell.Session(CalculatorApp.Vector));

        screen.TakeAnswer().Should().BeFalse();
        screen.Answer.Should().BeNull();
        screen.Names.Should().HaveCount(4);
    }

    [Fact]
    public void APolynomialWithComplexRootsShowsThem()
    {
        // x² + 1 = 0, with Complex Roots on (p. 118).
        CalculatorSession session = Shell.Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        screen.Kind = EquationKind.Polynomial;
        Fill(screen.Grid, "1", "0", "1");

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.Solved);
        screen.Solutions.Should().HaveCountGreaterThanOrEqualTo(2);
        screen.Solutions[0].Text.Should().Contain("i", "a complex root is written with its imaginary part");
        screen.Solutions[1].Text.Should().Contain("i");
    }

    [Fact]
    public void ACubicShowsItsExtremaToo()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        screen.Kind = EquationKind.Polynomial;
        screen.Size = 3;
        Fill(screen.Grid, "1", "0", "-3", "0");

        screen.Solve();

        screen.Solutions.Select(line => line.Name).Should().Contain(name => name == "Maximum" || name == "Minimum", "a cubic has a maximum and a minimum (p. 119)");
    }

    [Fact]
    public void AnEquationScreenIsEmptied()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Equation);
        EquationViewModel screen = new(session);
        Fill(screen.Grid, "1", "2", "3", "4", "5", "6");
        screen.Solve();

        screen.Clear();

        screen.Solutions.Should().BeEmpty();
        screen.Outcome.Should().BeNull();
        screen.HasSolution.Should().BeFalse();
        screen.Grid[0, 0].Value.Should().Be(Value.Zero);
    }

    [Fact]
    public void AnInequalityEveryNumberSatisfiesSaysSo()
    {
        // x² + 1 > 0 holds for every real x (p. 125).
        CalculatorSession session = Shell.Session(CalculatorApp.Inequality);
        InequalityViewModel screen = new(session);
        Fill(screen.Grid, "1", "0", "1");
        screen.Relation = RelationOperator.Greater;

        screen.Solve();

        screen.Outcome.Should().Be(SolutionOutcome.AllRealNumbers);
        screen.Intervals.Should().BeEmpty();
        screen.Clear();
        screen.Outcome.Should().BeNull();
        screen.Text.Should().BeEmpty();
    }

    [Fact]
    public void ASheetKeepsWhatWasTypedWhenAnotherCellIsChosen()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);
        screen.Selected = new CellAddress(0, 0);
        screen.Input = "12";
        screen.Commit();

        screen.Selected = new CellAddress(1, 0);
        screen.Input.Should().BeEmpty("an empty cell has nothing in it");

        screen.Selected = new CellAddress(0, 0);
        screen.Input.Should().Be("12", "choosing a cell puts what it holds back on the line");
        screen.Rows.Should().BeGreaterThan(0);
        screen.Columns.Should().Be(5);
    }

    [Fact]
    public void ASheetRefusesAFormulaThatIsNotOne()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);
        screen.Selected = new CellAddress(0, 0);
        screen.Input = "=1+";

        screen.Commit();

        screen.ErrorKey.Should().Be("error.SyntaxError");
        screen.Editing.ErrorKey.Should().Be("error.SyntaxError", "the cell keeps what was typed and says what is wrong with it");
        screen.Editing.Text.Should().BeEmpty("and it has no value");
    }

    [Fact]
    public void ACopyAndAFillThatCannotBeDoneSayWhy()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Spreadsheet);
        SpreadsheetViewModel screen = new(session);

        screen.Copy(new CellAddress(0, 0), new CellAddress(1, 1));
        screen.ErrorKey.Should().BeNull("copying an empty cell copies nothing");
        screen.Fill("=1+", new CellAddress(0, 0), new CellAddress(0, 1));
        screen.ErrorKey.Should().Be("error.SyntaxError");
    }

    [Fact]
    public void ATableOfBothFunctionsHoldsFewerRows()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Table);
        TableViewModel screen = new(session);

        screen.Type.Should().Be(TableType.FunctionsFAndG);
        screen.RowLimit.Should().Be(30);
        screen.FunctionF = "x";
        screen.FunctionG = "x+1";
        screen.Generate();

        screen.Rows.Should().HaveCount(5);
        screen.Rows[0].G!.Text.Should().Be("2");
        screen.Types.Should().HaveCount(3);
    }

    [Fact]
    public void AValueOfATableThatFailsShowsItsErrorInItsPlace()
    {
        // U34: where f(x) has no value, the error its calculation ended in is shown where the value would be, and the
        // rest of the row as it is.
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table)) { FunctionF = "1÷x", FunctionG = "x+1" };
        screen.Range[0, 0].Text = "-1";
        screen.Range[0, 1].Text = "1";

        screen.Generate();

        screen.ErrorKey.Should().BeNull("the table is generated all the same");
        screen.Rows.Should().HaveCount(3);
        screen.Rows[0].F.Should().Be(new TableCell("-1", null));
        screen.Rows[1].Should().Be(new TableLine(new TableCell("0", null), new TableCell(string.Empty, "error.MathError"), new TableCell("1", null)));
    }

    [Fact]
    public void ATableOfTooManyRowsIsRefused()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Table);
        TableViewModel screen = new(session);
        screen.Type = TableType.FunctionF;
        screen.FunctionF = "x";
        screen.Range[0, 0].Text = "1";
        screen.Range[0, 1].Text = "1000";
        screen.Range[0, 2].Text = "1";

        screen.Generate();

        screen.ErrorKey.Should().Be("error.RangeError");
        screen.Rows.Should().BeEmpty();
    }

    [Fact]
    public void ATableIsEmptied()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Table);
        TableViewModel screen = new(session);
        screen.Type = TableType.FunctionF;
        screen.Generate();

        screen.Clear();

        screen.Rows.Should().BeEmpty();
        screen.SetX(0, Value.One).Should().BeFalse("there is no table to change");
        screen.RemoveRow(0).Should().BeFalse();
        screen.Verify(0, TableFunction.F, "1").Should().BeNull();
    }

    [Fact]
    public void ARowOutsideTheTableIsNeitherChangedNorRemovedNorVerified()
    {
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table)) { Type = TableType.FunctionF };
        screen.Generate();
        int rows = screen.Rows.Length;

        foreach (int row in (int[])[-1, rows])
        {
            screen.SetX(row, Value.One).Should().BeFalse("row {0} is not in the table", row);
            screen.RemoveRow(row).Should().BeFalse("row {0} is not in the table", row);
            screen.Verify(row, TableFunction.F, "1").Should().BeNull("row {0} is not in the table", row);
        }

        screen.Rows.Should().HaveCount(rows);
        screen.SetX(rows - 1, Value.One).Should().BeTrue("the last row is one");
        screen.Verify(rows - 1, TableFunction.F, "1").Should().BeTrue();
    }

    [Theory]
    [InlineData(TableType.FunctionF)]
    [InlineData(TableType.FunctionG)]
    public void OnlyTheFunctionsOfTheTableAreDefined(TableType type)
    {
        // What is typed for the function the table leaves out is not read, so it cannot be an error of the table.
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table))
        {
            Type = type,
            FunctionF = type is TableType.FunctionF ? "x²" : "1+",
            FunctionG = type is TableType.FunctionG ? "x²" : "1+",
        };

        screen.Generate();

        screen.ErrorKey.Should().BeNull();
        (type is TableType.FunctionF ? screen.Rows[1].F : screen.Rows[1].G)!.Text.Should().Be("4");
        (type is TableType.FunctionF ? screen.Rows[1].G : screen.Rows[1].F).Should().BeNull("the table holds one function");
    }

    [Fact]
    public void AFillWithALeadingEqualsSignIsAFormula()
    {
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet)) { Input = "5" };
        screen.Commit();

        screen.Fill("=A1+1", new CellAddress(1, 0), new CellAddress(1, 1));

        screen.ErrorKey.Should().BeNull();
        screen.Cells.Single(cell => cell.Address == new CellAddress(1, 0)).Should().Be(new SheetCell(new CellAddress(1, 0), "=A1+1", "6", null));
        screen.Cells.Single(cell => cell.Address == new CellAddress(1, 1)).Input.Should().Be("=A2+1", "a filled formula's references move with it");
        screen.Invoking(sheet => sheet.Fill(null!, new CellAddress(0, 0), new CellAddress(0, 0))).Should().Throw<ArgumentNullException>().WithParameterName("text");
    }

    [Fact]
    public void AStatisticsScreenWithFrequenciesCountsThem()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Statistics);
        StatisticsViewModel screen = new(session);
        screen.HasFrequencies = true;
        screen.Data.Resize(2, 2);
        Fill(screen.Data, "1", "3", "2", "1");

        screen.Apply();

        screen.Summary.First(line => line.Name == "n").Text.Should().Be("4", "three of the first and one of the second");
        screen.Summary.First(line => line.Name == "x̄").Text.Should().Be("1.25");
    }

    [Fact]
    public void EveryScreenOfTheShellIsBuiltWhenItIsAskedFor()
    {
        CalculatorShellViewModel shell = Shell.Create();

        // The Spreadsheet screen builds a sheet, which belongs to its own application: a screen is therefore built
        // when the user opens it, and the route has switched the session by then.
        shell.Open(CalculatorApps.Of(CalculatorApp.Spreadsheet));
        shell.Spreadsheet.Should().NotBeNull();
        shell.Spreadsheet.Should().BeSameAs(shell.Spreadsheet, "the screen is built once");

        foreach (CalculatorAppInfo app in CalculatorApps.Available)
        {
            shell.Open(app);
        }

        shell.BaseN.Should().NotBeNull();
        shell.Matrix.Should().NotBeNull();
        shell.Vector.Should().NotBeNull();
        shell.Statistics.Should().NotBeNull();
        shell.Distribution.Should().NotBeNull();
        shell.Equation.Should().NotBeNull();
        shell.Inequality.Should().NotBeNull();
        shell.Ratio.Should().NotBeNull();
        shell.Table.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CalculatorApp.Calculate, "app.calculate")]
    [InlineData(CalculatorApp.Complex, "app.complex")]
    public void TheLineSaysWhichApplicationItBelongsTo(CalculatorApp app, string key)
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.Open(CalculatorApps.Of(app));

        shell.Calculate.TitleKey.Should().Be(key, "Calculate and Complex are the same screen under two names");
    }

    [Fact]
    public void EveryLineOfTheCalculatorReachesTheScreens()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string> routes = [];
        shell.NavigationRequested += (_, route) => routes.Add(route);

        shell.Matrix.Calculate.Input.Press(KeyId.Home);
        shell.Vector.Calculate.Input.Press(KeyId.Settings);
        shell.Statistics.Calculate.Input.Press(KeyId.Home);
        shell.BaseN.Calculate.Input.Press(KeyId.Settings);

        routes.Should().Equal("/", "/settings", "/", "/settings");
    }
}
