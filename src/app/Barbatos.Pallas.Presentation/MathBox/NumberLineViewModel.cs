// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Number Line screen of Math Box: up to three expressions on axes A, B and C, and the View-Window they are drawn in
/// (manual pp. 153-157).
/// </summary>
/// <remarks>
/// The rules of an expression and of a view are the engine's (<see cref="NumberLine"/>). What this adds is what the
/// calculator's screen does around them: an axis left empty is not drawn, a view set by hand stays until View-Reset, and
/// a change of the angle unit clears every expression (p. 155).
/// </remarks>
public sealed partial class NumberLineViewModel : ObservableObject
{
    private readonly CalculatorSession _session;
    private AngleUnit _unit;
    private NumberLineView? _window;
    private ImmutableArray<string> _written = [];

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session whose values the bounds are typed in.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public NumberLineViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _unit = session.Settings.AngleUnit;
        Entries = [new NumberLineEntryViewModel(session, "A"), new NumberLineEntryViewModel(session, "B"), new NumberLineEntryViewModel(session, "C")];
        Window = new ValueGridViewModel(session, 1, 2);
        Show(View);
    }

    /// <summary>Gets the three expressions, on axes A, B and C.</summary>
    public ImmutableArray<NumberLineEntryViewModel> Entries { get; }

    /// <summary>Gets the View-Window as typed: Scale, then Center.</summary>
    public ValueGridViewModel Window { get; }

    /// <summary>Gets the expressions that were drawn, top to bottom, or none before Execute.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDrawn))]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    [NotifyPropertyChangedFor(nameof(Bars))]
    private ImmutableArray<NumberLineAxis> _axes = [];

    /// <summary>Gets the view the axes are drawn in.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Bars))]
    [NotifyPropertyChangedFor(nameof(ScaleLabels))]
    private NumberLineView _view = new(0m, 1m);

    /// <summary>Gets or sets which drawn expression is shown under the axis (▲ and ▼, p. 155).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedText))]
    private int _selected;

    /// <summary>Gets the localization key of the error of the last Execute or View-Window, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets whether the expressions are drawn.</summary>
    public bool IsDrawn => !Axes.IsEmpty;

    /// <summary>Gets the selected expression as the screen writes it under the axis, such as <c>A:x≤-1.5</c>.</summary>
    /// <remarks>Its bounds are written as they were typed when it was drawn, as the manual shows <c>B:x&gt;-1.0</c> (p. 154).</remarks>
    public string SelectedText => _written.IsEmpty ? string.Empty : _written[Selected];

    /// <summary>Gets every drawn expression as where its bounds fall across the view, top to bottom.</summary>
    public ImmutableArray<NumberLineBar> Bars => [.. Axes.Select(axis => new NumberLineBar(Across(axis.Lower), axis.LowerIncluded, Across(axis.Upper), axis.UpperIncluded))];

    /// <summary>Gets what the x axis is labelled with: its left end, its center and its right end (p. 155).</summary>
    /// <remarks>They are decimals whatever the Input/Output setting, as the manual labels the axis -2.8, -1.2 and 0.4.</remarks>
    public ImmutableArray<string> ScaleLabels => [Decimal(View.Minimum), Decimal(View.Center), Decimal(View.Maximum)];

    /// <summary>Draws the expressions that are registered (p. 154), in a view fitted to them unless one was set by hand.</summary>
    [RelayCommand]
    public void Execute()
    {
        ErrorKey = null;
        List<NumberLineAxis> axes = [];
        List<string> written = [];
        foreach (NumberLineEntryViewModel entry in Entries.Where(entry => entry.Form is not null))
        {
            if (entry.Bounds.ErrorKey is { } typed)
            {
                ErrorKey = typed;
                return;
            }

            NumberLineAxis axis = entry.Define();
            if (!axis.Succeeded)
            {
                ErrorKey = "error." + axis.Error!.Value.Kind;
                return;
            }

            axes.Add(axis);
            written.Add(entry.Label + ":" + entry.Written());
        }

        _unit = _session.Settings.AngleUnit;
        _written = [.. written];
        Selected = 0;
        Axes = [.. axes];
        Show(_window ?? NumberLine.Fit(Axes));
    }

    /// <summary>Sets the View-Window from what was typed (p. 156).</summary>
    [RelayCommand]
    public void ApplyWindow()
    {
        if (Window.ErrorKey is { } typed)
        {
            ErrorKey = typed;
            return;
        }

        NumberLineView view = NumberLine.View(Window[0, 1].Value, Window[0, 0].Value);
        if (!view.Succeeded)
        {
            ErrorKey = "error." + view.Error!.Value.Kind;
            return;
        }

        ErrorKey = null;
        _window = view;
        View = view;
    }

    /// <summary>View-Reset: back to the view fitted to the expressions (p. 157).</summary>
    [RelayCommand]
    public void ResetWindow()
    {
        _window = null;
        Show(NumberLine.Fit(Axes));
    }

    /// <summary>▲: the expression above the selected one.</summary>
    [RelayCommand]
    public void Previous() => Selected = Axes.IsEmpty ? 0 : (Selected + Axes.Length - 1) % Axes.Length;

    /// <summary>▼: the expression below the selected one.</summary>
    [RelayCommand]
    public void Next() => Selected = Axes.IsEmpty ? 0 : (Selected + 1) % Axes.Length;

    /// <summary>Clears every expression and the drawing.</summary>
    [RelayCommand]
    public void Clear()
    {
        foreach (NumberLineEntryViewModel entry in Entries)
        {
            entry.Clear();
        }

        _written = [];
        Axes = [];
        _window = null;
        Show(new NumberLineView(0m, 1m));
        ErrorKey = null;
    }

    /// <summary>Takes the settings back into account; a new angle unit clears every expression, as on the calculator (p. 155).</summary>
    public void Refresh()
    {
        if (_session.Settings.AngleUnit != _unit)
        {
            _unit = _session.Settings.AngleUnit;
            Clear();
        }

        OnPropertyChanged(nameof(ScaleLabels));
    }

    /// <summary>Returns how the calculator's list writes a form.</summary>
    /// <param name="form">The form.</param>
    /// <returns>Such as <c>x≤a</c> or <c>a&lt;x≤b</c>.</returns>
    public static string FormText(NumberLineForm form) => Write(form, "a", "b");

    /// <summary>Writes an expression as the calculator's list does.</summary>
    /// <param name="form">The form.</param>
    /// <param name="a">a, as it is to be written.</param>
    /// <param name="b">b, as it is to be written; a form with one bound does not use it.</param>
    /// <returns>Such as <c>x≤-1.5</c> or <c>-2.0&lt;x≤-0.5</c>.</returns>
    public static string Write(NumberLineForm form, string a, string b) => form switch
    {
        NumberLineForm.Less => "x<" + a,
        NumberLineForm.LessOrEqual => "x≤" + a,
        NumberLineForm.Equal => "x=" + a,
        NumberLineForm.Greater => "x>" + a,
        NumberLineForm.GreaterOrEqual => "x≥" + a,
        NumberLineForm.Between => a + "<x<" + b,
        NumberLineForm.FromIncluded => a + "≤x<" + b,
        NumberLineForm.ToIncluded => a + "<x≤" + b,
        _ => a + "≤x≤" + b,
    };

    // The View-Window screen shows the view in effect, as p. 156 shows Scale 0.2 and Center -1.2 after Execute: in
    // decimals, which is also text the cells read back.
    private void Show(NumberLineView view)
    {
        View = view;
        Window[0, 0].Text = Decimal(view.Scale);
        Window[0, 1].Text = Decimal(view.Center);
    }

    private string Decimal(decimal value) =>
        PallasEngine.Format(Value.FromDecimal(value), SimulationViewModel.DecimalOutput(_session.Settings), _session.Profile)?.Text ?? string.Empty;

    // A fraction of the view for the screen to draw at: a pixel, not a result, so double is what it needs.
    private double? Across(Value? bound) =>
        bound is { } value ? (value.ToDouble() - (double)View.Minimum) / (double)(View.Maximum - View.Minimum) : null;
}

