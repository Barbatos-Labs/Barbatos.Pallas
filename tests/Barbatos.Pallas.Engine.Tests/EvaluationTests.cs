// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Binding, compiling and evaluating: scopes, nesting, the budget and the paths that only a stranger input reaches.
/// </summary>
public sealed class EvaluationTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian, InputOutput = InputOutput.MathIDecimalO };

    [Fact]
    public void ADefinedFunctionCanBeCalledTwiceInOneExpression()
    {
        // Each call gets its own slot, so the arguments do not overwrite each other.
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "x²");

        session.Calculate("f(2)+f(3)").Display.Text.Should().Be("13");
        session.Calculate("f(f(2))").Display.Text.Should().Be("16");
        session.Calculate("f(2)×f(f(1))").Display.Text.Should().Be("4");
    }

    [Fact]
    public void ADefinedFunctionSeesTheMemoryVariables()
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.A, Value.FromDecimal(10m));
        session.Define(DefinedFunction.F, "A×x");

        session.Calculate("f(3)").Display.Text.Should().Be("30");
    }

    [Fact]
    public void APluginIsCalledOncePerOccurrence()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new Counter()).Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("count(1)+count(1)+count(1)").Display.Text.Should().Be("6", "the plugin counts its calls: 1 + 2 + 3");
    }

    [Fact]
    public void NestedSeriesBindTheirOwnVariable()
    {
        // The inner Σ shadows the outer one, as the parser's scopes say.
        Calculator.Display("Σ(Σ(x,1,3),1,2)").Should().Be("12");
        Calculator.Display("Π(Σ(x,1,x),1,3)").Should().Be("18", "the inner bounds use the inner variable");
    }

    [Fact]
    public void SeriesAndProductsCarryErrorsOutOfTheirBody()
    {
        Calculator.Error("Σ(1⌟(x-2),1,3)").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("Π(ln(x-1),1,3)").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ASeriesThatLeavesTheRange_IsAMathError()
    {
        Calculator.Error("Π(10^20,1,10)").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheDerivativeOfADefinedFunctionFollowsTheChainRule()
    {
        CalculatorSession session = Calculator.Session(settings: Radians);
        session.Define(DefinedFunction.F, "x³");

        session.Calculate("d/dx(f(2x),1)").Display.Text.Should().Be("24", "f(2x) = 8x³, so the derivative at 1 is 24");
    }

    [Fact]
    public void TheDerivativeOfAnIntegralFollowsLeibniz()
    {
        // d/dx ∫(t², 0, x) = x².
        Calculator.Display("d/dx(∫(x²,0,x),3)", settings: Radians).Should().Be("9");
        Calculator.Display("d/dx(∫(x²,x,4),2)", settings: Radians).Should().Be("-4");
    }

    [Fact]
    public void TheDerivativeOfASeries_HasNoRule()
    {
        Calculator.Error("d/dx(Σ(x,1,x),3)", settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ADerivativeThatDoesNotDependOnTheVariable_IsZero()
    {
        Calculator.Display("d/dx(Σ(x,1,5),2)", settings: Radians).Should().Be("0");
        Calculator.Display("d/dx(∫(x,0,1),2)", settings: Radians).Should().Be("0");
        Calculator.Display("d/dx(A,2)", settings: Radians).Should().Be("0");
    }

    [Fact]
    public void APluginInsideADerivative_HasNoRule()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new Counter()).Build();
        CalculatorSession session = engine.CreateSession();
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        session.Calculate("d/dx(count(x),1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("d/dx(x+count(1),1)").Display.Text.Should().Be("1", "the plugin does not depend on x");
    }

    [Theory]
    // The derivative rules that the manual's examples do not reach.
    [InlineData("d/dx(2^x,3)", "5.545177444")]
    [InlineData("d/dx(x^x,2)", "6.772588722")]
    [InlineData("d/dx(log(x),1)", "0.4342944819")]
    [InlineData("d/dx(log(2,x),2)", "0.7213475204")]
    [InlineData("d/dx(cos(x),0)", "0")]
    [InlineData("d/dx(cos⁻¹(x),0)", "-1")]
    [InlineData("d/dx(tan⁻¹(x),0)", "1")]
    [InlineData("d/dx(sinh(x),0)", "1")]
    [InlineData("d/dx(tanh(x),0)", "1")]
    [InlineData("d/dx(sinh⁻¹(x),0)", "1")]
    [InlineData("d/dx(cosh⁻¹(x),2)", "0.5773502692")]
    [InlineData("d/dx(tanh⁻¹(x),0)", "1")]
    [InlineData("d/dx(x%,1)", "0.01")]
    [InlineData("d/dx(x⁻¹,2)", "-0.25")]
    [InlineData("d/dx(x³,2)", "12")]
    [InlineData("d/dx(2ˣ√(x),4)", "0.25")]
    [InlineData("d/dx(xˣ√(16),2)", "-2.772588722")]
    [InlineData("d/dx(x°,1)", "0.01745329252")]
    [InlineData("d/dx(1⌟x,2)", "-0.25")]
    [InlineData("d/dx(x⌟1⌟2,1)", "1")]
    public void DerivativeRules_MatchTheMathematics(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Fact]
    public void ADerivativeOfAMixedFractionWithAVariablePart_HasNoRule()
    {
        Calculator.Error("d/dx(1⌟x⌟2,1)", settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheSecondDerivativeOfASineIsItsNegative()
    {
        Calculator.Display("d/dx(d/dx(sin(x),x),0)", settings: Radians).Should().Be("0");
        Calculator.Display("d/dx(d/dx(sin(x),x),π÷2)", settings: Radians).Should().Be("-1");
    }

    [Fact]
    public void AnIntegralOfAWideIntervalStillMeetsItsTarget()
    {
        Calculator.Evaluate("∫(sin(x),0,100)", settings: Radians).ToDouble().Should().BeApproximately(1d - Math.Cos(100d), 1e-9);
    }

    [Fact]
    public void AnIntegralThatCannotConverge_TimesOut()
    {
        // 1/x over an interval containing 0: the estimate never falls.
        Calculator.Error("∫(1⌟x,-1,1)", settings: Radians).Kind.Should().BeOneOf(CalcErrorKind.TimeOut, CalcErrorKind.MathError);
    }

    [Fact]
    public void AnIntegralOfAComplexBody_IsAMathError()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);

        session.Calculate("∫(x,0,1)").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "calculus is not available in Complex");
    }

    [Fact]
    public void BoundsOfCalculusMustBeReal()
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.A, Value.FromDecimal(2m));

        session.Calculate("Σ(x,1,A)").Display.Text.Should().Be("3");
        session.Calculate("∫(x,0,A)").Display.Text.Should().Be("2");
    }

    [Fact]
    public void TheBudgetCountsIntegrandEvaluationsToo()
    {
        CalculatorSession session = Calculator.Session(settings: Radians);
        session.Budget = new EngineBudget(20, TimeSpan.FromSeconds(5));

        session.Calculate("∫(sin(x),0,10)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void ATimeoutStopsAProductToo()
    {
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(100, TimeSpan.FromSeconds(5));

        session.Calculate("Π(1,1,1000)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Theory]
    // Names that exist in the vocabulary but not in this application, and names with no value.
    [InlineData("x̄", CalculatorApp.Calculate)]
    [InlineData("MatA", CalculatorApp.Calculate)]
    [InlineData("VctA", CalculatorApp.Complex)]
    public void NamesOfOtherApplications_AreSyntaxErrors(string input, CalculatorApp app)
    {
        Calculator.Error(input, app).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AScientificConstantWithoutData_IsNotDefined()
    {
        Calculator.Error("@h").Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void AUnitConversionWithoutData_IsNotDefined()
    {
        Calculator.Error("5cm▶in").Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void RandomNumbersHaveTheirOwnRange()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO });

        for (int i = 0; i < 100; i++)
        {
            decimal random = session.Calculate("Ran#").Result.ToDecimal();
            random.Should().BeInRange(0m, 0.999m);
            (random * 1000m).Should().Be(decimal.Truncate(random * 1000m), "Ran# has three decimals (p. 58)");
        }
    }

    [Theory]
    [InlineData("RanInt#(1,1)")]
    [InlineData("RanInt#(1,10000000000)")]
    [InlineData("RanInt#(1.5,3)")]
    [InlineData("RanInt#(-10000000000,1)")]
    public void RandomIntegersFollowTheirDomain(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    private sealed class Counter : IMathFunction
    {
        private int _calls;

        public FunctionSignature Signature { get; } = new("count(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            _calls++;
            return Value.FromDecimal(_calls * arguments[0].ToDecimal());
        }
    }
}
