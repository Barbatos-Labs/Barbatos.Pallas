// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Graphing;

/// <summary>A point of a curve worth naming: a root, an extremum, or where two curves meet.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="X">Its x, a value to 15 significant digits as every approximate value is.</param>
/// <param name="Y">The expression calculated by the engine at <paramref name="X"/>, displayed as a result is.</param>
public sealed record GraphFeature(GraphFeatureKind Kind, Value X, Calculation Y);

/// <summary>What a named point of a curve is.</summary>
public enum GraphFeatureKind
{
    /// <summary>The curve crosses the x axis.</summary>
    Root,

    /// <summary>The curve turns from falling to rising.</summary>
    Minimum,

    /// <summary>The curve turns from rising to falling.</summary>
    Maximum,

    /// <summary>Two curves cross.</summary>
    Intersection,
}
