// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.RegularExpressions;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// The data is itself a deliverable: these tests keep it well-formed so a conformance run can be trusted.
/// </summary>
public sealed partial class ConformanceDataTests
{
    [Fact]
    public void DataFiles_ArePresent()
    {
        ConformanceCatalog.Files.Should().NotBeEmpty();
        ConformanceCatalog.Cases.Should().HaveCountGreaterThan(100, "the manual has more than a hundred worked examples with usable data");
    }

    [Fact]
    public void CaseIds_AreUniqueAndKebabCase()
    {
        ConformanceCase[] cases = [.. ConformanceCatalog.Cases];

        cases.Select(conformanceCase => conformanceCase.Id).Should().OnlyHaveUniqueItems();
        cases.Should().AllSatisfy(conformanceCase => KebabCase().IsMatch(conformanceCase.Id).Should().BeTrue("'{0}' must be kebab-case", conformanceCase.Id));
    }

    [Fact]
    public void Cases_UseTheClosedVocabulary()
    {
        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases)
        {
            conformanceCase.Page.Should().BeInRange(1, ConformanceVocabulary.LastPage, "{0}", conformanceCase);
            ConformanceVocabulary.Apps.Keys.Should().Contain(conformanceCase.App, "{0}", conformanceCase);
            ConformanceVocabulary.Kinds.Should().Contain(conformanceCase.Kind, "{0}", conformanceCase);
            ConformanceVocabulary.Statuses.Should().Contain(conformanceCase.Status, "{0}", conformanceCase);
            ConformanceVocabulary.ExpectationSources.Should().Contain(conformanceCase.ExpectationSource, "{0}", conformanceCase);
            ConformanceVocabulary.Profiles.Should().Contain(conformanceCase.Profile, "{0}", conformanceCase);

            foreach ((string key, string value) in conformanceCase.Settings)
            {
                ConformanceVocabulary.Settings.Keys.Should().Contain(key, "{0}", conformanceCase);
                ConformanceVocabulary.Settings[key].Should().Contain(value, "setting {0} of {1}", key, conformanceCase);
            }
        }
    }

    [Fact]
    public void ReadyCases_CarryAnExpectation()
    {
        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases.Where(c => c.Status != "needs-oracle"))
        {
            conformanceCase.ExpectationSource.Should().NotBe("pending-oracle", "{0} is not waiting for an oracle", conformanceCase);

            if (conformanceCase.Kind == "sequence")
            {
                conformanceCase.Steps.Should().NotBeNullOrEmpty("{0} is a sequence", conformanceCase);
                conformanceCase.Steps!.Should().AllSatisfy(step => HasContent(step.Expect).Should().BeTrue("every step of {0} needs an expectation", conformanceCase));
            }
            else
            {
                HasContent(conformanceCase.Expect).Should().BeTrue("{0} needs an expectation", conformanceCase);
            }
        }
    }

    [Fact]
    public void IncompleteCases_ExplainThemselves()
    {
        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases.Where(c => c.Status != "ready"))
        {
            conformanceCase.Note.Should().NotBeNullOrWhiteSpace("{0} is {1} and must say why", conformanceCase, conformanceCase.Status);
        }

        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases.Where(c => c.Status == "needs-oracle"))
        {
            conformanceCase.ExpectationSource.Should().Be("pending-oracle", "{0}", conformanceCase);
        }
    }

    [Fact]
    public void ExpressionCases_HaveAnInput()
    {
        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases.Where(c => c.Kind is "expression" or "calc"))
        {
            conformanceCase.Input.Should().NotBeNullOrWhiteSpace("{0}", conformanceCase);
        }
    }

    [Fact]
    public void ExpectedErrors_AreKnownErrorKinds()
    {
        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases)
        {
            IEnumerable<JsonElement?> expectations = [conformanceCase.Expect, .. conformanceCase.Steps?.Select(step => step.Expect) ?? []];
            foreach (JsonElement expect in expectations.Where(HasContent).Select(expect => expect!.Value))
            {
                if (expect.TryGetProperty("error", out JsonElement error))
                {
                    ConformanceVocabulary.ErrorKinds.Should().Contain(error.GetString()!, "{0}", conformanceCase);
                }
            }
        }
    }

    [Fact]
    public void SameDataReferences_PointAtExistingCases()
    {
        HashSet<string> ids = [.. ConformanceCatalog.Cases.Select(c => c.Id)];

        foreach (ConformanceCase conformanceCase in ConformanceCatalog.Cases.Where(c => HasContent(c.Given)))
        {
            if (conformanceCase.Given!.Value.TryGetProperty("sameDataAs", out JsonElement reference))
            {
                ids.Should().Contain(reference.GetString()!, "{0} reuses the data of another case", conformanceCase);
            }
        }
    }

    [Fact]
    public void DomainTable_IsComplete()
    {
        DomainsFile domains = ConformanceCatalog.Domains;

        domains.Domains.Should().HaveCountGreaterThanOrEqualTo(35);
        domains.Domains.Should().AllSatisfy(domain =>
        {
            domain.Function.Should().NotBeNullOrWhiteSpace();
            domain.Domain.Should().NotBeNullOrWhiteSpace();
            domain.Page.Should().BeInRange(169, 171);
        });
    }

    private static bool HasContent(JsonElement? element)
    {
        return element is { ValueKind: JsonValueKind.Object } value && value.EnumerateObject().Any();
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KebabCase();
}
