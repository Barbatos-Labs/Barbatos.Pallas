// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// One cell of a <see cref="SpreadsheetGrid"/>: a constant, whose value was fixed when it was entered, or a formula,
/// which is calculated again whenever the sheet is (manual p. 101).
/// </summary>
public sealed class SpreadsheetCell
{
    internal SpreadsheetCell(CellAddress address, string input, bool isFormula, Value value, CalcError? error)
    {
        Address = address;
        Input = input;
        IsFormula = isFormula;
        Value = value;
        Error = error;
    }

    /// <summary>Gets where the cell is.</summary>
    public CellAddress Address { get; }

    /// <summary>Gets the input as it was entered, in Canonical Linear Syntax and without the leading <c>=</c> of a formula.</summary>
    public string Input { get; }

    /// <summary>Gets whether the cell holds a formula; a constant holds the value it was entered with.</summary>
    public bool IsFormula { get; }

    /// <summary>Gets the text the calculator's edit box shows: <c>=A1+7</c> for a formula, <c>7×5</c> for a constant.</summary>
    public string Text => IsFormula ? "=" + Input : Input;

    /// <summary>Gets the value; 0 when the cell is in error.</summary>
    public Value Value { get; }

    /// <summary>Gets the error the cell is in, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the cell has a value.</summary>
    public bool Succeeded => Error is null;

    /// <inheritdoc/>
    public override string ToString() => Address + ": " + Text;
}
