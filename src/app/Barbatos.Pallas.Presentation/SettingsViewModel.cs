// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Calc Settings screen (manual pp. 22-25), over the settings of a session.
/// </summary>
/// <remarks>
/// The settings are the engine's immutable record; every property here reads that record and writes a new one, so what
/// the screen shows and what the next calculation uses cannot drift apart. Two settings the manual keeps outside those
/// pages are here as well, because this application shows them in the same place: Verify (p. 73) and Complex Roots
/// (p. 118). The Base-N number mode is read-only here - the Base-N application sets it (p. 129).
/// </remarks>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session whose settings are shown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public SettingsViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
    }

    /// <summary>Gets the Input/Output settings to choose from, in the order of the manual (p. 22).</summary>
    public static ImmutableArray<InputOutput> InputOutputOptions { get; } = [.. Enum.GetValues<InputOutput>()];

    /// <summary>Gets the angle units to choose from (p. 23).</summary>
    public static ImmutableArray<AngleUnit> AngleUnitOptions { get; } = [.. Enum.GetValues<AngleUnit>()];

    /// <summary>Gets the fraction results to choose from (p. 24).</summary>
    public static ImmutableArray<FractionResult> FractionResultOptions { get; } = [.. Enum.GetValues<FractionResult>()];

    /// <summary>Gets the complex results to choose from (p. 24).</summary>
    public static ImmutableArray<ComplexResult> ComplexResultOptions { get; } = [.. Enum.GetValues<ComplexResult>()];

    /// <summary>Gets the decimal marks to choose from (p. 25).</summary>
    public static ImmutableArray<DecimalMark> DecimalMarkOptions { get; } = [.. Enum.GetValues<DecimalMark>()];

    /// <summary>Gets every number format to choose from: Norm 1-2, Fix 0-9 and Sci 1-10 (p. 23).</summary>
    /// <remarks>
    /// The calculator asks for the kind and then the digits; one list of the twenty-two settings they reach is the
    /// same choice in one step, and it cannot name a format that does not exist.
    /// </remarks>
    public static ImmutableArray<NumberFormat> NumberFormatOptions { get; } =
    [
        NumberFormat.Norm1,
        NumberFormat.Norm2,
        .. Enumerable.Range(0, 10).Select(NumberFormat.Fix),
        .. Enumerable.Range(1, 10).Select(NumberFormat.Sci),
    ];

    /// <summary>Gets or sets how input and output are written; initially MathI/MathO.</summary>
    public InputOutput InputOutput
    {
        get => _session.Settings.InputOutput;
        set => Apply(_session.Settings with { InputOutput = value });
    }

    /// <summary>Gets or sets the angle unit; initially degrees.</summary>
    public AngleUnit AngleUnit
    {
        get => _session.Settings.AngleUnit;
        set => Apply(_session.Settings with { AngleUnit = value });
    }

    /// <summary>Gets or sets the number format; initially Norm 1.</summary>
    public NumberFormat NumberFormat
    {
        get => _session.Settings.NumberFormat;
        set => Apply(_session.Settings with { NumberFormat = value });
    }

    /// <summary>Gets whether results are displayed with engineering symbols; initially off.</summary>
    public bool EngineerSymbol
    {
        get => _session.Settings.EngineerSymbol;
        set => Apply(_session.Settings with { EngineerSymbol = value });
    }

    /// <summary>Gets or sets how fractions are displayed; initially improper.</summary>
    public FractionResult FractionResult
    {
        get => _session.Settings.FractionResult;
        set => Apply(_session.Settings with { FractionResult = value });
    }

    /// <summary>Gets or sets how complex results are displayed; initially a+bi.</summary>
    public ComplexResult ComplexResult
    {
        get => _session.Settings.ComplexResult;
        set => Apply(_session.Settings with { ComplexResult = value });
    }

    /// <summary>Gets or sets the decimal mark, which also decides the list separator; initially a dot.</summary>
    public DecimalMark DecimalMark
    {
        get => _session.Settings.DecimalMark;
        set => Apply(_session.Settings with { DecimalMark = value });
    }

    /// <summary>Gets or sets whether displayed results group digits in threes; initially off.</summary>
    public bool DigitSeparator
    {
        get => _session.Settings.DigitSeparator;
        set => Apply(_session.Settings with { DigitSeparator = value });
    }

    /// <summary>Gets or sets whether Verify is on (p. 73).</summary>
    public bool Verify
    {
        get => _session.Settings.Verify;
        set => Apply(_session.Settings with { Verify = value });
    }

    /// <summary>Gets or sets whether a polynomial shows its complex roots (p. 118); initially on.</summary>
    public bool ComplexRoots
    {
        get => _session.Settings.ComplexRoots;
        set => Apply(_session.Settings with { ComplexRoots = value });
    }

    /// <summary>Gets the Base-N number mode, which the Base-N application sets rather than this screen (p. 129).</summary>
    public NumberBase BaseMode => _session.Settings.BaseMode;

    /// <summary>Sets the number format from a kind and a digit count, as the screen's two steps give them.</summary>
    /// <param name="kind">Norm, Fix or Sci.</param>
    /// <param name="digits">1-2 for Norm, 0-9 for Fix, 1-10 for Sci.</param>
    /// <returns><see langword="true"/> when the format was set; <see langword="false"/> for a digit count outside its range.</returns>
    /// <remarks>
    /// A number the user types is input, and input does not throw (the engine's rule for errors): a count outside the
    /// range leaves the setting as it was, and the screen says so.
    /// </remarks>
    public bool TrySetNumberFormat(NumberFormatKind kind, int digits)
    {
        if (!NumberFormats.TryCreate(kind, digits, out NumberFormat format))
        {
            return false;
        }

        NumberFormat = format;
        return true;
    }

    /// <summary>Puts every setting back to what the calculator starts with (the values marked ◆, pp. 22-25).</summary>
    /// <remarks>
    /// Reset: Setup Data of p. 33, which is the settings alone: the memories, the defined functions and the data of an
    /// application are cleared by their own commands.
    /// </remarks>
    [RelayCommand]
    public void Reset() => Apply(CalculatorSettings.Initial);

    /// <summary>Tells the screen that the settings changed elsewhere, as a restored session or a new application does.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);

    private void Apply(CalculatorSettings settings)
    {
        _session.Settings = settings;
        Refresh();
    }
}
