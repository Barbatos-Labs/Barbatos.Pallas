// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// The Spreadsheet application (manual pp. 100-107): a grid of cells, each a constant or a formula, calculated through
/// a <see cref="CalculatorSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// A constant is fixed when it is entered: <c>A2+7</c> without a leading <c>=</c> is calculated once and keeps that
/// value. A formula, written with a leading <c>=</c>, is calculated again whenever the sheet is, which is after every
/// change while Auto Calc is on (p. 107).
/// </para>
/// <para>
/// A formula is calculated where it stands: the cells it refers to are calculated first, and a cell that is asked for
/// while it is being calculated is a Circular ERROR (p. 165). Nothing is calculated twice in one pass, and an empty
/// cell reads as 0 (assumption U26).
/// </para>
/// <para>
/// The grid takes the cell references of the session while it exists: a session has one sheet, as a calculator does.
/// </para>
/// </remarks>
public sealed class SpreadsheetGrid
{
    /// <summary>The columns and rows of the calculator's sheet, A1 to E45 (p. 100).</summary>
    private const int StandardColumns = 5;
    private const int StandardRows = 45;

    /// <summary>The bytes the calculator's sheet holds in all (p. 100).</summary>
    private const int StandardCapacity = 1700;

    /// <summary>The bytes one input may have before it is confirmed (p. 101).</summary>
    private const int InputBytes = 49;

    /// <summary>Beyond ten significant digits a constant is stored with ten, as the calculator converts it (p. 101).</summary>
    private const int ConstantDigits = 10;

    private readonly CalculatorSession _session;
    private readonly Dictionary<CellAddress, SpreadsheetCell> _cells = [];
    private readonly Dictionary<CellAddress, EvalResult> _values = [];
    private readonly HashSet<CellAddress> _calculating = [];

    /// <summary>Creates an empty sheet on a session of the Spreadsheet application.</summary>
    /// <param name="session">The session, which the sheet calculates through and reads cell references from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The session is not in the Spreadsheet application.</exception>
    public SpreadsheetGrid(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.App != CalculatorApp.Spreadsheet)
        {
            throw new ArgumentException($"A sheet belongs to the Spreadsheet application; the session is in {session.App}.", nameof(session));
        }

