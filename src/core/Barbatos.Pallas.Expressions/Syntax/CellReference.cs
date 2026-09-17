// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A Spreadsheet cell reference: <c>A1</c>, <c>$A1</c>, <c>A$1</c> or <c>$A$1</c>.
/// </summary>
public sealed class CellReference : SyntaxNode
{
    /// <summary>Initializes a reference.</summary>
    /// <param name="text">The reference as written.</param>
    /// <param name="span">Where the reference is.</param>
    public CellReference(string text, SourceSpan span = default)
        : base(span)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        Text = text;
    }

    /// <summary>Gets the reference as written, including any <c>$</c>.</summary>
    public string Text { get; }
}
