// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;
using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The application shell window: one outlet, into which the router renders the screen of the current route.
/// </summary>
public partial class MainWindow : Window
{
    private readonly Router _router;
    private readonly IPreferences _preferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="router">The router whose current route this window shows.</param>
    /// <param name="preferences">The preferences where the window was when it was last closed is kept.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public MainWindow(Router router, IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(preferences);
        _router = router;
        _preferences = preferences;
        InitializeComponent();
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
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
        {
            WindowPlacement.Keep(this, _preferences);
        }
    }
}
