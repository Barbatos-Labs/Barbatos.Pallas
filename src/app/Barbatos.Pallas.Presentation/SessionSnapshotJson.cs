// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// A <see cref="SessionSnapshot"/> as one string, so a host can store a session wherever it keeps its preferences.
/// </summary>
/// <remarks>
/// <para>
/// This is a stored format: what it writes today has to be readable by every later build, so it is JSON of plain
/// strings and flags - the values themselves are already the engine's own text - and every name here is fixed. An
/// enum is written by name rather than by number, because a number would change meaning when a value is inserted.
/// </para>
/// <para>
/// Reading is forgiving in one direction only: a field that is missing or says something this build does not know
/// falls back to what a new calculator has, and text that is not JSON at all is no session. A snapshot of a later
/// version is read as it stands and refused by <see cref="CalculatorSession.Restore"/>, which is where that rule
/// lives.
/// </para>
/// </remarks>
public static class SessionSnapshotJson
{
    /// <summary>Writes a snapshot as JSON.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <returns>The JSON text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string Write(SessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        SnapshotDocument document = new()
        {
            Version = snapshot.Version,
            App = snapshot.App.ToString(),
            Settings = new SettingsDocument
            {
                InputOutput = snapshot.Settings.InputOutput.ToString(),
                AngleUnit = snapshot.Settings.AngleUnit.ToString(),
                NumberFormat = NumberFormats.Write(snapshot.Settings.NumberFormat),
                EngineerSymbol = snapshot.Settings.EngineerSymbol,
                FractionResult = snapshot.Settings.FractionResult.ToString(),
                ComplexResult = snapshot.Settings.ComplexResult.ToString(),
                DecimalMark = snapshot.Settings.DecimalMark.ToString(),
                DigitSeparator = snapshot.Settings.DigitSeparator,
                BaseMode = snapshot.Settings.BaseMode.ToString(),
                Verify = snapshot.Settings.Verify,
                ComplexRoots = snapshot.Settings.ComplexRoots,
            },
            Regression = snapshot.Regression.ToString(),
            Variables = [.. snapshot.Variables],
            Ans = snapshot.Ans,
            PreAns = snapshot.PreAns,
            Matrices = Written(snapshot.Matrices),
            Vectors = Written(snapshot.Vectors),
            FunctionF = snapshot.FunctionF,
            FunctionG = snapshot.FunctionG,
            StatisticsX = [.. snapshot.StatisticsX],
            StatisticsY = [.. snapshot.StatisticsY],
            StatisticsFrequencies = [.. snapshot.StatisticsFrequencies],
        };

        return JsonSerializer.Serialize(document, SnapshotJsonContext.Default.SnapshotDocument);
    }

    /// <summary>Reads back what <see cref="Write"/> produced.</summary>
    /// <param name="json">The JSON text, or <see langword="null"/>.</param>
    /// <returns>The snapshot, or <see langword="null"/> when the text is not one.</returns>
    public static SessionSnapshot? Read(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        SnapshotDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(json, SnapshotJsonContext.Default.SnapshotDocument);
        }
        catch (JsonException)
        {
            return null;
        }

        if (document is null)
        {
            return null;
        }

        SettingsDocument settings = document.Settings ?? new SettingsDocument();
        CalculatorSettings initial = CalculatorSettings.Initial;
        return new SessionSnapshot
        {
            Version = document.Version,
            App = Parsed(document.App, CalculatorApp.Calculate),
            Settings = initial with
            {
                InputOutput = Parsed(settings.InputOutput, initial.InputOutput),
                AngleUnit = Parsed(settings.AngleUnit, initial.AngleUnit),
                NumberFormat = NumberFormats.TryRead(settings.NumberFormat, out NumberFormat format) ? format : initial.NumberFormat,
                EngineerSymbol = settings.EngineerSymbol,
                FractionResult = Parsed(settings.FractionResult, initial.FractionResult),
                ComplexResult = Parsed(settings.ComplexResult, initial.ComplexResult),
                DecimalMark = Parsed(settings.DecimalMark, initial.DecimalMark),
                DigitSeparator = settings.DigitSeparator,
                BaseMode = Parsed(settings.BaseMode, initial.BaseMode),
                Verify = settings.Verify,
                ComplexRoots = settings.ComplexRoots,
            },
            Regression = Parsed(document.Regression, RegressionModel.Linear),
            Variables = Read(document.Variables),
            Ans = document.Ans ?? string.Empty,
            PreAns = document.PreAns ?? string.Empty,
            Matrices = Read(document.Matrices),
            Vectors = Read(document.Vectors),
            FunctionF = document.FunctionF,
            FunctionG = document.FunctionG,
            StatisticsX = Read(document.StatisticsX),
            StatisticsY = Read(document.StatisticsY),
            StatisticsFrequencies = Read(document.StatisticsFrequencies),
        };
    }

    private static TEnum Parsed<TEnum>(string? text, TEnum fallback)
        where TEnum : struct, Enum
    {
        // Only a name: Enum.TryParse also reads "3", and nothing here writes a number, so a number is not a setting.
        return text is { Length: > 0 } && !char.IsAsciiDigit(text[0]) && text[0] != '-'
            && Enum.TryParse(text, out TEnum value) && Enum.IsDefined(value)
            ? value
            : fallback;
    }

    private static ImmutableArray<string> Read(string[]? texts) => texts is null ? [] : [.. texts.Select(text => text ?? string.Empty)];

    private static ImmutableArray<MatrixSnapshot?> Read(MatrixDocument?[]? matrices)
    {
        return matrices is null
            ? []
            : [.. matrices.Select(matrix => matrix is null ? null : new MatrixSnapshot(matrix.Rows, matrix.Columns, Read(matrix.Entries)))];
    }

    private static MatrixDocument?[] Written(ImmutableArray<MatrixSnapshot?> matrices)
    {
        return [.. matrices.Select(matrix => matrix is null
            ? null
            : new MatrixDocument { Rows = matrix.Rows, Columns = matrix.Columns, Entries = [.. matrix.Entries] })];
    }
}

