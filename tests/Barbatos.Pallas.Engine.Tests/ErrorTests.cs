// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Errors carry the kind the calculator shows and the span it puts the cursor at (manual p. 162).
/// </summary>
public sealed class ErrorTests
{
    [Fact]
    public void ASyntaxError_KeepsTheParsersCodeAndSpan()
    {
        CalcError error = Calculator.Error("2+");

        error.Kind.Should().Be(CalcErrorKind.SyntaxError);
        error.SyntaxCode.Should().Be(SyntaxErrorCode.MissingOperand);
        error.Span.Start.Should().Be(2);
    }

    [Fact]
    public void NestingTooDeep_IsAStackError()
    {
        // The parser stops at 128 levels; the calculator calls that a Stack ERROR (assumption U11).
        string input = new string('(', 200) + "1" + new string(')', 200);

        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.StackError);
    }

    [Fact]
    public void AMathError_PointsAtTheOperationThatFailed()
    {
        CalcError error = Calculator.Error("14÷0×2");

        error.Kind.Should().Be(CalcErrorKind.MathError);
        error.Span.Start.Should().Be(0);
        error.Span.Length.Should().Be("14÷0".Length);
    }

    [Fact]
    public void AnErrorInsideAFunction_PointsAtTheCall()
    {
        CalcError error = Calculator.Error("1+ln(0)");

        error.Kind.Should().Be(CalcErrorKind.MathError);
        error.Span.Start.Should().Be(2);
    }

    [Fact]
    public void AnErrorInsideADefinedFunction_PointsAtTheCallInTheInput()
    {
        CalculatorSession session = Calculator.Session();
        session.Define(DefinedFunction.F, "1⌟x");

        Calculation calculation = session.Calculate("2+f(0)");

        calculation.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        calculation.Error!.Value.Span.Start.Should().Be(2);
        calculation.Error!.Value.Span.End.Should().Be("2+f(0)".Length);
    }

    [Fact]
    public void WrongArity_IsASyntaxError()
    {
        Calculator.Error("sin(1,2)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("GCD(4)").Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Σ(x,1)").Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AnErrorLeavesNoDisplay()
    {
        Calculation calculation = Calculator.Session().Calculate("1÷0");

        calculation.Succeeded.Should().BeFalse();
        calculation.Display.Text.Should().BeEmpty();
        calculation.ToString().Should().Be("1÷0: MathError");
        Calculator.Session().Format(calculation).Should().BeNull();
    }

    [Fact]
    public void AnEmptyInput_IsASyntaxError()
    {
        Calculator.Error(string.Empty).SyntaxCode.Should().Be(SyntaxErrorCode.EmptyExpression);
    }

    [Fact]
    public void NullInputs_AreRejected()
    {
        CalculatorSession session = Calculator.Session();

        Action calculate = () => session.Calculate(null!);
        Action define = () => session.Define(DefinedFunction.F, null!);
        Action settings = () => session.Settings = null!;

        calculate.Should().Throw<ArgumentNullException>();
        define.Should().Throw<ArgumentNullException>();
        settings.Should().Throw<ArgumentNullException>();
    }
}
