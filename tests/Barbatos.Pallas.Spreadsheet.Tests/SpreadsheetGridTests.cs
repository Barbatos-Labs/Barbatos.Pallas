// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// The Spreadsheet application (manual pp. 100-107), against the worked examples of the manual.
/// </summary>
public sealed class SpreadsheetGridTests
{
    [Fact]
    public void TheExampleOfTheManual()
    {
        // p. 102: constants are fixed when entered, the formula in B1 follows A1.
        SpreadsheetGrid grid = Sheet.Example();

        grid.Display("A1").Should().Be("35");
        grid.Display("A2").Should().Be("42");
        grid.Display("A3").Should().Be("49");
        grid.Display("B1").Should().Be("42");
        grid[Sheet.At("A3")]!.IsFormula.Should().BeFalse();
        grid[Sheet.At("B1")]!.IsFormula.Should().BeTrue();
        grid[Sheet.At("B1")]!.Text.Should().Be("=A1+7");
        grid[Sheet.At("C1")].Should().BeNull();
    }

    [Fact]
    public void AFormulaFollowsTheCellsItReadsAndAConstantDoesNot()
    {
        // p. 101: a constant keeps the value it was entered with, whatever the cells it was calculated from do later.
        SpreadsheetGrid grid = Sheet.Example();

        grid.SetConstant(Sheet.At("A1"), "100");

        grid.Display("A3").Should().Be("49");
        grid.Display("B1").Should().Be("107");
    }

    [Fact]
    public void AutoCalculateOffCalculatesWhatIsEnteredAndLeavesTheRestUntilTheSheetIsCalculated()
    {
        // p. 107: with Auto Calc off the sheet is calculated again by Recalculate. What is entered meanwhile is
        // assumption U33: a formula entered is calculated, and the formulas that refer to what changed wait.
        SpreadsheetGrid grid = Sheet.Example();
        grid.AutoCalculate = false;

        grid.SetFormula(Sheet.At("B2"), "A2+7").Should().BeNull("a formula that calculates has no error");
        grid.Display("B2").Should().Be("49", "the formula entered is calculated as it is entered");

        grid.SetConstant(Sheet.At("A1"), "100").Should().BeNull();
        grid.Display("B1").Should().Be("42", "the formulas that refer to a constant entered wait");
        grid.SetFormula(Sheet.At("C1"), "B1+1").Should().BeNull();
        grid.Display("C1").Should().Be("43", "a formula entered reads the others as they hold their values");
        grid.Clear(Sheet.At("A2"));
        grid.Display("B2").Should().Be("49", "the formulas that refer to a cell cleared wait too");
        grid.SetFormula(Sheet.At("D1"), "1+")!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "what does not read is still its error");

        grid.Recalculate();

