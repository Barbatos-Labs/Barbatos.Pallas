// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The Table screen: f(x) and g(x) over a range of x, as rows or as a graph.
/// </summary>
/// <remarks>
/// The graph is sampled at the pixels of its drawing, so the drawing says how large it is whenever that changes, and
/// where the pointer is over it: what to sample and what to read is the view model's.
/// </remarks>
public partial class TableView : UserControl
{
    private readonly TableGraphViewModel _graph;

    /// <summary>Creates the screen over the shell.</summary>
    /// <param name="shell">The shell, which owns the session every screen shares.</param>
    /// <exception cref="ArgumentNullException"><paramref name="shell"/> is <see langword="null"/>.</exception>
    public TableView(CalculatorShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        InitializeComponent();
        DataContext = shell.Table;
        _graph = shell.Table.Graph;
        Drawing.SizeChanged += OnDrawingSizeChanged;
        Drawing.MouseMove += OnDrawingMouseMove;
        Drawing.MouseLeave += OnDrawingMouseLeave;
    }

    private void OnDrawingSizeChanged(object sender, SizeChangedEventArgs e) =>
        _graph.Resize((int)e.NewSize.Width, (int)e.NewSize.Height);

    private void OnDrawingMouseMove(object sender, MouseEventArgs e)
    {
        if (Drawing.ActualWidth > 0)
        {
            _graph.Read(e.GetPosition(Drawing).X / Drawing.ActualWidth);
        }
    }

    private void OnDrawingMouseLeave(object sender, MouseEventArgs e) => _graph.StopReading();
}
