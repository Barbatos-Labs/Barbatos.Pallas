// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The atomic weights of the elements, such as the CIAAW standard atomic weights.
/// </summary>
public sealed class AtomicWeightTable
{
    /// <summary>Initializes a table.</summary>
    /// <param name="name">The name of the table.</param>
    /// <param name="elements">The elements; each atomic number at most once.</param>
    /// <exception cref="ArgumentException">An atomic number appears twice.</exception>
    public AtomicWeightTable(string name, IEnumerable<AtomicWeight> elements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(elements);

        Name = name;
        Elements = [.. elements];
        ByAtomicNumber = Elements.ToFrozenDictionary(element => element.AtomicNumber);
    }

    /// <summary>Gets the name of the table.</summary>
    public string Name { get; }

    /// <summary>Gets the elements.</summary>
    public ImmutableArray<AtomicWeight> Elements { get; }

    internal FrozenDictionary<int, AtomicWeight> ByAtomicNumber { get; }
}
