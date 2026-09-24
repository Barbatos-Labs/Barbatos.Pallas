// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Graphing;
using Barbatos.Pallas.Spreadsheet;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The graph of the Table application: f(x) and g(x) of the session drawn over the x of the table, with its rows on
/// the curves.
/// </summary>
/// <remarks>
/// <para>
/// On the calculator a table becomes a graph through its QR code (manual pp. 77-78), drawn elsewhere; here it is drawn
/// beside the table, from the same functions of the same session. The view fits the rows of the table - every x of
/// the table, every value of it - and zooms from there.
/// </para>
/// <para>
/// Every value on the screen is the engine's: the curves are sampled by <see cref="GraphSampler"/> on the engine's
/// calculations, roots, extrema and intersections are named by <see cref="GraphAnalysis"/> with the engine's
/// calculation there, and a point read off the graph is calculated at the x it shows. The coordinates are
/// <see langword="double"/> because they only place things on a screen.
/// </para>
/// <para>
/// The view, its marks and the rows change at once; the curves, what the graph names and what it reads are worked out
/// off the window's thread (<see cref="Drawing"/>), each on a session of its own restored from the one the table was
/// generated in, because a session is used from one thread at a time. So the graph is always of the table as it was
/// generated, whatever the session has done since.
/// </para>
/// </remarks>
public sealed partial class TableGraphViewModel : ObservableObject
{
    /// <summary>How many intervals the x of the view is searched in for roots, extrema and intersections.</summary>
    /// <remarks>
    /// Fixed, rather than the width of the drawing, so that what the graph names does not change as the window is
    /// resized. Two roots closer than a four-hundredth of the view are one change of sign or none: zoom in to part them.
    /// </remarks>
    public const int AnalysisColumns = 400;

    // A view is zoomed out no further than ±10⁹⁹: from 9.9999999995×10⁹⁹ on every value is a Math ERROR in the Standard
    // profile, and within it the engine has a value for every x a view has, since the x of a table is a decimal. A view
    // is zoomed in no further than twelve digits of where it is, or than the profile's smallest number: a narrower one
    // shows nothing a wider one does not.
    private const double Reach = 1e99;
    private const double Resolution = 1e-12;
    private const double Smallest = 1e-99;

    // A spread of values the display's ten digits show as one number is no spread to fit.
    private const double Flat = 1e-9;

    private readonly CalculatorSession _session;
    private readonly GraphWork _sampling;
    private readonly GraphWork _analysis;
    private readonly GraphWork _pointer;
    private GraphSource? _source;
    private GraphViewport? _fitted;
    private int _columns;
    private int _rows;

