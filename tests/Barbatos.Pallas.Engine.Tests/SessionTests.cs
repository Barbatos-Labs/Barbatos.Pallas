// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The calculator's memory and session behavior: Ans, PreAns, variables, CALC, defined functions and history.
/// </summary>
public sealed class SessionTests
{
    [Fact]
    public void Ans_HoldsTheLastResult()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("3×4").Display.Text.Should().Be("12");
        session.Calculate("Ans÷30").Display.Text.Should().Be("2⌟5");
        session.Ans.ToDecimal().Should().Be(0.4m);
    }

    [Fact]
    public void PreAns_HoldsTheResultBeforeIt()
    {
        // The Fibonacci example of p. 37.
        CalculatorSession session = Calculator.Session();
        session.Calculate("1");
        session.Calculate("1");

        session.Calculate("Ans+PreAns").Display.Text.Should().Be("2");
        session.Calculate("Ans+PreAns").Display.Text.Should().Be("3");
        session.Calculate("Ans+PreAns").Display.Text.Should().Be("5");
    }

    [Fact]
    public void PreAns_ExistsOnlyInCalculate()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);

        session.Calculate("PreAns+1").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AnErroneousCalculation_LeavesAnsAlone()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("2+2");

        session.Calculate("1÷0").Error.Should().NotBeNull();

        session.Ans.ToDecimal().Should().Be(4m);
    }

    [Fact]
    public void Store_KeepsAResultInAVariable()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("3+5");
        session.Store(MemoryVariable.A);

        session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(8m);
        session.Calculate("A×10").Display.Text.Should().Be("80");
    }

    [Fact]
    public void Variables_RejectUndefinedValues()
    {
        CalculatorSession session = Calculator.Session();

        Action act = () => session.GetVariable((MemoryVariable)42);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Calc_SubstitutesValuesIntoAnExpression()
    {
        // Manual p. 40: 3A + B with A = 5 and B = 10 is 25.
        CalculatorSession session = Calculator.Session();

        Calculation calculation = session.Calculate("3A+B", new Dictionary<MemoryVariable, Value>
        {
            [MemoryVariable.A] = Value.FromDecimal(5m),
            [MemoryVariable.B] = Value.FromDecimal(10m),
        });

        calculation.Display.Text.Should().Be("25");
        session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(5m);
    }

    [Fact]
    public void DefinedFunctions_AreCalledByName()
    {
        // Manual pp. 70-71: f(x) = x² + 1, then g(x) = f(x) × 2 − x.
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "x²+1").Should().BeNull();
        session.Define(DefinedFunction.G, "f(x)×2-x").Should().BeNull();

        session.Calculate("f(0)").Display.Text.Should().Be("1");
        session.Calculate("f(5)").Display.Text.Should().Be("26");
        session.Calculate("g(3)").Display.Text.Should().Be("17");
    }

    [Fact]
    public void DefinedFunctions_DoNotDisturbTheVariableX()
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.X, Value.FromDecimal(7m));
        session.Define(DefinedFunction.F, "x²");

        session.Calculate("f(3)").Display.Text.Should().Be("9");
        session.GetVariable(MemoryVariable.X).ToDecimal().Should().Be(7m);
    }

    [Fact]
    public void UndefinedFunctions_AreNotDefined()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void MutuallyReferringFunctions_AreACircularError()
    {
        // Manual p. 72.
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "g(x)");
        session.Define(DefinedFunction.G, "f(x)");

        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.CircularError);
    }

    [Fact]
    public void ADefinitionWithASyntaxError_IsRefused()
    {
        CalculatorSession session = Calculator.Session();

        session.Define(DefinedFunction.F, "x²+")!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void Undefine_RemovesADefinition()
    {
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "x+1");
        session.Undefine(DefinedFunction.F);

        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void History_KeepsEveryCalculation()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("4×3+2");
        session.Calculate("4×3-7");

        session.History.Select(calculation => calculation.Display.Text).Should().Equal("14", "5");

        session.ClearHistory();
        session.History.Should().BeEmpty();
    }

    [Fact]
    public void SwitchingApplication_ClearsPreAnsAndHistoryButKeepsMemory()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("1");
        session.Calculate("2");
        session.Store(MemoryVariable.A);

        session.SwitchApp(CalculatorApp.Complex);

        session.App.Should().Be(CalculatorApp.Complex);
        session.PreAns.Should().Be(Value.Zero);
        session.Ans.ToDecimal().Should().Be(2m);
        session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(2m);
        session.History.Should().BeEmpty();
    }

    [Fact]
    public void EveryApplication_HasItsEngine()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();

        foreach (CalculatorApp app in Enum.GetValues<CalculatorApp>())
        {
            engine.CreateSession(app).App.Should().Be(app);
            CalculatorSession session = engine.CreateSession();
            session.SwitchApp(app);
            session.App.Should().Be(app);
        }
    }

    [Fact]
    public void AnApplicationThatIsNotOne_IsRefused()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();

        Action create = () => engine.CreateSession((CalculatorApp)99);
        Action switchApp = () => engine.CreateSession().SwitchApp((CalculatorApp)99);

        create.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("app");
        switchApp.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("app");
    }

    [Fact]
    public void AComplexValueInAVariable_CannotBeUsedInCalculate()
    {
        // Manual p. 163: a variable holding a complex number is a Math ERROR outside Complex.
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);
        session.Calculate("2+3i");
        session.Store(MemoryVariable.A);
        session.SwitchApp(CalculatorApp.Calculate);

        session.Calculate("A+1").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void Remainder_StoresTheQuotientInEAndTheRemainderInF()
    {
        // Manual p. 56: 5 ÷R 2 is quotient 2, remainder 1; only the quotient goes to Ans.
        CalculatorSession session = Calculator.Session();
        Calculation calculation = session.Calculate("5÷R2");

        calculation.Kind.Should().Be(CalculationKind.Remainder);
        calculation.Display.Text.Should().Be("2, R=1");
        session.GetVariable(MemoryVariable.E).ToDecimal().Should().Be(2m);
        session.GetVariable(MemoryVariable.F).ToDecimal().Should().Be(1m);
        session.Ans.ToDecimal().Should().Be(2m);
    }

    [Theory]
    // A quotient that is not a positive integer, or no remainder, makes it an ordinary division (p. 56).
    [InlineData("4÷R2", "2")]
    [InlineData("-5÷R2", "-5⌟2")]
    [InlineData("5.5÷R2", "2, R=3⌟2")]
    [InlineData("10000000000÷R3", "3333333333")]
    public void Remainder_FallsBackToOrdinaryDivision(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void RemainderInsideAnExpression_PassesOnTheQuotient()
    {
        // Assumption U13: only the quotient continues into the larger calculation.
        Calculator.Display("10+17÷R6").Should().Be("12");
    }

    [Fact]
    public void PolarAndRectangular_StoreTheirResultsInXAndY()
    {
        // Manual p. 62: Pol(√2, √2) is r = 2, θ = 45; Rec(√2, 45) is x = 1, y = 1.
        CalculatorSession session = Calculator.Session();

        Calculation polar = session.Calculate("Pol(√(2),√(2))");
        polar.Kind.Should().Be(CalculationKind.Polar);
        polar.Display.Text.Should().Be("r=2, θ=45");
        session.GetVariable(MemoryVariable.X).ToDecimal().Should().Be(2m);
        session.GetVariable(MemoryVariable.Y).ToDecimal().Should().Be(45m);

        Calculation rectangular = session.Calculate("Rec(√(2),45)");
        rectangular.Display.Text.Should().Be("x=1, y=1");
        session.GetVariable(MemoryVariable.X).ToDecimal().Should().Be(1m);
        session.GetVariable(MemoryVariable.Y).ToDecimal().Should().Be(1m);
    }

    [Fact]
    public void PolarAndRectangular_StandOnTheirOwn()
    {
        // Assumption U14: Pol( and Rec( are not part of a larger expression.
        Calculator.Error("1+Pol(1,1)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Rec(-1,45)").Kind.Should().Be(CalcErrorKind.MathError, "a radius is not negative");
    }

    [Fact]
    public void RandomNumbers_AreReproducibleWithASeed()
    {
        string[] first = [.. Enumerable.Range(0, 5).Select(_ => Calculator.Session(randomSeed: 12345).Calculate("Ran#").Display.Text)];
        string[] again = [.. Enumerable.Range(0, 5).Select(_ => Calculator.Session(randomSeed: 12345).Calculate("Ran#").Display.Text)];

        first.Should().Equal(again);
    }

    [Fact]
    public void RandomIntegers_StayInTheirRange()
    {
        CalculatorSession session = Calculator.Session();

        for (int i = 0; i < 200; i++)
        {
            session.Calculate("RanInt#(1,6)").Result.ToDecimal().Should().BeInRange(1m, 6m);
        }

        session.Calculate("RanInt#(6,1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }
}
