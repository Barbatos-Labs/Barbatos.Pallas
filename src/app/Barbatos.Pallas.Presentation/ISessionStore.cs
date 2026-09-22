// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Where the application keeps a session between runs (manual p. 102: a calculator that is switched off keeps its
/// memory; this one keeps it on disk).
/// </summary>
/// <remarks>
/// The host implements it - in the WPF app over the preferences of Barbatos.Wpf.Core - so that the presentation layer
/// needs no file system, no registry and no serializer of its own.
/// </remarks>
public interface ISessionStore
{
    /// <summary>Reads the session that was stored, or <see langword="null"/> when there is none or it cannot be read.</summary>
    /// <returns>The snapshot, or <see langword="null"/>.</returns>
    SessionSnapshot? Load();

    /// <summary>Stores a session, replacing what was there.</summary>
    /// <param name="snapshot">The snapshot.</param>
    void Save(SessionSnapshot snapshot);
}

/// <summary>
/// A store that keeps a session only while the application runs: the default, and what tests use.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private SessionSnapshot? _snapshot;

    /// <inheritdoc/>
    public SessionSnapshot? Load() => _snapshot;

    /// <inheritdoc/>
    public void Save(SessionSnapshot snapshot) => _snapshot = snapshot;
}
