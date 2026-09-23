// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Barbatos.i18n.DependencyInjection;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.LifecycleEvents;
using Barbatos.Wpf.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// What the application does with an exception nothing else caught, and when it keeps the session.
/// </summary>
/// <remarks>
/// <para>
/// Left to WPF, an exception on the window's thread ends the process on the spot: the session is written as the
/// application exits normally, so it and everything typed since the start went with it. Now the exception is written
/// down (<see cref="CrashReports"/>), the session is saved, the user is told in their language where the report is,
/// and the calculator carries on. A fault that comes back a third time in one run ends it - clicking through the same
/// message is not using a calculator.
/// </para>
/// <para>
/// An exception on another thread ends the process whatever is done, so it is only reported: saving from there, while
/// the window's thread may be halfway through a calculation, could write a torn session over a good one. The session is
/// saved whenever the window loses the focus and when Windows ends the session instead, so what a crash can take is
/// what was typed since the user last looked away.
/// </para>
/// </remarks>
internal static class CrashGuard
{
    private const int Tolerated = 3;

    private static int _faults;

    /// <summary>Handles what reaches the window's thread and saves the session at the moments above.</summary>
    /// <param name="builder">The builder of the host.</param>
    public static void Configure(WpfAppBuilder builder) =>
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnDispatcherUnhandledException((_, args) => OnWindowThreadFault(args))
            .OnDeactivated((_, _) => Save())
            .OnSessionEnding((_, _) => Save())));

    /// <summary>Reports what is thrown on any other thread, and what a task threw that nothing awaited.</summary>
    public static void WatchOtherThreads()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Report(exception);
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Report(args.Exception);
            args.SetObserved();
        };
    }

    private static void OnWindowThreadFault(DispatcherUnhandledExceptionEventArgs args)
    {
        string? report = Report(args.Exception);
        Save();
        args.Handled = true;

        if (Interlocked.Increment(ref _faults) >= Tolerated)
        {
            Application.Current?.Shutdown();
            return;
        }

        ICompositeStringLocalizer? text = Services?.GetService<ICompositeStringLocalizer>();
        string message = text?["crash.message", report ?? "-"].Value ?? "Barbatos Pallas met an error and kept your session.";
        MessageBox.Show(message, text?["crash.title"].Value ?? "Barbatos Pallas", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static string? Report(Exception exception)
    {
        try
        {
            string data = Services?.GetService<IFileSystem>()?.AppDataDirectory ?? Path.GetTempPath();
            string version = typeof(CrashGuard).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
            return CrashReports.Write(Path.Combine(data, CrashReports.FolderName), exception, DateTimeOffset.Now, version);
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // A report that cannot be written must not become a second fault inside the handler of the first.
            return null;
        }
    }

    private static void Save()
    {
        try
        {
            Services?.GetService<CalculatorShellViewModel>()?.Save();
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // A session that cannot be written leaves the one written before it, which the next start reads.
        }
    }

    private static IServiceProvider? Services => IWpfPlatformApplication.Current?.Services;
}
