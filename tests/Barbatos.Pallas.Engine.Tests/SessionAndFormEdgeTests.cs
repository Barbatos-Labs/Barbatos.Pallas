// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using NumericsComplex = System.Numerics.Complex;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Sessions, exact forms and number text at their edges: statements whose operands fail, forms whose coefficients
/// overflow, and the digits a format pads or trims.
/// </summary>
public sealed class SessionAndFormEdgeTests
{
    private const string Max = "79228162514264337593543950335";

    [Theory]
    // Dividing by a + b√r multiplies by the conjugate: (a − b√r)/(a² − b²r).
    [InlineData("1÷(2+√(3))", "2-√(3)")]
    [InlineData("1÷(1+2√(2))", "-1⌟7+2√(2)⌟7")]
    [InlineData("1÷(√(2)+1)", "-1+√(2)")]
    [InlineData("√(2)+√(3)+√(5)+√(7)-√(5)-√(7)", "√(3)+√(2)")]
    [InlineData("√(2)+√(3)+√(5)+√(7)+√(11)-√(5)-√(7)-√(11)", "3.14626437")]
    public void FormsThroughDivisionAndCancellation(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void FormsEndWhereTheirNumbersEnd()
    {
        // Radicands are factored up to 10¹⁰, and a coefficient that overflows decimal drops the form, not the value.
        Calculator.Evaluate("√(10000000000)").IsExact.Should().BeTrue();
        Calculator.Evaluate("√(10000000001)²").IsExact.Should().BeFalse();
        Calculator.Evaluate("(√(100003)×√(100019))²").IsExact.Should().BeFalse();
        Calculator.Display("√(2)×" + Max + "+√(2)×" + Max).Should().Be("2.240910839×10^29");
        Calculator.Display("(√(2)×" + Max + ")×(√(3)×" + Max + ")").Should().Be("1.537569632×10^58");
    }

    [Fact]
    public void LargeCoefficientsAreSnappedToo()
    {
        // 9999/7 × 7 is 9998.999…997 in decimal; the tolerance grows with the coefficient, so the form says 9999.
        Calculator.Evaluate("((9999÷7×√(2))×7)²").ToDecimal().Should().Be(199960002m);
    }

    [Fact]
    public void TheSessionChecksItsArguments()
    {
        CalculatorSession session = Calculator.Session();
        Calculation calculation = session.Calculate("1");

        Action define = () => session.Define(DefinedFunction.F, null!);
        Action calculate = () => session.Calculate(null!);
        Action values = () => session.Calculate("A", (IReadOnlyDictionary<MemoryVariable, Value>)null!);
        Action format = () => session.Format(null!);
        Action engineering = () => session.FormatEngineering(null!, 0);

        define.Should().Throw<ArgumentNullException>().WithParameterName("expression");
        calculate.Should().Throw<ArgumentNullException>().WithParameterName("input");
        values.Should().Throw<ArgumentNullException>().WithParameterName("values");
        format.Should().Throw<ArgumentNullException>().WithParameterName("calculation");
        engineering.Should().Throw<ArgumentNullException>().WithParameterName("calculation");
        session.FormatEngineering(calculation, 0).Should().Be("1×10^0");
    }

    [Fact]
    public void StayingInCalculate_KeepsPreAns()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("2");
        session.Calculate("3");

        session.SwitchApp(CalculatorApp.Calculate);

        session.PreAns.ToDecimal().Should().Be(2m);
    }

    [Theory]
    [InlineData("1<1", false)]
    [InlineData("1≤1", true)]
    public void VerifyOfEqualValues(string input, bool expected)
    {
        Calculator.Session(settings: settings => settings with { Verify = true }).Calculate(input).IsTrue.Should().Be(expected);
    }

    [Theory]
    // Complex values verify equal part by part (p. 128).
    [InlineData("1+i=1+i", true)]
    [InlineData("1+i=2+i", false)]
    [InlineData("1+i=1+2i", false)]
    [InlineData("1+i≠2+i", true)]
    [InlineData("1+i≠1+i", false)]
    public void VerifyOfComplexValues(string input, bool expected)
    {
        Calculator.Session(CalculatorApp.Complex, settings: settings => settings with { Verify = true }).Calculate(input).IsTrue.Should().Be(expected);
    }

