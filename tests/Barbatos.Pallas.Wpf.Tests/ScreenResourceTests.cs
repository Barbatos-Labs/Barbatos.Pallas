// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Presentation;
using Barbatos.Pallas.Wpf.Views;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// Every screen is built once, with the application's own theme loaded.
/// </summary>
/// <remarks>
/// A <c>StaticResource</c> that names a key no dictionary has compiles and then throws the moment the screen is
/// built, which no other test in the repository would see: the view models are tested without a window, and the
/// LaTeX tests draw formulas rather than screens. This is the test that a renamed colour or a deleted style breaks.
/// </remarks>
public sealed class ScreenResourceTests
{
    [Fact]
    public void EveryScreenIsBuiltWithTheThemeItAsksFor()
    {
        List<string> failed = [];

        OnStaThread(() =>
        {
            LoadTheme();
            CalculatorShellViewModel shell = Shell();
            foreach ((CalculatorApp? app, Func<CalculatorShellViewModel, UserControl> screen) in Screens)
            {
                try
                {
                    // A screen of an application works on a session that is in it, as the router's guard arranges.
                    if (app is { } opened)
                    {
                        shell.Open(CalculatorApps.Of(opened));
                    }

                    UserControl built = screen(shell);
                    built.Should().NotBeNull();
                }
                catch (Exception error)
                {
                    failed.Add(error.Message);
                }
            }
        });

        failed.Should().BeEmpty();
    }

    private static IEnumerable<(CalculatorApp? App, Func<CalculatorShellViewModel, UserControl> Screen)> Screens =>
    [
        (null, shell => new HomeView(shell)),
        (null, shell => new SettingsView(shell)),
        (null, shell => new AppScreenView(shell)),
        (CalculatorApp.Calculate, shell => new CalculateView(shell)),
        (null, _ => new CalculationPanelView()),
        (null, _ => new KeypadView()),
        (null, _ => new ValueGridView()),
        (CalculatorApp.BaseN, shell => new BaseNView(shell)),
        (CalculatorApp.Matrix, shell => new MatrixView(shell)),
        (CalculatorApp.Vector, shell => new VectorView(shell)),
        (CalculatorApp.Statistics, shell => new StatisticsView(shell)),
        (CalculatorApp.Distribution, shell => new DistributionView(shell)),
        (CalculatorApp.Equation, shell => new EquationView(shell)),
        (CalculatorApp.Inequality, shell => new InequalityView(shell)),
        (CalculatorApp.Ratio, shell => new RatioView(shell)),
        (CalculatorApp.Table, shell => new TableView(shell)),
        (CalculatorApp.Spreadsheet, shell => new SpreadsheetView(shell)),
    ];

    /// <summary>One shell over one engine, as the application has.</summary>
    private static CalculatorShellViewModel Shell()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
        return new CalculatorShellViewModel(engine.CreateSession(), new InMemorySessionStore());
    }

    /// <summary>Puts the application's own dictionaries where a screen looks for them.</summary>
    private static void LoadTheme()
    {
        Application application = Application.Current ?? new Application();
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Barbatos.Pallas;component/Theme/Controls.xaml", UriKind.Absolute),
        });
    }

    /// <summary>Runs the body on a thread WPF will build controls on, and hands back what it threw.</summary>
    private static void OnStaThread(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                body();
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
