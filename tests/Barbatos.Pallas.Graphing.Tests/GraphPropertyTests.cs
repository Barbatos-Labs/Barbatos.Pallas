// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using CsCheck;

namespace Barbatos.Pallas.Graphing.Tests;

/// <summary>
/// What holds for any curve in any viewport, not only for the curves whose shape a test knows.
/// </summary>
/// <remarks>
/// The curves are built from the functions a table of the calculator is made of - powers, roots, logarithms, the
/// trigonometric functions in degrees, Abs, Int and division, which bring poles, jumps and domains - over viewports of
/// any size and place. A property samples a few hundred of them: more makes Stryker's first run fail on time alone.
/// </remarks>
public sealed class GraphPropertyTests
{
    private const int Depth = 3;

    private static readonly Gen<string> Curve = Curves();

    private static readonly Gen<(GraphViewport View, int Columns, int Rows)> Views =
        Gen.Select(Gen.Int[-50, 50], Gen.Int[1, 100], Gen.Int[-50, 50], Gen.Int[1, 100], Gen.Int[20, 300], Gen.Int[20, 300],
            (left, width, bottom, height, columns, rows) => (new GraphViewport(left, left + width, bottom, bottom + height), columns, rows));

    [Fact]
    public void TheGeneratedCurvesAreCurves()
    {
        // A property of curves that are never drawn, or never cross the axis, would hold of nothing. Of 3,000 on
        // 24 Sep 2026, 44% were drawn in their viewport, 15% had a root in it and 5% a break.
        (string Input, (GraphViewport View, int Columns, int Rows) Sample)[] samples = Gen.Select(Curve, Views).Array[600].Single();
        int drawn = 0;
        int rooted = 0;
        int broken = 0;
        foreach ((string input, (GraphViewport view, int columns, int rows)) in samples)
        {
            CompiledExpression curve = GraphSamplerTests.Compile(input);
            GraphTrace trace = GraphSampler.Sample(curve, view, columns, rows);
            drawn += trace.Pieces.IsEmpty ? 0 : 1;
            broken += trace.Breaks.IsEmpty ? 0 : 1;
            rooted += GraphAnalysis.Roots(curve, view, columns).IsEmpty ? 0 : 1;
        }

        drawn.Should().BeGreaterThan(180);
        rooted.Should().BeGreaterThan(50);
        broken.Should().BeGreaterThan(10);
    }

    [Fact]
    public void AnyCurveIsDrawnInsideItsViewportWhereTheEngineSaysItIs()
    {
        Gen.Select(Curve, Views).Sample(
            (input, sample) =>
            {
                CompiledExpression curve = GraphSamplerTests.Compile(input);
                GraphViewport view = sample.View;
                GraphTrace trace = GraphSampler.Sample(curve, view, sample.Columns, sample.Rows);

                double previous = double.NegativeInfinity;
                foreach (IReadOnlyList<GraphPoint> piece in trace.Pieces)
                {
                    if (piece.Count < 2)
                    {
                        return false;
                    }

                    foreach (GraphPoint point in piece)
                    {
                        // Inside the viewport, left to right; and a point not on the top or the bottom edge, where a
                        // piece is clipped, is where the engine's value is.
                        bool inside = point.X >= view.Left && point.X <= view.Right && point.Y >= view.Bottom && point.Y <= view.Top;
                        bool onEdge = point.Y == view.Bottom || point.Y == view.Top;
                        if (!inside || point.X < previous || (!onEdge && !(curve.TryEvaluate(point.X, out double y) && y == point.Y)))
                        {
                            return false;
                        }

                        previous = point.X;
                    }
                }

                // However wild the curve, the work is bounded by the columns: sixteen calculations each to refine,
                // thirty-two to look for breaks, and the edges of where it has a value followed to neighbouring numbers.
                // Of 3,000 curves on 24 Sep 2026 the most was 34.4 a column, ln(x) across 0 in 20 columns.
                return trace.Evaluations <= 64 * (sample.Columns + 1)
                    && trace.Breaks.All(found => found.X >= view.Left && found.X <= view.Right);
            },
            iter: 300);
    }

    [Fact]
    public void AnyRootIsWhereTheCurveChangesSign()
    {
        Gen.Select(Curve, Views).Sample(
            (input, sample) =>
            {
                CompiledExpression curve = GraphSamplerTests.Compile(input);
                GraphViewport view = sample.View;
                foreach (GraphFeature root in GraphAnalysis.Roots(curve, view, sample.Columns))
                {
                    double x = root.X.ToDouble();
                    if (x < view.Left || x > view.Right || !root.Y.Succeeded || !ChangesSign(At(curve), x))
                    {
                        return false;
                    }
                }

                return true;
            },
            iter: 300);
    }

