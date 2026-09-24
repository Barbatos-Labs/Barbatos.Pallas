// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Graphing;

/// <summary>A curve as it is drawn in a viewport: the pieces of it there are, and where it breaks.</summary>
/// <remarks>
/// A piece is a polyline to draw as it is. It lies inside the viewport - where the curve leaves it, the piece ends on
/// its edge - so the host draws the points it is given and clips nothing, and a curve that runs off to 10¹⁰⁰ does not
/// reach the drawing as a coordinate it cannot hold.
/// </remarks>
public sealed class GraphTrace
{
    internal GraphTrace(GraphViewport viewport, ImmutableArray<ImmutableArray<GraphPoint>> pieces, ImmutableArray<GraphBreak> breaks, int evaluations)
    {
        Viewport = viewport;
        Pieces = pieces;
        Breaks = breaks;
        Evaluations = evaluations;
    }

    /// <summary>Gets the viewport the curve was sampled in.</summary>
    public GraphViewport Viewport { get; }

    /// <summary>Gets the pieces of the curve inside the viewport, left to right, each with two points or more.</summary>
    public ImmutableArray<ImmutableArray<GraphPoint>> Pieces { get; }

    /// <summary>Gets where the curve breaks within the viewport's x, left to right.</summary>
    public ImmutableArray<GraphBreak> Breaks { get; }

    /// <summary>Gets how many times the expression was calculated.</summary>
    public int Evaluations { get; }
}
