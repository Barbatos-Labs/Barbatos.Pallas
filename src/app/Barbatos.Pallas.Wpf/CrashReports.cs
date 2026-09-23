// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// A report of an exception nothing else caught, written where the application keeps its data.
/// </summary>
/// <remarks>
/// The engine reports a calculation that fails as a value and never throws for what the user types, so an exception
/// that reaches the application is a fault of the application, and the report is what makes it one that can be
/// fixed: the version, the system and the exception with its stack, in a file the user can send. Reports stay on the
/// machine - nothing is sent anywhere - and only the newest <see cref="Kept"/> are kept, so a fault that repeats does
/// not fill the disk.
/// </remarks>
public static class CrashReports
{
    /// <summary>How many reports are kept; older ones are deleted as a new one is written.</summary>
    public const int Kept = 20;

    /// <summary>The folder under the application's data folder that holds the reports.</summary>
    public const string FolderName = "crash-reports";

    private const string Pattern = "crash-*.txt";

    /// <summary>Writes a report and deletes all but the newest <see cref="Kept"/>.</summary>
    /// <param name="directory">The folder of the reports, created when it does not exist.</param>
    /// <param name="exception">What was thrown.</param>
    /// <param name="when">When it was thrown.</param>
    /// <param name="version">The version of the application.</param>
    /// <returns>The path of the report.</returns>
    /// <exception cref="ArgumentException"><paramref name="directory"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> or <paramref name="version"/> is <see langword="null"/>.</exception>
    public static string Write(string directory, Exception exception, DateTimeOffset when, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(version);

        Directory.CreateDirectory(directory);

        // UTC and the invariant culture: a report is read by whoever fixes the fault, not in the user's locale.
        DateTime utc = when.UtcDateTime;
        string path = Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"crash-{utc:yyyyMMdd-HHmmss-fff}.txt"));
        for (int suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(directory, string.Create(CultureInfo.InvariantCulture, $"crash-{utc:yyyyMMdd-HHmmss-fff}-{suffix}.txt"));
        }

        File.WriteAllText(path, string.Join(
            Environment.NewLine,
            "Barbatos Pallas " + version,
            string.Create(CultureInfo.InvariantCulture, $"{utc:yyyy-MM-dd HH:mm:ss.fff} UTC"),
            RuntimeInformation.OSDescription + " " + RuntimeInformation.OSArchitecture,
            RuntimeInformation.FrameworkDescription,
            string.Empty,
            exception.ToString(),
            string.Empty));

        Prune(directory);
        return path;
    }

    /// <summary>Deletes every report but the newest <see cref="Kept"/>.</summary>
    /// <param name="directory">The folder of the reports.</param>
    public static void Prune(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
        {
            return;
        }

        // The names sort by time, so the oldest come first.
        foreach (string old in Directory.GetFiles(directory, Pattern).Order(StringComparer.Ordinal).SkipLast(Kept))
        {
            File.Delete(old);
        }
    }
}
