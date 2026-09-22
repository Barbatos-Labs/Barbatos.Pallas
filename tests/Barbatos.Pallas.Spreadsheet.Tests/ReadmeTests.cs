// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheSheet()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.Spreadsheet);
        SpreadsheetGrid sheet = new(session);
        CellAddress a1 = new(0, 0);

        sheet.SetConstant(a1, "7×5");
        sheet.SetConstant(new CellAddress(0, 1), "7×6");
        sheet.SetFormula(new CellAddress(1, 0), "A1+7");
        sheet.SetFormula(new CellAddress(0, 3), "Sum(A1:A2)");

        sheet[a1]!.Value.ToDecimal().Should().Be(35m);
        sheet[new CellAddress(0, 1)]!.Value.ToDecimal().Should().Be(42m);
        sheet[new CellAddress(1, 0)]!.Value.ToDecimal().Should().Be(42m);
        sheet[new CellAddress(0, 3)]!.Value.ToDecimal().Should().Be(77m);

        sheet.CopyPaste(new CellAddress(1, 0), new CellAddress(2, 2));
        sheet[new CellAddress(2, 2)]!.Text.Should().Be("=B3+7");

        sheet.Fill("2A1-3", new CellAddress(1, 0), new CellAddress(1, 2));
        sheet[new CellAddress(1, 1)]!.Text.Should().Be("=2A2-3");

        sheet.SetFormula(a1, "A1")!.Value.Kind.Should().Be(CalcErrorKind.CircularError);
    }

    [Fact]
    public void TheNumberTable()
    {
        CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.Table);
        session.Define(DefinedFunction.F, "x²+1⌟2");
        session.Define(DefinedFunction.G, "x²-1⌟2");

        NumberTable table = NumberTable.Generate(
            session, TableType.FunctionsFAndG, Value.FromDecimal(-1m), Value.One, Value.FromDecimal(0.5m));

        table.Rows[0].X.Display.Text.Should().Be("-1");
        table.Rows[0].F!.Display.Text.Should().Be("3⌟2");
        table.SetX(0, Value.FromDecimal(2m));
        table.Verify(0, TableFunction.F, "4.5").Should().BeTrue();
    }
}
