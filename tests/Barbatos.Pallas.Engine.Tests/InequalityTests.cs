// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Inequality application (manual pp. 124-125): the sign of a polynomial between its real roots.
/// </summary>
public sealed class InequalityTests
{
    private static CalculatorSession Session() => Calculator.Session(CalculatorApp.Inequality);

    private static Value[] Numbers(params decimal[] values) => [.. values.Select(Value.FromDecimal)];

    private static InequalitySolution Solve(RelationOperator relation, params decimal[] coefficients)
    {
        return Session().SolveInequality(Numbers(coefficients), relation);
    }

    [Theory]
    // x² + 2x − 3 = (x + 3)(x − 1), the example of p. 124, with each of the four relations.
    [InlineData(RelationOperator.GreaterOrEqual, "x≤-3, 1≤x")]
    [InlineData(RelationOperator.Greater, "x<-3, 1<x")]
    [InlineData(RelationOperator.LessOrEqual, "-3≤x≤1")]
    [InlineData(RelationOperator.Less, "-3<x<1")]
    public void TheExampleOfTheManualWithEachRelation(RelationOperator relation, string expected)
    {
        InequalitySolution solution = Solve(relation, 1, 2, -3);

        solution.Outcome.Should().Be(SolutionOutcome.Solved);
        solution.Text.Should().Be(expected);
        solution.Succeeded.Should().BeTrue();
        solution.Error.Should().BeNull();
    }

    [Fact]
    public void TheIntervalsCarryTheirBounds()
    {
        InequalitySolution solution = Solve(RelationOperator.GreaterOrEqual, 1, 2, -3);

        solution.Intervals.Should().HaveCount(2);
        solution.Intervals[0].Lower.Should().BeNull();
        solution.Intervals[0].Upper!.Display.Text.Should().Be("-3");
        solution.Intervals[0].UpperIncluded.Should().BeTrue();
        solution.Intervals[1].Lower!.Display.Text.Should().Be("1");
        solution.Intervals[1].LowerIncluded.Should().BeTrue();
        solution.Intervals[1].Upper.Should().BeNull();
        solution.Intervals[1].ToString().Should().Be("1≤x");
    }

    [Fact]
    public void NothingAndEverything()
    {
        // p. 125: x² < 0 has no solution, and x² ≥ 0 is every real number.
        Solve(RelationOperator.Less, 1, 0, 0).Outcome.Should().Be(SolutionOutcome.NoSolution);
        Solve(RelationOperator.Less, 1, 0, 0).Text.Should().BeEmpty();
        Solve(RelationOperator.Less, 1, 0, 0).Intervals.Should().BeEmpty();
        Solve(RelationOperator.GreaterOrEqual, 1, 0, 0).Outcome.Should().Be(SolutionOutcome.AllRealNumbers);
        Solve(RelationOperator.GreaterOrEqual, 1, 0, 0).Intervals.Should().BeEmpty();
    }

    [Fact]
    public void ARootThatIsTheWholeSolution()
    {
        // x² ≤ 0 holds at 0 and nowhere else, which the calculator writes as a point.
        InequalitySolution solution = Solve(RelationOperator.LessOrEqual, 1, 0, 0);

        solution.Outcome.Should().Be(SolutionOutcome.Solved);
        solution.Text.Should().Be("x=0");
        solution.Intervals[0].LowerIncluded.Should().BeTrue();
        solution.Intervals[0].UpperIncluded.Should().BeTrue();
    }

    [Fact]
    public void ADoubleRootThatIsCutOut()
    {
        // (x − 1)² > 0 holds on both sides of 1 but not at it, so the solution is two intervals, not one.
        Solve(RelationOperator.Greater, 1, -2, 1).Text.Should().Be("x<1, 1<x");
        Solve(RelationOperator.GreaterOrEqual, 1, -2, 1).Outcome.Should().Be(SolutionOutcome.AllRealNumbers);
    }

    [Fact]
    public void APolynomialWithoutRealRoots()
    {
        // x² + 1 is positive everywhere.
        Solve(RelationOperator.Greater, 1, 0, 1).Outcome.Should().Be(SolutionOutcome.AllRealNumbers);
        Solve(RelationOperator.LessOrEqual, 1, 0, 1).Outcome.Should().Be(SolutionOutcome.NoSolution);
    }

    [Fact]
    public void ACubicHasThreeStretches()
    {
        // x³ − 2x² − 5x + 6 = (x − 1)(x + 2)(x − 3) is negative below −2 and between 1 and 3.
        Solve(RelationOperator.Less, 1, -2, -5, 6).Text.Should().Be("x<-2, 1<x<3");
        Solve(RelationOperator.GreaterOrEqual, 1, -2, -5, 6).Text.Should().Be("-2≤x≤1, 3≤x");
    }

    [Fact]
    public void AQuarticWithSurdRoots()
    {
        // x⁴ − 5x² + 6 = (x² − 2)(x² − 3) is negative between the surds, which the bounds display as they are.
        InequalitySolution solution = Solve(RelationOperator.Less, 1, 0, -5, 0, 6);

        solution.Text.Should().Be("-√(3)<x<-√(2), √(2)<x<√(3)");
    }

    [Fact]
    public void ADecimalInequalityIsScaledToIntegersExactly()
    {
        // 0.1x² − 0.3x + 0.2 = 0.1(x − 1)(x − 2) is negative between 1 and 2.
        Solve(RelationOperator.Less, 0.1m, -0.3m, 0.2m).Text.Should().Be("1<x<2");
    }

    [Fact]
    public void ALeadingCoefficientOfZero_IsAMathError()
    {
        InequalitySolution solution = Solve(RelationOperator.Less, 0, 1, 2);

        solution.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        solution.Succeeded.Should().BeFalse();
        solution.Intervals.Should().BeEmpty();
    }

    [Fact]
    public void TheListSeparatorFollowsTheDecimalMark()
    {
        // p. 24: with a decimal comma the list separator is a semicolon.
        CalculatorSession session = Session();
        session.Settings = session.Settings with { DecimalMark = DecimalMark.Comma };

        session.SolveInequality(Numbers(1, 2, -3), RelationOperator.GreaterOrEqual).Text.Should().Be("x≤-3; 1≤x");
    }

    [Fact]
    public void TheInequalityApplicationRefusesWhatItDoesNotSolve()
    {
        CalculatorSession inequality = Session();
        CalculatorSession calculate = Calculator.Session();

        inequality.Invoking(session => session.SolveInequality(Numbers(1, 2), RelationOperator.Less)).Should().Throw<ArgumentException>().WithParameterName("coefficients");
        inequality.Invoking(session => session.SolveInequality(Numbers(1, 2, 3, 4, 5, 6), RelationOperator.Less)).Should().Throw<ArgumentException>().WithParameterName("coefficients");
        inequality.Invoking(session => session.SolveInequality(null!, RelationOperator.Less)).Should().Throw<ArgumentNullException>();
        inequality.Invoking(session => session.SolveInequality(Numbers(1, 2, 3), RelationOperator.Equal)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("relation");
        inequality.Invoking(session => session.SolveInequality(Numbers(1, 2, 3), RelationOperator.NotEqual)).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("relation");
        calculate.Invoking(session => session.SolveInequality(Numbers(1, 2, 3), RelationOperator.Less)).Should().Throw<InvalidOperationException>();
    }
}
