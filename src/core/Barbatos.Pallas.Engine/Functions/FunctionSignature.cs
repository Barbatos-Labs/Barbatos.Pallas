// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The name, arity and availability of a plugin function.
/// </summary>
public sealed class FunctionSignature
{
    /// <summary>Initializes a signature.</summary>
    /// <param name="name">The name as written, ending with <c>(</c>: <c>beam(</c>.</param>
    /// <param name="minimumArity">The fewest arguments; at least 1.</param>
    /// <param name="maximumArity">The most arguments; at least <paramref name="minimumArity"/>.</param>
    /// <param name="applications">The applications the function is available in; empty for all.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid function name.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An arity is out of range.</exception>
    public FunctionSignature(string name, int minimumArity, int maximumArity, params ReadOnlySpan<CalculatorApp> applications)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minimumArity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumArity, minimumArity);

        Symbol = SyntaxSymbol.CreateName(name, SymbolKind.Function, applications);
        MinimumArity = minimumArity;
        MaximumArity = maximumArity;
    }

    /// <summary>Gets the name as written.</summary>
    public string Name => Symbol.Text;

    /// <summary>Gets the fewest arguments.</summary>
    public int MinimumArity { get; }

    /// <summary>Gets the most arguments.</summary>
    public int MaximumArity { get; }

    /// <summary>Gets the applications the function is available in; empty for all.</summary>
    public ImmutableArray<CalculatorApp> Applications => Symbol.Applications;

    internal SyntaxSymbol Symbol { get; }
}
