// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using Barbatos.Pallas.Presentation;

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
