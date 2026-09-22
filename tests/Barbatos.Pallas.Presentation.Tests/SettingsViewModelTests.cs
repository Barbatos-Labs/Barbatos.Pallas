// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The Calc Settings screen (manual pp. 22-25): what it shows is the session's settings, and what it writes is what
/// the next calculation uses.
/// </summary>
public sealed class SettingsViewModelTests
{
    [Fact]
    public void TheScreenStartsAtTheSettingsTheCalculatorStartsWith()
    {
        SettingsViewModel settings = new(Shell.Session());

        settings.InputOutput.Should().Be(InputOutput.MathIMathO);
        settings.AngleUnit.Should().Be(AngleUnit.Degree);
        settings.NumberFormat.Should().Be(NumberFormat.Norm1);
        settings.EngineerSymbol.Should().BeFalse();
        settings.FractionResult.Should().Be(FractionResult.Improper);
        settings.ComplexResult.Should().Be(ComplexResult.Rectangular);
        settings.DecimalMark.Should().Be(DecimalMark.Dot);
        settings.DigitSeparator.Should().BeFalse();
        settings.Verify.Should().BeFalse();
        settings.ComplexRoots.Should().BeTrue();
        settings.BaseMode.Should().Be(NumberBase.Dec);
    }

    [Fact]
    public void ASettingChangesWhatTheNextCalculationDoes()
    {
        CalculatorSession session = Shell.Session();
        SettingsViewModel settings = new(session);

        settings.AngleUnit = AngleUnit.Radian;

        session.Settings.AngleUnit.Should().Be(AngleUnit.Radian, "the screen writes the session, not a copy of it");
        session.Calculate("sin(π÷2)").Display.Text.Should().Be("1");
    }

    [Fact]
    public void EverySettingIsReadBackAsItWasWritten()
    {
        CalculatorSession session = Shell.Session();
        SettingsViewModel settings = new(session);

        settings.InputOutput = InputOutput.LineILineO;
        settings.AngleUnit = AngleUnit.Gradian;
        settings.NumberFormat = NumberFormat.Sci(4);
        settings.EngineerSymbol = true;
        settings.FractionResult = FractionResult.Mixed;
        settings.ComplexResult = ComplexResult.Polar;
        settings.DecimalMark = DecimalMark.Comma;
        settings.DigitSeparator = true;
        settings.Verify = true;
        settings.ComplexRoots = false;

        settings.InputOutput.Should().Be(InputOutput.LineILineO);
        settings.AngleUnit.Should().Be(AngleUnit.Gradian);
        settings.NumberFormat.Should().Be(NumberFormat.Sci(4));
        settings.EngineerSymbol.Should().BeTrue();
        settings.FractionResult.Should().Be(FractionResult.Mixed);
        settings.ComplexResult.Should().Be(ComplexResult.Polar);
        settings.DecimalMark.Should().Be(DecimalMark.Comma);
        settings.DigitSeparator.Should().BeTrue();
        settings.Verify.Should().BeTrue();
        settings.ComplexRoots.Should().BeFalse();
        session.Settings.Should().Be(CalculatorSettings.Initial with
        {
            InputOutput = InputOutput.LineILineO,
            AngleUnit = AngleUnit.Gradian,
            NumberFormat = NumberFormat.Sci(4),
            EngineerSymbol = true,
            FractionResult = FractionResult.Mixed,
            ComplexResult = ComplexResult.Polar,
            DecimalMark = DecimalMark.Comma,
            DigitSeparator = true,
            Verify = true,
            ComplexRoots = false,
        });
    }

    [Fact]
    public void ASettingTellsTheScreenThatEverythingMayHaveChanged()
    {
        SettingsViewModel settings = new(Shell.Session());
        List<string?> changed = settings.Changes();

        settings.DigitSeparator = true;

        changed.Should().Equal([string.Empty], "one setting is one record, so the screen re-reads all of it");
    }

