// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A set of physical constants, such as CODATA 2022.
/// </summary>
public sealed class ConstantSet
{
    /// <summary>Initializes a set.</summary>
    /// <param name="name">The name of the set: <c>CODATA 2022</c>.</param>
    /// <param name="constants">The constants; each symbol at most once.</param>
    /// <exception cref="ArgumentException">A symbol appears twice.</exception>
    public ConstantSet(string name, IEnumerable<ScientificConstant> constants)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(constants);

        Name = name;
        Constants = [.. constants];
        BySymbol = Constants.ToFrozenDictionary(constant => constant.Symbol, StringComparer.Ordinal);
    }

    /// <summary>Gets the name of the set.</summary>
    public string Name { get; }

    /// <summary>Gets the constants.</summary>
    public ImmutableArray<ScientificConstant> Constants { get; }

    internal FrozenDictionary<string, ScientificConstant> BySymbol { get; }
}
