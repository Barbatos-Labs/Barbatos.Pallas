// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Calc Settings of the reference calculator (manual pp. 22-25), plus the Base-N number mode and Verify.
/// </summary>
/// <remarks>Every property starts at the calculator's initial setting, marked ◆ in the manual.</remarks>
public sealed record CalculatorSettings
{
    /// <summary>Gets the initial settings.</summary>
    public static CalculatorSettings Initial { get; } = new();

    /// <summary>Gets the Input/Output setting; initially MathI/MathO.</summary>
    public InputOutput InputOutput { get; init; } = InputOutput.MathIMathO;

    /// <summary>Gets the angle unit; initially degrees.</summary>
    public AngleUnit AngleUnit { get; init; } = AngleUnit.Degree;

    /// <summary>Gets the number format; initially Norm 1.</summary>
    public NumberFormat NumberFormat { get; init; } = NumberFormat.Norm1;

    /// <summary>Gets whether results are displayed with engineering symbols (k, M, …); initially off.</summary>
    public bool EngineerSymbol { get; init; }

    /// <summary>Gets how fractions are displayed; initially improper.</summary>
    public FractionResult FractionResult { get; init; } = FractionResult.Improper;

    /// <summary>Gets how complex results are displayed; initially a+bi.</summary>
    public ComplexResult ComplexResult { get; init; } = ComplexResult.Rectangular;

    /// <summary>Gets the decimal mark of displayed results; initially a dot.</summary>
    public DecimalMark DecimalMark { get; init; } = DecimalMark.Dot;

    /// <summary>Gets whether displayed results group digits in threes; initially off.</summary>
    public bool DigitSeparator { get; init; }

    /// <summary>Gets the Base-N number mode; initially decimal.</summary>
    public NumberBase BaseMode { get; init; } = NumberBase.Dec;

    /// <summary>Gets whether Verify is on (manual p. 73): every input must then be an equation or inequality.</summary>
    public bool Verify { get; init; }
}
