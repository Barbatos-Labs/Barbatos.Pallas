// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Verify (manual pp. 73-76): equations and inequalities, with the comparison rule of docs/PRECISION.md §9.
/// </summary>
public sealed class VerifyTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> VerifyOn = settings => settings with { Verify = true };

    [Theory]
    [InlineData("4√(9)=12", true)]
    [InlineData("4=√(16)", true)]
    [InlineData("4≠3", true)]
    [InlineData("π>3", true)]
    [InlineData("1+2≤5", true)]
    [InlineData("(3×6)<(2+6)×2", false)]
    [InlineData("0<(8⌟9)²-8⌟9", false)]
    [InlineData("1≤1<1+1", true)]
    [InlineData("3<π<4", true)]
    [InlineData("2²=2+2=4", true)]
    [InlineData("2+3=5≠2+5=8", false)]
    public void Examples_MatchTheManual(string input, bool expected)
    {
        CalculatorSession session = Calculator.Session(settings: VerifyOn);
        Calculation calculation = session.Calculate(input);

        calculation.IsTrue.Should().Be(expected);
        calculation.Display.Text.Should().Be(expected ? "True" : "False");
        session.Ans.ToDecimal().Should().Be(expected ? 1m : 0m, "Verify stores 1 for True and 0 for False (p. 76)");
    }

    [Fact]
    public void AnExpressionWithoutARelation_HasNoOperator()
    {
        Calculator.Error("5", settings: VerifyOn).Kind.Should().Be(CalcErrorKind.NoOperator);
    }

    [Theory]
    [InlineData("5≤6≥4")]
    [InlineData("4<6≠8")]
    public void ChainsThatDoNotPointOneWay_AreASyntaxError(string input)
    {
        Calculator.Error(input, settings: VerifyOn).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void ARelationWithVerifyOff_IsASyntaxError()
    {
        Calculator.Error("1=1").Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void VerifyUsesTheValueStoredInAVariable()
    {
        // Manual p. 76: the identities hold for whatever x currently is.
        CalculatorSession session = Calculator.Session(settings: VerifyOn);
        session.SetVariable(MemoryVariable.X, Value.FromDecimal(3m));

        session.Calculate("(x+1)(x+5)=x²+x+5x+5").IsTrue.Should().BeTrue();
        session.Calculate("x²+x+5x+5=x²+6x+5").IsTrue.Should().BeTrue();
    }

    [Fact]
    public void ValuesThatWentThroughDouble_CompareWithinTolerance()
    {
        // Assumption U6: (√2)² is 2. Its decimal is 2.000000000000014, so the comparison is not bit for bit; the exact
        // form makes this one exact, and the tolerance covers the rest.
        CalculatorSession session = Calculator.Session(settings: VerifyOn);

        session.Calculate("(√(2))²=2").IsTrue.Should().BeTrue();
        session.Calculate("sin(45)²+cos(45)²=1").IsTrue.Should().BeTrue();
        session.Calculate("sin(30)=0.5").IsTrue.Should().BeTrue();
    }

    [Fact]
    public void ValuesThatDifferInTheTenthDigit_AreNotEqual()
    {
        // The tolerance is 10⁻¹³ relative: far finer than the 10 digits displayed.
        CalculatorSession session = Calculator.Session(settings: VerifyOn);

        session.Calculate("1.0000000001=1").IsTrue.Should().BeFalse();
        session.Calculate("1.000000000001=1").IsTrue.Should().BeFalse();
        session.Calculate("0.1+0.2=0.3").IsTrue.Should().BeTrue("decimal arithmetic is exact");
    }

    [Fact]
    public void StrictInequalities_AreFalseForValuesThatCountAsEqual()
    {
        CalculatorSession session = Calculator.Session(settings: VerifyOn);

        session.Calculate("(√(2))²>2").IsTrue.Should().BeFalse();
        session.Calculate("(√(2))²≥2").IsTrue.Should().BeTrue();
    }

    [Fact]
    public void ComplexNumbers_CanBeVerifiedForEqualityOnly()
    {
        // Manual p. 128: i² = −1 is True, and an inequality with a complex number is a Math ERROR.
        CalculatorSession session = Calculator.Session(CalculatorApp.Complex, settings: VerifyOn);

        session.Calculate("i²=-1").IsTrue.Should().BeTrue();
        session.Calculate("(1+i)(1-i)=2").IsTrue.Should().BeTrue();
        session.Calculate("2+3i≠2-3i").IsTrue.Should().BeTrue();
        session.Calculate("i<2").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }
}
