// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A function added to the engine by a plugin, such as a beam deflection formula.
/// </summary>
/// <remarks>
/// A function is registered with <see cref="PallasEngineBuilder.AddFunction(IMathFunction)"/>, which adds its name to the
/// vocabulary. The engine checks the number of arguments before calling <see cref="Invoke"/>, and checks the result:
/// a value outside the profile's calculation range becomes a Math ERROR.
/// </remarks>
public interface IMathFunction
{
    /// <summary>Gets the name, arity and availability of the function.</summary>
    FunctionSignature Signature { get; }

    /// <summary>Evaluates the function.</summary>
    /// <param name="arguments">The argument values; their count is within the signature's arity.</param>
    /// <param name="context">The settings and profile of the calculation.</param>
    /// <returns>The value, or a calculator error such as <see cref="CalcErrorKind.MathError"/> for an argument outside the domain.</returns>
    EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context);
}
