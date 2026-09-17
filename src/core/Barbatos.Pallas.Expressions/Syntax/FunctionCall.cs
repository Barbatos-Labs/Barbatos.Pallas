// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A function with its arguments: <c>sin(30)</c>, <c>log(2,16)</c>, <c>Σ(x+1,1,5)</c>.
/// </summary>
/// <remarks>The parser does not check the number of arguments; which counts a function accepts is the engine's business.</remarks>
public sealed class FunctionCall : SyntaxNode
{
    /// <summary>Initializes a call.</summary>
    /// <param name="function">The function; an alias is replaced by its canonical symbol.</param>
    /// <param name="arguments">The arguments, at least one.</param>
    /// <param name="span">Where the call is, closing parenthesis included when written.</param>
    /// <exception cref="ArgumentException"><paramref name="function"/> is not a function, or there are no arguments.</exception>
    public FunctionCall(SyntaxSymbol function, ImmutableArray<SyntaxNode> arguments, SourceSpan span = default)
        : base(span)
    {
        Function = NotNull(function, nameof(function)).Canonical;
        if (Function.Kind != SymbolKind.Function)
        {
            throw new ArgumentException($"'{function.Text}' is {function.Kind}, not a function.", nameof(function));
        }

        if (arguments.IsDefaultOrEmpty || arguments.Any(argument => argument is null))
        {
            throw new ArgumentException("A function call has at least one argument, and none is null.", nameof(arguments));
        }

        Arguments = arguments;
    }

    /// <summary>Gets the canonical function symbol, whose text ends with <c>(</c>.</summary>
    public SyntaxSymbol Function { get; }

    /// <summary>Gets the arguments.</summary>
    public ImmutableArray<SyntaxNode> Arguments { get; }
}
