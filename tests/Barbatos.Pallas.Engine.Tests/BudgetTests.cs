// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The budget of a calculation (docs/PRECISION.md §11): a named Time Out, never a hang, and never a silent stop.
/// </summary>
public sealed class BudgetTests
{
    private static CalculatorSession Session(long iterations, double seconds = 30d)
    {
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(iterations, TimeSpan.FromSeconds(seconds));
        return session;
    }

    [Fact]
    public void ASeriesOfExactlyTheBudget_StillFinishes()
    {
        // 100 terms with a budget of 100 iterations is the last calculation that fits.
        Session(100).Calculate("Σ(x,1,100)").Display.Text.Should().Be("5050");
        Session(99).Calculate("Σ(x,1,100)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void TheBudgetIsSharedBySeveralLoopsOfOneCalculation()
    {
        Session(100).Calculate("Σ(x,1,50)+Σ(x,1,50)").Display.Text.Should().Be("2550");
        Session(99).Calculate("Σ(x,1,50)+Σ(x,1,50)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void EachCalculationStartsWithItsOwnBudget()
    {
        CalculatorSession session = Session(100);

        session.Calculate("Σ(x,1,100)").Succeeded.Should().BeTrue();
        session.Calculate("Σ(x,1,100)").Succeeded.Should().BeTrue("the budget is per calculation, not per session");
    }

    [Fact]
    public void ATimeoutStopsALongCalculation()
    {
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(long.MaxValue, TimeSpan.FromMilliseconds(50));

        Calculation calculation = session.Calculate("Σ(x,1,100000000)");

        calculation.Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void AnIntegralCountsFifteenEvaluationsPerInterval()
    {
        // The Kronrod rule evaluates the integrand fifteen times; a budget below that cannot even start.
        CalculatorSession session = Session(14);
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        session.Calculate("∫(x,0,1)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);

        CalculatorSession enough = Session(15);
        enough.Settings = enough.Settings with { AngleUnit = AngleUnit.Radian };
        enough.Calculate("∫(x,0,1)").Display.Text.Should().Be("1⌟2");
    }

    [Fact]
    public void ADerivativeCostsNoIterations()
    {
        // The derivative is an expression, not a loop: it needs no budget beyond evaluating it.
        Session(1).Calculate("d/dx(x²,3)").Display.Text.Should().Be("6");
    }

    [Fact]
    public void CancellationLeavesNoResultBehind()
    {
        CalculatorSession session = Calculator.Session();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Action act = () => session.Calculate("Σ(x,1,100000000)", cancellation.Token);

        act.Should().Throw<OperationCanceledException>();
        session.Ans.Should().Be(Value.Zero);
        session.History.Should().BeEmpty("a cancelled calculation is not history");
    }

    [Fact]
    public void ABudgetlessCalculationIsStillBounded()
    {
        // The default budget is what a session starts with; a calculation that would run forever still ends.
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(1_000, TimeSpan.FromSeconds(30));

        session.Calculate("Π(1,1,10000)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }
}
