// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Graphing;

/// <summary>
/// Finds the roots, the extrema and the intersections of curves across the x of a viewport.
/// </summary>
/// <remarks>
/// <para>
/// Each is found where something changes sign between two samples: the expression for a root, its derivative for an
/// extremum, the difference of two expressions for an intersection. The interval is then halved on the engine's own
/// values until its ends are neighbouring numbers, and the x found becomes a value as any <see cref="double"/> does,
/// to fifteen significant digits: so a root at 1 is 1, not 0.999999999999999889. The y named is the expression
/// calculated by the engine at that x, displayed as a result is.
/// </para>
/// <para>
/// A change of sign is not always a root. Across a pole or a jump - 1÷x at 0, Intg(x)−0.5 at 1 - the curve breaks,
/// as the sampler sees it breaks, and nothing is named there. An extremum is found on the derivative, which the
/// engine takes exactly (<see cref="CompiledExpression.Derivative"/>), because a search along a flat curve can only
/// place its x to about half the digits of its y. A kink is an extremum too: |x| at 0, where the slope jumps from −1
/// to 1. A slope that grows without bound is a pole, not a turn.
/// </para>
/// <para>
/// What changes no sign between two samples is not found: a root where the curve only touches the axis is an extremum,
/// and two roots closer together than a column are one change of sign or none. More columns see more.
/// </para>
/// </remarks>
public static class GraphAnalysis
{
    /// <summary>Finds where an expression crosses the x axis.</summary>
    /// <param name="expression">The expression in x.</param>
    /// <param name="viewport">Whose x is searched; its y plays no part.</param>
    /// <param name="columns">How many intervals the x is sampled in.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>The roots, left to right.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> is not 1 to 10,000.</exception>
    /// <exception cref="OperationCanceledException">The search was cancelled.</exception>
    public static ImmutableArray<GraphFeature> Roots(CompiledExpression expression, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Require(viewport, columns);
        Searcher search = new(x => expression.TryEvaluate(x, out double y) ? y : null, expression.ValueOf, cancellationToken);
        List<GraphFeature> found = [];
        foreach (Crossing crossing in search.Crossings(viewport, columns))
        {
            if (search.Unbroken(crossing) && Feature(GraphFeatureKind.Root, expression, search.Settle(crossing), cancellationToken) is { } root)
            {
                found.Add(root);
            }
        }

        return [.. found];
    }

    /// <summary>Finds where an expression turns: its minima and maxima.</summary>
    /// <param name="expression">The expression in x.</param>
    /// <param name="viewport">Whose x is searched; its y plays no part.</param>
    /// <param name="columns">How many intervals the x is sampled in.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>The extrema, left to right; none when the expression cannot be differentiated.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> is not 1 to 10,000.</exception>
    /// <exception cref="OperationCanceledException">The search was cancelled.</exception>
    public static ImmutableArray<GraphFeature> Extrema(CompiledExpression expression, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Require(viewport, columns);
        CompiledExpression derivative = expression.Derivative();
        Searcher search = new(x => derivative.TryEvaluate(x, out double slope) ? slope : null, expression.ValueOf, cancellationToken);
        List<GraphFeature> found = [];
        foreach (Crossing crossing in search.Crossings(viewport, columns))
        {
            // Falling then rising is a minimum. A slope that is zero where the curve does not fall on one side and
            // rise on the other is no turn, and neither is one that grows without bound: that is a pole.
            if (crossing.Before * crossing.After >= 0 || search.Pole(crossing))
            {
                continue;
            }

            GraphFeatureKind kind = crossing.Before < 0 ? GraphFeatureKind.Minimum : GraphFeatureKind.Maximum;
            if (Feature(kind, expression, search.Settle(crossing), cancellationToken) is { } turn && Turns(expression, crossing, turn))
            {
                found.Add(turn);
            }
        }

        return [.. found];
    }

