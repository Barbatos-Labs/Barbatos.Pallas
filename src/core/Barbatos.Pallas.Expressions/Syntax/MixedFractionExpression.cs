// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A mixed fraction <c>a⌟b⌟c</c>, meaning a + b/c: <c>1⌟1⌟2</c> is 1½.
/// </summary>
public sealed class MixedFractionExpression : SyntaxNode
{
    /// <summary>Initializes a mixed fraction.</summary>
    /// <param name="whole">The whole part.</param>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <param name="span">Where the fraction is.</param>
    public MixedFractionExpression(SyntaxNode whole, SyntaxNode numerator, SyntaxNode denominator, SourceSpan span = default)
        : base(span)
    {
        Whole = NotNull(whole, nameof(whole));
        Numerator = NotNull(numerator, nameof(numerator));
        Denominator = NotNull(denominator, nameof(denominator));
    }

    /// <summary>Gets the whole part.</summary>
    public SyntaxNode Whole { get; }

    /// <summary>Gets the numerator.</summary>
    public SyntaxNode Numerator { get; }

    /// <summary>Gets the denominator.</summary>
    public SyntaxNode Denominator { get; }
}
