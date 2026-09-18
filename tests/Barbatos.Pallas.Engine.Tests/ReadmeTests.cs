// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void OneCalculation()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("2⌟3+1⌟1⌟2").Display.Text.Should().Be("13⌟6");
        session.Calculate("√(2)×3").Display.Text.Should().Be("3√(2)");
        session.Calculate("10√(2)+15×3√(3)").Display.Text.Should().Be("45√(3)+10√(2)");
        session.Calculate("d/dx(x³,0.1)").Result.ToDecimal().Should().Be(0.03m);
        session.Calculate("0.1+0.2").Result.ToDecimal().Should().Be(0.3m);
    }

    [Fact]
    public void ErrorsCarryTheirKindAndSpan()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

        Calculation calculation = session.Calculate("14÷0×2");

        calculation.Succeeded.Should().BeFalse();
        calculation.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        calculation.Error!.Value.Span.Should().Be(new SourceSpan(0, 4));
    }

    [Fact]
    public void SettingsAndFormatting()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();
        session.Settings = session.Settings with
        {
            AngleUnit = AngleUnit.Radian,
            NumberFormat = NumberFormat.Fix(3),
            InputOutput = InputOutput.MathIDecimalO,
        };

        Calculation result = session.Calculate("π÷6");

        session.Format(result, FormatTarget.DecimalValue)!.Text.Should().Be("0.524");
        session.Format(result, FormatTarget.Standard)!.Text.Should().Be("1⌟6π");
        result.Display.Latex.Should().NotBeEmpty();
    }

    [Fact]
    public void MemoryVerifyAndBaseN()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

        session.Calculate("3+5");
        session.Store(MemoryVariable.A);
        session.Calculate("A×10").Display.Text.Should().Be("80");
        session.Define(DefinedFunction.F, "x²+1");
        session.Calculate("f(5)").Display.Text.Should().Be("26");

        session.Settings = session.Settings with { Verify = true };
        session.Calculate("2²=2+2=4").IsTrue.Should().BeTrue();

        session.SwitchApp(CalculatorApp.BaseN);
        session.Settings = session.Settings with { BaseMode = NumberBase.Bin };
        session.Calculate("Not(1010)").Display.Text.Should().Be("11111111111111111111111111110101");
    }

    [Fact]
    public void APluginFunctionAsWrittenInTheReadme()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new BeamDeflection()).Build();

        engine.CreateSession().Calculate("beam(3,4)").Display.Text.Should().Be("12");
        engine.CreateSession().Calculate("beam(-1,4)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    private sealed class BeamDeflection : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("beam(", 2, 2);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            return arguments[0].ToDouble() < 0d
                ? EvalResult.Failure(CalcErrorKind.MathError)
                : Value.FromDecimal(arguments[0].ToDecimal() * arguments[1].ToDecimal());
        }
    }
}
