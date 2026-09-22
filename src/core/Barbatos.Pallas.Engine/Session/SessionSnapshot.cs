// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Everything a <see cref="CalculatorSession"/> holds, as text and settings an application can store and read back
/// (<see cref="CalculatorSession.Capture"/>, <see cref="CalculatorSession.Restore"/>).
/// </summary>
/// <remarks>
/// <para>
/// A calculator keeps its memory, its settings and its data when it is switched off; an application does the same by
/// writing a snapshot to its preferences. Everything here is a string, an enum or a settings record, so the
/// application can store it however it likes without knowing what a <see cref="Value"/> is; the engine writes the
/// values with all of their digits, and with their exact form where they have one, so a restored session holds what
/// was captured rather than what a display would have shown.
/// </para>
/// <para>
/// The history is not part of a snapshot: it is a list of what was calculated in this run, and an application that
/// wants to keep it across runs keeps the inputs and their displays itself.
/// </para>
/// </remarks>
public sealed record SessionSnapshot
{
    /// <summary>The version of this format; a snapshot of a later version is refused.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Gets the format version.</summary>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>Gets the application the session was in.</summary>
    public CalculatorApp App { get; init; }

    /// <summary>Gets the settings.</summary>
    public CalculatorSettings Settings { get; init; } = CalculatorSettings.Initial;

    /// <summary>Gets the regression of two-variable statistics.</summary>
    public RegressionModel Regression { get; init; }

    /// <summary>Gets the variables A, B, C, D, E, F, x, y and z, in that order.</summary>
    public ImmutableArray<string> Variables { get; init; } = [];

    /// <summary>Gets the last result (Ans).</summary>
    public string Ans { get; init; } = string.Empty;

    /// <summary>Gets the result before the last one (PreAns).</summary>
    public string PreAns { get; init; } = string.Empty;

    /// <summary>Gets MatA to MatD, each <see langword="null"/> where no matrix is stored.</summary>
    public ImmutableArray<MatrixSnapshot?> Matrices { get; init; } = [];

    /// <summary>Gets VctA to VctD, each <see langword="null"/> where no vector is stored.</summary>
    public ImmutableArray<MatrixSnapshot?> Vectors { get; init; } = [];

    /// <summary>Gets f(x) as it was defined, or <see langword="null"/>.</summary>
    public string? FunctionF { get; init; }

    /// <summary>Gets g(x) as it was defined, or <see langword="null"/>.</summary>
    public string? FunctionG { get; init; }

    /// <summary>Gets the x column of the statistics data.</summary>
    public ImmutableArray<string> StatisticsX { get; init; } = [];

    /// <summary>Gets the y column, empty for one-variable data.</summary>
    public ImmutableArray<string> StatisticsY { get; init; } = [];

    /// <summary>Gets the frequency column, empty when the data have none.</summary>
    public ImmutableArray<string> StatisticsFrequencies { get; init; } = [];
}

/// <summary>
/// A matrix or a vector of a <see cref="SessionSnapshot"/>: its size and its entries, row by row.
/// </summary>
/// <param name="Rows">The rows; 1 for a vector.</param>
/// <param name="Columns">The columns; the dimension of a vector.</param>
/// <param name="Entries">The entries, row by row.</param>
public sealed record MatrixSnapshot(int Rows, int Columns, ImmutableArray<string> Entries);
