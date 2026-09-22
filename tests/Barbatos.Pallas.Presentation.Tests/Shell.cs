// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.ComponentModel;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// One shell over one engine, as the application has, so a test reads like a user who opened the calculator.
/// </summary>
internal static class Shell
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().Build();

    /// <summary>Creates a session of the engine the tests share.</summary>
    public static CalculatorSession Session(CalculatorApp app = CalculatorApp.Calculate) => Engine.CreateSession(app, randomSeed: 880);

    /// <summary>Creates a shell over a new session and a store that lives as long as the test.</summary>
    public static CalculatorShellViewModel Create(out ISessionStore store, CalculatorApp app = CalculatorApp.Calculate)
    {
        store = new InMemorySessionStore();
        return new CalculatorShellViewModel(Session(app), store);
    }

    /// <summary>Creates a shell over a new session and a store nothing else looks at.</summary>
    public static CalculatorShellViewModel Create(CalculatorApp app = CalculatorApp.Calculate) => Create(out _, app);

    /// <summary>Records the properties an object says have changed, in order.</summary>
    public static List<string?> Changes(this INotifyPropertyChanged source)
    {
        List<string?> changed = [];
        source.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
        return changed;
    }
}
