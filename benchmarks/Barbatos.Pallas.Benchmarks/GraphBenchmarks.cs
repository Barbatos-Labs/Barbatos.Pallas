// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Graphing;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// A curve drawn across 2,000 columns, as wide as a screen, and what the graph of a table names on it.
/// </summary>
/// <remarks>
/// A parabola, a curve with asymptotes, the normal density with its constant √(2π) and a curve that oscillates: the
/// shapes that cost the sampler the least and the most refinement and break-searching.
/// </remarks>
public class GraphBenchmarks
{
    private CompiledExpression _curve = null!;
    private GraphViewport _view = null!;

    /// <summary>The curves, with the viewport each is drawn in (degrees).</summary>
    public static IEnumerable<string> Curves => ["x²−3x+1", "tan(x)", "e^(-x²÷2)÷√(2π)", "sin(x)÷x"];

    /// <summary>Gets or sets the curve.</summary>
    [ParamsSource(nameof(Curves))]
    public string Curve { get; set; } = "";

    /// <summary>Compiles the curve once, as the graph of a table does.</summary>
    [GlobalSetup]
    public void Setup()
    {
        CalculatorSession session = Sessions.In(CalculatorApp.Table);
        _curve = session.Compile(Curve);
        if (!_curve.Succeeded)
        {
            throw new InvalidOperationException($"'{Curve}' does not compile.");
        }

        _view = Curve switch
        {
            "tan(x)" => new GraphViewport(0, 360, -10, 10),
            "e^(-x²÷2)÷√(2π)" => new GraphViewport(-4, 4, -0.1, 0.5),
            "sin(x)÷x" => new GraphViewport(-1080, 1080, -0.3, 1.1),
            _ => new GraphViewport(-10, 10, -10, 10),
        };
    }

    /// <summary>Samples the curve for a drawing 2,000 pixels wide and 1,000 high: one frame of it.</summary>
    [Benchmark]
    [Target(16)]
    public GraphTrace Sample() => GraphSampler.Sample(_curve, _view, 2000, 1000);

    /// <summary>Names its roots and extrema, as the graph of a table searches them: in 400 intervals of the view.</summary>
    /// <remarks>The names are drawn with the curve, so they have a frame's target too.</remarks>
    [Benchmark]
    [Target(16)]
    public int NamePoints()
    {
        ImmutableArray<GraphFeature> roots = GraphAnalysis.Roots(_curve, _view, 400);
        ImmutableArray<GraphFeature> extrema = GraphAnalysis.Extrema(_curve, _view, 400);
        return roots.Length + extrema.Length;
    }
}
