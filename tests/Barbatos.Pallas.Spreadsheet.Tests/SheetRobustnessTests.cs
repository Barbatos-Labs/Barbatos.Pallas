// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using CsCheck;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// What a sheet and a table promise for any input at all: a value or an error the calculator has a name for, the same
/// values however often the sheet is calculated, and never an exception.
/// </summary>
public sealed class SheetRobustnessTests
{
    /// <summary>The cells of a small sheet, A1 to C3, which is room enough for every kind of reference and cycle.</summary>
    private static readonly Gen<CellAddress> Address = Gen.Select(Gen.Int[0, 2], Gen.Int[0, 2]).Select(pair => new CellAddress(pair.Item1, pair.Item2));

    /// <summary>What a user enters: a number, a calculation of other cells, or a range of them.</summary>
    private static readonly Gen<string> Input = Gen.OneOf(
        Gen.Int[-99, 99].Select(number => number.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        Gen.Select(Address, Address).Select(pair => $"{pair.Item1}+{pair.Item2}"),
        Gen.Select(Address, Address).Select(pair => $"{pair.Item1}×{pair.Item2}-1"),
        Gen.Select(Address, Address).Select(pair => $"Sum({pair.Item1}:{pair.Item2})"),
        Gen.Select(Address, Address).Select(pair => $"Mean({pair.Item1}:{pair.Item2})"),
        Gen.Select(Address, Address).Select(pair => $"{pair.Item1}÷{pair.Item2}"));

    [Fact]
    public void AnySheetEndsInValuesOrNamedErrors()
    {
        Gen.Select(Address, Input, Gen.Bool).Array[1, 9]
            .Sample(
                entries =>
                {
                    SpreadsheetGrid grid = Sheet.Grid();
                    foreach ((CellAddress address, string input, bool formula) in entries)
                    {
                        _ = formula ? grid.SetFormula(address, input) : grid.SetConstant(address, input);
                    }

                    foreach (SpreadsheetCell cell in grid.Cells)
                    {
                        if (!cell.Succeeded)
                        {
                            cell.Error!.Value.Kind.Should().BeOneOf(
                                CalcErrorKind.MathError,
                                CalcErrorKind.CircularError,
                                CalcErrorKind.SyntaxError,
                                CalcErrorKind.TimeOut,
                                CalcErrorKind.MemoryError);
                        }
                    }

                    grid.UsedBytes.Should().BeLessThanOrEqualTo(grid.Capacity);
                },
                iter: 500);
    }

    [Fact]
    public void CalculatingASheetAgainChangesNothing()
    {
        // A sheet that is calculated twice must show what it showed: a formula reads cells, never the pass it is in.
        Gen.Select(Address, Input, Gen.Bool).Array[1, 9]
            .Sample(
                entries =>
                {
                    SpreadsheetGrid grid = Sheet.Grid();
                    foreach ((CellAddress address, string input, bool formula) in entries)
                    {
                        _ = formula ? grid.SetFormula(address, input) : grid.SetConstant(address, input);
                    }

                    string[] before = [.. grid.Cells.OrderBy(cell => cell.Address.Row).ThenBy(cell => cell.Address.Column).Select(Text)];
                    grid.Recalculate();
                    grid.Recalculate();
                    string[] after = [.. grid.Cells.OrderBy(cell => cell.Address.Row).ThenBy(cell => cell.Address.Column).Select(Text)];

                    after.Should().Equal(before);
                },
                iter: 300);
    }

    [Fact]
    public void ALongTableIsCancelled()
    {
        CalculatorSession session = Sheet.Session(CalculatorApp.Table, CalculatorProfile.Extended);
        session.Define(DefinedFunction.F, "x²");
        using CancellationTokenSource cancelled = new();
        cancelled.Cancel();

        FluentActions.Invoking(() => NumberTable.Generate(
                session,
                TableType.FunctionF,
                Value.FromDecimal(1m),
                Value.FromDecimal(10_000m),
                Value.One,
                cancelled.Token))
            .Should().Throw<OperationCanceledException>();
    }

    [Fact]
    public void ATableOfTheExtendedProfileIsGeneratedWithinItsBudget()
    {
        CalculatorSession session = Sheet.Session(CalculatorApp.Table, CalculatorProfile.Extended);
        session.Define(DefinedFunction.F, "x²");

        NumberTable table = NumberTable.Generate(session, TableType.FunctionF, Value.One, Value.FromDecimal(10_000m), Value.One);

        table.Succeeded.Should().BeTrue();
        table.Rows.Should().HaveCount(10_000);
        table.Rows[9_999].F!.Result.ToDecimal().Should().Be(100_000_000m);
    }

    private static string Text(SpreadsheetCell cell)
    {
        return cell.Succeeded
            ? cell.Address + "=" + PallasEngine.Format(cell.Value, CalculatorSettings.Initial)!.Text
            : cell.Address + "=" + cell.Error!.Value.Kind;
    }
}
