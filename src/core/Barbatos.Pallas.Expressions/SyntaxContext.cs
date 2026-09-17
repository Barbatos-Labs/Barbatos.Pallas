// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The circumstances an expression is read in.
/// </summary>
/// <param name="App">The calculator application, which decides context-dependent tokens.</param>
/// <param name="AllowRelations">
/// Whether the relational operators <c>= ≠ &lt; &gt; ≤ ≥</c> may appear, as when Verify is on (manual p. 73).
/// </param>
public readonly record struct SyntaxContext(CalculatorApp App, bool AllowRelations = false)
{
    /// <summary>Gets the context of the Calculate application with Verify off.</summary>
    public static SyntaxContext Calculate => new(CalculatorApp.Calculate);
}
