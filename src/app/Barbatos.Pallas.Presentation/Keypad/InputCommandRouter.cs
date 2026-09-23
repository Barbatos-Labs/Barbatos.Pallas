// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// What one keystroke did: the input after it, whether it is worth undoing, and what it asks the application to do.
/// </summary>
/// <param name="Document">The input after the keystroke.</param>
/// <param name="Edited">Whether the input itself changed, which is what undo takes back.</param>
/// <param name="Request">What the key asks of the application, or <see langword="null"/>.</param>
public readonly record struct KeyResult(MathDocument Document, bool Edited, KeyCommand? Request);

/// <summary>
/// Turns what a key means into what the input does. The only place that does.
/// </summary>
/// <remarks>
/// Moving the cursor is not an edit: undo takes back what was typed, not where the user looked. A key the input
/// cannot carry out itself - Execute, Undo, the modes, the screens - is handed back as a request, so the router
/// needs to know nothing about sessions or navigation.
/// </remarks>
public static class InputCommandRouter
{
    /// <summary>Applies what a key means to the input.</summary>
    /// <param name="document">The input.</param>
    /// <param name="action">What the key means.</param>
    /// <returns>What the keystroke did.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="action"/> is of a kind this router has not.</exception>
    public static KeyResult Apply(MathDocument document, KeyAction action)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(action);

        switch (action)
        {
            case InsertSymbol symbol:
                return new KeyResult(document.Insert(symbol.Text), Edited: true, Request: null);
            case InsertTemplate template:
                return new KeyResult(document.Insert(template.Kind), Edited: true, Request: null);
            case InsertSequence sequence:
                MathDocument typed = document;
                foreach (KeyAction one in sequence.Actions)
                {
                    typed = Apply(typed, one).Document;
                }

                return new KeyResult(typed, Edited: true, Request: null);
            case RunCommand command:
                return Run(document, command.Command);
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Not a kind of KeyAction this router has.");
        }
    }

    private static KeyResult Run(MathDocument document, KeyCommand command)
    {
        return command switch
        {
            KeyCommand.Delete => new KeyResult(document.Backspace(), Edited: true, Request: null),
            KeyCommand.ClearAll => new KeyResult(MathDocument.Empty, Edited: true, Request: null),
            KeyCommand.MoveLeft => new KeyResult(document.MoveLeft(), Edited: false, Request: null),
            KeyCommand.MoveRight => new KeyResult(document.MoveRight(), Edited: false, Request: null),
            KeyCommand.MoveUp => new KeyResult(document.MoveUp(), Edited: false, Request: null),
            KeyCommand.MoveDown => new KeyResult(document.MoveDown(), Edited: false, Request: null),
            _ => new KeyResult(document, Edited: false, Request: command),
        };
    }
}
