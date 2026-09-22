// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The WPF application entry point. Composition lives in <see cref="WpfProgram"/>.
/// </summary>
/// <remarks>
/// A calculator that is switched off keeps its memory: the session is read back before the first window is shown and
/// written out as the application closes, while the services are still there to write it with.
/// </remarks>
public partial class App : WpfApplication
{
    /// <inheritdoc />
    protected override WpfApp CreateWpfApp()
    {
        return WpfProgram.CreateWpfApp();
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Services.GetRequiredService<CalculatorShellViewModel>().Load();

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        // Before the base call: it is what disposes the host the session is written through.
        Services.GetRequiredService<CalculatorShellViewModel>().Save();

        base.OnExit(e);
    }
}
