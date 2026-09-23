// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The grid every form of the calculator is made of: cells that hold what was typed and what it comes to.
/// </summary>
public sealed class ValueGridViewModelTests
{
    private static ValueGridViewModel Grid(int rows = 2, int columns = 2) => new(Shell.Session(), rows, columns);

    [Fact]
    public void ANewGridIsZeros()
    {
        ValueGridViewModel grid = Grid();

        grid.Rows.Should().Be(2);
        grid.Columns.Should().Be(2);
        grid.Cells.Should().HaveCount(4);
        grid[0, 0].Value.Should().Be(Value.Zero);
        grid[1, 1].Display.Should().Be("0");
        grid.HasError.Should().BeFalse();
    }

    [Fact]
    public void ACellHoldsAnExpression()
    {
        ValueGridViewModel grid = Grid();

        grid[0, 0].Text = "2⌟3";

        grid[0, 0].Value.ToDecimal().Should().BeApproximately(2m / 3m, 0.0000000001m);
        grid[0, 0].Display.Should().Be("2⌟3", "a coefficient may be a fraction, and it is shown as one");
        grid[0, 0].HasError.Should().BeFalse();
    }

    [Fact]
    public void ACellThatIsNotAValueSaysSoAndCountsAsZero()
    {
        ValueGridViewModel grid = Grid();

        grid[0, 1].Text = "1÷0";

        grid[0, 1].HasError.Should().BeTrue();
        grid[0, 1].Error.Should().Be(CalcErrorKind.MathError);
        grid[0, 1].ErrorKey.Should().Be("error.MathError");
        grid[0, 1].Value.Should().Be(Value.Zero);
        grid.HasError.Should().BeTrue();
    }

    [Fact]
    public void AnEmptyCellIsZero()
    {
        ValueGridViewModel grid = Grid();

        grid[0, 0].Text = "5";
        grid[0, 0].Text = string.Empty;

        grid[0, 0].Value.Should().Be(Value.Zero);
        grid[0, 0].HasError.Should().BeFalse();
        grid[0, 0].Display.Should().Be("0");
    }

    [Fact]
    public void ACellSaysWhenItChanges()
    {
        ValueGridViewModel grid = Grid();
        List<string?> changed = grid[0, 0].Changes();

        grid[0, 0].Text = "7";

        changed.Should().Contain([nameof(ValueCellViewModel.Text), nameof(ValueCellViewModel.Value), nameof(ValueCellViewModel.Display)]);
    }

    [Fact]
    public void FillingACellDoesNotTouchTheCalculator()
    {
        CalculatorSession session = Shell.Session();
        ValueGridViewModel grid = new(session, 1, 1);

        grid[0, 0].Text = "1+1";

        session.Ans.Should().Be(Value.Zero, "filling in a form is not a calculation");
        session.History.Should().BeEmpty();
    }

    [Fact]
    public void AGridKeepsWhatFitsWhenItChangesSize()
    {
        ValueGridViewModel grid = Grid(2, 2);
        grid[0, 0].Text = "1";
        grid[0, 1].Text = "2";
        grid[1, 0].Text = "3";

        grid.Resize(3, 1);

        grid.Rows.Should().Be(3);
        grid.Columns.Should().Be(1);
        grid[0, 0].Text.Should().Be("1");
        grid[1, 0].Text.Should().Be("3");
        grid[2, 0].Text.Should().Be("0", "a new cell starts at zero");
    }

    [Fact]
    public void AGridOfTheSameSizeIsLeftAlone()
    {
        ValueGridViewModel grid = Grid(2, 2);
        grid[0, 0].Text = "9";

        grid.Resize(2, 2);

        grid[0, 0].Text.Should().Be("9");
    }

    [Fact]
    public void AGridReadsOutAsValues()
    {
        ValueGridViewModel grid = Grid(2, 2);
        grid[0, 0].Text = "1";
        grid[0, 1].Text = "2";
        grid[1, 0].Text = "3";
        grid[1, 1].Text = "4";

        Value[,] values = grid.ToArray();

        values[0, 0].ToDecimal().Should().Be(1m);
        values[1, 1].ToDecimal().Should().Be(4m);
        grid.Column(0).Select(value => value.ToDecimal()).Should().Equal(1m, 3m);
    }

    [Fact]
    public void ValuesCanBePutIntoAGrid()
    {
        ValueGridViewModel grid = Grid(1, 1);

        grid.Set(new[,] { { Value.FromDecimal(0.5m), Value.FromDecimal(2) } });

        grid.Rows.Should().Be(1);
        grid.Columns.Should().Be(2);
        grid[0, 0].Text.Should().Be("1⌟2", "what was put in is what can be edited, as the calculator writes it");
        grid[0, 1].Value.ToDecimal().Should().Be(2m);
    }

    [Fact]
    public void AGridIsEmptiedToZeros()
    {
        ValueGridViewModel grid = Grid();
        grid[0, 0].Text = "5";

        grid.Clear();

        grid[0, 0].Value.Should().Be(Value.Zero);
        grid.HasError.Should().BeFalse();
    }

    [Fact]
    public void AGridRefusesWhatIsNotAGrid()
    {
        CalculatorSession session = Shell.Session();
        Action noSession = () => _ = new ValueGridViewModel(null!, 1, 1);
        Action noRows = () => _ = new ValueGridViewModel(session, 0, 1);
        Action noColumns = () => _ = new ValueGridViewModel(session, 1, 0);
        ValueGridViewModel grid = Grid();
        Action outside = () => _ = grid[2, 0];
        Action negative = () => _ = grid[0, -1];
        Action noSize = () => grid.Resize(0, 1);
        Action noColumn = () => grid.Column(5);
        Action noValues = () => grid.Set(null!);

        noSession.Should().Throw<ArgumentNullException>();
        noRows.Should().Throw<ArgumentOutOfRangeException>();
        noColumns.Should().Throw<ArgumentOutOfRangeException>();
        outside.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
        noSize.Should().Throw<ArgumentOutOfRangeException>();
        noColumn.Should().Throw<ArgumentOutOfRangeException>();
        noValues.Should().Throw<ArgumentNullException>();
    }
}
