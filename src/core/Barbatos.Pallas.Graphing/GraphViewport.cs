// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Graphing;

/// <summary>
/// The region of the plane a graph shows: x from <see cref="Left"/> to <see cref="Right"/>, y from
/// <see cref="Bottom"/> to <see cref="Top"/>.
/// </summary>
/// <remarks>
/// A viewport is a place on the screen, so its bounds are <see cref="double"/>: they position pixels, and no value the
/// calculator shows is read from them.
/// </remarks>
public sealed record GraphViewport
{
    /// <summary>Creates a viewport.</summary>
    /// <param name="left">The smallest x shown.</param>
    /// <param name="right">The largest x shown; greater than <paramref name="left"/>.</param>
    /// <param name="bottom">The smallest y shown.</param>
    /// <param name="top">The largest y shown; greater than <paramref name="bottom"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A bound is not finite, or the region is empty.</exception>
    public GraphViewport(double left, double right, double bottom, double top)
    {
        // Right and top need no check of their own: a size that is not a finite positive number is refused below.
        Require(left, nameof(left));
        Require(bottom, nameof(bottom));
        if (!(right - left > 0d) || !double.IsFinite(right - left))
        {
            throw new ArgumentOutOfRangeException(nameof(right), right, "The right of a viewport is to the right of its left.");
        }

        if (!(top - bottom > 0d) || !double.IsFinite(top - bottom))
        {
            throw new ArgumentOutOfRangeException(nameof(top), top, "The top of a viewport is above its bottom.");
        }

        Left = left;
        Right = right;
        Bottom = bottom;
        Top = top;
    }

    /// <summary>Gets the smallest x shown.</summary>
    public double Left { get; }

    /// <summary>Gets the largest x shown.</summary>
    public double Right { get; }

    /// <summary>Gets the smallest y shown.</summary>
    public double Bottom { get; }

    /// <summary>Gets the largest y shown.</summary>
    public double Top { get; }

    /// <summary>Gets the width of the region, in units of x.</summary>
    public double Width => Right - Left;

    /// <summary>Gets the height of the region, in units of y.</summary>
    public double Height => Top - Bottom;

    private static void Require(double bound, string name)
    {
        if (!double.IsFinite(bound))
        {
            throw new ArgumentOutOfRangeException(name, bound, "A bound of a viewport is a finite number.");
        }
    }
}
