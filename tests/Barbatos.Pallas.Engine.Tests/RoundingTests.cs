// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Rounding to significant digits, which happens at every display and in <c>Rnd(</c> (docs/PRECISION.md §5).
/// </summary>
public sealed class RoundingTests
{
    private static string Sci(string input, int digits)
    {
        return Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Sci(digits),
        });
    }

    [Theory]
    // Significant digits are counted from the first nonzero one, at every magnitude.
    [InlineData("123456789", 4, "1.235×10^8")]
    [InlineData("0.000123456789", 4, "1.235×10^-4")]
    [InlineData("123456789012345678901234567", 4, "1.235×10^26")]
    [InlineData("0.0000000000001234", 3, "1.23×10^-13")]
    [InlineData("9.995", 3, "1.00×10^1")]
    [InlineData("0.09995", 3, "1.00×10^-1")]
    [InlineData("999999999999", 1, "1×10^12")]
    [InlineData("0", 5, "0.0000×10^0")]
    public void SignificantDigits_AreRoundedHalfAwayFromZero(string input, int digits, string expected)
    {
        Sci(input, digits).Should().Be(expected);
    }

    [Theory]
    // Rnd replaces the value, so the rounding must be exact in decimal, at every magnitude.
    [InlineData("Rnd(123456789)", 4, "123500000")]
    [InlineData("Rnd(0.000123456789)", 4, "0.0001235")]
    [InlineData("Rnd(1.0000000000000000000000005)", 10, "1")]
    [InlineData("Rnd(-2.5)", 1, "-3")]
    public void RoundOff_ReplacesTheValueAtEveryMagnitude(string input, int digits, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Sci(digits),
        }).Should().Be(Sci(expected, digits));
    }

    [Fact]
    public void RoundOff_IsExactInDecimal()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Sci(4),
        });

        session.Calculate("Rnd(123456789)").Result.ToDecimal().Should().Be(123500000m);
        session.Calculate("Rnd(0.000123456789)").Result.ToDecimal().Should().Be(0.0001235m);
        session.Calculate("Rnd(1234.5678)").Result.ToDecimal().Should().Be(1235m);
    }

    [Fact]
    public void TheDerivativeOfRnd_JumpsWhereTheRoundingDoes()
    {
        // Rnd is constant between its jumps: the derivative is 0 there and a Math ERROR at a tie.
        CalculatorSession session = Calculator.Session(settings: settings => settings with
        {
            AngleUnit = Numerics.AngleUnit.Radian,
            NumberFormat = NumberFormat.Fix(2),
        });

        session.Calculate("d/dx(Rnd(x),1.2345)").Display.Text.Should().Be("0.00", "Fix 2 displays the zero with two decimals");
        session.Calculate("d/dx(Rnd(x),1.005)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);

        session.Settings = session.Settings with { NumberFormat = NumberFormat.Sci(3) };
        session.Calculate("d/dx(Rnd(x),1.235)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "1.235 is a tie at three significant digits");
        session.Calculate("d/dx(Rnd(x),-1.235)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "the tie is the same on the negative side");
        session.Calculate("d/dx(Rnd(x),123.5)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError, "significant digits follow the magnitude");
        session.Calculate("d/dx(Rnd(x),1.2344)").Display.Text.Should().Be("0.00×10^0");

        session.Settings = session.Settings with { NumberFormat = NumberFormat.Norm1 };
        session.Calculate("d/dx(Rnd(x),1.2345)").Display.Text.Should().Be("0", "Norm rounds at ten digits");
        session.Calculate("d/dx(Rnd(x),0)").Display.Text.Should().Be("0");
    }

    [Fact]
    public void TheDerivativeOfRnd_OnAValueBeyondDecimal_IsZero()
    {
        Calculator.Display("d/dx(Rnd(x×10^30),1)", settings: settings => settings with { AngleUnit = Numerics.AngleUnit.Radian }).Should().Be("0");
    }

    [Theory]
    // Fix rounds to decimal places, and carries into the integer part.
    [InlineData("0.999", 2, "1.00")]
    [InlineData("-0.999", 2, "-1.00")]
    [InlineData("0.005", 2, "0.01")]
    [InlineData("1234.5", 0, "1235")]
    public void Fix_CarriesItsRounding(string input, int decimals, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = NumberFormat.Fix(decimals),
        }).Should().Be(expected);
    }

    [Fact]
    public void NormRoundsToTenDigitsAndTrimsTheZeros()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO });

        session.Calculate("2÷3").Display.Text.Should().Be("0.6666666667");
        session.Calculate("1÷8").Display.Text.Should().Be("0.125");
        session.Calculate("1.00000000004").Display.Text.Should().Be("1");
        session.Calculate("1.0000000006").Display.Text.Should().Be("1.000000001");
    }
}