    /// <summary>Creates the graph over a session.</summary>
    /// <param name="session">The session whose f(x) and g(x) are drawn.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public TableGraphViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _sampling = new GraphWork(Busy);
        _analysis = new GraphWork(Busy);
        _pointer = new GraphWork(() => { });
    }

    /// <summary>Gets the region of the plane shown, or <see langword="null"/> when there is no table to draw.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGraph))]
    [NotifyCanExecuteChangedFor(nameof(ZoomInCommand), nameof(ZoomOutCommand), nameof(FitCommand))]
    private GraphViewport? _viewport;

    /// <summary>Gets the curves, sampled at the pixels of the drawing; none until <see cref="Resize"/> has said how many.</summary>
    [ObservableProperty]
    private ImmutableArray<TableCurve> _curves = [];

    /// <summary>Gets the rows of the table, as points on their curves.</summary>
    [ObservableProperty]
    private ImmutableArray<TablePoint> _points = [];

    /// <summary>Gets the roots, extrema and intersections within the x of the view, left to right.</summary>
    [ObservableProperty]
    private ImmutableArray<TableGraphFeature> _features = [];

    /// <summary>Gets the marks of the x axis.</summary>
    [ObservableProperty]
    private ImmutableArray<GraphTick> _xTicks = [];

    /// <summary>Gets the marks of the y axis.</summary>
    [ObservableProperty]
    private ImmutableArray<GraphTick> _yTicks = [];

    /// <summary>Gets what the graph reads at the pointer, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private TableGraphReading? _reading;

    /// <summary>Gets whether the curves or what the graph names are still being worked out.</summary>
    [ObservableProperty]
    private bool _isDrawing;

    /// <summary>Gets whether there is a graph to draw.</summary>
    public bool HasGraph => Viewport is not null;

    /// <summary>Gets the work the graph is doing, completed once what it shows is what its view and its drawing ask for, and nothing more is on its way.</summary>
    /// <remarks>The result is applied where the work was started from: on the window's thread, in the application.</remarks>
    public Task Drawing => Task.WhenAll(_sampling.Completion, _analysis.Completion, _pointer.Completion);

    /// <summary>Says how large the drawing is, and samples the curves at its pixels.</summary>
    /// <param name="columns">Its width in pixels.</param>
    /// <param name="rows">Its height in pixels.</param>
    /// <remarks>
    /// A drawing with no area, such as one not shown, is sampled at nothing. The curves sampled at the size before
    /// stay until the new ones are ready: the view is the same, so they are drawn where they are.
    /// </remarks>
    public void Resize(int columns, int rows)
    {
        _columns = Math.Clamp(columns, 0, GraphSampler.MaximumColumns);
        _rows = Math.Clamp(rows, 0, GraphSampler.MaximumRows);
        Sample();
    }

    /// <summary>Reads the graph at a point of the drawing: x there, and the value of each function at that x.</summary>
    /// <param name="fraction">How far across the drawing the point is, from 0 at its left to 1 at its right.</param>
    /// <remarks>
    /// The x is written with the decimals a pixel tells apart and no more - 1.23, not 1.23456789012345 - and the
    /// functions are calculated at that x, so what is read is what the line would calculate for it. The x is a place
    /// on the axis, so it is written as decimals whatever the display's format, as the marks of the axes are; the
    /// values are calculations, written as the display writes any result: under MathO f(1.1) of x²−2 is -79⌟100.
    /// </remarks>
    public void Read(double fraction)
    {
        GraphViewport? view = Viewport;
        if (view is null || _columns == 0)
        {
            return;
        }

        GraphSource source = _source!;
        double x = view.Left + (view.Width * Math.Clamp(fraction, 0d, 1d));
        double pixel = view.Width / _columns;
        _pointer.Start(token => ReadAt(source, x, pixel, token), reading => Reading = reading);
    }

    /// <summary>Stops reading the graph: the pointer has left it.</summary>
    public void StopReading()
    {
        _pointer.Cancel();
        Reading = null;
    }

    /// <summary>Shows half as much of the plane, around the same center.</summary>
    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    public void ZoomIn() => View(Zoomed(0.5));

    /// <summary>Shows twice as much of the plane, around the same center.</summary>
    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    public void ZoomOut() => View(Zoomed(2d));

    /// <summary>Shows the rows of the table again, as the graph first did.</summary>
    [RelayCommand(CanExecute = nameof(CanFit))]
    public void Fit() => View(_fitted);

    /// <summary>Draws a table: its functions as they are defined now, over its rows.</summary>
    /// <param name="table">The table.</param>
    internal void Show(NumberTable table)
    {
        _source = new GraphSource(_session.Engine, _session.Profile, _session.Capture(), table.Type);

        // A value is not a number where the function has none there: that row has no point on that curve. An x given to
        // a row by hand (p. 110) can be what the Table application has no value for, a complex number: that row is not
        // drawn at all.
        List<double> xs = [];
        List<TablePoint> points = [];
        foreach (TableRow row in table.Rows)
        {
            double? x = Real(row.X);
            if (x is null)
            {
                continue;
            }

            xs.Add(x.Value);
            foreach (TableFunction function in _source.Functions)
            {
                double? y = Real(row.Value(function));
                if (y is not null)
                {
                    points.Add(new TablePoint(function, new GraphPoint(x.Value, y.Value)));
                }
            }
        }

        Points = [.. points];
        _fitted = xs.Count == 0 ? null : Fitted(xs, points);
        View(_fitted);
    }

    /// <summary>Draws nothing.</summary>
    internal void Clear()
    {
        _source = null;
        _fitted = null;
        Points = [];
        View(null);
    }

    private bool CanZoomIn() => Zoomed(0.5) is not null;

    private bool CanZoomOut() => Zoomed(2d) is not null;

    private bool CanFit() => Viewport is not null && Viewport != _fitted;

    /// <summary>Shows a view: its marks at once, its curves and what it names once they are worked out.</summary>
    private void View(GraphViewport? view)
    {
        Viewport = view;
        StopReading();
        Features = [];
        Curves = [];
        if (view is null)
        {
            XTicks = [];
            YTicks = [];
            _analysis.Cancel();
            _sampling.Cancel();
            return;
        }

        GraphSource source = _source!;
        XTicks = Ticks(view.Left, view.Right, source);
        YTicks = Ticks(view.Bottom, view.Top, source);
        _analysis.Start(token => Analyse(source, view, token), features => Features = features);
        Sample();
    }

    private void Sample()
    {
        GraphViewport? view = Viewport;
        (int columns, int rows) = (_columns, _rows);
        if (view is null || columns == 0 || rows == 0)
        {
            _sampling.Cancel();
            Curves = [];
            return;
        }

        GraphSource source = _source!;
        _sampling.Start(token => Sample(source, view, columns, rows, token), curves => Curves = curves);
    }

    private void Busy() => IsDrawing = _sampling.IsRunning || _analysis.IsRunning;

    private static ImmutableArray<TableCurve> Sample(GraphSource source, GraphViewport view, int columns, int rows, CancellationToken token) =>
        [.. source.Compile().Select(curve => new TableCurve(curve.Function, GraphSampler.Sample(curve.Expression, view, columns, rows, token)))];

    private static ImmutableArray<TableGraphFeature> Analyse(GraphSource source, GraphViewport view, CancellationToken token)
    {
        List<(TableFunction Function, CompiledExpression Expression)> functions = source.Compile();
        List<TableGraphFeature> found = [];
        foreach ((TableFunction function, CompiledExpression expression) in functions)
        {
            found.AddRange(GraphAnalysis.Roots(expression, view, AnalysisColumns, token).Select(feature => Named(feature, function, source)));
            found.AddRange(GraphAnalysis.Extrema(expression, view, AnalysisColumns, token).Select(feature => Named(feature, function, source)));
        }

        if (functions.Count == 2)
        {
            found.AddRange(GraphAnalysis.Intersections(functions[0].Expression, functions[1].Expression, view, AnalysisColumns, token).Select(feature => Named(feature, null, source)));
        }

        return [.. found.OrderBy(feature => feature.At.X)];
    }

    private static TableGraphFeature Named(GraphFeature feature, TableFunction? function, GraphSource source) => new(
        feature.Kind,
        function,
        source.Text(feature.X),
        feature.Y.Display.Text,
        new GraphPoint(feature.X.ToDouble(), feature.Y.Result.ToDouble()));

    private static TableGraphReading ReadAt(GraphSource source, double x, double pixel, CancellationToken token)
    {
        // Every x of a view is within ±10⁹⁹ (Reach), where the engine has a value for it.
        List<(TableFunction Function, CompiledExpression Expression)> functions = source.Compile();
        Value at = Snap(x, pixel) ?? functions[0].Expression.ValueOf(x)!.Value;

        List<TableGraphValue> values = [];
        foreach ((TableFunction function, CompiledExpression expression) in functions)
        {
            Calculation y = expression.Evaluate(at, token);
            values.Add(y.Succeeded
                ? new TableGraphValue(function, y.Display.Text, null, y.Result.ToDouble())
                : new TableGraphValue(function, null, "error." + y.Error!.Value.Kind, null));
        }

        return new TableGraphReading(at.ToDouble(), source.Decimal(at), [.. values]);
    }

    /// <summary>The view that shows every row: every x of the table, and every value of it.</summary>
    private static GraphViewport Fitted(List<double> xs, List<TablePoint> points)
    {
        // A table whose every value is an error still has its x: the x axis is drawn through the middle.
        (double left, double right) = Around(xs.Min(), xs.Max(), 20);
        (double bottom, double top) = points.Count == 0
            ? Around(0d, 0d, 10)
            : Around(points.Min(point => point.At.Y), points.Max(point => point.At.Y), 10);
        return new GraphViewport(left, right, bottom, top);
    }

    /// <summary>A range from <paramref name="min"/> to <paramref name="max"/> and a <paramref name="parts"/>th of it either side.</summary>
    /// <remarks>A single number, or numbers too close to tell apart, is shown from half itself below to half itself above, or from -1 to 1 for 0.</remarks>
    private static (double Low, double High) Around(double min, double max, int parts)
    {
        double size = Math.Max(Math.Abs(min), Math.Abs(max));
        double spread = max - min;
        if (spread > size * Flat)
        {
            return (min - (spread / parts), max + (spread / parts));
        }

        double half = size > 0d ? size / 2d : 1d;
        return (min - half, max + half);
    }

    /// <summary>
    /// The view around the same center, <paramref name="factor"/> times as wide and as high; none where zooming in would
    /// tell nothing more apart, or zooming out would reach past ±10⁹⁹.
    /// </summary>
    /// <remarks>
    /// Each way has its own limit: a table whose values are near 9×10⁹⁹ is fitted beyond ±10⁹⁹ already, and is zoomed
    /// into all the same.
    /// </remarks>
    private GraphViewport? Zoomed(double factor)
    {
        GraphViewport? view = Viewport;
        if (view is null)
        {
            return null;
        }

        double x = view.Left + (view.Width / 2d);
        double y = view.Bottom + (view.Height / 2d);
        double width = view.Width * factor / 2d;
        double height = view.Height * factor / 2d;
        bool shows = factor < 1d ? Resolves(x, width) && Resolves(y, height) : Within(x, width) && Within(y, height);
        return shows ? new GraphViewport(x - width, x + width, y - height, y + height) : null;
    }

    // Half a view is more than twelve digits of its center, and more than the smallest number.
    private static bool Resolves(double center, double half) => half > Math.Max(Math.Abs(center) * Resolution, Smallest);

    private static bool Within(double center, double half) => Math.Abs(center) + half <= Reach;

    /// <summary>The marks of an axis: a step of 1, 2 or 5 times a power of ten, about six to a view.</summary>
    /// <remarks>
    /// A mark is labelled with the engine's display of its value, as decimals: k times the step in
    /// <see langword="double"/> is 0.30000000000000004 for 3 × 0.1, which a value's fifteen digits make 0.3.
    /// </remarks>
    private static ImmutableArray<GraphTick> Ticks(double low, double high, GraphSource source)
    {
        double rough = (high - low) / 6d;
        double power = Math.Pow(10d, Math.Floor(Math.Log10(rough)));
        double leading = rough / power;
        double step = power * (leading <= 1d ? 1d : leading <= 2d ? 2d : leading <= 5d ? 5d : 10d);

        List<GraphTick> ticks = [];
        for (double k = Math.Ceiling(low / step); k * step <= high; k++)
        {
            ticks.Add(new GraphTick(k * step, source.Decimal(Value.FromDouble(k * step))));
        }

        return [.. ticks];
    }

    /// <summary>An x rounded to the decimals a pixel tells apart; none where that is more than fourteen, or x is too large for them.</summary>
    private static Value? Snap(double x, double pixel)
    {
        int places = Math.Max(0, (int)Math.Ceiling(-Math.Log10(pixel)));
        return places <= 14 && Math.Abs(x) < 1e15 ? Value.FromDecimal(Math.Round((decimal)x, places, MidpointRounding.AwayFromZero)) : null;
    }

    // The Table application calculates in real numbers: a complex value, even an x given to a row by hand, is a Math
    // ERROR there, so what succeeds has a double.
    private static double? Real(Calculation? cell) => cell is { Succeeded: true } ? cell.Result.ToDouble() : null;

    /// <summary>
    /// What a graph draws: the functions of a table as the session held them when the table was generated, with the
    /// settings it displayed them in.
    /// </summary>
    /// <remarks>
    /// Each work compiles the functions on a session of its own, restored from the snapshot: a session and what is
    /// compiled on it are used from one thread at a time, and two works - the curves and what they name - run at once.
    /// </remarks>
    private sealed class GraphSource(PallasEngine engine, CalculatorProfile profile, SessionSnapshot snapshot, TableType type)
    {
        /// <summary>Gets the functions the table holds, f(x) first.</summary>
        public ImmutableArray<TableFunction> Functions { get; } = type switch
        {
            TableType.FunctionF => [TableFunction.F],
            TableType.FunctionG => [TableFunction.G],
            _ => [TableFunction.F, TableFunction.G],
        };

        /// <summary>Compiles the functions on a session of their own.</summary>
        public List<(TableFunction Function, CompiledExpression Expression)> Compile()
        {
            CalculatorSession session = engine.CreateSession(snapshot.App, profile);
            session.Restore(snapshot);
            return [.. Functions.Select(function => (function, session.Compile(function == TableFunction.F ? "f(x)" : "g(x)")))];
        }

        /// <summary>Writes a value as the display writes a result.</summary>
        public string Text(Value value) => PallasEngine.Format(value, snapshot.Settings, profile)?.Text ?? string.Empty;

        /// <summary>Writes a place on an axis rather than a result: decimals, whatever the display's format.</summary>
        public string Decimal(Value value) =>
            PallasEngine.Format(value, SimulationViewModel.DecimalOutput(snapshot.Settings), profile)?.Text ?? string.Empty;
    }
}