    [Theory]
    // A statement stops at its first operand that fails.
    [InlineData("1÷0=1", true)]
    [InlineData("(1÷0)÷R2", false)]
    [InlineData("Pol(1÷0,1)", false)]
    [InlineData("Pol(1,1÷0)", false)]
    [InlineData("Rec(1÷0,1)", false)]
    [InlineData("Pol(10^60,1)", false)]
    [InlineData("Pol(1,10^60)", false)]
    [InlineData("Rec(1,10^10)", false)]
    public void StatementsWithAFailingOperand_AreMathErrors(string input, bool verify)
    {
        Calculator.Session(settings: settings => settings with { Verify = verify }).Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void CoordinateConversions_StoreTheirFirstResultInAns()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("Pol(3,4)");
        session.Calculate("Ans").Result.ToDecimal().Should().Be(5m);

        Calculation origin = session.Calculate("Pol(0,1)");
        origin.Second!.Value.ToDecimal().Should().Be(90m);
        session.Calculate("Rec(0,30)").Display.Text.Should().Be("x=0, y=0");
    }

    [Fact]
    public void CoordinateConversionsOfAComplexPluginResult_AreMathErrors()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().AddFunction(new ComplexValue()).Build().CreateSession();

        session.Calculate("Pol(cplx(1),1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("Pol(1,cplx(1))").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("Rec(cplx(1),1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("Rec(1,cplx(1))").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void BaseNResults_BecomeDecimalsInTheOtherApplications()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.BaseN);
        session.Calculate("5");

        session.SwitchApp(CalculatorApp.Complex);
        session.Calculate("Ans").Result.Kind.Should().Be(ValueKind.DecimalReal);

        session.SwitchApp(CalculatorApp.BaseN);
        session.Calculate("7");
        session.SwitchApp(CalculatorApp.Calculate);
        session.Calculate("Ans").Result.Kind.Should().Be(ValueKind.DecimalReal);
    }

    [Theory]
    [InlineData(2147483647, 2147483647)]
    [InlineData(-2147483648, -2147483648)]
    [InlineData(2147483647.9, 2147483647)]
    public void MemoryEntersBaseNAtBothEndsOfTheRange(decimal stored, int expected)
    {
        CalculatorSession session = Calculator.Session();
        session.SetVariable(MemoryVariable.X, Value.FromDecimal(stored));

        session.SwitchApp(CalculatorApp.BaseN);

        session.Calculate("x").Result.ToInt32().Should().Be(expected);
    }

    [Theory]
    [InlineData("-1234", "Sci3", "-1.23×10^3")]
    [InlineData("10000000000", "Fix2", "1.00×10^10")]
    [InlineData("-0.001", "Fix2", "0.00")]
    [InlineData("10^-20", "Fix0", "0")]
    [InlineData("10^-20", "Fix2", "0.00")]
    public void NumberFormatsAtTheirEdges(string input, string format, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = format switch
            {
                "Sci3" => NumberFormat.Sci(3),
                "Fix2" => NumberFormat.Fix(2),
                _ => NumberFormat.Fix(0),
            },
        }).Should().Be(expected);
    }

    [Fact]
    public void EngineeringShiftedFar_PadsTheMantissa()
    {
        CalculatorSession session = Calculator.Session();

        session.FormatEngineering(session.Calculate("1024000"), -4).Should().Be("1024000000000×10^-6");
    }

    [Theory]
    // Digit separators group the integer part only (p. 25).
    [InlineData("123456", "123,456")]
    [InlineData("1234.5678", "1,234.5678")]
    [InlineData("1234567", "1,234,567")]
    [InlineData("1.5×10^-20", "1.5×10^-20")]
    public void DigitSeparatorsGroupTheIntegerPart(string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { DigitSeparator = true, InputOutput = InputOutput.MathIDecimalO })
            .Should().Be(expected);
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
