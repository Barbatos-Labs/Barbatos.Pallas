// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// One cell of the Spreadsheet application, by column and row (manual pp. 100-101).
/// </summary>
/// <param name="Column">The column, counted from 0: A is 0.</param>
/// <param name="Row">The row, counted from 0: row 1 is 0.</param>
/// <remarks>
/// Both are counted from 0, as the grid holds them; <see cref="ToString"/> writes the calculator's name, <c>A1</c>. A
/// reference may be written with a dollar sign before the column, the row or both (<c>$A$1</c>), which says what a
/// paste leaves alone; the dollar signs belong to the formula, not to the cell, so they are not part of this.
/// </remarks>
public readonly record struct CellAddress(int Column, int Row)
{
    /// <summary>Reads a cell name, with or without dollar signs.</summary>
    /// <param name="text">The name, such as <c>A1</c>, <c>$A1</c> or <c>a45</c>.</param>
    /// <param name="address">The address, on success.</param>
    /// <returns><see langword="true"/> when the text is a column letter and a row number of 1 or more.</returns>
    public static bool TryParse(string? text, out CellAddress address)
    {
        address = default;
        ReadOnlySpan<char> rest = text.AsSpan();
        if (rest.Length > 0 && rest[0] == '$')
        {
            rest = rest[1..];
        }

        if (rest.Length < 2 || !char.IsAsciiLetter(rest[0]))
        {
            return false;
        }

        int column = char.ToUpperInvariant(rest[0]) - 'A';
        rest = rest[1..];
        if (rest[0] == '$')
        {
            rest = rest[1..];
        }

        if (!int.TryParse(rest, NumberStyles.None, CultureInfo.InvariantCulture, out int row) || row < 1)
        {
            return false;
        }

        address = new CellAddress(column, row - 1);
        return true;
    }

    /// <summary>Returns the cell this many columns and rows away, which may be outside any grid.</summary>
    /// <param name="columns">Columns to the right; negative to the left.</param>
    /// <param name="rows">Rows down; negative up.</param>
    /// <returns>The cell.</returns>
    public CellAddress Offset(int columns, int rows) => new(Column + columns, Row + rows);

    /// <summary>Gets whether the cell is inside a grid of this size.</summary>
    /// <param name="columns">The number of columns.</param>
    /// <param name="rows">The number of rows.</param>
    /// <returns><see langword="true"/> when the cell is one of them.</returns>
    public bool IsInside(int columns, int rows) => Column >= 0 && Column < columns && Row >= 0 && Row < rows;

    /// <inheritdoc/>
    public override string ToString()
    {
        return Column is >= 0 and < 26
            ? string.Create(CultureInfo.InvariantCulture, $"{(char)('A' + Column)}{Row + 1}")
            : string.Create(CultureInfo.InvariantCulture, $"?{Row + 1}");
    }
}
