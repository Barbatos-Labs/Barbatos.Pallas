// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Architecture.Tests;

/// <summary>
/// Locates the repository and the build outputs the tests inspect.
/// </summary>
internal static class RepositoryLayout
{
    private static readonly Lazy<string> Root = new(FindRoot);

    public static string RepositoryRoot => Root.Value;

    public static string ProjectFile(string project)
    {
        string tier = project is "Barbatos.Pallas.Presentation" or "Barbatos.Pallas.Rendering.Skia" or "Barbatos.Pallas.Wpf" ? "app" : "core";
        return Path.Combine(RepositoryRoot, "src", tier, project, project + ".csproj");
    }

    /// <summary>The compiled assembly of a referenced project, as copied into this test's output directory.</summary>
    public static string AssemblyPath(string project)
    {
        string path = Path.Combine(AppContext.BaseDirectory, project + ".dll");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"'{project}.dll' is not in the test output. Is it referenced by Barbatos.Pallas.Architecture.Tests.csproj?", path);
        }

        return path;
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Barbatos.Pallas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find Barbatos.Pallas.slnx above " + AppContext.BaseDirectory);
    }
}
