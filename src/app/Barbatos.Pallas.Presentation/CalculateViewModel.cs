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
    private readonly SessionHistory _history;
    private readonly SessionWork _work;
    private int _recalled = -1;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that calculates.</param>
    /// <param name="history">
    /// The session's history, shared by every screen of it; <see langword="null"/> for a screen of its own, whose
    /// history is this run's.
    /// </param>
    /// <param name="work">
    /// The work of the session, shared by every screen of it; <see langword="null"/> for a screen of its own, which
    /// calculates where it is asked.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public CalculateViewModel(CalculatorSession session, SessionHistory? history = null, SessionWork? work = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _history = history ?? new SessionHistory(session);
        _work = work ?? SessionWork.Immediate;
        Input = new MathInputViewModel { App = session.App };
        Input.Requested += OnRequested;
        Input.StoreRequested += (_, name) => StoreIn(name);
        Catalog = new CatalogViewModel(session.Engine.Vocabulary, session.App);
        Catalog.Chosen += (_, action) => TypeAndClose(action);
    }

    /// <summary>Gets the line the user types on.</summary>
    public MathInputViewModel Input { get; }

    /// <summary>Gets the CATALOG of the application the line belongs to.</summary>
    public CatalogViewModel Catalog { get; }

    /// <summary>Gets the conversions the FORMAT menu offers (pp. 42-50).</summary>
    /// <remarks>
    /// All of them, as the calculator lists them: one that does not apply to the result - a polar form of a real
    /// number - is chosen and changes nothing, which is what <see cref="Format"/> reports.
    /// </remarks>
    public ImmutableArray<FormatTarget> Formats { get; } = [.. Enum.GetValues<FormatTarget>()];

    /// <summary>Gets the variables of the application and what they hold, as RCL lists them.</summary>
    /// <remarks>In Base-N, A to F are digits, so only x, y and z are variables there (assumption U12).</remarks>
    public ImmutableArray<VariableLine> Variables =>
    [
        .. VariablesOf(_session.App).Select(variable => new VariableLine(
            NameOf(variable),
            PallasEngine.Format(_session.GetVariable(variable), _session.Settings, _session.Profile)?.Text ?? string.Empty)),
    ];

    /// <summary>Gets which menu is on the display.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Variables))]
    private CalculatorMenu _menu;

    /// <summary>Gets what the last store did, such as <c>Ans→A</c>, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _notice;

    /// <summary>Closes the menu that is on the display.</summary>
    [RelayCommand]
    public void CloseMenu() => Menu = CalculatorMenu.None;

    /// <summary>Shows the result as the FORMAT menu entry says, and closes the menu.</summary>
    /// <param name="target">The conversion.</param>
    [RelayCommand]
    public void ChooseFormat(FormatTarget target)
    {
        Format(target);
        Menu = CalculatorMenu.None;
    }

    /// <summary>Puts a variable on the line, and closes the list.</summary>
    /// <param name="variable">The variable; <see langword="null"/> does nothing.</param>
    [RelayCommand]
    public void ChooseVariable(VariableLine? variable)
    {
        if (variable is null)
        {
            return;
        }

        TypeAndClose(new InsertSymbol(variable.Name));
    }

    /// <summary>Gets the localization key of the name of the application the line belongs to.</summary>
    /// <remarks>Calculate and Complex are the same screen, and the title is how the user tells them apart.</remarks>
    public string TitleKey => CalculatorApps.Of(_session.App).NameKey;

    /// <summary>Tells the screen that the application changed, and with it its title and its keys.</summary>
    public void Refresh()
    {
        Input.App = _session.App;
        Catalog.App = _session.App;
        Menu = CalculatorMenu.None;
        OnPropertyChanged(nameof(TitleKey));
        OnPropertyChanged(nameof(History));
    }

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

    /// <summary>Gets the result as text, which is what copying it takes, or <see langword="null"/> when there is none.</summary>
    /// <remarks>What the screen shows - after S⇔D or a FORMAT conversion, that form - since that is what the user sees.</remarks>
    public string? Answer => HasResult ? Display!.Text : null;

    /// <summary>Puts text on the line, as it would come back from the history, replacing what is there.</summary>
    /// <param name="text">The text, such as a calculation copied from elsewhere; only its first line is read.</param>
    /// <returns><see langword="true"/> when there was something to put there.</returns>
    /// <remarks>
    /// The text is read by the engine's own parser, so a pasted fraction is a fraction on the line; what it cannot
    /// read comes back as its characters, to be corrected. Replacing the line is an edit, so undo brings it back.
    /// </remarks>
    public bool Paste(string? text)
    {
        string line = (text ?? string.Empty).Split('\n', 2)[0].Trim();
        if (line.Length == 0)
        {
            return false;
        }

        Input.Set(MathDocumentReader.Read(line, _session.App));
        return true;
    }

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    public string? ErrorKey => Calculation?.Error is { } error ? "error." + error.Kind : null;

    /// <summary>Gets what was calculated before, newest first - this run's, then what was kept from an earlier one.</summary>
    public ImmutableArray<HistoryEntry> History => [.. _history.Entries.Reverse()];

    /// <summary>Calculates what is on the line.</summary>
    /// <remarks>
    /// An empty line calculates nothing. A line that fails leaves what was typed where it is, with the cursor at the
    /// place the engine could not read, so the next keystroke corrects it. The calculation is a work of the session:
    /// what is shown until it is done is what was shown before, and a calculation that is stopped shows nothing new.
    /// </remarks>
    [RelayCommand]
    public void Execute()
    {
        string text = Input.Linear;
        if (text.Length == 0)
        {
            return;
        }

        _work.Start(token => _session.Calculate(text, token), Show);
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
        ImmutableArray<HistoryEntry> history = History;
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

    /// <summary>Puts a line of the history back on the line.</summary>
    /// <param name="entry">The line.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A calculation of this run comes back with what it came to; a line of an earlier run comes back as what was
    /// typed, to be calculated again, because what it came to depended on memories that may have changed since.
    /// </remarks>
    [RelayCommand]
    public void Recall(HistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Input.Set(MathDocumentReader.Read(entry.Input, _session.App));
        Calculation = entry.Calculation;
        Display = entry.Calculation?.Display;
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

    private void Show(Calculation calculation)
    {
        Calculation = calculation;
        Display = calculation.Display;
        Notice = null;
        _recalled = -1;

        if (calculation.Error is { } error)
        {
            Input.MoveTo(error.Span.Start);
        }
    }

    private void Recall(ImmutableArray<HistoryEntry> history, int index)
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
            case KeyCommand.SwapForm:
                ToggleDecimal();
                break;
            case KeyCommand.Catalog:
                Menu = CalculatorMenu.Catalog;
                break;
            case KeyCommand.Format:
                Menu = CalculatorMenu.Format;
                break;
            case KeyCommand.Recall:
                Menu = CalculatorMenu.Recall;
                break;
            default:
                break;
        }
    }

    private void TypeAndClose(KeyAction action)
    {
        Input.Type(action);
        Menu = CalculatorMenu.None;
    }

    /// <summary>Stores the last answer in the variable STO named, where the application has one of that name.</summary>
    private void StoreIn(string name)
    {
        // A name the application has no variable for - A in Base-N, where it is a digit - stores nothing.
        if (VariablesOf(_session.App).Where(variable => NameOf(variable) == name).ToArray() is not [MemoryVariable variable])
        {
            return;
        }

        _session.Store(variable);
        Notice = "Ans→" + name;
        OnPropertyChanged(nameof(Variables));
    }

    private static ImmutableArray<MemoryVariable> VariablesOf(CalculatorApp app) =>
        app is CalculatorApp.BaseN
            ? [MemoryVariable.X, MemoryVariable.Y, MemoryVariable.Z]
            : [.. Enum.GetValues<MemoryVariable>()];

    /// <summary>The variable as the line spells it: A to F in capitals, x, y and z in small letters (p. 30).</summary>
    private static string NameOf(MemoryVariable variable) => variable switch
    {
        MemoryVariable.X => "x",
        MemoryVariable.Y => "y",
        MemoryVariable.Z => "z",
        _ => variable.ToString(),
    };
}
