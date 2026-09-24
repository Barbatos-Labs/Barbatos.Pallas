// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Xml.Linq;

namespace Barbatos.Pallas.Architecture.Tests;

/// <summary>
/// Every core library tracks its public surface, so that nothing reaches a package without saying so.
/// </summary>
/// <remarks>
/// From 1.0.0 on an incompatible change to the public surface of either package - the libraries it carries included -
/// waits for 2.0.0 (docs/ARCHITECTURE.md §7). <c>Microsoft.CodeAnalysis.PublicApiAnalyzers</c> fails the build of a
/// library whose public members differ from its <c>PublicAPI.Shipped.txt</c> and <c>PublicAPI.Unshipped.txt</c>; these
/// tests fail when a library is left out of that, which the analyzer itself cannot see.
/// </remarks>
public sealed class PublicApiTrackingTests
{
    private static readonly string[] Files = ["PublicAPI.Shipped.txt", "PublicAPI.Unshipped.txt"];

    public static TheoryData<string> CoreProjects => [.. ArchitectureMap.CoreProjects];

    [Theory]
    [MemberData(nameof(CoreProjects))]
    public void EveryCoreLibrary_HasItsPublicApiFiles(string project)
    {
        string directory = Path.GetDirectoryName(RepositoryLayout.ProjectFile(project))!;

        foreach (string file in Files)
        {
            string path = Path.Combine(directory, file);
            File.Exists(path).Should().BeTrue("{0} declares its public surface in {1}", project, file);

            // Without the header the analyzer reads every reference type as oblivious and tracks no nullability.
            File.ReadLines(path).First().Should().Be("#nullable enable", "{0}/{1} tracks nullable annotations", project, file);
        }

        File.ReadLines(Path.Combine(directory, Files[0])).Concat(File.ReadLines(Path.Combine(directory, Files[1]))).Skip(1)
            .Should().NotBeEmpty("{0} has a public surface to declare", project);
    }

    [Fact]
    public void TheAnalyzer_RunsOnEveryCoreLibrary()
    {
        // One ItemGroup of src/core/Directory.Build.props, without a condition, brings the analyzer and both files to
        // every core library.
        XDocument props = XDocument.Load(Path.Combine(RepositoryLayout.RepositoryRoot, "src", "core", "Directory.Build.props"));
        XElement group = props.Descendants("ItemGroup")
            .Single(items => items.Elements("PackageReference").Any(reference => reference.Attribute("Include")?.Value == "Microsoft.CodeAnalysis.PublicApiAnalyzers"));

        group.Attribute("Condition").Should().BeNull("every core library is tracked, not only some");
        group.Elements("AdditionalFiles").Select(file => file.Attribute("Include")!.Value).Should().BeEquivalentTo(Files);
    }

    [Theory]
    [MemberData(nameof(CoreProjects))]
    public void NoCoreLibrary_SilencesTheAnalyzer(string project)
    {
        string text = File.ReadAllText(RepositoryLayout.ProjectFile(project));

        text.Should().NotContain("PublicApiAnalyzers", "{0} keeps the analyzer src/core gives it", project);
        text.Should().NotContain("RS0016").And.NotContain("RS0017", "{0} may not leave a public member undeclared", project);
    }
}
