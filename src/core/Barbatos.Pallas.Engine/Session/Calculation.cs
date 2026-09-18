// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// One calculation of a <see cref="CalculatorSession"/>: its input, result or error, and display.
/// </summary>
public sealed class Calculation
{
    internal Calculation(
        string input,
        CalculatorApp app,
        CalculatorSettings settings,
        CalculatorProfile profile,
        CalculationKind kind,
        Value result,
        Value? second,
        bool? isTrue,
        CalcError? error,
        ImmutableArray<IntegralEstimate> integrals,
        DisplayHints hints = DisplayHints.None)
    {
        Hints = hints;
        Input = input;
        App = app;
        Settings = settings;
        Profile = profile;
        Kind = kind;
        Result = result;
        Second = second;
        IsTrue = isTrue;
        Error = error;
        Integrals = integrals;
        Display = ResultFormatter.Format(this, settings, target: null) ?? new FormattedResult(string.Empty, string.Empty);
    }

    /// <summary>Gets the input, in Canonical Linear Syntax.</summary>
    public string Input { get; }

    /// <summary>Gets the application the calculation ran in.</summary>
    public CalculatorApp App { get; }

    /// <summary>Gets the settings in effect.</summary>
    public CalculatorSettings Settings { get; }

    /// <summary>Gets the profile.</summary>
    public CalculatorProfile Profile { get; }

    /// <summary>Gets what the calculation produced.</summary>
    public CalculationKind Kind { get; }

    /// <summary>Gets the result: the value, the quotient of ÷R, r of Pol(, x of Rec(, or 1 or 0 for Verify.</summary>
    public Value Result { get; }

    /// <summary>Gets the second result: the remainder of ÷R, θ of Pol( or y of Rec(; otherwise <see langword="null"/>.</summary>
    public Value? Second { get; }

    /// <summary>Gets the Verify result; <see langword="null"/> for other calculations.</summary>
    public bool? IsTrue { get; }

    /// <summary>Gets the error, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the calculation produced a result.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Gets the error estimates of the integrals computed (invariant I7 of docs/PRECISION.md).</summary>
    public ImmutableArray<IntegralEstimate> Integrals { get; }

    /// <summary>Gets the result as displayed with <see cref="Settings"/>; empty text for an error.</summary>
    public FormattedResult Display { get; }

    internal DisplayHints Hints { get; }

    /// <inheritdoc/>
    public override string ToString() => Succeeded ? $"{Input} = {Display.Text}" : $"{Input}: {Error!.Value.Kind}";
}
