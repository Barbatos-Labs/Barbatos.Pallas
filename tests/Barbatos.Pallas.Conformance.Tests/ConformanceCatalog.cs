// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text.Json;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Loads the conformance data once per test run.
/// </summary>
internal static class ConformanceCatalog
{
    public const string DomainsFileName = "domains.json";

    /// <summary>
    /// Strict on purpose: an unknown member is a typo ("expcet") that would otherwise drop an expectation and let
    /// a case pass vacuously.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    private static readonly Lazy<IReadOnlyList<(string FileName, ConformanceFile File)>> LoadedFiles = new(LoadFiles);

    private static readonly Lazy<DomainsFile> LoadedDomains = new(() =>
        JsonSerializer.Deserialize<DomainsFile>(File.ReadAllText(Path.Combine(DataDirectory, DomainsFileName)), Options)!);

    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data", "calculator");

    public static IReadOnlyList<(string FileName, ConformanceFile File)> Files => LoadedFiles.Value;

    public static IEnumerable<ConformanceCase> Cases => Files.SelectMany(file => file.File.Cases);

    public static DomainsFile Domains => LoadedDomains.Value;

    public static ConformanceCase Find(string id)
    {
        return Cases.Single(conformanceCase => conformanceCase.Id == id);
    }

    private static List<(string FileName, ConformanceFile File)> LoadFiles()
    {
        List<(string, ConformanceFile)> files = [];
        foreach (string path in Directory.EnumerateFiles(DataDirectory, "*.json").Order(StringComparer.Ordinal))
        {
            string fileName = Path.GetFileName(path);
            if (fileName == DomainsFileName)
            {
                continue;
            }

            ConformanceFile file = JsonSerializer.Deserialize<ConformanceFile>(File.ReadAllText(path), Options)
                ?? throw new InvalidDataException($"{fileName} is empty.");

            foreach (ConformanceCase conformanceCase in file.Cases)
            {
                conformanceCase.FileName = fileName;
            }

            files.Add((fileName, file));
        }

        return files;
    }
}
