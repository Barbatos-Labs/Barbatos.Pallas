// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Data.Tests;

/// <summary>
/// The CODATA 2022 constants: the defining values of the 2019 SI, and the relations between the derived ones.
/// </summary>
public sealed class ConstantSetTests
{
    private static readonly ConstantSet Codata = ConstantSets.Codata2022;

    [Fact]
    public void TheSetCoversTheCalculatorsCatalogExactly()
    {
        // Every @ name of the vocabulary must have a value, and the set must hold nothing the calculator does not.
        string[] vocabulary =
        [
            .. SyntaxVocabulary.Standard.Symbols
                .Where(symbol => symbol.Kind == SymbolKind.ScientificConstant && ReferenceEquals(symbol, symbol.Canonical))
                .Select(symbol => symbol.Text)
                .Order(StringComparer.Ordinal),
        ];

        Codata.Constants.Select(constant => constant.Symbol).Order(StringComparer.Ordinal).Should().Equal(vocabulary);
        Codata.Constants.Should().HaveCount(47);
    }

    [Theory]
    // The constants the 2019 SI defines exactly (CODATA lists their uncertainty as zero).
    [InlineData("@c", 299792458, 0)]
    [InlineData("@h", 6.62607015, -34)]
    [InlineData("@e", 1.602176634, -19)]
    [InlineData("@k", 1.380649, -23)]
    [InlineData("@N_A", 6.02214076, 23)]
    [InlineData("@g_n", 9.80665, 0)]
    [InlineData("@atm", 101325, 0)]
    [InlineData("@t", 273.15, 0)]
    public void DefiningConstants_CarryTheirExactValues(string symbol, double mantissa, int exponent)
    {
        ScientificConstant constant = Find(symbol);

        constant.Value.Mantissa.Should().Be((decimal)mantissa);
        constant.Value.Exponent.Should().Be(exponent);
        constant.IsExact.Should().BeTrue();
    }

    [Theory]
    // Relations that define a constant from others; each must hold within the uncertainty the data carries.
    [InlineData("@ħ", "@h/(2π)")]
    [InlineData("@R", "@N_A×@k")]
    [InlineData("@F", "@N_A×@e")]
    [InlineData("@R_K", "@h/@e²")]
    [InlineData("@K_J", "2@e/@h")]
    [InlineData("@G_0", "2@e²/@h")]
    [InlineData("@Z_0", "@μ_0×@c")]
    [InlineData("@μ_B", "@e×@ħ/(2@m_e)")]
    [InlineData("@μ_N", "@e×@ħ/(2@m_p)")]
    [InlineData("@a_0", "@ħ/(@m_e×@c×@α)")]
    [InlineData("@r_e", "@α²×@a_0")]
    [InlineData("@λ_C", "@h/(@m_e×@c)")]
    [InlineData("@λ_Cp", "@h/(@m_p×@c)")]
    [InlineData("@λ_Cn", "@h/(@m_n×@c)")]
    [InlineData("@V_m", "@R×273.15/101325")]
    [InlineData("@c_1", "2π@h×@c²")]
    [InlineData("@c_2", "@h×@c/@k")]
    [InlineData("@ε_0", "1/(@μ_0×@c²)")]
    public void DerivedConstants_AgreeWithTheirDefinitions(string symbol, string relation)
    {
        double expected = Relation(relation);
        double actual = Number(Find(symbol));

        actual.Should().BeApproximately(expected, Math.Abs(expected) * 1e-8, "{0} = {1}", symbol, relation);
    }

    [Fact]
    public void ConventionalConstants_AreTheirDefinedValues()
    {
        // The 1990 conventional values, still exact by definition (pp. 64-65 list them under Adopted Values).
        Number(Find("@R_K-90")).Should().Be(25812.807);
        Number(Find("@K_J-90")).Should().Be(483597.9e9);
        Find("@R_K-90").IsExact.Should().BeTrue();
    }

    [Fact]
    public void MeasuredConstants_CarryTheirUncertainty()
    {
        ScientificConstant gravitation = Find("@G");

        gravitation.IsExact.Should().BeFalse();
        gravitation.StandardUncertainty.Mantissa.Should().Be(0.00015m);
        gravitation.StandardUncertainty.Exponent.Should().Be(-11);
        gravitation.Unit.Should().Be("m^3 kg^-1 s^-2");
    }

    [Fact]
    public void MagneticMoments_AreNegative()
    {
        // The electron, neutron and muon moments are antiparallel to their spin.
        Number(Find("@μ_e")).Should().BeNegative();
        Number(Find("@μ_n")).Should().BeNegative();
        Number(Find("@μ_μ")).Should().BeNegative();
        Number(Find("@μ_p")).Should().BePositive();
    }

    [Fact]
    public void EveryConstantHasAUnitUnlessItIsDimensionless()
    {
        foreach (ScientificConstant constant in Codata.Constants)
        {
            if (constant.Symbol is "@α")
            {
                constant.Unit.Should().BeEmpty("the fine-structure constant is dimensionless");
            }
            else
            {
                constant.Unit.Should().NotBeEmpty("{0} is a physical quantity", constant.Symbol);
            }
        }
    }

    [Fact]
    public void TheEngineReadsTheValuesWithThePrecisionRule()
    {
        // 6.62607015×10⁻³⁴ is 0 in decimal, so it must arrive as a double; the speed of light fits decimal exactly.
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddConstantSet(Codata).Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("@c").Result.Kind.Should().Be(ValueKind.DecimalReal);
        session.Calculate("@c").Result.ToDecimal().Should().Be(299792458m);
        session.Calculate("@h").Result.Kind.Should().Be(ValueKind.DoubleReal);
        session.Calculate("@h").Result.ToDouble().Should().Be(6.62607015e-34);
        session.Calculate("@N_A×@k").Display.Text.Should().Be("8.314462618");
    }

    private static ScientificConstant Find(string symbol) => Codata.Constants.Single(constant => constant.Symbol == symbol);

    private static double Number(ScientificConstant constant) => (double)constant.Value.Mantissa * Math.Pow(10d, constant.Value.Exponent);

    /// <summary>Evaluates a relation between constants with the engine itself.</summary>
    private static double Relation(string relation)
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddConstantSet(Codata).Build();
        Calculation calculation = engine.CreateSession(profile: CalculatorProfile.Extended).Calculate(relation);

        calculation.Error.Should().BeNull("'{0}' should compute", relation);
        return calculation.Result.ToDouble();
    }
}
