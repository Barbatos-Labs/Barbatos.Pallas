// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet.Tests;

/// <summary>
/// The Table application (manual pp. 108-113), against the worked example of the manual.
/// </summary>
public sealed class NumberTableTests
{
    private static Value Number(decimal value) => Value.FromDecimal(value);

    /// <summary>The session of the example of p. 108: f(x) = x² + 1/2 and g(x) = x² − 1/2.</summary>
    private static CalculatorSession Session(CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Sheet.Session(CalculatorApp.Table, profile);
        session.Define(DefinedFunction.F, "x²+1⌟2").Should().BeNull();
        session.Define(DefinedFunction.G, "x²-1⌟2").Should().BeNull();
        return session;
    }

    private static string[] Row(TableRow row)
    {
        return [.. new[] { row.X, row.F, row.G }.Where(cell => cell is not null).Select(cell => cell!.Display.Text)];
    }

    [Fact]
    public void TheExampleOfTheManual()
    {
        // p. 109: x from −1 to 1 in steps of 0.5, with both columns, and x keeps the last value afterwards.
        CalculatorSession session = Session();

        NumberTable table = NumberTable.Generate(session, TableType.FunctionsFAndG, Number(-1), Number(1), Number(0.5m));

        table.Succeeded.Should().BeTrue();
        string[][] expected =
        [
            ["-1", "3⌟2", "1⌟2"],
            ["-1⌟2", "3⌟4", "-1⌟4"],
            ["0", "1⌟2", "-1⌟2"],
            ["1⌟2", "3⌟4", "-1⌟4"],
            ["1", "3⌟2", "1⌟2"],
        ];
        table.Rows.Select(Row).Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        session.GetVariable(MemoryVariable.X).Should().Be(Value.One, "generating a table changes x (p. 109)");
        session.Ans.Should().Be(Value.Zero, "a table calculates for the user, not at their command");
        session.History.Should().BeEmpty();
    }

    [Theory]
    [InlineData(TableType.FunctionF, 2)]
    [InlineData(TableType.FunctionG, 2)]
    [InlineData(TableType.FunctionsFAndG, 3)]
    public void TheColumnsOfEachTableType(TableType type, int columns)
    {
        NumberTable table = NumberTable.Generate(Session(), type, Number(1), Number(2), Number(1));

        Row(table.Rows[0]).Should().HaveCount(columns);
        table.Rows[0].F.Should().Be(type == TableType.FunctionG ? null : table.Rows[0].Value(TableFunction.F));
        table.Rows[0].G.Should().Be(type == TableType.FunctionF ? null : table.Rows[0].Value(TableFunction.G));
        table.Type.Should().Be(type);
    }

    [Theory]
    // p. 109: 30 rows with both columns, 45 with one; p. 164: more is a Range ERROR.
    [InlineData(TableType.FunctionsFAndG, 30, true)]
    [InlineData(TableType.FunctionsFAndG, 31, false)]
    [InlineData(TableType.FunctionF, 45, true)]
    [InlineData(TableType.FunctionF, 46, false)]
    public void TheRowsEachTableTypeAllows(TableType type, int rows, bool allowed)
    {
        NumberTable table = NumberTable.Generate(Session(), type, Number(1), Number(rows), Number(1));

        table.Succeeded.Should().Be(allowed);
        if (allowed)
        {
            table.Rows.Should().HaveCount(rows);
        }
        else
        {
            table.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
            table.Rows.Should().BeEmpty();
        }
    }

    [Theory]
    // A step of 0 never reaches the end, and a step that runs the other way never does either (p. 164).
    [InlineData("1", "3", "0")]
    [InlineData("1", "3", "-1")]
    [InlineData("3", "1", "1")]
    public void ARangeThatLeadsNowhere_IsARangeError(string start, string end, string step)
    {
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(decimal.Parse(start, null)), Number(decimal.Parse(end, null)), Number(decimal.Parse(step, null)));

