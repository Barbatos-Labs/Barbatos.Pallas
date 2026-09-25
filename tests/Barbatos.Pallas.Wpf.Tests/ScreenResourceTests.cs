// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    public void EveryMathBoxToolIsDrawnWithTheTheme()
    {
        // A tool's screen is a template, built only when the tool is opened: building MathBoxView alone reads none of
        // them. Each is opened, filled and laid out here, and drawn, so every style and both drawings are used.
        OnUiThread(() =>
        {
            CalculatorShellViewModel shell = Shell();
            shell.Open(CalculatorApps.Of(CalculatorApp.MathBox));
            MathBoxView view = new(shell);
            MathBoxViewModel box = shell.MathBox;

            box.Dice.Count = 2;
            box.Dice.Execute();
            box.Coins.Count = 3;
            box.Coins.Execute();
            box.Coins.View = SimulationResultView.RelativeFrequency;
            box.NumberLine.Entries[0].Form = NumberLineForm.LessOrEqual;
            box.NumberLine.Entries[0].Bounds[0, 0].Text = "-1.5";
            box.NumberLine.Entries[1].Form = NumberLineForm.Greater;
            box.NumberLine.Entries[1].Bounds[0, 0].Text = "-1";
            box.NumberLine.Entries[2].Form = NumberLineForm.ToIncluded;
            box.NumberLine.Entries[2].Bounds[0, 0].Text = "-2";
            box.NumberLine.Entries[2].Bounds[0, 1].Text = "-0.5";
            box.NumberLine.Execute();
            box.Circle.Angles[0, 0].Text = "45";
            box.Circle.Angles[0, 1].Text = "90";
            box.Circle.Execute();

            foreach (MathBoxTool tool in box.Tools)
            {
                box.Open(tool);
                Draw(view).Should().BeGreaterThan(0, "{0} draws something", tool);
            }

            foreach (CircleScreen screen in box.Circle.Screens)
            {
                box.Circle.Screen = screen;
                box.Circle.Execute();
                Draw(view).Should().BeGreaterThan(0, "{0} draws something", screen);
            }

            box.Back();
            Draw(view).Should().BeGreaterThan(0, "the menu draws its tools");
        });
    }

    [Fact]
    public void TheGraphOfATableIsDrawnWithTheTheme()
    {
        // The graph is sampled at the size its drawing is laid out at, which only the view can say: laid out here, the
        // drawing has told the view model how large it is, and the curves, the rows, the named points and a reading
        // are all drawn. They are worked out off this thread and applied back on it, which Settle waits for.
        OnUiThread(() =>
        {
            CalculatorShellViewModel shell = Shell();
            shell.Open(CalculatorApps.Of(CalculatorApp.Table));
            TableView view = new(shell);
            TableViewModel table = shell.Table;
            table.IsGraphShown = true;
            Draw(view).Should().BeGreaterThan(0, "the display says there is nothing to draw yet");

            table.FunctionF = "1÷x";
            table.FunctionG = "x²−2";
            table.Range[0, 0].Text = "-2";
            table.Range[0, 1].Text = "2";
            table.Generate();
            table.Graph.IsDrawing.Should().BeTrue();
            Draw(view).Should().BeGreaterThan(0, "the display says it is drawing");
            Settle(table.Graph.Drawing);
            Draw(view).Should().BeGreaterThan(0);

            table.Graph.IsDrawing.Should().BeFalse();
            table.Graph.Curves.Should().HaveCount(2, "the drawing said how large it is");
            table.Graph.Curves[0].Trace.Breaks.Should().NotBeEmpty("1÷x has an asymptote at 0");
            table.Graph.Features.Should().NotBeEmpty();

            table.Graph.Read(0.3);
            Settle(table.Graph.Drawing);
            table.Graph.Reading.Should().NotBeNull();
            Draw(view).Should().BeGreaterThan(0, "a reading is drawn over the graph");

            table.Graph.ZoomIn();
            Settle(table.Graph.Drawing);
            Draw(view).Should().BeGreaterThan(0);
            table.Graph.Curves.Should().OnlyContain(curve => curve.Trace.Viewport == table.Graph.Viewport);
        });
    }

    [Fact]
    public void AValueWhoseCalculationFailedIsShownAsItsError()
    {
        // U34: a value of a table or a cell of the sheet whose calculation failed shows its error where its value would
        // be; every other value shows no error.
        OnUiThread(() =>
        {
            CalculatorShellViewModel shell = Shell();
            shell.Open(CalculatorApps.Of(CalculatorApp.Table));
            TableView table = new(shell);
            shell.Table.FunctionF = "1÷x";
            shell.Table.Range[0, 0].Text = "-1";
            shell.Table.Range[0, 1].Text = "1";
            shell.Table.Generate();
            Draw(table).Should().BeGreaterThan(0);
            ErrorsShown(table).Should().Equal(["error.MathError"], "f(0) is 1÷0, and nothing else of the three rows fails");

            shell.Open(CalculatorApps.Of(CalculatorApp.Spreadsheet));
            SpreadsheetView sheet = new(shell);
            shell.Spreadsheet.Selected = new CellAddress(0, 0);
            shell.Spreadsheet.Input = "=1÷0";
            shell.Spreadsheet.Commit();
            shell.Spreadsheet.Selected = new CellAddress(1, 0);
            shell.Spreadsheet.Input = "=2";
            shell.Spreadsheet.Commit();
            Draw(sheet).Should().BeGreaterThan(0);
            ErrorsShown(sheet).Should().Equal(["error.MathError"], "A1 is 1÷0, and B1 is 2");
        });
    }

    [Fact]
    public void TheCoverIsOverTheScreenOnlyWhileTheSessionCalculates()
    {
        // The cover takes the clicks from the moment a work starts; what it says is hidden until the work has run for
        // BusyDelay, so a calculation that is over in a moment shows nothing.
        OnUiThread(() =>
        {
            using SessionWork work = new(offThread: true);
            CalculatorShellViewModel shell = new(PallasEngineBuilder.CreateDefault().Build().CreateSession(), new InMemorySessionStore(), work);
            BusyView cover = new() { DataContext = work };
            Border panel = (Border)cover.Content;

            // The bindings of a view are applied by the dispatcher once it is built, as the window's are; from then on
            // the cover follows the work at once, before the next key or click is taken.
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            cover.Visibility.Should().Be(Visibility.Collapsed);

            shell.Calculate.Input.Set(MathDocumentReader.Read("Σ(x,1,10^7)"));
            shell.Calculate.Execute();
            cover.Visibility.Should().Be(Visibility.Visible, "the screen takes no click while the session is lent");
            cover.IsHitTestVisible.Should().BeTrue();
            panel.Visibility.Should().Be(Visibility.Hidden, "nothing is said before the calculation has run a while");
            Draw(cover).Should().Be(0, "the cover is not seen");

            work.Cancel();
            Settle(work.Completion);
            cover.Visibility.Should().Be(Visibility.Collapsed);
            shell.Calculate.Calculation.Should().BeNull("AC stopped the calculation");
        });
    }

    [Fact]
    public void TheKeypadTakesKeysAndNotText()
    {
        // Windows' Vietnamese input method takes keys of the digit row, and WPF then reports them as ImeProcessed:
        // with it on, digits typed on a Vietnamese keyboard never reached the line (measured 23 Sep 2026).
        OnUiThread(() => InputMethod.GetIsInputMethodEnabled(new KeypadView()).Should().BeFalse());
    }

    [Fact]
    public void EveryBoxAnExpressionIsTypedIntoTakesKeysAndNotText()
    {
        // The same input method held the digits typed into a box in a composition, and a click on a button, which takes
        // no focus, did not end it: Enter or Solve took what the box held before (measured 25 Sep 2026).
        OnUiThread(() =>
        {
            foreach (string style in (string[])["CellBox", "ValueCellBox"])
            {
                TextBox box = new() { Style = (Style)Application.Current.FindResource(style) };
                InputMethod.GetIsInputMethodEnabled(box).Should().BeFalse("{0} is a box an expression is typed into", style);
            }
        });
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
        (CalculatorApp.Calculate, shell => new CalculateView(shell)),
        (null, _ => new CalculationPanelView()),
        (null, _ => new KeypadView()),
        (null, _ => new CalculatorMenuView()),
        (null, _ => new ValueGridView()),
        (null, _ => new BusyView()),
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
        (CalculatorApp.MathBox, shell => new MathBoxView(shell)),
    ];

    /// <summary>Runs the window's thread until a work of a screen has come back to it, as the application does between frames.</summary>
    private static void Settle(Task work)
    {
        DispatcherFrame frame = new();
        _ = work.ContinueWith(_ => frame.Continue = false, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        Dispatcher.PushFrame(frame);
    }

    /// <summary>Lays a screen out at the window's size, draws it, and counts the pixels that are not blank.</summary>
    private static int Draw(UserControl screen)
    {
        Size size = new(760, 900);
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
        screen.Measure(size);
        screen.Arrange(new Rect(size));
        screen.UpdateLayout();

        RenderTargetBitmap bitmap = new((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(screen);
        int[] pixels = new int[(int)size.Width * (int)size.Height];
        bitmap.CopyPixels(pixels, (int)size.Width * 4, 0);
        return pixels.Count(pixel => pixel != 0);
    }

    /// <summary>The errors a screen laid out shows in the place of a value, as the keys of their text.</summary>
    /// <remarks>These tests load no language, so the text of a key is the key itself.</remarks>
    private static List<string> ErrorsShown(DependencyObject screen)
    {
        Style error = (Style)Application.Current.FindResource("CellError");
        List<string> shown = [];
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(screen); index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(screen, index);
            if (child is TextBlock { Text.Length: > 0 } block && block.Style == error)
            {
                shown.Add(block.Text);
            }

            shown.AddRange(ErrorsShown(child));
        }

        return shown;
    }

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
