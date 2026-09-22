// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// What the engine does with the cell references of a Spreadsheet formula (manual pp. 102, 105); the grid itself is
/// Barbatos.Pallas.Spreadsheet.
/// </summary>
public sealed class SpreadsheetCellTests
{
    /// <summary>A sheet where A1 is 1, A2 is 2, …, B1 is 10, B2 is 20, … and everything else is empty.</summary>
    private static EvalResult Cell(CellAddress address)
    {
        return address.Column switch
        {
            0 => Value.FromDecimal(address.Row + 1),
            1 => Value.FromDecimal(10 * (address.Row + 1)),
            2 => EvalResult.Failure(CalcErrorKind.MathError),
            _ => Value.Zero,
        };
    }

    private static CalculatorSession Session(bool cells = true, CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Spreadsheet, profile);
        session.CellValues = cells ? Cell : null;
        return session;
    }

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("$A$1", 0, 0)]
    [InlineData("$A1", 0, 0)]
    [InlineData("A$1", 0, 0)]
    [InlineData("e45", 4, 44)]
    [InlineData("B12", 1, 11)]
    public void TheCellsOfAName(string name, int column, int row)
    {
        CellAddress.TryParse(name, out CellAddress address).Should().BeTrue();

        address.Should().Be(new CellAddress(column, row));
        address.ToString().Should().Be(name.Replace("$", string.Empty, StringComparison.Ordinal).ToUpperInvariant());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("A0")]
    [InlineData("1A")]
    [InlineData("A1A")]
    [InlineData("$")]
    [InlineData("$$1")]
    public void WhatIsNotACellName(string? name)
    {
        CellAddress.TryParse(name, out CellAddress address).Should().BeFalse();

        address.Should().Be(default(CellAddress));
    }

    [Fact]
    public void ACellMovesAndKnowsWhetherItIsInsideAGrid()
    {
        CellAddress address = new(1, 2);

        address.Offset(1, -1).Should().Be(new CellAddress(2, 1));
        address.Offset(-2, 0).Should().Be(new CellAddress(-1, 2));
        address.IsInside(5, 45).Should().BeTrue();
        address.Offset(-2, 0).IsInside(5, 45).Should().BeFalse();
        address.Offset(0, 43).IsInside(5, 45).Should().BeFalse();
        new CellAddress(26, 0).ToString().Should().Be("?1", "there is no letter for that column");
    }

    [Fact]
    public void AFormulaReadsTheCellsOfTheApplication()
    {
        Session().Calculate("A1+7").Display.Text.Should().Be("8");
        Session().Calculate("A3×B2").Display.Text.Should().Be("60");
        Session().Calculate("$A$1+A$2+$A3").Display.Text.Should().Be("6");
    }

    [Fact]
    public void ASheetThatIsNotThere_ReadsEveryCellAsZero()
    {
        // Assumption U26: an empty cell is 0, and a session with no sheet attached is empty everywhere.
        Session(cells: false).Calculate("A1+7").Display.Text.Should().Be("7");
        Session(cells: false).Calculate("Sum(A1:E45)").Display.Text.Should().Be("0");
    }

    [Fact]
    public void TheRangeCommandsOfTheManual()
    {
        // p. 105: the four commands over A1:A4, which hold 1, 2, 3 and 4.
        Session().Calculate("Sum(A1:A4)").Display.Text.Should().Be("10");
        Session().Calculate("Min(A1:A4)").Display.Text.Should().Be("1");
        Session().Calculate("Max(A1:A4)").Display.Text.Should().Be("4");
        Session().Calculate("Mean(A1:A4)").Display.Text.Should().Be("5⌟2", "a cell value is displayed as the settings say, MathI/MathO here");
        Session().Calculate("Sum(A1:B2)").Display.Text.Should().Be("33", "a range may be a rectangle");
        Session().Calculate("Sum(A4:A1)").Display.Text.Should().Be("10", "the corners may be given either way round");
    }

    [Fact]
    public void ARangeThatReadsACellInError()
    {
        // Column C fails: the error of the cell is the error of the range.
        Session().Calculate("Sum(A1:C1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().Calculate("C1").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ARangeCommandTakesARangeAndNothingElse()
    {
        Calculator.Error("Sum(A1)", CalculatorApp.Spreadsheet).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Sum(A1:A2,A3)", CalculatorApp.Spreadsheet).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Sum(1:2)", CalculatorApp.Spreadsheet).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("A1:A2", CalculatorApp.Spreadsheet).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("2+A1:A2", CalculatorApp.Spreadsheet).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void ACellReferenceBelongsToTheSpreadsheetApplication()
    {
        // Elsewhere A1 is A×1, the variable times one (docs/LINEAR-SYNTAX.md §4).
        CalculatorSession calculate = Calculator.Session();
        calculate.SetVariable(MemoryVariable.A, Value.FromDecimal(3));

        calculate.Calculate("A1+7").Display.Text.Should().Be("10");
        calculate.Calculate("Sum(A1:A3)").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError, "the range commands are the Spreadsheet's");
    }

    [Fact]
    public void ADerivativeOfACellIsZero()
    {
        // A cell does not depend on the variable of a d/dx, whatever it holds.
        Session().Calculate("d/dx(A2×x,3)").Display.Text.Should().Be("2");
        Session().Calculate("d/dx(A2,3)").Display.Text.Should().Be("0");
    }

    [Fact]
    public void ALongRangeCountsAgainstTheBudget()
    {
        CalculatorSession session = Session(profile: CalculatorProfile.Extended);
        session.Budget = new EngineBudget(MaxIterations: 10, session.Budget.Timeout);

        session.Calculate("Sum(A1:E45)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut);
    }

    [Fact]
    public void EvaluateStoresNothing()
    {
        CalculatorSession session = Session();

        Calculation calculation = session.Evaluate("A1+7");

        calculation.Display.Text.Should().Be("8");
        session.Ans.Should().Be(Value.Zero);
        session.History.Should().BeEmpty();
        session.Calculate("A1+7");
        session.Ans.Should().Be(calculation.Result);
        session.History.Should().ContainSingle();
        session.Invoking(s => s.Evaluate(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EvaluateStoresNothingOfAVerifyOrADivisionWithRemainder()
    {
        CalculatorSession session = Calculator.Session(settings: settings => settings with { Verify = true });

        session.Evaluate("1+1=2").IsTrue.Should().BeTrue();
        session.Ans.Should().Be(Value.Zero);

        CalculatorSession remainder = Calculator.Session();
        remainder.Evaluate("5÷R2").Display.Text.Should().Be("2, R=1");
        remainder.Ans.Should().Be(Value.Zero);
        remainder.GetVariable(MemoryVariable.E).Should().Be(Value.Zero);
    }
}
