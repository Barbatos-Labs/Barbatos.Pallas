// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using NumericsComplex = System.Numerics.Complex;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The built-in operations and the derivative rules at their edges: tiny and huge doubles, negative parts, complex
/// arguments, and the simplifications that choose a derivative rule.
/// </summary>
public sealed class OperationEdgeTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian, InputOutput = InputOutput.MathIDecimalO };

    private static readonly Func<CalculatorSettings, CalculatorSettings> Line =
        settings => settings with { InputOutput = InputOutput.LineILineO };

    [Theory]
    [InlineData("Intg(-10^-20)", "-1")]
    [InlineData("Int(-10^-20)", "0")]
    [InlineData("10^30÷R7", "1.428571429×10^29")]
    [InlineData("7÷R10^30", "7×10^-30")]
    [InlineData("LCM(5,0)", "0")]
    [InlineData("(-1)⌟1⌟2", "-1.5")]
    [InlineData("RanInt#(-4999999999,5000000000)×0", "0")]
    public void OperationsOnTheirEdges(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO }).Should().Be(expected);
    }

    [Theory]
    [InlineData("RanInt#(-5000000000,5000000000)")]
    [InlineData("GCD(5,10000000000)")]
    [InlineData("log(0,4)")]
    [InlineData("ln(0)")]
    [InlineData("log(0)")]
    public void OperationsOutsideTheirDomain_AreMathErrors(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // The real functions take no complex argument (p. 125).
    [InlineData("sin(i)")]
    [InlineData("sin⁻¹(i)")]
    [InlineData("sinh(i)")]
    [InlineData("ln(i)")]
    [InlineData("log(2,i)")]
    [InlineData("log(i,2)")]
    [InlineData("Int(i)")]
    [InlineData("(1+i)∠30")]
    [InlineData("2∠i")]
    [InlineData("i°30′0″")]
    public void RealFunctionsOfComplexValues_AreMathErrors(string input)
    {
        Calculator.Error(input, CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheImaginaryPartOfARealNumber_IsExactlyZero()
    {
        Calculator.Evaluate("ImP(2)", CalculatorApp.Complex).IsExact.Should().BeTrue();
    }

    [Fact]
    public void RoundOffOfADouble_KeepsTenSignificantDigits()
    {
        Calculator.Evaluate("Rnd(10^30÷3)").ToDouble().Should().Be(3.333333333e29);
    }

    [Fact]
    public void IntegerFunctionsOfApproximateValues_AreApproximate()
    {
        Calculator.Evaluate("GCD(20sin(30),4)").IsExact.Should().BeFalse();
        Calculator.Evaluate("LCM(20sin(30),4)").IsExact.Should().BeFalse();
        Calculator.Evaluate("GCD(10,4)").IsExact.Should().BeTrue();
    }

    [Fact]
    public void SexagesimalValues_KeepTheirSignAndExactness()
    {
        Calculator.Evaluate("(-1)°30′0″").ToDecimal().Should().Be(-1.5m);
        Calculator.Evaluate("20°30′0″").IsExact.Should().BeTrue();
        Calculator.Evaluate("(20sin(30))°30′0″").IsExact.Should().BeFalse();
    }

    [Theory]
    // Degrees beyond decimal, or below 10⁻¹⁴, are converted in double.
    [InlineData("(10^-20)°30′0″", 0.5)]
    [InlineData("(10^-20)°0′36″", 0.01)]
    [InlineData("(-10^-20)°30′0″", -0.5)]
    [InlineData("(10^30)°0′0″", 1e30)]
    public void SexagesimalValuesOfDoubles(string input, double expected)
    {
        Calculator.Evaluate(input).ToDouble().Should().BeApproximately(expected, Math.Abs(expected) * 1e-14);
    }

    [Theory]
    // Int and Intg are constant between integers; Rnd between its ties, at ten digits in Norm.
    [InlineData("d/dx(Int(x),1.5)", "0")]
    [InlineData("d/dx(Rnd(x),12345678904)", "0")]
    public void SlopesOfStepFunctions(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Theory]
    [InlineData("d/dx(Int(x),2)")]
    [InlineData("d/dx(Rnd(x),1.0000000005)")]
    [InlineData("d/dx(Rnd(x),12345678905)")]
    public void StepFunctionsJump_WhereTheyHaveNoDerivative(string input)
    {
        Calculator.Error(input, settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // An exponent that depends on x but is constant (0×x) must still take the power rule, which has no ln x:
    // at x = −2 the general rule u^v·(v′·ln u + v·u′/u) would take the logarithm of a negative number.
    [InlineData("d/dx(x^(0×x),-2)", "0")]
    [InlineData("d/dx(x^(x×0),-2)", "0")]
    [InlineData("d/dx(x^(0×x÷2),-2)", "0")]
    [InlineData("d/dx(x^(-(0×x)),-2)", "0")]
    [InlineData("d/dx(x^3,0)", "0")]
    [InlineData("d/dx(x÷10^60,1)", "1×10^-60")]
    [InlineData("d/dx(x°30′0″,1)", "1")]
    public void DerivativeRules_ChooseTheSimplestForm(string input, string expected)
    {
        Calculator.Display(input, settings: Radians).Should().Be(expected);
    }

    [Fact]
    public void DerivativesThroughDefinedFunctionsAndIntegrals()
    {
        CalculatorSession session = Calculator.Session(settings: Radians);
        session.Define(DefinedFunction.F, "x²");

        session.Calculate("d/dx(3f(x),1)").Display.Text.Should().Be("6");
        session.Calculate("d/dx(2∫(x²,0,x),3)").Display.Text.Should().Be("18");
    }

    [Fact]
    public void APluginWithSeveralArguments_HasNoDerivativeInAnyOfThem()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().AddFunction(new Sum()).Build().CreateSession();
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        session.Calculate("d/dx(twosum(x,1),1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("d/dx(twosum(1,x),1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("d/dx(x+twosum(1,2),1)").Display.Text.Should().Be("1");
        session.Calculate("twosum(1,2)+twosum(3,4)").Display.Text.Should().Be("10", "each call has its own plugin slot");
    }

    [Theory]
    // LineO shows a fraction only for fraction input (p. 32), wherever the fraction is.
    [InlineData("1⌟2", "1⌟2")]
    [InlineData("1+1⌟2", "3⌟2")]
    [InlineData("1.5+0", "1.5")]
    [InlineData("GCD(1⌟1×2,4)÷3", "2⌟3")]
    public void FractionInput_IsFoundAnywhereInTheInput(string input, string expected)
    {
        Calculator.Display(input, settings: Line).Should().Be(expected);
    }

    [Theory]
    // Only a sum or difference whose every term is sexagesimal is displayed in degrees-minutes-seconds (U16).
    [InlineData("1°0′0″+2°0′0″", "3°0′0″")]
    [InlineData("1°0′0″+2", "3")]
    [InlineData("2+1°0′0″", "3")]
    public void SexagesimalSums_AreDisplayedInDegreesMinutesSeconds(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void TheClockIsReadEveryThousandAndTwentyFourIterations()
    {
        // Reading the clock on every iteration would cost more than a Σ term; a timeout is noticed within 1024.
        CalculatorSession session = Calculator.Session();
        session.Budget = new EngineBudget(long.MaxValue, TimeSpan.FromTicks(1));

        session.Calculate("Σ(x,1,1023)").Display.Text.Should().Be("523776");
        session.Calculate("Σ(x,1,1024)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void ValuesOfTheBoundaryMagnitudes()
    {
        Value.FromDouble(7.9e28).Kind.Should().Be(ValueKind.DoubleReal, "7.9×10²⁸ is where decimal is no longer used");
        Value.FromDouble(7.8e28).Kind.Should().Be(ValueKind.DecimalReal);
        Value.FromComplex(new NumericsComplex(1d, 2d)).IsExact.Should().BeFalse();
        Calculator.Evaluate("0.00000000000001").IsExact.Should().BeTrue("10⁻¹⁴ still holds 15 significant digits");
        NumberFormat.Norm1.ToString().Should().Be("Norm1");
        NumberFormat.Fix(3).ToString().Should().Be("Fix3");
    }

    [Fact]
    public void DataSetsCheckTheirArguments()
    {
        Action tableName = () => _ = new AtomicWeightTable(" ", []);
        Action tableElements = () => _ = new AtomicWeightTable("Table", null!);
        Action unitName = () => _ = new UnitSet(" ", []);
        Action unitConversions = () => _ = new UnitSet("Units", null!);
        Action constantName = () => _ = new ConstantSet(" ", []);
        Action constantConstants = () => _ = new ConstantSet("Constants", null!);
        Action divisor = () => _ = new UnitConversion("in▶cm", 1m, 0m);

        tableName.Should().Throw<ArgumentException>().WithParameterName("name");
        tableElements.Should().Throw<ArgumentNullException>().WithParameterName("elements");
        unitName.Should().Throw<ArgumentException>().WithParameterName("name");
        unitConversions.Should().Throw<ArgumentNullException>().WithParameterName("conversions");
        constantName.Should().Throw<ArgumentException>().WithParameterName("name");
        constantConstants.Should().Throw<ArgumentNullException>().WithParameterName("constants");
        divisor.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("divisor");
    }

    [Fact]
    public void TheBuilderChecksTheBudget()
    {
        Action none = () => PallasEngineBuilder.CreateDefault().WithBudget(null!);
        Action noIterations = () => PallasEngineBuilder.CreateDefault().WithBudget(new EngineBudget(0, TimeSpan.FromSeconds(1)));
        Action noTime = () => PallasEngineBuilder.CreateDefault().WithBudget(new EngineBudget(1, TimeSpan.Zero));

        none.Should().Throw<ArgumentNullException>().WithParameterName("budget");
        noIterations.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("budget.MaxIterations");
        noTime.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("budget.Timeout");
    }

    [Fact]
    public void AnAtomicNumberBeyondInt_IsNotHydrogen()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault()
            .AddAtomicWeights(new AtomicWeightTable("Test", [new AtomicWeight(1, "H", 1.008m, IsMassNumber: false)]))
            .Build()
            .CreateSession();

        session.Calculate("AtWt(1)").Result.ToDecimal().Should().Be(1.008m);
        session.Calculate("AtWt(4294967297)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("AtWt(2)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    private sealed class Sum : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("twosum(", 2, 2);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            return Value.FromDecimal(arguments[0].ToDecimal() + arguments[1].ToDecimal());
        }
    }
}
