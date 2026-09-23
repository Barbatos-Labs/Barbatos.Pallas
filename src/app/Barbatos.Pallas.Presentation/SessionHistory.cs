// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One line of the history: what was typed, and what it came to.
/// </summary>
/// <param name="Input">The calculation as Canonical Linear Syntax, which is what recalling it puts back on the line.</param>
/// <param name="Text">What it came to, as the screen showed it; empty for a calculation that failed.</param>
/// <param name="Calculation">
/// The calculation itself, where it was made in this run; <see langword="null"/> for a line kept from an earlier run,
/// which is stored as its text.
/// </param>
public sealed record HistoryEntry(string Input, string Text, Calculation? Calculation = null)
{
    /// <summary>Returns the line of the history a calculation of this run is.</summary>
    /// <param name="calculation">The calculation.</param>
    /// <returns>Its line.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="calculation"/> is <see langword="null"/>.</exception>
    public static HistoryEntry Of(Calculation calculation)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        return new HistoryEntry(calculation.Input, calculation.Succeeded ? calculation.Display.Text : string.Empty, calculation);
    }
}

/// <summary>
/// The history of the session's application: the calculations of an earlier run, before those of this one.
/// </summary>
/// <remarks>
/// <para>
/// The engine keeps the history of this run and not of any other (<see cref="SessionSnapshot"/> says why), so an
/// application that keeps it across runs keeps the inputs and what they came to itself: this is that, and one of it
/// is shared by every screen of the session, as the history is the session's and not a screen's.
/// </para>
/// <para>
/// A line of an earlier run is its text, not a calculation: recalling it puts it back on the line to be calculated
/// again, because what it came to depended on memories and settings that may have changed since.
/// </para>
/// </remarks>
public sealed class SessionHistory
{
    /// <summary>The most lines kept for the next run. The calculator's own history is bounded by its memory; this
    /// bound is the application's, so that a stored session stays a small string in the preferences.</summary>
    public const int Kept = 200;

    private readonly CalculatorSession _session;
    private ImmutableArray<HistoryEntry> _earlier = [];

    /// <summary>Creates the history of a session.</summary>
    /// <param name="session">The session.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public SessionHistory(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    /// <summary>Gets every line, oldest first: those of an earlier run, then those of this one.</summary>
    public ImmutableArray<HistoryEntry> Entries => [.. _earlier, .. _session.History.Select(HistoryEntry.Of)];

    /// <summary>Gets the lines to keep for the next run: the newest <see cref="Kept"/>, oldest first.</summary>
    public ImmutableArray<HistoryEntry> ToKeep
    {
        get
        {
            ImmutableArray<HistoryEntry> entries = Entries;
            return entries.Length > Kept ? entries[^Kept..] : entries;
        }
    }

    /// <summary>Puts the lines of an earlier run before those of this one.</summary>
    /// <param name="entries">The lines, oldest first.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
    public void Restore(IEnumerable<HistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        _earlier = [.. entries];
    }

    /// <summary>Forgets the lines of an earlier run, as the engine forgets its own when the application changes (pp. 35, 37).</summary>
    public void Forget() => _earlier = [];
}
