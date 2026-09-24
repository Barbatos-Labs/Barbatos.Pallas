// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Barbatos.Pallas.Graphing;
using Barbatos.Pallas.Presentation;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The graph of a table: its grid and axes, its curves, the rows of the table on them, what the graph names and what
/// it reads at the pointer.
/// </summary>
/// <remarks>
/// Everything drawn is the view model's (<see cref="TableGraphViewModel"/>): the curves were sampled at this drawing's
/// pixels and clipped to its view, and the labels are the display's numbers. This only turns a point of the view into a
/// pixel, f(x) in <see cref="Ink"/> and g(x) in <see cref="Second"/>. The colours and the type are the theme's, set by
/// its style.
/// </remarks>
public sealed class GraphDrawing : FrameworkElement
{
    /// <summary>Identifies <see cref="Viewport"/>.</summary>
    public static readonly DependencyProperty ViewportProperty = Affecting<GraphViewport?>(nameof(Viewport), null);

    /// <summary>Identifies <see cref="Curves"/>.</summary>
    public static readonly DependencyProperty CurvesProperty = Affecting<IReadOnlyList<TableCurve>>(nameof(Curves), []);

    /// <summary>Identifies <see cref="Points"/>.</summary>
    public static readonly DependencyProperty PointsProperty = Affecting<IReadOnlyList<TablePoint>>(nameof(Points), []);

    /// <summary>Identifies <see cref="Features"/>.</summary>
    public static readonly DependencyProperty FeaturesProperty = Affecting<IReadOnlyList<TableGraphFeature>>(nameof(Features), []);

    /// <summary>Identifies <see cref="XTicks"/>.</summary>
    public static readonly DependencyProperty XTicksProperty = Affecting<IReadOnlyList<GraphTick>>(nameof(XTicks), []);

    /// <summary>Identifies <see cref="YTicks"/>.</summary>
    public static readonly DependencyProperty YTicksProperty = Affecting<IReadOnlyList<GraphTick>>(nameof(YTicks), []);

    /// <summary>Identifies <see cref="Reading"/>.</summary>
    public static readonly DependencyProperty ReadingProperty = Affecting<TableGraphReading?>(nameof(Reading), null);

    /// <summary>Identifies <see cref="Ink"/>.</summary>
    public static readonly DependencyProperty InkProperty = Affecting<Brush>(nameof(Ink), Brushes.Black);

    /// <summary>Identifies <see cref="Second"/>.</summary>
    public static readonly DependencyProperty SecondProperty = Affecting<Brush>(nameof(Second), Brushes.Blue);

    /// <summary>Identifies <see cref="Faint"/>.</summary>
    public static readonly DependencyProperty FaintProperty = Affecting<Brush>(nameof(Faint), Brushes.Gray);

    /// <summary>Identifies <see cref="Mark"/>.</summary>
    public static readonly DependencyProperty MarkProperty = Affecting<Brush>(nameof(Mark), Brushes.OrangeRed);

    /// <summary>Identifies <see cref="Paper"/>.</summary>
    public static readonly DependencyProperty PaperProperty = Affecting<Brush>(nameof(Paper), Brushes.White);

    /// <summary>Identifies <see cref="FontFamily"/>.</summary>
    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(GraphDrawing), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Identifies <see cref="FontSize"/>.</summary>
    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(GraphDrawing), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    // A label keeps this far from the edge and from the label before it.
    private const double Gap = 4;