        _session = session;
        session.CellValues = Read;
        Columns = StandardColumns;
        Rows = session.Profile == CalculatorProfile.Standard ? StandardRows : ExtendedRows;
        Capacity = session.Profile == CalculatorProfile.Standard ? StandardCapacity : int.MaxValue;
    }

    /// <summary>The rows of the <see cref="CalculatorProfile.Extended"/> profile, which the lexer's two-digit row numbers allow.</summary>
    internal const int ExtendedRows = 99;

    /// <summary>Gets the columns, A to E.</summary>
    public int Columns { get; }

    /// <summary>Gets the rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the bytes the sheet holds in all (p. 100).</summary>
    public int Capacity { get; }

    /// <summary>Gets or sets whether a change calculates the sheet again; initially on (p. 107).</summary>
    public bool AutoCalculate { get; set; } = true;

    /// <summary>Gets the cells that have content, in no particular order.</summary>
    public IReadOnlyCollection<SpreadsheetCell> Cells => _cells.Values;

    /// <summary>Gets the bytes the cells use, of <see cref="Capacity"/> (p. 101).</summary>
    public int UsedBytes => _cells.Values.Sum(Bytes);

    /// <summary>Gets a cell, or <see langword="null"/> when it is empty.</summary>
    /// <param name="address">The cell.</param>
    /// <returns>The cell, or <see langword="null"/>.</returns>
    public SpreadsheetCell? this[CellAddress address] => _cells.GetValueOrDefault(address);

    /// <summary>Enters a constant: an expression without a leading <c>=</c>, calculated once and then fixed (p. 101).</summary>
    /// <param name="address">The cell.</param>
    /// <param name="input">The input, in Canonical Linear Syntax.</param>
    /// <returns>The error of the input, or <see langword="null"/>; the value of a cell in error is 0.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is outside the sheet.</exception>
    public CalcError? SetConstant(CellAddress address, string input) => Set(address, input, formula: false);

    /// <summary>Enters a formula, the text after the <c>=</c>, which is calculated again whenever the sheet is (p. 101).</summary>
    /// <param name="address">The cell.</param>
    /// <param name="formula">The formula without its leading <c>=</c>, in Canonical Linear Syntax.</param>
    /// <returns>The error of the formula, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formula"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is outside the sheet.</exception>
    public CalcError? SetFormula(CellAddress address, string formula) => Set(address, formula, formula: true);

    /// <summary>Clears one cell (p. 104).</summary>
    /// <param name="address">The cell.</param>
    public void Clear(CellAddress address)
    {
        if (_cells.Remove(address))
        {
            Recalculate();
        }
    }

    /// <summary>Clears every cell (p. 104).</summary>
    public void ClearAll()
    {
        _cells.Clear();
    }

    /// <summary>Copies a cell and pastes it, moving the relative references by the distance between the two (p. 103).</summary>
    /// <param name="from">The cell to copy.</param>
    /// <param name="to">Where to paste it.</param>
    /// <returns>The error of the pasted content, or <see langword="null"/>; nothing happens when <paramref name="from"/> is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A cell is outside the sheet.</exception>
    public CalcError? CopyPaste(CellAddress from, CellAddress to)
    {
        Require(from);
        SpreadsheetCell? cell = _cells.GetValueOrDefault(from);
        return cell is null
            ? null
            : Set(to, CellFormula.Shift(cell.Input, to.Column - from.Column, to.Row - from.Row, Columns, Rows, _session.Engine.Vocabulary), cell.IsFormula);
    }

    /// <summary>Cuts a cell and pastes it, leaving every reference where it is (p. 104).</summary>
    /// <param name="from">The cell to cut.</param>
    /// <param name="to">Where to paste it.</param>
    /// <returns>The error of the pasted content, or <see langword="null"/>; nothing happens when <paramref name="from"/> is empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A cell is outside the sheet.</exception>
    public CalcError? CutPaste(CellAddress from, CellAddress to)
    {
        Require(from);
        SpreadsheetCell? cell = _cells.GetValueOrDefault(from);
        if (cell is null)
        {
            return null;
        }

        _cells.Remove(from);
        return Set(to, cell.Input, cell.IsFormula);
    }

    /// <summary>Enters one formula in every cell of a range, its relative references taken from the first cell (p. 106).</summary>
    /// <param name="formula">The formula without its leading <c>=</c>, as it belongs in the first cell of the range.</param>
    /// <param name="start">The first cell of the range.</param>
    /// <param name="end">The last cell of the range.</param>
    /// <returns>The first error the range produced, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="formula"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A cell of the range is outside the sheet.</exception>
    public CalcError? Fill(string formula, CellAddress start, CellAddress end) => Fill(formula, start, end, asFormula: true);

    /// <summary>Enters one constant in every cell of a range, its relative references taken from the first cell (p. 106).</summary>
    /// <param name="input">The input, as it belongs in the first cell of the range.</param>
    /// <param name="start">The first cell of the range.</param>
    /// <param name="end">The last cell of the range.</param>
    /// <returns>The first error the range produced, or <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A cell of the range is outside the sheet.</exception>
    public CalcError? FillValue(string input, CellAddress start, CellAddress end) => Fill(input, start, end, asFormula: false);

    /// <summary>Calculates every formula of the sheet again, as the Recalculate command does (p. 107).</summary>
    public void Recalculate()
    {
        _values.Clear();
        foreach (CellAddress address in _cells.Keys.ToArray())
        {
            _ = Read(address);
        }

        foreach ((CellAddress address, EvalResult result) in _values)
        {
            _cells[address] = Cell(address, _cells[address].Input, isFormula: true, result);
        }
    }

    /// <summary>The value a formula reads from a cell, calculating it first when it is a formula itself.</summary>
    private EvalResult Read(CellAddress address)
    {
        if (_values.TryGetValue(address, out EvalResult known))
        {
            return known;
        }

        if (!_cells.TryGetValue(address, out SpreadsheetCell? cell))
        {
            return Value.Zero;
        }

        if (!cell.IsFormula)
        {
            return cell.Error is { } error ? EvalResult.Failure(error.Kind) : cell.Value;
        }

        if (!_calculating.Add(address))
        {
            // p. 165: a cell asked for while it is being calculated refers to itself, however far around.
            return EvalResult.Failure(CalcErrorKind.CircularError);
        }

        Calculation calculation = _session.Evaluate(cell.Input);
        _calculating.Remove(address);
        EvalResult result = calculation.Succeeded ? calculation.Result : EvalResult.Failure(calculation.Error!.Value.Kind);
        _values[address] = result;
        return result;
    }

    private CalcError? Set(CellAddress address, string input, bool formula)
    {
        ArgumentNullException.ThrowIfNull(input);
        Require(address);
        if (CellFormula.Bytes(input, _session.Engine.Vocabulary) > InputBytes)
        {
            return new CalcError(CalcErrorKind.MemoryError, new SourceSpan(0, input.Length));
        }

        SpreadsheetCell entered = formula
            ? Cell(address, input, isFormula: true, EvalResult.Failure(CalcErrorKind.SyntaxError))
            : Constant(address, input);

        // p. 100: the sheet holds so many bytes, whatever they are spent on.
        SpreadsheetCell? replaced = _cells.GetValueOrDefault(address);
        if (UsedBytes - (replaced is null ? 0 : Bytes(replaced)) + Bytes(entered) > Capacity)
        {
            return new CalcError(CalcErrorKind.MemoryError, new SourceSpan(0, input.Length));
        }

        _cells[address] = entered;
        if (AutoCalculate || !formula)
        {
            Recalculate();
        }

        return _cells[address].Error;
    }

    /// <summary>A constant: calculated once, and stored with ten significant digits when it was typed with more (p. 101).</summary>
    private SpreadsheetCell Constant(CellAddress address, string input)
    {
        Calculation calculation = _session.Evaluate(input);
        if (calculation.Succeeded && CellFormula.SignificantDigits(input) > ConstantDigits)
        {
            string rounded = PallasEngine.Format(calculation.Result, Rounding, _session.Profile)!.Text;
            calculation = _session.Evaluate(rounded);
        }

        EvalResult result = calculation.Succeeded ? calculation.Result : EvalResult.Failure(calculation.Error!.Value.Kind);
        return Cell(address, input, isFormula: false, result);
    }

    private CalcError? Fill(string input, CellAddress start, CellAddress end, bool asFormula)
    {
        ArgumentNullException.ThrowIfNull(input);
        Require(start);
        Require(end);
        // p. 106: the relative references belong to the cell at the top left of the range, wherever its corners were given.
        CellAddress corner = new(Math.Min(start.Column, end.Column), Math.Min(start.Row, end.Row));
        CellAddress last = new(Math.Max(start.Column, end.Column), Math.Max(start.Row, end.Row));
        CalcError? first = null;
        for (int row = corner.Row; row <= last.Row; row++)
        {
            for (int column = corner.Column; column <= last.Column; column++)
            {
                string shifted = CellFormula.Shift(input, column - corner.Column, row - corner.Row, Columns, Rows, _session.Engine.Vocabulary);
                first ??= Set(new CellAddress(column, row), shifted, asFormula);
            }
        }

        return first;
    }

    private static SpreadsheetCell Cell(CellAddress address, string input, bool isFormula, EvalResult result)
    {
        return new SpreadsheetCell(
            address,
            input,
            isFormula,
            result.Succeeded ? result.Value : Value.Zero,
            result.Succeeded ? null : new CalcError(result.Error!.Value, new SourceSpan(0, input.Length)));
    }

    private int Bytes(SpreadsheetCell cell)
    {
        return cell.IsFormula
            ? CellFormula.Bytes(cell.Input, _session.Engine.Vocabulary) + CellFormula.FormulaBytes
            : CellFormula.ConstantBytes;
    }

    private void Require(CellAddress address)
    {
        if (!address.IsInside(Columns, Rows))
        {
            throw new ArgumentOutOfRangeException(nameof(address), address, $"The sheet holds A1 to {new CellAddress(Columns - 1, Rows - 1)}.");
        }
    }

    /// <summary>Norm 1 with decimal output: the ten significant digits the calculator converts a long constant to (p. 101).</summary>
    private static CalculatorSettings Rounding { get; } = CalculatorSettings.Initial with { InputOutput = InputOutput.MathIDecimalO };
}
