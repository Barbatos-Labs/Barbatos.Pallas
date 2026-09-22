// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The screen of an application whose own screen has not been built yet: it says which application is open and what
/// is still to come.
/// </summary>
/// <remarks>
/// The engine behind these applications is finished; the screens arrive one milestone at a time. Showing the opened
/// application rather than nothing keeps the shell honest about the difference.
/// </remarks>
public partial class AppScreenView : UserControl
{
    /// <summary>Creates the screen over the shell.</summary>
    /// <param name="shell">The shell, which knows the application that is open.</param>
    public AppScreenView(CalculatorShellViewModel shell)
    {
        InitializeComponent();
        DataContext = shell;
    }
}