        grid.Display("B1").Should().Be("107");
        grid.Display("B2").Should().Be("7");
        grid.Display("C1").Should().Be("108");
    }

    [Fact]
    public void AutoCalculateOffReadsACellThatWasCutAsEmpty()
    {
        // U33: a formula entered reads every other cell as it holds its value, and a cell cut away holds none.
        SpreadsheetGrid grid = Sheet.Example();
        grid.AutoCalculate = false;

        grid.CutPaste(Sheet.At("B1"), Sheet.At("C3")).Should().BeNull();
        grid.Display("C3").Should().Be("42");
        grid.SetFormula(Sheet.At("D1"), "B1+1").Should().BeNull();

        grid.Display("D1").Should().Be("1", "B1 is empty since the cut");
    }

    [Fact]
    public void TheSessionReadsACellAsTheSheetHoldsIt()
    {
        // The engine reads a cell through the session (CalculatorSession.CellValues), outside any change of the sheet
        // too, and with Auto Calc off what a cell holds is what was last entered or calculated in it (U33).
        CalculatorSession session = Sheet.Session();
        SpreadsheetGrid grid = new(session);
        grid.SetFormula(Sheet.At("A1"), "2+3").Should().BeNull();
        session.Evaluate("A1").Display.Text.Should().Be("5");
        grid.AutoCalculate = false;

        grid.SetConstant(Sheet.At("A1"), "7").Should().BeNull();
        session.Evaluate("A1").Display.Text.Should().Be("7", "a formula replaced by a constant is read as the constant");

        grid.SetFormula(Sheet.At("A1"), "2+3").Should().BeNull();
        grid.Clear(Sheet.At("A1"));
        session.Evaluate("A1").Display.Text.Should().Be("0", "a formula cleared is an empty cell");

        grid.SetFormula(Sheet.At("A1"), "2+3").Should().BeNull();
        grid.ClearAll();
        session.Evaluate("A1").Display.Text.Should().Be("0", "and so is every cell of a sheet cleared");
    }

    [Fact]
    public void TheGrabExampleOfTheManual()
    {
        // p. 103: B2 = A2 + 7 is 49.
        SpreadsheetGrid grid = Sheet.Example();

        grid.SetFormula(Sheet.At("B2"), "A2+7");

        grid.Display("B2").Should().Be("49");
    }

    [Fact]
    public void ARelativeReferenceMovesWithACopy()
    {
        // p. 103: =A1+7 in B1, copied to C3, is =B3+7 there.
        SpreadsheetGrid grid = Sheet.Example();

        grid.CopyPaste(Sheet.At("B1"), Sheet.At("C3"));

        grid[Sheet.At("C3")]!.Text.Should().Be("=B3+7");
        grid[Sheet.At("B1")]!.Text.Should().Be("=A1+7", "a copy leaves the cell it came from");
    }

    [Theory]
    // p. 103: a dollar sign holds the column, the row or both where they are.
    [InlineData("$A1+7", "=$A3+7")]
    [InlineData("A$1+7", "=B$1+7")]
    [InlineData("$A$1+7", "=$A$1+7")]
    [InlineData("Sum(A1:A2)", "=Sum(B3:B4)")]
    public void WhatACopyDoesToEachKindOfReference(string formula, string pasted)
    {
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetFormula(Sheet.At("B1"), formula);

        grid.CopyPaste(Sheet.At("B1"), Sheet.At("C3"));

        grid[Sheet.At("C3")]!.Text.Should().Be(pasted);
    }

    [Fact]
    public void ACutLeavesEveryReferenceWhereItIs()
    {
        // p. 104: a cut and paste moves the content and changes nothing in it; the cell it came from is cleared.
        SpreadsheetGrid grid = Sheet.Example();

        grid.CutPaste(Sheet.At("B1"), Sheet.At("C3"));

        grid[Sheet.At("C3")]!.Text.Should().Be("=A1+7");
        grid[Sheet.At("B1")].Should().BeNull();
        grid.Display("C3").Should().Be("42");
    }

    [Fact]
    public void AReferenceThatWouldLeaveTheSheetBecomesAQuestionMark()
    {
        // p. 103: the column letter and the row number become "?", and the cell shows an error.
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetFormula(Sheet.At("B2"), "A1+7");

        grid.CopyPaste(Sheet.At("B2"), Sheet.At("A1"));

        grid[Sheet.At("A1")]!.Text.Should().Be("=?" + "?+7");
        grid[Sheet.At("A1")]!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void PastingAnEmptyCellDoesNothing()
    {
        SpreadsheetGrid grid = Sheet.Example();

        grid.CopyPaste(Sheet.At("E45"), Sheet.At("C3")).Should().BeNull();
        grid.CutPaste(Sheet.At("E45"), Sheet.At("C3")).Should().BeNull();

        grid[Sheet.At("C3")].Should().BeNull();
    }

    [Fact]
    public void TheRangeCommandsOfTheManual()
    {
        // p. 105: Sum(A1:A3) is 126 over 35, 42 and 49; the other three commands read the same range.
        SpreadsheetGrid grid = Sheet.Example();

        grid.SetFormula(Sheet.At("A4"), "Sum(A1:A3)");
        grid.SetFormula(Sheet.At("B4"), "Min(A1:A3)");
        grid.SetFormula(Sheet.At("C4"), "Max(A1:A3)");
        grid.SetFormula(Sheet.At("D4"), "Mean(A1:A3)");

        grid.Display("A4").Should().Be("126");
        grid.Display("B4").Should().Be("35");
        grid.Display("C4").Should().Be("49");
        grid.Display("D4").Should().Be("42");
    }

    [Fact]
    public void ARangeCountsEveryCellOfItIncludingTheEmptyOnes()
    {
        // Assumption U26: an empty cell is 0, and Mean divides by the cells of the range.
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A1"), "3");
        grid.SetConstant(Sheet.At("A3"), "9");

        grid.SetFormula(Sheet.At("B1"), "Sum(A1:A3)");
        grid.SetFormula(Sheet.At("B2"), "Mean(A1:A3)");
        grid.SetFormula(Sheet.At("B3"), "Min(A1:A3)");

        grid.Display("B1").Should().Be("12");
        grid.Display("B2").Should().Be("4");
        grid.Display("B3").Should().Be("0");
    }

    [Fact]
    public void ARangeMayBeWrittenEitherWayRound()
    {
        SpreadsheetGrid grid = Sheet.Example();

        grid.SetFormula(Sheet.At("A4"), "Sum(A3:A1)");

        grid.Display("A4").Should().Be("126");
    }

    [Fact]
    public void ARangeOverColumnsAndRows()
    {
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A1"), "1");
        grid.SetConstant(Sheet.At("B1"), "2");
        grid.SetConstant(Sheet.At("A2"), "3");
        grid.SetConstant(Sheet.At("B2"), "4");

        grid.SetFormula(Sheet.At("C1"), "Sum(A1:B2)");

        grid.Display("C1").Should().Be("10");
    }

    [Fact]
    public void TheFillFormulaExampleOfTheManual()
    {
        // p. 106: =2A1-3 filled into B1:B3 becomes =2A2-3 and =2A3-3, for 67, 81 and 95.
        SpreadsheetGrid grid = Sheet.Example();

        grid.Fill("2A1-3", Sheet.At("B1"), Sheet.At("B3"));

        grid[Sheet.At("B1")]!.Text.Should().Be("=2A1-3");
        grid[Sheet.At("B2")]!.Text.Should().Be("=2A2-3");
        grid[Sheet.At("B3")]!.Text.Should().Be("=2A3-3");
        grid.Display("B1").Should().Be("67");
        grid.Display("B2").Should().Be("81");
        grid.Display("B3").Should().Be("95");
    }

    [Fact]
    public void TheFillValueExampleOfTheManual()
    {
        // p. 106: B1×3 filled into C1:C3 enters the values 201, 243 and 285 as constants.
        SpreadsheetGrid grid = Sheet.Example();
        grid.Fill("2A1-3", Sheet.At("B1"), Sheet.At("B3"));

        grid.FillValue("B1×3", Sheet.At("C1"), Sheet.At("C3"));

        grid.Display("C1").Should().Be("201");
        grid.Display("C2").Should().Be("243");
        grid.Display("C3").Should().Be("285");
        grid.Cells.Where(cell => cell.Address.Column == 2).Should().OnlyContain(cell => !cell.IsFormula);
    }

    [Fact]
    public void AFillWithAnAbsoluteReferenceEntersItEverywhere()
    {
        // p. 106: an absolute reference goes into every cell of the range as it is.
        SpreadsheetGrid grid = Sheet.Example();

        grid.Fill("$A$1+1", Sheet.At("B1"), Sheet.At("B3"));

        grid.Cells.Where(cell => cell.IsFormula).Should().OnlyContain(cell => cell.Input == "$A$1+1");
        grid.Display("B3").Should().Be("36");
    }

    [Fact]
    public void ACellThatRefersToItself_IsACircularError()
    {
        // p. 165: a cell asked for while it is being calculated.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetFormula(Sheet.At("A1"), "A1").Should().NotBeNull();

        grid[Sheet.At("A1")]!.Error!.Value.Kind.Should().Be(CalcErrorKind.CircularError);
    }

    [Fact]
    public void CellsThatRefersToEachOther_AreACircularError()
    {
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetFormula(Sheet.At("A1"), "B1+1");
        grid.SetFormula(Sheet.At("B1"), "A1+1");

        grid[Sheet.At("A1")]!.Error!.Value.Kind.Should().Be(CalcErrorKind.CircularError);
        grid[Sheet.At("B1")]!.Error!.Value.Kind.Should().Be(CalcErrorKind.CircularError);
    }

    [Fact]
    public void ACellThatReadsACellInError_IsInErrorItself()
    {
        SpreadsheetGrid grid = Sheet.Grid();
        grid.SetConstant(Sheet.At("A1"), "1÷0");

        grid.SetFormula(Sheet.At("B1"), "A1+1");

        grid[Sheet.At("A1")]!.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        grid[Sheet.At("B1")]!.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AConstantOfElevenDigitsKeepsTen()
    {
        // p. 101: 12345678915 becomes 1.234567892×10¹⁰ when the input is confirmed.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "12345678915");

        grid.Display("A1").Should().Be("1.234567892×10^10");
        grid.SetConstant(Sheet.At("A2"), "1234567891");
        grid.Display("A2").Should().Be("1234567891", "ten digits are kept as they are");
    }

    [Fact]
    public void ClearingOneCellAndTheWholeSheet()
    {
        // p. 104.
        SpreadsheetGrid grid = Sheet.Example();

        grid.Clear(Sheet.At("A1"));

        grid[Sheet.At("A1")].Should().BeNull();
        grid.Display("B1").Should().Be("7", "the formula now reads an empty cell");
        grid.Clear(Sheet.At("A1"));

        grid.ClearAll();
        grid.Cells.Should().BeEmpty();
        grid.UsedBytes.Should().Be(0);
    }

    [Fact]
    public void TheBytesOfTheCellsOfTheManual()
    {
        // p. 101: a constant is 14 bytes; a formula is what was typed plus 15, and a command is one byte.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), "12345678915");
        grid.UsedBytes.Should().Be(14);

        grid.SetFormula(Sheet.At("B1"), "A1+7");
        grid.UsedBytes.Should().Be(14 + 4 + 15);

        grid.SetFormula(Sheet.At("C1"), "Sum(A1:A3)");
        grid.UsedBytes.Should().Be(14 + 19 + 1 + 6 + 15, "Sum( is one byte, A1:A3) is six, and a formula costs fifteen more");
    }

    [Fact]
    public void AnInputBeyondFortyNineBytes_IsAMemoryError()
    {
        // p. 101: 49 bytes is what an input may have.
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetConstant(Sheet.At("A1"), new string('1', 50))!.Value.Kind.Should().Be(CalcErrorKind.MemoryError);
        grid[Sheet.At("A1")].Should().BeNull();
        grid.SetConstant(Sheet.At("A1"), new string('1', 49)).Should().BeNull();
    }

    [Fact]
    public void ASheetBeyondItsCapacity_IsAMemoryError()
    {
        // p. 100: the sheet holds 1,700 bytes, which is 121 constants of 14.
        SpreadsheetGrid grid = Sheet.Grid();
        CalcError? error = null;
        for (int row = 0; row < grid.Rows && error is null; row++)
        {
            for (int column = 0; column < grid.Columns && error is null; column++)
            {
                error = grid.SetConstant(new CellAddress(column, row), "1");
            }
        }

        error!.Value.Kind.Should().Be(CalcErrorKind.MemoryError);
        grid.Cells.Should().HaveCount(grid.Capacity / 14);
        grid.UsedBytes.Should().BeLessThanOrEqualTo(grid.Capacity);
    }

    [Fact]
    public void TheExtendedProfileHasMoreRowsAndNoCapacity()
    {
        SpreadsheetGrid grid = Sheet.Grid(CalculatorProfile.Extended);

        grid.Rows.Should().Be(99);
        grid.Capacity.Should().Be(int.MaxValue);
        grid.SetConstant(Sheet.At("E99"), "1").Should().BeNull();
    }

    [Fact]
    public void ACellOutsideTheSheetIsRefused()
    {
        SpreadsheetGrid grid = Sheet.Grid();
        CellAddress outside = new(grid.Columns, 0);

        grid.Invoking(sheet => sheet.SetConstant(outside, "1")).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.SetFormula(outside, "1")).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.CopyPaste(outside, Sheet.At("A1"))).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.CutPaste(outside, Sheet.At("A1"))).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.Fill("1", outside, Sheet.At("A1"))).Should().Throw<ArgumentOutOfRangeException>();
        grid.Invoking(sheet => sheet.FillValue("1", Sheet.At("A1"), outside)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ASheetNeedsASessionOfItsOwnApplication()
    {
        Action wrongApp = () => _ = new SpreadsheetGrid(Sheet.Session(CalculatorApp.Calculate));
        Action missing = () => _ = new SpreadsheetGrid(null!);
        SpreadsheetGrid grid = Sheet.Grid();

        wrongApp.Should().Throw<ArgumentException>().WithParameterName("session");
        missing.Should().Throw<ArgumentNullException>();
        grid.Invoking(sheet => sheet.SetConstant(Sheet.At("A1"), null!)).Should().Throw<ArgumentNullException>().WithParameterName("input");
        grid.Invoking(sheet => sheet.SetFormula(Sheet.At("A1"), null!)).Should().Throw<ArgumentNullException>().WithParameterName("formula");
        grid.Invoking(sheet => sheet.Fill(null!, Sheet.At("A1"), Sheet.At("A2"))).Should().Throw<ArgumentNullException>().WithParameterName("formula");
        grid.Invoking(sheet => sheet.FillValue(null!, Sheet.At("A1"), Sheet.At("A2"))).Should().Throw<ArgumentNullException>().WithParameterName("input");
    }

    [Fact]
    public void AFormulaThatDoesNotParse_IsASyntaxError()
    {
        SpreadsheetGrid grid = Sheet.Grid();

        grid.SetFormula(Sheet.At("A1"), "A1+")!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        grid.SetConstant(Sheet.At("A2"), "Sum(A1)")!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "a range command takes a range");
        grid.SetConstant(Sheet.At("A3"), "A1:A2")!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "a range belongs to a range command");
    }

    [Fact]
    public void TheCellsOfASheetAreWhatWasEntered()
    {
        SpreadsheetGrid grid = Sheet.Example();

        grid.Cells.Select(cell => cell.Address.ToString()).Should().BeEquivalentTo(["A1", "A2", "A3", "B1"]);
        grid[Sheet.At("A1")]!.ToString().Should().Be("A1: 7×5");
        grid[Sheet.At("B1")]!.ToString().Should().Be("B1: =A1+7");
    }
}