    [Fact]
    public void AnyIntersectionIsWhereTheCurvesCross()
    {
        Gen.Select(Curve, Curve, Views).Sample(
            (first, second, sample) =>
            {
                CompiledExpression f = GraphSamplerTests.Compile(first);
                CompiledExpression g = GraphSamplerTests.Compile(second);
                GraphViewport view = sample.View;
                foreach (GraphFeature meeting in GraphAnalysis.Intersections(f, g, view, sample.Columns))
                {
                    double x = meeting.X.ToDouble();
                    if (x < view.Left || x > view.Right || !meeting.Y.Succeeded
                        || !(ChangesSign(z => f.TryEvaluate(z, out double a) && g.TryEvaluate(z, out double b) ? a - b : null, x) || Agree(f, g, x)))
                    {
                        return false;
                    }
                }

                return true;
            },
            iter: 200);
    }

    [Fact]
    public void AnyExtremumIsInsideTheViewWithTheEnginesValueThere()
    {
        Gen.Select(Curve, Views).Sample(
            (input, sample) =>
            {
                CompiledExpression curve = GraphSamplerTests.Compile(input);
                GraphViewport view = sample.View;
                return GraphAnalysis.Extrema(curve, view, sample.Columns).All(turn =>
                    turn.X.ToDouble() >= view.Left && turn.X.ToDouble() <= view.Right && turn.Y.Succeeded
                    && turn.Y.Result == curve.Evaluate(turn.X).Result
                    && turn.Kind is GraphFeatureKind.Minimum or GraphFeatureKind.Maximum);
            },
            iter: 200);
    }

    /// <summary>
    /// Whether a function is 0 at x, or has another sign at a number next to x as the engine tells numbers apart: to
    /// fifteen significant digits, the resolution a root is placed to (the analysis halves a crossing down to two
    /// neighbouring numbers and names the one of them nearer zero).
    /// </summary>
    /// <remarks>
    /// A step of 10⁻¹³ relative - ten such numbers - was taken before. A curve that is zero but for the engine's last
    /// digits, ((π+x)−x)−π, changes sign several times within it, so a root placed exactly where its sign changes
    /// failed the property in two runs of the suite (25 Sep 2026), and a root misplaced by a few numbers passed it.
    /// </remarks>
    private static bool ChangesSign(Func<double, double?> function, double x)
    {
        if (function(x) is not { } at)
        {
            return false;
        }

        if (at == 0d)
        {
            return true;
        }

        double step = Math.Pow(10d, Math.Floor(Math.Log10(Math.Abs(x))) - 14d);
        return (function(x - step) is { } before && Math.Sign(before) != Math.Sign(at))
            || (function(x + step) is { } after && Math.Sign(after) != Math.Sign(at));
    }

    private static Gen<string> Curves()
    {
        Gen<string> leaf = Gen.Frequency((5, Gen.Const("x")), (5, Gen.OneOfConst("1", "2", "3", "0.5", "10", "π")));
        Gen<string> tree = leaf;
        for (int depth = 1; depth <= Depth; depth++)
        {
            Gen<string> inner = tree;
            Gen<string> unary = Gen.Select(Gen.OneOfConst("sin(", "cos(", "tan(", "√(", "ln(", "Abs(", "Int(", "-("), inner, (name, argument) => name + argument + ")");
            Gen<string> binary = Gen.Select(inner, Gen.OneOfConst("+", "−", "×", "÷"), inner, (left, operation, right) => "(" + left + ")" + operation + "(" + right + ")");
            Gen<string> power = Gen.Select(inner, Gen.Int[0, 3], (bottom, exponent) => "(" + bottom + ")^" + exponent.ToString(System.Globalization.CultureInfo.InvariantCulture));
            tree = Gen.Frequency((2, leaf), (3, unary), (3, binary), (1, power));
        }

        return tree;
    }

    // Two values held to fifteen digits each agree where their difference is within a unit of the fifteenth digit of the
    // larger: ln(1−x)+x·Abs(x) and tan(1°)³−x are both -0.00326 near their meeting, and differ by 5×10⁻¹⁸ at it and
    // either side of it, which is no sign the engine can tell.
    private static bool Agree(CompiledExpression first, CompiledExpression second, double x) =>
        first.TryEvaluate(x, out double a) && second.TryEvaluate(x, out double b) && Math.Abs(a - b) <= 1e-14 * Math.Max(Math.Abs(a), Math.Abs(b));

    private static Func<double, double?> At(CompiledExpression curve) => x => curve.TryEvaluate(x, out double y) ? y : null;
}
