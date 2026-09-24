// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Graphing;
using Barbatos.Pallas.Numerics;
using Barbatos.Pallas.Presentation;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// Every key the screens ask for has words, in both languages.
/// </summary>
/// <remarks>
/// A key without a translation shows up as the key itself - <c>error.MathError</c> on the screen - and nothing else
/// says so. The keys are what the presentation layer names, so this reads them from there rather than repeating a
/// list.
/// </remarks>
public sealed class LocalizationTests
{
    public static TheoryData<string> Languages() => ["en-US", "vi-VN"];

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryApplicationHasAName(string language)
    {
        Dictionary<string, string> text = Read(language);

        foreach (CalculatorAppInfo app in CalculatorApps.All)
        {
            text.Should().ContainKey(app.NameKey);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryErrorOfTheEngineHasWords(string language)
    {
        Dictionary<string, string> text = Read(language);

        foreach (CalcErrorKind kind in Enum.GetValues<CalcErrorKind>())
        {
            text.Should().ContainKey("error." + kind);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryValueOfASettingHasWords(string language)
    {
        Dictionary<string, string> text = Read(language);
        IEnumerable<string> values =
        [
            .. Enum.GetValues<InputOutput>().Select(value => value.ToString()),
            .. Enum.GetValues<AngleUnit>().Select(value => value.ToString()),
            .. Enum.GetValues<FractionResult>().Select(value => value.ToString()),
            .. Enum.GetValues<ComplexResult>().Select(value => value.ToString()),
            .. Enum.GetValues<DecimalMark>().Select(value => value.ToString()),
        ];

        foreach (string value in values)
        {
            text.Should().ContainKey("setting:" + value, "a setting shows its values by name");
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryChoiceAScreenOffersHasWords(string language)
    {
        Dictionary<string, string> text = Read(language);
        IEnumerable<string> choices =
        [
            .. Enum.GetValues<MatrixVariable>().Select(value => value.ToString()),
            .. Enum.GetValues<VectorVariable>().Select(value => value.ToString()),
            .. Enum.GetValues<NumberBase>().Select(value => value.ToString()),
            .. Enum.GetValues<EquationKind>().Select(value => value.ToString()),
            .. Enum.GetValues<RelationOperator>().Select(value => value.ToString()),
            .. Enum.GetValues<RatioForm>().Select(value => value.ToString()),
            .. Enum.GetValues<DistributionKind>().Select(value => value.ToString()),
            .. Enum.GetValues<TableType>().Select(value => value.ToString()),
            .. Enum.GetValues<RegressionModel>().Select(value => value.ToString()),
            .. Enum.GetValues<SolutionOutcome>().Select(value => value.ToString()),
            .. Enum.GetValues<FormatTarget>().Select(value => value.ToString()),
            .. Enum.GetValues<MathBoxTool>().Select(value => value.ToString()),
            .. Enum.GetValues<SameResult>().Select(value => value.ToString()),
            .. Enum.GetValues<SimulationResultView>().Select(value => value.ToString()),
            .. Enum.GetValues<SimulationTally>().Select(value => value.ToString()),
            .. Enum.GetValues<CircleScreen>().Select(value => value.ToString()),
        ];

        foreach (string choice in choices)
        {
            text.Should().ContainKey("choice:" + choice, "a screen shows this choice by name");
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryToolOfMathBoxHasItsWords(string language)
    {
        Dictionary<string, string> text = Read(language);
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.MathBox);

        foreach (MathBoxTool tool in Enum.GetValues<MathBoxTool>())
        {
            text.Should().ContainKey("about:" + tool, "the menu says what {0} does", tool);
        }

        foreach (SimulationKind kind in Enum.GetValues<SimulationKind>())
        {
            text.Should().ContainKey("count:" + kind, "the parameters say what is thrown");
            SimulationViewModel simulation = new(session, kind) { Count = 3 };
            IEnumerable<string> headings = [.. simulation.Columns, "Sum", "Diff", "Side"];
            foreach (string heading in headings)
            {
                text.Should().ContainKey("heading:" + heading, "a table of {0} is headed by it", kind);
            }
        }

        foreach (string key in (string[])["mathbox.menu", "mathbox.attempts", "mathbox.sameResult", "mathbox.execute", "mathbox.store", "mathbox.stored", "mathbox.frequency", "mathbox.relativeFrequency", "mathbox.scale", "mathbox.center", "mathbox.applyWindow", "mathbox.resetWindow"])
        {
            text.Should().ContainKey(key);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryPointTheGraphNamesHasAName(string language)
    {
        Dictionary<string, string> text = Read(language);

        foreach (GraphFeatureKind kind in Enum.GetValues<GraphFeatureKind>())
        {
            text.Should().ContainKey("graph:" + kind, "the graph of a table lists each {0} by name", kind);
        }

        foreach (string key in (string[])["table.graph", "table.fit", "table.noGraph"])
        {
            text.Should().ContainKey(key);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryGroupOfEveryCatalogHasAName(string language)
    {
        Dictionary<string, string> text = Read(language);
        SyntaxVocabulary vocabulary = PallasEngineBuilder.CreateDefault().Build().Vocabulary;

        foreach (CalculatorApp app in Enum.GetValues<CalculatorApp>())
        {
            foreach (CatalogSection section in CalculatorCatalog.For(vocabulary, app))
            {
                text.Should().ContainKey(section.NameKey, "the CATALOG of {0} shows {1} by name", app, section.Group);
            }
        }

        foreach (string key in (string[])["catalog.title", "catalog.search", "format.title", "recall.title", "menu.close"])
        {
            text.Should().ContainKey(key, "a menu of the display names itself");
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryLanguageOfTheApplicationHasAName(string language)
    {
        Dictionary<string, string> text = Read(language);

        text.Should().ContainKey("settings.language");
        foreach (string choice in CalculatorShellViewModel.Languages)
        {
            text.Should().ContainKey("language:" + choice, "the settings offer {0} by name", choice);
        }

        // Each language in its own words, whichever one is on the screen.
        text["language:en-US"].Should().Be("English");
        text["language:vi-VN"].Should().Be("Tiếng Việt");
    }

    [Fact]
    public void EveryLanguageOfTheApplicationHasItsText()
    {
        foreach (string choice in CalculatorShellViewModel.Languages.Where(name => name != CalculatorShellViewModel.SystemLanguage))
        {
            File.Exists(Path.Combine(RepositoryRoot(), "src", "app", "Barbatos.Pallas.Wpf", "Locales", $"Locales.{choice}.yaml"))
                .Should().BeTrue("the settings offer {0}", choice);
        }
    }

    [Fact]
    public void BothLanguagesSayTheSameThings()
    {
        Dictionary<string, string> english = Read("en-US");
        Dictionary<string, string> vietnamese = Read("vi-VN");

        vietnamese.Keys.Should().BeEquivalentTo(english.Keys);
        english.Values.Should().OnlyContain(value => value.Length > 0);
        vietnamese.Values.Should().OnlyContain(value => value.Length > 0);
    }

    private static Dictionary<string, string> Read(string language)
    {
        // The same small YAML the localizer reads: a line with a colon is a key, a line with nothing after the colon
        // opens a namespace that its keys are reached under.
        Dictionary<string, string> text = new(StringComparer.Ordinal);
        string space = string.Empty;
        foreach (string line in File.ReadAllLines(Path.Combine(RepositoryRoot(), "src", "app", "Barbatos.Pallas.Wpf", "Locales", $"Locales.{language}.yaml")))
        {
            string trimmed = line.Trim();
            int colon = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (trimmed.Length == 0 || trimmed.StartsWith('#') || colon < 0)
            {
                continue;
            }

            string key = trimmed[..colon];
            string value = trimmed[(colon + 1)..].Trim();
            if (value.Length == 0)
            {
                space = key + ":";
                continue;
            }

            text[space + key] = value;
        }

        return text;
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Barbatos.Pallas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find Barbatos.Pallas.slnx above " + AppContext.BaseDirectory);
    }
}
