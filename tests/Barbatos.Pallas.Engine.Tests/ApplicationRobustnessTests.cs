// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using CsCheck;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// What every application of Phase 4 promises for any input at all: a result or an error the calculator has a name
/// for, inside the budget, and never an exception.
/// </summary>
/// <remarks>
/// The errors a calculation may end with are those of <see cref="CalcErrorKind"/>; an application form may also refuse
/// an argument before it calculates, which is an exception of the API and is tested where that form is. These
/// properties cover what comes back from a calculation that ran.
/// </remarks>
public sealed class ApplicationRobustnessTests
{
    private const int Samples = 500;

    /// <summary>Values a user could type: decimals of a few digits, and a few extremes of the calculation range.</summary>
    private static readonly Gen<Value> Number = Gen.OneOf(
        Gen.Int[-1000, 1000].Select(whole => Value.FromDecimal(whole)),
        Gen.Int[-10000, 10000].Select(hundredths => Value.FromDecimal(hundredths / 100m)),
        Gen.Const(Value.Zero),
        Gen.Const(Value.FromDouble(1e-99)),
        Gen.Const(Value.FromDouble(9e99)));

    [Fact]
    public void APolynomialAlwaysEndsInRootsOrANamedError()
    {
        Gen.Select(Number, Number, Number, Number, Number)
            .Sample(
                coefficients =>
                {
                    Value[] entered = [coefficients.Item1, coefficients.Item2, coefficients.Item3, coefficients.Item4, coefficients.Item5];
                    CalculatorSession session = Calculator.Session(CalculatorApp.Equation);

                    PolynomialSolution solution = session.SolvePolynomial(entered);

                    if (!solution.Succeeded)
                    {
                        solution.Error!.Value.Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
                        return;
                    }

                    // Every root of a polynomial of degree 4 is one of four, and the extrema belong to degree 2 and 3.
                    solution.Roots.Length.Should().BeLessThanOrEqualTo(4);
                    solution.Extrema.Should().BeEmpty();
                    if (solution.Outcome == SolutionOutcome.Solved)
                    {
                        solution.Roots.Should().NotBeEmpty();
                    }
                },
                iter: Samples);
    }

    [Fact]
    public void AnInequalityAlwaysEndsInIntervalsInOrder()
    {
        Gen.Select(Number, Number, Number)
            .Sample(
                coefficients =>
                {
                    CalculatorSession session = Calculator.Session(CalculatorApp.Inequality);
                    Value[] entered = [coefficients.Item1, coefficients.Item2, coefficients.Item3];

                    InequalitySolution solution = session.SolveInequality(entered, RelationOperator.GreaterOrEqual);

                    if (!solution.Succeeded)
                    {
                        solution.Error!.Value.Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
                        return;
                    }

                    // The stretches run up the line, and no two of them touch: that is what makes them stretches.
                    double previous = double.NegativeInfinity;
                    foreach (SolutionInterval interval in solution.Intervals)
                    {
                        double lower = interval.Lower?.Result.ToDouble() ?? double.NegativeInfinity;
                        double upper = interval.Upper?.Result.ToDouble() ?? double.PositiveInfinity;
                        lower.Should().BeGreaterThanOrEqualTo(previous);
                        upper.Should().BeGreaterThanOrEqualTo(lower);
                        previous = upper;
                    }
                },
                iter: Samples);
    }

    [Fact]
    public void ASystemAlwaysEndsInASolutionOrSaysWhyNot()
    {
        Gen.Select(Number, Number, Number, Number, Number, Number)
            .Sample(
                entries =>
                {
                    CalculatorSession session = Calculator.Session(CalculatorApp.Equation);
                    Value[,] augmented =
                    {
                        { entries.Item1, entries.Item2, entries.Item3 },
                        { entries.Item4, entries.Item5, entries.Item6 },
                    };

                    SimultaneousSolution solution = session.SolveSimultaneous(augmented);

                    if (!solution.Succeeded)
                    {
                        solution.Error!.Value.Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
                        return;
                    }

                    solution.Unknowns.Should().HaveCount(solution.Outcome == SolutionOutcome.Solved ? 2 : 0);
                },
                iter: Samples);
    }

    [Fact]
    public void ADistributionAlwaysEndsInAProbabilityOrANamedError()
    {
        Gen.Select(Gen.Int[0, 6], Number, Number, Number)
            .Sample(
                parameters =>
                {
                    CalculatorSession session = Calculator.Session(CalculatorApp.Distribution);
                    session.Budget = new EngineBudget(MaxIterations: 100_000, TimeSpan.FromSeconds(2));
                    DistributionParameters values = new()
                    {
                        X = parameters.Item2,
                        Trials = parameters.Item3,
                        Probability = parameters.Item4,
                        Mean = parameters.Item2,
                        StandardDeviation = parameters.Item3,
                        Lower = parameters.Item2,
                        Upper = parameters.Item3,
                        Area = parameters.Item4,
                        Lambda = parameters.Item3,
                    };

                    Calculation calculation = session.CalculateDistribution((DistributionKind)parameters.Item1, values);

                    if (!calculation.Succeeded)
                    {
                        calculation.Error!.Value.Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
                        return;
                    }

                    // A probability is between 0 and 1; Inverse Normal answers with an x instead.
                    if (parameters.Item1 != (int)DistributionKind.InverseNormal)
                    {
                        calculation.Result.ToDouble().Should().BeGreaterThanOrEqualTo(0d);
                    }
                },
                iter: Samples);
    }

    [Fact]
    public void AStatisticAlwaysEndsInAValueOrANamedError()
    {
        Gen.Select(Number.Array[1, 6], Number.Array[1, 6])
            .Sample(
                columns =>
                {
                    // The quartiles belong to one-variable data and the regression to two, as the menus of pp. 88-91 do.
                    CalculatorSession session = Calculator.Session(CalculatorApp.Statistics);
                    int rows = Math.Min(columns.Item1.Length, columns.Item2.Length);
                    bool twoVariable = rows == columns.Item1.Length;
                    session.SetStatisticsData(twoVariable
                        ? new StatisticsData(columns.Item1.Take(rows), columns.Item2.Take(rows))
                        : new StatisticsData(columns.Item1));

                    string[] statistics = twoVariable
                        ? ["x̄", "ȳ", "σx", "Σxy", "a", "b", "r"]
                        : ["x̄", "σx", "sx", "Σx²", "n", "Q1", "Med", "Q3", "min(x)"];
                    foreach (string statistic in statistics)
                    {
                        Calculation calculation = session.Calculate(statistic);
                        if (!calculation.Succeeded)
                        {
                            calculation.Error!.Value.Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
                        }
                    }
                },
                iter: 100);
    }

    [Fact]
    public void ALongCalculationIsCancelled()
    {
        // A budget the calculation cannot spend, and a token already cancelled: the token wins, as a key press would.
        CalculatorSession session = Calculator.Session();
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        session.Invoking(s => s.Calculate("Σ(x,1,1000000)", cancelled.Token)).Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void ALongDistributionListIsCancelled()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Distribution);
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();
        DistributionParameters parameters = new() { Trials = Value.FromDecimal(20000), Probability = Value.FromDecimal(0.5m) };

        session.Invoking(s => s.CalculateDistribution(DistributionKind.BinomialCD, parameters, [Value.FromDecimal(10000)], cancelled.Token))
            .Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void ALongSolverIsCancelled()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Equation);
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        session.Invoking(s => s.SolveEquation("Σ(x×y,1,100000)=0", MemoryVariable.X, Value.One, cancelled.Token))
            .Should().Throw<OperationCanceledException>();
    }
}
