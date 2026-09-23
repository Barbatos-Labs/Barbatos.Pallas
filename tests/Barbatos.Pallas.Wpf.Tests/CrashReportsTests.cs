// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.IO;
using System.Linq;
using Barbatos.i18n;
using Barbatos.i18n.DependencyInjection;
using Barbatos.i18n.Yaml;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// An exception nothing caught leaves a report behind, and the user is told where it is in their own language.
/// </summary>
public sealed class CrashReportsTests : IDisposable
{
    private static readonly DateTimeOffset Moment = new(2026, 9, 23, 22, 15, 30, 125, TimeSpan.FromHours(7));

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pallas-crash-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void AReportSaysWhatWasThrownAndWhere()
    {
        InvalidOperationException thrown = Thrown();

        string path = CrashReports.Write(_directory, thrown, Moment, "0.1.0+abc");

        Path.GetFileName(path).Should().Be("crash-20260923-151530-125.txt", "the name is the moment in UTC");
        string text = File.ReadAllText(path);
        text.Should().StartWith("Barbatos Pallas 0.1.0+abc");
        text.Should().Contain("2026-09-23 15:15:30.125 UTC");
        text.Should().Contain("System.InvalidOperationException: The key did nothing.");
        text.Should().Contain(nameof(Thrown), "the stack is what makes the fault one that can be found");
    }

    [Fact]
    public void AReportIsWrittenTheSameWayInEveryCulture()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");

            string path = CrashReports.Write(_directory, Thrown(), Moment, "0.1.0");

            File.ReadAllText(path).Should().Contain("2026-09-23 15:15:30.125 UTC");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void TwoReportsOfTheSameMomentAreTwoFiles()
    {
        string first = CrashReports.Write(_directory, Thrown(), Moment, "0.1.0");
        string second = CrashReports.Write(_directory, Thrown(), Moment, "0.1.0");

        second.Should().NotBe(first);
        Directory.GetFiles(_directory).Should().HaveCount(2);
    }

    [Fact]
    public void OnlyTheNewestReportsAreKept()
    {
        for (int minute = 0; minute < CrashReports.Kept + 5; minute++)
        {
            CrashReports.Write(_directory, Thrown(), Moment.AddMinutes(minute), "0.1.0");
        }

        string[] kept = [.. Directory.GetFiles(_directory).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];
        kept.Should().HaveCount(CrashReports.Kept);
        kept[0].Should().Be("crash-20260923-152030-125.txt", "the five oldest went");
        kept[^1].Should().Be("crash-20260923-153930-125.txt");
    }

    [Fact]
    public void PruningLeavesOtherFilesAndMissingFoldersAlone()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "notes.txt"), "mine");

        CrashReports.Prune(_directory);
        CrashReports.Prune(Path.Combine(_directory, "missing"));

        File.Exists(Path.Combine(_directory, "notes.txt")).Should().BeTrue();
    }

    [Fact]
    public void ArgumentsAreRequired()
    {
        ((Action)(() => CrashReports.Write(" ", Thrown(), Moment, "0.1.0"))).Should().Throw<ArgumentException>().WithParameterName("directory");
        ((Action)(() => CrashReports.Write(_directory, null!, Moment, "0.1.0"))).Should().Throw<ArgumentNullException>().WithParameterName("exception");
        ((Action)(() => CrashReports.Write(_directory, Thrown(), Moment, null!))).Should().Throw<ArgumentNullException>().WithParameterName("version");
        ((Action)(() => CrashReports.Prune(string.Empty))).Should().Throw<ArgumentException>().WithParameterName("directory");
    }

    [Theory]
    [InlineData("en-US", "Your session has been saved")]
    [InlineData("vi-VN", "Phiên làm việc đã được lưu")]
    public void TheUserIsToldWhereTheReportIs(string language, string words)
    {
        // Through the localizer the application builds, from the files embedded in it: the message has a place for
        // the path, and a YAML reader that took {0} for something else would drop it.
        ServiceCollection services = new();
        services.AddStringLocalizer(localization =>
        {
            localization.FromYaml(typeof(CrashReports).Assembly, "Locales.en-US.yaml", new CultureInfo("en-US"));
            localization.FromYaml(typeof(CrashReports).Assembly, "Locales.vi-VN.yaml", new CultureInfo("vi-VN"));
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        CultureInfo culture = CultureInfo.CurrentCulture;
        CultureInfo interfaceCulture = CultureInfo.CurrentUICulture;
        try
        {
            // As AppLanguage switches the application: through the culture manager, not the thread's culture alone.
            provider.GetRequiredService<ILocalizationCultureManager>().SetCulture(language);
            ICompositeStringLocalizer text = provider.GetRequiredService<ICompositeStringLocalizer>();

            string message = text["crash.message", @"C:\data\crash-reports\crash-1.txt"].Value;

            message.Should().Contain(words).And.EndWith(@"C:\data\crash-reports\crash-1.txt");
            text["crash.title"].Value.Should().Be("Barbatos Pallas");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = interfaceCulture;
        }
    }

    private static InvalidOperationException Thrown()
    {
        try
        {
            throw new InvalidOperationException("The key did nothing.");
        }
        catch (InvalidOperationException exception)
        {
            return exception;
        }
    }
}
