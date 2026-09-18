// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// d/dx, ∫, Σ and Π (manual pp. 51-55): the derivative is exact, the integral reports its error estimate, and both
/// stop within their budget.
/// </summary>
public sealed class CalculusTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian };

    [Fact]
    public void Derivative_IsExactWhereTheOperationsAre()
    {
        // The calculator differentiates numerically with a tolerance; Pallas differentiates the tree (deviation D5).
        Calculator.Display("d/dx(sin(x),π÷2)", settings: Radians).Should().Be("0");
        Calculator.Evaluate("d/dx(x³,0.1)").ToDecimal().Should().Be(0.03m);
        Calculator.Evaluate("d/dx(x³,0.1)").IsExact.Should().BeTrue();
    }

    [Theory]
    [InlineData("d/dx(x²,3)", "6")]
    [InlineData("d/dx(x^4,2)", "32")]
    [InlineData("d/dx(1⌟x,2)", "-1⌟4")]
    [InlineData("d/dx(√(x),4)", "1⌟4")]
    [InlineData("d/dx(ln(x),2)", "1⌟2")]
    [InlineData("d/dx(e^(x),0)", "1")]
    [InlineData("d/dx(x×sin(x),0)", "0")]
    [InlineData("d/dx(x^x,1)", "1")]
    [InlineData("d/dx(Abs(x),-2)", "-1")]
    [InlineData("d/dx(3ˣ√(x),8)", "1⌟12")]
    [InlineData("d/dx(tan(x),0)", "1")]
    [InlineData("d/dx(sin⁻¹(x),0)", "1")]
    [InlineData("d/dx(cosh(x),0)", "0")]
    [InlineData("d/dx(x²+3x-2,1)", "5")]
    public void Derivative_FollowsTheRulesOfCalculus(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Fact]
    public void Derivative_UsesTheAngleUnit()
    {
        // In degrees, sin' is cos × π/180.
        Calculator.Evaluate("d/dx(sin(x),0)").ToDouble().Should().BeApproximately(double.Pi / 180d, 1e-15);
    }

    [Theory]
    // Deviation D5: where there is no derivative, the engine refuses rather than returning a number.
    [InlineData("d/dx(Abs(x),0)")]
    [InlineData("d/dx(√(x),0)")]
    [InlineData("d/dx(Int(x),2)")]
    [InlineData("d/dx(x!,3)")]
    [InlineData("d/dx(ln(x),-1)")]
    [InlineData("d/dx(GCD(x,4),3)")]
    public void Derivative_RefusesWhereThereIsNone(string input)
    {
        Calculator.Error(input, settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void Derivative_OfAConstantSubtree_IsZeroEvenWithoutARule()
    {
        // Ran# has no derivative, but it does not depend on x.
        Calculator.Display("d/dx(x×5!,2)", settings: Radians).Should().Be("120");
        Calculator.Session(settings: Radians).Calculate("d/dx(x+Ran#,2)").Display.Text.Should().Be("1");
    }

    [Fact]
    public void SecondDerivative_IsTheDerivativeOfTheDerivative()
    {
        Calculator.Display("d/dx(d/dx(x³,x),2)", settings: Radians).Should().Be("12");
    }

    [Fact]
    public void Derivative_AcceptsATolerance_ThoughItChangesNothing()
    {
        Calculator.Display("d/dx(x²,3,1×10^-10)", settings: Radians).Should().Be("6");
        Calculator.Error("d/dx(x²,3,1×10^-30)", settings: Radians).Kind.Should().Be(CalcErrorKind.MathError, "the manual asks for tol ≥ 10⁻²² (p. 52)");
    }

    [Theory]
    [InlineData("∫(ln(x),1,e)", "1")]
    [InlineData("∫(x²,0,3)", "9")]
    [InlineData("∫(sin(x),0,π)", "2")]
    [InlineData("∫(1⌟x,1,e)", "1")]
    [InlineData("∫(x²,3,0)", "-9")]
    [InlineData("∫(x,2,2)", "0")]
    public void Integral_MatchesTheExactValue(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Fact]
    public void Integral_ReportsItsErrorEstimate()
    {
        // Invariant I7: a numerical method stands behind its result with an estimate.
        Calculation calculation = Calculator.Session(settings: Radians).Calculate("∫(ln(x),1,e)");

        calculation.Integrals.Should().HaveCount(1);
        calculation.Integrals[0].Result.Should().BeApproximately(1d, 1e-12);
        calculation.Integrals[0].ErrorEstimate.Should().BeLessThan(1e-10);
        calculation.Integrals[0].Span.Length.Should().Be("∫(ln(x),1,e)".Length);
    }

    [Fact]
    public void Integral_HandlesAnEndpointSingularity()
    {
        // The Gauss-Kronrod nodes never touch the end points, so ln(0) is never evaluated.
        Calculator.Evaluate("∫(ln(x),0,1)", settings: Radians).ToDouble().Should().BeApproximately(-1d, 1e-8);
    }

    [Fact]
    public void Integral_RefusesAnIntegrandItCannotEvaluate()
    {
        Calculator.Error("∫(1⌟(x-1),0,2)", settings: Radians).Kind.Should().BeOneOf(CalcErrorKind.MathError, CalcErrorKind.TimeOut);
    }

    [Fact]
    public void Integral_TakesATolerance()
    {
        Calculator.Display("∫(x²,0,3,1×10^-5)", settings: Radians).Should().Be("9");
        Calculator.Error("∫(x²,0,3,0)", settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData("Σ(x+1,1,5)", "20")]
    [InlineData("Π(x+1,1,5)", "720")]
    [InlineData("Σ(x²,1,10)", "385")]
    [InlineData("Σ(1⌟x,1,4)", "25⌟12")]
    [InlineData("Σ(x,-3,3)", "0")]
    [InlineData("Π(x,1,10)", "3628800")]
    public void SeriesAndProducts_AreExactWhileTheTermsAre(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("Σ(x,5,1)")]
    [InlineData("Σ(x,1.5,3)")]
    [InlineData("Σ(x,1,10000000000)")]
    public void SeriesBounds_AreIntegersInOrder(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ALongSeries_StopsAtItsBudget()
    {
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(10_000, TimeSpan.FromSeconds(5));

        session.Calculate("Σ(x,1,1000000)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void ALongCalculation_CanBeCancelled()
    {
        CalculatorSession session = Calculator.Session();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Action act = () => session.Calculate("Σ(x,1,100000000)", cancellation.Token);

        act.Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void CalculusIsNotAvailableInComplexOrBaseN()
    {
        // The Func Analysis commands list the applications they work in (p. 51).
        Calculator.Error("Σ(x,1,5)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("∫(x,1,5)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void NestedCalculus_BindsTheInnermostVariable()
    {
        // Σ over an integral: the inner x belongs to the integral.
        Calculator.Display("Σ(∫(x,0,2),1,3)", settings: Radians).Should().Be("6");
    }
}
