// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// What the screens tell the window: a value that changed, a screen that was built once, and a guard that names the
/// argument it refused.
/// </summary>
public sealed class ScreenNotificationTests
{
    [Fact]
    public void ACellThatIsGivenAValueSaysEverythingChanged()
    {
        ValueGridViewModel grid = new(Shell.Session(), 1, 1);
        List<string?> changed = grid[0, 0].Changes();

        grid[0, 0].Set(Value.FromDecimal(12));

        changed.Should().Contain([
            nameof(ValueCellViewModel.Text),
            nameof(ValueCellViewModel.Value),
            nameof(ValueCellViewModel.Error),
            nameof(ValueCellViewModel.HasError),
            nameof(ValueCellViewModel.ErrorKey),
            nameof(ValueCellViewModel.Display)]);
        grid[0, 0].Text.Should().Be("12");
    }

    [Fact]
    public void ACellShowsWhatWasTypedWhileItIsNotAValue()
    {
        ValueGridViewModel grid = new(Shell.Session(), 1, 1);

        grid[0, 0].Text = "1÷0";

        grid[0, 0].Display.Should().Be("1÷0", "there is nothing else to show it as");
    }

    [Fact]
    public void ANewCellIsCalculatedBeforeAnybodyLooksAtIt()
    {
        ValueGridViewModel grid = new(Shell.Session(), 1, 1);

        grid[0, 0].Display.Should().Be("0");
        grid[0, 0].Value.Should().Be(Value.Zero);
    }

    [Fact]
    public void AGridOfTheSameSizeKeepsTheCellsThemselves()
    {
        ValueGridViewModel grid = new(Shell.Session(), 2, 2);
        ValueCellViewModel first = grid[0, 0];

        grid.Resize(2, 2);

        grid[0, 0].Should().BeSameAs(first, "nothing was rebuilt, so nothing the window is bound to moved");
    }

    [Fact]
    public void AGridThatChangesShapeKeepsEveryCellWhereItBelongs()
    {
        ValueGridViewModel grid = new(Shell.Session(), 2, 3);
        grid[0, 0].Text = "1";
        grid[0, 1].Text = "2";
        grid[0, 2].Text = "3";
        grid[1, 0].Text = "4";
        grid[1, 1].Text = "5";
        grid[1, 2].Text = "6";

        grid.Resize(3, 2);

        grid[0, 0].Text.Should().Be("1");
        grid[0, 1].Text.Should().Be("2");
        grid[1, 0].Text.Should().Be("4", "the second row starts where the second row started");
        grid[1, 1].Text.Should().Be("5");
        grid[2, 0].Text.Should().Be("0", "the row that was not there before is empty");
    }

