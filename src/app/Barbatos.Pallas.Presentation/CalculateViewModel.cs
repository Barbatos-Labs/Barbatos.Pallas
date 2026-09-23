// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Calculate screen: the line being typed, what the last calculation came to, and what was calculated before.
/// </summary>
/// <remarks>
/// <para>
/// The engine answers with a value, a display and, where something went wrong, the span of the input it belongs to;
/// this puts the cursor there, which is what the calculator does with an error (manual p. 162). The error itself
/// stays a <see cref="CalcErrorKind"/> - the core says nothing in any language - and the screen looks its text up
/// under <see cref="ErrorKey"/>.
/// </para>
/// <para>
/// The history is the session's own: every calculation that was executed, in order, read back onto the line through
/// the parser rather than as characters, so a fraction comes back as a fraction.
/// </para>
/// </remarks>
public sealed partial class CalculateViewModel : ObservableObject
{
    private readonly CalculatorSession _session;
    private int _recalled = -1;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that calculates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public CalculateViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Input = new MathInputViewModel();
        Input.Requested += OnRequested;
    }

    /// <summary>Gets the line the user types on.</summary>
    public MathInputViewModel Input { get; }

    /// <summary>Gets the localization key of the name of the application the line belongs to.</summary>
    /// <remarks>Calculate and Complex are the same screen, and the title is how the user tells them apart.</remarks>
    public string TitleKey => CalculatorApps.Of(_session.App).NameKey;

    /// <summary>Tells the screen that the application changed, and with it its title.</summary>
    public void Refresh() => OnPropertyChanged(nameof(TitleKey));

    /// <summary>Gets the last calculation, or <see langword="null"/> before the first one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorKey))]
    [NotifyPropertyChangedFor(nameof(History))]
    private Calculation? _calculation;

    /// <summary>Gets the result as the screen shows it, or <see langword="null"/>.</summary>
    /// <remarks>Not the calculation's own display: a FORMAT conversion replaces it, and the result stays converted.</remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    private FormattedResult? _display;

    /// <summary>Gets whether there is a result to show.</summary>
    /// <remarks>
    /// A result always has mathematics to draw as well as text, and an error has neither (measured over the
    /// manual's own examples, 23 Sep 2026), so this one flag governs the whole result line.
    /// </remarks>
    public bool HasResult => Display is not null && Calculation is { Succeeded: true };

    /// <summary>Gets whether the last calculation failed.</summary>
    public bool HasError => Calculation is { Succeeded: false };

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    public string? ErrorKey => Calculation?.Error is { } error ? "error." + error.Kind : null;

    /// <summary>Gets what was calculated before, newest first.</summary>
    public ImmutableArray<Calculation> History => [.. _session.History.Reverse()];

    /// <summary>Calculates what is on the line.</summary>
    /// <remarks>
    /// An empty line calculates nothing. A line that fails leaves what was typed where it is, with the cursor at the
    /// place the engine could not read, so the next keystroke corrects it.
    /// </remarks>
    [RelayCommand]
    public void Execute()
    {
        string text = Input.Linear;
        if (text.Length == 0)
        {
            return;
        }

        Calculation calculation = _session.Calculate(text);
        Calculation = calculation;
        Display = calculation.Display;
        _recalled = -1;

        if (calculation.Error is { } error)
        {
            Input.MoveTo(error.Span.Start);
        }
    }

    /// <summary>Shows the result another way (the FORMAT menu, manual pp. 42-50).</summary>
    /// <param name="target">The conversion.</param>
    /// <returns><see langword="true"/> when the result can be shown that way.</returns>
    public bool Format(FormatTarget target)
    {
        if (Calculation is not { Succeeded: true } calculation || _session.Format(calculation, target) is not { } formatted)
        {
            return false;
        }

        Display = formatted;
        return true;
    }

    /// <summary>Turns the result between its exact form and its decimal (the S⇔D key, manual p. 42).</summary>
    /// <returns><see langword="true"/> when the result has two forms to turn between.</returns>
    public bool ToggleDecimal()
    {
        if (Calculation is not { Succeeded: true } calculation)
        {
            return false;
        }

        FormattedResult? standard = _session.Format(calculation, FormatTarget.Standard);
        return Display?.Text == standard?.Text ? Format(FormatTarget.DecimalValue) : Format(FormatTarget.Standard);
    }

    /// <summary>Puts the calculation before the one on the line back on it.</summary>
    /// <returns><see langword="true"/> when there was one.</returns>
    public bool RecallPrevious()
    {
        ImmutableArray<Calculation> history = History;
        if (_recalled + 1 >= history.Length)
        {
            return false;
        }

        Recall(history, _recalled + 1);
        return true;
    }

    /// <summary>Puts the calculation after the one on the line back on it.</summary>
    /// <returns><see langword="true"/> when there was one.</returns>
    public bool RecallNext()
    {
        if (_recalled <= 0)
        {
            return false;
        }

        Recall(History, _recalled - 1);
        return true;
    }

    /// <summary>Puts a calculation of the history back on the line.</summary>
    /// <param name="calculation">The calculation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="calculation"/> is <see langword="null"/>.</exception>
    [RelayCommand]
    public void Recall(Calculation calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        Input.Set(MathDocumentReader.Read(calculation.Input, _session.App));
        Calculation = calculation;
        Display = calculation.Display;
    }

    /// <summary>Clears the line and what was calculated (the AC key).</summary>
    [RelayCommand]
    public void Clear()
    {
        Input.Clear();
        Calculation = null;
        Display = null;
        _recalled = -1;
    }

    /// <summary>The S⇔D key, as a command a key of the keypad can be bound to.</summary>
    [RelayCommand]
    private void SwapForm() => ToggleDecimal();

    /// <summary>One conversion of the FORMAT menu, as a command.</summary>
    /// <param name="target">The conversion.</param>
    [RelayCommand]
    private void ApplyFormat(FormatTarget target) => Format(target);

    /// <summary>The older calculation, as a command.</summary>
    [RelayCommand]
    private void RecallOlder() => RecallPrevious();

    /// <summary>The newer calculation, as a command.</summary>
    [RelayCommand]
    private void RecallNewer() => RecallNext();

    private void Recall(ImmutableArray<Calculation> history, int index)
    {
        _recalled = index;
        Recall(history[index]);
    }

    private void OnRequested(object? sender, KeyCommand command)
    {
        switch (command)
        {
            case KeyCommand.Execute:
                Execute();
                break;
            case KeyCommand.MoveUp:
                RecallPrevious();
                break;
            case KeyCommand.MoveDown:
                RecallNext();
                break;
            default:
                break;
        }
    }
}