/// <summary>One expression of the Number Line as it is drawn: where its bounds fall, from 0 at the left end of the view to 1 at the right.</summary>
/// <param name="Lower">Where the lower bound falls, or <see langword="null"/> when the expression runs on to the left.</param>
/// <param name="LowerIncluded">Whether the lower bound is part of the set: a filled circle rather than an open one.</param>
/// <param name="Upper">Where the upper bound falls, or <see langword="null"/> when the expression runs on to the right.</param>
/// <param name="UpperIncluded">Whether the upper bound is part of the set.</param>
/// <remarks>A bound outside the view falls below 0 or above 1: the line is drawn to the edge, and the circle is not.</remarks>
public sealed record NumberLineBar(double? Lower, bool LowerIncluded, double? Upper, bool UpperIncluded);

/// <summary>One of the forms of the calculator's list, and how the list writes it.</summary>
/// <param name="Form">The form.</param>
/// <param name="Text">Such as <c>x≤a</c>.</param>
public sealed record NumberLineFormChoice(NumberLineForm Form, string Text);

/// <summary>One of the three expressions of the Number Line screen: its form, and a and b as typed.</summary>
public sealed partial class NumberLineEntryViewModel : ObservableObject
{
    /// <summary>Creates an empty expression.</summary>
    /// <param name="session">The session whose values a and b are typed in.</param>
    /// <param name="label">The axis it is drawn on: A, B or C.</param>
    public NumberLineEntryViewModel(CalculatorSession session, string label)
    {
        Label = label;
        Bounds = new ValueGridViewModel(session, 1, 2);
    }

