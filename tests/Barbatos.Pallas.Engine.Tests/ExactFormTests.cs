// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Exact forms: what they keep, what they drop, and the exactness they give back.
/// </summary>
public sealed class ExactFormTests
{
    [Theory]
    // A form survives the operations it can represent.
    [InlineData("√(2)×√(2)", "2")]
    [InlineData("√(2)×√(8)", "4")]
    [InlineData("√(2)²", "2")]
    [InlineData("√(8)", "2√(2)")]
    [InlineData("√(0.25)", "1⌟2")]
    [InlineData("√(1⌟3)", "√(3)⌟3")]
    [InlineData("√(2)+√(2)", "2√(2)")]
    [InlineData("√(2)-√(2)", "0")]
    [InlineData("3√(2)÷√(2)", "3")]
    [InlineData("√(2)×π", "4.442882938")]
    [InlineData("π×√(2)", "4.442882938")]
    [InlineData("(√(2)+1)²", "3+2√(2)")]
    [InlineData("√(2)^3", "2√(2)")]
    [InlineData("√(2)^-1", "√(2)⌟2")]
    [InlineData("π÷π", "1")]
    [InlineData("2π÷π", "2")]
    [InlineData("π×2÷4", "1⌟2π")]
    public void FormsFollowTheArithmetic(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void AFormThatBecomesRational_MakesTheValueExactAgain()
    {
        Calculator.Evaluate("√(2)×√(2)").IsExact.Should().BeTrue();
        Calculator.Evaluate("√(2)×√(2)").ToDecimal().Should().Be(2m);
        Calculator.Evaluate("(√(3))²-3").ToDecimal().Should().Be(0m, "the surds cancel exactly");
        Calculator.Evaluate("√(2)").IsExact.Should().BeFalse("a square root is not a decimal");
    }

    [Theory]
    // Where a form cannot be kept, the value is still right; only the display is a decimal.
    [InlineData("π×π", "9.869604401")]
    [InlineData("√(2)×√(3)", "√(6)")]
    [InlineData("√(2)+√(3)+√(5)+√(7)", "8.028083659")]
    [InlineData("1÷(√(2)+√(3))", "0.3178372452")]
    [InlineData("√(π)", "1.772453851")]
    [InlineData("√(2)^0.5", "1.189207115")]
    public void FormsAreDroppedRatherThanGuessed(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Fact]
    public void AFormReachesTheFunctionsWithAllItsDigits()
    {
        // The decimal of √2 keeps 15 digits; the form keeps the double, so the product is 1, not 1.0000000000000042.
        Calculator.Display("Rec(√(2),45)").Should().Be("x=1, y=1");
        Calculator.Evaluate("√(2)×√(2)").ToDouble().Should().Be(2d);
        Calculator.Display("sin(π÷6)", settings: settings => settings with { AngleUnit = AngleUnit.Radian }).Should().Be("1⌟2");
        Calculator.Display("sin(π)", settings: settings => settings with { AngleUnit = AngleUnit.Radian }).Should().Be("0");
    }

    [Fact]
    public void EKeepsItsDigitsToo()
    {
        // e as a decimal is 2.71828182845904, one unit short in the 15th digit; the form gives ln(e) = 1 exactly.
        Calculator.Display("ln(e)").Should().Be("1");
        Calculator.Evaluate("e").ToDouble().Should().Be(double.E);
        Calculator.Display("e÷e").Should().Be("1");
        Calculator.Display("e").Should().Be("2.718281828", "e has no display form");
    }

    [Fact]
    public void AnAngleInDegreesBecomesAnExactMultipleOfPi()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { AngleUnit = AngleUnit.Radian });

        session.Calculate("90°").Display.Text.Should().Be("1⌟2π");
        session.Calculate("sin(90°)").Display.Text.Should().Be("1");
        session.Calculate("cos(180°)").Display.Text.Should().Be("-1");
    }

    [Fact]
    public void FormsSurviveThroughMemory()
    {
        CalculatorSession session = Calculator.Session();

        session.Calculate("√(2)");
        session.Calculate("Ans×Ans").Display.Text.Should().Be("2");

        session.Calculate("√(3)");
        session.Store(MemoryVariable.A);
        session.Calculate("A²").Display.Text.Should().Be("3");
    }

    [Fact]
    public void ARadicandTooLargeToFactor_LosesItsForm()
    {
        // The engine factors radicands up to 10¹⁰; beyond that the value stands on its own.
        Calculator.Display("√(123456789012345)").Should().Be("11111111.06");
        Calculator.Display("√(10000000000)").Should().Be("100000");
    }

    [Fact]
    public void AFormWithTooManyTerms_IsDropped()
    {
        // The engine keeps at most four terms; the fifth drops the form, not the value.
        Calculator.Display("(1+√(2))×(1+√(3))").Should().Be("6.595754113");
        Calculator.Evaluate("(1+√(2))×(1+√(3))").ToDouble().Should().BeApproximately(6.5957541128, 1e-9);
    }

    [Fact]
    public void CoefficientsAreSnappedToTheFractionTheyStandFor()
    {
        // 1/3 × 3 is 0.9999999999999999999999999999 in decimal; the form says 1.
        Calculator.Display("√(2)÷3×3").Should().Be("√(2)");
        Calculator.Display("(√(2)÷3)×(3√(2))").Should().Be("2");
    }
}
