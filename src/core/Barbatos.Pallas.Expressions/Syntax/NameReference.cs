// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A named value: a constant (<c>π</c>, <c>@h</c>), a variable (<c>A</c>, <c>x</c>), a memory (<c>Ans</c>), a matrix, a
/// vector or a statistic.
/// </summary>
public sealed class NameReference : SyntaxNode
{
    /// <summary>Initializes a reference.</summary>
    /// <param name="symbol">The name; an alias is replaced by its canonical symbol.</param>
    /// <param name="span">Where the name is.</param>
    /// <exception cref="ArgumentException"><paramref name="symbol"/> is not a value: a function, engineering symbol, unit conversion or operator.</exception>
    public NameReference(SyntaxSymbol symbol, SourceSpan span = default)
        : base(span)
    {
        Symbol = NotNull(symbol, nameof(symbol)).Canonical;
        if (Symbol.Kind is not (SymbolKind.Constant or SymbolKind.ScientificConstant or SymbolKind.Variable or SymbolKind.Memory
            or SymbolKind.MatrixVariable or SymbolKind.VectorVariable or SymbolKind.StatisticsVariable))
        {
            throw new ArgumentException($"'{symbol.Text}' is {symbol.Kind}, not a value.", nameof(symbol));
        }
    }

    /// <summary>Gets the canonical symbol.</summary>
    public SyntaxSymbol Symbol { get; }
}
