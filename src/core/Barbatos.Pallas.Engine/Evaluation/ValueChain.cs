// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A calculation written as a series of steps, where the first error is what comes out.
/// </summary>
/// <remarks>
/// A formula such as (−b ± √(b² − 4ac))/2a is a dozen <see cref="ValueMath"/> calls, each of which may be a Math ERROR.
/// Passing the error of every step down by hand is a conditional per step that nothing ever takes, so the steps run one
/// after another instead: a step that failed hands on 0, and the error it failed with is the error of the whole
/// calculation. Nothing here throws, so the steps after a failure are wasted work and nothing worse.
/// </remarks>
internal sealed class ValueChain
{
    private CalcErrorKind? _error;

    /// <summary>Gets whether every step so far produced a value.</summary>
    public bool Succeeded => _error is null;

    /// <summary>Gets the error of the first step that failed, or <see langword="null"/>.</summary>
    public CalcError? Error => _error is { } kind ? new CalcError(kind, default) : null;

    /// <summary>Takes the value of one step, or 0 from a step that failed.</summary>
    public Value Step(EvalResult result)
    {
        _error ??= result.Error;
        return result.Succeeded ? result.Value : Value.Zero;
    }
}
