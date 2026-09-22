// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>What a cell reference in a formula reads: one cell, or a range through one of the commands of p. 105.</summary>
internal enum RangeAggregate
{
    /// <summary>One cell, <c>A1</c>.</summary>
    Cell,

    /// <summary><c>Min(</c>.</summary>
    Minimum,

    /// <summary><c>Max(</c>.</summary>
    Maximum,

    /// <summary><c>Mean(</c>.</summary>
    Mean,

    /// <summary><c>Sum(</c>.</summary>
    Sum,
}

/// <summary>The cells one instruction reads: a single cell when <paramref name="Start"/> and <paramref name="End"/> are equal.</summary>
/// <param name="Start">The first cell of the range.</param>
/// <param name="End">The last cell of the range.</param>
/// <param name="Aggregate">What the formula does with them.</param>
internal readonly record struct CellSelection(CellAddress Start, CellAddress End, RangeAggregate Aggregate);

/// <summary>
/// Reads the cells of the Spreadsheet application for the evaluator (manual pp. 102, 105).
/// </summary>
/// <remarks>
/// The values come from the application through a callback, because the grid, its dependencies and its recalculation
/// are Barbatos.Pallas.Spreadsheet's work; a session with no grid attached reads every cell as 0, which is what an empty
/// cell is (assumption U26). Each cell of a range counts against the budget, and against the count of Mean.
/// </remarks>
internal static class SpreadsheetCells
{
    public static EvalResult Read(CellSelection selection, Func<CellAddress, EvalResult>? cells, EvaluationContext context)
    {
        if (selection.Aggregate == RangeAggregate.Cell)
        {
            return Cell(selection.Start, cells);
        }

        // The corners may be given either way round: Sum(A3:A1) is Sum(A1:A3).
        int firstColumn = Math.Min(selection.Start.Column, selection.End.Column);
        int lastColumn = Math.Max(selection.Start.Column, selection.End.Column);
        int firstRow = Math.Min(selection.Start.Row, selection.End.Row);
        int lastRow = Math.Max(selection.Start.Row, selection.End.Row);

        ValueChain chain = new();
        Value total = Value.Zero;
        Value extreme = Value.Zero;
        int count = 0;
        for (int row = firstRow; row <= lastRow; row++)
        {
            for (int column = firstColumn; column <= lastColumn; column++)
            {
                if (!context.TryIterate())
                {
                    return EvalResult.Failure(CalcErrorKind.TimeOut);
                }

                Value value = chain.Step(Cell(new CellAddress(column, row), cells));
                total = chain.Step(ValueMath.Add(total, value, context));
                bool replace = count == 0
                    || (selection.Aggregate == RangeAggregate.Minimum && RealOrder.Instance.Compare(value, extreme) < 0)
                    || (selection.Aggregate == RangeAggregate.Maximum && RealOrder.Instance.Compare(value, extreme) > 0);
                extreme = replace ? value : extreme;
                count++;
            }
        }

        Value result = selection.Aggregate switch
        {
            RangeAggregate.Sum => total,
            RangeAggregate.Mean => chain.Step(ValueMath.Divide(total, Value.FromDecimal(count), context)),
            _ => extreme,
        };

        return chain.Succeeded ? result : EvalResult.Failure(chain.Error!.Value.Kind);
    }

    private static EvalResult Cell(CellAddress address, Func<CellAddress, EvalResult>? cells)
    {
        return cells is null ? Value.Zero : cells(address);
    }
}
