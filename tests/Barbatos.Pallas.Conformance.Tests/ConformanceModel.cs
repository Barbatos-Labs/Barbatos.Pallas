// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>One data file under Data/calculator, grouping the cases of a manual chapter.</summary>
internal sealed class ConformanceFile
{
    public required string Source { get; init; }

    public required string Chapter { get; init; }

    public required List<ConformanceCase> Cases { get; init; }
}

/// <summary>One worked example. The format is specified in docs/CONFORMANCE.md.</summary>
internal sealed class ConformanceCase
{
    public required string Id { get; init; }

    public required int Page { get; init; }

    public required string App { get; init; }

    public required string Kind { get; init; }

    public required string Status { get; init; }

    public string Profile { get; init; } = "Standard";

    public Dictionary<string, string> Settings { get; init; } = [];

    public string? Input { get; init; }

    public JsonElement? Given { get; init; }

    public JsonElement? Expect { get; init; }

    public List<ConformanceStep>? Steps { get; init; }

    public required string ExpectationSource { get; init; }

    public string? Note { get; init; }

    [JsonIgnore]
    public string FileName { get; set; } = "";

    public override string ToString()
    {
        return $"{Id} (p. {Page}, {FileName})";
    }
}

/// <summary>One step of a sequence case, evaluated in order against the same calculator session.</summary>
internal sealed class ConformanceStep
{
    public string? Input { get; init; }

    public string? Store { get; init; }

    public string? SwitchBaseMode { get; init; }

    public JsonElement? Expect { get; init; }
}

/// <summary>The function input-domain table of pp. 169-171.</summary>
internal sealed class DomainsFile
{
    public required string Source { get; init; }

    public required string Chapter { get; init; }

    public required JsonElement CalculationRange { get; init; }

    public required List<FunctionDomain> Domains { get; init; }
}

internal sealed class FunctionDomain
{
    public required string Function { get; init; }

    public string? AngleUnit { get; init; }

    public required string Domain { get; init; }

    public required int Page { get; init; }

    public string? Note { get; init; }
}
