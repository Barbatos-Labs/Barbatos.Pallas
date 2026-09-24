// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Graphing;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The graph of the Table screen: the session's f(x) and g(x) drawn over the rows of the table, which on the calculator
/// become a graph through its QR code (manual pp. 77-78).
/// </summary>
public sealed class TableGraphViewModelTests
{
    private static TableViewModel Table(string f, string? g, string start, string end, string step)
    {
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table))
        {
            Type = g is null ? TableType.FunctionF : TableType.FunctionsFAndG,
            FunctionF = f,
            FunctionG = g ?? "x",
        };
        screen.Range[0, 0].Text = start;
        screen.Range[0, 1].Text = end;
        screen.Range[0, 2].Text = step;
        screen.Generate();
        return screen;
    }

    // A view worked out in double, such as the center of one plus half its size, is only near what it is in decimals.
    private static void Near(GraphViewport? view, double left, double right, double bottom, double top)
    {
        view.Should().NotBeNull();
        view!.Left.Should().BeApproximately(left, 1e-9);
        view.Right.Should().BeApproximately(right, 1e-9);
        view.Bottom.Should().BeApproximately(bottom, 1e-9);
        view.Top.Should().BeApproximately(top, 1e-9);
    }

    [Fact]
    public void TheViewShowsEveryRowOfTheTable()
    {
        // x² and x+1 from 1 to 5: x from 1 to 5 and a twentieth of it either side, y from 1 to 25 and a tenth of it.
        TableGraphViewModel graph = Table("x²", "x+1", "1", "5", "1").Graph;

        graph.HasGraph.Should().BeTrue();
        graph.Viewport.Should().Be(new GraphViewport(0.8, 5.2, -1.4, 27.4));
        graph.Points.Should().HaveCount(10);
        graph.Points[0].Should().Be(new TablePoint(TableFunction.F, new GraphPoint(1, 1)));
        graph.Points[1].Should().Be(new TablePoint(TableFunction.G, new GraphPoint(1, 2)));
        graph.Points[^2].Should().Be(new TablePoint(TableFunction.F, new GraphPoint(5, 25)));
    }

    [Fact]
    public void TheCurvesAreSampledAtThePixelsOfTheDrawing()
    {
        TableGraphViewModel graph = Table("x²", "x+1", "1", "5", "1").Graph;
        graph.Curves.Should().BeEmpty("nothing says how large the drawing is yet");

        graph.Resize(400, 300);

        graph.Curves.Select(curve => curve.Function).Should().Equal(TableFunction.F, TableFunction.G);
        graph.Curves[0].Trace.Viewport.Should().Be(graph.Viewport);
        graph.Curves[0].Trace.Pieces.Should().ContainSingle();
        graph.Curves[0].Trace.Pieces[0][0].Should().Be(new GraphPoint(0.8, 0.64), "0.64 is the engine's 0.8², where the double 0.8 × 0.8 is 0.6400000000000001");
        graph.Curves[1].Trace.Pieces[0][^1].Should().Be(new GraphPoint(5.2, 6.2));

        graph.Resize(0, 300);
        graph.Curves.Should().BeEmpty("a drawing with no area is sampled at nothing");
        graph.Resize(400, 0);
        graph.Curves.Should().BeEmpty();

        graph.Resize(50_000, 50_000);
        graph.Curves.Should().HaveCount(2, "a drawing wider than any screen is sampled at the most columns there are");
    }

    [Fact]
    public void ACurveIsNotDrawnAcrossAnAsymptote()
    {
        TableGraphViewModel graph = Table("1÷x", null, "-2", "2", "1").Graph;
        graph.Resize(400, 300);

        TableCurve curve = graph.Curves.Should().ContainSingle().Subject;
        curve.Trace.Pieces.Should().HaveCount(2);
        GraphBreak asymptote = curve.Trace.Breaks.Should().ContainSingle().Subject;
        asymptote.Kind.Should().Be(GraphBreakKind.Asymptote);
        asymptote.X.Should().BeApproximately(0, 1e-99, "an x below 10⁻⁹⁹ is 0 in the Standard profile, where 1÷x has no value");
        graph.Points.Should().HaveCount(4, "1÷0 is a Math ERROR, which is no point");
    }

    [Fact]
    public void RootsExtremaAndIntersectionsAreNamedLeftToRight()
    {
        // x²−2 and x from -2 to 2: roots at ±√2 and a minimum at 0; x at 0; they meet at -1 and 2. A root is written
        // as the display writes a result, so its fifteen digits are √(2) under MathO, and its y is the engine's value
        // there, which is not 0: the square of a fifteen-digit number is held to fifteen digits too.
        TableGraphViewModel graph = Table("x²−2", "x", "-2", "2", "1").Graph;

        graph.Features.Select(feature => (feature.Kind, feature.Name, feature.X, feature.Y)).Should().Equal(
            (GraphFeatureKind.Root, "f(x)", "-√(2)", "1.40041036×10^-14"),
            (GraphFeatureKind.Intersection, "f(x) = g(x)", "-1", "-1"),
            (GraphFeatureKind.Minimum, "f(x)", "0", "-2"),
            (GraphFeatureKind.Root, "g(x)", "0", "0"),
            (GraphFeatureKind.Root, "f(x)", "√(2)", "1.40041036×10^-14"),
            (GraphFeatureKind.Intersection, "f(x) = g(x)", "2", "2"));
        graph.Features[0].At.Should().Be(new GraphPoint(-1.4142135623731, 1.400410360361E-14));
        graph.Features[2].At.Should().Be(new GraphPoint(0, -2));
        graph.Features[5].At.Should().Be(new GraphPoint(2, 2));
        graph.Features[1].Function.Should().BeNull("an intersection is both functions'");
        graph.Features[3].Function.Should().Be(TableFunction.G);
    }

    [Fact]
    public void AFeatureOfGAloneIsNamedForG()
    {
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table)) { Type = TableType.FunctionG, FunctionG = "x²−1" };
        screen.Generate();

        screen.Graph.Features.Select(feature => (feature.Kind, feature.Name, feature.X)).Should().Equal(
            (GraphFeatureKind.Root, "g(x)", "1"));
        screen.Graph.Points.Should().AllSatisfy(point => point.Function.Should().Be(TableFunction.G));
    }

    [Fact]
    public void TheAxesAreMarkedWithTheDisplaysNumbers()
    {
        TableGraphViewModel graph = Table("x²", "x+1", "1", "5", "1").Graph;

        graph.XTicks.Select(tick => tick.Label).Should().Equal("1", "2", "3", "4", "5");
        graph.XTicks.Select(tick => tick.At).Should().Equal(1, 2, 3, 4, 5);
        graph.YTicks.Select(tick => tick.Label).Should().Equal("0", "5", "10", "15", "20", "25");
    }

    [Fact]
    public void AMarkIsLabelledWithFifteenDigitsNotTheDoublesSeventeen()
    {
        // From 0 to 1 by 0.25 the step of a mark is 0.2, and 3 × 0.2 is 0.6000000000000001 as a double.
        TableGraphViewModel graph = Table("x", null, "0", "1", "0.25").Graph;

        graph.XTicks.Select(tick => tick.Label).Should().Equal("0", "0.2", "0.4", "0.6", "0.8", "1");
        graph.XTicks[3].At.Should().Be(3 * 0.2);
    }

    [Fact]
    public void AStepOfTwoOrOfTenIsChosenWhereItFits()
    {
        // A sixth of 11 is 1.83, a step of 2; of 55, 9.17, a step of 10; of 13.2, 2.2, a step of 5.
        Table("x", null, "0", "10", "1").Graph.XTicks.Select(tick => tick.Label).Should().Equal("0", "2", "4", "6", "8", "10");
        Table("x", null, "0", "50", "5").Graph.XTicks.Select(tick => tick.Label).Should().Equal("0", "10", "20", "30", "40", "50");
        Table("x", null, "0", "12", "1").Graph.XTicks.Select(tick => tick.Label).Should().Equal("0", "5", "10");
    }

    [Fact]
    public void AStepOfExactlyOneTwoOrFiveIsThatStep()
    {
        // x from 0 to 5, to 10 and to 25: y a tenth more either side is 6, 12 and 30 high, a sixth of which is 1, 2 and 5.
        Table("x", null, "0", "5", "1").Graph.YTicks.Select(tick => tick.Label).Should().Equal("0", "1", "2", "3", "4", "5");
        Table("x", null, "0", "10", "1").Graph.YTicks.Select(tick => tick.Label).Should().Equal("0", "2", "4", "6", "8", "10");
        Table("x", null, "0", "25", "5").Graph.YTicks.Select(tick => tick.Label).Should().Equal("0", "5", "10", "15", "20", "25");
    }

    [Fact]
    public void AMarkOnTheEdgeOfTheViewIsMarked()
    {
        // y from -1 to 11, zoomed in: from 2 to 8, marked at both ends.
        TableGraphViewModel graph = Table("x", null, "0", "10", "1").Graph;

        graph.ZoomIn();

        graph.YTicks.Select(tick => tick.Label).Should().Equal("2", "3", "4", "5", "6", "7", "8");
    }

    [Fact]
    public void ZoomingKeepsTheCenterAndFittingComesBack()
    {
        TableGraphViewModel graph = Table("x²", "x+1", "1", "5", "1").Graph;
        GraphViewport fitted = graph.Viewport!;
        graph.FitCommand.CanExecute(null).Should().BeFalse("the view already fits the table");
        graph.Resize(400, 300);
        graph.Read(0.5);

        graph.ZoomIn();

        Near(graph.Viewport, 1.9, 4.1, 5.8, 20.2);
        graph.FitCommand.CanExecute(null).Should().BeTrue();
        graph.Reading.Should().BeNull("what was read is not where the pointer is any more");
        graph.Curves[0].Trace.Viewport.Should().Be(graph.Viewport, "the curves are sampled again");

        graph.ZoomOut();
        graph.ZoomOut();

        Near(graph.Viewport, -1.4, 7.4, -15.8, 41.8);

        graph.Fit();

        graph.Viewport.Should().Be(fitted);
    }

    [Fact]
    public void ZoomingStopsWhereAViewShowsNothingMore()
    {
        TableGraphViewModel graph = Table("x", null, "1", "5", "1").Graph;
        int zoomed = 0;
        while (graph.ZoomInCommand.CanExecute(null))
        {
            graph.ZoomIn();
            zoomed++;
        }

        // Twelve digits of 3, the center, are 3×10⁻¹²: half of 4.4 halved 39 times is 4.0×10⁻¹², once more 2.0×10⁻¹².
        zoomed.Should().Be(39);
        (graph.Viewport!.Width / 2).Should().BeGreaterThan(3e-12);
        graph.ZoomOutCommand.CanExecute(null).Should().BeTrue();

        graph.Fit();
        zoomed = 0;
        while (graph.ZoomOutCommand.CanExecute(null))
        {
            graph.ZoomOut();
            zoomed++;
        }

        // From 9.9999999995×10⁹⁹ on every value is a Math ERROR: a view goes no further than 10⁹⁹.
        Math.Max(graph.Viewport!.Right, graph.Viewport.Top).Should().BeLessThanOrEqualTo(1e99);
        (Math.Max(graph.Viewport.Right, graph.Viewport.Top) * 2).Should().BeGreaterThan(1e99);
        graph.ZoomInCommand.CanExecute(null).Should().BeTrue();
        zoomed.Should().BeGreaterThan(300);
    }

    [Fact]
    public void AViewNearTheSmallestNumberStopsThere()
    {
        // 10⁻⁹⁹ and 2×10⁻⁹⁹: the view is 1.2×10⁻⁹⁹ high, and halved it would be lower than the profile's smallest number.
        TableGraphViewModel graph = Table("x×10^(-99)", null, "1", "2", "1").Graph;

        graph.Viewport!.Height.Should().BeApproximately(1.2e-99, 1e-110);
        graph.ZoomInCommand.CanExecute(null).Should().BeFalse();
        graph.ZoomOutCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ATableBeyondWhereAViewReachesIsZoomedIntoAllTheSame()
    {
        // 10⁹⁹ to 9×10⁹⁹: fitted, the view reaches 9.8×10⁹⁹ already, so it is zoomed out no further, but zoomed into.
        TableGraphViewModel graph = Table("x×10^99", null, "1", "9", "1").Graph;

        graph.Viewport!.Top.Should().BeApproximately(9.8e99, 1e89);
        graph.ZoomOutCommand.CanExecute(null).Should().BeFalse();
        graph.ZoomInCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ARowAloneIsShownAroundItself()
    {
        // One row, x = 2 and x² = 4: from half of each below to half above.
        TableGraphViewModel graph = Table("x²", null, "2", "2", "1").Graph;

        graph.Viewport.Should().Be(new GraphViewport(1, 3, 2, 6));
    }

    [Fact]
    public void ACurveAtZeroIsShownFromMinusOneToOne()
    {
        TableGraphViewModel graph = Table("0x", null, "-1", "1", "1").Graph;

        graph.Viewport.Should().Be(new GraphViewport(-1.1, 1.1, -1, 1));
    }

    [Fact]
    public void ValuesTheDisplayShowsAsOneNumberAreNoSpread()
    {
        // 1 and 1 + 10⁻¹⁰ differ in the eleventh digit, which the display does not show.
        TableGraphViewModel graph = Table("1+x×10^(-10)", null, "0", "1", "1").Graph;

        // Shown around the larger, from half of it below to half above.
        graph.Viewport!.Bottom.Should().BeApproximately(1 - ((1 + 1e-10) / 2), 1e-15);
        graph.Viewport.Top.Should().BeApproximately(1 + 1e-10 + ((1 + 1e-10) / 2), 1e-15);
    }

    [Fact]
    public void ATableWithNoValueHasItsXAxisThroughTheMiddle()
    {
        TableGraphViewModel graph = Table("√(-1-x²)", null, "1", "3", "1").Graph;

        graph.Points.Should().BeEmpty();
        graph.Viewport.Should().Be(new GraphViewport(0.9, 3.1, -1, 1));
    }

    [Fact]
    public void TheGraphIsReadAtThePointer()
    {
        TableGraphViewModel graph = Table("x²−2", "x", "-2", "2", "1").Graph;
        graph.Read(0.5);
        graph.Reading.Should().BeNull("nothing says how large the drawing is yet");

        // 440 pixels across 4.4: a pixel is 0.01, so x is read to two decimals.
        graph.Resize(440, 300);
        graph.Read(0.75);

        TableGraphReading reading = graph.Reading!;
        reading.X.Should().Be("1.1");
        reading.At.Should().Be(1.1);
        reading.Values.Select(value => (value.Name, value.Text, value.ErrorKey, value.Y)).Should().Equal(
            ("f(x)", "-79⌟100", null, -0.79),
            ("g(x)", "11⌟10", null, 1.1));

        graph.Read(0.123456);
        graph.Reading!.X.Should().Be("-1.66", "-2.2 + 0.123456 × 4.4 is -1.6568, to the pixel");

        graph.Read(-3);
        graph.Reading!.X.Should().Be("-2.2", "a pointer past the drawing reads its edge");

        graph.StopReading();
        graph.Reading.Should().BeNull();
    }

    [Fact]
    public void WhereAFunctionHasNoValueTheReadingSaysWhy()
    {
        TableGraphViewModel graph = Table("1÷x", null, "-2", "2", "1").Graph;
        graph.Resize(440, 300);

        graph.Read(0.5);

        graph.Reading!.X.Should().Be("0");
        graph.Reading.Values.Should().ContainSingle().Which.Should().Be(new TableGraphValue(TableFunction.F, null, "error.MathError", null));
    }

    [Fact]
    public void TheTableCalculatesInRealNumbersOnly()
    {
        // A variable keeps the complex number the Complex application stored in it, and A+x is a Math ERROR in the
        // Table application: what the graph draws and reads is always a real number, or nothing.
        CalculatorSession session = Shell.Session(CalculatorApp.Table);
        session.SetVariable(MemoryVariable.A, Value.FromComplex(new Complex(0, 1)));
        TableViewModel screen = new(session) { Type = TableType.FunctionF, FunctionF = "A+x" };
        screen.Generate();
        screen.Graph.Resize(440, 300);

        screen.Graph.Read(0.5);

        screen.Graph.Points.Should().BeEmpty();
        screen.Graph.Reading!.Values.Should().ContainSingle().Which.Should().Be(new TableGraphValue(TableFunction.F, null, "error.MathError", null));
    }

    [Fact]
    public void AnXIsReadToFourteenDecimalsAtMost()
    {
        // Around 0, zoomed in 38 times, a pixel of 100 is 8.0×10⁻¹⁴, which tells fourteen decimals apart: halfway to the
        // right edge, 2.0009×10⁻¹², is read as 2×10⁻¹². A pixel finer than that reads the engine's own value.
        TableGraphViewModel graph = Table("x", null, "-1", "1", "1").Graph;
        for (int zoom = 0; zoom < 38; zoom++)
        {
            graph.ZoomIn();
        }

        graph.Resize(100, 100);
        graph.Read(0.75);

        graph.Reading!.X.Should().Be("2×10^-12");
    }

    [Fact]
    public void AWidePixelReadsWholeNumbersAndATinyOneTheEngineValue()
    {
        // 1,100 across 100 pixels: a pixel is 11, and -50 + 0.123 × 1,100 = 85.3 is read as 85.
        TableGraphViewModel wide = Table("x", null, "0", "1000", "100").Graph;
        wide.Resize(100, 100);
        wide.Read(0.123);
        wide.Reading!.X.Should().Be("85");
        wide.Reading.At.Should().Be(85);

        // Around 0, zoomed in sixty times, a pixel is 1.9×10⁻²⁰: finer than fourteen decimals, below which a decimal is
        // no longer what the precision rule holds, so x is the engine's own value of the double there.
        TableGraphViewModel narrow = Table("x", null, "-1", "1", "1").Graph;
        for (int zoom = 0; zoom < 60; zoom++)
        {
            narrow.ZoomIn();
        }

        narrow.Resize(100, 100);
        narrow.Read(0.75);
        narrow.Reading!.X.Should().Be("4.770489559×10^-19");

        // From 10¹⁵ a number of fifteen digits has no decimals to round, and past 7.9×10²⁸ no decimal holds it at all.
        TableGraphViewModel large = Table("x", null, "7×10^28", "7.9×10^28", "9×10^27").Graph;
        large.Resize(100, 100);
        large.Read(1);
        large.Reading!.X.Should().Be("7.945×10^28");
    }

    [Fact]
    public void ChangingARowDrawsTheTableAgain()
    {
        TableViewModel screen = Table("x²", null, "1", "5", "1");
        screen.Graph.Resize(400, 300);
        screen.Graph.ZoomIn();

        screen.SetX(0, Value.FromDecimal(10));

        // The view fits the table as it is now, zoomed or not: x from 2 to 10, y from 4 to 100.
        Near(screen.Graph.Viewport, 1.6, 10.4, -5.6, 109.6);
        screen.Graph.Points.Should().Contain(new TablePoint(TableFunction.F, new GraphPoint(10, 100)));
        screen.Graph.Curves.Should().ContainSingle();

        screen.RemoveRow(0);

        screen.Graph.Points.Should().HaveCount(4);
        Near(screen.Graph.Viewport, 1.85, 5.15, 1.9, 27.1);
    }

    [Fact]
    public void ARowWhoseXIsNoRealNumberIsNotDrawn()
    {
        TableViewModel screen = Table("x²", null, "1", "5", "1");

        screen.SetX(0, Value.FromComplex(new Complex(0, 1)));

        screen.Graph.Points.Should().HaveCount(4);
        screen.Graph.Viewport!.Left.Should().Be(1.85);
    }

    [Fact]
    public void ClearingTheTableClearsTheGraph()
    {
        TableViewModel screen = Table("x²", null, "1", "5", "1");
        screen.Graph.Resize(400, 300);
        screen.Graph.Resize(440, 300);
        screen.Graph.Read(0.5);

        screen.Clear();

        screen.Graph.HasGraph.Should().BeFalse();
        screen.Graph.Viewport.Should().BeNull();
        screen.Graph.Curves.Should().BeEmpty();
        screen.Graph.Points.Should().BeEmpty();
        screen.Graph.Features.Should().BeEmpty();
        screen.Graph.XTicks.Should().BeEmpty();
        screen.Graph.YTicks.Should().BeEmpty();
        screen.Graph.Reading.Should().BeNull();
        screen.Graph.ZoomInCommand.CanExecute(null).Should().BeFalse();
        screen.Graph.ZoomOutCommand.CanExecute(null).Should().BeFalse();
        screen.Graph.FitCommand.CanExecute(null).Should().BeFalse();

        screen.Graph.Read(0.5);
        screen.Graph.Reading.Should().BeNull();
    }

    [Fact]
    public void ATableThatFailsHasNoGraph()
    {
        TableViewModel screen = Table("x²", null, "1", "5", "1");

        screen.FunctionF = "x²+";
        screen.Generate();

        screen.Graph.HasGraph.Should().BeFalse("a function that is not syntax draws nothing");

        screen.FunctionF = "x";
        screen.Range[0, 1].Text = "1000";
        screen.Generate();

        screen.Graph.HasGraph.Should().BeFalse("a table of too many rows has none");
    }

    [Fact]
    public void TheScreenShowsTheRowsOrTheGraph()
    {
        TableViewModel screen = new(Shell.Session(CalculatorApp.Table));
        List<string?> changes = screen.Changes();

        screen.IsGraphShown.Should().BeFalse();
        screen.IsGraphShown = true;

        changes.Should().Contain(nameof(TableViewModel.IsGraphShown));
    }

    [Fact]
    public void TheGraphNeedsASession()
    {
        Action graph = () => _ = new TableGraphViewModel(null!);

        graph.Should().Throw<ArgumentNullException>().WithParameterName("session");
    }
}
