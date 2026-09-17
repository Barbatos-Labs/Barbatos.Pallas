// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A number as written: <c>12</c>, <c>1.25</c>, <c>.5</c>, <c>3.(021)</c>, or Base-N digits such as <c>1F</c>.
/// </summary>
public sealed class NumberLiteral : SyntaxNode
{
    /// <summary>Initializes a literal.</summary>
    /// <param name="text">The digits as written.</param>
    /// <param name="span">Where the literal is.</param>
    public NumberLiteral(string text, SourceSpan span = default)
        : base(span)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>Gets the digits as written.</summary>
    public string Text { get; }

    /// <summary>Gets a value indicating whether the literal has a recurring part, as <c>0.(3)</c> does.</summary>
    public bool IsRecurring => Text.Contains('(', StringComparison.Ordinal);
}
