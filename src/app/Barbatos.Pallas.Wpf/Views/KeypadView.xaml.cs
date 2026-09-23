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
/// The keyboard is routed through the same table as the keypad (<see cref="KeyboardMap"/>), so what a key does on
/// the screen and what it does on the keyboard cannot differ. A key the calculator has no use for is left to WPF.
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
        KeyId key = KeyboardMap.Find(e.Key.ToString(), shift);
        if (key == KeyId.None)
        {
            return;
        }

        input.Press(key);
        e.Handled = true;
    }
}