    [Fact]
    public void TheBaseModeIsShownButNotChangedHere()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.BaseN);
        SettingsViewModel settings = new(session);

        session.Calculate("b1111");

        settings.BaseMode.Should().Be(session.Settings.BaseMode, "the Base-N application owns the setting (p. 129)");
    }

    [Theory]
    [InlineData(NumberFormatKind.Norm, 1)]
    [InlineData(NumberFormatKind.Norm, 2)]
    [InlineData(NumberFormatKind.Fix, 0)]
    [InlineData(NumberFormatKind.Fix, 9)]
    [InlineData(NumberFormatKind.Sci, 1)]
    [InlineData(NumberFormatKind.Sci, 10)]
    public void ANumberFormatInRangeIsSet(NumberFormatKind kind, int digits)
    {
        SettingsViewModel settings = new(Shell.Session());

        settings.TrySetNumberFormat(kind, digits).Should().BeTrue();

        settings.NumberFormat.Kind.Should().Be(kind);
        settings.NumberFormat.Digits.Should().Be(digits);
    }

    [Theory]
    [InlineData(NumberFormatKind.Norm, 0)]
    [InlineData(NumberFormatKind.Norm, 3)]
    [InlineData(NumberFormatKind.Fix, -1)]
    [InlineData(NumberFormatKind.Fix, 10)]
    [InlineData(NumberFormatKind.Sci, 0)]
    [InlineData(NumberFormatKind.Sci, 11)]
    [InlineData((NumberFormatKind)7, 1)]
    public void ANumberFormatOutOfRangeLeavesTheSettingAsItWas(NumberFormatKind kind, int digits)
    {
        SettingsViewModel settings = new(Shell.Session());
        settings.NumberFormat = NumberFormat.Fix(2);

        settings.TrySetNumberFormat(kind, digits).Should().BeFalse("input does not throw");

        settings.NumberFormat.Should().Be(NumberFormat.Fix(2));
    }

    [Fact]
    public void TheNumberFormatOfTheScreenIsTheOneTheResultIsShownIn()
    {
        CalculatorSession session = Shell.Session();
        SettingsViewModel settings = new(session);

        settings.InputOutput = InputOutput.LineILineO;
        settings.TrySetNumberFormat(NumberFormatKind.Fix, 3);

        session.Calculate("1÷3").Display.Text.Should().Be("0.333");
    }

    [Fact]
    public void ResetPutsEverySettingBack()
    {
        CalculatorSession session = Shell.Session();
        SettingsViewModel settings = new(session);
        settings.AngleUnit = AngleUnit.Radian;
        settings.NumberFormat = NumberFormat.Fix(2);
        settings.DecimalMark = DecimalMark.Comma;
        List<string?> changed = settings.Changes();

        settings.ResetCommand.Execute(null);

        session.Settings.Should().Be(CalculatorSettings.Initial);
        changed.Should().Equal([string.Empty]);
    }

    [Fact]
    public void ResetLeavesTheMemoriesAlone()
    {
        // Reset: Setup Data (p. 33) is the settings; Reset: Memory is a different command.
        CalculatorSession session = Shell.Session();
        session.SetVariable(MemoryVariable.A, Value.FromDecimal(42));
        SettingsViewModel settings = new(session);

        settings.Reset();

        session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(42m);
    }

    [Fact]
    public void TheOptionsAreEveryValueOfTheirSetting()
    {
        SettingsViewModel.InputOutputOptions.Should().BeEquivalentTo(Enum.GetValues<InputOutput>());
        SettingsViewModel.AngleUnitOptions.Should().BeEquivalentTo(Enum.GetValues<AngleUnit>());
        SettingsViewModel.FractionResultOptions.Should().BeEquivalentTo(Enum.GetValues<FractionResult>());
        SettingsViewModel.ComplexResultOptions.Should().BeEquivalentTo(Enum.GetValues<ComplexResult>());
        SettingsViewModel.DecimalMarkOptions.Should().BeEquivalentTo(Enum.GetValues<DecimalMark>());
    }

    [Fact]
    public void AScreenWithoutASessionIsRefused()
    {
        Action act = () => _ = new SettingsViewModel(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
