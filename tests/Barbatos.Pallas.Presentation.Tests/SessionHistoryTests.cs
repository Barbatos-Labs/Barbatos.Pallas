// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The history across runs: kept when the calculator closes, shown when it opens, and forgotten when the application
/// it belonged to is left.
/// </summary>
public sealed class SessionHistoryTests
{
    [Fact]
    public void WhatWasCalculatedIsThereTheNextTimeTheCalculatorOpens()
    {
        CalculatorShellViewModel first = Shell.Create(out ISessionStore store);
        first.Session.Calculate("1+1");
        first.Session.Calculate("2×3");
        first.Save();

        CalculatorShellViewModel second = new(Shell.Session(), store);
        second.Load().Should().BeTrue();

        second.Calculate.History.Select(entry => entry.Input).Should().Equal(["2×3", "1+1"], "newest first, as in the run it came from");
        second.Calculate.History.Select(entry => entry.Text).Should().Equal(["6", "2"]);
    }

    [Fact]
    public void ThisRunsCalculationsComeAfterTheEarlierRuns()
    {
        CalculatorShellViewModel shell = Restored([new HistoryEntry("1+1", "2")]);

        shell.Session.Calculate("5×5");

        shell.Calculate.History.Select(entry => entry.Input).Should().Equal("5×5", "1+1");
        shell.Calculate.History[0].Calculation.Should().NotBeNull();
        shell.Calculate.History[1].Calculation.Should().BeNull();
    }

    [Fact]
    public void ALineOfAnEarlierRunComesBackToBeCalculatedAgain()
    {
        CalculatorShellViewModel shell = Restored([new HistoryEntry("√(2)×3", "3√2")]);

        shell.Calculate.RecallPrevious().Should().BeTrue();

        shell.Calculate.Input.Linear.Should().Be("√(2)×3");
        shell.Calculate.Display.Should().BeNull("what it came to depended on memories that may have changed since");
        shell.Calculate.Input.Document.Root[0].Should().BeOfType<MathStructure>("it comes back as the structures it was typed with");
    }

    [Fact]
    public void LeavingTheApplicationForgetsItsHistoryAsTheEngineDoes()
    {
        CalculatorShellViewModel shell = Restored([new HistoryEntry("1+1", "2")]);
        List<string?> changed = shell.Calculate.Changes();

        shell.Open(CalculatorApps.Of(CalculatorApp.Complex));

        shell.Calculate.History.Should().BeEmpty("the history belongs to the application it was made in (pp. 35, 37)");
        changed.Should().Contain(nameof(CalculateViewModel.History));

        shell.Open(CalculatorApps.Of(CalculatorApp.Calculate));

        shell.Calculate.History.Should().BeEmpty("coming back does not bring back what was forgotten");
    }

    [Fact]
    public void OpeningTheApplicationTheSessionIsInKeepsItsHistory()
    {
        CalculatorShellViewModel shell = Restored([new HistoryEntry("1+1", "2")]);

        shell.Open(CalculatorApps.Of(CalculatorApp.Calculate));

        shell.Calculate.History.Should().ContainSingle();
    }

    [Fact]
    public void EveryScreenOfTheSessionShowsTheSameHistory()
    {
        CalculatorShellViewModel shell = Restored([new HistoryEntry("MatA", "[1]")], CalculatorApp.Matrix);

        shell.Matrix.Calculate.History.Should().ContainSingle().Which.Input.Should().Be("MatA");
        shell.History.Entries.Should().ContainSingle();
    }

    [Fact]
    public void OnlyTheNewestLinesAreKeptForTheNextRun()
    {
        CalculatorSession session = Shell.Session();
        SessionHistory history = new(session);
        history.Restore([.. Enumerable.Range(0, SessionHistory.Kept).Select(index => new HistoryEntry(index.ToString(System.Globalization.CultureInfo.InvariantCulture), string.Empty))]);

        session.Calculate("1+1");

        ImmutableArray<HistoryEntry> kept = history.ToKeep;
        kept.Should().HaveCount(SessionHistory.Kept);
        kept[0].Input.Should().Be("1", "the oldest line went to make room");
        kept[^1].Input.Should().Be("1+1", "and the newest is the last");
        history.Entries.Should().HaveCount(SessionHistory.Kept + 1, "this run still shows everything");
    }

    [Fact]
    public void AHistoryShorterThanWhatIsKeptIsKeptWhole()
    {
        CalculatorSession session = Shell.Session();
        SessionHistory history = new(session);
        session.Calculate("1+1");

        history.ToKeep.Should().Equal(history.Entries);
    }

    [Fact]
    public void AFailedCalculationIsALineWithNothingItCameTo()
    {
        CalculatorSession session = Shell.Session();
        Calculation failed = session.Calculate("1÷0");

        HistoryEntry.Of(failed).Should().Be(new HistoryEntry("1÷0", string.Empty, failed));
    }

    [Fact]
    public void ASessionAndItsLinesAreRequired()
    {
        Action session = () => _ = new SessionHistory(null!);
        Action entries = () => new SessionHistory(Shell.Session()).Restore(null!);
        Action calculation = () => HistoryEntry.Of(null!);

        session.Should().Throw<ArgumentNullException>().WithParameterName("session");
        entries.Should().Throw<ArgumentNullException>().WithParameterName("entries");
        calculation.Should().Throw<ArgumentNullException>().WithParameterName("calculation");
    }

    [Fact]
    public void AScreenOfItsOwnHasThisRunsHistory()
    {
        CalculatorSession session = Shell.Session();
        CalculateViewModel screen = new(session);

        session.Calculate("4+4");

        screen.History.Should().ContainSingle().Which.Text.Should().Be("8");
    }

    private static CalculatorShellViewModel Restored(ImmutableArray<HistoryEntry> history, CalculatorApp app = CalculatorApp.Calculate)
    {
        InMemorySessionStore store = new();
        store.Save(new StoredSession(Shell.Session(app).Capture(), history));
        CalculatorShellViewModel shell = new(Shell.Session(), store);
        shell.Load().Should().BeTrue();
        return shell;
    }
}
