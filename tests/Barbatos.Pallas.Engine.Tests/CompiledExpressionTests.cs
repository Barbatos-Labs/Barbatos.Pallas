// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// An expression in x compiled once and calculated at many x (<see cref="CalculatorSession.Compile(string)"/>).
/// </summary>
public sealed class CompiledExpressionTests
{
    private static Value Number(decimal value) => Value.FromDecimal(value);

    [Fact]
    public void AnExpressionIsCalculatedAtEachX()
    {
        CompiledExpression square = Calculator.Session().Compile("x^2+1");

        square.Succeeded.Should().BeTrue();
        square.Evaluate(Number(3m)).Result.ToDecimal().Should().Be(10m);
        square.Evaluate(Number(0.5m)).Display.Text.Should().Be("5⌟4");
        square.TryEvaluate(2d, out double y).Should().BeTrue();
        y.Should().Be(5d);
    }

    [Fact]
    public void ItCalculatesAsTheSessionDoes()
    {
        // The same input on the line and compiled: one result, exact forms included.
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.X, Number(45m));

        Calculation typed = session.Evaluate("sin(x)+√(2)");
        Calculation compiled = session.Compile("sin(x)+√(2)").Evaluate(Number(45m));

        compiled.Result.Should().Be(typed.Result);
        compiled.Display.Should().Be(typed.Display);
    }

    [Theory]
    [InlineData("e^(−x^2÷2)÷√(2π)")]
    [InlineData("sin(30)+x")]
    [InlineData("2π÷x")]
    [InlineData("5!+x")]
    [InlineData("√(2)×√(3)x")]
    [InlineData("Σ(2π,1,3)+x")]
    [InlineData("d/dx(x^2+π,x)")]
    [InlineData("1÷0+x")]
    [InlineData("√(−1)+x")]
    public void WhatIsTheSameAtEveryXIsCalculatedOnceAndTheSame(string input)
    {
        // The constant parts are calculated when the expression is compiled (ConstantFolder): the result, its display
        // and its error must be what the line gives.
        CalculatorSession session = Calculator.Session();
        foreach (decimal x in (decimal[])[0.5m, 2m, -3m])
        {
            session.SetVariable(MemoryVariable.X, Number(x));
            Calculation typed = session.Evaluate(input);
            Calculation compiled = session.Compile(input).Evaluate(Number(x));

            compiled.Succeeded.Should().Be(typed.Succeeded, "{0} at {1}", input, x);
            compiled.Error.Should().Be(typed.Error, "{0} at {1} fails where the line does", input, x);
            compiled.Result.Should().Be(typed.Result, "{0} at {1}", input, x);
            compiled.Display.Should().Be(typed.Display, "{0} at {1}", input, x);
        }
    }

    [Fact]
    public void ARandomNumberIsDrawnAtEachCalculation()
    {
        // Ran# is the same call with the same arguments at every x, and still not a constant.
        CompiledExpression noise = Calculator.Session(randomSeed: 7).Compile("Ran#+RanInt#(1,1000)+x");

        IEnumerable<decimal> draws = Enumerable.Range(0, 5).Select(_ => noise.Evaluate(Number(0m)).Result.ToDecimal());

        draws.Distinct().Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void AFunctionOfTheTableIsFoldedToo()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Table);
        session.Define(DefinedFunction.F, "x÷√(2π)").Should().BeNull();

        foreach (decimal x in (decimal[])[1m, 2.5m])
        {
            session.SetVariable(MemoryVariable.X, Number(x));
            Calculation compiled = session.Compile("f(x)+∫(√(2)x,0,1)").Evaluate(Number(x));
            compiled.Succeeded.Should().BeTrue();
            compiled.Result.Should().Be(session.Evaluate("f(x)+∫(√(2)x,0,1)").Result);
        }
    }

    [Fact]
    public void NothingIsStored()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("7");
        session.SetVariable(MemoryVariable.X, Number(1m));

        session.Compile("x×2").Evaluate(Number(21m)).Result.ToDecimal().Should().Be(42m);

        session.Ans.ToDecimal().Should().Be(7m, "Ans is left alone, as a row of a table leaves it (p. 109)");
        session.GetVariable(MemoryVariable.X).ToDecimal().Should().Be(1m, "x is only lent to the expression");
    }

    [Fact]
    public void TheOtherVariablesAreReadAsTheyAreAtEachCalculation()
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.A, Number(2m));
        CompiledExpression line = session.Compile("Ax");

        line.Evaluate(Number(3m)).Result.ToDecimal().Should().Be(6m);
        session.SetVariable(MemoryVariable.A, Number(3m));
        line.Evaluate(Number(3m)).Result.ToDecimal().Should().Be(9m);
    }

    [Fact]
    public void TheSettingsAreThoseItWasCompiledWith()
    {
        CalculatorSession session = Calculator.Session();
        CompiledExpression degrees = session.Compile("sin(x)");
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        degrees.Evaluate(Number(90m)).Result.ToDecimal().Should().Be(1m);
        degrees.Settings.AngleUnit.Should().Be(AngleUnit.Degree);
        session.Compile("sin(x)").Evaluate(Number(90m)).Result.ToDouble().Should().BeApproximately(Math.Sin(90d), 1e-14);
    }

    [Fact]
    public void TheTablesFunctionsAreCompiledAsDefined()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Table);
        session.Define(DefinedFunction.F, "x^2−1").Should().BeNull();
        CompiledExpression f = session.Compile("f(x)");

        f.App.Should().Be(CalculatorApp.Table);
        f.Evaluate(Number(4m)).Result.ToDecimal().Should().Be(15m);
    }

    [Theory]
    [InlineData("x=2")]
    [InlineData("x+")]
    [InlineData("7÷R2")]
    [InlineData("Pol(x,1)")]
    public void WhatIsNotAnExpressionInXIsASyntaxError(string input)
    {
        CompiledExpression compiled = Calculator.Session().Compile(input);

        compiled.Succeeded.Should().BeFalse();
        compiled.Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        compiled.Evaluate(Number(1m)).Error.Should().Be(compiled.Error);
        compiled.TryEvaluate(1d, out double y).Should().BeFalse();
        y.Should().Be(0d);
        compiled.Derivative().Should().BeSameAs(compiled, "there is nothing to differentiate");
    }

    [Fact]
    public void ARelationIsRefusedWhereItsSignIs()
    {
        // The cursor goes to the =, as it does for any character an expression cannot have there.
        CompiledExpression relation = Calculator.Session().Compile("x=2");

        relation.Error!.Value.Span.Start.Should().Be(1);
    }

    [Fact]
    public void WhatCannotBeBoundIsThatError()
    {
        // Without the reference data there is no Planck constant (EvaluationTests): a Not Defined, as on the line.
        CompiledExpression planck = Calculator.Session().Compile("@h×x");

        planck.Succeeded.Should().BeFalse();
        planck.Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
        planck.Evaluate(Number(1m)).Error.Should().Be(planck.Error);
    }

    [Fact]
    public void AnErrorAtOneXIsThatCalculationsError()
    {
        // p. 163: 1÷0 is a Math ERROR, with the cursor where the division is.
        CompiledExpression reciprocal = Calculator.Session().Compile("1÷x");

        Calculation atZero = reciprocal.Evaluate(Number(0m));

        atZero.Succeeded.Should().BeFalse();
        atZero.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        atZero.Error.Value.Span.Length.Should().BeGreaterThan(0);
        reciprocal.TryEvaluate(0d, out _).Should().BeFalse();
        reciprocal.TryEvaluate(4d, out double quarter).Should().BeTrue();
        quarter.Should().Be(0.25d);
    }

    [Fact]
    public void AComplexResultHasNoPointOnAGraph()
    {
        CompiledExpression root = Calculator.Session(CalculatorApp.Complex).Compile("√(x)");

        root.Evaluate(Number(-4m)).Result.Kind.Should().Be(ValueKind.Complex);
        root.TryEvaluate(-4d, out _).Should().BeFalse();
        root.TryEvaluate(4d, out double two).Should().BeTrue();
        two.Should().Be(2d);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AnXThatIsNotANumberHasNoValue(double x)
    {
        Calculator.Session().Compile("x").TryEvaluate(x, out _).Should().BeFalse();
    }

    [Fact]
    public void AnXIsANumberToFifteenDigits()
    {
        // A double becomes a value as every double does (PRECISION.md §3): 0.1 is exactly 0.1, not 0.1000000000000000055.
        CompiledExpression tenfold = Calculator.Session().Compile("10x−1");

        tenfold.TryEvaluate(0.1d, out double y).Should().BeTrue();
        y.Should().Be(0d);
    }

    [Fact]
    public void AnXIsTheValueTheCalculatorHolds()
    {
        // Within the calculator's range, 10⁻¹⁰⁰ is 0 and 10¹⁰⁰ is beyond it (PRECISION.md §10).
        CompiledExpression standard = Calculator.Session().Compile("x");

        standard.ValueOf(0.1d)!.Value.ToDecimal().Should().Be(0.1m);
        standard.ValueOf(1e-100d)!.Value.ToDecimal().Should().Be(0m);
        standard.ValueOf(1e100d).Should().BeNull();
        standard.ValueOf(double.NaN).Should().BeNull();
        standard.TryEvaluate(1e100d, out _).Should().BeFalse("the calculator cannot hold that x");
        standard.TryEvaluate(1e-100d, out double zero).Should().BeTrue();
        zero.Should().Be(0d);

        CompiledExpression extended = Calculator.Session(profile: CalculatorProfile.Extended).Compile("x");
        extended.ValueOf(1e-100d)!.Value.ToDouble().Should().Be(1e-100d);
        extended.ValueOf(1e150d)!.Value.ToDouble().Should().Be(1e150d);
    }

    [Fact]
    public void TheDerivativeIsExact()
    {
        // PRECISION.md: d/dx(x³,0.1) is exactly 0.03, and d/dx(sin(x),90) in degrees exactly 0.
        CalculatorSession session = Calculator.Session();

        session.Compile("x^3").Derivative().Evaluate(Number(0.1m)).Result.ToDecimal().Should().Be(0.03m);
        CompiledExpression slope = session.Compile("sin(x)").Derivative();
        slope.Input.Should().Be("d/dx(sin(x),x)");
        slope.Settings.Should().Be(session.Settings);
        slope.Evaluate(Number(90m)).Result.ToDecimal().Should().Be(0m);
    }

    [Fact]
    public void ADerivativeKeepsTheSettingsOfItsExpression()
    {
        CalculatorSession session = Calculator.Session();
        CompiledExpression degrees = session.Compile("sin(x)");
        session.Settings = session.Settings with { AngleUnit = AngleUnit.Radian };

        degrees.Derivative().Settings.AngleUnit.Should().Be(AngleUnit.Degree);
    }

    [Fact]
    public void ALongCalculationIsCancelled()
    {
        CompiledExpression sum = Calculator.Session().Compile("Σ(x,1,1000000)");
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        ((Action)(() => sum.Evaluate(Number(1m), cancelled.Token))).Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void AnInputIsRequired()
    {
        ((Action)(() => Calculator.Session().Compile(null!))).Should().Throw<ArgumentNullException>().WithParameterName("input");
    }
}
