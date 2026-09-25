// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The application shell window: one outlet, into which the router renders the screen of the current route, and the
/// cover that keeps the screen still while the session calculates.
/// </summary>
/// <remarks>
/// While the session calculates, it belongs to the thread doing it (<see cref="SessionWork"/>): a key or a click that
/// reached a screen could read or change it halfway. So the window takes none, as a calculator's keys wait while it
/// calculates - except AC, which stops the calculation (assumption U32).
/// </remarks>
public partial class MainWindow : Window
{
    private readonly Router _router;
    private readonly IPreferences _preferences;
    private readonly SessionWork _work;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="router">The router whose current route this window shows.</param>
    /// <param name="preferences">The preferences where the window was when it was last closed is kept.</param>
    /// <param name="shell">The shell, whose session's work the window waits for.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public MainWindow(Router router, IPreferences preferences, CalculatorShellViewModel shell)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(shell);
        _router = router;
        _preferences = preferences;
        _work = shell.Work;
        InitializeComponent();
        Busy.DataContext = _work;
        WindowPlacement.Restore(this, preferences);
    }

    /// <inheritdoc />
    protected override async void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // There is no address bar to seed the current route from, so the first navigation is made here. The
        // calculator starts on its home screen even when a session was restored: the session says which application
        // the memories belong to, not which screen the user asked for this time.
        await _router.Start("/");
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPreviewKeyDown(e);
        if (!_work.IsBusy)
        {
            return;
        }

        // The key AC is on the keyboard - Escape - stops the calculation; every other key waits with the screen.
        bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (KeyboardMap.Find(e.Key.ToString(), shift) == KeyId.ClearAll)
        {
            _work.Cancel();
        }

        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPreviewTextInput(TextCompositionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        base.OnPreviewTextInput(e);
        e.Handled |= _work.IsBusy;
    }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
        {
            // A calculation still running has nothing to show its result on; the session is saved as it was before it.
            _work.Cancel();
            WindowPlacement.Keep(this, _preferences);
        }
    }
}
