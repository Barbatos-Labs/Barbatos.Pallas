// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Equation application (manual pp. 114-124): simultaneous equations, polynomials and the Solver.
/// </summary>
public sealed class EquationTests
{
    private static CalculatorSession Session(CalculatorProfile profile = CalculatorProfile.Standard) => Calculator.Session(CalculatorApp.Equation, profile);

    private static Value[] Numbers(params decimal[] values) => [.. values.Select(Value.FromDecimal)];

    private static Value[,] System(params decimal[][] rows)
    {
        Value[,] augmented = new Value[rows.Length, rows[0].Length];
        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                augmented[row, column] = Value.FromDecimal(rows[row][column]);
            }
        }

        return augmented;
    }

    private static string[] Roots(PolynomialSolution solution)
    {
        return
        [
            .. solution.Roots.Select(root => root.IsReal
                ? root.Real.Display.Text
                : root.Real.Display.Text + (root.Imaginary!.Display.Text.StartsWith('-') ? string.Empty : "+") + root.Imaginary.Display.Text + "i"),
        ];
    }

    [Fact]
    public void TheSimultaneousExampleOfTheManual()
    {
        // p. 115: x − y + z = 2, x + y − z = 0, −x + y + z = 4 gives x = 1, y = 2, z = 3.
        SimultaneousSolution solution = Session().SolveSimultaneous(System([1, -1, 1, 2], [1, 1, -1, 0], [-1, 1, 1, 4]));

        solution.Outcome.Should().Be(SolutionOutcome.Solved);
        solution.Unknowns.Select(unknown => unknown.Input).Should().Equal("x", "y", "z");
        solution.Unknowns.Select(unknown => unknown.Display.Text).Should().Equal("1", "2", "3");
        solution.Error.Should().BeNull();
        solution.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void ASolutionThatIsAFraction_KeepsItsForm()
    {
        // 2x + 3y = 1, 4x − y = 2: x = 1/2, y = 0 exactly, which decimal elimination would not give.
        SimultaneousSolution solution = Session().SolveSimultaneous(System([2, 3, 1], [4, -1, 2]));

        solution.Unknowns.Select(unknown => unknown.Display.Text).Should().Equal("1⌟2", "0");
        solution.Unknowns[0].Result.IsExact.Should().BeTrue();
    }

    [Fact]
    public void FourUnknownsAreNamedXYZAndT()
    {
        SimultaneousSolution solution = Session().SolveSimultaneous(System(
            [1, 0, 0, 0, 5],
            [0, 1, 0, 0, 6],
            [0, 0, 1, 0, 7],
            [0, 0, 0, 1, 8]));

        solution.Unknowns.Select(unknown => unknown.Input).Should().Equal("x", "y", "z", "t");
        solution.Unknowns.Select(unknown => unknown.Display.Text).Should().Equal("5", "6", "7", "8");
    }

    [Fact]
    public void ASystemWithoutASolution_AndOneWithTooMany()
    {
        // x + y = 1 with 2x + 2y = 3 contradicts itself; with 2x + 2y = 2 it is the same equation twice (p. 116).
        Session().SolveSimultaneous(System([1, 1, 1], [2, 2, 3])).Outcome.Should().Be(SolutionOutcome.NoSolution);
        Session().SolveSimultaneous(System([1, 1, 1], [2, 2, 2])).Outcome.Should().Be(SolutionOutcome.InfiniteSolutions);
        Session().SolveSimultaneous(System([1, 1, 1], [2, 2, 2])).Unknowns.Should().BeEmpty();
    }

    [Fact]
    public void ACoefficientThatIsNotReal_IsAMathError()
    {
        Value[,] augmented = System([1, 1, 1], [2, 3, 4]);
        augmented[0, 0] = Calculator.Evaluate("2+3i", CalculatorApp.Complex);

        SimultaneousSolution solution = Session().SolveSimultaneous(augmented);

        solution.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        solution.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TheQuadraticExampleOfTheManual()
    {
        // pp. 122-123: x² + 2x − 2 has the roots −1 ± √3 and its minimum at (−1, −3).
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, 2, -2));

        Roots(solution).Should().Equal("-1+√(3)", "-1-√(3)");
        solution.Roots.Select(root => root.Real.Input).Should().Equal("x₁", "x₂");
        solution.Extrema.Should().ContainSingle();
        solution.Extrema[0].Kind.Should().Be(ExtremumKind.Minimum);
        solution.Extrema[0].X.Display.Text.Should().Be("-1");
        solution.Extrema[0].Y.Display.Text.Should().Be("-3");
    }

    [Fact]
    public void AQuadraticWithComplexRoots()
    {
        // p. 118: 2x² + 3x + 4 has the roots −3/4 ± (√23/4)i and its minimum at (−3/4, 23/8).
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(2, 3, 4));

        Roots(solution).Should().Equal("-3⌟4+√(23)⌟4i", "-3⌟4-√(23)⌟4i");
        solution.Roots.Should().OnlyContain(root => !root.IsReal);
        solution.Roots[0].Imaginary!.Input.Should().Be("x₁i");
        solution.Extrema[0].Y.Display.Text.Should().Be("23⌟8");
    }

    [Fact]
    public void ComplexRootsOff_LeavesOnlyRealRoots()
    {
        CalculatorSession session = Session();
        session.Settings = session.Settings with { ComplexRoots = false };

        // 2x² + 3x + 4 has no real root; x³ − x² + x − 1 = (x − 1)(x² + 1) has one (p. 118).
        session.SolvePolynomial(Numbers(2, 3, 4)).Outcome.Should().Be(SolutionOutcome.NoRealRoots);
        session.SolvePolynomial(Numbers(2, 3, 4)).Roots.Should().BeEmpty();
        Roots(session.SolvePolynomial(Numbers(1, -1, 1, -1))).Should().Equal("1");
    }

    [Fact]
    public void ACubicThatFactorsOverTheRationals_KeepsExactRoots()
    {
        // x³ − 2x² − 5x + 6 = (x − 1)(x + 2)(x − 3), by decreasing root.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, -2, -5, 6));

        Roots(solution).Should().Equal("3", "1", "-2");
        solution.Roots.Should().OnlyContain(root => root.Real.Result.IsExact);
    }

    [Fact]
    public void ACubicWithARationalRootAndASurdPair()
    {
        // 2x³ − x² − 4x + 2 = (2x − 1)(x² − 2): the rational root is exact and so are ±√2.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(2, -1, -4, 2));

        Roots(solution).Should().Equal("√(2)", "1⌟2", "-√(2)");
    }

    [Fact]
    public void ACubicWithNoRationalRoot_IsIterated()
    {
        // x³ − 2 has one real root, ∛2 = 1.259921049894873…, and a conjugate pair.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, 0, 0, -2));

        solution.Roots.Should().HaveCount(3);
        solution.Roots.Count(root => root.IsReal).Should().Be(1);
        solution.Roots.Single(root => root.IsReal).Real.Result.ToDouble().Should().BeApproximately(Math.Cbrt(2d), 1e-14d);
        solution.Roots.Where(root => !root.IsReal).Select(root => root.Imaginary!.Result.ToDouble()).Should().BeInDescendingOrder();
    }

    [Fact]
    public void AQuarticThatIsASquare_RepeatsItsRoots()
    {
        // (x² − 2)² = x⁴ − 4x² + 4: √2 and −√2, each of them twice, and exactly.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, 0, -4, 0, 4));

        Roots(solution).Should().Equal("√(2)", "√(2)", "-√(2)", "-√(2)");
        solution.Extrema.Should().BeEmpty();
    }

    [Fact]
    public void AQuarticWithNoRationalRoot_IsIterated()
    {
        // x⁴ − 10x² + 1 has the four real roots ±√2 ± √3, none of them rational.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, 0, -10, 0, 1));

        solution.Roots.Should().OnlyContain(root => root.IsReal);
        solution.Roots.Select(root => root.Real.Result.ToDouble()).Should().BeInDescendingOrder();
        solution.Roots[0].Real.Result.ToDouble().Should().BeApproximately(Math.Sqrt(2d) + Math.Sqrt(3d), 1e-13d);
    }

    [Fact]
    public void ACubicHasAMaximumAndAMinimum()
    {
        // x³ − 3x rises to (−1, 2) and falls to (1, −2); x³ has neither ("No Local Max/Min", p. 117).
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(1, 0, -3, 0));

        solution.Extrema.Select(extremum => extremum.Kind).Should().Equal(ExtremumKind.Maximum, ExtremumKind.Minimum);
        solution.Extrema.Select(extremum => extremum.X.Display.Text).Should().Equal("-1", "1");
        solution.Extrema.Select(extremum => extremum.Y.Display.Text).Should().Equal("2", "-2");
        Session().SolvePolynomial(Numbers(1, 0, 0, 0)).Extrema.Should().BeEmpty();
    }

    [Fact]
    public void ACubicThatFalls_HasItsMinimumFirst()
    {
        // −x³ + 3x falls to (−1, −2) and rises to (1, 2): the extrema come by increasing x, whichever they are.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(-1, 0, 3, 0));

        solution.Extrema.Select(extremum => extremum.Kind).Should().Equal(ExtremumKind.Minimum, ExtremumKind.Maximum);
        solution.Extrema.Select(extremum => extremum.X.Display.Text).Should().Equal("-1", "1");
    }

    [Fact]
    public void AQuadraticThatOpensDownward_HasAMaximum()
    {
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(-1, 0, 4));

        solution.Extrema[0].Kind.Should().Be(ExtremumKind.Maximum);
        solution.Extrema[0].Y.Display.Text.Should().Be("4");
        Roots(solution).Should().Equal("2", "-2");
    }

    [Fact]
    public void ALeadingCoefficientOfZero_IsAMathError()
    {
        // The degree is the one the application asks for, so a leading 0 is no polynomial of it (assumption U25).
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(0, 1, 2));

        solution.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        solution.Succeeded.Should().BeFalse();
        solution.Roots.Should().BeEmpty();
    }

    [Fact]
    public void ADecimalPolynomialIsScaledToIntegersExactly()
    {
        // 0.1x² − 0.3x + 0.2 = 0.1(x − 1)(x − 2): the roots are exact, although the coefficients are not integers.
        PolynomialSolution solution = Session().SolvePolynomial(Numbers(0.1m, -0.3m, 0.2m));

        Roots(solution).Should().Equal("2", "1");
        solution.Roots.Should().OnlyContain(root => root.Real.Result.IsExact);
    }

    [Fact]
    public void AnApproximateCoefficientMakesAnApproximateAnswer()
    {
        // A coefficient held as double is approximate, and so is everything computed from it.
        Value[,] augmented = System([1, 1, 2], [1, -1, 0]);
        augmented[0, 2] = Value.FromDouble(2e-20);

        Session().SolveSimultaneous(augmented).Unknowns.Should().OnlyContain(unknown => !unknown.Result.IsExact);
        Session().SolvePolynomial([Value.FromDouble(1e-20), Value.FromDecimal(0), Value.FromDecimal(-1)])
            .Roots.Should().OnlyContain(root => !root.Real.Result.IsExact);
    }

    [Fact]
    public void ACoefficientThatIsNotReal_IsAMathErrorForAPolynomialToo()
    {
        Value complex = Calculator.Evaluate("2+3i", CalculatorApp.Complex);

        Session().SolvePolynomial([complex, Value.One, Value.One]).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().SolvePolynomial([Value.One, complex, Value.One]).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void RootsBeyondTheCalculationRange_AreAMathError()
    {
        // p. 169: the Standard range ends at 9.999999999×10⁹⁹. 10⁻⁹⁹x² + 10¹⁰x has the root −10¹⁰⁹, and
        // (x − 10¹²⁰)(x² + 1) has one that the iteration finds instead of recovering it as a rational.
        Value tiny = Value.FromDouble(1e-99);
        Value large = Value.FromDouble(-1e120);

        Session().SolvePolynomial([tiny, Value.FromDecimal(1e10m), Value.Zero]).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().SolvePolynomial([Value.One, large, Value.One, large]).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        // The Extended profile reaches as far as double does, so the same roots are results there.
        Session(CalculatorProfile.Extended).SolvePolynomial([Value.One, large, Value.Zero, Value.One, large]).Succeeded.Should().BeTrue();
        Session().SolvePolynomial([Value.One, large, Value.Zero, Value.One, large]).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheSolverExampleOfTheManual()
    {
        // p. 120: x² − B² = 0 with B = 4, from x = 1, converges to 4 with Left − Right exactly 0.
        CalculatorSession session = Session();
        session.SetVariable(MemoryVariable.B, Value.FromDecimal(4));

        Calculation solved = session.SolveEquation("x²-B²=0", MemoryVariable.X, Value.One);

        solved.Display.Text.Should().Be("4");
        solved.Kind.Should().Be(CalculationKind.Solution);
        solved.Second!.Value.ToDouble().Should().Be(0d);
        session.GetVariable(MemoryVariable.X).Should().Be(solved.Result);
    }

    [Fact]
    public void AnExpressionWithoutAnEqualsSign_StandsForItBeingZero()
    {
        Calculation solved = Session().SolveEquation("x³-8", MemoryVariable.X, Value.FromDecimal(3));

        solved.Display.Text.Should().Be("2");
        solved.Second!.Value.ToDouble().Should().Be(0d);
    }

    [Fact]
    public void ASolutionThatIsNoDecimal_ConvergesToItsDigits()
    {
        // x² = 2 from 1: √2 is no decimal, so the steps fall below 10⁻¹⁴, where the precision rule continues them in
        // double (PRECISION.md §3) - the solution is then as accurate as double, which is past the ten digits displayed.
        Calculation solved = Session().SolveEquation("x²=2", MemoryVariable.X, Value.One);

        solved.Result.ToDouble().Should().BeApproximately(Math.Sqrt(2d), 1e-14d);

        // MathI/MathO recognizes the form from the value, as the calculator does from its own (CALCULATOR-CATALOG.md §9).
        solved.Display.Text.Should().Be("√(2)");
    }

    [Fact]
    public void AnEquationWithoutTheVariable_IsAVariableError()
    {
        // p. 165: the Solver needs the variable it solves for.
        Calculation solved = Session().SolveEquation("2+3=5", MemoryVariable.X, Value.One);

        solved.Error!.Value.Kind.Should().Be(CalcErrorKind.VariableError);
    }

    [Fact]
    public void AnEquationTheIterationCannotSettle_IsCannotSolve()
    {
        // e^x = 0 has no solution: the iteration walks off to the left until the step no longer helps.
        Session().SolveEquation("e^(x)=0", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.CannotSolve);

        // A flat function has no slope to follow at all.
        Session().SolveEquation("0×x=2", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.CannotSolve);
    }

    [Fact]
    public void AnEquationThatDoesNotParseOrBind()
    {
        Session().SolveEquation("x²+", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        Session().SolveEquation("1<x<3", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        Session().SolveEquation("x≠2", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AnEquationThatFailsWhereItIsEvaluated()
    {
        // √(x) below 0 is a Math ERROR, and the iteration reports it where it happened.
        Session().SolveEquation("√(x)=-1", MemoryVariable.X, Value.FromDecimal(-1)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ASolverBudgetThatRunsOut_IsATimeOut()
    {
        CalculatorSession session = Session();
        session.Budget = new EngineBudget(MaxIterations: 2, session.Budget.Timeout);

        session.SolveEquation("x²=2", MemoryVariable.X, Value.One).Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void TheEquationApplicationRefusesWhatItDoesNotSolve()
    {
        CalculatorSession equation = Session();
        CalculatorSession calculate = Calculator.Session();

        equation.Invoking(session => session.SolveSimultaneous(System([1, 1, 1]))).Should().Throw<ArgumentException>().WithParameterName("augmented");
        equation.Invoking(session => session.SolveSimultaneous(new Value[2, 2])).Should().Throw<ArgumentException>().WithParameterName("augmented");
        equation.Invoking(session => session.SolveSimultaneous(null!)).Should().Throw<ArgumentNullException>();
        equation.Invoking(session => session.SolvePolynomial(Numbers(1, 2))).Should().Throw<ArgumentException>().WithParameterName("coefficients");
        equation.Invoking(session => session.SolvePolynomial(Numbers(1, 2, 3, 4, 5, 6))).Should().Throw<ArgumentException>().WithParameterName("coefficients");
        equation.Invoking(session => session.SolvePolynomial(null!)).Should().Throw<ArgumentNullException>();
        equation.Invoking(session => session.SolveEquation(null!, MemoryVariable.X, Value.One)).Should().Throw<ArgumentNullException>();
        equation.Invoking(session => session.SolveEquation("x=1", (MemoryVariable)99, Value.One)).Should().Throw<ArgumentOutOfRangeException>();

        calculate.Invoking(session => session.SolveSimultaneous(System([1, 1, 1], [2, 2, 3]))).Should().Throw<InvalidOperationException>();
        calculate.Invoking(session => session.SolvePolynomial(Numbers(1, 2, 3))).Should().Throw<InvalidOperationException>();
        calculate.Invoking(session => session.SolveEquation("x=1", MemoryVariable.X, Value.One)).Should().Throw<InvalidOperationException>();
    }
}
