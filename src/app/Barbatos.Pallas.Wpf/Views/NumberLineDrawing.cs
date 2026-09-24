// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Number Line as the calculator draws it (manual p. 155): each expression on a row of its own, top to bottom, over
/// the x axis with its sixteen ticks and its ends and center labelled.
/// </summary>
/// <remarks>
/// Where a bound falls is the view model's (<see cref="NumberLineBar"/>); this only turns a fraction of the view into
/// a pixel. An arrow is an expression that runs on past the view, a filled circle a bound in the set and an open one
/// a bound outside it. The colours and the type are the theme's, set by its style.
/// </remarks>
public sealed class NumberLineDrawing : FrameworkElement
{
    /// <summary>Identifies <see cref="Bars"/>.</summary>
    public static readonly DependencyProperty BarsProperty = DependencyProperty.Register(
        nameof(Bars), typeof(IReadOnlyList<NumberLineBar>), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(Array.Empty<NumberLineBar>(), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Selected"/>.</summary>
    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
        nameof(Selected), typeof(int), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Labels"/>.</summary>
    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels), typeof(IReadOnlyList<string>), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(Array.Empty<string>(), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Ink"/>.</summary>
    public static readonly DependencyProperty InkProperty = DependencyProperty.Register(
        nameof(Ink), typeof(Brush), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Faint"/>.</summary>
    public static readonly DependencyProperty FaintProperty = DependencyProperty.Register(
        nameof(Faint), typeof(Brush), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Paper"/>.</summary>
    public static readonly DependencyProperty PaperProperty = DependencyProperty.Register(
        nameof(Paper), typeof(Brush), typeof(NumberLineDrawing),
        new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="FontFamily"/>.</summary>
    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(
        typeof(NumberLineDrawing), new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Identifies <see cref="FontSize"/>.</summary>
    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(
        typeof(NumberLineDrawing), new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits));

    // The calculator has three axes, so an expression keeps the height of a third of the rows whatever is drawn.
    private const int Rows = 3;
    private const double Gutter = 14;
    private const double Tick = 3;

    /// <summary>Gets or sets the expressions, top to bottom.</summary>
    public IReadOnlyList<NumberLineBar> Bars
    {
        get => (IReadOnlyList<NumberLineBar>)GetValue(BarsProperty);
        set => SetValue(BarsProperty, value);
    }

    /// <summary>Gets or sets which expression is drawn bold.</summary>
    public int Selected
    {
        get => (int)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    /// <summary>Gets or sets the labels of the x axis: its left end, its center and its right end.</summary>
    public IReadOnlyList<string> Labels
    {
        get => (IReadOnlyList<string>)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    /// <summary>Gets or sets the brush the expressions are drawn with.</summary>
    public Brush Ink
    {
        get => (Brush)GetValue(InkProperty);
        set => SetValue(InkProperty, value);
    }

    /// <summary>Gets or sets the brush the x axis and its labels are drawn with.</summary>
    public Brush Faint
    {
        get => (Brush)GetValue(FaintProperty);
        set => SetValue(FaintProperty, value);
    }

    /// <summary>Gets or sets the brush an open circle is filled with: the display's own.</summary>
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
        double left = Gutter;
        double right = Math.Max(left + 1, ActualWidth - Gutter);
        double labelHeight = FontSize * 1.4;
        double axis = Math.Max(Tick * 2, ActualHeight - labelHeight - Tick);

        Pen faint = new(Faint, 1);
        drawingContext.DrawLine(faint, new Point(left - Gutter + 4, axis), new Point(right + Gutter - 4, axis));
        Arrow(drawingContext, Faint, new Point(left - Gutter + 2, axis), -1, 4);
        Arrow(drawingContext, Faint, new Point(right + Gutter - 2, axis), 1, 4);
        for (int tick = 0; tick <= 16; tick++)
        {
            double x = left + ((right - left) * tick / 16);
            double length = tick % 8 == 0 ? Tick * 2 : Tick;
            drawingContext.DrawLine(faint, new Point(x, axis - length), new Point(x, axis + length));
        }

        IReadOnlyList<string> labels = Labels;
        for (int index = 0; index < labels.Count && index < 3; index++)
        {
            FormattedText text = Text(labels[index], Faint);
            double at = left + ((right - left) * index / 2);
            double x = Math.Clamp(at - (text.Width / 2), 0, Math.Max(0, ActualWidth - text.Width));
            drawingContext.DrawText(text, new Point(x, axis + Tick + 1));
        }

        double row = (axis - Tick) / Rows;
        IReadOnlyList<NumberLineBar> bars = Bars;
        for (int index = 0; index < bars.Count && index < Rows; index++)
        {
            Bar(drawingContext, bars[index], (row * index) + (row / 2), left, right, index == Selected);
        }
    }

    private void Bar(DrawingContext drawingContext, NumberLineBar bar, double y, double left, double right, bool selected)
    {
        double width = selected ? 3 : 1.4;
        double radius = selected ? 4.5 : 3.5;
        Pen pen = new(Ink, width);
        double Across(double fraction) => left + ((right - left) * fraction);

        // Past the view the line runs to the edge; an unbounded side ends in an arrow there.
        double from = bar.Lower is { } lower ? Math.Clamp(Across(lower), left - 2, right + 2) : left - 2;
        double to = bar.Upper is { } upper ? Math.Clamp(Across(upper), left - 2, right + 2) : right + 2;
        if (to > from)
        {
            drawingContext.DrawLine(pen, new Point(from, y), new Point(to, y));
        }

        if (bar.Lower is null)
        {
            Arrow(drawingContext, Ink, new Point(left - 6, y), -1, radius);
        }

        if (bar.Upper is null)
        {
            Arrow(drawingContext, Ink, new Point(right + 6, y), 1, radius);
        }

        Bound(drawingContext, bar.Lower, bar.LowerIncluded, y, radius, left, right);
        Bound(drawingContext, bar.Upper, bar.UpperIncluded, y, radius, left, right);
    }

    private void Bound(DrawingContext drawingContext, double? fraction, bool included, double y, double radius, double left, double right)
    {
        if (fraction is not { } at || at < 0 || at > 1)
        {
            return;
        }

        Point center = new(left + ((right - left) * at), y);
        drawingContext.DrawEllipse(included ? Ink : Paper, new Pen(Ink, 1.4), center, radius, radius);
    }

    private static void Arrow(DrawingContext drawingContext, Brush brush, Point tip, int direction, double size)
    {
        StreamGeometry head = new();
        using (StreamGeometryContext context = head.Open())
        {
            context.BeginFigure(tip, isFilled: true, isClosed: true);
            context.LineTo(new Point(tip.X - (direction * size * 1.6), tip.Y - size), isStroked: true, isSmoothJoin: false);
            context.LineTo(new Point(tip.X - (direction * size * 1.6), tip.Y + size), isStroked: true, isSmoothJoin: false);
        }

        head.Freeze();
        drawingContext.DrawGeometry(brush, null, head);
    }

    private FormattedText Text(string text, Brush brush) => new(
        text,
        CultureInfo.InvariantCulture,
        FlowDirection.LeftToRight,
        new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
        FontSize,
        brush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);
}
