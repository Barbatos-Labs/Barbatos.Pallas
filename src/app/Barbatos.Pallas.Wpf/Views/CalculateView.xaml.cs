// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Calculate screen: the line, what it came to, and what was calculated before it.
/// </summary>
public partial class CalculateView : UserControl
{
    /// <summary>Creates the screen over the shell.</summary>
    /// <param name="shell">The shell, which owns the session and the line being typed.</param>
    public CalculateView(CalculatorShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell.Calculate;
    }
}
