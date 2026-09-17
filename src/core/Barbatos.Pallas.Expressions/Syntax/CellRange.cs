// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A Spreadsheet range such as <c>A1:B5</c>, the argument of <c>Sum(</c> and its siblings.
/// </summary>
public sealed class CellRange : SyntaxNode
{
    /// <summary>Initializes a range.</summary>
    /// <param name="start">The first cell.</param>
    /// <param name="end">The last cell.</param>
    /// <param name="span">Where the range is.</param>
    public CellRange(CellReference start, CellReference end, SourceSpan span = default)
        : base(span)
    {
        Start = NotNull(start, nameof(start));
        End = NotNull(end, nameof(end));
    }

    /// <summary>Gets the first cell.</summary>
    public CellReference Start { get; }

    /// <summary>Gets the last cell.</summary>
    public CellReference End { get; }
}