    /// <summary>Gets or sets the region of the plane shown; nothing is drawn without one.</summary>
    public GraphViewport? Viewport
    {
        get => (GraphViewport?)GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    /// <summary>Gets or sets the curves, sampled at this drawing's pixels.</summary>
    public IReadOnlyList<TableCurve> Curves
    {
        get => (IReadOnlyList<TableCurve>)GetValue(CurvesProperty);
        set => SetValue(CurvesProperty, value);
    }

    /// <summary>Gets or sets the rows of the table, as points.</summary>
    public IReadOnlyList<TablePoint> Points
    {
        get => (IReadOnlyList<TablePoint>)GetValue(PointsProperty);
        set => SetValue(PointsProperty, value);
    }

    /// <summary>Gets or sets the roots, extrema and intersections, drawn as rings.</summary>
    public IReadOnlyList<TableGraphFeature> Features
    {
        get => (IReadOnlyList<TableGraphFeature>)GetValue(FeaturesProperty);
        set => SetValue(FeaturesProperty, value);
    }

    /// <summary>Gets or sets the marks of the x axis.</summary>
    public IReadOnlyList<GraphTick> XTicks
    {
        get => (IReadOnlyList<GraphTick>)GetValue(XTicksProperty);
        set => SetValue(XTicksProperty, value);
    }

    /// <summary>Gets or sets the marks of the y axis.</summary>
    public IReadOnlyList<GraphTick> YTicks
    {
        get => (IReadOnlyList<GraphTick>)GetValue(YTicksProperty);
        set => SetValue(YTicksProperty, value);
    }

    /// <summary>Gets or sets what the graph reads at the pointer.</summary>
    public TableGraphReading? Reading
    {
        get => (TableGraphReading?)GetValue(ReadingProperty);
        set => SetValue(ReadingProperty, value);
    }

    /// <summary>Gets or sets the brush f(x) is drawn with.</summary>
    public Brush Ink
    {
        get => (Brush)GetValue(InkProperty);
        set => SetValue(InkProperty, value);
    }

    /// <summary>Gets or sets the brush g(x) is drawn with.</summary>
    public Brush Second
    {
        get => (Brush)GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    /// <summary>Gets or sets the brush of the grid, the axes, the asymptotes and the labels.</summary>
    public Brush Faint
    {
        get => (Brush)GetValue(FaintProperty);
        set => SetValue(FaintProperty, value);
    }

    /// <summary>Gets or sets the brush of what the graph names and of what it reads.</summary>
    public Brush Mark
    {
        get => (Brush)GetValue(MarkProperty);
        set => SetValue(MarkProperty, value);
    }

    /// <summary>Gets or sets the brush a ring is filled with: the display's own.</summary>
    public Brush Paper
    {
        get => (Brush)GetValue(PaperProperty);
        set => SetValue(PaperProperty, value);
    }

    /// <summary>Gets or sets the font of the labels.</summary>
    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>Gets or sets the size of the labels.</summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        // The whole of the drawing answers the pointer, not only its lines: the graph is read wherever it is pointed at.
        drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
        if (Viewport is not { } view || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        double Across(double x) => (x - view.Left) / view.Width * ActualWidth;
        double Down(double y) => (view.Top - y) / view.Height * ActualHeight;
        Point At(GraphPoint point) => new(Across(point.X), Down(point.Y));
        bool Inside(GraphPoint point) =>
            point.X >= view.Left && point.X <= view.Right && point.Y >= view.Bottom && point.Y <= view.Top;

        drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
        Grid(drawingContext, view, Across, Down);

        Pen dashed = Frozen(new Pen(Faint, 1) { DashStyle = DashStyles.Dash });
        foreach (TableCurve curve in Curves)
        {
            Pen pen = Frozen(new Pen(Brush(curve.Function), 1.8) { LineJoin = PenLineJoin.Round });
            foreach (ImmutableArray<GraphPoint> piece in curve.Trace.Pieces)
            {
                StreamGeometry line = new();
                using (StreamGeometryContext context = line.Open())
                {
                    context.BeginFigure(At(piece[0]), isFilled: false, isClosed: false);
                    context.PolyLineTo([.. piece.Skip(1).Select(At)], isStroked: true, isSmoothJoin: true);
                }

                line.Freeze();
                drawingContext.DrawGeometry(null, pen, line);
            }

            // A jump is seen in the curve itself; an asymptote is drawn, where the curve runs off the view towards it.
            foreach (GraphBreak asymptote in curve.Trace.Breaks.Where(found => found.Kind == GraphBreakKind.Asymptote))
            {
                drawingContext.DrawLine(dashed, new Point(Across(asymptote.X), 0), new Point(Across(asymptote.X), ActualHeight));
            }
        }

        foreach (TablePoint point in Points.Where(point => Inside(point.At)))
        {
            drawingContext.DrawEllipse(Brush(point.Function), null, At(point.At), 2.6, 2.6);
        }

        Pen ring = Frozen(new Pen(Mark, 1.6));
        foreach (TableGraphFeature feature in Features.Where(feature => Inside(feature.At)))
        {
            drawingContext.DrawEllipse(Paper, ring, At(feature.At), 4.2, 4.2);
        }

        if (Reading is { } reading)
        {
            double x = Across(reading.At);
            drawingContext.DrawLine(Frozen(new Pen(Mark, 1) { DashStyle = DashStyles.Dot }), new Point(x, 0), new Point(x, ActualHeight));
            foreach (TableGraphValue value in reading.Values.Where(value => value.Y is { } y && y >= view.Bottom && y <= view.Top))
            {
                drawingContext.DrawEllipse(Mark, null, new Point(x, Down(value.Y!.Value)), 3.4, 3.4);
            }
        }

        drawingContext.Pop();
    }

    private static DependencyProperty Affecting<T>(string name, T defaultValue) => DependencyProperty.Register(
        name, typeof(T), typeof(GraphDrawing), new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

    private static Pen Frozen(Pen pen)
    {
        pen.Freeze();
        return pen;
    }

    private Brush Brush(TableFunction function) => function == TableFunction.F ? Ink : Second;

    /// <summary>The grid at every mark, the axes where they are in view, and the labels along the bottom and the left.</summary>
    private void Grid(DrawingContext drawingContext, GraphViewport view, Func<double, double> across, Func<double, double> down)
    {
        Pen grid = Frozen(new Pen(Faint, 0.5) { DashStyle = DashStyles.Dot });
        Pen axis = Frozen(new Pen(Faint, 1));
        foreach (GraphTick tick in XTicks)
        {
            drawingContext.DrawLine(tick.At == 0 ? axis : grid, new Point(across(tick.At), 0), new Point(across(tick.At), ActualHeight));
        }

        foreach (GraphTick tick in YTicks)
        {
            drawingContext.DrawLine(tick.At == 0 ? axis : grid, new Point(0, down(tick.At)), new Point(ActualWidth, down(tick.At)));
        }

        // Where an axis is in the view, the other's labels are written along it; elsewhere along the edge.
        double below = view.Bottom <= 0 && view.Top >= 0 ? down(0) : ActualHeight;
        double beside = view.Left <= 0 && view.Right >= 0 ? across(0) : 0;
        double right = double.NegativeInfinity;
        foreach (GraphTick tick in XTicks)
        {
            // The 0 of x is where the y axis is: its label goes beside it, as the labels of y do, not across it.
            FormattedText text = Text(tick.Label);
            double at = tick.At == 0 ? across(0) + Gap + (text.Width / 2) : across(tick.At);
            double x = Math.Clamp(at - (text.Width / 2), Gap, Math.Max(Gap, ActualWidth - text.Width - Gap));
            double y = Math.Min(below + 2, ActualHeight - text.Height - 2);
            if (x > right + Gap)
            {
                drawingContext.DrawText(text, new Point(x, y));
                right = x + text.Width;
            }
        }

        // The 0 of y is where the x axis is, whose labels are written along it already.
        double top = double.PositiveInfinity;
        foreach (GraphTick tick in YTicks)
        {
            FormattedText text = Text(tick.Label);
            double x = Math.Clamp(beside + Gap, Gap, Math.Max(Gap, ActualWidth - text.Width - Gap));
            double y = Math.Clamp(down(tick.At) - (text.Height / 2), 2, Math.Max(2, ActualHeight - text.Height - 2));
            if (y + text.Height < top - 1 && tick.At != 0)
            {
                drawingContext.DrawText(text, new Point(x, y));
                top = y;
            }
        }
    }

    private FormattedText Text(string text) => new(
        text,
        CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight,
        new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
        FontSize,
        Faint,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
