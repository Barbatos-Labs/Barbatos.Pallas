// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Graphing.Tests;

/// <summary>
/// The examples of the README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void ACurveAndItsNamedPoints()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
        CalculatorSession session = engine.CreateSession(CalculatorApp.Table);
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Degree };

        GraphViewport view = new(left: 0, right: 360, bottom: -10, top: 10);
        GraphTrace tangent = GraphSampler.Sample(session.Compile("tan(x)"), view, columns: 400, rows: 300);

        tangent.Pieces.Length.Should().Be(3);
        tangent.Breaks[0].Kind.Should().Be(GraphBreakKind.Asymptote);
        tangent.Breaks[0].X.Should().BeApproximately(90, 1e-12);

        GraphAnalysis.Roots(session.Compile("x^2−2"), new GraphViewport(-3, 3, -1, 1), columns: 100)[1].X.ToDecimal().Should().Be(1.41421356237310m);

        GraphFeature top = GraphAnalysis.Extrema(session.Compile("sin(x)"), view, columns: 36)[0];
        (top.Kind, top.X.ToDecimal(), top.Y.Display.Text).Should().Be((GraphFeatureKind.Maximum, 90m, "1"));

        GraphAnalysis.Intersections(session.Compile("sin(x)"), session.Compile("cos(x)"), view, 90)[0].Y.Display.Text.Should().Be("√(2)⌟2");
    }
}
