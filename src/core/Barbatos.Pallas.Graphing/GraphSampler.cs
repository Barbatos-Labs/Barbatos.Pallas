// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Graphing;

/// <summary>
/// Samples an expression in x across a viewport into the pieces of curve to draw.
/// </summary>
/// <remarks>
/// <para>
/// One sample per column of pixels is where it starts. A column whose middle falls more than half a pixel off the chord
/// between its ends is halved, down to a sixteenth of a pixel, so a curve is drawn as curved as the screen can show and
/// no more. Where the expression has no value - an error, or a complex number - the curve stops, and it is followed to
/// the edge of where it has one, to neighbouring numbers.
/// </para>
/// <para>
/// A curve is never drawn across a break. Between two points more than two pixels apart, the interval is halved
/// towards the larger step: a continuous curve's step shrinks with its interval, a jump's does not, and an asymptote's
/// grows. That is what tells Int(x) at 1 and tan x at 90° from a steep but unbroken x³.
/// </para>
/// <para>
/// Where the curve is above the top of the viewport or below its bottom, it is neither refined nor tested for breaks,
/// so that part costs one calculation per column. Measured on 24 Sep 2026: refining it too put x²−3x+1 in the square
/// of side 20 at 17.7 ms for 2,000 columns, above the 16 ms of a frame.
/// </para>
/// <para>
/// Every value comes from <see cref="CompiledExpression.TryEvaluate"/>, calculated by the engine as the session would;
/// the coordinates here only place it.
/// </para>
/// </remarks>
public static class GraphSampler
{
    /// <summary>The most columns a graph is sampled across: a screen wider than any there is.</summary>
    public const int MaximumColumns = 10_000;

    /// <summary>The most rows: likewise.</summary>
    public const int MaximumRows = 10_000;

