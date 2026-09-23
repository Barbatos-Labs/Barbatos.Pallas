// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Presentation;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// Keeps the session in the application's preferences, so the calculator starts where it was left.
/// </summary>
/// <remarks>
/// A calculator that is switched off keeps its memory; this one keeps it in
/// <c>%LocalAppData%\Barbatos Labs\{AppId}\Settings\preferences.dat</c>. The whole session is one string, written by
/// <see cref="SessionSnapshotJson"/>, because the format has to be readable by every later build and a file of one
/// value is easier to keep that promise with than a key per memory.
/// </remarks>
public sealed class PreferencesSessionStore : ISessionStore
{
    /// <summary>The preference the session is written to. Immutable: an earlier build's session is read by this name.</summary>
    public const string Key = "session";

    private readonly IPreferences _preferences;

    /// <summary>Creates the store over the application's preferences.</summary>
    /// <param name="preferences">The preferences.</param>
    /// <exception cref="ArgumentNullException"><paramref name="preferences"/> is <see langword="null"/>.</exception>
    public PreferencesSessionStore(IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        _preferences = preferences;
    }

    /// <inheritdoc/>
    public StoredSession? Load() => SessionSnapshotJson.ReadSession(_preferences.Get(Key, string.Empty));

    /// <inheritdoc/>
    public void Save(StoredSession session) => _preferences.Set(Key, SessionSnapshotJson.Write(session));
}