    [Fact]
    public void AGridNamesTheArgumentItRefuses()
    {
        CalculatorSession session = Shell.Session();
        ValueGridViewModel grid = new(session, 2, 2);

        FluentActions.Invoking(() => _ = new ValueGridViewModel(session, 0, 1)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("rows");
        FluentActions.Invoking(() => _ = new ValueGridViewModel(session, 1, 0)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
        FluentActions.Invoking(() => _ = grid[-1, 0]).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("row");
        FluentActions.Invoking(() => _ = grid[0, -1]).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
        FluentActions.Invoking(() => _ = grid[2, 0]).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("row");
        FluentActions.Invoking(() => _ = grid[0, 2]).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
        FluentActions.Invoking(() => grid.Resize(1, 0)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
        FluentActions.Invoking(() => grid.Resize(0, 1)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("rows");
        FluentActions.Invoking(() => grid.Column(-1)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
        FluentActions.Invoking(() => grid.Column(2)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
    }

    [Theory]
    // A matrix of the calculator is between one and four rows and columns (p. 135).
    [InlineData(1, 1, true)]
    [InlineData(4, 4, true)]
    [InlineData(0, 2, false)]
    [InlineData(2, 0, false)]
    [InlineData(5, 2, false)]
    [InlineData(2, 5, false)]
    public void AMatrixIsOneToFourRowsAndColumns(int rows, int columns, bool allowed)
    {
        MatrixViewModel screen = new(Shell.Session(CalculatorApp.Matrix));

        screen.Resize(rows, columns).Should().Be(allowed);

        if (allowed)
        {
            screen.Grid.Rows.Should().Be(rows);
            screen.Grid.Columns.Should().Be(columns);
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void AVectorIsTwoOrThreeElementsExactly(int dimension, bool allowed)
    {
        VectorViewModel screen = new(Shell.Session(CalculatorApp.Vector));

        screen.Resize(dimension).Should().Be(allowed);
    }

    [Fact]
    public void AMatrixScreenSaysWhenWhatItHoldsChanges()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        List<string?> changed = screen.Changes();

        screen.Store();
        changed.Should().Contain(nameof(MatrixViewModel.IsDefined), "a screen that offers to delete has to know there is something to delete");

        changed.Clear();
        screen.Delete();
        changed.Should().Contain(nameof(MatrixViewModel.IsDefined));

        changed.Clear();
        screen.Name = MatrixVariable.MatB;
        changed.Should().Contain([nameof(MatrixViewModel.IsDefined), nameof(MatrixViewModel.Answer)]);
    }

    [Fact]
    public void ChoosingAnotherMatrixEmptiesTheLine()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Matrix);
        MatrixViewModel screen = new(session);
        screen.Calculate.Input.Set(MathDocumentReader.Read("MatA", CalculatorApp.Matrix));

        screen.Name = MatrixVariable.MatC;

        screen.Calculate.Input.IsEmpty.Should().BeTrue("the line belonged to the matrix that was open");
    }

    [Fact]
    public void ChoosingAnotherVectorEmptiesTheLine()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Vector);
        VectorViewModel screen = new(session);
        screen.Calculate.Input.Set(MathDocumentReader.Read("VctA", CalculatorApp.Vector));

        screen.Name = VectorVariable.VctC;

        screen.Calculate.Input.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void EveryScreenOfTheShellIsBuiltOnce()
    {
        // A screen is built while its own application is open, which is when the route reaches it.
        CalculatorShellViewModel shell = Shell.Create();
        foreach ((CalculatorApp app, Func<object> screen) in new (CalculatorApp, Func<object>)[]
        {
            (CalculatorApp.BaseN, () => shell.BaseN),
            (CalculatorApp.Matrix, () => shell.Matrix),
            (CalculatorApp.Vector, () => shell.Vector),
            (CalculatorApp.Statistics, () => shell.Statistics),
            (CalculatorApp.Distribution, () => shell.Distribution),
            (CalculatorApp.Equation, () => shell.Equation),
            (CalculatorApp.Inequality, () => shell.Inequality),
            (CalculatorApp.Ratio, () => shell.Ratio),
            (CalculatorApp.Table, () => shell.Table),
            (CalculatorApp.Spreadsheet, () => shell.Spreadsheet),
        })
        {
            shell.Open(CalculatorApps.Of(app));
            screen().Should().BeSameAs(screen(), "the {0} screen is built once", app);
        }

        shell.BaseN.Should().BeSameAs(shell.BaseN);
        shell.Matrix.Should().BeSameAs(shell.Matrix);
        shell.Vector.Should().BeSameAs(shell.Vector);
        shell.Statistics.Should().BeSameAs(shell.Statistics);
        shell.Distribution.Should().BeSameAs(shell.Distribution);
        shell.Equation.Should().BeSameAs(shell.Equation);
        shell.Inequality.Should().BeSameAs(shell.Inequality);
        shell.Ratio.Should().BeSameAs(shell.Ratio);
        shell.Table.Should().BeSameAs(shell.Table);
        shell.Spreadsheet.Should().BeSameAs(shell.Spreadsheet);
    }

    [Fact]
    public void AComplexRootIsWrittenWithTheSignOfItsImaginaryPart()
    {
        // x² + 1 = 0 has the roots i and −i, and the screen writes both signs.
        EquationViewModel screen = new(Shell.Session(CalculatorApp.Equation));
        screen.Kind = EquationKind.Polynomial;
        screen.Grid[0, 0].Text = "1";
        screen.Grid[0, 1].Text = "0";
        screen.Grid[0, 2].Text = "1";

        screen.Solve();

        screen.Solutions[0].Name.Should().Be("x₁");
        screen.Solutions[1].Name.Should().Be("x₂");
        string both = screen.Solutions[0].Text + " " + screen.Solutions[1].Text;
        both.Should().Contain("+").And.Contain("-", "one root is above the real line and the other below it");
        both.Should().NotContain("--", "a minus is written once");
    }

    [Fact]
    public void TheDistributionGridFollowsWhatTheDistributionAsksFor()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));

        screen.Kind = DistributionKind.NormalCD;
        screen.Arguments.Columns.Should().Be(4);
        screen.TakesOneValue.Should().BeTrue("the normal distributions take one x (p. 96)");
        screen.Values.Rows.Should().Be(1);

        screen.SetValueCount(5);
        screen.Values.Rows.Should().Be(1, "and only one, however many are asked for");

        screen.Kind = DistributionKind.BinomialPD;
        screen.TakesOneValue.Should().BeFalse();
        screen.SetValueCount(5);
        screen.Values.Rows.Should().Be(5, "a binomial is calculated for as many values as are typed");
    }

    [Fact]
    public void TheDistributionSaysWhenTheKindChangesWhatItTakes()
    {
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution));
        List<string?> changed = screen.Changes();

        screen.Kind = DistributionKind.NormalPD;

        changed.Should().Contain([nameof(DistributionViewModel.Kind), nameof(DistributionViewModel.Parameters), nameof(DistributionViewModel.TakesOneValue)]);
    }

    [Fact]
    public void RecallStartsAtTheNewestAfterTheLineIsCleared()
    {
        CalculateViewModel screen = new(Shell.Session());
        screen.Input.Set(MathDocumentReader.Read("1+1"));
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2+2"));
        screen.Execute();

        screen.Clear();
        screen.RecallPrevious().Should().BeTrue();

        screen.Input.Linear.Should().Be("2+2", "the newest, not the one before it");
    }

    [Fact]
    public void MovingTheCursorIsNotAnEditWhereverItIsAsked()
    {
        MathDocument document = MathDocument.Empty.Insert("1").Insert(MathTemplateKind.Fraction).Insert("2");

        KeyResult up = InputCommandRouter.Apply(document, new RunCommand(KeyCommand.MoveUp));
        KeyResult down = InputCommandRouter.Apply(up.Document, new RunCommand(KeyCommand.MoveDown));
        KeyResult home = InputCommandRouter.Apply(document, new RunCommand(KeyCommand.Home));

        up.Edited.Should().BeFalse();
        up.Document.Cursor.Path[0].Slot.Should().Be(0);
        down.Edited.Should().BeFalse();
        down.Document.Cursor.Path[0].Slot.Should().Be(1);
        home.Edited.Should().BeFalse();
        home.Request.Should().Be(KeyCommand.Home);
    }

    [Fact]
    public void TheLineIsToldWhenItBelongsToAnotherApplication()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string?> changed = shell.Calculate.Changes();

        shell.Open(CalculatorApps.Of(CalculatorApp.Complex));

        changed.Should().Contain(nameof(CalculateViewModel.TitleKey));
    }
}
