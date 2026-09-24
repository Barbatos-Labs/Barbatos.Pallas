// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Graphing.Tests;

/// <summary>
/// Roots, extrema and intersections against their closed forms, and the engine's own values there.
/// </summary>
public sealed class GraphAnalysisTests
{
    private static CompiledExpression Compile(string input, AngleUnit unit = AngleUnit.Degree) => GraphSamplerTests.Compile(input, unit);

    private static GraphViewport Across(double left, double right) => new(left, right, -1d, 1d);

    [Fact]
    public void AnIrrationalRootIsPlacedToFifteenDigits()
    {
        ImmutableArray<GraphFeature> roots = GraphAnalysis.Roots(Compile("x^2−2"), Across(-3d, 3d), 100);

        roots.Should().HaveCount(2).And.OnlyContain(root => root.Kind == GraphFeatureKind.Root);
        roots[0].X.ToDecimal().Should().Be(-1.41421356237310m, "√2 to 15 significant digits");
        roots[1].X.ToDecimal().Should().Be(1.41421356237310m);
        roots[1].Y.Succeeded.Should().BeTrue();
        Math.Abs(roots[1].Y.Result.ToDouble()).Should().BeLessThan(1e-13);
    }

    [Fact]
    public void ARootAtASimpleNumberIsThatNumber()
    {
        // Halving ends at neighbouring doubles either side of 1, and both are 1 to fifteen digits.
        ImmutableArray<GraphFeature> roots = GraphAnalysis.Roots(Compile("(x−1)(x+2.5)"), Across(-3.3d, 3.1d), 97);

        roots.Select(root => root.X.ToDecimal()).Should().Equal(-2.5m, 1m);
        roots.Should().OnlyContain(root => root.Y.Result.ToDecimal() == 0m, "the engine's value at an exact root is exactly 0");
    }

    [Fact]
    public void ARootAtZeroIsZero()
    {
        ImmutableArray<GraphFeature> roots = GraphAnalysis.Roots(Compile("x^3+x"), Across(-1.7d, 2.3d), 50);

        roots.Should().ContainSingle().Which.X.ToDecimal().Should().Be(0m);
    }

    [Fact]
    public void ARootAtZeroIsZeroWhereTheRangeReachesTheSmallestDouble()
    {
        // In the Extended profile nothing below 10⁻⁹⁹ is 0, so the halving ends at the smallest doubles either side
        // of 0: those are 0.
        CompiledExpression cubic = GraphSamplerTests.Compile("x^3+x", profile: CalculatorProfile.Extended);

        GraphAnalysis.Roots(cubic, Across(-0.53d, 0.47d), 10).Should().ContainSingle().Which.X.ToDecimal().Should().Be(0m);
    }

