// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The outcome of evaluating one operation or function: a value, or a calculator error.
/// </summary>
/// <remarks>
/// Errors are returned rather than thrown, so a plugin function reports a Math ERROR the same way the built-in functions
/// do. The engine attaches the source span.
/// </remarks>
public readonly record struct EvalResult
{
    private EvalResult(Value value, CalcErrorKind? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>Gets the value; meaningful only when <see cref="Succeeded"/>.</summary>
    public Value Value { get; }

    /// <summary>Gets the error, or <see langword="null"/> on success.</summary>
    public CalcErrorKind? Error { get; }

    /// <summary>Gets whether the operation produced a value.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Returns a successful result.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static EvalResult Success(Value value) => new(value, null);

    /// <summary>Returns a failed result.</summary>
    /// <param name="error">The calculator error.</param>
    /// <returns>The result.</returns>
    public static EvalResult Failure(CalcErrorKind error) => new(default, error);

    /// <summary>Returns a successful result.</summary>
    /// <param name="value">The value.</param>
    public static implicit operator EvalResult(Value value) => Success(value);

    /// <summary>Returns a successful result; the named alternative to the implicit conversion.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The result.</returns>
    public static EvalResult FromValue(Value value) => Success(value);
}
