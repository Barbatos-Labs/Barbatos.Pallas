// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One application of the calculator, as the home screen lists it and the router reaches it (manual p. 20).
/// </summary>
/// <param name="App">Which application.</param>
/// <param name="Route">Its route, such as <c>/calculate</c>.</param>
/// <param name="NameKey">The localization key of its name, such as <c>app.calculate</c>.</param>
/// <param name="IsAvailable">Whether this build has its engine; Math Box arrives in Phase 6.</param>
public sealed record CalculatorAppInfo(CalculatorApp App, string Route, string NameKey, bool IsAvailable);

/// <summary>
/// The applications, in the order of the calculator's home screen.
/// </summary>
/// <remarks>
/// The registry is the only list: the home screen, the router table and the application menu all read it, so adding an
/// application touches nothing else. An application without an engine is listed and disabled rather than hidden, so
/// what the calculator has and what this build can do stay comparable.
/// </remarks>
public static class CalculatorApps
{
    /// <summary>Gets every application, in the order the home screen shows them.</summary>
    public static ImmutableArray<CalculatorAppInfo> All { get; } =
    [
        Entry(CalculatorApp.Calculate, "calculate"),
        Entry(CalculatorApp.Statistics, "statistics"),
        Entry(CalculatorApp.Distribution, "distribution"),
        Entry(CalculatorApp.Spreadsheet, "spreadsheet"),
        Entry(CalculatorApp.Table, "table"),
        Entry(CalculatorApp.Equation, "equation"),
        Entry(CalculatorApp.Inequality, "inequality"),
        Entry(CalculatorApp.Complex, "complex"),
        Entry(CalculatorApp.BaseN, "base-n"),
        Entry(CalculatorApp.Matrix, "matrix"),
        Entry(CalculatorApp.Vector, "vector"),
        Entry(CalculatorApp.Ratio, "ratio"),
        Entry(CalculatorApp.MathBox, "math-box"),
    ];

    /// <summary>Gets the applications this build can open.</summary>
    public static ImmutableArray<CalculatorAppInfo> Available { get; } = [.. All.Where(app => app.IsAvailable)];

    /// <summary>Returns the entry of an application.</summary>
    /// <param name="app">The application.</param>
    /// <returns>Its entry.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="app"/> is not a defined value.</exception>
    public static CalculatorAppInfo Of(CalculatorApp app)
    {
        return All.FirstOrDefault(entry => entry.App == app)
            ?? throw new ArgumentOutOfRangeException(nameof(app), app, "Not a defined CalculatorApp value.");
    }

    /// <summary>Returns the entry a route belongs to, or <see langword="null"/>.</summary>
    /// <param name="route">The route, with or without its leading slash.</param>
    /// <returns>The entry, or <see langword="null"/>.</returns>
    public static CalculatorAppInfo? ByRoute(string? route)
    {
        string path = "/" + (route ?? string.Empty).TrimStart('/');
        return All.FirstOrDefault(entry => string.Equals(entry.Route, path, StringComparison.OrdinalIgnoreCase));
    }

    private static CalculatorAppInfo Entry(CalculatorApp app, string name, bool available = true)
    {
        return new CalculatorAppInfo(app, "/" + name, "app." + name, available);
    }
}
