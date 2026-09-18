// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A set of unit conversion commands, such as those of NIST SP 811.
/// </summary>
public sealed class UnitSet
{
    /// <summary>Initializes a set.</summary>
    /// <param name="name">The name of the set.</param>
    /// <param name="conversions">The conversions; each command at most once.</param>
    /// <exception cref="ArgumentException">A command appears twice.</exception>
    public UnitSet(string name, IEnumerable<UnitConversion> conversions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(conversions);

        Name = name;
        Conversions = [.. conversions];
        ByCommand = Conversions.ToFrozenDictionary(conversion => conversion.Command, StringComparer.Ordinal);
    }

    /// <summary>Gets the name of the set.</summary>
    public string Name { get; }

    /// <summary>Gets the conversions.</summary>
    public ImmutableArray<UnitConversion> Conversions { get; }

    internal FrozenDictionary<string, UnitConversion> ByCommand { get; }
}
