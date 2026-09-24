// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Xml.Linq;

namespace Barbatos.Pallas.Architecture.Tests;

/// <summary>
/// Two projects are packages, and every other library ships inside exactly one of them.
/// </summary>
/// <remarks>
/// NuGet puts a project reference into a package as a dependency on a package of that name, whether or not one is
/// ever published, so a library that is not a package has to be carried: Engine carries every project it references
/// and drops every dependency (SuppressDependenciesWhenPacking), DependencyInjection references what it carries with
/// PrivateAssets="all" and keeps Engine as a dependency. Each way is wrong the moment the csproj changes under it -
/// a package reference added to Engine would be dropped from the package without a word - so the rules are here.
/// build/Test-Packages.ps1 installs what comes out of the pack and is the other half.
/// </remarks>
public sealed class PackagingRulesTests
{
    public static TheoryData<string> AllProjects => [.. ArchitectureMap.DeclaredDependencies.Keys];

    public static TheoryData<string> PublishedPackages => [.. ArchitectureMap.PublishedPackages];

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void OnlyThePublishedProjects_ArePackages(string project)
    {
        XDocument document = XDocument.Load(RepositoryLayout.ProjectFile(project));
        bool published = ArchitectureMap.PublishedPackages.Contains(project);

        Property(document, "IsPackable").Should().Be(published ? "true" : null, "{0} is {1}published", project, published ? string.Empty : "not ");
        Property(document, "PackageId").Should().Be(published ? project : null, "a package is named after its project");
        if (!published)
        {
            Property(document, "PackageTags").Should().BeNull("{0} is not a package, so it has nothing to tag", project);
        }
    }

    [Fact]
    public void TheLibraries_ArePackagesOnlyWhenTheySaySo()
    {
        // The tier's props used to make every library a package; now each published csproj says it is one.
        XDocument props = XDocument.Load(Path.Combine(RepositoryLayout.RepositoryRoot, "src", "core", "Directory.Build.props"));

        props.Descendants("IsPackable").Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(PublishedPackages))]
    public void APublishedPackage_CarriesTheLibrariesItReferences(string package)
    {
        XDocument document = XDocument.Load(RepositoryLayout.ProjectFile(package));

        if (Property(document, "SuppressDependenciesWhenPacking") == "true")
        {
            // Every dependency is dropped from the package, which is only right while it has none to drop.
            document.Descendants("PackageReference").Where(reference => reference.Attribute("PrivateAssets")?.Value != "all")
                .Should().BeEmpty("{0} drops its dependencies, so a package it needs would be missing for its users", package);
            ProjectReferences(document).Select(reference => reference.Name).Should().NotIntersectWith(ArchitectureMap.PublishedPackages,
                "{0} drops its dependencies, so a published package it references would be missing for its users", package);
            return;
        }

        foreach ((string name, string? privateAssets) in ProjectReferences(document))
        {
            if (ArchitectureMap.PublishedPackages.Contains(name))
            {
                privateAssets.Should().BeNull("{0} is a package, so {1} depends on it", name, package);
            }
            else
            {
                privateAssets.Should().Be("all", "{0} is not a package, so {1} carries it rather than depending on it", name, package);
            }
        }
    }

    [Fact]
    public void EveryLibrary_ShipsInExactlyOnePackage()
    {
        Dictionary<string, List<string>> shippedIn = ArchitectureMap.CoreProjects.ToDictionary(project => project, _ => new List<string>());
        foreach (string package in ArchitectureMap.PublishedPackages)
        {
            shippedIn[package].Add(package);
            foreach (string carried in Carried(package))
            {
                shippedIn[carried].Add(package);
            }
        }

        foreach ((string library, List<string> packages) in shippedIn)
        {
            if (ArchitectureMap.NotShipped.TryGetValue(library, out string? reason))
            {
                packages.Should().BeEmpty("{0} ships nowhere: {1}", library, reason);
                Directory.EnumerateFiles(Path.GetDirectoryName(RepositoryLayout.ProjectFile(library))!, "*.cs").Should()
                    .BeEmpty("{0} has code now, so it needs a package to ship in", library);
            }
            else
            {
                packages.Should().ContainSingle("{0} must reach nuget.org in exactly one package", library);
            }
        }
    }

    /// <summary>The libraries a published package carries inside it.</summary>
    internal static IEnumerable<string> Carried(string package)
    {
        XDocument document = XDocument.Load(RepositoryLayout.ProjectFile(package));
        return Property(document, "SuppressDependenciesWhenPacking") == "true"
            ? ArchitectureMap.TransitiveDependencies(package)
            : ProjectReferences(document).Where(reference => reference.PrivateAssets == "all").Select(reference => reference.Name);
    }

    private static IEnumerable<(string Name, string? PrivateAssets)> ProjectReferences(XDocument document) =>
        document.Descendants("ProjectReference").Select(reference =>
            (Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value), reference.Attribute("PrivateAssets")?.Value));

    private static string? Property(XDocument document, string name) =>
        document.Descendants(name).Select(element => element.Value.Trim()).LastOrDefault();
}
