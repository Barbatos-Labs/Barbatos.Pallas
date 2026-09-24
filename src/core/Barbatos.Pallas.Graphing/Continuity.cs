// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Graphing;

/// <summary>
/// Whether a function runs on between two points or breaks there, and how.
/// </summary>
/// <remarks>
/// <para>
/// The interval is halved towards its larger step. A continuous function's step shrinks with its interval - by half
/// for a smooth one, by a fifth for the cube root at 0 - and the first halving that shrinks it by a tenth or more
/// shows the function runs on. A jump's step stays what it is down to neighbouring numbers, and an asymptote's grows:
/// the values there end more than ten times as large as they started.
/// </para>
/// <para>
/// Where the function has no value between the two points - tan x at 90° - the side with the larger value is
/// followed to the edge of where it has one, and what the value came to says which kind of break it is.
/// </para>
/// <para>
/// The sampler draws no line across a break, and a change of sign across one is no root, no intersection, and, when
/// the slope grows without bound, no extremum. It is one test, so the curve and the points named on it agree.
/// </para>
/// </remarks>
internal static class Continuity
{
    /// <summary>Halves towards the larger step, and says where and how the function breaks, or that it does not.</summary>
    /// <param name="function">The function, with no value where it has none.</param>
    /// <param name="a">The left point.</param>
    /// <param name="ya">The value there.</param>
    /// <param name="b">The right point.</param>
    /// <param name="yb">The value there.</param>
    /// <returns>The break, or <see langword="null"/> when the function runs on between the two points.</returns>
    public static GraphBreak? Break(Func<double, double?> function, double a, double ya, double b, double yb)
    {
        double step = Math.Abs(yb - ya);
        double start = Math.Max(Math.Abs(ya), Math.Abs(yb));
        for (;;)
        {
            double m = a + ((b - a) / 2d);
            if (m <= a || m >= b)
            {
                return new GraphBreak(m, Kind(Math.Max(Math.Abs(ya), Math.Abs(yb)), start));
            }

            if (function(m) is not { } ym)
            {
                // No value between the two sides: follow the larger to the edge of where the function has one.
                (_, double value, double edge) = Math.Abs(ya) >= Math.Abs(yb) ? Follow(function, a, ya, m) : Follow(function, b, yb, m);
                return new GraphBreak(edge, Kind(Math.Abs(value), start));
            }

            double left = Math.Abs(ym - ya);
            double right = Math.Abs(yb - ym);
            (a, ya, b, yb) = left >= right ? (a, ya, m, ym) : (m, ym, b, yb);
            double next = Math.Max(left, right);
            if (next <= 0.9d * step)
            {
                return null;
            }

            step = next;
        }
    }

    /// <summary>
    /// Follows the function from a point with a value towards one without, halving, until the two are neighbouring
    /// numbers.
    /// </summary>
    /// <param name="function">The function, with no value where it has none.</param>
    /// <param name="inside">A point with a value.</param>
    /// <param name="value">The value there.</param>
    /// <param name="outside">A point with none, on either side of <paramref name="inside"/>.</param>
    /// <returns>The last point with a value, its value, and the first without: the edge.</returns>
    public static (double Inside, double Value, double Outside) Follow(Func<double, double?> function, double inside, double value, double outside)
    {
        for (;;)
        {
            double m = inside + ((outside - inside) / 2d);
            if (m == inside || m == outside)
            {
                return (inside, value, outside);
            }

            if (function(m) is { } y)
            {
                (inside, value) = (m, y);
            }
            else
            {
                outside = m;
            }
        }
    }

    /// <summary>An asymptote is where the values end more than ten times as large as they started; a jump is anything less.</summary>
    public static GraphBreakKind Kind(double magnitude, double start) =>
        magnitude > 10d * start ? GraphBreakKind.Asymptote : GraphBreakKind.Jump;
}
