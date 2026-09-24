// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Architecture.Tests;

/// <summary>
/// The intended dependency graph, written down once. docs/ARCHITECTURE.md §2 draws the same graph; when a
/// project reference changes, this table and that diagram change together or a test fails.
/// </summary>
internal static class ArchitectureMap
{
    public const string Prefix = "Barbatos.Pallas.";

    /// <summary>Direct project references each project is declared with, and nothing else.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> DeclaredDependencies = new Dictionary<string, string[]>
    {
        ["Barbatos.Pallas.Numerics"] = [],
        ["Barbatos.Pallas.LinearAlgebra"] = ["Barbatos.Pallas.Numerics"],
        ["Barbatos.Pallas.Statistics"] = ["Barbatos.Pallas.Numerics"],
        ["Barbatos.Pallas.Expressions"] = [],
        ["Barbatos.Pallas.Solvers"] = [],
        ["Barbatos.Pallas.Engine"] = ["Barbatos.Pallas.Numerics", "Barbatos.Pallas.Expressions", "Barbatos.Pallas.LinearAlgebra", "Barbatos.Pallas.Statistics", "Barbatos.Pallas.Solvers"],
        ["Barbatos.Pallas.Spreadsheet"] = ["Barbatos.Pallas.Engine"],
        ["Barbatos.Pallas.Data"] = ["Barbatos.Pallas.Engine"],
        ["Barbatos.Pallas.Graphing"] = ["Barbatos.Pallas.Engine"],
        ["Barbatos.Pallas.DependencyInjection"] = ["Barbatos.Pallas.Engine", "Barbatos.Pallas.Data", "Barbatos.Pallas.Spreadsheet", "Barbatos.Pallas.Graphing"],
        ["Barbatos.Pallas.Presentation"] = ["Barbatos.Pallas.Engine", "Barbatos.Pallas.Spreadsheet", "Barbatos.Pallas.Graphing"],
        ["Barbatos.Pallas.Wpf"] = ["Barbatos.Pallas.Presentation", "Barbatos.Pallas.DependencyInjection"],
    };

    /// <summary>
    /// The projects published to nuget.org (maintainer, 23 Sep 2026: only what is large and widely useful is worth a
    /// package). Every other library travels inside the one published package that references it.
    /// </summary>
    public static readonly string[] PublishedPackages =
    [
        "Barbatos.Pallas.Engine",
        "Barbatos.Pallas.DependencyInjection",
    ];

    /// <summary>
    /// Libraries no package carries, with the reason; one that gains code has to be given a package. None since Phase 6
    /// gave Graphing its code and the DependencyInjection package carries it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> NotShipped = new Dictionary<string, string>();

    /// <summary>Projects under src/core: platform-neutral; binary floating point only where allow-listed.</summary>
    public static readonly string[] CoreProjects =
    [
        "Barbatos.Pallas.Numerics",
        "Barbatos.Pallas.LinearAlgebra",
        "Barbatos.Pallas.Statistics",
        "Barbatos.Pallas.Solvers",
        "Barbatos.Pallas.Expressions",
        "Barbatos.Pallas.Engine",
        "Barbatos.Pallas.Spreadsheet",
        "Barbatos.Pallas.Data",
        "Barbatos.Pallas.Graphing",
        "Barbatos.Pallas.DependencyInjection",
    ];

    /// <summary>Projects under src/app whose assemblies the test host can load (net8/9/10, not -windows).</summary>
    public static readonly string[] PortableAppProjects =
    [
        "Barbatos.Pallas.Presentation",
    ];

    /// <summary>
    /// Non-Pallas assemblies a project may reference in its compiled metadata, beyond the BCL. The BCL itself
    /// (System.*) is always allowed except for the UI and GDI assemblies in <see cref="ForbiddenBclAssemblies"/>.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> AllowedExternalAssemblies = new Dictionary<string, string[]>
    {
        ["Barbatos.Pallas.DependencyInjection"] = ["Microsoft.Extensions.DependencyInjection.Abstractions", "Microsoft.Extensions.Options"],
        ["Barbatos.Pallas.Presentation"] = ["CommunityToolkit.Mvvm"],
    };

    /// <summary>Framework assemblies that tie code to a UI stack or to Windows GDI.</summary>
    public static readonly string[] ForbiddenBclAssemblies =
    [
        "System.Xaml",
        "System.Drawing",
        "System.Drawing.Common",
        "System.Windows.Forms",
        "PresentationCore",
        "PresentationFramework",
        "WindowsBase",
    ];

    /// <summary>Every project reachable from <paramref name="project"/> through declared references, excluding itself.</summary>
    public static HashSet<string> TransitiveDependencies(string project)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        Stack<string> pending = new(DeclaredDependencies[project]);

        while (pending.Count > 0)
        {
            string next = pending.Pop();
            if (seen.Add(next))
            {
                foreach (string dependency in DeclaredDependencies[next])
                {
                    pending.Push(dependency);
                }
            }
        }

        return seen;
    }
}
