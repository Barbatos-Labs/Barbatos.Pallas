// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// Short ways to build a sheet or a table, so that a test reads like the manual's examples.
/// </summary>
internal static class Sheet
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().Build();

    public static CalculatorSession Session(CalculatorApp app = CalculatorApp.Spreadsheet, CalculatorProfile profile = CalculatorProfile.Standard)
    {
        return Engine.CreateSession(app, profile, randomSeed: 880);
    }

    /// <summary>A sheet on its own session.</summary>
    public static SpreadsheetGrid Grid(CalculatorProfile profile = CalculatorProfile.Standard) => new(Session(profile: profile));

    /// <summary>The cell of a name such as <c>A1</c>.</summary>
    public static CellAddress At(string name)
    {
        CellAddress.TryParse(name, out CellAddress address).Should().BeTrue("'{0}' is a cell", name);
        return address;
    }

    /// <summary>What the cell shows, with the initial settings.</summary>
    public static string Display(this SpreadsheetGrid grid, string name)
    {
        SpreadsheetCell? cell = grid[At(name)];
        cell.Should().NotBeNull("{0} should have content", name);
        return cell!.Succeeded
            ? PallasEngine.Format(cell.Value, CalculatorSettings.Initial)!.Text
            : cell.Error!.Value.Kind.ToString();
    }

    /// <summary>The example of pp. 102-106: A1 = 7×5, A2 = 7×6, A3 = A2+7 as constants, and B1 = A1+7 as a formula.</summary>
    public static SpreadsheetGrid Example()
    {
        SpreadsheetGrid grid = Grid();
        grid.SetConstant(At("A1"), "7×5");
        grid.SetConstant(At("A2"), "7×6");
        grid.SetConstant(At("A3"), "A2+7");
        grid.SetFormula(At("B1"), "A1+7");
        return grid;
    }
}
