// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Data.Tests;

/// <summary>
/// The CIAAW atomic weights behind <c>AtWt(</c> (manual p. 67).
/// </summary>
public sealed class AtomicWeightTests
{
    private static readonly AtomicWeightTable Table = AtomicWeightTables.Ciaaw;

    [Fact]
    public void TheTableHasEveryElement()
    {
        Table.Elements.Should().HaveCount(118);
        Table.Elements.Select(element => element.AtomicNumber).Should().Equal(Enumerable.Range(1, 118));
        Table.Elements.Select(element => element.Symbol).Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [InlineData(1, "H", 1.0080)]
    [InlineData(2, "He", 4.002602)]
    [InlineData(6, "C", 12.011)]
    [InlineData(8, "O", 15.999)]
    [InlineData(21, "Sc", 44.955907)]
    [InlineData(26, "Fe", 55.845)]
    [InlineData(79, "Au", 196.966570)]
    [InlineData(92, "U", 238.02891)]
    public void KnownWeights_MatchTheCiaawValues(int atomicNumber, string symbol, double weight)
    {
        AtomicWeight element = Table.Elements[atomicNumber - 1];

        element.Symbol.Should().Be(symbol);
        element.Weight.Should().Be((decimal)weight);
        element.IsMassNumber.Should().BeFalse();
    }

    [Theory]
    // Elements without a standard atomic weight carry the mass number of their longest-lived isotope.
    [InlineData(43, "Tc", 97)]
    [InlineData(61, "Pm", 145)]
    [InlineData(94, "Pu", 244)]
    [InlineData(118, "Og", 294)]
    public void RadioactiveElements_CarryAMassNumber(int atomicNumber, string symbol, int massNumber)
    {
        AtomicWeight element = Table.Elements[atomicNumber - 1];

        element.Symbol.Should().Be(symbol);
        element.Weight.Should().Be(massNumber);
        element.IsMassNumber.Should().BeTrue();
    }

    [Fact]
    public void WeightsGrowWithTheAtomicNumber_ExceptWhereNatureSaysOtherwise()
    {
        // Among the elements that have a standard atomic weight, the four inversions are the ones chemistry knows:
        // Ar/K, Co/Ni, Te/I and Th/Pa. Mass numbers of radioactive elements follow their own order and are left out.
        int[] inversions =
        [
            .. Table.Elements.Zip(Table.Elements.Skip(1))
                .Where(pair => !pair.First.IsMassNumber && !pair.Second.IsMassNumber && pair.Second.Weight < pair.First.Weight)
                .Select(pair => pair.First.AtomicNumber),
        ];

        inversions.Should().Equal(18, 27, 52, 90);
    }

    [Fact]
    public void TheEngineReturnsTheWeightOfAnElement()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddAtomicWeights(Table).Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("AtWt(21)").Display.Text.Should().Be("44.955907");
        session.Calculate("AtWt(0)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        session.Calculate("AtWt(119)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void WithoutATable_AtomicWeightsAreNotDefined()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();

        engine.CreateSession().Calculate("AtWt(21)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }
}
