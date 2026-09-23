// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// The language chosen on the settings screen is the one the application speaks, this run and the next.
/// </summary>
public sealed class AppLanguageTests
{
    private static readonly CultureInfo Windows = CultureInfo.GetCultureInfo("fr-FR");

    private static CalculatorShellViewModel Shell() =>
        new(PallasEngineBuilder.CreateDefault().Build().CreateSession(), new InMemorySessionStore());

    [Fact]
    public void AFirstRunSpeaksTheLanguageOfWindows()
    {
        CalculatorShellViewModel shell = Shell();
        List<CultureInfo> applied = [];

        AppLanguage.Follow(shell, new FakePreferences(), Windows, applied.Add);

        shell.Language.Should().Be(CalculatorShellViewModel.SystemLanguage);
        applied.Should().Equal(Windows);
    }

    [Fact]
    public void TheStoredLanguageIsTheOneSpoken()
    {
        CalculatorShellViewModel shell = Shell();
        FakePreferences preferences = new();
        preferences.Set(AppLanguage.Key, "vi-VN");
        List<CultureInfo> applied = [];

        AppLanguage.Follow(shell, preferences, Windows, applied.Add);

        shell.Language.Should().Be("vi-VN");
        applied.Select(culture => culture.Name).Should().Equal("vi-VN");
    }

    [Fact]
    public void ALanguageThisBuildDoesNotSpeakIsWindowsOwn()
    {
        CalculatorShellViewModel shell = Shell();
        FakePreferences preferences = new();
        preferences.Set(AppLanguage.Key, "de-DE");
        List<CultureInfo> applied = [];

        AppLanguage.Follow(shell, preferences, Windows, applied.Add);

        shell.Language.Should().Be(CalculatorShellViewModel.SystemLanguage);
        applied.Should().Equal(Windows);
    }

    [Fact]
    public void AChoiceIsSpokenAtOnceAndKept()
    {
        CalculatorShellViewModel shell = Shell();
        FakePreferences preferences = new();
        List<CultureInfo> applied = [];
        AppLanguage.Follow(shell, preferences, Windows, applied.Add);

        shell.Language = "en-US";
        shell.Language = CalculatorShellViewModel.SystemLanguage;

        applied.Select(culture => culture.Name).Should().Equal("fr-FR", "en-US", "fr-FR");
        preferences.Values[AppLanguage.Key].Should().Be(CalculatorShellViewModel.SystemLanguage);
    }

    [Fact]
    public void OtherChangesOfTheShellAreNotAChoiceOfLanguage()
    {
        CalculatorShellViewModel shell = Shell();
        FakePreferences preferences = new();
        List<CultureInfo> applied = [];
        AppLanguage.Follow(shell, preferences, Windows, applied.Add);

        shell.Open(CalculatorApps.Of(CalculatorApp.Statistics));

        applied.Should().ContainSingle();
        preferences.Values.Should().NotContainKey(AppLanguage.Key);
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("vi-VN", "vi-VN")]
    [InlineData(CalculatorShellViewModel.SystemLanguage, "fr-FR")]
    [InlineData("", "fr-FR")]
    [InlineData("ja-JP", "fr-FR")]
    public void EveryLanguageIsACulture(string language, string culture)
    {
        AppLanguage.CultureOf(language, Windows).Name.Should().Be(culture);
    }

    [Fact]
    public void ArgumentsAreRequired()
    {
        CalculatorShellViewModel shell = Shell();
        FakePreferences preferences = new();
        Action<CultureInfo> apply = _ => { };

        ((Action)(() => AppLanguage.Follow(null!, preferences, Windows, apply))).Should().Throw<ArgumentNullException>()
            .WithParameterName("shell");
        ((Action)(() => AppLanguage.Follow(shell, null!, Windows, apply))).Should().Throw<ArgumentNullException>()
            .WithParameterName("preferences");
        ((Action)(() => AppLanguage.Follow(shell, preferences, null!, apply))).Should().Throw<ArgumentNullException>()
            .WithParameterName("windows");
        ((Action)(() => AppLanguage.Follow(shell, preferences, Windows, null!))).Should().Throw<ArgumentNullException>()
            .WithParameterName("apply");
        ((Action)(() => AppLanguage.CultureOf("en-US", null!))).Should().Throw<ArgumentNullException>()
            .WithParameterName("windows");
    }
}
