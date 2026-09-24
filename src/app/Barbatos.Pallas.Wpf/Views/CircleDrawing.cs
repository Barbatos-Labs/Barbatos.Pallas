// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Media;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Circle as the calculator draws it (manual pp. 157-161): the unit circle or its upper half with θ1 and θ2 on it,
/// the selected one bold, or the face of a clock with its two hands.
/// </summary>
/// <remarks>
/// The directions are degrees from the positive x axis, counterclockwise, whatever the angle unit
/// (<see cref="CircleViewModel.FirstDirection"/>): the drawing places lines, it calculates nothing the screen shows.
/// </remarks>
public sealed class CircleDrawing : FrameworkElement
{
    /// <summary>Identifies <see cref="Screen"/>.</summary>
    public static readonly DependencyProperty ScreenProperty = DependencyProperty.Register(
        nameof(Screen), typeof(CircleScreen), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(CircleScreen.UnitCircle, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="First"/>.</summary>
    public static readonly DependencyProperty FirstProperty = DependencyProperty.Register(
        nameof(First), typeof(double?), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Second"/>.</summary>
    public static readonly DependencyProperty SecondProperty = DependencyProperty.Register(
        nameof(Second), typeof(double?), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Selected"/>.</summary>
    public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register(
        nameof(Selected), typeof(int), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Hour"/>.</summary>
    public static readonly DependencyProperty HourProperty = DependencyProperty.Register(
        nameof(Hour), typeof(int), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(12, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Ink"/>.</summary>
    public static readonly DependencyProperty InkProperty = DependencyProperty.Register(
        nameof(Ink), typeof(Brush), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies <see cref="Faint"/>.</summary>
    public static readonly DependencyProperty FaintProperty = DependencyProperty.Register(
        nameof(Faint), typeof(Brush), typeof(CircleDrawing),
        new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

    private const double Gutter = 10;

    /// <summary>Gets or sets what is drawn: the unit circle, the half circle or the clock.</summary>
    public CircleScreen Screen
    {
        get => (CircleScreen)GetValue(ScreenProperty);
        set => SetValue(ScreenProperty, value);
    }

    /// <summary>Gets or sets the direction of θ1 in degrees, or <see langword="null"/> when it is not drawn.</summary>
    public double? First
    {
        get => (double?)GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    /// <summary>Gets or sets the direction of θ2 in degrees, or <see langword="null"/> when it is not drawn.</summary>
    public double? Second
    {
        get => (double?)GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    /// <summary>Gets or sets which angle is drawn bold: 0 for θ1, 1 for θ2.</summary>
    public int Selected
    {
        get => (int)GetValue(SelectedProperty);
        set => SetValue(SelectedProperty, value);
    }

    /// <summary>Gets or sets the hour the clock shows, 1 to 12.</summary>
    public int Hour
    {
        get => (int)GetValue(HourProperty);
        set => SetValue(HourProperty, value);
    }

    /// <summary>Gets or sets the brush of the circle and the angles.</summary>
    public Brush Ink
    {
        get => (Brush)GetValue(InkProperty);
        set => SetValue(InkProperty, value);
    }

    /// <summary>Gets or sets the brush of the axes and the ticks.</summary>
    public Brush Faint
    {
        get => (Brush)GetValue(FaintProperty);
        set => SetValue(FaintProperty, value);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        switch (Screen)
        {
            case CircleScreen.Clock:
                Clock(drawingContext);
                break;
            case CircleScreen.HalfCircle:
                Half(drawingContext);
                break;
            default:
                Unit(drawingContext);
                break;
        }
    }

    private void Unit(DrawingContext drawingContext)
    {
        double radius = Math.Max(1, (Math.Min(ActualWidth, ActualHeight) / 2) - Gutter);
        Point center = new(ActualWidth / 2, ActualHeight / 2);
        Axes(drawingContext, center, radius, radius);
        drawingContext.DrawEllipse(null, new Pen(Ink, 1.4), center, radius, radius);
        Angles(drawingContext, center, radius);
    }

    private void Half(DrawingContext drawingContext)
    {
        double radius = Math.Max(1, Math.Min((ActualWidth / 2) - Gutter, ActualHeight - (Gutter * 2)));
        Point center = new(ActualWidth / 2, (ActualHeight + radius) / 2);
        Axes(drawingContext, center, radius, 0);

        StreamGeometry arc = new();
        using (StreamGeometryContext context = arc.Open())
        {
            context.BeginFigure(new Point(center.X + radius, center.Y), isFilled: false, isClosed: false);
            context.ArcTo(new Point(center.X - radius, center.Y), new Size(radius, radius), 0, isLargeArc: false, SweepDirection.Counterclockwise, isStroked: true, isSmoothJoin: false);
        }

        arc.Freeze();
        drawingContext.DrawGeometry(null, new Pen(Ink, 1.4), arc);

        // The half circle is a protractor: a tick every 30°.
        Pen faint = new(Faint, 1);
        for (int degrees = 30; degrees < 180; degrees += 30)
        {
            drawingContext.DrawLine(faint, On(center, radius * 0.92, degrees), On(center, radius, degrees));
        }

        Angles(drawingContext, center, radius);
    }

    private void Clock(DrawingContext drawingContext)
    {
        double radius = Math.Max(1, (Math.Min(ActualWidth, ActualHeight) / 2) - Gutter);
        Point center = new(ActualWidth / 2, ActualHeight / 2);
        drawingContext.DrawEllipse(null, new Pen(Ink, 1.6), center, radius, radius);

        Pen faint = new(Faint, 1);
        Pen hour = new(Ink, 1.6);
        for (int mark = 0; mark < 12; mark++)
        {
            double degrees = 90 - (mark * 30);
            drawingContext.DrawLine(mark % 3 == 0 ? hour : faint, On(center, radius * (mark % 3 == 0 ? 0.82 : 0.88), degrees), On(center, radius, degrees));
        }

        // The minute hand stays at 12; the hour hand turns an hour, 30°, a step (p. 161).
        drawingContext.DrawLine(new Pen(Ink, 2), center, On(center, radius * 0.78, 90));
        drawingContext.DrawLine(new Pen(Ink, 3.5), center, On(center, radius * 0.5, 90 - (Hour % 12 * 30)));
        drawingContext.DrawEllipse(Ink, null, center, 3, 3);
    }

    private void Axes(DrawingContext drawingContext, Point center, double radius, double below)
    {
        Pen dashed = new(Faint, 1) { DashStyle = DashStyles.Dash };
        double reach = radius + (Gutter * 0.7);
        drawingContext.DrawLine(dashed, new Point(center.X - reach, center.Y), new Point(center.X + reach, center.Y));
        drawingContext.DrawLine(dashed, new Point(center.X, center.Y + Math.Min(reach, below + (Gutter * 0.7))), new Point(center.X, center.Y - reach));
        Arrow(drawingContext, new Point(center.X + reach, center.Y), 0);
        Arrow(drawingContext, new Point(center.X, center.Y - reach), 90);
    }

    private void Angles(DrawingContext drawingContext, Point center, double radius)
    {
        // The view model only ever selects an angle that is drawn.
        int chosen = Selected == 1 ? 1 : 0;
        double?[] directions = [First, Second];
        for (int index = 0; index < directions.Length; index++)
        {
            if (directions[index] is { } degrees)
            {
                drawingContext.DrawLine(new Pen(Ink, index == chosen ? 3 : 1.2), center, On(center, radius, degrees));
            }
        }

        if (directions[chosen] is { } angle)
        {
            Sweep(drawingContext, center, radius * 0.22, angle);
        }
    }

    // The arc from the x axis to the angle, the way it turns: counterclockwise for a positive angle.
    private void Sweep(DrawingContext drawingContext, Point center, double radius, double degrees)
    {
        double turn = degrees % 360;
        if (Math.Abs(turn) < 0.5)
        {
            return;
        }

        StreamGeometry arc = new();
        using (StreamGeometryContext context = arc.Open())
        {
            context.BeginFigure(On(center, radius, 0), isFilled: false, isClosed: false);
            context.ArcTo(
                On(center, radius, turn),
                new Size(radius, radius),
                0,
                isLargeArc: Math.Abs(turn) > 180,
                turn > 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                isStroked: true,
                isSmoothJoin: false);
        }

        arc.Freeze();
        drawingContext.DrawGeometry(null, new Pen(Ink, 1), arc);
    }

    private void Arrow(DrawingContext drawingContext, Point tip, double degrees)
    {
        StreamGeometry head = new();
        using (StreamGeometryContext context = head.Open())
        {
            context.BeginFigure(tip, isFilled: true, isClosed: true);
            context.LineTo(On(tip, 6, degrees + 150), isStroked: true, isSmoothJoin: false);
            context.LineTo(On(tip, 6, degrees - 150), isStroked: true, isSmoothJoin: false);
        }

        head.Freeze();
        drawingContext.DrawGeometry(Faint, null, head);
    }

    // A point at a distance from a center, in a direction in degrees; the screen's y grows downwards.
    private static Point On(Point center, double distance, double degrees)
    {
        double radians = double.DegreesToRadians(degrees);
        return new Point(center.X + (distance * Math.Cos(radians)), center.Y - (distance * Math.Sin(radians)));
    }
}