        table.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }

    [Fact]
    public void ARangeThatEndsWhereItStarts_IsOneRow()
    {
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(2), Number(2), Number(1));

        table.Rows.Should().ContainSingle();
        Row(table.Rows[0]).Should().Equal("2", "9⌟2");
    }

    [Fact]
    public void ARangeOfValuesDecimalCannotHold_IsARangeError()
    {
        // Assumption U26: a table steps through decimals, as they are typed on the calculator.
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Value.FromDouble(1e-20), Number(1), Number(1));

        table.Error!.Value.Kind.Should().Be(CalcErrorKind.RangeError);
    }

    [Fact]
    public void EveryRowStartsFromTheFirstOne()
    {
        // x = start + row·step, so a step of 0.1 does not drift: the tenth row is exactly 1.
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(0), Number(1), Number(0.1m));

        table.Rows.Should().HaveCount(11);
        table.Rows[10].X.Result.ToDecimal().Should().Be(1m);
    }

    [Fact]
    public void ChangingAnXCalculatesThatRowAgain()
    {
        // p. 110: the f(x) and g(x) of the row follow the x that was entered.
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionsFAndG, Number(-1), Number(1), Number(0.5m));

        table.SetX(0, Number(2));

        Row(table.Rows[0]).Should().Equal("2", "9⌟2", "7⌟2");
        Row(table.Rows[1]).Should().Equal("-1⌟2", "3⌟4", "-1⌟4");
    }

    [Fact]
    public void DeletingARow()
    {
        // p. 110.
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(1), Number(3), Number(1));

        table.RemoveRow(1);

        table.Rows.Select(row => row.X.Display.Text).Should().Equal("1", "3");
    }

    [Fact]
    public void AFunctionThatIsNotDefined_IsNotDefinedInEveryRow()
    {
        // p. 163: f(x) used before it is defined.
        CalculatorSession session = Sheet.Session(CalculatorApp.Table);

        NumberTable table = NumberTable.Generate(session, TableType.FunctionF, Number(1), Number(2), Number(1));

        table.Succeeded.Should().BeTrue();
        table.Rows.Should().OnlyContain(row => row.F!.Error!.Value.Kind == CalcErrorKind.NotDefined);
    }

    [Fact]
    public void VerifyChecksAnAnswerAgainstTheValue()
    {
        // p. 112: with Verify on, an f(x) entered on the table screen is checked.
        CalculatorSession session = Session();
        NumberTable table = NumberTable.Generate(session, TableType.FunctionsFAndG, Number(-1), Number(1), Number(0.5m));

        table.Verify(0, TableFunction.F, "1.5").Should().BeTrue();
        table.Verify(0, TableFunction.F, "1.4").Should().BeFalse();
        table.Verify(0, TableFunction.G, "1⌟2").Should().BeTrue();
        session.Settings.Verify.Should().BeFalse("Verify is the application's setting, not something a check turns on");
    }

    [Fact]
    public void AnAnswerThatTakesLongToCalculateCanBeStopped()
    {
        // The answer is the user's expression; a sum of 2,048 terms reaches the step at which the engine looks at its
        // token, and the setting Verify turned on for the check is turned off again.
        CalculatorSession session = Session();
        NumberTable table = NumberTable.Generate(session, TableType.FunctionF, Number(1), Number(2), Number(1));

        table.Invoking(t => t.Verify(0, TableFunction.F, "Σ(x,1,2048)", new CancellationToken(canceled: true))).Should().Throw<OperationCanceledException>();
        session.Settings.Verify.Should().BeFalse();
    }

    [Fact]
    public void VerifyOfAColumnTheTableHasNot()
    {
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(1), Number(2), Number(1));

        table.Verify(0, TableFunction.G, "0.5").Should().BeNull();
    }

    [Fact]
    public void TheExtendedProfileAllowsMoreRows()
    {
        NumberTable.RowLimit(TableType.FunctionsFAndG, CalculatorProfile.Extended).Should().Be(10_000);
        NumberTable.RowLimit(TableType.FunctionF, CalculatorProfile.Standard).Should().Be(45);

        NumberTable table = NumberTable.Generate(Session(CalculatorProfile.Extended), TableType.FunctionsFAndG, Number(1), Number(100), Number(1));

        table.Rows.Should().HaveCount(100);
    }

    [Fact]
    public void ATableNeedsASessionOfItsOwnApplicationAndRowsThatExist()
    {
        CalculatorSession calculate = Sheet.Session(CalculatorApp.Calculate);
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionF, Number(1), Number(2), Number(1));

        FluentActions.Invoking(() => NumberTable.Generate(calculate, TableType.FunctionF, Number(1), Number(2), Number(1)))
            .Should().Throw<ArgumentException>().WithParameterName("session");
        FluentActions.Invoking(() => NumberTable.Generate(null!, TableType.FunctionF, Number(1), Number(2), Number(1)))
            .Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => NumberTable.Generate(Session(), (TableType)7, Number(1), Number(2), Number(1)))
            .Should().Throw<ArgumentOutOfRangeException>().WithParameterName("type");
        table.Invoking(rows => rows.SetX(2, Number(1))).Should().Throw<ArgumentOutOfRangeException>();
        table.Invoking(rows => rows.SetX(-1, Number(1))).Should().Throw<ArgumentOutOfRangeException>();
        table.Invoking(rows => rows.RemoveRow(2)).Should().Throw<ArgumentOutOfRangeException>();
        table.Invoking(rows => rows.RemoveRow(-1)).Should().Throw<ArgumentOutOfRangeException>();
        table.Invoking(rows => rows.Verify(2, TableFunction.F, "1")).Should().Throw<ArgumentOutOfRangeException>();
        table.Invoking(rows => rows.Verify(0, TableFunction.F, null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ARowReadsAsItsValues()
    {
        NumberTable table = NumberTable.Generate(Session(), TableType.FunctionsFAndG, Number(1), Number(1), Number(1));

        table.Rows[0].ToString().Should().Be("1, 3⌟2, 1⌟2");
    }
}
