// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Runs every conformance case against the engine.
/// </summary>
/// <remarks>
/// A case runs once the engine of its application exists (Calculate, Complex and Base-N from Phase 3, Matrix, Vector,
/// Statistics, Distribution, Equation, Inequality and Ratio from Phase 4); the others are
/// reported as skipped with the phase that implements them, never as passed. A case waiting for an oracle is skipped
/// too, because it has nothing to check.
/// </remarks>
public sealed class CalculatorConformanceTests
{
    private static readonly HashSet<string> ImplementedApps =
        ["Calculate", "Complex", "BaseN", "Matrix", "Vector", "Statistics", "Distribution", "Equation", "Inequality", "Ratio", "Spreadsheet", "Table", "MathBox"];

    public static TheoryData<string> CaseIds => [.. ConformanceCatalog.Cases.Select(conformanceCase => conformanceCase.Id)];

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Case_MatchesTheReferenceCalculator(string id)
    {
        ConformanceCase conformanceCase = ConformanceCatalog.Find(id);
        if (!ImplementedApps.Contains(conformanceCase.App))
        {
            Assert.Skip($"The {conformanceCase.App} engine is not implemented yet ({ConformanceVocabulary.Apps[conformanceCase.App]}).");
        }

        if (conformanceCase.Status == "needs-oracle")
        {
            Assert.Skip($"{conformanceCase} waits for its expected value: {conformanceCase.Note}");
        }

        ConformanceRunner.Run(conformanceCase);
    }
}
