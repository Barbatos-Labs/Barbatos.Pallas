// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// What a <see cref="Token"/> is.
/// </summary>
public enum TokenKind
{
    /// <summary>The end of the text. The lexer returns it repeatedly once reached.</summary>
    End = 0,

    /// <summary>A character that starts no token, such as <c>q</c> or <c>#</c> on its own.</summary>
    Invalid = 1,

    /// <summary>A number: <c>12</c>, <c>1.25</c>, <c>.5</c>, <c>0.(3)</c>; in Base-N, digits <c>0</c>-<c>9</c> and <c>A</c>-<c>F</c>.</summary>
    Number = 2,

    /// <summary>A Base-N prefix <c>d</c>, <c>h</c>, <c>b</c> or <c>o</c> directly before a number.</summary>
    BasePrefix = 3,

    /// <summary>A Spreadsheet cell reference: <c>A1</c>, <c>$A1</c>, <c>A$1</c>, <c>$A$1</c>.</summary>
    CellReference = 4,

    /// <summary>A symbol of the vocabulary; <see cref="Token.Symbol"/> says which.</summary>
    Symbol = 5,
}