/// <summary>One curve of the graph.</summary>
/// <param name="Function">Which function it is.</param>
/// <param name="Trace">Its pieces inside the view and where it breaks.</param>
public sealed record TableCurve(TableFunction Function, GraphTrace Trace);

/// <summary>A row of the table, as a point on its curve.</summary>
/// <param name="Function">Which function's value it is.</param>
/// <param name="At">Where it is.</param>
public sealed record TablePoint(TableFunction Function, GraphPoint At);

/// <summary>A root, an extremum or an intersection the graph names.</summary>
/// <param name="Kind">Which it is.</param>
/// <param name="Function">Whose it is; <see langword="null"/> for an intersection, which is both functions'.</param>
/// <param name="X">Its x, as the display writes it.</param>
/// <param name="Y">The engine's calculation there, as the display writes it.</param>
/// <param name="At">Where it is drawn.</param>
public sealed record TableGraphFeature(GraphFeatureKind Kind, TableFunction? Function, string X, string Y, GraphPoint At)
{
    /// <summary>Gets the function it belongs to as the screen names it: f(x), g(x), or both for an intersection.</summary>
    public string Name => Function switch
    {
        TableFunction.F => "f(x)",
        TableFunction.G => "g(x)",
        _ => "f(x) = g(x)",
    };
}

/// <summary>A mark on an axis.</summary>
/// <param name="At">Where it is, in units of the axis.</param>
/// <param name="Label">Its value, as the display writes it.</param>
public readonly record struct GraphTick(double At, string Label);

/// <summary>What the graph reads at the pointer.</summary>
/// <param name="At">The x, where the line is drawn.</param>
/// <param name="X">The x, as the display writes it.</param>
/// <param name="Values">The value of each function drawn, at that x.</param>
public sealed record TableGraphReading(double At, string X, ImmutableArray<TableGraphValue> Values);

/// <summary>The value of one function at the x read.</summary>
/// <param name="Function">Which function.</param>
/// <param name="Text">Its value as the display writes it, or <see langword="null"/> when it has none there.</param>
/// <param name="ErrorKey">The localization key of why it has none, or <see langword="null"/>.</param>
/// <param name="Y">Where the value is drawn; <see langword="null"/> for an error.</param>
public sealed record TableGraphValue(TableFunction Function, string? Text, string? ErrorKey, double? Y)
{
    /// <summary>Gets the function as the screen names it.</summary>
    public string Name => Function == TableFunction.F ? "f(x)" : "g(x)";
}
