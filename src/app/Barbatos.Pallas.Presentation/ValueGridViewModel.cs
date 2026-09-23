// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One place a value is typed: a matrix entry, a coefficient, a parameter, a datum.
/// </summary>
/// <remarks>
/// What the user types is an expression, not a number - the calculator lets a coefficient be <c>2⌟3</c> or
/// <c>√(2)</c> - so every cell is calculated by the session that owns the grid, without storing anything (Ans and
/// the history belong to what the user calculated, not to filling in a form).
/// </remarks>
public sealed partial class ValueCellViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    internal ValueCellViewModel(CalculatorSession session, string text)
    {
        _session = session;
        _text = text;
        Recalculate();
    }

    /// <summary>Gets or sets what was typed, as Canonical Linear Syntax.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Value))]
    [NotifyPropertyChangedFor(nameof(Error))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorKey))]
    [NotifyPropertyChangedFor(nameof(Display))]
    private string _text;

    /// <summary>Gets what was typed, calculated; zero while it cannot be.</summary>
    public Value Value { get; private set; } = Value.Zero;

    /// <summary>Gets why what was typed is not a value, or <see langword="null"/>.</summary>
    public CalcErrorKind? Error { get; private set; }

    /// <summary>Gets whether what was typed is not a value.</summary>
    public bool HasError => Error is not null;

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    public string? ErrorKey => Error is { } kind ? "error." + kind : null;

    /// <summary>Gets the value as the calculator shows it.</summary>
    public string Display { get; private set; } = "0";

    /// <summary>Puts a value into the cell, as reading a stored matrix does.</summary>
    /// <param name="value">The value.</param>
    public void Set(Value value)
    {
        Value = value;
        Error = null;
        Display = Text = Formatted(value);
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ErrorKey));
        OnPropertyChanged(nameof(Display));
    }

    partial void OnTextChanged(string value) => Recalculate();

    private void Recalculate()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            // A cell left empty is a zero, which is what an empty entry of the calculator's own forms is.
            Value = Value.Zero;
            Error = null;
            Display = "0";
            return;
        }

        Calculation calculation = _session.Evaluate(Text);
        Value = calculation.Succeeded ? calculation.Result : Value.Zero;
        Error = calculation.Error?.Kind;
        Display = calculation.Succeeded ? calculation.Display.Text : Text;
    }

    private string Formatted(Value value) => PallasEngine.Format(value, _session.Settings, _session.Profile)?.Text ?? value.ToString();
}

/// <summary>
/// A grid of values the user fills in: a matrix, a vector, the coefficients of an equation, the parameters of a
/// distribution, a column of data.
/// </summary>
/// <remarks>
/// Every form of the calculator is a grid of this kind, so there is one of these rather than one per application.
/// The grid holds text and values; what they mean is the application's business.
/// </remarks>
public sealed partial class ValueGridViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates a grid.</summary>
    /// <param name="session">The session that calculates what is typed.</param>
    /// <param name="rows">How many rows.</param>
    /// <param name="columns">How many columns.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public ValueGridViewModel(CalculatorSession session, int rows, int columns)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        _session = session;
        _rows = rows;
        _columns = columns;
        _cells = Build(rows, columns);
    }

    /// <summary>Gets how many rows the grid has.</summary>
    [ObservableProperty]
    private int _rows;

    /// <summary>Gets how many columns the grid has.</summary>
    [ObservableProperty]
    private int _columns;

    /// <summary>Gets the cells, row by row.</summary>
    [ObservableProperty]
    private ImmutableArray<ValueCellViewModel> _cells;

    /// <summary>Gets the cell at a place.</summary>
    /// <param name="row">The row.</param>
    /// <param name="column">The column.</param>
    /// <returns>The cell.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The place is not in the grid.</exception>
    public ValueCellViewModel this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
            return Cells[(row * Columns) + column];
        }
    }

    /// <summary>Gets whether any cell holds something that is not a value.</summary>
    public bool HasError => Cells.Any(cell => cell.HasError);

    /// <summary>Changes the size of the grid, keeping what fits.</summary>
    /// <param name="rows">How many rows.</param>
    /// <param name="columns">How many columns.</param>
    /// <exception cref="ArgumentOutOfRangeException">A size is not positive.</exception>
    public void Resize(int rows, int columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        if (rows == Rows && columns == Columns)
        {
            return;
        }

        ImmutableArray<ValueCellViewModel> kept = Cells;
        int wasColumns = Columns;
        int wasRows = Rows;
        ImmutableArray<ValueCellViewModel> cells = Build(rows, columns);
        for (int row = 0; row < Math.Min(rows, wasRows); row++)
        {
            for (int column = 0; column < Math.Min(columns, wasColumns); column++)
            {
                cells[(row * columns) + column].Text = kept[(row * wasColumns) + column].Text;
            }
        }

        Rows = rows;
        Columns = columns;
        Cells = cells;
    }

    /// <summary>Returns what the grid holds.</summary>
    /// <returns>The values, row by row.</returns>
    public Value[,] ToArray()
    {
        Value[,] values = new Value[Rows, Columns];
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                values[row, column] = this[row, column].Value;
            }
        }

        return values;
    }

    /// <summary>Returns what one column holds.</summary>
    /// <param name="column">The column.</param>
    /// <returns>Its values, top to bottom.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The column is not in the grid.</exception>
    public ImmutableArray<Value> Column(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        return [.. Enumerable.Range(0, Rows).Select(row => this[row, column].Value)];
    }

    /// <summary>Puts values into the grid, one for one.</summary>
    /// <param name="values">The values, row by row.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public void Set(Value[,] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Resize(values.GetLength(0), values.GetLength(1));
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                this[row, column].Set(values[row, column]);
            }
        }
    }

    /// <summary>Empties every cell.</summary>
    public void Clear()
    {
        foreach (ValueCellViewModel cell in Cells)
        {
            cell.Text = "0";
        }
    }

    private ImmutableArray<ValueCellViewModel> Build(int rows, int columns) =>
        [.. Enumerable.Range(0, rows * columns).Select(_ => new ValueCellViewModel(_session, "0"))];
}
