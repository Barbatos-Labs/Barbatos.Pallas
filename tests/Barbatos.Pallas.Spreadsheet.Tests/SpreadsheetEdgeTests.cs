// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// The edges of the Spreadsheet and Table applications: the bounds of the sheet, what a fill and a paste do at them,
/// and the values behind the displays.
/// </summary>
public sealed class SpreadsheetEdgeTests
{
    private static Value Number(decimal value) => Value.FromDecimal(value);

    [Fact]
    public void AConstantOfElevenDigitsIsStoredWithTen()
    {
        // p. 101: the value itself is converted, not only what is displayed.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "12345678915");
        grid.SetConstant(Sheet.At("A2"), "1234567891");

        grid[Sheet.At("A1")]!.Value.ToDecimal().Should().Be(12345678920m);
        grid[Sheet.At("A2")]!.Value.ToDecimal().Should().Be(1234567891m);
    }

    [Fact]
    public void OnlyANumberIsConvertedToTenDigits()
    {
        // p. 101 counts the digits of a constant that is entered; an expression keeps what it calculates.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "12345678915+0");
        grid.SetConstant(Sheet.At("A2"), "0.00000000012345678915");

        grid[Sheet.At("A1")]!.Value.ToDecimal().Should().Be(12345678915m);
        grid[Sheet.At("A2")]!.Value.ToDecimal().Should().Be(0.00000000012345678920m, "the digits after the leading zeros are what counts");
    }

    [Fact]
    public void AReferenceThatWouldLeaveTheColumnsOrTheRows()
    {
        // p. 103: a paste that moves a reference past E or past row 45 writes "?" in its place.
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetFormula(Sheet.At("A1"), "E1+D45");

        grid.CopyPaste(Sheet.At("A1"), Sheet.At("B2"));

        grid[Sheet.At("B2")]!.Text.Should().Be("=?2+E?");
        grid[Sheet.At("B2")]!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TheSheetEndsAtItsLastCell()
    {
        SpreadsheetGrid grid = Sheet.Grid();

        grid.Rows.Should().Be(45);
        grid.Columns.Should().Be(5);
        grid.SetConstant(new CellAddress(4, 44), "1").Should().BeNull();
        grid.Invoking(sheet => sheet.SetConstant(new CellAddress(4, 45), "1")).Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*E45*", "the message names the last cell of the sheet");
        grid.Invoking(sheet => sheet.SetConstant(new CellAddress(5, 44), "1")).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReplacingACellFreesTheBytesItUsed()
    {
        // p. 101: a constant is 14 bytes and a formula is what was typed plus 15, so a cell that is replaced gives its own back.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "1");
        grid.SetConstant(Sheet.At("A1"), "2");
        grid.UsedBytes.Should().Be(14);

        grid.SetFormula(Sheet.At("A1"), "A2+1");
        grid.UsedBytes.Should().Be(4 + 15);
    }

    [Fact]
    public void AFillTakesItsCornersEitherWayRound()
    {
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A1"), "1");
        grid.SetConstant(Sheet.At("A2"), "2");

        grid.Fill("A1+10", Sheet.At("B2"), Sheet.At("B1"));

        grid[Sheet.At("B1")]!.Text.Should().Be("=A1+10");
        grid[Sheet.At("B2")]!.Text.Should().Be("=A2+10");
    }

    [Fact]
    public void AFillAwayFromTheFirstRowAndColumn()
    {
        // The references of a fill are those of its first cell, wherever the range is.
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A2"), "5");

        grid.Fill("A2×2", Sheet.At("B2"), Sheet.At("C3"));

        grid[Sheet.At("B2")]!.Text.Should().Be("=A2×2");
        grid[Sheet.At("C2")]!.Text.Should().Be("=B2×2");
        grid[Sheet.At("B3")]!.Text.Should().Be("=A3×2");
        grid[Sheet.At("C3")]!.Text.Should().Be("=B3×2");
    }

    [Fact]
    public void AFillOutsideTheSheetWritesNothing()
    {
        SpreadsheetGrid grid = Sheet.Grid();

        grid.Invoking(sheet => sheet.Fill("1", Sheet.At("A1"), new CellAddress(5, 0))).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.FillValue("1", new CellAddress(0, 45), Sheet.At("A1"))).Should().Throw<ArgumentOutOfRangeException>();

        grid.Cells.Should().BeEmpty("a fill that cannot be done leaves the sheet as it was");
    }

    [Fact]
    public void ACellInErrorHoldsZero()
    {
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "1÷0");

        grid[Sheet.At("A1")]!.Value.Should().Be(Value.Zero);
        grid[Sheet.At("A1")]!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void AFormulaIsCalculatedAgainWhenTheSheetIs()
    {
        // With Auto Calc off the values wait; the next Calculate brings every formula up to date at once (U33).
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A1"), "1");
        grid.SetFormula(Sheet.At("B1"), "A1+1");
        grid.SetFormula(Sheet.At("C1"), "B1+1");
        grid.AutoCalculate = false;

        grid.SetFormula(Sheet.At("D1"), "C1+1");
        grid.SetConstant(Sheet.At("A1"), "10");

        grid.Display("C1").Should().Be("3");
        grid.Display("D1").Should().Be("4");
        grid.Recalculate();
        grid.Display("B1").Should().Be("11");
        grid.Display("D1").Should().Be("13");
    }

    [Fact]
    public void AVerifyOfARowThatIsNotTheLastOne()
    {
        // The row's own x is what is checked, not the x the table left behind (p. 112).
        CalculatorSession session = Sheet.Session(CalculatorApp.Table);
        session.Define(DefinedFunction.F, "x²+1⌟2");
        NumberTable table = NumberTable.Generate(session, TableType.FunctionF, Number(-1), Number(1), Number(0.5m));

        table.Verify(1, TableFunction.F, "0.75").Should().BeTrue();
        table.Verify(1, TableFunction.F, "1.5").Should().BeFalse();
        session.GetVariable(MemoryVariable.X).ToDecimal().Should().Be(-0.5m, "the check leaves x where the row is");
    }

    [Fact]
    public void ARangeTooWideForDecimal_IsARangeError()
    {
        CalculatorSession session = Sheet.Session(CalculatorApp.Table);
        session.Define(DefinedFunction.F, "x");

        NumberTable table = NumberTable.Generate(session, TableType.FunctionF, Value.FromDecimal(decimal.MinValue), Value.FromDecimal(decimal.MaxValue), Number(1));

        table.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }
}
