// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The arithmetic rules value by value: which type a result is held in, whether it stays exact, and the domains of
/// the reference calculator (pp. 170-171).
/// </summary>
public sealed class ValueMathTests
{
    private static readonly Func<CalculatorSettings, CalculatorSettings> Decimals =
        settings => settings with { InputOutput = InputOutput.MathIDecimalO };

    [Theory]
    // Exactness is lost as soon as one operand went through double, whatever the operation.
    [InlineData("1+2", true)]
    [InlineData("1+sin(30)", false)]
    [InlineData("sin(30)+1", false)]
    [InlineData("2×sin(30)", false)]
    [InlineData("sin(30)÷2", false)]
    [InlineData("1-sin(30)", false)]
    [InlineData("sin(30)²", false)]
    [InlineData("-sin(30)", false)]
    [InlineData("1⌟2+1⌟3", true)]
    [InlineData("Int(sin(30)×3)", false)]
    [InlineData("Rnd(sin(30))", true)]
    public void Exactness_FollowsTheOperands(string input, bool exact)
    {
        Calculator.Evaluate(input).IsExact.Should().Be(exact);
    }

    [Theory]
    // Beyond decimal's range the arithmetic continues in double, and the kind says so.
    [InlineData("10^30+10^30", ValueKind.DoubleReal)]
    [InlineData("10^30-10^29", ValueKind.DoubleReal)]
    [InlineData("10^30×10^30", ValueKind.DoubleReal)]
    [InlineData("10^30÷10^15", ValueKind.DecimalReal)]
    [InlineData("10^-20×10^-20", ValueKind.DoubleReal)]
    [InlineData("10^-20÷10^20", ValueKind.DoubleReal)]
    [InlineData("10^-20+10^-20", ValueKind.DoubleReal)]
    [InlineData("1÷10^20", ValueKind.DoubleReal)]
    [InlineData("10^20×10^-20", ValueKind.DecimalReal)]
    public void ThePrecisionRule_ChoosesTheTypeOfEveryResult(string input, ValueKind kind)
    {
        Calculator.Evaluate(input, settings: Decimals).Kind.Should().Be(kind);
    }

    [Fact]
    public void ArithmeticOnDoubleValues_KeepsWorking()
    {
        CalculatorSession session = Calculator.Session(settings: Decimals);

        session.Calculate("10^30+10^30").Display.Text.Should().Be("2×10^30");
        session.Calculate("10^30-10^30").Display.Text.Should().Be("0");
        session.Calculate("10^30×2").Display.Text.Should().Be("2×10^30");
        session.Calculate("10^30÷10^30").Display.Text.Should().Be("1");
        session.Calculate("-10^30").Display.Text.Should().Be("-1×10^30");
        session.Calculate("10^-20+10^-20").Display.Text.Should().Be("2×10^-20");
    }