    /// <summary>Finds where two expressions meet.</summary>
    /// <param name="first">The first expression in x; the y named is its value.</param>
    /// <param name="second">The second.</param>
    /// <param name="viewport">Whose x is searched; its y plays no part.</param>
    /// <param name="columns">How many intervals the x is sampled in.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    /// <returns>The intersections, left to right.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> is not 1 to 10,000.</exception>
    /// <exception cref="OperationCanceledException">The search was cancelled.</exception>
    /// <remarks>
    /// Which is the greater at each x is decided by the sign of the difference of the engine's two values. A decimal of
    /// fifteen digits converts to the double nearest it in order, and the sign of a difference of two doubles is exact,
    /// so that sign is the engine's own comparison, whatever the difference rounds to.
    /// </remarks>
    public static ImmutableArray<GraphFeature> Intersections(CompiledExpression first, CompiledExpression second, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        Require(viewport, columns);
        Searcher search = new(x => first.TryEvaluate(x, out double a) && second.TryEvaluate(x, out double b) ? a - b : null, first.ValueOf, cancellationToken);
        List<GraphFeature> found = [];
        foreach (Crossing crossing in search.Crossings(viewport, columns))
        {
            if (search.Unbroken(crossing) && Feature(GraphFeatureKind.Intersection, first, search.Settle(crossing), cancellationToken) is { } meeting)
            {
                found.Add(meeting);
            }
        }

        return [.. found];
    }

