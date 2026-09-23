// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Globalization;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// The language the application speaks: chosen on the settings screen, kept in the preferences, applied as it changes.
/// </summary>
/// <remarks>
/// The shell only holds the choice (<see cref="CalculatorShellViewModel.Language"/>); what a culture is and where it is
/// kept are the host's. The language of Windows is the one the application started in, read before any choice is
/// applied: after that the current culture is the choice, and following Windows would follow the choice back.
/// </remarks>
public static class AppLanguage
{
    /// <summary>The name the language is stored under.</summary>
    public const string Key = "language";

    /// <summary>Returns the culture a language of the shell is.</summary>
    /// <param name="language">A value of <see cref="CalculatorShellViewModel.Languages"/>.</param>
    /// <param name="windows">The culture Windows is in, which <see cref="CalculatorShellViewModel.SystemLanguage"/> is.</param>
    /// <returns>The culture.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="windows"/> is <see langword="null"/>.</exception>
    public static CultureInfo CultureOf(string language, CultureInfo windows)
    {
        ArgumentNullException.ThrowIfNull(windows);
        string known = CalculatorShellViewModel.KnownLanguage(language);
        return known == CalculatorShellViewModel.SystemLanguage ? windows : CultureInfo.GetCultureInfo(known);
    }

    /// <summary>Gives the shell the language that was stored, applies it, and keeps and applies every later choice.</summary>
    /// <param name="shell">The shell whose choice this is.</param>
    /// <param name="preferences">Where the choice is kept.</param>
    /// <param name="windows">The culture Windows is in.</param>
    /// <param name="apply">Switches the application to a culture.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static void Follow(CalculatorShellViewModel shell, IPreferences preferences, CultureInfo windows, Action<CultureInfo> apply)
    {
        ArgumentNullException.ThrowIfNull(shell);
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(apply);

        shell.Language = CalculatorShellViewModel.KnownLanguage(preferences.Get(Key, string.Empty));
        apply(CultureOf(shell.Language, windows));
        shell.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CalculatorShellViewModel.Language))
            {
                preferences.Set(Key, shell.Language);
                apply(CultureOf(shell.Language, windows));
            }
        };
    }
}