    /// <summary>Samples an expression across a viewport.</summary>
    /// <param name="expression">The expression in x.</param>
    /// <param name="viewport">The region of the plane shown.</param>
    /// <param name="columns">The width of the drawing in pixels: one sample per column to start with.</param>
    /// <param name="rows">The height of the drawing in pixels, which says how far off a chord is too far.</param>
    /// <param name="cancellationToken">Cancels the sampling.</param>
    /// <returns>The pieces of the curve inside the viewport and where it breaks; none for an expression that did not compile.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="viewport"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is not 1 to 10,000.</exception>
    /// <exception cref="OperationCanceledException">The sampling was cancelled.</exception>
    public static GraphTrace Sample(CompiledExpression expression, GraphViewport viewport, int columns, int rows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, MaximumColumns);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(rows, MaximumRows);
        return new Sampler(expression, viewport, columns, rows, cancellationToken).Run();
    }

    /// <summary>One sampling of one curve.</summary>
    private sealed class Sampler
    {
        // A column is halved at most four times: a sixteenth of a pixel is finer than any screen draws.
        private const int MaxDepth = 4;

        // A value beyond ±10³⁰⁰ is drawn as if it were there, so that the difference of two never overflows; where the
        // curve crosses the edge of the viewport moves by less than 10⁻²⁸⁰ of the interval.
        private const double Reach = 1e300;

        private readonly CompiledExpression _expression;
        private readonly GraphViewport _viewport;
        private readonly int _columns;
        private readonly double _pixel;
        private readonly int _budget;
        private readonly CancellationToken _cancellationToken;
        private readonly List<GraphPoint> _run = [];
        private readonly List<ImmutableArray<GraphPoint>> _pieces = [];
        private readonly List<GraphBreak> _breaks = [];
        private int _evaluations;

        public Sampler(CompiledExpression expression, GraphViewport viewport, int columns, int rows, CancellationToken cancellationToken)
        {
            _expression = expression;
            _viewport = viewport;
            _columns = columns;
            _pixel = viewport.Height / rows;
            _cancellationToken = cancellationToken;

            // However wild the curve, a sampling stops refining after sixteen calculations per column, and stops
            // looking for breaks after thirty-two.
            _budget = 16 * columns;
        }

        public GraphTrace Run()
        {
            double previousX = _viewport.Left;
            double? previousY = Value(previousX);
            if (previousY is { } first)
            {
                Add(previousX, first);
            }

            for (int column = 1; column <= _columns; column++)
            {
                double x = _viewport.Left + (_viewport.Width * column / _columns);
                double? y = Value(x);
                Interval(previousX, previousY, x, y, 0);
                (previousX, previousY) = (x, y);
            }

            Close();
            return new GraphTrace(_viewport, [.. _pieces], [.. _breaks], _evaluations);
        }

        private double? Value(double x)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            _evaluations++;
            return _expression.TryEvaluate(x, out double y) ? y : null;
        }

        /// <summary>Draws the curve from just after <paramref name="a"/> to <paramref name="b"/>.</summary>
        private void Interval(double a, double? fa, double b, double? fb, int depth)
        {
            // Values read out rather than matched, so that every condition here can be mutation-tested: a pattern's
            // variable is unassigned in a mutant that changes the condition, and Stryker then tests nothing here.
            if (fa is null || fb is null)
            {
                if (fa is not null || fb is not null)
                {
                    Edge(a, fa, b, fb, depth);
                }

                return;
            }

            double ya = fa.Value;
            double yb = fb.Value;
            if (depth < MaxDepth && _evaluations < _budget && !Beyond(ya, yb))
            {
                double m = Middle(a, b);
                double? fm = Value(m);
                if (fm is null)
                {
                    Edge(a, ya, m, null, depth + 1);
                    Edge(m, null, b, yb, depth + 1);
                    return;
                }

                double ym = fm.Value;
                if (Math.Abs(ym - ((ya + yb) / 2d)) > _pixel / 2d)
                {
                    Interval(a, ya, m, ym, depth + 1);
                    Interval(m, ym, b, yb, depth + 1);
                    return;
                }

                Chord(a, ya, m, ym);
                Chord(m, ym, b, yb);
                return;
            }

            Chord(a, ya, b, yb);
        }

        /// <summary>Draws a straight line to <paramref name="b"/>, unless the curve breaks on the way there.</summary>
        private void Chord(double a, double ya, double b, double yb)
        {
            if (Math.Abs(yb - ya) > 2d * _pixel && !Beyond(ya, yb) && _evaluations < 2 * _budget && Continuity.Break(Value, a, ya, b, yb) is { } found)
            {
                Close();
                Found(found);
            }

            Add(b, yb);
        }

        /// <summary>
        /// Follows the curve to the edge of where the expression has a value, between a point with one and a point
        /// without, and draws it up to there. A curve whose value at the edge is ten times beyond both where it started
        /// and the height of the viewport meets an asymptote there: 1÷x at 0, ln x at 0; √x−0.15 does not.
        /// </summary>
        private void Edge(double a, double? fa, double b, double? fb, int depth)
        {
            bool fromLeft = fa is not null;
            double start = fromLeft ? a : b;
            double first = fromLeft ? fa!.Value : fb!.Value;
            (double inside, double value, double outside) = Continuity.Follow(Value, start, first, fromLeft ? b : a);
            if (Continuity.Kind(Math.Abs(value), Math.Max(Math.Abs(first), _viewport.Height)) == GraphBreakKind.Asymptote)
            {
                // Where the expression has no value is where the asymptote is: 1÷x at 0 itself, not beside it.
                Found(new GraphBreak(outside, GraphBreakKind.Asymptote));
            }

            if (fromLeft)
            {
                if (inside != start)
                {
                    Interval(a, fa, inside, value, depth);
                }

                Close();
                return;
            }

            // From the right, the curve starts at the edge; when the edge is b itself, that is all there is.
            Add(inside, value);
            if (inside != start)
            {
                Interval(inside, value, b, fb, depth);
            }
        }

        /// <summary>Names a break, once: either side of an asymptote at a sample finds it.</summary>
        private void Found(GraphBreak found)
        {
            if (_breaks.Count == 0 || _breaks[^1].Kind != found.Kind || found.X - _breaks[^1].X > _viewport.Width / _columns)
            {
                _breaks.Add(found);
            }
        }

        /// <summary>Whether two values are both above the top of the viewport, or both below its bottom.</summary>
        private bool Beyond(double ya, double yb) =>
            Math.Min(ya, yb) > _viewport.Top || Math.Max(ya, yb) < _viewport.Bottom;

        private bool Inside(double y) => y >= _viewport.Bottom && y <= _viewport.Top;

        private void Add(double x, double y) => _run.Add(new GraphPoint(x, Math.Clamp(y, -Reach, Reach)));

        /// <summary>Ends the run of points drawn so far, keeping what lies inside the viewport.</summary>
        private void Close()
        {
            List<GraphPoint> piece = [];
            for (int index = 1; index < _run.Count; index++)
            {
                if (!Clip(_run[index - 1], _run[index], out GraphPoint from, out GraphPoint to))
                {
                    continue;
                }

                if (piece.Count > 0 && piece[^1] != from)
                {
                    // The curve left the viewport and came back: that is two pieces.
                    Flush(piece);
                }

                if (piece.Count == 0)
                {
                    piece.Add(from);
                }

                piece.Add(to);
            }

            Flush(piece);
            _run.Clear();
        }

        private void Flush(List<GraphPoint> piece)
        {
            if (piece.Count >= 2)
            {
                _pieces.Add([.. piece]);
            }

            piece.Clear();
        }

        /// <summary>
        /// The part of a segment between the bottom and the top of the viewport. Its x is within the viewport already:
        /// the samples go from its left to its right.
        /// </summary>
        private bool Clip(GraphPoint p, GraphPoint q, out GraphPoint from, out GraphPoint to)
        {
            (from, to) = (p, q);
            if (Beyond(p.Y, q.Y))
            {
                // Both ends beyond the same edge, told by comparing them: next to a point near 10⁹⁹ the arithmetic
                // below would round where they cross to the end itself.
                return false;
            }

            double dy = q.Y - p.Y;
            if (dy == 0d)
            {
                return true;
            }

            // A rising segment enters the band between bottom and top through the bottom and leaves through the top; a
            // falling one the other way round. Where it crosses an edge, its y is the edge's own: worked out from a point
            // near 10⁹⁹, it would keep none of its digits. An end inside the band is kept as it is, and one outside it
            // is on the edge it crossed, whatever the arithmetic says: from −10³⁰⁰ up to −12.5, where the band is −37
            // to −16, both crossings are at 1 to the last digit, and the end at −12.5 was kept outside the viewport
            // (found by a property test, 24 Sep 2026).
            (double entry, double exit) = dy > 0d ? (_viewport.Bottom, _viewport.Top) : (_viewport.Top, _viewport.Bottom);
            from = Inside(p.Y) ? p : new GraphPoint(p.X + (Math.Clamp((entry - p.Y) / dy, 0d, 1d) * (q.X - p.X)), entry);
            to = Inside(q.Y) ? q : new GraphPoint(p.X + (Math.Clamp((exit - p.Y) / dy, 0d, 1d) * (q.X - p.X)), exit);

            // Not both beyond one edge, the segment runs from one side of the band to the other, or starts or ends in
            // it: either way it crosses it.
            return true;
        }

        private static double Middle(double a, double b) => a + ((b - a) / 2d);
    }
}