    private static void Require(GraphViewport viewport, int columns)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, GraphSampler.MaximumColumns);
    }

    /// <summary>The named point at x, with the engine's calculation there; nothing where the expression has no value.</summary>
    private static GraphFeature? Feature(GraphFeatureKind kind, CompiledExpression expression, double x, CancellationToken cancellationToken)
    {
        if (expression.ValueOf(x) is not { } at)
        {
            return null;
        }

        Calculation y = expression.Evaluate(at, cancellationToken);
        return y.Succeeded ? new GraphFeature(kind, at, y) : null;
    }

    /// <summary>Whether the curve is as high at a maximum, or as low at a minimum, as at the samples either side.</summary>
    /// <remarks>
    /// The slope of Abs(x−1)+5Intg(x) turns from falling to rising at 1, where the curve jumps up: the lowest point is
    /// just before 1, where there is none to name.
    /// </remarks>
    private static bool Turns(CompiledExpression expression, Crossing crossing, GraphFeature turn)
    {
        double y = turn.Y.Result.ToDouble();
        int direction = turn.Kind == GraphFeatureKind.Maximum ? 1 : -1;
        return Beyond(expression, crossing.A, y, direction) && Beyond(expression, crossing.B, y, direction);
    }

    // The turn is as high as a maximum, or as low as a minimum, as the curve is at x; a curve with no value there
    // says nothing against it.
    private static bool Beyond(CompiledExpression expression, double x, double y, int direction) =>
        !expression.TryEvaluate(x, out double there) || (direction * (y - there)) >= 0d;

    /// <summary>
    /// An interval in which something changes sign, or a sample at which it is zero: then <see cref="A"/> and
    /// <see cref="B"/> are that sample.
    /// </summary>
    private readonly record struct Crossing(double A, double ValueA, double B, double ValueB, int Before, int After);

    /// <summary>A search of one function of x for where it changes sign.</summary>
    /// <param name="function">The function searched, with no value where it has none.</param>
    /// <param name="enter">The value an x becomes, as the expressions take it (<see cref="CompiledExpression.ValueOf"/>).</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    private sealed class Searcher(Func<double, double?> function, Func<double, Value?> enter, CancellationToken cancellationToken)
    {
        public double? At(double x)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return function(x);
        }

        /// <summary>The changes of sign between the samples of a viewport's x, left to right.</summary>
        public List<Crossing> Crossings(GraphViewport viewport, int columns)
        {
            // Whether each sample has a value, and the value, held apart rather than matched as double?, so that every
            // condition here can be mutation-tested: a pattern's variable is unassigned in a mutant of its condition.
            double[] xs = new double[columns + 1];
            double[] ys = new double[columns + 1];
            bool[] known = new bool[columns + 1];
            for (int column = 0; column <= columns; column++)
            {
                xs[column] = viewport.Left + (viewport.Width * column / columns);
                double? y = At(xs[column]);
                known[column] = y.HasValue;
                ys[column] = y.GetValueOrDefault();
            }

            List<Crossing> crossings = [];
            for (int column = 0; column <= columns; column++)
            {
                if (!known[column])
                {
                    // No value at a sample between two of opposite signs: the slope of |x| at 0, a pole, a hole. The
                    // interval over it is a change of sign, and what it is gets decided like any other.
                    if (column > 0 && column < columns && known[column - 1] && known[column + 1]
                        && Math.Sign(ys[column - 1]) * Math.Sign(ys[column + 1]) < 0)
                    {
                        crossings.Add(new Crossing(xs[column - 1], ys[column - 1], xs[column + 1], ys[column + 1], Math.Sign(ys[column - 1]), Math.Sign(ys[column + 1])));
                    }

                    continue;
                }

                double value = ys[column];
                if (value == 0d)
                {
                    // Zero at a sample: the signs either side say whether the curve crosses there or only touches.
                    int before = column > 0 && known[column - 1] ? Math.Sign(ys[column - 1]) : 0;
                    int after = column < columns && known[column + 1] ? Math.Sign(ys[column + 1]) : 0;
                    crossings.Add(new Crossing(xs[column], 0d, xs[column], 0d, before, after));
                }
                else if (column < columns && known[column + 1] && Math.Sign(ys[column + 1]) == -Math.Sign(value))
                {
                    crossings.Add(new Crossing(xs[column], value, xs[column + 1], ys[column + 1], Math.Sign(value), Math.Sign(ys[column + 1])));
                }
            }

            return crossings;
        }

        /// <summary>Whether the function runs on across the crossing, as a root or an intersection needs.</summary>
        public bool Unbroken(Crossing crossing) =>
            crossing.A == crossing.B || Continuity.Break(At, crossing.A, crossing.ValueA, crossing.B, crossing.ValueB) is null;

        /// <summary>Whether the function grows without bound across the crossing.</summary>
        public bool Pole(Crossing crossing) =>
            crossing.A != crossing.B && Continuity.Break(At, crossing.A, crossing.ValueA, crossing.B, crossing.ValueB) is { Kind: GraphBreakKind.Asymptote };

        /// <summary>
        /// Halves a crossing until its ends are neighbouring numbers, or its middle is a zero or a point with no value:
        /// the corner of |x|, whose slope has none there.
        /// </summary>
        public double Settle(Crossing crossing)
        {
            (double a, double fa, double b) = (crossing.A, crossing.ValueA, crossing.B);
            for (;;)
            {
                double m = a + ((b - a) / 2d);
                if (m <= a || m >= b)
                {
                    // Neighbours either side of 0 are 0 and the smallest double: halving between them lands on 0.
                    return Nearer(a, b);
                }

                if (At(m) is not { } fm || fm == 0d)
                {
                    return m;
                }

                (a, fa, b) = Math.Sign(fm) == Math.Sign(fa) ? (m, fm, b) : (a, fa, m);
            }
        }

        /// <summary>
        /// Of the values to fifteen digits that two neighbouring numbers become, the one where the function is nearer
        /// zero; where it is as near at both, the one further from zero, as the display rounds a midpoint away from it.
        /// </summary>
        /// <remarks>
        /// x²−2 changes sign between 1.4142135623730949 and 1.4142135623730951, which become 1.41421356237309 and
        /// 1.41421356237310. The engine holds x² to fifteen digits, so x²−2 is −10⁻¹⁴ at the one and 10⁻¹⁴ at the
        /// other: the root is as near to both as the engine can tell, and √2 is 1.41421356237310 to fifteen digits.
        /// </remarks>
        private double Nearer(double a, double b)
        {
            if (enter(a) is not { } lower || enter(b) is not { } upper)
            {
                return a;
            }

            double left = lower.ToDouble();
            double right = upper.ToDouble();
            if (At(left) is not { } atLeft || At(right) is not { } atRight)
            {
                return left;
            }

            int nearer = Math.Abs(atRight).CompareTo(Math.Abs(atLeft));
            return nearer < 0 || (nearer == 0 && Math.Abs(right) > Math.Abs(left)) ? right : left;
        }
    }
}
