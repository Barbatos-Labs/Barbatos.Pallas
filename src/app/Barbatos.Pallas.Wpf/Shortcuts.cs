// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.Input;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The keyboard shortcuts of the window: what a desktop application is expected to answer that a calculator has no
/// key for.
/// </summary>
/// <remarks>
/// <para>
/// The keys of the calculator are the keypad table, reached from the keyboard through <see cref="KeyboardMap"/>;
/// these are the window's own, bound through Barbatos.Wpf.Core's input system rather than a handler of our own.
/// </para>
/// <para>
/// The input system sees a key before the element with the focus does and does not stop it there, so a shortcut
/// that edits - undo, copy, paste - leaves the key to a text box that has the focus: Ctrl+V in a cell of the sheet
/// pastes into the cell, not onto the calculator's line behind it. A shortcut that edits also waits while a menu is
/// on the display, whose own keys those are.
/// </para>
/// </remarks>
internal static class Shortcuts
{
    /// <summary>The name of the map of actions, as the input system knows it.</summary>
    public const string Map = "Calculator";

    /// <summary>Takes back the last edit of the line (Ctrl+Z).</summary>
    public const string Undo = "Undo";

    /// <summary>Puts back what was taken back (Ctrl+Y).</summary>
    public const string Redo = "Redo";

    /// <summary>Copies the result as the screen shows it (Ctrl+C).</summary>
    public const string Copy = "Copy";

    /// <summary>Puts a calculation from the clipboard on the line (Ctrl+V).</summary>
    public const string Paste = "Paste";

    /// <summary>Opens the CATALOG (Ctrl+K).</summary>
    public const string Catalog = "Catalog";

    /// <summary>Opens the settings (Ctrl+,).</summary>
    public const string Settings = "Settings";

    /// <summary>Adds the shortcuts to the input system's options.</summary>
    /// <param name="options">The options.</param>
    public static void Configure(InputSystemOptions options)
    {
        InputActionMap map = new(Map);
        map.AddAction(Undo).AddKeyBinding(Key.Z, ModifierKeys.Control);
        map.AddAction(Redo).AddKeyBinding(Key.Y, ModifierKeys.Control);
        map.AddAction(Copy).AddKeyBinding(Key.C, ModifierKeys.Control);
        map.AddAction(Paste).AddKeyBinding(Key.V, ModifierKeys.Control);
        map.AddAction(Catalog).AddKeyBinding(Key.K, ModifierKeys.Control);
        map.AddAction(Settings).AddKeyBinding(Key.OemComma, ModifierKeys.Control);
        options.ActionMaps.Add(map);
    }

    /// <summary>Says what each shortcut does, once the application's services exist.</summary>
    /// <param name="input">The input system.</param>
    /// <param name="shell">The shell whose screens the shortcuts act on.</param>
    /// <param name="router">The router, which says which screen is showing.</param>
    public static void Wire(IInputSystemService input, CalculatorShellViewModel shell, Router router)
    {
        void On(string action, Action<CalculateViewModel> run, bool edits = true) =>
            input.FindAction(action)!.Performed += (_, _) =>
            {
                if (shell.LineOf(router.CurrentRoute.Value.Path) is not { } line
                    || (edits && (Keyboard.FocusedElement is TextBoxBase || line.Menu is not CalculatorMenu.None)))
                {
                    return;
                }

                run(line);
            };

        On(Undo, line => line.Input.Undo());
        On(Redo, line => line.Input.Redo());
        On(Copy, CopyAnswer);
        On(Paste, line => line.Paste(Clipboard.ContainsText() ? Clipboard.GetText() : null));
        On(Catalog, line => line.Input.Press(KeyId.Catalog), edits: false);

        // The settings are not a screen with a line, so they are reached from anywhere.
        input.FindAction(Settings)!.Performed += (_, _) => _ = router.Push("/settings");
    }

    private static void CopyAnswer(CalculateViewModel line)
    {
        if (line.Answer is { } answer)
        {
            Clipboard.SetText(answer);
        }
    }
}