    [Fact]
    public void DividingByZero_IsAMathErrorForEveryType()
    {
        Calculator.Error("1÷0").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("10^30÷0").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("1÷(1-1)").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("i÷0", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // The domain of xʸ and of 10ˣ (pp. 170-171).
    [InlineData("10^99.99999999", true)]
    [InlineData("10^100", false)]
    [InlineData("2^400", false)]
    [InlineData("(-8)^(2⌟3)", true)]
    [InlineData("(-8)^0.5", false)]
    [InlineData("(-8)^2", true)]
    [InlineData("0^2", true)]
    [InlineData("0^0.5", true)]
    [InlineData("0^-2", false)]
    [InlineData("0^0", false)]
    public void PowersFollowTheirDomain(string input, bool succeeds)
    {
        Calculation calculation = Calculator.Session(settings: Decimals).Calculate(input);

        calculation.Succeeded.Should().Be(succeeds, "'{0}'", input);
    }

    [Fact]
    public void TenToTheHundred_IsOutOfRangeEvenInsideTheDomain()
    {
        // The Extended profile has no such limit.
        Calculator.Session(profile: CalculatorProfile.Extended, settings: Decimals).Calculate("10^100").Error.Should().BeNull();
        Calculator.Session(profile: CalculatorProfile.Extended, settings: Decimals).Calculate("10^400").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData("3ˣ√(27)", "3")]
    [InlineData("3ˣ√(-27)", "-3")]
    [InlineData("4ˣ√(16)", "2")]
    [InlineData("2ˣ√(9)", "3")]
    [InlineData("(-2)ˣ√(4)", "0.5")]
    [InlineData("0.5ˣ√(4)", "16")]
    public void RootsFollowTheirIndex(string input, string expected)
    {
        Calculator.Display(input, settings: Decimals).Should().Be(expected);
    }

    [Theory]
    [InlineData("4ˣ√(-16)")]
    [InlineData("0ˣ√(16)")]
    [InlineData("2ˣ√(-16)")]
    public void RootsOutsideTheirDomain_AreMathErrors(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ComplexRootsAndPowers_AreAvailableInComplexOnly()
    {
        Calculator.Session(CalculatorApp.Complex).Calculate("2ˣ√(-16)").Result.Kind.Should().Be(ValueKind.Complex);
        Calculator.Session(CalculatorApp.Complex).Calculate("(-8)^0.5").Result.Kind.Should().Be(ValueKind.Complex);
        Calculator.Session(CalculatorApp.Complex).Calculate("4ˣ√(-16)").Result.Kind.Should().Be(ValueKind.Complex);
    }

    [Theory]
    // n! ≤ 69 on the calculator, 170 in the Extended profile (beyond it double overflows).
    [InlineData("69!", CalculatorProfile.Standard, true)]
    [InlineData("70!", CalculatorProfile.Standard, false)]
    [InlineData("170!", CalculatorProfile.Extended, true)]
    [InlineData("171!", CalculatorProfile.Extended, false)]
    [InlineData("0!", CalculatorProfile.Standard, true)]
    public void FactorialsFollowTheProfile(string input, CalculatorProfile profile, bool succeeds)
    {
        Calculator.Session(profile: profile, settings: Decimals).Calculate(input).Succeeded.Should().Be(succeeds);
    }

    [Theory]
    [InlineData("10P0", "1")]
    [InlineData("10P10", "3628800")]
    [InlineData("10C0", "1")]
    [InlineData("10C10", "1")]
    [InlineData("10C8", "45")]
    [InlineData("100C2", "4950")]
    public void SelectionsAreExact(string input, string expected)
    {
        Calculator.Display(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("10P11")]
    [InlineData("10C11")]
    [InlineData("(-1)P1")]
    [InlineData("10P(-1)")]
    [InlineData("10000000000P2")]
    [InlineData("2.5P1")]
    public void SelectionsOutsideTheirDomain_AreMathErrors(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void LargeSelections_AreRefusedByTheProfile()
    {
        // C(n, k) above 10¹⁰⁰ in the calculator's profile, above double's range in the Extended one.
        Calculator.Error("1000C400").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("1000C400").Error.Should().BeNull();
        Calculator.Session(profile: CalculatorProfile.Extended).Calculate("100000C50000").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // GCD and LCM take integers below 10¹⁰; LCM takes non-negative ones (p. 171).
    [InlineData("GCD(-28,35)", true)]
    [InlineData("GCD(10000000000,5)", false)]
    [InlineData("LCM(-9,15)", false)]
    [InlineData("LCM(0,15)", true)]
    [InlineData("GCD(0,0)", true)]
    public void IntegerFunctionsFollowTheirDomain(string input, bool succeeds)
    {
        Calculator.Session().Calculate(input).Succeeded.Should().Be(succeeds);
    }

    [Fact]
    public void TheExtendedProfile_TakesNegativeMultiplesAndLargerIntegers()
    {
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended);

        session.Calculate("LCM(-9,15)").Display.Text.Should().Be("45");
        session.Calculate("GCD(10000000000,5)").Display.Text.Should().Be("5");
    }

    [Theory]
    // pp. 170-171: the angle domains, and the hyperbolic limits.
    [InlineData("sin(9000000000)", AngleUnit.Degree, false)]
    [InlineData("sin(8999999999)", AngleUnit.Degree, true)]
    [InlineData("sin(157079633)", AngleUnit.Radian, false)]
    [InlineData("sin(157079632)", AngleUnit.Radian, true)]
    [InlineData("cos(10000000000)", AngleUnit.Gradian, false)]
    [InlineData("tan(9999999999)", AngleUnit.Gradian, true)]
    public void AngleDomains_FollowTheAngleUnit(string input, AngleUnit unit, bool succeeds)
    {
        Calculator.Session(settings: settings => settings with { AngleUnit = unit, InputOutput = InputOutput.MathIDecimalO })
            .Calculate(input).Succeeded.Should().Be(succeeds);
    }

    [Theory]
    [InlineData("sinh(230.2585092)", true)]
    [InlineData("sinh(230.2585093)", false)]
    [InlineData("cosh(-230.2585092)", true)]
    [InlineData("tanh⁻¹(0.9999999999)", true)]
    [InlineData("tanh⁻¹(0.99999999999)", false)]
    [InlineData("sinh⁻¹(10^99)", true)]
    [InlineData("sinh⁻¹(6×10^99)", false)]
    [InlineData("cosh⁻¹(0.5)", false)]
    [InlineData("tanh(10^30)", true)]
    public void HyperbolicDomains_FollowTheCalculator(string input, bool succeeds)
    {
        Calculator.Session(settings: Decimals).Calculate(input).Succeeded.Should().Be(succeeds);
    }

    [Fact]
    public void TheExtendedProfile_HasNoAngleOrHyperbolicLimits()
    {
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended, settings: Decimals);

        session.Calculate("sin(9000000000)").Error.Should().BeNull();
        session.Calculate("sinh(300)").Error.Should().BeNull();
        session.Calculate("tanh⁻¹(0.99999999999)").Error.Should().BeNull();
    }

    [Theory]
    [InlineData("log(1000)", "3")]
    [InlineData("log(2,16)", "4")]
    [InlineData("log(0.5,0.25)", "2")]
    [InlineData("ln(1)", "0")]
    public void LogarithmsAreExactWhereTheyCanBe(string input, string expected)
    {
        Calculator.Display(input, settings: Decimals).Should().Be(expected);
    }

    [Theory]
    [InlineData("log(-2,4)")]
    [InlineData("log(1,4)")]
    [InlineData("log(2,-4)")]
    [InlineData("log(2,0)")]
    [InlineData("log(-1)")]
    [InlineData("ln(-1)")]
    public void LogarithmsOutsideTheirDomain_AreMathErrors(string input)
    {
        Calculator.Error(input).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData(AngleUnit.Degree, "90°", "90")]
    [InlineData(AngleUnit.Degree, "100ᵍ", "90")]
    [InlineData(AngleUnit.Gradian, "90°", "100")]
    [InlineData(AngleUnit.Gradian, "(π÷2)ʳ", "100")]
    [InlineData(AngleUnit.Radian, "200ᵍ", "π")]
    [InlineData(AngleUnit.Radian, "1ʳ", "1")]
    [InlineData(AngleUnit.Degree, "45°", "45")]
    public void AngleMarks_ConvertIntoTheCurrentUnit(AngleUnit unit, string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with { AngleUnit = unit }).Should().Be(expected);
    }

    [Fact]
    public void AnAngleMarkOnAComplexValue_IsAMathError()
    {
        Calculator.Error("(2+3i)°", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData("1°30′0″", "1.5")]
    [InlineData("-1°30′0″", "-1.5")]
    [InlineData("0°0′36″", "0.01")]
    [InlineData("100°0′0″", "100")]
    public void SexagesimalValues_AreExact(string input, string expected)
    {
        Calculator.Display(input, settings: Decimals).Should().Be(expected);
    }

    [Theory]
    [InlineData("-1⌟1⌟2", "-1.5")]
    [InlineData("2⌟1⌟2", "2.5")]
    [InlineData("0⌟1⌟2", "0.5")]
    public void MixedFractions_TakeTheSignOfTheWholePart(string input, string expected)
    {
        Calculator.Display(input, settings: Decimals).Should().Be(expected);
    }

    [Fact]
    public void AMixedFractionOverZero_IsAMathError()
    {
        Calculator.Error("1⌟1⌟0").Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData("Abs(-2.5)", "2.5")]
    [InlineData("Abs(2.5)", "2.5")]
    [InlineData("Abs(0)", "0")]
    [InlineData("Abs(-10^30)", "1×10^30")]
    [InlineData("Int(2.7)", "2")]
    [InlineData("Int(-2.7)", "-2")]
    [InlineData("Intg(2.7)", "2")]
    [InlineData("Intg(-2.7)", "-3")]
    [InlineData("Int(10^30)", "1×10^30")]
    [InlineData("Intg(-10^30)", "-1×10^30")]
    public void IntegerPartsAndAbsoluteValues(string input, string expected)
    {
        Calculator.Display(input, settings: Decimals).Should().Be(expected);
    }

    [Fact]
    public void IntegerPartsOfComplexValues_AreMathErrors()
    {
        Calculator.Error("Int(2+3i)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("Intg(2+3i)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Error("Rnd(2+3i)", CalculatorApp.Complex).Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // Rnd rounds to the display format (p. 60), whatever that format is.
    [InlineData("Fix2", "Rnd(1.005)", "1.01")]
    [InlineData("Fix0", "Rnd(2.5)", "3")]
    [InlineData("Sci3", "Rnd(1234.5)", "1.23×10^3")]
    [InlineData("Norm1", "Rnd(1.00000000005)", "1")]
    public void RoundOff_FollowsTheNumberFormat(string format, string input, string expected)
    {
        Calculator.Display(input, settings: settings => settings with
        {
            InputOutput = InputOutput.MathIDecimalO,
            NumberFormat = format switch
            {
                "Fix2" => NumberFormat.Fix(2),
                "Fix0" => NumberFormat.Fix(0),
                "Sci3" => NumberFormat.Sci(3),
                _ => NumberFormat.Norm1,
            },
        }).Should().Be(expected);
    }

    [Fact]
    public void PercentAndReciprocal()
    {
        Calculator.Display("50%", settings: Decimals).Should().Be("0.5");
        Calculator.Display("4⁻¹", settings: Decimals).Should().Be("0.25");
        Calculator.Error("0⁻¹").Kind.Should().Be(CalcErrorKind.MathError);
        Calculator.Display("2³", settings: Decimals).Should().Be("8");
        Calculator.Display("(-2)³", settings: Decimals).Should().Be("-8");
    }

    [Fact]
    public void ComparingValues_UsesExactnessAndTolerance()
    {
        // Two exact decimals compare exactly; a value from double compares within 10⁻¹³ relative.
        CalculatorSession session = Calculator.Session(settings: settings => settings with { Verify = true });

        session.Calculate("10^30=10^30").IsTrue.Should().BeTrue("two double values, equal");
        session.Calculate("10^30<10^31").IsTrue.Should().BeTrue();
        session.Calculate("10^-20>0").IsTrue.Should().BeTrue();
        session.Calculate("1⌟3=0.3333333333333333333333333333").IsTrue.Should().BeTrue("the decimal quotient is that number");
        session.Calculate("1⌟3>0.333").IsTrue.Should().BeTrue();
    }
}
