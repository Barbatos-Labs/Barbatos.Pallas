// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, following the Barbatos.Wpf <c>WpfProgram</c> pattern.
/// </summary>
/// <remarks>
/// Phase 0 registers only the shell window. The engine (<c>AddPallas()</c>), the router for the thirteen
/// calculator applications and localization are registered here as their phases land.
/// </remarks>
public static class WpfProgram
{
    /// <summary>
    /// Builds the application host.
    /// </summary>
    /// <returns>The configured host.</returns>
    public static WpfApp CreateWpfApp()
    {
        WpfAppBuilder builder = WpfApp.CreateBuilder();

        builder.ConfigureSingleInstance();

        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }
}
