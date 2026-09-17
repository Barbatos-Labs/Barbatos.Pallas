// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace Barbatos.Pallas.Architecture.Tests;

public sealed class DependencyRulesTests
{
    public static TheoryData<string> AllProjects => [.. ArchitectureMap.DeclaredDependencies.Keys];

    public static TheoryData<string> InspectableAssemblies => [.. ArchitectureMap.CoreProjects, .. ArchitectureMap.PortableAppProjects];

    public static TheoryData<string> CoreProjects => [.. ArchitectureMap.CoreProjects];

    [Theory]
    [MemberData(nameof(AllProjects))]
    public void ProjectReferences_MatchTheArchitectureMap(string project)
    {
        XDocument document = XDocument.Load(RepositoryLayout.ProjectFile(project));
        string[] declared =
        [
            .. document.Descendants("ProjectReference")
                .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value))
                .Order(StringComparer.Ordinal),
        ];

        declared.Should().Equal(ArchitectureMap.DeclaredDependencies[project].Order(StringComparer.Ordinal),
            "src/{0} must reference exactly the projects docs/ARCHITECTURE.md allows - change the map and the diagram together", project);
    }

    [Fact]
    public void ArchitectureMap_HasNoCycles()
    {
        foreach (string project in ArchitectureMap.DeclaredDependencies.Keys)
        {
            ArchitectureMap.TransitiveDependencies(project).Should().NotContain(project, "a dependency cycle through {0} would make layering meaningless", project);
        }
    }

    [Theory]
    [MemberData(nameof(CoreProjects))]
    public void CoreProjects_DoNotOverrideTargetFrameworks(string project)
    {
        // A csproj that sets <TargetFramework> or <TargetFrameworks> silently narrows or diverges from the
        // net8.0;net9.0;net10.0 contract that src/core/Directory.Build.props establishes for every package.
        XDocument document = XDocument.Load(RepositoryLayout.ProjectFile(project));

        document.Descendants("TargetFramework").Should().BeEmpty();
        document.Descendants("TargetFrameworks").Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(InspectableAssemblies))]
    public void CompiledReferences_StayInsideTheAllowedGraph(string project)
    {
        using FileStream stream = File.OpenRead(RepositoryLayout.AssemblyPath(project));
        using PEReader reader = new(stream);
        MetadataReader metadata = reader.GetMetadataReader();

        HashSet<string> allowedPallas = ArchitectureMap.TransitiveDependencies(project);
        string[] allowedExternal = ArchitectureMap.AllowedExternalAssemblies.GetValueOrDefault(project) ?? [];

        List<string> violations = [];
        foreach (AssemblyReferenceHandle handle in metadata.AssemblyReferences)
        {
            string name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);

            bool allowed = name.StartsWith(ArchitectureMap.Prefix, StringComparison.Ordinal)
                ? allowedPallas.Contains(name)
                : IsAllowedExternal(name, allowedExternal);

            if (!allowed)
            {
                violations.Add(name);
            }
        }

        violations.Should().BeEmpty("{0} may only reference its declared dependencies and the BCL (no UI or GDI assemblies)", project);
    }

    private static bool IsAllowedExternal(string name, string[] allowedExternal)
    {
        if (allowedExternal.Contains(name, StringComparer.Ordinal))
        {
            return true;
        }

        bool isBcl = name is "System" or "mscorlib" or "netstandard" || name.StartsWith("System.", StringComparison.Ordinal);
        return isBcl && !ArchitectureMap.ForbiddenBclAssemblies.Contains(name, StringComparer.Ordinal);
    }
}
