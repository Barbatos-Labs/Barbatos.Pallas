// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
/// Every screen is built on one thread with the theme loaded, as the application builds them: a style belongs to the
/// thread that read it, so a second thread could not use the theme a first one loaded.
/// </remarks>
public sealed class ScreenResourceTests
{
    private static readonly Lazy<Dispatcher> Ui = new(StartUi);

    [Fact]
    public void EveryScreenIsBuiltWithTheThemeItAsksFor()
    {
        List<string> failed = [];

        OnUiThread(() =>
        {
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

    [Fact]
    public void TheKeypadTakesKeysAndNotText()
    {
        // Windows' Vietnamese input method takes keys of the digit row, and WPF then reports them as ImeProcessed:
        // with it on, digits typed on a Vietnamese keyboard never reached the line (measured 23 Sep 2026).
        OnUiThread(() => InputMethod.GetIsInputMethodEnabled(new KeypadView()).Should().BeFalse());
    }

    [Fact]
    public void TheLanguageIsChosenOnTheSettingsScreen()
    {
        OnUiThread(() =>
        {
            CalculatorShellViewModel shell = Shell();
            SettingsView view = new(shell);
            ComboBox choice = (ComboBox)LogicalTreeHelper.FindLogicalNode(view, "LanguageChoice");

            // The bindings of a screen that is not shown yet are applied by the dispatcher, as the window's would be.
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            choice.Items.Cast<string>().Should().Equal(CalculatorShellViewModel.Languages);
            choice.SelectedItem.Should().Be(CalculatorShellViewModel.SystemLanguage);
            choice.SelectedItem = "vi-VN";

            shell.Language.Should().Be("vi-VN", "the choice is the shell's, not the calculator settings'");
            view.DataContext.Should().BeSameAs(shell.Settings);
        });
    }

    private static IEnumerable<(CalculatorApp? App, Func<CalculatorShellViewModel, UserControl> Screen)> Screens =>
    [
        (null, shell => new HomeView(shell)),
        (null, shell => new SettingsView(shell)),
        (null, shell => new AppScreenView(shell)),
        (CalculatorApp.Calculate, shell => new CalculateView(shell)),
        (null, _ => new CalculationPanelView()),
        (null, _ => new KeypadView()),
        (null, _ => new CalculatorMenuView()),
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

    /// <summary>Runs the body on the thread the screens are built on, and hands back what it threw.</summary>
    private static void OnUiThread(Action body) => Ui.Value.Invoke(body);

    /// <summary>Starts the thread every screen is built on, and loads the theme on it.</summary>
    private static Dispatcher StartUi()
    {
        Dispatcher? dispatcher = null;
        ExceptionDispatchInfo? failure = null;
        using ManualResetEventSlim ready = new();
        Thread thread = new(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            try
            {
                LoadTheme();
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
            finally
            {
                ready.Set();
            }

            Dispatcher.Run();
        })
        {
            IsBackground = true,
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        failure?.Throw();
        return dispatcher!;
    }
}
