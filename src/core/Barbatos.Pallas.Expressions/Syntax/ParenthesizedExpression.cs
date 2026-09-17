// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// An expression in parentheses, kept so that printing reproduces what was typed: <c>(3×6)&lt;(2+6)×2</c>.
/// </summary>
/// <remarks><see cref="SyntaxEquivalence"/> looks through parentheses, so <c>6÷2(1+2)</c> and <c>6÷(2(1+2))</c> are equivalent.</remarks>
public sealed class ParenthesizedExpression : SyntaxNode
{
    /// <summary>Initializes a parenthesized expression.</summary>
    /// <param name="inner">The expression inside.</param>
    /// <param name="span">Where the parentheses are; the closing one may have been omitted at the end of the text.</param>
    public ParenthesizedExpression(SyntaxNode inner, SourceSpan span = default)
        : base(span)
    {
        Inner = NotNull(inner, nameof(inner));
    }

    /// <summary>Gets the expression inside.</summary>
    public SyntaxNode Inner { get; }
}
