// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Data.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheDataSetsInUse()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault()
            .AddConstantSet(ConstantSets.Codata2022)
            .AddUnitSet(UnitSets.NistSp811)
            .AddAtomicWeights(AtomicWeightTables.Ciaaw)
            .Build();

        CalculatorSession session = engine.CreateSession();

        session.Calculate("@N_A×@k").Display.Text.Should().Be("8.314462618", "the molar gas constant");
        session.Calculate("AtWt(21)").Display.Text.Should().Be("44.955907", "scandium");
        session.Calculate("5cm▶in").Result.ToDecimal().Should().Be(5m / 2.54m, "the inch is exactly 2.54 cm");
    }
}
