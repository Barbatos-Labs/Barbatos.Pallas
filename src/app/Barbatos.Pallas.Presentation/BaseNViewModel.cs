// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Base-N screen (manual pp. 127-132): an ordinary line of the calculator, and which base it counts in.
/// </summary>
/// <remarks>
/// The base is a setting of the session, because it decides both what a bare number means and how a result is
/// written (p. 129); the screen is therefore the Calculate line with that one switch above it.
/// </remarks>
public sealed partial class BaseNViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that calculates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public BaseNViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Calculate = new CalculateViewModel(session);
    }

    /// <summary>Gets the bases there are: decimal, hexadecimal, binary and octal.</summary>
    public ImmutableArray<NumberBase> Bases { get; } = [.. Enum.GetValues<NumberBase>()];

    /// <summary>Gets the line calculations are typed on.</summary>
    public CalculateViewModel Calculate { get; }

    /// <summary>Gets or sets which base the calculator counts in.</summary>
    public NumberBase Mode
    {
        get => _session.Settings.BaseMode;
        set
        {
            if (_session.Settings.BaseMode == value)
            {
                return;
            }

            _session.Settings = _session.Settings with { BaseMode = value };
            OnPropertyChanged();
        }
    }
}
