// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Calc Settings screen (manual pp. 22-25).
/// </summary>
public partial class SettingsView : UserControl
{
    /// <summary>Creates the screen over the settings of the session.</summary>
    /// <param name="shell">The shell, whose settings every application shares.</param>
    public SettingsView(CalculatorShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        InitializeComponent();
        DataContext = shell.Settings;
    }
}
