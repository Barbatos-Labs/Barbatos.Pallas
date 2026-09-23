// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Input;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The line of the calculator and what it came to: the part every application that calculates an expression shows.
/// </summary>
/// <remarks>
/// Its data context is a <see cref="Presentation.CalculateViewModel"/>. When a menu closes, the keys take the
/// keyboard back, or typing would stop reaching the line.
/// </remarks>
public partial class CalculationPanelView : UserControl
{
    /// <summary>Creates the control.</summary>
    public CalculationPanelView()
    {
        InitializeComponent();
        Menu.Closed += (_, _) =>
        {
            Keys.Focus();
            Keyboard.Focus(Keys);
        };
    }
}