/// <summary>The stored shape of a session. Public only because the JSON source generator needs it.</summary>
/// <remarks>Every name here is part of the stored format, so a rename is a change to what earlier builds wrote.</remarks>
public sealed class SnapshotDocument
{
    /// <summary>Gets or sets the format version.</summary>
    public int Version { get; set; }

    /// <summary>Gets or sets the application.</summary>
    public string? App { get; set; }

    /// <summary>Gets or sets the settings.</summary>
    public SettingsDocument? Settings { get; set; }

    /// <summary>Gets or sets the regression model.</summary>
    public string? Regression { get; set; }

    /// <summary>Gets or sets the variables A to z.</summary>
    public string[]? Variables { get; set; }

    /// <summary>Gets or sets the last result.</summary>
    public string? Ans { get; set; }

    /// <summary>Gets or sets the result before the last one.</summary>
    public string? PreAns { get; set; }

    /// <summary>Gets or sets MatA to MatD.</summary>
    public MatrixDocument?[]? Matrices { get; set; }

    /// <summary>Gets or sets VctA to VctD.</summary>
    public MatrixDocument?[]? Vectors { get; set; }

    /// <summary>Gets or sets f(x).</summary>
    public string? FunctionF { get; set; }

    /// <summary>Gets or sets g(x).</summary>
    public string? FunctionG { get; set; }

    /// <summary>Gets or sets the x column of the statistics data.</summary>
    public string[]? StatisticsX { get; set; }

    /// <summary>Gets or sets the y column.</summary>
    public string[]? StatisticsY { get; set; }

    /// <summary>Gets or sets the frequency column.</summary>
    public string[]? StatisticsFrequencies { get; set; }
}

/// <summary>The stored shape of the settings. Public only because the JSON source generator needs it.</summary>
public sealed class SettingsDocument
{
    /// <summary>Gets or sets the Input/Output setting.</summary>
    public string? InputOutput { get; set; }

    /// <summary>Gets or sets the angle unit.</summary>
    public string? AngleUnit { get; set; }

    /// <summary>Gets or sets the number format, as <c>Norm1</c>, <c>Fix3</c> or <c>Sci10</c>.</summary>
    public string? NumberFormat { get; set; }

    /// <summary>Gets or sets whether engineering symbols are used.</summary>
    public bool EngineerSymbol { get; set; }

    /// <summary>Gets or sets how fractions are displayed.</summary>
    public string? FractionResult { get; set; }

    /// <summary>Gets or sets how complex results are displayed.</summary>
    public string? ComplexResult { get; set; }

    /// <summary>Gets or sets the decimal mark.</summary>
    public string? DecimalMark { get; set; }

    /// <summary>Gets or sets whether digits are grouped in threes.</summary>
    public bool DigitSeparator { get; set; }

    /// <summary>Gets or sets the Base-N number mode.</summary>
    public string? BaseMode { get; set; }

    /// <summary>Gets or sets whether Verify is on.</summary>
    public bool Verify { get; set; }

    /// <summary>Gets or sets whether a polynomial shows its complex roots.</summary>
    public bool ComplexRoots { get; set; } = true;
}

/// <summary>The stored shape of a matrix or a vector. Public only because the JSON source generator needs it.</summary>
public sealed class MatrixDocument
{
    /// <summary>Gets or sets the rows.</summary>
    public int Rows { get; set; }

    /// <summary>Gets or sets the columns.</summary>
    public int Columns { get; set; }

    /// <summary>Gets or sets the entries, row by row.</summary>
    public string[]? Entries { get; set; }
}

/// <summary>
/// The serializer for the stored session, generated at compile time rather than by reflection.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SnapshotDocument))]
internal sealed partial class SnapshotJsonContext : JsonSerializerContext;
