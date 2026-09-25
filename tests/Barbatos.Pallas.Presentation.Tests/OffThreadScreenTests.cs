// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The screens as the application has them, with what they calculate off the window's thread: nothing is shown before
/// it is done, AC stops it and shows nothing new, and the shell neither switches nor saves a session it has lent.
/// </summary>
public sealed class OffThreadScreenTests
{
    /// <summary>Ten million terms: seconds of work, which a test only ever starts in order to stop it.</summary>
    private const string Long = "Σ(x,1,10^7)";

    [Fact]
    public void TheLineIsShownOnceItIsCalculated() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        CalculateViewModel screen = new(Shell.Session(), work: work);
        screen.Input.Set(MathDocumentReader.Read("1+2"));

        screen.Execute();
        screen.Calculation.Should().BeNull("nothing is shown before the calculation is done");
        work.IsBusy.Should().BeTrue();

        await work.Completion;
        screen.Display!.Text.Should().Be("3");
        screen.History.Should().ContainSingle();
    });

    [Fact]
    public void ALineStoppedByACShowsNothingNewAndChangesNothing() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        CalculatorSession session = Shell.Session();
        CalculateViewModel screen = new(session, work: work);
        screen.Input.Set(MathDocumentReader.Read("2+3"));
        screen.Execute();
        await work.Completion;
        Value five = screen.Calculation!.Result;

        screen.Input.Set(MathDocumentReader.Read(Long));
        string line = screen.Input.Linear;
        screen.Execute();
        work.Cancel();
        await work.Completion;

        screen.Display!.Text.Should().Be("5", "the calculation that was stopped came to nothing");
        session.Ans.Should().Be(five);
        screen.History.Should().ContainSingle("a calculation that was stopped is not one that was executed");
        screen.Input.Linear.Should().Be(line, "the line stays, to be changed and calculated again");
    });

    [Fact]
    public void ATableIsShownOnceItIsGenerated() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table), work) { Type = TableType.FunctionF, FunctionF = "x²" };

        screen.Generate();
        screen.Rows.Should().BeEmpty();
        await work.Completion;

        screen.Rows.Should().HaveCount(5);
        screen.Rows[4].F!.Text.Should().Be("25");
        screen.Graph.HasGraph.Should().BeTrue();
    });

    [Fact]
    public void ATableStoppedByACHasNoRows() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table), work) { Type = TableType.FunctionF, FunctionF = Long };

        screen.Generate();
        work.Cancel();
        await work.Completion;

        screen.Rows.Should().BeEmpty();
        screen.ErrorKey.Should().BeNull();
        screen.Graph.HasGraph.Should().BeFalse();
    });

    [Fact]
    public void ARowCalculatedAgainIsShownOnlyWhileItsTableIs() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table), work) { Type = TableType.FunctionF, FunctionF = "x²" };
        screen.Generate();
        await work.Completion;

        screen.SetX(0, Value.FromDecimal(10)).Should().BeTrue();
        screen.Rows[0].X.Text.Should().Be("1", "the row is shown once it is calculated");
        await work.Completion;
        screen.Rows[0].F!.Text.Should().Be("100");

        screen.SetX(0, Value.FromDecimal(20)).Should().BeTrue();
        screen.Clear();
        await work.Completion;
        screen.Rows.Should().BeEmpty("the table the row belonged to was cleared while it was calculated");
    });

    [Fact]
    public void ADistributionIsShownForTheValuesThatWereOnTheScreen() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        DistributionViewModel screen = new(Shell.Session(CalculatorApp.Distribution), work) { Kind = DistributionKind.BinomialPD };
        screen.SetValueCount(2);
        screen.Values[0, 0].Text = "3";
        screen.Values[1, 0].Text = "4";
        screen.Arguments[0, 0].Text = "10";
        screen.Arguments[0, 1].Text = "0.5";

        screen.Execute();
        screen.SetValueCount(1);
        await work.Completion;

        screen.Results.Select(line => line.Name).Should().Equal("3", "4");
        screen.Results[0].Text.Should().Be("0.1171875");
    });

    [Fact]
    public void ASheetIsShownOnceItIsCalculated() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet), work) { Input = "=2+3" };

        screen.Commit();
        screen.Editing.Text.Should().BeEmpty();
        await work.Completion;

        screen.Editing.Text.Should().Be("5");
    });

    [Fact]
    public void ASheetStoppedByACIsAsItWas() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet), work) { Input = "=" + Long };

        // However far the change got before AC, the sheet is left as it was, and the screen shows it as it was.
        screen.Commit();
        work.Cancel();
        await work.Completion;
        screen.Editing.Input.Should().BeEmpty();
        screen.UsedBytes.Should().Be(0, "the formula was not entered");

        screen.Selected = new CellAddress(1, 0);
        screen.Input = "7";
        screen.Commit();
        await work.Completion;
        screen.Cells.Single(cell => cell.Address == new CellAddress(1, 0)).Text.Should().Be("7", "the sheet carries on");
        screen.Cells.Single(cell => cell.Address == new CellAddress(0, 0)).Input.Should().BeEmpty();
    });

    [Fact]
    public void TheSelectedCellIsSaidAgainWhenTheSheetIsShownAgain()
    {
        // The cells are new records each time: the grid is told again which one is selected, or it loses it.
        SpreadsheetViewModel screen = new(Shell.Session(CalculatorApp.Spreadsheet)) { Input = "5" };
        List<string?> changes = screen.Changes();

        screen.Commit();

        changes.Should().ContainInOrder(nameof(SpreadsheetViewModel.Cells), nameof(SpreadsheetViewModel.Selected));
    }

    [Fact]
    public void TheShellOpensNoApplicationWhileItsSessionCalculates() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        CalculatorShellViewModel shell = new(Shell.Session(), new InMemorySessionStore(), work);
        string matrix = CalculatorApps.Of(CalculatorApp.Matrix).Route;
        shell.Calculate.Input.Set(MathDocumentReader.Read("1+1"));

        shell.Calculate.Execute();
        shell.OpenRoute(matrix).Should().BeFalse();
        shell.Open(CalculatorApps.Of(CalculatorApp.Table));
        shell.Session.App.Should().Be(CalculatorApp.Calculate);
        shell.CurrentApp.App.Should().Be(CalculatorApp.Calculate);

        await work.Completion;
        shell.OpenRoute(matrix).Should().BeTrue();
        shell.Session.App.Should().Be(CalculatorApp.Matrix);
    });

    [Fact]
    public void ASaveWhileTheSessionCalculatesWritesItAsItWasBeforeTheCalculation() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        InMemorySessionStore store = new();
        CalculatorShellViewModel shell = new(Shell.Session(), store, work);
        string before = shell.Session.Capture().Ans;
        shell.Calculate.Input.Set(MathDocumentReader.Read("6×7"));

        shell.Calculate.Execute();

        // The calculation is done on its thread and not yet applied on this one, so the session itself already holds
        // it: a save that read the session now would write it.
        SpinWait.SpinUntil(() => shell.Session.History.Count == 1, TimeSpan.FromSeconds(10)).Should().BeTrue();
        work.IsBusy.Should().BeTrue();
        shell.Save();
        store.Load()!.Snapshot.Ans.Should().Be(before);
        store.Load()!.History.Should().BeEmpty();

        await work.Completion;
        shell.Save();
        store.Load()!.Snapshot.Ans.Should().Be(shell.Session.Capture().Ans).And.NotBe(before);
        store.Load()!.History.Should().ContainSingle();
    });
}
