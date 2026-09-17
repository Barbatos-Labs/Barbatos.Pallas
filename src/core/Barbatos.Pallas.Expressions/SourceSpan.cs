// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A range of characters in the text that was parsed.
/// </summary>
/// <param name="Start">The index of the first character.</param>
/// <param name="Length">The number of characters; 0 for a position, such as the end of the text.</param>
/// <remarks>
/// Every token, syntax node and diagnostic carries a span, because the calculator places the cursor at the error
/// (manual p. 162). Indexes refer to <see cref="ParseResult.Text"/>, the text after Unicode normalization.
/// </remarks>
public readonly record struct SourceSpan(int Start, int Length)
{
    /// <summary>Gets the index just past the last character.</summary>
    public int End => Start + Length;

    /// <summary>Creates the span that starts at <paramref name="start"/> and ends at <paramref name="end"/>.</summary>
    /// <param name="start">The span containing the first character.</param>
    /// <param name="end">The span containing the last character.</param>
    /// <returns>A span covering both.</returns>
    public static SourceSpan Covering(SourceSpan start, SourceSpan end) => new(start.Start, end.End - start.Start);
}
