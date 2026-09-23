// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
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
    /// <returns>The session, or <see langword="null"/>.</returns>
    StoredSession? Load();

    /// <summary>Stores a session, replacing what was there.</summary>
    /// <param name="session">The session.</param>
    void Save(StoredSession session);
}

/// <summary>
/// What is kept of a session between runs: the engine's snapshot, and the history the application keeps itself.
/// </summary>
/// <param name="Snapshot">The memories, settings and data (<see cref="CalculatorSession.Capture"/>).</param>
/// <param name="History">The history of the application the session was in, oldest first.</param>
/// <remarks>
/// The history is not in the snapshot because the engine does not keep a history across runs; it is stored beside it
/// so that one string holds both and they cannot be read back from two different runs.
/// </remarks>
public sealed record StoredSession(SessionSnapshot Snapshot, ImmutableArray<HistoryEntry> History)
{
    /// <summary>Creates a stored session with no history.</summary>
    /// <param name="snapshot">The snapshot.</param>
    public StoredSession(SessionSnapshot snapshot)
        : this(snapshot, [])
    {
    }
}

/// <summary>
/// A store that keeps a session only while the application runs: the default, and what tests use.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private StoredSession? _session;

    /// <inheritdoc/>
    public StoredSession? Load() => _session;

    /// <inheritdoc/>
    public void Save(StoredSession session) => _session = session;
}
