// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// The session in the preferences: one string under one name, which every later build reads.
/// </summary>
public sealed class PreferencesSessionStoreTests
{
    private static CalculatorSession Session(CalculatorApp app = CalculatorApp.Calculate) =>
        PallasEngineBuilder.CreateDefault().Build().CreateSession(app);

    [Fact]
    public void WhatWasStoredIsWhatComesBack()
    {
        FakePreferences preferences = new();
        PreferencesSessionStore store = new(preferences);
        CalculatorSession saved = Session(CalculatorApp.Statistics);
        saved.SetVariable(MemoryVariable.A, Value.FromDecimal(1m / 3m));
        saved.Calculate("√(2)");

        store.Save(saved.Capture());

        CalculatorSession restored = Session();
        restored.Restore(store.Load()!);
        restored.App.Should().Be(CalculatorApp.Statistics);
        restored.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(1m / 3m);
        restored.Calculate("Ans×Ans").Display.Text.Should().Be("2");
    }

    [Fact]
    public void AFirstRunHasNothingStored()
    {
        PreferencesSessionStore store = new(new FakePreferences());

        store.Load().Should().BeNull();
    }

    [Fact]
    public void WhatWasStoredBySomethingElseIsNoSession()
    {
        FakePreferences preferences = new();
        preferences.Set(PreferencesSessionStore.Key, "not a session");

        new PreferencesSessionStore(preferences).Load().Should().BeNull();
    }

    [Fact]
    public void TheSessionIsOneValueUnderOneName()
    {
        FakePreferences preferences = new();

        new PreferencesSessionStore(preferences).Save(Session().Capture());

        preferences.Values.Should().ContainSingle().Which.Key.Should().Be("session");
    }

    [Fact]
    public void PreferencesAreRequired()
    {
        Action act = () => _ = new PreferencesSessionStore(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class FakePreferences : IPreferences
    {
        public Dictionary<string, object?> Values { get; } = [];

        public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(key);

        public void Remove(string key, string? sharedName = null) => Values.Remove(key);

        public void Clear(string? sharedName = null) => Values.Clear();

        public void Set<T>(string key, T value, string? sharedName = null) => Values[key] = value;

        public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
            Values.TryGetValue(key, out object? value) && value is T stored ? stored : defaultValue;
    }
}
