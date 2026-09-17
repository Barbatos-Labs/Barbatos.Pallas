// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The WPF application entry point. Composition lives in <see cref="WpfProgram"/>.
/// </summary>
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

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
