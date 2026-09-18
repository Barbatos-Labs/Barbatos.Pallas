// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Binding: reading literals, resolving names and refusing what an application does not have.
/// </summary>
public sealed class BinderTests
{
    [Theory]
    // A literal is read into decimal while every digit fits (invariant I2).
    [InlineData("0.5", "0.5")]
    [InlineData(".5", "0.5")]
    [InlineData("5.", "5")]
    [InlineData("0", "0")]
    [InlineData("0.0", "0")]
    [InlineData("00012", "12")]
    [InlineData("3.(3)", "3.333333333")]
    [InlineData("0.(9)", "1")]
    [InlineData("12.3(45)", "12.34545455")]
    [InlineData("0.0(1)", "0.01111111111")]
    public void Literals_AreReadExactly(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO }).Should().Be(expected);
    }

    [Fact]
    public void ARecurringLiteralIsTheFractionItStandsFor()
    {
        Calculator.Evaluate("0.(9)").ToDecimal().Should().Be(1m);
        Calculator.Evaluate("0.(3)").ToDecimal().Should().Be(1m / 3m);
        Calculator.Evaluate("0.(3)").IsExact.Should().BeTrue();
        Calculator.Display("0.(142857)").Should().Be("1⌟7");
    }

    [Fact]
    public void ARecurringLiteralWithTooManyDigits_IsAMathError()
    {
        Calculator.Error("0.(12345678901234567890123456789012)").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ALiteralBeyondDecimal_BecomesADouble()
    {
        Value value = Calculator.Evaluate("123456789012345678901234567890123");

        value.Kind.Should().Be(ValueKind.DoubleReal);
        value.ToDouble().Should().Be(1.2345678901234568e32);
    }

    [Theory]
    [InlineData(NumberBase.Dec, "2147483647", 2147483647)]
    [InlineData(NumberBase.Hex, "7FFFFFFF", 2147483647)]
    [InlineData(NumberBase.Hex, "80000000", int.MinValue)]
    [InlineData(NumberBase.Oct, "37777777777", -1)]
    [InlineData(NumberBase.Bin, "11111111111111111111111111111111", -1)]
    [InlineData(NumberBase.Dec, "0", 0)]
    public void BaseNLiterals_AreThirtyTwoBitPatterns(NumberBase mode, string input, int expected)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN, settings: settings => settings with { BaseMode = mode });

        session.Calculate(input).Result.ToInt32().Should().Be(expected);
    }

    [Theory]
    [InlineData(NumberBase.Dec, "d10", 10)]
    [InlineData(NumberBase.Dec, "h10", 16)]
    [InlineData(NumberBase.Dec, "b10", 2)]
    [InlineData(NumberBase.Dec, "o10", 8)]
    [InlineData(NumberBase.Bin, "d255", 255)]
    public void BasePrefixes_ReadTheirOwnBase(NumberBase mode, string input, int expected)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN, settings: settings => settings with { BaseMode = mode });

        session.Calculate(input).Result.ToInt32().Should().Be(expected);
    }

    [Theory]
    [InlineData(NumberBase.Bin, "2")]
    [InlineData(NumberBase.Oct, "8")]
    [InlineData(NumberBase.Dec, "b2")]
    [InlineData(NumberBase.Dec, "o8")]
    public void DigitsOutsideTheirBase_AreSyntaxErrors(NumberBase mode, string input)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN, settings: settings => settings with { BaseMode = mode });

        session.Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void TheLowestBaseNValue_IsWrittenWithItsSign()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);

        session.Calculate("-2147483648").Result.ToInt32().Should().Be(int.MinValue);
        session.Calculate("-2147483649").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("2147483648").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void EachConstantHasItsValue()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO });

        session.Calculate("π").Result.ToDouble().Should().Be(double.Pi);
        session.Calculate("e").Result.ToDouble().Should().Be(double.E);
        session.Calculate("Ran#").Succeeded.Should().BeTrue();
        Calculator.Session(CalculatorApp.Complex).Calculate("i").Result.ToComplex().Should().Be(System.Numerics.Complex.ImaginaryOne);
    }

    [Fact]
    public void EPowerIsTheExponentialFunction()
    {
        // e^x is the exponential, not a power of the 15-digit decimal of e (p. 69).
        Calculator.Evaluate("e^(1)").ToDecimal().Should().Be((decimal)Math.E, "the exponential returns to decimal at 15 digits");
        Calculator.Display("e^(0)").Should().Be("1");
        Calculator.Display("ln(e^(2))").Should().Be("2");
    }

    [Fact]
    public void FunctionsThatNeedTwoArgumentsAcceptOnlyTwo()
    {
        Calculator.Error("GCD(1,2,3)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("log(1,2,3)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("RanInt#(1)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("d/dx(x²,1,1×10^-10,4)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("∫(x,0,1,1×10^-10,4)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Σ(x,1,2,3)").Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void RemainderAndCoordinatesAreNotAvailableEverywhere()
    {
        // ÷R, Pol( and Rec( exist in Calculate, Statistics, Matrix and Vector (pp. 56, 62).
        Calculator.Error("5÷R2", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Pol(1,1)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("1+5÷R2", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void ComplexFunctionsWorkOnRealValuesToo()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex);

        session.Calculate("Conjg(2)").Display.Text.Should().Be("2");
        session.Calculate("ReP(2)").Display.Text.Should().Be("2");
        session.Calculate("ImP(2)").Display.Text.Should().Be("0");
        session.Calculate("Arg(0)").Display.Text.Should().Be("0");
        session.Calculate("Arg(-2)").Display.Text.Should().Be("180");
    }

    [Fact]
    public void AnEmptyDefinitionIsRefusedAndTheOldOneKept()
    {
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "x+1");

        session.Define(DefinedFunction.F, string.Empty)!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);

        session.Calculate("f(1)").Display.Text.Should().Be("2");
    }

    [Fact]
    public void ADefinitionMayReferToAFunctionDefinedLater()
    {
        // g(x) = f(x) + 1 can be written before f exists; only the calculation needs both.
        CalculatorSession session = Calculator.Session();

        session.Define(DefinedFunction.G, "f(x)+1").Should().BeNull();
        session.Calculate("g(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);

        session.Define(DefinedFunction.F, "x²");
        session.Calculate("g(3)").Display.Text.Should().Be("10");
    }

    [Fact]
    public void ADefinitionWithAMathErrorIsAccepted()
    {
        // Only syntax is checked when a function is defined; 1÷0 fails when it is used.
        CalculatorSession session = Calculator.Session();

        session.Define(DefinedFunction.F, "1⌟0").Should().BeNull();
        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }
}
