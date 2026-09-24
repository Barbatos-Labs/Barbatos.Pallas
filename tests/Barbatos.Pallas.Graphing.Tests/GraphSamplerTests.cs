// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Graphing.Tests;

/// <summary>
/// Curves whose shape is known, sampled as a screen of 400 by 400 pixels would sample them.
/// </summary>
public sealed class GraphSamplerTests
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().Build();

    internal static CompiledExpression Compile(
        string input,
        AngleUnit unit = AngleUnit.Degree,
        CalculatorApp app = CalculatorApp.Table,
        CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Engine.CreateSession(app, profile);
        session.Settings = session.Settings with { AngleUnit = unit };
        return session.Compile(input);
    }

    private static GraphTrace Sample(string input, GraphViewport viewport, AngleUnit unit = AngleUnit.Degree) =>
        GraphSampler.Sample(Compile(input, unit), viewport, 400, 400);

    private static readonly GraphViewport Square = new(-10d, 10d, -10d, 10d);

    [Fact]
    public void ALineIsOnePieceFromCornerToCorner()
    {
        GraphTrace line = Sample("x", Square);

        line.Pieces.Should().ContainSingle();
        line.Pieces[0][0].Should().Be(new GraphPoint(-10d, -10d));
        line.Pieces[0][^1].Should().Be(new GraphPoint(10d, 10d));
        line.Breaks.Should().BeEmpty();
        line.Viewport.Should().Be(Square);
    }

    [Fact]
    public void ACurveEndsWhereItLeavesTheViewport()
    {
        // x² leaves the square at x = ±√10, through its top.
        GraphTrace parabola = Sample("x^2", Square);

        parabola.Pieces.Should().ContainSingle();
        ImmutableArray<GraphPoint> piece = parabola.Pieces[0];
        piece[0].Y.Should().Be(10d);
        piece[^1].Y.Should().Be(10d);
        piece[0].X.Should().BeApproximately(-Math.Sqrt(10d), 1e-3);
        piece[^1].X.Should().BeApproximately(Math.Sqrt(10d), 1e-3);
        parabola.Pieces.SelectMany(p => p).Should().OnlyContain(point => Inside(point, Square));
    }

    [Fact]
    public void ACurveIsDrawnAsCurvedAsTheScreenShows()
    {
        // No chord is more than half a pixel from the curve: the middle of each is checked against x².
        GraphTrace parabola = Sample("x^2", new GraphViewport(-2d, 2d, -1d, 4d));
        double pixel = 5d / 400d;

        foreach (ImmutableArray<GraphPoint> piece in parabola.Pieces)
        {
            for (int index = 1; index < piece.Length; index++)
            {
                double middle = (piece[index - 1].X + piece[index].X) / 2d;
                double chord = (piece[index - 1].Y + piece[index].Y) / 2d;
                Math.Abs((middle * middle) - chord).Should().BeLessThan(pixel, "a chord stays within a pixel of x² at {0}", middle);
            }
        }
    }

    [Fact]
    public void AnAsymptoteBreaksTheCurve()
    {
        // tan x in degrees has asymptotes at 90 and 270.
        GraphTrace tangent = Sample("tan(x)", new GraphViewport(0d, 360d, -10d, 10d));

        tangent.Breaks.Select(found => found.Kind).Should().Equal(GraphBreakKind.Asymptote, GraphBreakKind.Asymptote);
        tangent.Breaks[0].X.Should().BeApproximately(90d, 1e-6);
        tangent.Breaks[1].X.Should().BeApproximately(270d, 1e-6);
        tangent.Pieces.Should().HaveCount(3);
        tangent.Pieces.SelectMany(p => p).Should().OnlyContain(point => point.Y >= -10d && point.Y <= 10d);
    }

    [Fact]
    public void AnAsymptoteInTheMiddleOfAColumnIsFound()
    {
        // Ninety columns of 4°: 90 and 270 are no samples but the middles of the columns around them, where tan x has
        // no value.
        GraphTrace tangent = GraphSampler.Sample(Compile("tan(x)"), new GraphViewport(0d, 360d, -10d, 10d), 90, 400);

        tangent.Breaks.Select(found => found.Kind).Should().Equal(GraphBreakKind.Asymptote, GraphBreakKind.Asymptote);
        tangent.Breaks[0].X.Should().BeApproximately(90d, 1e-9);
        tangent.Breaks[1].X.Should().BeApproximately(270d, 1e-9);
        tangent.Pieces.Should().HaveCount(3);
    }

    [Fact]
    public void ACurveIsDrawnUpToAHoleInTheMiddleOfAColumnFromBothSides()
    {
        // |x−90|÷(x−90) is −1 and 1 either side of 90, the middle of the column from 88 to 92, where it has no value.
        GraphTrace sign = GraphSampler.Sample(Compile("Abs(x−90)÷(x−90)"), new GraphViewport(0d, 360d, -2d, 2d), 90, 400);

        sign.Pieces.Should().HaveCount(2);
        sign.Pieces[0][^1].Should().Match<GraphPoint>(point => Math.Abs(point.X - 90d) < 1e-9 && point.Y == -1d);
        sign.Pieces[1][0].Should().Match<GraphPoint>(point => Math.Abs(point.X - 90d) < 1e-9 && point.Y == 1d);
    }

    [Fact]
    public void AColumnIsHalvedUntilItsChordsFollowTheCurve()
    {
        // Ten columns of 0.4 across 400 rows: an unhalved chord of x² would be three pixels off the curve.
        GraphTrace parabola = GraphSampler.Sample(Compile("x^2"), new GraphViewport(-2d, 2d, -1d, 4d), 10, 400);
        double pixel = 5d / 400d;

        foreach (ImmutableArray<GraphPoint> piece in parabola.Pieces)
        {
            for (int index = 1; index < piece.Length; index++)
            {
                double middle = (piece[index - 1].X + piece[index].X) / 2d;
                double chord = (piece[index - 1].Y + piece[index].Y) / 2d;
                // An interval is halved until its middle is within half a pixel of its chord, and drawn as the two chords
                // either side of that middle; for a parabola each of those is a quarter as far off as the one it halves.
                Math.Abs((middle * middle) - chord).Should().BeLessThanOrEqualTo(pixel / 8d, "a chord of x² at {0} is an eighth of a pixel from it at most", middle);
            }
        }
    }

    [Fact]
    public void AnAsymptoteInRadiansIsFoundBetweenSamples()
    {
        // π/2 is no sample, so the break is found by halving towards it.
        GraphTrace tangent = Sample("tan(x)", new GraphViewport(0d, 3d, -10d, 10d), AngleUnit.Radian);

        tangent.Breaks.Should().ContainSingle().Which.Should().Match<GraphBreak>(found => found.Kind == GraphBreakKind.Asymptote);
        tangent.Breaks[0].X.Should().BeApproximately(Math.PI / 2d, 1e-9);
    }

    [Fact]
    public void AnAsymptoteAtASampleIsNamedOnce()
    {
        // 0 is a sample of this viewport, and 1÷0 is a Math ERROR: the curve stops either side of it.
        GraphTrace reciprocal = Sample("1÷x", Square);

        reciprocal.Breaks.Should().ContainSingle().Which.Should().Be(reciprocal.Breaks[0] with { Kind = GraphBreakKind.Asymptote });
        reciprocal.Breaks[0].X.Should().BeApproximately(0d, 1e-3);
        reciprocal.Pieces.Should().HaveCount(2);
        reciprocal.Pieces[0].Should().OnlyContain(point => point.X < 0d);
        reciprocal.Pieces[1].Should().OnlyContain(point => point.X > 0d);
    }

    [Fact]
    public void AnAsymptoteAtTheEdgeOfADomainIsNamed()
    {
        // ln x has no value left of 0 and falls without bound towards it, if slowly: followed to the edge, it is −228
        // at 10⁻⁹⁹, where the calculator's range ends, more than ten heights of this viewport below it.
        GraphTrace logarithm = Sample("ln(x)", new GraphViewport(-3d, 3d, -5d, 5d));

        logarithm.Breaks.Should().ContainSingle().Which.Kind.Should().Be(GraphBreakKind.Asymptote);
        logarithm.Breaks[0].X.Should().BeApproximately(0d, 1e-98);
        logarithm.Pieces.Should().ContainSingle();
    }

    [Theory]
    [InlineData("Intg(x)", new[] { -2d, -1d, 0d, 1d, 2d })]
    [InlineData("Int(x)", new[] { -2d, -1d, 1d, 2d })]
    public void AJumpBreaksTheCurveWithoutAnAsymptote(string input, double[] jumps)
    {
        // Intg is the largest integer not above x; Int is the integer part, truncated, which has no step at 0
        // (docs/CALCULATOR-CATALOG.md, Numeric Calc).
        GraphTrace steps = Sample(input, new GraphViewport(-2.5d, 2.5d, -3d, 3d));

        steps.Breaks.Select(found => found.Kind).Should().OnlyContain(kind => kind == GraphBreakKind.Jump);
        steps.Breaks.Select(found => Math.Round(found.X)).Should().Equal(jumps);
        steps.Breaks.Should().OnlyContain(found => Math.Abs(found.X - Math.Round(found.X)) < 1e-9);
        steps.Pieces.Should().HaveCount(jumps.Length + 1);
        steps.Pieces.Should().OnlyContain(piece => piece.All(point => point.Y == piece[0].Y), "each step is flat");
    }

    [Fact]
    public void ASteepCurveIsNotABreak()
    {
        // The cube root is vertical at 0, and unbroken.
        GraphTrace root = Sample("x^(1÷3)", Square);

        root.Breaks.Should().BeEmpty();
        root.Pieces.Should().ContainSingle();
    }

    [Fact]
    public void ACurveStopsWhereItHasNoValue()
    {
        // √x has none left of 0: the curve starts there.
        GraphTrace root = Sample("√(x)", Square);

        root.Pieces.Should().ContainSingle();
        root.Pieces[0][0].X.Should().BeApproximately(0d, 1e-98, "the curve is followed to its edge");
        root.Breaks.Should().BeEmpty("the edge of a domain is no asymptote");
    }

    [Fact]
    public void ACurveEndsWhereItHasNoValue()
    {
        GraphTrace root = Sample("√(−x)", Square);

        root.Pieces.Should().ContainSingle();
        root.Pieces[0][^1].X.Should().BeApproximately(0d, 1e-98, "the curve is followed to its edge, which a range that ends at 10^-99 puts there");
        root.Breaks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("√(x)", 1)]
    [InlineData("√(−x)", -1)]
    public void ACurveIsFollowedToItsEdgeBetweenTwoSamples(string input, int side)
    {
        // 0 is no sample here: the samples either side of it are ±0.025.
        GraphTrace root = Sample(input, new GraphViewport(-10.025d, 9.975d, -10d, 10d));

        root.Pieces.Should().ContainSingle();
        GraphPoint edge = side > 0 ? root.Pieces[0][0] : root.Pieces[0][^1];
        edge.X.Should().BeApproximately(0d, 1e-98);
        edge.Y.Should().BeApproximately(0d, 1e-49);
        root.Pieces[0].Should().Contain(point => Math.Abs(point.X - (0.025d * side)) < 1e-12, "the sample beside the edge is drawn too");
        root.Pieces[0].Zip(root.Pieces[0].Skip(1)).Should().OnlyContain(pair => pair.First != pair.Second, "no point is drawn twice");
        root.Breaks.Should().BeEmpty();
    }

    [Fact]
    public void AValueThatOnlyShrinksToAnEdgeIsNoAsymptote()
    {
        // √x−0.15 goes from about 0.008 to −0.15 on the way to its edge at 0: many times what it was, and bounded.
        Sample("√(x)−0.15", new GraphViewport(-1.025d, 0.975d, -1d, 1d)).Breaks.Should().BeEmpty();

        // From exactly 0 at x = 0.0225, the sample beside the edge, to −0.15: bounded all the same, which the height
        // of the viewport says when the value it started from cannot.
        GraphSampler.Sample(Compile("√(x)−0.15"), new GraphViewport(-1.0025d, 0.9975d, -1d, 1d), 80, 80).Breaks.Should().BeEmpty();
    }

    [Fact]
    public void AnAsymptoteOnOneSideIsAnAsymptote()
    {
        // Nothing left of π, and 1÷(x−π) right of it: the values grow on one side only, and that is enough.
        GraphTrace half = Sample("(Abs(x−π)+(x−π))÷(2(x−π)^2)", new GraphViewport(2d, 4d, -10d, 10d), AngleUnit.Radian);

        half.Breaks.Should().ContainSingle().Which.Kind.Should().Be(GraphBreakKind.Asymptote);
        half.Breaks[0].X.Should().BeApproximately(Math.PI, 1e-9);
    }

    [Fact]
    public void ACurveWithTwoPartsIsTwoPieces()
    {
        // √(x²−1) has no value between −1 and 1.
        GraphTrace root = Sample("√(x^2−1)", Square);

        root.Pieces.Should().HaveCount(2);
        root.Pieces[0][^1].X.Should().BeApproximately(-1d, 1e-12);
        root.Pieces[1][0].X.Should().BeApproximately(1d, 1e-12);
        root.Breaks.Should().BeEmpty();
    }

    [Theory]
    [InlineData("tan(x)", GraphBreakKind.Asymptote)]
    [InlineData("Abs(x−90)÷(x−90)", GraphBreakKind.Jump)]
    [InlineData("(Abs(x−90)+(x−90))÷(2(x−90)^2)", GraphBreakKind.Asymptote)]
    [InlineData("(Abs(x−90)−(x−90))÷(2(x−90)^2)", GraphBreakKind.Asymptote)]
    public void ABreakWithoutAValueIsFollowedToSayWhichKind(string input, GraphBreakKind kind)
    {
        // One column from 89.875 to 93.875, halved four times: 90 is the middle of the halving that follows, and has
        // no value. What the curve comes to on the way to it says whether it is an asymptote or a jump.
        GraphTrace trace = GraphSampler.Sample(Compile(input), new GraphViewport(89.875d, 93.875d, -10d, 10d), 1, 100);

        trace.Breaks.Should().ContainSingle().Which.Kind.Should().Be(kind);
        trace.Breaks[0].X.Should().BeApproximately(90d, 1e-9);
    }

    [Theory]
    [InlineData("Intg(x)÷10", -0.3d, 0.3d)]
    [InlineData("Intg(x)+100", 97d, 103d)]
    public void AJumpOfAnySizeIsAJump(string input, double bottom, double top)
    {
        // A step of a tenth, and steps around 100: the size of the values plays no part.
        GraphTrace steps = Sample(input, new GraphViewport(-2.5d, 2.5d, bottom, top));

        steps.Breaks.Should().HaveCount(5).And.OnlyContain(found => found.Kind == GraphBreakKind.Jump);
    }

    [Fact]
    public void AStraightCrossingIsOneSegment()
    {
        // 1000x crosses this viewport between x = 0.001 and 0.005, inside one chord.
        GraphTrace line = Sample("1000x", new GraphViewport(-10.025d, 9.975d, 1d, 5d));

        line.Pieces.Should().ContainSingle();
        line.Pieces[0].Should().HaveCount(2);
        line.Pieces[0][0].X.Should().BeApproximately(0.001d, 1e-12);
        line.Pieces[0][0].Y.Should().Be(1d);
        line.Pieces[0][1].X.Should().BeApproximately(0.005d, 1e-12);
        line.Pieces[0][1].Y.Should().Be(5d);
    }

    [Fact]
    public void ACurveOffTheViewportIsNotDrawnAndCostsOneCalculationAColumn()
    {
        foreach (string input in (string[])["x^2+100", "−x^2−100"])
        {
            GraphTrace away = Sample(input, Square);
            away.Pieces.Should().BeEmpty();
            away.Evaluations.Should().Be(401, "{0} is neither refined nor tested for breaks beyond the viewport", input);
        }
    }

    [Fact]
    public void ACurveThatLeavesAndComesBackIsTwoPieces()
    {
        GraphTrace wave = Sample("20sin(x)", new GraphViewport(0d, 360d, -10d, 10d));

        wave.Pieces.Should().HaveCount(3, "it leaves through the top at 30° and the bottom at 210°, and comes back each time");
        wave.Breaks.Should().BeEmpty();
        for (int index = 1; index < wave.Pieces.Length; index++)
        {
            wave.Pieces[index][0].X.Should().BeGreaterThan(wave.Pieces[index - 1][^1].X, "each piece starts where the one before it has ended");
        }
    }

    [Fact]
    public void AnExpressionThatDidNotCompileDrawsNothing()
    {
        GraphTrace nothing = GraphSampler.Sample(Compile("x+"), Square, 400, 400);

        nothing.Pieces.Should().BeEmpty();
        nothing.Breaks.Should().BeEmpty();
    }

    [Fact]
    public void AComplexValueHasNoPoint()
    {
        GraphTrace root = GraphSampler.Sample(Compile("√(x)", app: CalculatorApp.Complex), Square, 400, 400);

        root.Pieces.Should().ContainSingle();
        root.Pieces[0].Should().OnlyContain(point => point.X > -1e-98, "an x below 10⁻⁹⁹ is 0 to the calculator, and √0 is real");
    }

    [Fact]
    public void EveryPointIsInsideTheViewport()
    {
        GraphViewport view = new(-3d, 3d, -2d, 2d);
        foreach (string input in (string[])["1÷x", "x^3", "tan(x)", "e^x", "Int(x)", "√(x)", "1÷(x^2−1)"])
        {
            GraphTrace trace = Sample(input, view, AngleUnit.Radian);
            trace.Pieces.SelectMany(p => p).Should().OnlyContain(point => Inside(point, view), "{0} is clipped to the viewport", input);
            trace.Pieces.Should().OnlyContain(piece => piece.Length >= 2);
            trace.Evaluations.Should().BeLessThanOrEqualTo(32 * 401 + 64, "{0} stays within the budget", input);
        }
    }

    [Theory]
    [InlineData("√(x)÷(sin(x)−x)", -34d, 38d, -37d, -16d, 152, 128)]
    [InlineData("1÷√(x÷0.5)", -36d, 17d, 48d, 82d, 214, 94)]
    public void ACurveThatCrossesTheViewportFromNearItsAsymptoteIsClippedToIt(string input, double left, double right, double bottom, double top, int columns, int rows)
    {
        // Found by GraphPropertyTests on 24 Sep 2026. Followed to where it has a value just right of 0, each curve starts
        // far beyond the viewport and is past its other edge by the next point: it crosses both edges at a fraction of
        // the segment that is 1 to the last digit, and the point beyond the viewport was kept as the end of the piece.
        GraphViewport view = new(left, right, bottom, top);
        GraphTrace trace = GraphSampler.Sample(Compile(input), view, columns, rows);

        trace.Pieces.Should().NotBeEmpty();
        trace.Pieces.SelectMany(p => p).Should().OnlyContain(point => Inside(point, view));
    }

    [Fact]
    public void ASamplingIsCancelled()
    {
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        ((Action)(() => GraphSampler.Sample(Compile("x"), Square, 400, 400, cancelled.Token))).Should().Throw<OperationCanceledException>();
    }

    [Theory]
    [InlineData(0, 400, "columns")]
    [InlineData(10_001, 400, "columns")]
    [InlineData(400, 0, "rows")]
    [InlineData(400, 10_001, "rows")]
    public void TheSizeOfTheDrawingIsOneToTenThousandPixels(int columns, int rows, string parameter)
    {
        ((Action)(() => GraphSampler.Sample(Compile("x"), Square, columns, rows))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName(parameter);
    }

    [Fact]
    public void AnExpressionAndAViewportAreRequired()
    {
        ((Action)(() => GraphSampler.Sample(null!, Square, 1, 1))).Should().Throw<ArgumentNullException>().WithParameterName("expression");
        ((Action)(() => GraphSampler.Sample(Compile("x"), null!, 1, 1))).Should().Throw<ArgumentNullException>().WithParameterName("viewport");
    }

    private static bool Inside(GraphPoint point, GraphViewport viewport) =>
        point.X >= viewport.Left && point.X <= viewport.Right && point.Y >= viewport.Bottom && point.Y <= viewport.Top;
}
