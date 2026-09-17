// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text.Json;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Every input of the conformance data, read by the Phase 2 parser in its case's application and settings.
/// </summary>
/// <remarks>
/// This checks syntax only. A case is still reported as skipped by <see cref="CalculatorConformanceTests"/> until the
/// engine that computes its result exists; parsing an expression is not conforming to the calculator.
/// </remarks>
public sealed class ConformanceSyntaxTests
{
    public static TheoryData<string> CasesWithInputs =>
    [
        .. ConformanceCatalog.Cases.Where(conformanceCase => Inputs(conformanceCase).Any()).Select(conformanceCase => conformanceCase.Id),
    ];

    [Theory]
    [MemberData(nameof(CasesWithInputs))]
    public void Inputs_ParseInTheirApplication(string id)
    {
        ConformanceCase conformanceCase = ConformanceCatalog.Find(id);
        SyntaxContext context = ContextOf(conformanceCase);
        bool expectsSyntaxError = ExpectedError(conformanceCase) == "SyntaxError";

        foreach (string input in Inputs(conformanceCase))
        {
            ParseResult result = ExpressionParser.Parse(input, context);
            if (expectsSyntaxError)
            {
                result.Succeeded.Should().BeFalse("{0}: '{1}' is a Syntax ERROR on the calculator", conformanceCase, input);
                result.Diagnostic!.Value.Code.Should().NotBe(SyntaxErrorCode.NestingTooDeep, "{0}", conformanceCase);
            }
            else
            {
                result.Succeeded.Should().BeTrue("{0}: '{1}' should parse, but failed with {2}", conformanceCase, input, result.Diagnostic);
                ExpressionParser.Parse(LinearPrinter.Print(result.Root!, context), context).Root.Should()
                    .Match<SyntaxNode>(reparsed => SyntaxEquivalence.AreEquivalent(result.Root!, reparsed), "{0}: printing must round-trip", conformanceCase);
            }
        }
    }

    [Theory]
    [InlineData("basic-implicit-paren-001")]
    [InlineData("basic-implicit-paren-002")]
    public void EquivalentTo_ParsesToTheSameTree(string id)
    {
        // Manual p. 29: an omitted multiplication sign is parenthesized automatically.
        ConformanceCase conformanceCase = ConformanceCatalog.Find(id);
        SyntaxContext context = ContextOf(conformanceCase);
        string equivalent = conformanceCase.Expect!.Value.GetProperty("equivalentTo").GetString()!;

        SyntaxNode input = ExpressionParser.Parse(conformanceCase.Input!, context).Root!;
        SyntaxNode expected = ExpressionParser.Parse(equivalent, context).Root!;

        SyntaxEquivalence.AreEquivalent(input, expected).Should().BeTrue("{0}: '{1}' means '{2}'", conformanceCase, conformanceCase.Input, equivalent);
    }

    [Fact]
    public void EveryEquivalentToCase_IsCovered()
    {
        string[] withEquivalent =
        [
            .. ConformanceCatalog.Cases
                .Where(conformanceCase => conformanceCase.Expect is { ValueKind: JsonValueKind.Object } expect && expect.TryGetProperty("equivalentTo", out _))
                .Select(conformanceCase => conformanceCase.Id),
        ];

        withEquivalent.Should().BeEquivalentTo("basic-implicit-paren-001", "basic-implicit-paren-002");
    }

    private static SyntaxContext ContextOf(ConformanceCase conformanceCase)
    {
        return new SyntaxContext(
            Enum.Parse<CalculatorApp>(conformanceCase.App),
            AllowRelations: conformanceCase.Settings.GetValueOrDefault("verify") == "On");
    }

    private static string? ExpectedError(ConformanceCase conformanceCase)
    {
        return conformanceCase.Expect is { ValueKind: JsonValueKind.Object } expect && expect.TryGetProperty("error", out JsonElement error)
            ? error.GetString()
            : null;
    }

    private static IEnumerable<string> Inputs(ConformanceCase conformanceCase)
    {
        if (conformanceCase.Input is not null)
        {
            yield return conformanceCase.Input;
        }

        foreach (ConformanceStep step in conformanceCase.Steps ?? [])
        {
            if (step.Input is not null)
            {
                yield return step.Input;
            }
        }

        if (conformanceCase.Given is { ValueKind: JsonValueKind.Object } given && given.TryGetProperty("inputs", out JsonElement inputs))
        {
            foreach (JsonElement input in inputs.EnumerateArray())
            {
                yield return input.GetString()!;
            }
        }
    }
}
