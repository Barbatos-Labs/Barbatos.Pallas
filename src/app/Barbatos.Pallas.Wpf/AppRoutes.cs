// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Presentation;
using Barbatos.Pallas.Wpf.Views;
using Barbatos.Wpf.AquariusRouter.Routing;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The screens of the application and the paths that reach them.
/// </summary>
/// <remarks>
/// The thirteen application routes are built from <see cref="CalculatorApps.All"/> rather than written out, so the
/// registry stays the only list of what the calculator does: adding an application to it adds its route, its place on
/// the home screen and its name in every language at once. Each one renders the screen of its application.
/// </remarks>
public static class AppRoutes
{
    /// <summary>The key the matched application is carried under, for a screen to read from its route.</summary>
    public const string AppKey = "app";

    /// <summary>Returns the screen an application is shown on.</summary>
    /// <param name="app">The application.</param>
    /// <returns>Its screen.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="app"/> is not an application of the calculator.</exception>
    public static Type Screen(CalculatorApp app) => app switch
    {
        CalculatorApp.Calculate or CalculatorApp.Complex => typeof(CalculateView),
        CalculatorApp.BaseN => typeof(BaseNView),
        CalculatorApp.Matrix => typeof(MatrixView),
        CalculatorApp.Vector => typeof(VectorView),
        CalculatorApp.Statistics => typeof(StatisticsView),
        CalculatorApp.Distribution => typeof(DistributionView),
        CalculatorApp.Equation => typeof(EquationView),
        CalculatorApp.Inequality => typeof(InequalityView),
        CalculatorApp.Ratio => typeof(RatioView),
        CalculatorApp.Table => typeof(TableView),
        CalculatorApp.Spreadsheet => typeof(SpreadsheetView),
        CalculatorApp.MathBox => typeof(MathBoxView),
        _ => throw new ArgumentOutOfRangeException(nameof(app), app, "Not an application of the calculator."),
    };

    /// <summary>Builds the route table.</summary>
    /// <returns>The routes, home first.</returns>
    public static IReadOnlyList<RouteRecord> Build()
    {
        List<RouteRecord> routes =
        [
            new RouteRecord { Path = "/", Name = "home", View = typeof(HomeView) },
            new RouteRecord { Path = "/settings", Name = "settings", View = typeof(SettingsView) },
        ];

        foreach (CalculatorAppInfo app in CalculatorApps.All)
        {
            routes.Add(new RouteRecord
            {
                Path = app.Route,
                Name = app.Route.TrimStart('/'),
                View = Screen(app.App),
                Meta = new Dictionary<string, object?> { [AppKey] = app },
            });
        }

        return routes;
    }
}
