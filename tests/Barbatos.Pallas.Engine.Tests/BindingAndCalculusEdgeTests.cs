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
/// Binding and calculus at their edges: literal limits, error spans, plugins with unusual results, and integrals that do
/// not converge.
/// </summary>
public sealed class BindingAndCalculusEdgeTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Radians =
        settings => settings with { AngleUnit = AngleUnit.Radian };

    private static CalculatorSession PluginSession(IMathFunction function) =>
        PallasEngineBuilder.CreateDefault().AddFunction(function).Build().CreateSession();

    [Theory]
    // Engineering symbols scale by powers of 1000 (p. 64).
    [InlineData("5_m", "5×10^-3")]
    [InlineData("5_μ", "5×10^-6")]
    [InlineData("5_n", "5×10^-9")]
    [InlineData("5_p", "5×10^-12")]
    [InlineData("5_f", "5×10^-15")]
    public void EngineeringSymbolsScaleTheirNumber(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO }).Should().Be(expected);
    }

    [Fact]
    public void ARecurringLiteralOfTwentyEightDigits_IsStillExact()
    {
        Value value = Calculator.Evaluate("0.(123456789012345678901234567)");

        value.IsExact.Should().BeTrue();
        value.ToDecimal().Should().Be(123456789012345678901234567m / 999999999999999999999999999m);
        Calculator.Error("0.(1234567890123456789012345678)").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheFirstErrorOfAnInputIsTheOneReported()
    {
        CalcError error = Calculator.Error("GCD(1)+GCD(1)");

        error.Kind.Should().Be(CalcErrorKind.SyntaxError);
        error.Span.Start.Should().Be(0);
    }

    [Fact]
    public void AnErrorInsideADefinedFunction_PointsAtTheCall()
    {
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.G, "f(x)+1");

        CalcError error = session.Calculate("1+g(1)").Error!.Value;

        error.Kind.Should().Be(CalcErrorKind.NotDefined);
        error.Span.Start.Should().Be(2);
    }

    [Theory]
    [InlineData("PreAns")]
    [InlineData("1°")]
    [InlineData("5%")]
    public void WhatBaseNDoesNotHave_IsASyntaxError(string input)
    {
        Calculator.Session(CalculatorApp.BaseN).Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void BaseNMemories_AreXYZAndAns()
    {
        // A to F are hex digits in Base-N, in every number mode (assumption U12).
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);
        session.SetVariable(MemoryVariable.X, Value.FromDecimal(5m));

        session.Calculate("x+1").Result.ToInt32().Should().Be(6);
        session.Calculate("Ans×2").Result.ToInt32().Should().Be(12);
        session.Calculate("A").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "A is not a decimal digit");
    }

    [Fact]
    public void EToTheXIsMathExp()
    {
        // Not a power of the double nearest e: Math.Pow(Math.E, 100) differs from Math.Exp(100) in the 14th digit.
        Calculator.Evaluate("e^(100)").ToDouble().Should().Be(Math.Exp(100d));
    }

    [Fact]
    public void SexagesimalInputMayOmitTheSeconds()
    {
        Calculator.Evaluate("1°30′").ToDecimal().Should().Be(1.5m);
    }

    [Fact]
    public void APluginsExactDecimalResult_StaysExact()
    {
        CalculatorSession session = PluginSession(new Tenth());

        session.Calculate("tenth(1)").Result.IsExact.Should().BeTrue();
        session.Calculate("tenth(1)").Result.ToDecimal().Should().Be(0.1m);
    }

    [Fact]
    public void AComplexPluginResultInCalculate_IsDisplayedAndRefusedWhereARealIsNeeded()
    {
        CalculatorSession session = PluginSession(new ComplexValue());
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        session.Calculate("cplx(1)").Display.Latex.Should().NotStartWith(@"\text", "a complex result is written in the Complex notation");
        session.Calculate("∫(x,0,cplx(1))").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("Σ(x,1,cplx(1))").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        CalcError error = session.Calculate("∫(cplx(x),0,1)").Error!.Value;
        error.Kind.Should().Be(CalcErrorKind.MathError);
        error.Span.Start.Should().Be(0, "a complex integrand is the integral's error");
    }

    [Fact]
    public void AnErrorInsideAnIntegrand_PointsAtItsCause()
    {
        CalcError error = Calculator.Error("∫(ln(x-2),0,1)", settings: Radians);

        error.Kind.Should().Be(CalcErrorKind.MathError);
        error.Span.Start.Should().Be(2);
    }

    [Fact]
    public void AnIntegrandThatRunsOutOfBudget_IsATimeOutOfTheIntegral()
    {
        CalculatorSession session = Calculator.Session(settings: Radians);
        session.Budget = new EngineBudget(5_000, TimeSpan.FromSeconds(30));

        CalcError error = session.Calculate("∫(Σ(x,1,1000),0,1)").Error!.Value;

        error.Kind.Should().Be(CalcErrorKind.TimeOut);
        error.Span.Start.Should().Be(0);
    }

    [Fact]
    public void AnErrorInADerivative_PointsAtItsCause()
    {
        // d/dx √x = 1/(2√x), a division by zero at 0 inside the √ that caused it.
        CalcError error = Calculator.Error("d/dx(√(x),0)", settings: Radians);

        error.Kind.Should().Be(CalcErrorKind.MathError);
        error.Span.Start.Should().Be(5);
    }

    [Theory]
    [InlineData("∫(√(0.6-x),0,1)", CalcErrorKind.MathError)]
    [InlineData("∫(sin(x²),0,10000)", CalcErrorKind.TimeOut)]
    [InlineData("Σ(x,-10000000000,-10000000000)", CalcErrorKind.MathError)]
    public void CalculusOutsideItsDomainOrAccuracy(string input, CalcErrorKind kind)
    {
        Calculator.Error(input, settings: Radians).Kind.Should().Be(kind);
    }

    [Fact]
    public void ASmallIntegrandIsIntegratedRelativeToItself()
    {
        // The target is relative to ∫|f|, so 10⁻²⁰ sin x over 16 periods is as accurate as sin x.
        double expected = 1e-20 * (1d - Math.Cos(100d));

        Calculator.Evaluate("∫(10^-20sin(x),0,100)", settings: Radians).ToDouble().Should().BeApproximately(expected, Math.Abs(expected) * 1e-9);
    }

    [Fact]
    public void IntegralTolerances_HaveADomain()
    {
        // A tol below what double can promise is accepted rather than a Time Out: the integral answers within 10⁻⁹.
        Calculator.Display("∫(x,0,1,1×10^-21)", settings: Radians).Should().Be("1⌟2");
        Calculator.Display("∫(x,0,1,1×10^-14)", settings: Radians).Should().Be("1⌟2");
        Calculator.Error("∫(x,0,1,1×10^-23)", settings: Radians).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ALooseToleranceStopsRefinementEarly()
    {
        // sin over 16 periods needs several bisections for 10⁻¹⁴; a tol of 10⁻² is met with fewer evaluations.
        CalculatorSession tight = Calculator.Session(settings: Radians);
        CalculatorSession loose = Calculator.Session(settings: Radians);

        Calculation precise = tight.Calculate("∫(sin(x),0,100)");
        Calculation rough = loose.Calculate("∫(sin(x),0,100,0.01)");

        precise.Integrals[0].ErrorEstimate.Should().BeLessThan(1e-9);
        rough.Integrals[0].ErrorEstimate.Should().BeGreaterThan(precise.Integrals[0].ErrorEstimate);
    }

    [Fact]
    public void FormatConversionsOfFractionsAndComplexNumbers()
    {
        CalculatorSession session = Calculator.Session();
        session.Format(session.Calculate("7⌟3"), FormatTarget.ImproperFraction)!.Text.Should().Be("7⌟3");

        CalculatorSession complex = Calculator.Session(CalculatorApp.Complex);
        complex.Format(complex.Calculate("√(2)+i"), FormatTarget.DecimalValue)!.Text.Should().Be("1.414213562+i");

        complex.Settings = complex.Settings with { InputOutput = InputOutput.MathIDecimalO };
        complex.Calculate("√(2)+i").Display.Text.Should().Be("1.414213562+i");
    }

    [Fact]
    public void DigitSeparators_AreWrittenAsTextInLatex()
    {
        Calculator.Session(settings: settings => settings with { DigitSeparator = true, InputOutput = InputOutput.MathIDecimalO })
            .Calculate("1234").Display.Latex.Should().Be(@"\text{1,234}");
    }

    private sealed class Tenth : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("tenth(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            return Value.FromDecimal(arguments[0].ToDecimal() / 10m);
        }
    }

    private sealed class ComplexValue : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("cplx(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            return Value.FromComplex(new NumericsComplex(arguments[0].ToDouble(), 1d));
        }
    }
}
