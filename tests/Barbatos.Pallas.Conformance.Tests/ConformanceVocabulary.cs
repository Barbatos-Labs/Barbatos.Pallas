// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// The closed vocabulary of the data format (docs/CONFORMANCE.md). A value outside it is a data error.
/// </summary>
internal static class ConformanceVocabulary
{
    /// <summary>The thirteen applications of the reference calculator, with the roadmap phase that implements each.</summary>
    public static readonly IReadOnlyDictionary<string, string> Apps = new Dictionary<string, string>
    {
        ["Calculate"] = "Phase 3",
        ["Complex"] = "Phase 3",
        ["BaseN"] = "Phase 3",
        ["Statistics"] = "Phase 4",
        ["Distribution"] = "Phase 4",
        ["Spreadsheet"] = "Phase 4",
        ["Table"] = "Phase 4",
        ["Equation"] = "Phase 4",
        ["Inequality"] = "Phase 4",
        ["Matrix"] = "Phase 4",
        ["Vector"] = "Phase 4",
        ["Ratio"] = "Phase 4",
        ["MathBox"] = "Phase 6",
    };

    public static readonly HashSet<string> Kinds =
    [
        "expression", "sequence", "calc", "property", "statistics", "distribution", "spreadsheet", "table",
        "simultaneous", "polynomial", "solver", "inequality", "ratio", "mathbox",
    ];

    /// <summary>
    /// ready: fully specified. needs-oracle: the expected value needs data or a reference that does not exist yet.
    /// needs-visual-check: an input or output exists only as an image in the manual; the case states its assumption.
    /// </summary>
    public static readonly HashSet<string> Statuses = ["ready", "needs-oracle", "needs-visual-check"];

    /// <summary>
    /// manual: printed in the manual text. derived-exact: computed with exact rational arithmetic (and integer square
    /// roots) from data the manual prints - never with floating point. reference: needs a transcendental function,
    /// computed at 50+ digits with PeterO.Numbers series (docs/CONFORMANCE.md §5). pending-oracle: not yet available.
    /// </summary>
    public static readonly HashSet<string> ExpectationSources = ["manual", "derived-exact", "reference", "pending-oracle"];

    public static readonly HashSet<string> Profiles = ["Standard", "Extended"];

    public static readonly IReadOnlyDictionary<string, HashSet<string>> Settings = new Dictionary<string, HashSet<string>>
    {
        ["inputOutput"] = ["MathI/MathO", "MathI/DecimalO", "LineI/LineO", "LineI/DecimalO"],
        ["angleUnit"] = ["Degree", "Radian", "Gradian"],
        ["numberFormat"] =
        [
            "Norm1", "Norm2",
            .. Enumerable.Range(0, 10).Select(digits => $"Fix{digits}"),
            .. Enumerable.Range(1, 10).Select(digits => $"Sci{digits}"),
        ],
        ["engineerSymbol"] = ["On", "Off"],
        ["fractionResult"] = ["Improper", "Mixed"],
        ["complexResult"] = ["a+bi", "r∠θ"],
        ["decimalMark"] = ["Dot", "Comma"],
        ["digitSeparator"] = ["On", "Off"],
        ["baseMode"] = ["Decimal", "Hexadecimal", "Binary", "Octal"],
        ["frequency"] = ["On", "Off"],
        ["complexRoots"] = ["On", "Off"],
        ["verify"] = ["On", "Off"],
    };

    public static readonly HashSet<string> ErrorKinds =
    [
        "SyntaxError", "MathError", "StackError", "ArgumentError", "DimensionError", "VariableError", "CannotSolve",
        "RangeError", "TimeOut", "CircularError", "MemoryError", "NoOperator", "NotDefined",
    ];

    /// <summary>The last printed page of the manual body (the FAQ ends on p. 174).</summary>
    public const int LastPage = 174;
}
