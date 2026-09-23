// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The line the user types on: the input itself, what the screen draws of it, and what the keys do to it.
/// </summary>
/// <remarks>
/// Undo is a stack of inputs rather than a log of reversible edits, because an input is immutable and small. Moving
/// the cursor is not undone: what undo takes back is what was typed.
/// </remarks>
public sealed partial class MathInputViewModel : ObservableObject
{
    private readonly Stack<MathDocument> _undone = new();
    private readonly Stack<MathDocument> _redone = new();

    /// <summary>Gets the input.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Linear))]
    [NotifyPropertyChangedFor(nameof(Latex))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private MathDocument _document = MathDocument.Empty;

    /// <summary>Gets which meaning the next key carries (manual p. 18).</summary>
    [ObservableProperty]
    private KeyMode _mode = KeyMode.Primary;

    /// <summary>Raised for a key the input cannot carry out itself, such as Execute or the home screen.</summary>
    public event EventHandler<KeyCommand>? Requested;

    /// <summary>Gets the keys row by row, as the screen draws them.</summary>
    public ImmutableArray<ImmutableArray<KeyDefinition>> Rows { get; } = [.. Keypad.Rows.Select(row => row.Select(Keypad.Of).ToImmutableArray())];

    /// <summary>Gets the input as Canonical Linear Syntax, which is what the engine reads.</summary>
    public string Linear => MathLinearWriter.Write(Document);

    /// <summary>Gets the input as LaTeX, with the cursor drawn in it, which is what the screen shows.</summary>
    public string Latex => MathLatexWriter.Write(Document);

    /// <summary>Gets whether nothing has been typed.</summary>
    public bool IsEmpty => Document.IsEmpty;

    /// <summary>Gets whether there is an edit to take back.</summary>
    public bool CanUndo => _undone.Count > 0;

    /// <summary>Gets whether there is an edit to put back.</summary>
    public bool CanRedo => _redone.Count > 0;

    /// <summary>Presses a key.</summary>
    /// <param name="id">The key; one the keypad has not is ignored.</param>
    [RelayCommand]
    public void Press(KeyId id)
    {
        if (Keypad.Find(id) is not { } key)
        {
            return;
        }

        KeyAction? action = key.In(Mode);

        // A mode lasts for one key: Shift and then a key is that key's second meaning, and nothing after it.
        Mode = KeyMode.Primary;
        if (action is null)
        {
            return;
        }

        KeyResult result = InputCommandRouter.Apply(Document, action);
        switch (result.Request)
        {
            case null when action is RunCommand { Command: KeyCommand.MoveUp or KeyCommand.MoveDown } move && ReferenceEquals(result.Document, Document):
                // The cursor had nowhere to go, so the key belongs to whatever is above and below the line itself -
                // on the Calculate screen, the calculations before this one.
                Requested?.Invoke(this, move.Command);
                return;
            case null:
                Apply(result);
                return;
            case KeyCommand.Shift:
                // Pressing Shift twice turns it off, because a key has no second meaning in the shift mode and the
                // mode was already put back above.
                Mode = KeyMode.Shift;
                return;
            case KeyCommand.Alpha:
                Mode = KeyMode.Alpha;
                return;
            case KeyCommand.Undo:
                Undo();
                return;
            case KeyCommand.Redo:
                Redo();
                return;
            default:
                Requested?.Invoke(this, result.Request.Value);
                return;
        }
    }

    /// <summary>Puts an input on the line, as recalling a calculation does.</summary>
    /// <param name="document">The input.</param>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public void Set(MathDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Record(Document);
        Document = document;
    }

    /// <summary>Clears the line.</summary>
    public void Clear() => Set(MathDocument.Empty);

    /// <summary>Moves the cursor to a place in the written line, which is where an error says it is.</summary>
    /// <param name="offset">How many characters of <see cref="Linear"/> come before the place.</param>
    /// <remarks>Moving the cursor is not an edit, so this is not something undo takes back.</remarks>
    public void MoveTo(int offset) => Document = Document.MoveTo(offset);

    /// <summary>Takes back the last edit.</summary>
    public void Undo()
    {
        if (_undone.Count == 0)
        {
            return;
        }

        _redone.Push(Document);
        Document = _undone.Pop();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    /// <summary>Puts back what was taken back.</summary>
    public void Redo()
    {
        if (_redone.Count == 0)
        {
            return;
        }

        _undone.Push(Document);
        Document = _redone.Pop();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private void Apply(KeyResult result)
    {
        if (result.Edited)
        {
            Record(Document);
        }

        Document = result.Document;
    }

    private void Record(MathDocument document)
    {
        _undone.Push(document);
        _redone.Clear();
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }
}