    /// <summary>Gets the axis the expression is drawn on: A, B or C.</summary>
    public string Label { get; }

    /// <summary>Gets a and b, as typed.</summary>
    public ValueGridViewModel Bounds { get; }

    /// <summary>Gets the nine forms an expression can take, in the order of the calculator's list (p. 154).</summary>
    public ImmutableArray<NumberLineFormChoice> Forms { get; } =
        [.. Enum.GetValues<NumberLineForm>().Select(form => new NumberLineFormChoice(form, NumberLineViewModel.FormText(form)))];

    /// <summary>Gets or sets the form of the expression, or <see langword="null"/> when the axis is left empty.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpperBound))]
    private NumberLineForm? _form;

    /// <summary>Gets whether the form has a second bound, b.</summary>
    public bool HasUpperBound => Form >= NumberLineForm.Between;

    /// <summary>Returns the expression as the engine registers it.</summary>
    /// <returns>The axis, or its Range ERROR.</returns>
    /// <exception cref="InvalidOperationException">No form is chosen.</exception>
    public NumberLineAxis Define()
    {
        NumberLineForm form = Form ?? throw new InvalidOperationException("An axis left empty has no expression.");
        return NumberLine.Define(form, Bounds[0, 0].Value, HasUpperBound ? Bounds[0, 1].Value : null);
    }

    /// <summary>Writes the expression with a and b as they are typed, or nothing when no form is chosen.</summary>
    /// <returns>Such as <c>x&gt;-1.0</c>.</returns>
    public string Written() => Form is { } form ? NumberLineViewModel.Write(form, Bounds[0, 0].Text.Trim(), Bounds[0, 1].Text.Trim()) : string.Empty;

    /// <summary>Deletes the expression (DEL, p. 156).</summary>
    [RelayCommand]
    public void Clear()
    {
        Form = null;
        Bounds.Clear();
    }
}
