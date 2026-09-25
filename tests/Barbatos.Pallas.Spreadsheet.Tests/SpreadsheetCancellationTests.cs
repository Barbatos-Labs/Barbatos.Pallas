// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using static Barbatos.Pallas.Spreadsheet.Tests.Sheet;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// A sheet whose calculation is stopped: every change that calculates takes a token, and a sheet stopped halfway keeps
/// what it had and calculates as before the next time.
/// </summary>
/// <remarks>
/// A sum of 2,048 terms is what is stopped: the engine looks at its token every 1,024 steps, so a token cancelled
/// beforehand stops it at a known step.
/// </remarks>
public sealed class SpreadsheetCancellationTests
{
    private const string Sum = "Σ(x,1,2048)";

    private static SpreadsheetGrid WithASum(CalculatorSession session)
    {
        SpreadsheetGrid grid = new(session);
        grid.SetFormula(At("A1"), Sum).Should().BeNull();
        grid.SetFormula(At("B1"), "A1+1").Should().BeNull();
        return grid;
    }

    private static readonly CancellationToken Stopped = new(canceled: true);

    [Fact]
    public void AStoppedCalculationKeepsTheValuesTheSheetHadAndLeavesNoCircularError()
    {
        SpreadsheetGrid grid = WithASum(Session());
        grid.Display("A1").Should().Be("2098176");

        grid.Invoking(sheet => sheet.Recalculate(Stopped)).Should().Throw<OperationCanceledException>();
        grid.Display("A1").Should().Be("2098176", "a pass that did not finish changes no cell");
        grid.Display("B1").Should().Be("2098177");

        // A1 was being calculated when the pass stopped; had it stayed marked so, it would read as a Circular ERROR.
        grid.Recalculate();
        grid.Display("A1").Should().Be("2098176");
        grid.Display("B1").Should().Be("2098177");
    }

    [Fact]
    public void EveryChangeThatCalculatesTakesItsTokenAndIsUndoneWhenItIsStopped()
    {
        SpreadsheetGrid grid = WithASum(Session());
        grid.SetConstant(At("C1"), "1").Should().BeNull();
        grid.SetConstant(At("D1"), "2").Should().BeNull();
        SpreadsheetCell[] before = [.. grid.Cells.OrderBy(cell => cell.Address.Row).ThenBy(cell => cell.Address.Column)];

        grid.Invoking(sheet => sheet.SetConstant(At("C2"), "1", Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.SetFormula(At("C3"), "2", Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.Clear(At("C1"), Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.CopyPaste(At("D1"), At("D2"), Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.CutPaste(At("D1"), At("D3"), Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.Fill("3", At("E1"), At("E2"), Stopped)).Should().Throw<OperationCanceledException>();
        grid.Invoking(sheet => sheet.FillValue("4", At("E3"), At("E4"), Stopped)).Should().Throw<OperationCanceledException>();

        grid.Cells.OrderBy(cell => cell.Address.Row).ThenBy(cell => cell.Address.Column).Should().Equal(before, "a change that is stopped leaves the sheet as it was");
        grid.Recalculate();
        grid.Display("A1").Should().Be("2098176", "and the sheet calculates as before");
    }

    [Fact]
    public void AChangeStoppedWithAutoCalcOffIsUndoneToo()
    {
        // With Auto Calc off the formula entered is what is calculated (U33); stopped, it is not entered at all.
        SpreadsheetGrid grid = WithASum(Session());
        grid.AutoCalculate = false;

        grid.Invoking(sheet => sheet.SetFormula(At("C1"), "Σ(x,1,2048)+1", Stopped)).Should().Throw<OperationCanceledException>();

        grid[At("C1")].Should().BeNull();
        grid.SetFormula(At("C1"), "A1+1").Should().BeNull();
        grid.Display("C1").Should().Be("2098177", "A1 is read as it holds its value");
    }

    [Fact]
    public void AStoppedChangeLeavesNoTokenBehind()
    {
        CalculatorSession session = Session();
        SpreadsheetGrid grid = WithASum(session);
        grid.Invoking(sheet => sheet.Recalculate(Stopped)).Should().Throw<OperationCanceledException>();

        // A1 is calculated again when the session reads it, under no token but the session's own.
        Calculation read = session.Evaluate("A1");
        read.Succeeded.Should().BeTrue();
        read.Display.Text.Should().Be("2098176");
    }

    [Fact]
    public void WhatAStoppedPassCalculatedIsForgotten()
    {
        // A pass stopped halfway has calculated A1, before it came to the sum B1 stops at, from the change it undoes;
        // what the session reads afterwards is what the cells hold again.
        CalculatorSession session = Session();
        SpreadsheetGrid grid = new(session);
        grid.SetConstant(At("C1"), "3").Should().BeNull();
        grid.SetFormula(At("A1"), "C1×10").Should().BeNull();
        grid.SetFormula(At("B1"), "A1+" + Sum).Should().BeNull();

        grid.Invoking(sheet => sheet.SetConstant(At("C1"), "5", Stopped)).Should().Throw<OperationCanceledException>();
        session.Evaluate("A1").Display.Text.Should().Be("30", "A1 was 50 in the pass, which was undone");

        grid.Invoking(sheet => sheet.SetFormula(At("C1"), "7", Stopped)).Should().Throw<OperationCanceledException>();
        session.Evaluate("C1").Display.Text.Should().Be("3", "C1 was a formula of 7 in the pass, and is the constant 3 again");
        session.Evaluate("A1").Display.Text.Should().Be("30");
    }

    [Fact]
    public void AChangeThatIsNotStoppedCalculatesAsBefore()
    {
        using CancellationTokenSource running = new();
        SpreadsheetGrid grid = new(Session());

        grid.SetFormula(At("A1"), Sum, running.Token).Should().BeNull();
        grid.SetConstant(At("A2"), "5", running.Token).Should().BeNull();
        grid.Fill("A1+A2", At("B1"), At("B1"), running.Token).Should().BeNull();
        grid.Recalculate(running.Token);

        grid.Display("B1").Should().Be("2098181");
    }
}
