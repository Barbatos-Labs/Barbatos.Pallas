// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Data;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>Sessions as the application has them: an engine with the reference data, in the application measured.</summary>
internal static class Sessions
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault()
        .AddConstantSet(ConstantSets.Codata2022)
        .AddUnitSet(UnitSets.NistSp811)
        .AddAtomicWeights(AtomicWeightTables.Ciaaw)
        .Build();

    /// <summary>Creates a session in an application, with the settings the calculator starts with.</summary>
    public static CalculatorSession In(CalculatorApp app) => Engine.CreateSession(app, randomSeed: 880);

    /// <summary>Calculates, and fails the benchmark's setup if the input was not what it meant to measure.</summary>
    public static Calculation Checked(this CalculatorSession session, string input)
    {
        Calculation calculation = session.Calculate(input);
        return calculation.Succeeded
            ? calculation
            : throw new InvalidOperationException($"'{input}' is a {calculation.Error!.Value.Kind} in {session.App}: a benchmark measures a result.");
    }

    /// <summary>A decimal as a value.</summary>
    public static Value N(decimal number) => Value.FromDecimal(number);
}
