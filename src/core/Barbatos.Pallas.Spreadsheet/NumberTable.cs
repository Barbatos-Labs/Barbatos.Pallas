// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// The Table application (manual pp. 108-113): f(x) and g(x) over a range of x, as a table of rows that can be edited
/// afterwards.
/// </summary>
/// <remarks>
/// <para>
/// A table is generated from Start, End and Step, and holds up to 30 rows with both functions or 45 with one (p. 109);
/// more than that, or a step that would never reach the end, is a Range ERROR (p. 164). Generating a table changes the
/// variable x, which keeps the last value of the column, as the calculator does (p. 109).
/// </para>
/// <para>
/// Every value is calculated through the session, so f(x) and g(x) are the session's defined functions and the settings
/// are the session's. A row calculated again after its x is edited follows the same path, and Verify checks an answer
/// with the engine's own comparison (p. 112).
/// </para>
/// </remarks>
public sealed class NumberTable
{
    /// <summary>The rows of the calculator's table: 45 with one function, 30 with both (p. 109).</summary>
    private const int SingleFunctionRows = 45;
    private const int TwoFunctionRows = 30;

    /// <summary>The rows of the <see cref="CalculatorProfile.Extended"/> profile, which has the memory for more.</summary>
    private const int ExtendedRowLimit = 10_000;

    private readonly CalculatorSession _session;
    private readonly List<TableRow> _rows = [];

    private NumberTable(CalculatorSession session, TableType type, CalcError? error)
    {
        _session = session;
        Type = type;
        Error = error;
    }

    /// <summary>Gets which columns the table has.</summary>
    public TableType Type { get; }

    /// <summary>Gets the rows, in the order the table was generated.</summary>
    public IReadOnlyList<TableRow> Rows => _rows;

    /// <summary>Gets the error the table could not be generated with, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the table was generated.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Gets how many rows the table may have, which the type and the profile decide (p. 109).</summary>
    /// <param name="type">The columns of the table.</param>
    /// <param name="profile">The profile.</param>
    /// <returns>The number of rows.</returns>
    public static int RowLimit(TableType type, CalculatorProfile profile)
    {
        return profile == CalculatorProfile.Extended
            ? ExtendedRowLimit
            : type == TableType.FunctionsFAndG ? TwoFunctionRows : SingleFunctionRows;
    }

    /// <summary>Generates a table of f(x) and g(x) from Start to End in steps of Step (pp. 108-109).</summary>
    /// <param name="session">The session, in the Table application, whose f(x) and g(x) the table shows.</param>
    /// <param name="type">Which columns to fill.</param>
    /// <param name="start">The first x.</param>
    /// <param name="end">The last x the table may reach.</param>
    /// <param name="step">What to add between rows; it must lead from start to end.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The table, or one with a Range ERROR when the settings ask for more rows than the profile allows.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="type"/> is not a defined value.</exception>
    /// <exception cref="ArgumentException">The session is not in the Table application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public static NumberTable Generate(
        CalculatorSession session,
        TableType type,
        Value start,
        Value end,
        Value step,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Not a defined TableType value.");
        }

        if (session.App != CalculatorApp.Table)
        {
            throw new ArgumentException($"A number table belongs to the Table application; the session is in {session.App}.", nameof(session));
        }

        // The rows are counted before any of them is calculated: x = start, start + step, … up to end.
        int limit = RowLimit(type, session.Profile);
        if (Count(start, end, step, limit) is not { } rows)
        {
            return new NumberTable(session, type, new CalcError(CalcErrorKind.RangeError, default));
        }

        NumberTable table = new(session, type, null);
        decimal first = start.ToDecimal();
        decimal by = step.ToDecimal();
        for (int row = 0; row < rows; row++)
        {
            // A table of the Extended profile is thousands of rows, each of them a calculation: the key that cancels it
            // must be heard between rows, not only inside one.
            cancellationToken.ThrowIfCancellationRequested();

            // Every x is start + row·step rather than the one before it plus step, so no row carries the rounding of the rows above it.
            table._rows.Add(table.Row(Value.FromDecimal(first + (row * by)), cancellationToken));
        }

        return table;
    }

    /// <summary>Changes the x of one row and calculates that row again (p. 110).</summary>
    /// <param name="row">The row, counted from 0.</param>
    /// <param name="x">The new x.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no such row.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public void SetX(int row, Value x, CancellationToken cancellationToken = default)
    {
        _ = _rows[row];
        _rows[row] = Row(x, cancellationToken);
    }

    /// <summary>Deletes one row (p. 110).</summary>
    /// <param name="row">The row, counted from 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">There is no such row.</exception>
    public void RemoveRow(int row)
    {
        _rows.RemoveAt(row);
    }

    /// <summary>Checks an answer against the value the table has, as Verify does in the Table application (p. 112).</summary>
    /// <param name="row">The row, counted from 0.</param>
    /// <param name="function">Which column the answer belongs to.</param>
    /// <param name="answer">The answer as it was entered, in Canonical Linear Syntax.</param>
    /// <returns><see langword="true"/> when the answer is the value, or <see langword="null"/> when the column has no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="answer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">There is no such row.</exception>
    public bool? Verify(int row, TableFunction function, string answer)
    {
        ArgumentNullException.ThrowIfNull(answer);
        if (_rows[row].Value(function) is not { Succeeded: true })
        {
            return null;
        }

        // The comparison is the engine's: the calculator takes values within its last digits as equal (p. 76).
        _session.SetVariable(MemoryVariable.X, _rows[row].X.Result);
        CalculatorSettings settings = _session.Settings;
        _session.Settings = settings with { Verify = true };
        try
        {
            Calculation verified = _session.Evaluate((function == TableFunction.F ? "f(x)=" : "g(x)=") + answer);
            return verified.IsTrue;
        }
        finally
        {
            _session.Settings = settings;
        }
    }

    /// <summary>
    /// How many rows x = start, start + step, … has up to end, or <see langword="null"/> for a range that has none or
    /// more than the limit.
    /// </summary>
    /// <remarks>
    /// The range is counted in <see cref="decimal"/>: a table of the calculator is typed, and a value too small or too
    /// large for a decimal is out of the range a table steps through (assumption U26).
    /// </remarks>
    private static int? Count(Value start, Value end, Value step, int limit)
    {
        if (start.Kind != ValueKind.DecimalReal || end.Kind != ValueKind.DecimalReal || step.Kind != ValueKind.DecimalReal)
        {
            return null;
        }

        try
        {
            decimal span = end.ToDecimal() - start.ToDecimal();
            decimal by = step.ToDecimal();
            if (by == 0m || (span != 0m && Math.Sign(span) != Math.Sign(by)))
            {
                return null;
            }

            decimal rows = decimal.Floor(span / by) + 1m;
            return rows >= 1m && rows <= limit ? (int)rows : null;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private TableRow Row(Value x, CancellationToken cancellationToken)
    {
        _session.SetVariable(MemoryVariable.X, x);
        Calculation column = _session.Evaluate("x", cancellationToken);
        Calculation? f = Type == TableType.FunctionG ? null : _session.Evaluate("f(x)", cancellationToken);
        Calculation? g = Type == TableType.FunctionF ? null : _session.Evaluate("g(x)", cancellationToken);
        return new TableRow(column, f, g);
    }
}
