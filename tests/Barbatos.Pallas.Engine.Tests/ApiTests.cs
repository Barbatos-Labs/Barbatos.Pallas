// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The small public surface around calculations: results, settings and the engine itself.
/// </summary>
public sealed class ApiTests
{
    [Fact]
    public void EvalResult_CarriesAValueOrAnError()
    {
        EvalResult value = EvalResult.FromValue(Value.One);
        EvalResult implicitValue = Value.One;
        EvalResult error = EvalResult.Failure(CalcErrorKind.MathError);

        value.Succeeded.Should().BeTrue();
        value.Value.Should().Be(Value.One);
        value.Error.Should().BeNull();
        implicitValue.Should().Be(value);
        error.Succeeded.Should().BeFalse();
        error.Error.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData(NumberFormatKind.Norm, 1, "Norm1")]
    [InlineData(NumberFormatKind.Norm, 2, "Norm2")]
    [InlineData(NumberFormatKind.Fix, 3, "Fix3")]
    [InlineData(NumberFormatKind.Sci, 5, "Sci5")]
    public void NumberFormat_PrintsAsTheSettingIsNamed(NumberFormatKind kind, int digits, string expected)
    {
        NumberFormat format = kind switch
        {
            NumberFormatKind.Fix => NumberFormat.Fix(digits),
            NumberFormatKind.Sci => NumberFormat.Sci(digits),
            _ => digits == 1 ? NumberFormat.Norm1 : NumberFormat.Norm2,
        };

        format.Kind.Should().Be(kind);
        format.Digits.Should().Be(digits);
        format.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void FixOutsideItsRange_IsRejected(int decimals)
    {
        Action act = () => NumberFormat.Fix(decimals);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void SciOutsideItsRange_IsRejected(int digits)
    {
        Action act = () => NumberFormat.Sci(digits);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TheEngineExposesItsVocabularyAndBudget()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().WithBudget(new EngineBudget(5, TimeSpan.FromSeconds(1))).Build();

        engine.Vocabulary.Symbols.Should().NotBeEmpty();
        engine.Budget.MaxIterations.Should().Be(5);
        engine.CreateSession().Budget.Should().BeSameAs(engine.Budget, "a session starts with the engine's budget");
    }

    [Fact]
    public void FormattingAValueTakesTheSettingsItIsGiven()
    {
        Value value = Value.FromDecimal(1234.5m);

        PallasEngine.Format(value, CalculatorSettings.Initial with { NumberFormat = NumberFormat.Fix(1), InputOutput = InputOutput.MathIDecimalO })!
            .Text.Should().Be("1234.5");
        PallasEngine.Format(Value.FromBaseN(255), CalculatorSettings.Initial with { BaseMode = NumberBase.Hex })!.Text.Should().Be("FF");
        PallasEngine.Format(Value.FromBaseN(255), CalculatorSettings.Initial, target: FormatTarget.Sexagesimal).Should().BeNull();
    }

    [Fact]
    public void FormattingRejectsNulls()
    {
        Action settings = () => PallasEngine.Format(Value.One, null!);
        Action calculation = () => Calculator.Session().Format(null!);

        settings.Should().Throw<ArgumentNullException>();
        calculation.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ACalculationRemembersHowItWasMade()
    {
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended);
        session.Settings = session.Settings with { AngleUnit = Numerics.AngleUnit.Gradian };

        Calculation calculation = session.Calculate("1+1");

        calculation.Input.Should().Be("1+1");
        calculation.App.Should().Be(CalculatorApp.Calculate);
        calculation.Profile.Should().Be(CalculatorProfile.Extended);
        calculation.Settings.AngleUnit.Should().Be(Numerics.AngleUnit.Gradian);
        calculation.Kind.Should().Be(CalculationKind.Value);
        calculation.Second.Should().BeNull();
        calculation.IsTrue.Should().BeNull();
        calculation.Integrals.Should().BeEmpty();
        calculation.ToString().Should().Be("1+1 = 2");
    }

    [Fact]
    public void RoundingAValueBeyondDecimal_UsesSignificantDigits()
    {
        // Rnd on a double value: Fix has no room, Sci and Norm still round the mantissa.
        CalculatorSession session = Calculator.Session(profile: CalculatorProfile.Extended);
        session.Settings = session.Settings with { InputOutput = InputOutput.MathIDecimalO, NumberFormat = NumberFormat.Sci(3) };

        session.Calculate("Rnd(1.23456×10^30)").Result.ToDouble().Should().Be(1.23e30);

        // Fix has no decimals to round at that size, so the value comes back unchanged.
        session.Settings = session.Settings with { NumberFormat = NumberFormat.Fix(2) };
        double unrounded = session.Calculate("1.23456×10^30").Result.ToDouble();
        session.Calculate("Rnd(1.23456×10^30)").Result.ToDouble().Should().Be(unrounded);
    }

    [Fact]
    public void DividingByASumOfARationalAndARoot_KeepsTheExactForm()
    {
        // (1 + √2)⁻¹ = √2 − 1: the conjugate keeps the form, and the display shows it.
        Calculator.Display("1÷(1+√(2))").Should().Be("-1+√(2)");
        Calculator.Display("1÷√(2)").Should().Be("√(2)⌟2");
        Calculator.Display("(1+√(2))×(1-√(2))").Should().Be("-1");
    }

    [Fact]
    public void FormsAreDroppedWhereTheyCannotBeRepresented()
    {
        // π² and a product of three different roots have no display form; the decimal remains correct.
        Calculator.Display("π²").Should().Be("9.869604401");
        Calculator.Display("√(2)×√(3)×√(5)").Should().Be("√(30)");
        Calculator.Display("√(2)+√(3)+√(5)").Should().Be("5.382332347");
    }

    [Fact]
    public void SettingsAreAValueObject()
    {
        CalculatorSettings initial = CalculatorSettings.Initial;
        CalculatorSettings changed = initial with { DigitSeparator = true };

        initial.Should().NotBe(changed);
        initial.Should().Be(CalculatorSettings.Initial with { });
        initial.InputOutput.Should().Be(InputOutput.MathIMathO);
        initial.NumberFormat.Should().Be(NumberFormat.Norm1);
        initial.BaseMode.Should().Be(NumberBase.Dec);
        initial.Verify.Should().BeFalse();
        initial.EngineerSymbol.Should().BeFalse();
        initial.FractionResult.Should().Be(FractionResult.Improper);
        initial.ComplexResult.Should().Be(ComplexResult.Rectangular);
        initial.DecimalMark.Should().Be(DecimalMark.Dot);
    }

    [Fact]
    public void TheDefaultBudgetIsGenerousButFinite()
    {
        EngineBudget.Default.MaxIterations.Should().Be(100_000_000);
        EngineBudget.Default.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
