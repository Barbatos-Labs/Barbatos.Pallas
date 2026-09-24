// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Globalization;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Circle screen of Math Box: θ1 and θ2 on the Unit Circle or the Half Circle with their trigonometric values, or the
/// hands of the Clock (manual pp. 157-161).
/// </summary>
/// <remarks>
/// The values are the engine's calculations (<see cref="CalculatorSession.CircleAngle"/>, <see cref="CalculatorSession.Clock"/>),
/// shown as results are, in LaTeX as well as text. The directions the screen draws the angles in are degrees from the
/// positive x axis, counterclockwise, whatever the angle unit: the drawing is the application's, not a result.
/// </remarks>
public sealed partial class CircleViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that calculates, in the Math Box application.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public CircleViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Angles = new ValueGridViewModel(session, 1, 2);

        // Neither angle is entered yet: an empty θ is not drawn (p. 160).
        Angles[0, 0].Text = string.Empty;
        Angles[0, 1].Text = string.Empty;
    }

    /// <summary>Gets the three types of the Circle application.</summary>
    public ImmutableArray<CircleScreen> Screens { get; } = [.. Enum.GetValues<CircleScreen>()];

    /// <summary>Gets θ1 and θ2 as typed, in the angle unit of the session.</summary>
    public ValueGridViewModel Angles { get; }

    /// <summary>Gets or sets the type; initially the Unit Circle.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsClock))]
    private CircleScreen _screen;

    /// <summary>Gets θ1 as it was drawn, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrawn))]
    [NotifyPropertyChangedFor(nameof(Lines))]
    [NotifyPropertyChangedFor(nameof(FirstDirection))]
    private CircleAngle? _first;

    /// <summary>Gets θ2 as it was drawn, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrawn))]
    [NotifyPropertyChangedFor(nameof(Lines))]
    [NotifyPropertyChangedFor(nameof(SecondDirection))]
    private CircleAngle? _second;

    /// <summary>Gets or sets which angle is selected: 0 for θ1, 1 for θ2 (▲ and ▼, p. 160).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Lines))]
    private int _selected;

    /// <summary>Gets or sets the hour of the Clock, 1 to 12; it starts at 12 (p. 161).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Clock))]
    [NotifyPropertyChangedFor(nameof(ClockText))]
    [NotifyPropertyChangedFor(nameof(Lines))]
    private int _hour = 12;

    /// <summary>Gets the localization key of the error of the last Execute, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets whether the Clock is shown rather than a circle.</summary>
    public bool IsClock => Screen == CircleScreen.Clock;

    /// <summary>Gets whether an angle is drawn on the circle.</summary>
    public bool IsDrawn => First is not null || Second is not null;

    /// <summary>Gets the angles between the hands at <see cref="Hour"/>.</summary>
    public ClockAngles Clock => _session.Clock(Hour);

    /// <summary>Gets the time the Clock shows, such as <c>3:00</c>.</summary>
    public string ClockText => Hour.ToString(CultureInfo.InvariantCulture) + ":00";

    /// <summary>Gets the direction θ1 is drawn in, in degrees, or <see langword="null"/>.</summary>
    public double? FirstDirection => Direction(First);

    /// <summary>Gets the direction θ2 is drawn in, in degrees, or <see langword="null"/>.</summary>
    public double? SecondDirection => Direction(Second);

    /// <summary>Gets what the screen writes beside the drawing: the selected angle and its sin, cos and tan, or θ1 and θ2 of the Clock.</summary>
    public ImmutableArray<CircleLine> Lines
    {
        get
        {
            if (IsClock)
            {
                ClockAngles clock = Clock;
                return [Line("θ1", clock.Smaller), Line("θ2", clock.Larger)];
            }

            // Execute, ▲ and ▼ only ever select an angle that is drawn.
            CircleAngle? angle = Selected == 1 ? Second : First;
            if (angle is null)
            {
                return [];
            }

            string name = Selected == 1 ? "θ2" : "θ1";
            return [Line("sin" + name, angle.Sine!), Line("cos" + name, angle.Cosine!), Line("tan" + name, angle.Tangent!), AngleLine(name, angle)];
        }
    }

    /// <summary>Draws the angles that were typed (p. 159).</summary>
    [RelayCommand]
    public void Execute()
    {
        ErrorKey = null;
        First = null;
        Second = null;
        if (IsClock)
        {
            return;
        }

        CircleKind kind = Screen == CircleScreen.HalfCircle ? CircleKind.HalfCircle : CircleKind.UnitCircle;
        CircleAngle? first = Draw(Angles[0, 0], kind);
        CircleAngle? second = Draw(Angles[0, 1], kind);
        if (ErrorKey is null)
        {
            (First, Second) = (first, second);
            Selected = first is null && second is not null ? 1 : 0;
        }
    }

    /// <summary>▲: θ1, or on the Clock an hour later (p. 161).</summary>
    [RelayCommand]
    public void Up()
    {
        if (IsClock)
        {
            Hour = (Hour % 12) + 1;
        }
        else if (First is not null)
        {
            Selected = 0;
        }
    }

    /// <summary>▼: θ2, or on the Clock an hour earlier.</summary>
    [RelayCommand]
    public void Down()
    {
        if (IsClock)
        {
            Hour = ((Hour + 10) % 12) + 1;
        }
        else if (Second is not null)
        {
            Selected = 1;
        }
    }

    /// <summary>Takes the settings back into account: the same angles, drawn in the settings in effect.</summary>
    public void Refresh()
    {
        if (IsDrawn)
        {
            Execute();
        }

        OnPropertyChanged(nameof(Clock));
        OnPropertyChanged(nameof(Lines));
    }

    partial void OnScreenChanged(CircleScreen value)
    {
        First = null;
        Second = null;
        ErrorKey = null;
    }

    private CircleAngle? Draw(ValueCellViewModel cell, CircleKind kind)
    {
        if (string.IsNullOrWhiteSpace(cell.Text))
        {
            return null;
        }

        if (cell.HasError)
        {
            ErrorKey ??= cell.ErrorKey;
            return null;
        }

        CircleAngle angle = _session.CircleAngle(kind, cell.Value);
        if (!angle.Succeeded)
        {
            ErrorKey ??= "error." + angle.Error!.Value.Kind;
            return null;
        }

        return angle;
    }

    private double? Direction(CircleAngle? angle) =>
        angle is null ? null : Trigonometry.ConvertAngle(angle.Angle.ToDouble(), _session.Settings.AngleUnit, AngleUnit.Degree);

    private CircleLine AngleLine(string name, CircleAngle angle)
    {
        FormattedResult? text = PallasEngine.Format(angle.Angle, _session.Settings, _session.Profile);
        return new CircleLine(name, text?.Text ?? string.Empty, text?.Latex ?? string.Empty, null);
    }

    private static CircleLine Line(string name, Calculation calculation) =>
        calculation.Succeeded
            ? new CircleLine(name, calculation.Display.Text, calculation.Display.Latex, null)
            : new CircleLine(name, string.Empty, string.Empty, "error." + calculation.Error!.Value.Kind);
}

/// <summary>One line beside the circle: a name, and its value as text and as LaTeX, or its error.</summary>
/// <param name="Name">Such as <c>sinθ1</c> or <c>θ2</c>.</param>
/// <param name="Text">The value as text.</param>
/// <param name="Latex">The value as LaTeX, drawn on the screen.</param>
/// <param name="ErrorKey">The localization key of the value's error - tan 90° - or <see langword="null"/>.</param>
public sealed record CircleLine(string Name, string Text, string Latex, string? ErrorKey);
