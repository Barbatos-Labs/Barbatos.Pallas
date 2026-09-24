// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using BenchmarkDotNet.Attributes;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;
using static Barbatos.Pallas.Benchmarks.Sessions;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// The other applications of the calculator, each at the largest the Standard profile allows or at its slowest form.
/// </summary>
/// <remarks>
/// §9 names no target for them, so each has the one a window has for what one action asks: a frame at 60 Hz, 16 ms.
/// </remarks>
public class ApplicationBenchmarks
{
    private CalculatorSession _matrix = null!;
    private CalculatorSession _equation = null!;
    private CalculatorSession _calculate = null!;
    private CalculatorSession _statistics = null!;
    private CalculatorSession _distribution = null!;
    private CalculatorSession _mathBox = null!;
    private SpreadsheetGrid _sheet = null!;
    private Value[,] _system = null!;
    private StatisticsData _pairs = null!;

    /// <summary>Opens every application with its data in place, and checks that each benchmark calculates.</summary>
    [GlobalSetup]
    public void Setup()
    {
        // A 4×4 matrix with decimals and no special structure: its inverse is an exact fraction, rounded once.
        _matrix = In(CalculatorApp.Matrix);
        _matrix.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,]
        {
            { N(2.5m), N(-1), N(3), N(0.25m) },
            { N(1), N(4), N(-2), N(1) },
            { N(0), N(1.5m), N(1), N(-3) },
            { N(2), N(0), N(1), N(1.75m) },
        }));

        // Four unknowns, the most the Equation application solves at once.
        _equation = In(CalculatorApp.Equation);
        _system = new[,]
        {
            { N(2), N(1), N(-1), N(3), N(7) },
            { N(1), N(-3), N(2), N(1), N(-2) },
            { N(3), N(2), N(1), N(-1), N(4) },
            { N(1), N(1), N(1), N(1), N(10) },
        };

        _calculate = In(CalculatorApp.Calculate);

        // Two variables at the Standard profile's 80 rows, fitted by the quadratic regression.
        _statistics = In(CalculatorApp.Statistics);
        _pairs = new StatisticsData(
            [.. Enumerable.Range(1, 80).Select(i => N(i * 0.5m))],
            [.. Enumerable.Range(1, 80).Select(i => N((i * i * 0.25m) - (3 * i) + (i % 7)))]);
        _statistics.Regression = RegressionModel.Quadratic;

        _distribution = In(CalculatorApp.Distribution);
        _mathBox = In(CalculatorApp.MathBox);

        // Sixty-one cells: a column of constants, two columns of formulas over it and a formula over those.
        _sheet = new SpreadsheetGrid(In(CalculatorApp.Spreadsheet));
        for (int row = 0; row < 20; row++)
        {
            string constant = (row + 1).ToString(CultureInfo.InvariantCulture);
            Require(_sheet.SetConstant(new CellAddress(0, row), constant) is null, constant);
        }

        Require(_sheet.Fill("A1²+1", new CellAddress(1, 0), new CellAddress(1, 19)) is null, "A1²+1");
        Require(_sheet.Fill("B1÷A1", new CellAddress(2, 0), new CellAddress(2, 19)) is null, "B1÷A1");
        Require(_sheet.SetFormula(new CellAddress(3, 0), "Sum(C1:C20)+Mean(B1:B20)") is null, "Sum(C1:C20)+Mean(B1:B20)");

        Require(MatrixInverse().Succeeded, nameof(MatrixInverse));
        Require(SimultaneousEquations().Succeeded, nameof(SimultaneousEquations));
        Require(RationalQuartic().Succeeded, nameof(RationalQuartic));
        Require(ComplexQuartic().Succeeded, nameof(ComplexQuartic));
        Require(Integral().Succeeded, nameof(Integral));
        Require(QuadraticRegression().Succeeded, nameof(QuadraticRegression));
        Require(BinomialCumulative().Succeeded, nameof(BinomialCumulative));
        Require(NormalCumulative().Succeeded, nameof(NormalCumulative));
        Require(SheetRecalculation() == 61 && _sheet.Cells.All(cell => cell.Error is null), nameof(SheetRecalculation));
        Require(DiceRoll() > 0, nameof(DiceRoll));
    }

    /// <summary>The inverse of a 4×4 matrix of decimals.</summary>
    [Benchmark]
    [Target(16)]
    public Calculation MatrixInverse() => Cleared(_matrix, "MatA⁻¹");

    /// <summary>Four simultaneous equations.</summary>
    [Benchmark]
    [Target(16)]
    public SimultaneousSolution SimultaneousEquations() => _equation.SolveSimultaneous(_system);

    /// <summary>A quartic with four rational roots, recovered exactly.</summary>
    [Benchmark]
    [Target(16)]
    public PolynomialSolution RationalQuartic() => _equation.SolvePolynomial([N(1), N(-2), N(-13), N(14), N(24)]);

    /// <summary>A quartic with complex roots only.</summary>
    [Benchmark]
    [Target(16)]
    public PolynomialSolution ComplexQuartic() => _equation.SolvePolynomial([N(1), N(0), N(0), N(0), N(1)]);

    /// <summary>An integral whose integrand has an infinite slope at one end.</summary>
    [Benchmark]
    [Target(16)]
    public Calculation Integral() => Cleared(_calculate, "∫(√(x)×e^(-x),0,10)");

    /// <summary>The quadratic regression of 80 pairs, fitted again as after the list is edited.</summary>
    /// <remarks>
    /// A session fits its data once and keeps the fit until the data or the model change, so the data is given again
    /// before each calculation: without it, what was measured was the kept fit, 2.6 µs.
    /// </remarks>
    [Benchmark]
    [Target(16)]
    public Calculation QuadraticRegression()
    {
        _statistics.SetStatisticsData(_pairs);
        return Cleared(_statistics, "c");
    }

    /// <summary>The binomial cumulative probability of 100 trials, exact on BigInteger.</summary>
    [Benchmark]
    [Target(16)]
    public Calculation BinomialCumulative() => _distribution.CalculateDistribution(DistributionKind.BinomialCD, new DistributionParameters
    {
        X = N(50), Trials = N(100), Probability = N(0.37m),
    });

    /// <summary>The normal cumulative probability between two bounds.</summary>
    [Benchmark]
    [Target(16)]
    public Calculation NormalCumulative() => _distribution.CalculateDistribution(DistributionKind.NormalCD, new DistributionParameters
    {
        Lower = N(-1.5m), Upper = N(2.25m), Mean = N(0.5m), StandardDeviation = N(1.25m),
    });

    /// <summary>Every formula of the sheet calculated again.</summary>
    [Benchmark]
    [Target(16)]
    public int SheetRecalculation()
    {
        _sheet.Recalculate();
        return _sheet.Cells.Count;
    }

    /// <summary>Three dice thrown 250 times, and the Relative Freq screen of their sums.</summary>
    [Benchmark]
    [Target(16)]
    public int DiceRoll()
    {
        Simulation roll = _mathBox.Simulate(SimulationKind.DiceRoll, 3, N(250));
        return roll.Succeeded ? roll.Frequencies(SimulationTally.Sum).Length : 0;
    }

    private static Calculation Cleared(CalculatorSession session, string input)
    {
        Calculation calculation = session.Calculate(input);
        session.ClearHistory();
        return calculation;
    }

    private static void Require(bool succeeded, string what)
    {
        if (!succeeded)
        {
            throw new InvalidOperationException($"{what} does not calculate: a benchmark measures a result.");
        }
    }
}
