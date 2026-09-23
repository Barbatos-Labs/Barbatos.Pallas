// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// The installer and the application say the same thing about who the application is.
/// </summary>
/// <remarks>
/// packaging/Barbatos.Pallas.json is what Barbatos.PackagingEngine builds the installer from, and the .csproj is what
/// every other build reads. barbatos-pack validate compares the two, but it is an internal tool CI cannot install, so
/// the comparisons that fail quietly are here: an AppId that differs strands every user's data, an uninstall key
/// without Inno's _is1 leaves AppInfo.InstallDate null forever, and a version that differs is an installer of one
/// number over binaries of another.
/// </remarks>
public sealed class PackagingProfileTests
{
    private static readonly JsonDocumentOptions Jsonc = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    [Fact]
    public void TheIdentityIsTheOneTheProfileNames()
    {
        using JsonDocument profile = Profile();
        JsonElement identity = profile.RootElement.GetProperty("Identity");
        string appGuid = identity.GetProperty("AppGuid").GetString()!;

        Metadata("Barbatos.Wpf.ApplicationModel.AppInfo.AppId").Should().Be(appGuid);
        Metadata("Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey").Should().Be(appGuid + "_is1");
        Property("Product").Should().Be(identity.GetProperty("AppName").GetString());
        Property("Company").Should().Be(profile.RootElement.GetProperty("Publisher").GetProperty("Name").GetString());
    }

    [Fact]
    public void TheVersionIsTheOneTheInstallerCarries()
    {
        using JsonDocument profile = Profile();

        Property("Version").Should().Be(profile.RootElement.GetProperty("Identity").GetProperty("Version").GetString());
    }

    [Fact]
    public void TheAppIdIsTheOneTheLedgerPinned()
    {
        // The AppId names the user-data folder, so it never changes after the first release (CLAUDE.md, Identity).
        using JsonDocument ledger = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "identity.lock.json")));

        Metadata("Barbatos.Wpf.ApplicationModel.AppInfo.AppId").Should()
            .Be(ledger.RootElement.GetProperty("Barbatos.Pallas").GetProperty("AppGuid").GetString())
            .And.Be("{5E50D3E6-0148-428F-88C9-F824709C75C4}");
    }

    [Fact]
    public void TheProfileBuildsThisApplication()
    {
        using JsonDocument profile = Profile();
        JsonElement build = profile.RootElement.GetProperty("Build");
        XDocument project = Project();

        File.Exists(Path.Combine(RepositoryRoot(), build.GetProperty("Project").GetString()!)).Should().BeTrue();
        build.GetProperty("ExpectedTargetFramework").GetString().Should().Be(Property("TargetFramework"));
        project.Descendants("ApplicationIcon").Single().Value.Should().NotBeEmpty("an installed application needs its icon");
    }

    private static JsonDocument Profile() =>
        JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot(), "packaging", "Barbatos.Pallas.json")), Jsonc);

    private static XDocument Project() =>
        XDocument.Load(Path.Combine(RepositoryRoot(), "src", "app", "Barbatos.Pallas.Wpf", "Barbatos.Pallas.Wpf.csproj"));

    private static string? Property(string name) => Project().Descendants(name).Select(element => element.Value.Trim()).SingleOrDefault();

    private static string? Metadata(string key) =>
        Project().Descendants("AssemblyMetadata")
            .Where(item => item.Attribute("Include")?.Value == key)
            .Select(item => item.Attribute("Value")?.Value)
            .SingleOrDefault();

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
