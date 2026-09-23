# Barbatos.Pallas.Spreadsheet

The Spreadsheet and Table applications of Barbatos.Pallas: a grid of cells with relative and absolute references,
calculated in dependency order with circular-reference detection, and number tables of f(x) and g(x).

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.
>
> **Not a package of its own.** It ships inside
> [Barbatos.Pallas.DependencyInjection](https://www.nuget.org/packages/Barbatos.Pallas.DependencyInjection), which
> carries its assembly: reference that package to use it.

## The sheet

A cell holds a constant or a formula. A constant is calculated once, when it is entered, and keeps that value; a
formula, entered without its leading `=`, is calculated again whenever the sheet is, which is after every change while
Auto Calc is on. A formula is calculated where it stands: the cells it reads are calculated first, nothing twice, an
empty cell reads as 0, and a cell asked for while it is being calculated is a Circular ERROR.

```csharp
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;

CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.Spreadsheet);
SpreadsheetGrid sheet = new(session);
CellAddress A1 = new(0, 0);

sheet.SetConstant(A1, "7×5");                                 // 35, fixed when entered
sheet.SetConstant(new CellAddress(0, 1), "7×6");              // A2 = 42
sheet.SetFormula(new CellAddress(1, 0), "A1+7");              // B1 = 42, and follows A1
sheet.SetFormula(new CellAddress(0, 3), "Sum(A1:A2)");        // A4 = 77

sheet.CopyPaste(new CellAddress(1, 0), new CellAddress(2, 2));
sheet[new CellAddress(2, 2)]!.Text;                           // "=B3+7": the relative reference moved with the paste

sheet.Fill("2A1-3", new CellAddress(1, 0), new CellAddress(1, 2));
sheet[new CellAddress(1, 1)]!.Text;                           // "=2A2-3": a fill takes its references from the first cell

sheet.SetFormula(A1, "A1")!.Value.Kind;                       // CircularError
```

`Min(`, `Max(`, `Mean(` and `Sum(` read a range, `A1:A3`. A `$` before the column, the row or both holds that part of a
reference where it is when the cell is pasted; a cut and paste holds all of them. The sheet is A1 to E45 and holds
1,700 bytes, a constant costing 14 and a formula what was typed plus 15.

## The number table

A table is f(x) and g(x) over a range of x. It holds 30 rows with both columns and 45 with one; more than that is a
Range ERROR. Generating a table changes the variable x, as the calculator does, but leaves Ans and the history alone.

```csharp
CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession(CalculatorApp.Table);
session.Define(DefinedFunction.F, "x²+1⌟2");
session.Define(DefinedFunction.G, "x²-1⌟2");

NumberTable table = NumberTable.Generate(
    session, TableType.FunctionsFAndG, Value.FromDecimal(-1m), Value.One, Value.FromDecimal(0.5m));

table.Rows[0].X.Display.Text;                                 // "-1"
table.Rows[0].F!.Display.Text;                                // "3⌟2"
table.SetX(0, Value.FromDecimal(2m));                         // the row is calculated again
table.Verify(0, TableFunction.F, "4.5");                      // true, as Verify does on the table screen
```

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger. The precision guarantees every library upholds are
described in [docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).
