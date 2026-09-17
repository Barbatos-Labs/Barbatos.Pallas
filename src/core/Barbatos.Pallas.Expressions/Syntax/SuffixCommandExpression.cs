// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A named command written after a value: an engineering symbol (<c>999_k</c>, level 3) or a unit conversion
/// (<c>5cm▶in</c>, level 6).
/// </summary>
public sealed class SuffixCommandExpression : SyntaxNode
{
    /// <summary>Initializes a suffix command.</summary>
    /// <param name="operand">The value.</param>
    /// <param name="command">The engineering symbol or unit conversion; an alias is replaced by its canonical symbol.</param>
    /// <param name="span">Where the value and command are.</param>
    /// <exception cref="ArgumentException"><paramref name="command"/> is neither an engineering symbol nor a unit conversion.</exception>
    public SuffixCommandExpression(SyntaxNode operand, SyntaxSymbol command, SourceSpan span = default)
        : base(span)
    {
        Operand = NotNull(operand, nameof(operand));
        Command = NotNull(command, nameof(command)).Canonical;
        if (Command.Kind is not (SymbolKind.EngineeringSymbol or SymbolKind.UnitConversion))
        {
            throw new ArgumentException($"'{command.Text}' is {command.Kind}, not an engineering symbol or unit conversion.", nameof(command));
        }
    }

    /// <summary>Gets the value.</summary>
    public SyntaxNode Operand { get; }

    /// <summary>Gets the canonical command symbol.</summary>
    public SyntaxSymbol Command { get; }
}