    [Theory]
    [InlineData("x−0.1234567890123456", "0.123456789012346")]
    [InlineData("x−0.1234567890123454", "0.123456789012345")]
    public void OfTwoFifteenDigitValuesTheNearerIsTheRoot(string input, string root)
    {
        // The engine's value is 4×10⁻¹⁶ from 0 at one of the two and 6×10⁻¹⁶ at the other.
        GraphAnalysis.Roots(Compile(input), Across(0.03d, 0.53d), 10).Should().ContainSingle()
            .Which.X.ToDecimal().Should().Be(decimal.Parse(root, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ARootAtASampleIsFoundWithoutHalving()
    {
        // sin 180° is exactly 0 in degrees, and 180 is a sample.
        ImmutableArray<GraphFeature> roots = GraphAnalysis.Roots(Compile("sin(x)"), Across(90d, 270d), 18);

        roots.Should().ContainSingle().Which.X.ToDecimal().Should().Be(180m);
    }

    [Theory]
    [InlineData("1÷x")]
    [InlineData("Intg(x)−0.5")]
    [InlineData("tan(x)")]
    public void APoleOrAJumpIsNoRoot(string input)
    {
        // Each changes sign between two samples without a value that shrinks towards 0: Intg(x)−0.5 jumps across 0 at 1,
        // tan x has its pole at 90, and 1÷x changes sign nowhere between 0.3 and 179.7.
        GraphAnalysis.Roots(Compile(input), Across(0.3d, 179.7d), 200).Should().BeEmpty();
    }

    [Fact]
    public void ARootWhereTheCurveHasNoValueIsNoRoot()
    {
        // x(x÷x) is x everywhere but at 0, where 0÷0 has no value: the curve crosses the axis through a hole.
        // The samples either side are −0.03 and 0.07, and the halving between them never lands on 0 itself.
        GraphAnalysis.Roots(Compile("x(x÷x)"), Across(-0.53d, 0.47d), 10).Should().BeEmpty();
    }

    [Fact]
    public void CurvesThatMeetAcrossAJumpDoNotMeet()
    {
        GraphAnalysis.Intersections(Compile("Intg(x)"), Compile("0.5"), Across(0.3d, 1.7d), 20).Should().BeEmpty();
    }

    [Fact]
    public void ARootOnlyTouchedIsNoRoot()
    {
        // (x−1)² touches the axis without crossing it: it is a minimum, found as one.
        CompiledExpression touching = Compile("(x−1)^2");

        GraphAnalysis.Roots(touching, Across(-2.3d, 2.9d), 40).Should().BeEmpty();
        GraphAnalysis.Extrema(touching, Across(-2.3d, 2.9d), 40).Should().ContainSingle().Which.X.ToDecimal().Should().Be(1m);
    }

    [Fact]
    public void TheExtremaAreWhereTheDerivativeChangesSign()
    {
        ImmutableArray<GraphFeature> turns = GraphAnalysis.Extrema(Compile("x^3−3x"), Across(-3.1d, 2.9d), 60);

        turns.Select(turn => (turn.Kind, turn.X.ToDecimal(), turn.Y.Result.ToDecimal())).Should().Equal(
            (GraphFeatureKind.Maximum, -1m, 2m),
            (GraphFeatureKind.Minimum, 1m, -2m));
    }

    [Fact]
    public void AnExtremumOfASineIsExact()
    {
        // The derivative is exact: cos(x)·π÷180 is exactly 0 at 90° and 270°.
        ImmutableArray<GraphFeature> turns = GraphAnalysis.Extrema(Compile("sin(x)"), Across(1d, 359d), 37);

        turns.Select(turn => (turn.Kind, turn.X.ToDecimal(), turn.Y.Display.Text)).Should().Equal(
            (GraphFeatureKind.Maximum, 90m, "1"),
            (GraphFeatureKind.Minimum, 270m, "-1"));
    }

    [Fact]
    public void AKinkIsAnExtremum()
    {
        GraphAnalysis.Extrema(Compile("Abs(x)"), Across(-1.3d, 2.2d), 30)
            .Should().ContainSingle().Which.Should().Match<GraphFeature>(turn => turn.Kind == GraphFeatureKind.Minimum && turn.X.ToDecimal() == 0m);
    }

    [Fact]
    public void AKinkAtASampleIsAnExtremum()
    {
        // 0 is a sample here, and the slope of |x| has no value there: the change of sign is across it.
        GraphAnalysis.Extrema(Compile("Abs(x)"), Across(-1d, 1d), 10)
            .Should().ContainSingle().Which.Should().Match<GraphFeature>(turn => turn.Kind == GraphFeatureKind.Minimum && turn.X.ToDecimal() == 0m);
    }

    [Fact]
    public void APoleAtASampleIsNoRoot()
    {
        GraphAnalysis.Roots(Compile("1÷x"), Across(-1d, 1d), 10).Should().BeEmpty();

        // At the last sample, with nothing beyond it to change sign against.
        GraphAnalysis.Roots(Compile("1÷(x−1)"), Across(0d, 1d), 10).Should().BeEmpty();
    }

    [Theory]
    [InlineData("x−0.95", "0.95")]
    [InlineData("x−1", "1")]
    public void TheLastColumnIsSearchedToo(string input, string root)
    {
        // A root in the last column, and one at the last sample itself.
        GraphAnalysis.Roots(Compile(input), Across(0d, 1d), 10).Should().ContainSingle()
            .Which.X.ToDecimal().Should().Be(decimal.Parse(root, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void AKinkAwayFromZeroIsWhereItIs()
    {
        GraphAnalysis.Extrema(Compile("Abs(x−1)"), Across(-0.3d, 2.2d), 30)
            .Should().ContainSingle().Which.Should().Match<GraphFeature>(turn => turn.Kind == GraphFeatureKind.Minimum && turn.X.ToDecimal() == 1m);
    }

    [Theory]
    [InlineData("Abs(x−1)+5Intg(x)")]
    [InlineData("Abs(x−1)+5Intg(−x)")]
    public void ATurnOfTheSlopeAtAJumpIsNoExtremum(string input)
    {
        // The slope turns from falling to rising at 1, where the curve jumps: up, so that the lowest point is just
        // before 1, or down, so that it is just after. Neither is a point of the curve to name.
        CompiledExpression curve = Compile(input);
        curve.Derivative().TryEvaluate(0.5d, out double falling).Should().BeTrue("the slope is there to turn");
        falling.Should().Be(-1d);

        // 1 is no sample of these twenty columns: the slope changes sign between 0.96 and 1.03.
        GraphAnalysis.Extrema(curve, Across(0.33d, 1.73d), 20).Should().BeEmpty();
    }

    [Theory]
    [InlineData("1÷x^2", AngleUnit.Degree, -1.3d, 1.7d)]
    [InlineData("1÷sin(x)^2", AngleUnit.Radian, 2.1d, 4.3d)]
    public void APoleIsNoExtremum(string input, AngleUnit unit, double left, double right)
    {
        // The slope changes sign across the pole, and the curve there is higher than anything - with no value.
        GraphAnalysis.Extrema(Compile(input, unit), Across(left, right), 40).Should().BeEmpty();
    }

    [Fact]
    public void AnExpressionWithoutADerivativeHasNoExtrema()
    {
        GraphAnalysis.Extrema(Compile("x+"), Across(-1d, 1d), 10).Should().BeEmpty();
    }

    [Fact]
    public void AFlatCurveHasNoExtremum()
    {
        // The slope of 5 is 0 at every sample, with 0 either side: it neither falls nor rises.
        GraphAnalysis.Extrema(Compile("5"), Across(-1d, 1d), 10).Should().BeEmpty();
    }

    [Fact]
    public void AFlatSlopeWithoutATurnIsNoExtremum()
    {
        // x³ is flat at 0 and rises either side of it.
        GraphAnalysis.Extrema(Compile("x^3"), Across(-2d, 2d), 40).Should().BeEmpty();
    }

    [Fact]
    public void TwoCurvesMeetWhereTheirDifferenceChangesSign()
    {
        ImmutableArray<GraphFeature> meetings = GraphAnalysis.Intersections(Compile("x^2"), Compile("x+2"), Across(-3.1d, 3.3d), 64);

        meetings.Select(meeting => (meeting.Kind, meeting.X.ToDecimal(), meeting.Y.Result.ToDecimal())).Should().Equal(
            (GraphFeatureKind.Intersection, -1m, 1m),
            (GraphFeatureKind.Intersection, 2m, 4m));
    }

    [Fact]
    public void ASineAndACosineMeetAt45Degrees()
    {
        ImmutableArray<GraphFeature> meetings = GraphAnalysis.Intersections(Compile("sin(x)"), Compile("cos(x)"), Across(1d, 359d), 90);

        meetings.Select(meeting => meeting.X.ToDecimal()).Should().Equal(45m, 225m);
        meetings[0].Y.Display.Text.Should().Be("√(2)⌟2", "the y is the engine's calculation, exact form included");
    }

    [Fact]
    public void CurvesThatNeverMeetHaveNoIntersection()
    {
        GraphAnalysis.Intersections(Compile("x^2+1"), Compile("x−1"), Across(-5d, 5d), 50).Should().BeEmpty();
    }

    [Fact]
    public void ASearchIsCancelled()
    {
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        ((Action)(() => GraphAnalysis.Roots(Compile("x"), Across(-1d, 1d), 10, cancelled.Token))).Should().Throw<OperationCanceledException>();
        ((Action)(() => GraphAnalysis.Extrema(Compile("x^2"), Across(-1d, 1d), 10, cancelled.Token))).Should().Throw<OperationCanceledException>();
        ((Action)(() => GraphAnalysis.Intersections(Compile("x"), Compile("−x"), Across(-1d, 1d), 10, cancelled.Token))).Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void TheArgumentsAreChecked()
    {
        CompiledExpression x = Compile("x");
        GraphViewport view = Across(-1d, 1d);

        ((Action)(() => GraphAnalysis.Roots(null!, view, 10))).Should().Throw<ArgumentNullException>().WithParameterName("expression");
        ((Action)(() => GraphAnalysis.Extrema(null!, view, 10))).Should().Throw<ArgumentNullException>().WithParameterName("expression");
        ((Action)(() => GraphAnalysis.Intersections(null!, x, view, 10))).Should().Throw<ArgumentNullException>().WithParameterName("first");
        ((Action)(() => GraphAnalysis.Intersections(x, null!, view, 10))).Should().Throw<ArgumentNullException>().WithParameterName("second");
        ((Action)(() => GraphAnalysis.Roots(x, null!, 10))).Should().Throw<ArgumentNullException>().WithParameterName("viewport");
        ((Action)(() => GraphAnalysis.Extrema(x, null!, 10))).Should().Throw<ArgumentNullException>().WithParameterName("viewport");
        ((Action)(() => GraphAnalysis.Intersections(x, x, null!, 10))).Should().Throw<ArgumentNullException>().WithParameterName("viewport");
        ((Action)(() => GraphAnalysis.Extrema(x, view, 0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
        ((Action)(() => GraphAnalysis.Intersections(x, x, view, 0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
        ((Action)(() => GraphAnalysis.Roots(x, view, 0))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
        ((Action)(() => GraphAnalysis.Roots(x, view, 10_001))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("columns");
    }
}
