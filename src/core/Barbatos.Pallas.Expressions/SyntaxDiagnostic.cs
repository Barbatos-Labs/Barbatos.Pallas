// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The first error found in an expression, and where it is.
/// </summary>
/// <param name="Code">What is wrong.</param>
/// <param name="Span">Where the error is, for placing the cursor; zero-length at the end of the text for a missing operand.</param>
public readonly record struct SyntaxDiagnostic(SyntaxErrorCode Code, SourceSpan Span);
