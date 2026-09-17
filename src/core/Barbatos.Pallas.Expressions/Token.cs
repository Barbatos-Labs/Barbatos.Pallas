// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A token: a kind and where it is. The text is the span of the lexed input; nothing is copied.
/// </summary>
/// <param name="Kind">What the token is.</param>
/// <param name="Span">Where the token is.</param>
/// <param name="Symbol">The vocabulary symbol matched, when <paramref name="Kind"/> is <see cref="TokenKind.Symbol"/>; may be an alias.</param>
public readonly record struct Token(TokenKind Kind, SourceSpan Span, SyntaxSymbol? Symbol = null);
