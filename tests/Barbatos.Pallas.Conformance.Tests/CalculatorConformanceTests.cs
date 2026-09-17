// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Runs every conformance case against the engine.
/// </summary>
/// <remarks>
/// Phase 0: no application engine exists yet, so every case is reported as skipped with the phase that will
/// implement it. A case is never allowed to pass before the code it describes exists.
/// </remarks>
public sealed class CalculatorConformanceTests
{
    public static TheoryData<string> CaseIds => [.. ConformanceCatalog.Cases.Select(conformanceCase => conformanceCase.Id)];

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Case_MatchesTheReferenceCalculator(string id)
    {
        ConformanceCase conformanceCase = ConformanceCatalog.Find(id);

        Assert.Skip($"The {conformanceCase.App} engine is not implemented yet ({ConformanceVocabulary.Apps[conformanceCase.App]}).");
    }
}
