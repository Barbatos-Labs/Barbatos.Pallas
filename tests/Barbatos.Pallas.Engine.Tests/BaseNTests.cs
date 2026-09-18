// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Base-N application (manual pp. 129-132): 32-bit two's complement integers in four number modes.
/// </summary>
public sealed class BaseNTests
{
    private static CalculatorSession Session(NumberBase mode = NumberBase.Dec)
    {
        return Calculator.Session(CalculatorApp.BaseN, settings: settings => settings with { BaseMode = mode });
    }

    [Theory]
    [InlineData(NumberBase.Bin, "11+1", "100")]
    [InlineData(NumberBase.Hex, "1F+1", "20")]
    [InlineData(NumberBase.Dec, "d10+h10+b10+o10", "36")]
    [InlineData(NumberBase.Bin, "1010 and 1100", "1000")]
    [InlineData(NumberBase.Bin, "1010 or 1100", "1110")]
    [InlineData(NumberBase.Bin, "1010 xor 1100", "110")]
    [InlineData(NumberBase.Bin, "1010 xnor 1100", "11111111111111111111111111111001")]
    [InlineData(NumberBase.Bin, "Not(1010)", "11111111111111111111111111110101")]
    [InlineData(NumberBase.Dec, "Neg(10)", "-10")]
    [InlineData(NumberBase.Hex, "FFFFFFFF", "FFFFFFFF")]
    public void Examples_MatchTheManual(NumberBase mode, string input, string expected)
    {
        Session(mode).Calculate(input).Display.Text.Should().Be(expected);
    }

    [Fact]
    public void HexadecimalPatterns_AreSignedValues()
    {
        // FFFFFFFF is −1: the other bases write the 32-bit pattern, decimal writes the signed value (p. 130).
        CalculatorSession session = Session(NumberBase.Hex);
        Calculation calculation = session.Calculate("FFFFFFFF");

        calculation.Result.ToInt32().Should().Be(-1);
        session.Settings = session.Settings with { BaseMode = NumberBase.Dec };
        session.Format(calculation)!.Text.Should().Be("-1");
    }

    [Fact]
    public void ChangingTheNumberMode_ShowsTheSameValueDifferently()
    {
        // Manual p. 131: 15 × 37 is 555, which is 22B in hexadecimal.
        CalculatorSession session = Session();
        Calculation calculation = session.Calculate("15×37");

        calculation.Display.Text.Should().Be("555");
        session.Settings = session.Settings with { BaseMode = NumberBase.Hex };
        session.Format(calculation)!.Text.Should().Be("22B");
    }

    [Theory]
    [InlineData(NumberBase.Dec, "2147483647+1")]
    [InlineData(NumberBase.Dec, "-2147483648-1")]
    [InlineData(NumberBase.Hex, "7FFFFFFF+1")]
    [InlineData(NumberBase.Dec, "2147483648")]
    [InlineData(NumberBase.Hex, "1FFFFFFFF")]
    [InlineData(NumberBase.Dec, "1÷0")]
    public void ResultsOutsideThe32BitRange_AreMathErrors(NumberBase mode, string input)
    {
        Session(mode).Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheLowestValue_CanBeWritten()
    {
        Session().Calculate("-2147483648").Result.ToInt32().Should().Be(int.MinValue);
    }

    [Fact]
    public void DivisionDropsTheFractionalPart()
    {
        // Manual p. 130: a fractional result is cut.
        Session().Calculate("7÷2").Display.Text.Should().Be("3");
        Session().Calculate("-7÷2").Display.Text.Should().Be("-3");
    }

    [Fact]
    public void DigitsOutsideTheNumberMode_AreASyntaxError()
    {
        Session(NumberBase.Bin).Calculate("102").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
        Session(NumberBase.Dec).Calculate("b12").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void TheCatalogCommandsAreNotAvailable()
    {
        // Manual p. 51: the commands, functions and symbols of the Advanced Calculations chapter are not in Base-N.
        foreach (string input in (string[])["sin(10)", "10²", "1⌟2", "10P2", "π", "5_k", "2^3", "10!", "√(4)"])
        {
            Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "'{0}' is not available in Base-N", input);
        }
    }

    [Fact]
    public void ValuesFromOtherApplications_LoseTheirFractionalPart()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("7÷2");
        session.SwitchApp(CalculatorApp.BaseN);

        session.Calculate("Ans+1").Display.Text.Should().Be("4");
    }

    [Fact]
    public void ValuesTooLargeForBaseN_AreMathErrors()
    {
        CalculatorSession session = Calculator.Session();
        session.Calculate("10^12");
        session.SwitchApp(CalculatorApp.BaseN);

        session.Calculate("Ans").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void BaseNValues_ReturnToDecimalInOtherApplications()
    {
        CalculatorSession session = Session(NumberBase.Hex);
        session.Calculate("FF");
        session.SwitchApp(CalculatorApp.Calculate);

        session.Calculate("Ans÷2").Display.Text.Should().Be("255⌟2");
    }
}
