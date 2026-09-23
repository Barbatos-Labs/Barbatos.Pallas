// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// What a key does: put something into the input, or run a command.
/// </summary>
public abstract record KeyAction;

/// <summary>
/// Puts a symbol into the input.
/// </summary>
/// <param name="Text">The Canonical Linear Syntax of the symbol, such as <c>7</c>, <c>×</c> or <c>sin(</c>.</param>
public sealed record InsertSymbol(string Text) : KeyAction;

/// <summary>
/// Puts a structure into the input.
/// </summary>
/// <param name="Kind">The template.</param>
public sealed record InsertTemplate(MathTemplateKind Kind) : KeyAction;

/// <summary>
/// Runs a command of the calculator rather than typing anything.
/// </summary>
/// <param name="Command">The command.</param>
public sealed record RunCommand(KeyCommand Command) : KeyAction;

/// <summary>
/// Does several things as one keystroke, which one Delete then takes back one at a time.
/// </summary>
/// <param name="Actions">What the key does, in order.</param>
/// <remarks>
/// The ×10ˣ key is the reason: it types a times, a ten and a power that takes the ten, which is three edits and one
/// key.
/// </remarks>
public sealed record InsertSequence(ImmutableArray<KeyAction> Actions) : KeyAction;

/// <summary>
/// The commands a key can run.
/// </summary>
public enum KeyCommand
{
    /// <summary>Clear the input (AC).</summary>
    ClearAll = 0,

    /// <summary>Delete what is before the cursor.</summary>
    Delete,

    /// <summary>Calculate what has been typed.</summary>
    Execute,

    /// <summary>Move the cursor left.</summary>
    MoveLeft,

    /// <summary>Move the cursor right.</summary>
    MoveRight,

    /// <summary>Move the cursor up, to the slot above.</summary>
    MoveUp,

    /// <summary>Move the cursor down.</summary>
    MoveDown,

    /// <summary>Turn the shift mode on or off.</summary>
    Shift,

    /// <summary>Turn the alpha mode on or off.</summary>
    Alpha,

    /// <summary>Undo the last edit.</summary>
    Undo,

    /// <summary>Redo what was undone.</summary>
    Redo,

    /// <summary>Go to the home screen.</summary>
    Home,

    /// <summary>Go to the settings screen.</summary>
    Settings,

    /// <summary>Turn the result between its exact form and its decimal (S⇔D, manual p. 42).</summary>
    SwapForm,
}
