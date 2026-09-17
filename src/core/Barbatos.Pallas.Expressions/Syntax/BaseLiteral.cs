// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A Base-N literal with an explicit base: <c>d10</c>, <c>h1F</c>, <c>b101</c>, <c>o17</c> (priority level 5).
/// </summary>
public sealed class BaseLiteral : SyntaxNode
{
    /// <summary>Initializes a prefixed literal.</summary>
    /// <param name="numberBase">The base the prefix names.</param>
    /// <param name="digits">The digits after the prefix.</param>
    /// <param name="span">Where the prefix and digits are.</param>
    public BaseLiteral(NumberBase numberBase, NumberLiteral digits, SourceSpan span = default)
        : base(span)
    {
        Base = numberBase;
        Digits = NotNull(digits, nameof(digits));
    }

    /// <summary>Gets the base.</summary>
    public NumberBase Base { get; }

    /// <summary>Gets the digits.</summary>
    public NumberLiteral Digits { get; }
}
