// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A result as the calculator displays it.
/// </summary>
/// <param name="Text">
/// The display in the output notation of docs/LINEAR-SYNTAX.md §7: <c>13⌟6</c>, <c>45√(3)+10√(2)</c>, <c>1.67×10^-1</c>,
/// <c>2∠45</c>. Several results are separated by <c>, </c> (<c>; </c> with a comma decimal mark).
/// </param>
/// <param name="Latex">The same display in LaTeX, for copy and export.</param>
public sealed record FormattedResult(string Text, string Latex);
