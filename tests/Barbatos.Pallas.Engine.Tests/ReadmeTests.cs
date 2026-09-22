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
    public void MatricesAndVectors()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

        session.SwitchApp(CalculatorApp.Matrix);
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,]
        {
            { Value.FromDecimal(2m), Value.One },
            { Value.One, Value.One },
        }));
        session.Calculate("MatA⁻¹").Display.Text.Should().Be("[[1, -1], [-1, 2]]");
        session.Calculate("Det(MatA)").Display.Text.Should().Be("1");

        session.SwitchApp(CalculatorApp.Vector);
        session.SetVector(VectorVariable.VctA, new VectorValue(Value.FromDecimal(3m), Value.FromDecimal(4m)));
        session.Calculate("Abs(VctA)").Display.Text.Should().Be("5");
        session.Calculate("UnitV(VctA)").Display.Text.Should().Be("[0.6, 0.8]");
    }

    [Fact]
    public void Statistics()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

        session.SwitchApp(CalculatorApp.Statistics);
        session.SetStatisticsData(new StatisticsData(
            x: [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(3m)],
            frequencies: [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(1m)]));
        session.Calculate("x̄").Display.Text.Should().Be("2");
        session.Calculate("σx").Display.Text.Should().Be("0.7071067812");
        session.Calculate("P(3▶t)").Display.Text.Should().Be("0.9213503965");

        session.SetStatisticsData(new StatisticsData(
            [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(3m)],
            [Value.FromDecimal(6m), Value.FromDecimal(12m), Value.FromDecimal(24m)]));
        session.Regression = RegressionModel.ExponentialAB;
        session.Calculate("b").Display.Text.Should().Be("2");
        session.Calculate("4ŷ").Display.Text.Should().Be("48");
    }

    [Fact]
    public void Distributions()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

        session.SwitchApp(CalculatorApp.Distribution);
        DistributionParameters parameters = new() { Trials = Value.FromDecimal(5m), Probability = Value.FromDecimal(0.5m) };
        session.CalculateDistribution(DistributionKind.BinomialCD, parameters, [Value.FromDecimal(2m), Value.FromDecimal(3m)])
            .Select(result => result.Display.Text).Should().Equal("0.5", "0.8125");
        session.CalculateDistribution(DistributionKind.NormalPD, new DistributionParameters
        {
            X = Value.FromDecimal(36m), Mean = Value.FromDecimal(35m), StandardDeviation = Value.FromDecimal(2m),
        }).Display.Text.Should().Be("0.1760326634");
    }

    [Fact]
    public void EquationsInequalitiesAndRatios()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();
        Value zero = Value.Zero;
        Value one = Value.One;
        Value two = Value.FromDecimal(2m);
        Value three = Value.FromDecimal(3m);
        Value four = Value.FromDecimal(4m);

        session.SwitchApp(CalculatorApp.Equation);
        Value[,] system =
        {
            { one, Value.FromDecimal(-1m), one, two },
            { one, one, Value.FromDecimal(-1m), zero },
            { Value.FromDecimal(-1m), one, one, four },
        };
        session.SolveSimultaneous(system).Unknowns.Select(unknown => unknown.Display.Text).Should().Equal("1", "2", "3");

        PolynomialSolution quadratic = session.SolvePolynomial([one, two, Value.FromDecimal(-2m)]);
        quadratic.Roots[0].Real.Display.Text.Should().Be("-1+√(3)");
        quadratic.Extrema[0].Y.Display.Text.Should().Be("-3");

        session.SetVariable(MemoryVariable.B, four);
        Calculation solved = session.SolveEquation("x²-B²=0", MemoryVariable.X, one);
        solved.Display.Text.Should().Be("4");
        solved.Second!.Value.ToDouble().Should().Be(0d);

        session.SwitchApp(CalculatorApp.Inequality);
        session.SolveInequality([one, two, Value.FromDecimal(-3m)], RelationOperator.GreaterOrEqual).Text.Should().Be("x≤-3, 1≤x");

        session.SwitchApp(CalculatorApp.Ratio);
        session.SolveRatio(RatioForm.XInSecondRatio, three, Value.FromDecimal(8m), Value.FromDecimal(12m)).Display.Text.Should().Be("9⌟2");
    }

    [Fact]
    public void SpreadsheetCellsAndTables()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
        CalculatorSession sheet = engine.CreateSession(CalculatorApp.Spreadsheet);
        sheet.CellValues = address => Value.FromDecimal(address.Row + 1);

        sheet.Evaluate("A2+7").Display.Text.Should().Be("9");
        sheet.Evaluate("Sum(A1:A4)").Display.Text.Should().Be("10");
        sheet.Ans.Should().Be(Value.Zero);
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
