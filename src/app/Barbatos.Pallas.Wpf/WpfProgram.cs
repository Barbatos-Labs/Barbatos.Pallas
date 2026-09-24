// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.i18n.DependencyInjection;
using Barbatos.i18n.Wpf;
using Barbatos.i18n.Yaml;
using Barbatos.Pallas.DependencyInjection;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Presentation;
using Barbatos.Pallas.Wpf.Views;
using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.AquariusRouter.Routing;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.Input;
using Barbatos.Wpf.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, following the Barbatos.Wpf <c>WpfProgram</c> pattern.
/// </summary>
/// <remarks>
/// One engine, one session, one shell: every screen works on the session the shell owns, as every application of a
/// calculator works on the same memories. Which application the session is in follows the route, through the one
/// navigation guard below, so a link, a restored session and a shortcut all reach it the same way.
/// </remarks>
public static class WpfProgram
{
    /// <summary>
    /// Builds the application host.
    /// </summary>
    /// <returns>The configured host.</returns>
    public static WpfApp CreateWpfApp()
    {
        // First, so that a fault while the host is being built is reported like any other.
        CrashGuard.WatchOtherThreads();

        WpfAppBuilder builder = WpfApp.CreateBuilder();

        builder.ConfigureSingleInstance();

        // An exception nothing else caught is reported and the session saved; the session is also saved whenever the
        // window loses the focus, so a crash takes little with it.
        CrashGuard.Configure(builder);

        // The window's own shortcuts - undo, copy, paste, the CATALOG, the settings - through the host's input
        // system; the keys of the calculator are the keypad table.
        builder.ConfigureInputSystem(Shortcuts.Configure);

        // The engine, with the reference data (CODATA 2022, NIST SP 811, CIAAW) it ships with.
        builder.Services.AddPallas();

        // One session for the whole application, stored in the preferences. AddPallas registers a session per
        // request, which is right for a service and wrong for a calculator: the memories are one set, so the
        // session is a singleton of the engine here. It is resolved from the engine and not from the shell that
        // owns it - a factory that reads it back off the shell asks the container for the shell while it is
        // building the shell, and that is a startup that never finishes (measured 22 Sep 2026).
        builder.Services.AddSingleton(provider => provider.GetRequiredService<PallasEngine>().CreateSession());
        builder.Services.AddSingleton<ISessionStore, PreferencesSessionStore>();
        builder.Services.AddSingleton<CalculatorShellViewModel>();
        builder.Services.AddSingleton(provider => provider.GetRequiredService<CalculatorShellViewModel>().Settings);

        builder.Services.AddStringLocalizer(localization =>
        {
            localization.FromYaml("Locales.en-US.yaml", new CultureInfo("en-US"));
            localization.FromYaml("Locales.vi-VN.yaml", new CultureInfo("vi-VN"));
        });

        builder.Services.AddAquariusRouter(_ => AppRoutes.Build());

        // The router builds a screen through the service provider, so each one is registered rather than discovered:
        // Barbatos.Pallas takes no reflection-based shortcuts in the application either.
        builder.Services.AddTransient<MainWindow>();
        builder.Services.AddTransient<HomeView>();
        builder.Services.AddTransient<SettingsView>();
        builder.Services.AddTransient<CalculateView>();
        builder.Services.AddTransient<BaseNView>();
        builder.Services.AddTransient<MatrixView>();
        builder.Services.AddTransient<VectorView>();
        builder.Services.AddTransient<StatisticsView>();
        builder.Services.AddTransient<DistributionView>();
        builder.Services.AddTransient<EquationView>();
        builder.Services.AddTransient<InequalityView>();
        builder.Services.AddTransient<RatioView>();
        builder.Services.AddTransient<TableView>();
        builder.Services.AddTransient<SpreadsheetView>();
        builder.Services.AddTransient<MathBoxView>();

        WpfApp app = builder.Build();

        // The language of Windows is read before a stored choice is applied, after which it is all there is to read.
        CultureInfo windows = CultureInfo.CurrentUICulture;
        CalculatorShellViewModel shell = app.Services.GetRequiredService<CalculatorShellViewModel>();
        app.Services.UseWpfLocalization();
        AppLanguage.Follow(
            shell,
            app.Services.GetRequiredService<IPreferences>(),
            windows,
            culture => app.Services.SetLocalizationCulture(culture));
        app.Services.UseAquarius();
        Router router = app.Services.UseAquariusRouter();

        router.BeforeEach((to, _) => Task.FromResult<NavigationGuardResult>(Opens(shell, to.Path)));

        // The keypad has keys for the home screen and the settings; the shell says where, the router goes there.
        shell.NavigationRequested += (_, route) => router.Push(route);
        Shortcuts.Wire(app.Services.GetRequiredService<IInputSystemService>(), shell, router);

        return app;
    }

    /// <summary>Switches the session to the application a route names, and refuses a route this build cannot open.</summary>
    private static bool Opens(CalculatorShellViewModel shell, string path)
    {
        // Home and the settings are screens of this application, not applications of the calculator.
        return CalculatorApps.ByRoute(path) is null || shell.OpenRoute(path);
    }
}
