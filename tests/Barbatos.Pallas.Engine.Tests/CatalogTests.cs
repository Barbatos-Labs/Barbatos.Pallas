// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// What the engine knows by name: data sets, their values, and how a published number becomes a <see cref="Value"/>.
/// </summary>
public sealed class CatalogTests
{
    private static CalculatorSession Session(params ScientificConstant[] constants)
    {
        return PallasEngineBuilder.CreateDefault()
            .AddConstantSet(new ConstantSet("test", constants))
            .Build()
            .CreateSession();
    }

    [Theory]
    // A scaled decimal becomes a decimal where every digit survives, and a double where it would not.
    [InlineData(1.5, 0, ValueKind.DecimalReal, true)]
    [InlineData(1.5, 5, ValueKind.DecimalReal, true)]
    [InlineData(1.5, -5, ValueKind.DecimalReal, true)]
    [InlineData(1.5, -14, ValueKind.DecimalReal, true)]
    [InlineData(1.5, -15, ValueKind.DoubleReal, false)]
    [InlineData(6.62607015, -34, ValueKind.DoubleReal, false)]
    [InlineData(1.5, 29, ValueKind.DoubleReal, false)]
    [InlineData(2.99792458, 8, ValueKind.DecimalReal, true)]
    public void ConstantsArriveWithThePrecisionRule(double mantissa, int exponent, ValueKind kind, bool exact)
    {
        CalculatorSession session = Session(new ScientificConstant("@h", new((decimal)mantissa, exponent), default, "x"));

        Value value = session.Calculate("@h").Result;

        value.Kind.Should().Be(kind);
        value.IsExact.Should().Be(exact);
    }

    [Fact]
    public void AConstantKeepsItsPublishedDigits()
    {
        CalculatorSession session = Session(new ScientificConstant("@h", new(6.62607015m, -34), default, "J Hz^-1"));

        session.Calculate("@h").Result.ToDouble().Should().Be(6.62607015e-34);
        session.Calculate("@h×10^34").Display.Text.Should().Be("6.62607015");
    }

    [Fact]
    public void ALaterSetReplacesTheConstantsOfAnEarlierOne()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault()
            .AddConstantSet(new ConstantSet("first", [new ScientificConstant("@h", new(1m, 0), default, "x")]))
            .AddConstantSet(new ConstantSet("second", [new ScientificConstant("@h", new(2m, 0), default, "x")]))
            .Build();

        engine.CreateSession().Calculate("@h").Display.Text.Should().Be("2");
    }

    [Fact]
    public void ASetRefusesTwoConstantsWithTheSameSymbol()
    {
        Action act = () => _ = new ConstantSet("twice",
        [
            new ScientificConstant("@h", new(1m, 0), default, "x"),
            new ScientificConstant("@h", new(2m, 0), default, "x"),
        ]).Constants;

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ASetValidatesItsNameAndContents()
    {
        Action noName = () => _ = new ConstantSet(" ", []).Name;
        Action noConstants = () => _ = new ConstantSet("x", null!).Name;
        Action badSymbol = () => _ = new ScientificConstant("h", new(1m, 0), default, "x").Symbol;
        Action noUnit = () => _ = new ScientificConstant("@h", new(1m, 0), default, null!).Symbol;

        noName.Should().Throw<ArgumentException>();
        noConstants.Should().Throw<ArgumentNullException>();
        badSymbol.Should().Throw<ArgumentException>();
        noUnit.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AConstantKnowsWhetherItIsExactByDefinition()
    {
        ScientificConstant defined = new("@c", new(299792458m, 0), default, "m s^-1");
        ScientificConstant measured = new("@G", new(6.67430m, -11), new(0.00015m, -11), "m^3 kg^-1 s^-2");

        defined.IsExact.Should().BeTrue();
        defined.Value.Should().Be(new ScaledDecimal(299792458m, 0));
        measured.IsExact.Should().BeFalse();
        measured.StandardUncertainty.Exponent.Should().Be(-11);
        measured.Unit.Should().Be("m^3 kg^-1 s^-2");
    }

    [Fact]
    public void AScaledDecimalIsANumberWithAnExponent()
    {
        ScaledDecimal fromDecimal = ScaledDecimal.FromDecimal(2.5m);
        ScaledDecimal implicitly = 2.5m;

        fromDecimal.Should().Be(implicitly);
        fromDecimal.Mantissa.Should().Be(2.5m);
        fromDecimal.Exponent.Should().Be(0);
    }

    [Fact]
    public void UnitSetsAndAtomicWeightsValidateTheirContents()
    {
        Action units = () => _ = new UnitSet("x", null!).Name;
        Action weights = () => _ = new AtomicWeightTable("x", null!).Name;
        Action twice = () => _ = new UnitSet("twice", [new UnitConversion("in▶cm", 1m), new UnitConversion("in▶cm", 2m)]).Conversions;
        Action sameElement = () => _ = new AtomicWeightTable("twice",
        [
            new AtomicWeight(1, "H", 1m, false),
            new AtomicWeight(1, "H", 2m, false),
        ]).Elements;

        units.Should().Throw<ArgumentNullException>();
        weights.Should().Throw<ArgumentNullException>();
        twice.Should().Throw<ArgumentException>();
        sameElement.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AUnitConversionAppliesItsOffsetsAndDivisor()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault()
            .AddUnitSet(new UnitSet("test", [new UnitConversion("in▶cm", 3m, 2m, offsetBefore: 1m, offsetAfter: 10m)]))
            .Build();
        CalculatorSession session = engine.CreateSession();
        session.Settings = session.Settings with { InputOutput = InputOutput.LineILineO };

        // (5 + 1) × 3 ÷ 2 + 10.
        session.Calculate("5in▶cm").Display.Text.Should().Be("19");
    }

    [Fact]
    public void AtomicWeightsComeFromTheirTable()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault()
            .AddAtomicWeights(new AtomicWeightTable("test", [new AtomicWeight(1, "H", 1.008m, false), new AtomicWeight(43, "Tc", 97m, true)]))
            .Build();
        CalculatorSession session = engine.CreateSession();

        session.Settings = session.Settings with { InputOutput = InputOutput.MathIDecimalO };
        session.Calculate("AtWt(1)").Display.Text.Should().Be("1.008");
        session.Calculate("AtWt(43)").Display.Text.Should().Be("97");
        session.Calculate("AtWt(2)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("AtWt(1.5)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheVocabularyGrowsWithThePluginsOnly()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();

        engine.Vocabulary.Should().BeSameAs(SyntaxVocabulary.Standard, "an engine without plugins uses the calculator's vocabulary");
    }
}
