// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The shell: one session for every application, and the session that is kept between runs.
/// </summary>
public sealed class CalculatorShellViewModelTests
{
    [Fact]
    public void TheShellOpensOnTheApplicationItsSessionIsIn()
    {
        CalculatorShellViewModel shell = Shell.Create(app: CalculatorApp.Statistics);

        shell.CurrentApp.App.Should().Be(CalculatorApp.Statistics);
        shell.Apps.Should().Equal(CalculatorApps.All);
        shell.Session.App.Should().Be(CalculatorApp.Statistics);
    }

    [Fact]
    public void OpeningAnApplicationSwitchesTheSessionToIt()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string?> changed = shell.Changes();

        shell.OpenCommand.Execute(CalculatorApps.Of(CalculatorApp.Matrix));

        shell.CurrentApp.App.Should().Be(CalculatorApp.Matrix);
        shell.Session.App.Should().Be(CalculatorApp.Matrix);
        changed.Should().Equal([nameof(CalculatorShellViewModel.CurrentApp)]);
    }

    [Fact]
    public void TheMemoriesAndTheSettingsFollowTheUserFromOneApplicationToTheNext()
    {
        // One calculator, one memory (pp. 36-40): what A holds in Calculate is what A holds in Statistics.
        CalculatorShellViewModel shell = Shell.Create();
        shell.Session.SetVariable(MemoryVariable.A, Value.FromDecimal(7));
        shell.Settings.AngleUnit = AngleUnit.Radian;

        shell.Open(CalculatorApps.Of(CalculatorApp.Statistics));

        shell.Session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(7m);
        shell.Settings.AngleUnit.Should().Be(AngleUnit.Radian);
    }

    [Fact]
    public void OpeningAnApplicationTurnsVerifyOffAndTheScreenSaysSo()
    {
        CalculatorShellViewModel shell = Shell.Create();
        shell.Settings.Verify = true;
        List<string?> changed = shell.Settings.Changes();

        shell.Open(CalculatorApps.Of(CalculatorApp.Complex));

        shell.Settings.Verify.Should().BeFalse("Verify belongs to Calculate (p. 73)");
        changed.Should().Equal([string.Empty]);
    }

    [Fact]
    public void AnApplicationThisBuildHasNotIsNotOpened()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string?> changed = shell.Changes();

        shell.Open(CalculatorApps.Of(CalculatorApp.MathBox));

        shell.CurrentApp.App.Should().Be(CalculatorApp.Calculate, "a disabled entry does nothing rather than throwing");
        shell.Session.App.Should().Be(CalculatorApp.Calculate);
        changed.Should().BeEmpty();
    }

    [Fact]
    public void OpeningNothingDoesNothing()
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.Open(null);

        shell.CurrentApp.App.Should().Be(CalculatorApp.Calculate);
    }

    [Theory]
    [InlineData("/vector", true, CalculatorApp.Vector)]
    [InlineData("ratio", true, CalculatorApp.Ratio)]
    [InlineData("/math-box", false, CalculatorApp.Calculate)]
    [InlineData("/nowhere", false, CalculatorApp.Calculate)]
    [InlineData(null, false, CalculatorApp.Calculate)]
    public void ARouteOpensItsApplication(string? route, bool opened, CalculatorApp expected)
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.OpenRoute(route).Should().Be(opened);

        shell.CurrentApp.App.Should().Be(expected);
    }

    [Fact]
    public void WhatWasSavedIsWhatComesBack()
    {
        CalculatorShellViewModel saved = Shell.Create(out ISessionStore store);
        saved.Open(CalculatorApps.Of(CalculatorApp.Statistics));
        saved.Settings.AngleUnit = AngleUnit.Gradian;
        saved.Session.SetVariable(MemoryVariable.B, Value.FromDecimal(0.125m));
        saved.Session.Calculate("√(2)");

        saved.Save();

        CalculatorShellViewModel started = new(Shell.Session(), store);
        started.Load().Should().BeTrue();

        started.CurrentApp.App.Should().Be(CalculatorApp.Statistics);
        started.Session.GetVariable(MemoryVariable.B).ToDecimal().Should().Be(0.125m);
        started.Settings.AngleUnit.Should().Be(AngleUnit.Gradian);
        started.Session.Calculate("Ans").Display.Text.Should().Be("√(2)", "a value keeps its exact form across a run");
    }

    [Fact]
    public void TheSettingsScreenIsToldWhatARestoredSessionBrought()
    {
        CalculatorShellViewModel saved = Shell.Create(out ISessionStore store);
        saved.Settings.DecimalMark = DecimalMark.Comma;
        saved.Save();

        CalculatorShellViewModel started = new(Shell.Session(), store);
        List<string?> changed = started.Settings.Changes();

        started.Load();

        changed.Should().Equal([string.Empty]);
        started.Settings.DecimalMark.Should().Be(DecimalMark.Comma);
    }

    [Fact]
    public void AFirstRunHasNothingToLoad()
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.Load().Should().BeFalse();

        shell.CurrentApp.App.Should().Be(CalculatorApp.Calculate);
    }

    [Fact]
    public void ASnapshotThisBuildCannotReadLeavesTheCalculatorStarting()
    {
        // A calculator that will not start because of what it remembers is worse than one that starts empty.
        CalculatorSession session = Shell.Session();
        StoredSnapshot store = new(session.Capture() with { App = CalculatorApp.MathBox });
        CalculatorShellViewModel shell = new(session, store);

        shell.Load().Should().BeFalse();

        shell.CurrentApp.App.Should().Be(CalculatorApp.Calculate);
    }

    [Fact]
    public void ASnapshotOfALaterVersionLeavesTheCalculatorStarting()
    {
        CalculatorSession session = Shell.Session();
        StoredSnapshot store = new(session.Capture() with { Version = SessionSnapshot.CurrentVersion + 1 });
        CalculatorShellViewModel shell = new(session, store);

        shell.Load().Should().BeFalse();
    }

    [Theory]
    [InlineData(KeyId.Home, "/")]
    [InlineData(KeyId.Settings, "/settings")]
    public void AKeyForAScreenAsksTheHostToGoThere(KeyId key, string route)
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string> routes = [];
        shell.NavigationRequested += (_, asked) => routes.Add(asked);

        shell.Calculate.Input.Press(key);

        routes.Should().Equal(route);
    }

    [Fact]
    public void AKeyThatIsNotAScreenAsksForNothing()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string> routes = [];
        shell.NavigationRequested += (_, asked) => routes.Add(asked);

        shell.Calculate.Input.Press(KeyId.One);
        shell.Calculate.Input.Press(KeyId.Execute);

        routes.Should().BeEmpty();
    }

    [Fact]
    public void AShellWithoutASessionOrAStoreIsRefused()
    {
        Action withoutSession = () => _ = new CalculatorShellViewModel(null!, new InMemorySessionStore());
        Action withoutStore = () => _ = new CalculatorShellViewModel(Shell.Session(), null!);

        withoutSession.Should().Throw<ArgumentNullException>();
        withoutStore.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AStoreKeepsOnlyTheLastSession()
    {
        InMemorySessionStore store = new();
        CalculatorSession session = Shell.Session();

        store.Load().Should().BeNull();
        session.SetVariable(MemoryVariable.C, Value.FromDecimal(1));
        store.Save(new StoredSession(session.Capture()));
        session.SetVariable(MemoryVariable.C, Value.FromDecimal(2));
        store.Save(new StoredSession(session.Capture()));

        CalculatorSession restored = Shell.Session();
        restored.Restore(store.Load()!.Snapshot);
        restored.GetVariable(MemoryVariable.C).ToDecimal().Should().Be(2m);
    }

    private sealed class StoredSnapshot : ISessionStore
    {
        private StoredSession _session;

        public StoredSnapshot(SessionSnapshot snapshot) => _session = new StoredSession(snapshot);

        public StoredSession? Load() => _session;

        public void Save(StoredSession session) => _session = session;
    }
}
