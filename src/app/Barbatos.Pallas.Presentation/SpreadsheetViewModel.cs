// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Spreadsheet;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One cell as the screen shows it: where it is, what was typed into it, and what it came to.
/// </summary>
/// <param name="Address">Where the cell is.</param>
/// <param name="Input">What was typed, with the leading <c>=</c> of a formula.</param>
/// <param name="Text">What it came to, or nothing when the cell is empty.</param>
/// <param name="ErrorKey">The localization key of its error, or <see langword="null"/>.</param>
public sealed record SheetCell(CellAddress Address, string Input, string Text, string? ErrorKey);

/// <summary>
/// The Spreadsheet screen (manual pp. 102-106): a sheet of constants and formulas, and the cell being edited.
/// </summary>
/// <remarks>
/// The sheet itself is the Spreadsheet package; the screen holds which cell is selected, what is being typed into
/// it, and a view of the whole sheet for the grid to draw. Everything else - recalculation, references, the byte
/// capacity, a cell that reads itself - belongs to the sheet.
/// </remarks>
public sealed partial class SpreadsheetViewModel : ObservableObject
{
    private readonly CalculatorSession _session;
    private readonly SessionWork _work;
    private readonly SpreadsheetGrid _sheet;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session the sheet calculates through.</param>
    /// <param name="work">The session's work, shared by its screens; <see langword="null"/> to calculate where asked.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public SpreadsheetViewModel(CalculatorSession session, SessionWork? work = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _work = work ?? SessionWork.Immediate;
        _sheet = new SpreadsheetGrid(session);
        _cells = Read();
    }

    /// <summary>Gets how many columns the sheet has (A to E).</summary>
    public int Columns => _sheet.Columns;

    /// <summary>Gets how many rows the sheet has.</summary>
    public int Rows => _sheet.Rows;

    /// <summary>Gets the whole sheet, row by row, for the grid to draw.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsedBytes))]
    private ImmutableArray<SheetCell> _cells;

    /// <summary>Gets or sets which cell is selected.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Editing))]
    private CellAddress _selected = new(0, 0);

    /// <summary>Gets or sets what is being typed into the selected cell.</summary>
    [ObservableProperty]
    private string _input = string.Empty;

    /// <summary>Gets the localization key of the last error, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets or sets whether the sheet calculates again after every change (Auto Calc, p. 104).</summary>
    public bool AutoCalculate
    {
        get => _sheet.AutoCalculate;
        set
        {
            _sheet.AutoCalculate = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets how many of the sheet's bytes are used (p. 101).</summary>
    public int UsedBytes => _sheet.UsedBytes;

    /// <summary>Gets what the selected cell holds.</summary>
    public SheetCell Editing => Cells[(Selected.Row * Columns) + Selected.Column];

    /// <summary>Puts what is being typed into the selected cell.</summary>
    /// <remarks>Text that starts with <c>=</c> is a formula, as it is on the calculator (p. 102).</remarks>
    [RelayCommand]
    public void Commit()
    {
        string text = Input;
        CellAddress address = Selected;
        Change(token => text.StartsWith('=') ? _sheet.SetFormula(address, text[1..], token) : _sheet.SetConstant(address, text, token));
    }

    /// <summary>Empties the selected cell.</summary>
    [RelayCommand]
    public void ClearCell()
    {
        CellAddress address = Selected;
        Change(token =>
        {
            _sheet.Clear(address, token);
            return null;
        });
    }

    /// <summary>Empties the whole sheet.</summary>
    [RelayCommand]
    public void ClearAll() => Change(_ =>
    {
        _sheet.ClearAll();
        return null;
    });

    /// <summary>Calculates the sheet again (p. 104); what the last change said stays said.</summary>
    [RelayCommand]
    public void Recalculate() => _work.Start(_sheet.Recalculate, Refresh);

    /// <summary>Copies what one cell holds into another, shifting its references (p. 105).</summary>
    /// <param name="from">The cell to copy.</param>
    /// <param name="to">Where to put it.</param>
    /// <remarks>What went wrong, if anything, is the <see cref="ErrorKey"/> once it is done.</remarks>
    public void Copy(CellAddress from, CellAddress to) => Change(token => _sheet.CopyPaste(from, to, token));

    /// <summary>Fills a range with a formula or a value (p. 105).</summary>
    /// <param name="text">What to fill with; a leading <c>=</c> makes it a formula.</param>
    /// <param name="start">The first cell of the range.</param>
    /// <param name="end">The last cell of the range.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <remarks>What went wrong, if anything, is the <see cref="ErrorKey"/> once it is done.</remarks>
    public void Fill(string text, CellAddress start, CellAddress end)
    {
        ArgumentNullException.ThrowIfNull(text);
        Change(token => text.StartsWith('=') ? _sheet.Fill(text[1..], start, end, token) : _sheet.FillValue(text, start, end, token));
    }

    /// <summary>Changes the sheet as a work of the session, and shows what the change said and the sheet it left.</summary>
    /// <remarks>
    /// The sheet calculates through the session, and a formula of the user's may take as long as its budget allows;
    /// the sheet is changed only by a work of the session, and read only once it is done. A change stopped by AC leaves
    /// the sheet as it was, which is what the screen still shows.
    /// </remarks>
    private void Change(Func<CancellationToken, CalcError?> change) => _work.Start(change, error =>
    {
        ErrorKey = error is { } fault ? "error." + fault.Kind : null;
        Refresh();
    });

    partial void OnSelectedChanged(CellAddress value) => Input = Editing.Input;

    private void Refresh()
    {
        Cells = Read();
        OnPropertyChanged(nameof(Editing));

        // The cells are new ones, so the grid is told again which of them is selected.
        OnPropertyChanged(nameof(Selected));
    }

    private ImmutableArray<SheetCell> Read()
    {
        SheetCell[] cells = new SheetCell[_sheet.Rows * _sheet.Columns];
        for (int row = 0; row < _sheet.Rows; row++)
        {
            for (int column = 0; column < _sheet.Columns; column++)
            {
                CellAddress address = new(column, row);
                SpreadsheetCell? cell = _sheet[address];
                cells[(row * _sheet.Columns) + column] = new SheetCell(
                    address,
                    cell?.Text ?? string.Empty,
                    cell is null ? string.Empty : cell.Succeeded ? Formatted(cell) : string.Empty,
                    cell?.Error is { } error ? "error." + error.Kind : null);
            }
        }

        return [.. cells];
    }

    private string Formatted(SpreadsheetCell cell) => PallasEngine.Format(cell.Value, _session.Settings, _session.Profile)?.Text ?? cell.Value.ToString();
}
