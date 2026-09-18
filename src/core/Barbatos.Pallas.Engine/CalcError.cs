// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// An error of a calculation: what went wrong and where.
/// </summary>
/// <param name="Kind">The calculator error.</param>
/// <param name="Span">The part of the input the error belongs to, where the calculator places the cursor (manual p. 162).</param>
/// <param name="SyntaxCode">For an error found while parsing, the parser's more specific code; otherwise <see langword="null"/>.</param>
public readonly record struct CalcError(CalcErrorKind Kind, SourceSpan Span, SyntaxErrorCode? SyntaxCode = null);
