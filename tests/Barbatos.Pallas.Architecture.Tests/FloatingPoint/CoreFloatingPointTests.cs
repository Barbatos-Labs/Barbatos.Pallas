// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Architecture.Tests.FloatingPoint;

/// <summary>
/// No compiled core assembly may contain binary floating point outside the reviewed allow-list.
/// </summary>
public sealed class CoreFloatingPointTests
{
    /// <summary>
    /// Where binary floating point may appear, per assembly, each with its reason. <c>decimal</c> is not floating point
    /// here and is allowed everywhere. Adding an entry is a design decision: it needs the same review as changing
    /// docs/PRECISION.md.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (string TypePrefix, string Reason)[]> AllowList =
        new Dictionary<string, (string TypePrefix, string Reason)[]>
        {
            ["Barbatos.Pallas.Numerics"] =
            [
                ("Barbatos.Pallas.Numerics.",
                    "Trigonometry in calculator angle units, on double.SinPi, double.CosPi, double.TanPi and System.Math (PRECISION.md §6)."),
            ],
            ["Barbatos.Pallas.Solvers"] =
            [
                ("Barbatos.Pallas.Solvers.PolynomialRoots",
                    "The roots of a polynomial of degree 3 or more have no closed form a calculator can display, so they are "
                    + "iterated in double and System.Numerics.Complex; a root that is rational is recovered exactly from them "
                    + "(PRECISION.md §7). IntegerPolynomial stays exact."),
            ],
            ["Barbatos.Pallas.Engine"] =
            [
                ("Barbatos.Pallas.Engine.",
                    "The engine calls System.Math and System.Numerics.Complex directly for transcendental and complex functions, "
                    + "holds values decimal cannot keep to 15 significant digits as double, and integrates numerically (PRECISION.md §§3, 6, 7)."),
            ],
        };

    public static TheoryData<string> CoreAssemblies => [.. ArchitectureMap.CoreProjects];

    [Theory]
    [MemberData(nameof(CoreAssemblies))]
    public void CoreAssembly_ContainsNoFloatingPoint(string project)
    {
        (string TypePrefix, string Reason)[] allowed = AllowList.GetValueOrDefault(project) ?? [];

        FloatingPointUsage[] violations =
        [
            .. FloatingPointScanner.Scan(RepositoryLayout.AssemblyPath(project))
                .Where(usage => !allowed.Any(entry => usage.Location.StartsWith(entry.TypePrefix, StringComparison.Ordinal))),
        ];

        violations.Should().BeEmpty(
            "binary floating point is confined to the allow-listed assembly (docs/PRECISION.md), but {0} contains it. Found:{1}{2}",
            project, Environment.NewLine, string.Join(Environment.NewLine, violations.Select(violation => violation.ToString())));
    }
}
