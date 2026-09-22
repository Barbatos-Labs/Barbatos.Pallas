// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// One row of a <see cref="NumberTable"/>: an x and the functions of it the table shows (manual pp. 108-109).
/// </summary>
public sealed class TableRow
{
    internal TableRow(Calculation x, Calculation? f, Calculation? g)
    {
        X = x;
        F = f;
        G = g;
    }

    /// <summary>Gets the x of the row.</summary>
    public Calculation X { get; }

    /// <summary>Gets f(x), or <see langword="null"/> when the table has no f(x) column.</summary>
    public Calculation? F { get; }

    /// <summary>Gets g(x), or <see langword="null"/> when the table has no g(x) column.</summary>
    public Calculation? G { get; }

    /// <summary>Returns the value of one of the columns, or <see langword="null"/> when the table has not got it.</summary>
    /// <param name="function">Which function.</param>
    /// <returns>The calculation, or <see langword="null"/>.</returns>
    public Calculation? Value(TableFunction function) => function == TableFunction.F ? F : G;

    /// <inheritdoc/>
    public override string ToString() => string.Join(", ", new[] { X, F, G }.Where(cell => cell is not null).Select(cell => cell!.Display.Text));
}
