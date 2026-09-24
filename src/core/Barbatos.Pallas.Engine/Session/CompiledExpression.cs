// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// An expression in x, read and bound once and calculated at as many values of x as a graph or a table needs.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CalculatorSession.Evaluate"/> reads, binds and compiles its input on every call, which is what a
/// calculation typed on the line needs and what a graph of two thousand points cannot afford. An expression compiled
/// by <see cref="CalculatorSession.Compile(string)"/> calculates exactly as the session would with the variable x holding the
/// value, under the application and the settings in effect when it was compiled, and it stores nothing: neither x nor
/// Ans. The other variables and memories are read from the session as they are at each calculation.
/// </para>
/// <para>
/// Like the session it belongs to, an instance is used from one thread at a time: a calculation that draws a random
/// number draws it from the session's generator.
/// </para>
/// </remarks>
public sealed class CompiledExpression
{
    private readonly CalculatorSession _session;
    private readonly CompiledProgram? _program;
    private readonly DisplayHints _hints;

    internal CompiledExpression(
        CalculatorSession session,
        string input,
        CalculatorApp app,
        CalculatorSettings settings,
        CompiledProgram? program,
        DisplayHints hints,
        CalcError? error)
    {
        _session = session;
        _program = program;
        _hints = hints;
        Input = input;
        App = app;
        Settings = settings;
        Error = error;
    }

    /// <summary>Gets the input, in Canonical Linear Syntax.</summary>
    public string Input { get; }

    /// <summary>Gets the application the expression was compiled in.</summary>
    public CalculatorApp App { get; }

    /// <summary>Gets the settings it calculates with: those in effect when it was compiled.</summary>
    public CalculatorSettings Settings { get; }

    /// <summary>Gets the error that kept the input from compiling, with the span the calculator would put the cursor at, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the input compiled.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Calculates the expression with x holding a value.</summary>
    /// <param name="x">The value of x.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The calculation, displayed with <see cref="Settings"/>: its result or its error, or the error of compiling.</returns>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public Calculation Evaluate(Value x, CancellationToken cancellationToken = default)
    {
        if (_program is null)
        {
            return new Calculation(Input, App, Settings, _session.Profile, CalculationKind.Value, default, null, null, Error, []);
        }

        EvalResult result = _session.RunCompiled(_program, App, Settings, x, cancellationToken, out EvaluationContext context, out SourceSpan errorSpan);
        return result.Succeeded
            ? new Calculation(Input, App, Settings, _session.Profile, CalculationKind.Value, result.Value, null, null, null, [.. context.Integrals], _hints)
            : new Calculation(Input, App, Settings, _session.Profile, CalculationKind.Value, default, null, null, new CalcError(result.Error!.Value, errorSpan), []);
    }

    /// <summary>Returns the value x holds when the expression is calculated at a <see cref="double"/>.</summary>
    /// <param name="x">The <see cref="double"/>.</param>
    /// <returns>
    /// The value, as any result becomes one: to 15 significant digits, and within the range of the profile, so that
    /// 10⁻¹⁰⁰ is 0 in <see cref="CalculatorProfile.Standard"/>; <see langword="null"/> where the calculator holds no
    /// value, beyond its range or for a <see cref="double"/> that is not a number.
    /// </returns>
    /// <remarks>
    /// A graph places its points at doubles, and names the x of a root with this: the x the engine calculated with,
    /// not the double it was given.
    /// </remarks>
    public Value? ValueOf(double x)
    {
        EvalResult entered = ValueMath.Real(x, null, _session.Profile);
        return entered.Succeeded ? entered.Value : null;
    }

    /// <summary>Calculates the expression at an x given as a <see cref="double"/>, for drawing it.</summary>
    /// <param name="x">The value of x; it becomes the value <see cref="ValueOf"/> says.</param>
    /// <param name="y">The real result as a <see cref="double"/>; 0 when there is none.</param>
    /// <returns>Whether the expression has a real value at <paramref name="x"/>: not when it is an error there, or complex.</returns>
    /// <remarks>
    /// A point of a graph is a position on the screen, not a result: this skips what <see cref="Evaluate"/> builds for
    /// a display, the exact forms and the text, and hands back the number. The calculation itself is the same.
    /// </remarks>
    public bool TryEvaluate(double x, out double y)
    {
        y = 0d;
        if (_program is null || ValueOf(x) is not { } value)
        {
            return false;
        }

        EvalResult result = _session.RunCompiled(_program, App, Settings, value, CancellationToken.None, out _, out _);
        if (!result.Succeeded || !result.Value.IsReal)
        {
            return false;
        }

        y = result.Value.ToDouble();
        return true;
    }

    /// <summary>Compiles the derivative of the expression in x, <c>d/dx(</c>expression<c>,x)</c>, in the same application and settings.</summary>
    /// <returns>The derivative; one that failed to compile when the expression did, or when it cannot be differentiated.</returns>
    /// <remarks>
    /// The engine differentiates the bound expression rather than taking a difference quotient (docs/PRECISION.md), so
    /// the derivative of sin x at π/2 is exactly 0. Where the derivative changes sign is where the expression has an
    /// extremum, which a search along the curve itself could only place to about half the digits of its value.
    /// </remarks>
    public CompiledExpression Derivative() =>
        _program is null
            ? this
            : _session.Compile("d/dx(" + Input + ",x)", App, Settings);
}
