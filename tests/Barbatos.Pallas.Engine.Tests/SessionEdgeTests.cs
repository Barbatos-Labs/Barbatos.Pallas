// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The session's corners: how memory travels between applications, and how the special results are stored.
/// </summary>
public sealed class SessionEdgeTests
{
    [Fact]
    public void AnsOutsideCalculate_DoesNotShiftPreAns()
    {
        // PreAns exists only in Calculate (p. 37), so a Complex calculation must not fill it.
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);

        session.Calculate("1+i");
        session.Calculate("2+i");

        session.PreAns.Should().Be(Value.Zero);
        session.Ans.ToComplex().Imaginary.Should().Be(1d);
    }

    [Fact]
    public void SwitchingToTheSameApplication_KeepsTheHistory()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("1+1");

        session.SwitchApp(CalculatorApp.Calculate);

        session.History.Should().HaveCount(1);
    }

    [Fact]
    public void SwitchingApplication_TurnsVerifyOff()
    {
        // Manual p. 74: Verify is off when a calculator application starts.
        CalculatorSession session = Calculator.Session(settings: settings => settings with { Verify = true });

        session.SwitchApp(CalculatorApp.Complex);

        session.Settings.Verify.Should().BeFalse();
    }

    [Fact]
    public void BaseNValuesReachComplexAsDecimals()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);
        session.Calculate("255");
        session.SwitchApp(CalculatorApp.Complex);

        session.Calculate("Ans+i").Display.Text.Should().Be("255+i");
    }

    [Fact]
    public void AVariableHoldingABaseNValueIsADecimalElsewhere()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);
        session.Calculate("100");
        session.Store(MemoryVariable.A);
        session.SwitchApp(CalculatorApp.Calculate);

        session.Calculate("A÷8").Display.Text.Should().Be("25⌟2");
    }

    [Fact]
    public void ComplexValuesCannotEnterBaseN()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);
        session.Calculate("1+i");
        session.SwitchApp(CalculatorApp.BaseN);

        session.Calculate("Ans").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void StoringAValueDirectly_IsSeenByTheNextCalculation()
    {
        CalculatorSession session = Calculator.Session();

        session.SetVariable(MemoryVariable.Z, Value.FromDecimal(7m));

        session.Calculate("z²").Display.Text.Should().Be("49");
        session.GetVariable(MemoryVariable.Z).ToDecimal().Should().Be(7m);
    }

    [Fact]
    public void EveryVariableCanBeReadAndWritten()
    {
        CalculatorSession session = Calculator.Session();

        foreach (MemoryVariable variable in Enum.GetValues<MemoryVariable>())
        {
            session.SetVariable(variable, Value.FromDecimal((int)variable + 1m));
            session.GetVariable(variable).ToDecimal().Should().Be((int)variable + 1m);
        }

        session.Calculate("A+B+C+D+E+F+x+y+z").Display.Text.Should().Be("45");
    }

    [Fact]
    public void SettingAnUndefinedVariable_IsRejected()
    {
        CalculatorSession session = Calculator.Session();

        Action act = () => session.SetVariable((MemoryVariable)(-1), Value.One);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TheRemainderResultIsStoredOnlyWhenItApplies()
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.E, Value.FromDecimal(-1m));
        session.SetVariable(MemoryVariable.F, Value.FromDecimal(-1m));

        session.Calculate("4÷R2");

        session.GetVariable(MemoryVariable.E).ToDecimal().Should().Be(-1m, "an exact division is an ordinary one (p. 56)");
        session.GetVariable(MemoryVariable.F).ToDecimal().Should().Be(-1m);
        session.Ans.ToDecimal().Should().Be(2m);
    }

    [Fact]
    public void AFailedRemainderLeavesTheMemoryAlone()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("5÷R0").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        session.Ans.Should().Be(Value.Zero);
    }

    [Fact]
    public void CoordinatesRefuseWhatTheyCannotConvert()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("Pol(10^99,10^99)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "the radius leaves the range");
        session.Calculate("Rec(1,10^10)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "the angle leaves its domain");
    }

    [Fact]
    public void PolarOfTheOrigin_IsZero()
    {
        CalculatorSession session = Calculator.Session();

        Calculation calculation = session.Calculate("Pol(0,0)");

        calculation.Display.Text.Should().Be("r=0, θ=0");
        session.GetVariable(MemoryVariable.X).Should().Be(Value.Zero);
    }

    [Fact]
    public void PolarAndRectangularAreNotAvailableWithVerifyOn()
    {
        // Manual p. 62: Pol( and Rec( work in Calculate with Verify off.
        Calculator.Error("Pol(1,1)", settings: settings => settings with { Verify = true }).Kind.Should().Be(CalcErrorKind.NoOperator);
    }

    [Fact]
    public void AVerifyResultIsAlsoAValue()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { Verify = true });

        Calculation calculation = session.Calculate("1<2");

        calculation.Result.ToDecimal().Should().Be(1m);
        calculation.Second.Should().BeNull();
        session.Format(calculation, FormatTarget.DecimalValue)!.Text.Should().Be("True", "a Verify result has one display");
    }

    [Fact]
    public void TheHistoryKeepsFailedCalculationsToo()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("1÷0");

        session.History.Should().HaveCount(1);
        session.History[0].Succeeded.Should().BeFalse();
    }

    [Fact]
    public void TheBudgetCanBeChangedPerSession()
    {
        CalculatorSession session = Calculator.Session();
        EngineBudget budget = new(10, TimeSpan.FromSeconds(1));

        session.Budget = budget;

        session.Budget.Should().BeSameAs(budget);
        session.Calculate("Σ(x,1,100)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }
}
