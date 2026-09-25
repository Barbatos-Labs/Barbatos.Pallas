// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Spreadsheet screen: a sheet of constants and formulas.
/// </summary>
public partial class SpreadsheetView : UserControl
{
    /// <summary>Creates the screen over the shell.</summary>
    /// <param name="shell">The shell, which owns the session every screen shares.</param>
    /// <exception cref="ArgumentNullException"><paramref name="shell"/> is <see langword="null"/>.</exception>
    public SpreadsheetView(CalculatorShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        InitializeComponent();
        DataContext = shell.Spreadsheet;
    }

    // The list tells the screen which cell the user chose, and the screen tells the list which cell is selected. The
    // cells the list shows are new each time the sheet changes, and the selection it clears then is no choice: bound
    // both ways it wrote itself back as nothing, which a cell address cannot be, and the window drew the red border of
    // a failed binding round the sheet and lost the selected cell (25 Sep 2026).
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedValue: CellAddress address } && DataContext is SpreadsheetViewModel screen)
        {
            screen.Selected = address;
        }
    }
}
