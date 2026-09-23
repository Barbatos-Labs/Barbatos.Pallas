// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The menus of the calculator, over its keys: the CATALOG, FORMAT and the variables.
/// </summary>
/// <remarks>
/// A menu takes the keyboard while it is open - the CATALOG into its search, the others into the menu itself - so
/// that a digit typed then does not go onto the line behind it, and Escape closes it as AC would. Its data context
/// is a <see cref="CalculateViewModel"/>.
/// </remarks>
public partial class CalculatorMenuView : UserControl
{
    private CalculateViewModel? _screen;

    /// <summary>Creates the view.</summary>
    public CalculatorMenuView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Watch(DataContext as CalculateViewModel);
    }

    /// <summary>Raised when a menu has closed, so that the keypad can take the keyboard back.</summary>
    public event EventHandler? Closed;

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e is { Key: Key.Escape } && _screen is not null)
        {
            _screen.CloseMenu();
            e.Handled = true;
        }
    }

    private void Watch(CalculateViewModel? screen)
    {
        if (_screen is not null)
        {
            _screen.PropertyChanged -= OnScreenChanged;
        }

        _screen = screen;
        if (_screen is not null)
        {
            _screen.PropertyChanged += OnScreenChanged;
        }
    }

    private void OnScreenChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CalculateViewModel.Menu) || _screen is null)
        {
            return;
        }

        CalculatorMenu menu = _screen.Menu;

        // After the layout pass that makes the menu visible: an element that is still collapsed takes no focus.
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            switch (menu)
            {
                case CalculatorMenu.None:
                    Closed?.Invoke(this, EventArgs.Empty);
                    break;
                case CalculatorMenu.Catalog:
                    Search.Focus();
                    Keyboard.Focus(Search);
                    break;
                default:
                    Panel.Focus();
                    break;
            }
        });
    }
}
