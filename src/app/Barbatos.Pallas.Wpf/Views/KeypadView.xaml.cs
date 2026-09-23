// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Input;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The keys of the calculator.
/// </summary>
/// <remarks>
/// <para>
/// The keyboard is routed through the same table as the keypad (<see cref="KeyboardMap"/>), so what a key does on
/// the screen and what it does on the keyboard cannot differ. A key the calculator has no use for is left to WPF.
/// </para>
/// <para>
/// The keypad is not a text field, so the input method is off on it. With Windows' Vietnamese input method on, the
/// digits typed on the keyboard never reached the line while + did: the input method takes keys of the digit row,
/// and WPF reports such a key as <see cref="Key.ImeProcessed"/> (measured 23 Sep 2026). A key an input method
/// still processes is read as the key it was.
/// </para>
/// </remarks>
public partial class KeypadView : UserControl
{
    /// <summary>Creates the view.</summary>
    public KeypadView()
    {
        InitializeComponent();
        Loaded += (_, _) => Focus();
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e is null || DataContext is not MathInputViewModel input)
        {
            return;
        }

        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        Key pressed = e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key;
        KeyId key = KeyboardMap.Find(pressed.ToString(), shift);
        if (key == KeyId.None)
        {
            return;
        }

        input.Press(key);
        e.Handled = true;
    }
}
