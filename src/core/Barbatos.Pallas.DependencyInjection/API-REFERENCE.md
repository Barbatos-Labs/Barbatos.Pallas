# Barbatos.Pallas.DependencyInjection API Reference

This document lists every public type and member of the Barbatos.Pallas.DependencyInjection package, modeled
after the official .NET API documentation. The package carries four assemblies, one namespace each: `AddPallas()`
and three libraries built on the engine - the reference data, the Spreadsheet and Table applications, and
graphs. Its [README](README.md) shows how the pieces fit together; the engine's own types are in the
[Barbatos.Pallas.Engine reference](../Barbatos.Pallas.Engine/API-REFERENCE.md).

Left out are what the compiler gives every record and record struct - `Equals`, `GetHashCode`, `ToString`, `==`,
`!=` and `Deconstruct` - and parameterless constructors. `ApiReferenceTests` checks this document against the
public API each library declares (`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`): a public member that is
missing here, an overload whose parameters are not named here, or a type described here that no longer exists
fails the build of the repository.

## Namespaces

| Namespace | Description |
|-----------|-------------|
| **[`Barbatos.Pallas.DependencyInjection`](#barbatospallasdependencyinjection-namespace)** | `AddPallas()`: the engine and its sessions registered with Microsoft.Extensions.DependencyInjection. |
| **[`Barbatos.Pallas.Data`](#barbatospallasdata-namespace)** | The reference data Pallas ships: CODATA 2022 constants, NIST SP 811 unit conversions and CIAAW atomic weights. |
| **[`Barbatos.Pallas.Spreadsheet`](#barbatospallasspreadsheet-namespace)** | The Spreadsheet and Table applications, both driven through a session. |
| **[`Barbatos.Pallas.Graphing`](#barbatospallasgraphing-namespace)** | Curves sampled across a viewport, and their roots, extrema and intersections placed on the engine's values. |

---

## `Barbatos.Pallas.DependencyInjection` Namespace

Registers a `PallasEngine` as a singleton and `CalculatorSession`s as transients (`AddPallas`), with validated
options (`PallasOptions`), and lets plugin functions and data sets be added to the engine (`IPallasBuilder`).

### Classes

| Class | Description |
|-------|-------------|
| [`PallasOptions`](#pallasoptions-class) | The options of `ServiceCollectionExtensions.AddPallas`. |
| [`ServiceCollectionExtensions`](#servicecollectionextensions-class) | Registers Barbatos.Pallas with Microsoft.Extensions.DependencyInjection. |

### Interfaces

| Interface | Description |
|-------|-------------|
| [`IPallasBuilder`](#ipallasbuilder-interface) | Adds functions and data sets to the engine being registered. |

---

### `PallasOptions` Class

The options of `ServiceCollectionExtensions.AddPallas`.

```csharp
public sealed class PallasOptions
```

#### Properties

- **`Profile`** (`CalculatorProfile`, settable): Gets or sets the profile new sessions are created with; the calculator's limits by default.
- **`App`** (`CalculatorApp`, settable): Gets or sets the application new sessions start in; Calculate by default.
- **`Budget`** (`EngineBudget`, settable): Gets or sets what one calculation may spend.
- **`IncludeReferenceData`** (`bool`, settable): Gets or sets whether the reference data of Barbatos.Pallas.Data is registered: the CODATA constants, the NIST SP 811 unit conversions and the CIAAW atomic weights. On by default.

#### Methods

- **`Validate()`**
  Validates the options, as the service provider does when the engine is first resolved.

---

### `ServiceCollectionExtensions` Class

Registers Barbatos.Pallas with Microsoft.Extensions.DependencyInjection.

```csharp
public static class ServiceCollectionExtensions
```

#### Methods

- **`AddPallas(this IServiceCollection services, Action<PallasOptions>? configure = null)`** *(static)* → `IPallasBuilder`
  Registers the calculation engine as a singleton and calculator sessions as transients.

---

### `IPallasBuilder` Interface

Adds functions and data sets to the engine being registered.

```csharp
public interface IPallasBuilder
```

#### Properties

- **`Services`** (`IServiceCollection`): Gets the service collection the engine is registered in.

#### Methods

- **`AddFunction(IMathFunction mathFunction)`** → `IPallasBuilder`
  Adds a plugin function.
- **`AddFunction<TFunction>()`** → `IPallasBuilder`
  Adds a plugin function, created without reflection so the engine stays AOT-compatible.
- **`AddConstantSet(ConstantSet constants)`** → `IPallasBuilder`
  Adds a set of scientific constants.
- **`AddUnitSet(UnitSet units)`** → `IPallasBuilder`
  Adds a set of unit conversions.
- **`AddAtomicWeights(AtomicWeightTable atomicWeights)`** → `IPallasBuilder`
  Sets the atomic weights.

---

## `Barbatos.Pallas.Data` Namespace

The data sets `AddPallas()` registers by default: the CODATA 2022 constants, the NIST SP 811 unit conversions with
their exact factors, and the CIAAW standard atomic weights.

### Classes

| Class | Description |
|-------|-------------|
| [`AtomicWeightTables`](#atomicweighttables-class) | The atomic weight tables Pallas ships. |
| [`ConstantSets`](#constantsets-class) | The sets of scientific constants Pallas ships. |
| [`UnitSets`](#unitsets-class) | The sets of unit conversions Pallas ships. |

---

### `AtomicWeightTables` Class

The atomic weight tables Pallas ships.

```csharp
public static class AtomicWeightTables
```

#### Properties

- **`Ciaaw`** (`AtomicWeightTable`, static): The standard atomic weights of the 118 elements, as `AtWt(` returns them (manual p. 67).

---

### `ConstantSets` Class

The sets of scientific constants Pallas ships.

```csharp
public static class ConstantSets
```

#### Properties

- **`Codata2022`** (`ConstantSet`, static): The 47 constants of the reference calculator's CATALOG (manual pp. 64-65), with the values of the 2022 CODATA adjustment.

---

### `UnitSets` Class

The sets of unit conversions Pallas ships.

```csharp
public static class UnitSets
```

#### Properties

- **`NistSp811`** (`UnitSet`, static): The 40 unit conversion commands of the reference calculator's CATALOG (manual p. 66), with the exact definitions of NIST Special Publication 811 (decision of 17 Sep 2026, deviation D4).

---

## `Barbatos.Pallas.Spreadsheet` Namespace

The Spreadsheet application (`SpreadsheetGrid`): constants and formulas in cells, references, ranges, copy, cut and
fill. The Table application (`NumberTable`): f(x) and g(x) of a session over a range of x. Both calculate through
a `CalculatorSession`.

### Classes

| Class | Description |
|-------|-------------|
| [`NumberTable`](#numbertable-class) | The Table application (manual pp. 108-113): f(x) and g(x) over a range of x, as a table of rows that can be edited afterwards. |
| [`SpreadsheetCell`](#spreadsheetcell-class) | One cell of a `SpreadsheetGrid`: a constant, whose value was fixed when it was entered, or a formula, which is calculated again whenever the sheet is (manual p. 101). |
| [`SpreadsheetGrid`](#spreadsheetgrid-class) | The Spreadsheet application (manual pp. 100-107): a grid of cells, each a constant or a formula, calculated through a `CalculatorSession`. |
| [`TableRow`](#tablerow-class) | One row of a `NumberTable`: an x and the functions of it the table shows (manual pp. 108-109). |

### Enums

| Enum | Description |
|-------|-------------|
| [`TableFunction`](#tablefunction-enum) | One of the two functions a number table shows (manual p. 109). |
| [`TableType`](#tabletype-enum) | Which columns a number table has (manual p. 109), which also decides how many rows it may have. |

---

### `NumberTable` Class

The Table application (manual pp. 108-113): f(x) and g(x) over a range of x, as a table of rows that can be edited afterwards.

```csharp
public sealed class NumberTable
```

#### Remarks

A table is generated from Start, End and Step, and holds up to 30 rows with both functions or 45 with one (p. 109); more than that, or a step that would never reach the end, is a Range ERROR (p. 164). Generating a table changes the variable x, which keeps the last value of the column, as the calculator does (p. 109).

Every value is calculated through the session, so f(x) and g(x) are the session's defined functions and the settings are the session's. A row calculated again after its x is edited follows the same path, and Verify checks an answer with the engine's own comparison (p. 112).

#### Properties

- **`Type`** (`TableType`): Gets which columns the table has.
- **`Rows`** (`IReadOnlyList<TableRow>`): Gets the rows, in the order the table was generated.
- **`Error`** (`CalcError?`): Gets the error the table could not be generated with, or `null`.
- **`Succeeded`** (`bool`): Gets whether the table was generated.

#### Methods

- **`RowLimit(TableType type, CalculatorProfile profile)`** *(static)* → `int`
  Gets how many rows the table may have, which the type and the profile decide (p. 109).
- **`Generate(CalculatorSession session, TableType type, Value start, Value end, Value step, CancellationToken cancellationToken = default)`** *(static)* → `NumberTable`
  Generates a table of f(x) and g(x) from Start to End in steps of Step (pp. 108-109).
- **`SetX(int row, Value x, CancellationToken cancellationToken = default)`**
  Changes the x of one row and calculates that row again (p. 110).
- **`RemoveRow(int row)`**
  Deletes one row (p. 110).
- **`Verify(int row, TableFunction function, string answer)`** → `bool?`
  Checks an answer against the value the table has, as Verify does in the Table application (p. 112).

---

### `SpreadsheetCell` Class

One cell of a `SpreadsheetGrid`: a constant, whose value was fixed when it was entered, or a formula, which is calculated again whenever the sheet is (manual p. 101).

```csharp
public sealed class SpreadsheetCell
```

#### Properties

- **`Address`** (`CellAddress`): Gets where the cell is.
- **`Input`** (`string`): Gets the input as it was entered, in Canonical Linear Syntax and without the leading `=` of a formula.
- **`IsFormula`** (`bool`): Gets whether the cell holds a formula; a constant holds the value it was entered with.
- **`Text`** (`string`): Gets the text the calculator's edit box shows: `=A1+7` for a formula, `7×5` for a constant.
- **`Value`** (`Value`): Gets the value; 0 when the cell is in error.
- **`Error`** (`CalcError?`): Gets the error the cell is in, or `null`.
- **`Succeeded`** (`bool`): Gets whether the cell has a value.

---

### `SpreadsheetGrid` Class

The Spreadsheet application (manual pp. 100-107): a grid of cells, each a constant or a formula, calculated through a `CalculatorSession`.

```csharp
public sealed class SpreadsheetGrid
```

#### Remarks

A constant is fixed when it is entered: `A2+7` without a leading `=` is calculated once and keeps that value. A formula, written with a leading `=`, is calculated again whenever the sheet is, which is after every change while Auto Calc is on (p. 107).

A formula is calculated where it stands: the cells it refers to are calculated first, and a cell that is asked for while it is being calculated is a Circular ERROR (p. 165). Nothing is calculated twice in one pass, and an empty cell reads as 0 (assumption U26).

The grid takes the cell references of the session while it exists: a session has one sheet, as a calculator does.

#### Constructors

- **`SpreadsheetGrid(CalculatorSession session)`**
  Creates an empty sheet on a session of the Spreadsheet application.

#### Properties

- **`Columns`** (`int`): Gets the columns, A to E.
- **`Rows`** (`int`): Gets the rows.
- **`Capacity`** (`int`): Gets the bytes the sheet holds in all (p. 100).
- **`AutoCalculate`** (`bool`, settable): Gets or sets whether a change calculates the sheet again; initially on (p. 107).
- **`Cells`** (`IReadOnlyCollection<SpreadsheetCell>`): Gets the cells that have content, in no particular order.
- **`UsedBytes`** (`int`): Gets the bytes the cells use, of `SpreadsheetGrid.Capacity` (p. 101).

#### Indexers

- **`this[CellAddress address]`** (`SpreadsheetCell?`): Gets a cell, or `null` when it is empty.

#### Methods

- **`SetConstant(CellAddress address, string input)`** → `CalcError?`
  Enters a constant: an expression without a leading `=`, calculated once and then fixed (p. 101).
- **`SetFormula(CellAddress address, string formula)`** → `CalcError?`
  Enters a formula, the text after the `=`, which is calculated again whenever the sheet is (p. 101).
- **`Clear(CellAddress address)`**
  Clears one cell (p. 104).
- **`ClearAll()`**
  Clears every cell (p. 104).
- **`CopyPaste(CellAddress from, CellAddress to)`** → `CalcError?`
  Copies a cell and pastes it, moving the relative references by the distance between the two (p. 103).
- **`CutPaste(CellAddress from, CellAddress to)`** → `CalcError?`
  Cuts a cell and pastes it, leaving every reference where it is (p. 104).
- **`Fill(string formula, CellAddress start, CellAddress end)`** → `CalcError?`
  Enters one formula in every cell of a range, its relative references taken from the first cell (p. 106).
- **`FillValue(string input, CellAddress start, CellAddress end)`** → `CalcError?`
  Enters one constant in every cell of a range, its relative references taken from the first cell (p. 106).
- **`Recalculate()`**
  Calculates every formula of the sheet again, as the Recalculate command does (p. 107).

---

### `TableRow` Class

One row of a `NumberTable`: an x and the functions of it the table shows (manual pp. 108-109).

```csharp
public sealed class TableRow
```

#### Properties

- **`X`** (`Calculation`): Gets the x of the row.
- **`F`** (`Calculation?`): Gets f(x), or `null` when the table has no f(x) column.
- **`G`** (`Calculation?`): Gets g(x), or `null` when the table has no g(x) column.

#### Methods

- **`Value(TableFunction function)`** → `Calculation?`
  Returns the value of one of the columns, or `null` when the table has not got it.

---

### `TableFunction` Enum

One of the two functions a number table shows (manual p. 109).

```csharp
public enum TableFunction
```

#### Fields

- **`F`** (`0`): f(x).
- **`G`** (`1`): g(x).

---

### `TableType` Enum

Which columns a number table has (manual p. 109), which also decides how many rows it may have.

```csharp
public enum TableType
```

#### Fields

- **`FunctionsFAndG`** (`0`): f(x) and g(x), the initial setting: up to 30 rows.
- **`FunctionF`** (`1`): f(x) alone: up to 45 rows.
- **`FunctionG`** (`2`): g(x) alone: up to 45 rows.

---

## `Barbatos.Pallas.Graphing` Namespace

Samples a `CompiledExpression` across a `GraphViewport` into pieces of curve to draw (`GraphSampler`), breaks told
apart as asymptotes or jumps, and finds roots, extrema and intersections by halving on the engine's values
(`GraphAnalysis`). Its `double`s only place points; every y it names is the engine's calculation.

### Classes

| Class | Description |
|-------|-------------|
| [`GraphAnalysis`](#graphanalysis-class) | Finds the roots, the extrema and the intersections of curves across the x of a viewport. |
| [`GraphFeature`](#graphfeature-class-record) | A point of a curve worth naming: a root, an extremum, or where two curves meet. |
| [`GraphSampler`](#graphsampler-class) | Samples an expression in x across a viewport into the pieces of curve to draw. |
| [`GraphTrace`](#graphtrace-class) | A curve as it is drawn in a viewport: the pieces of it there are, and where it breaks. |
| [`GraphViewport`](#graphviewport-class-record) | The region of the plane a graph shows: x from `GraphViewport.Left` to `GraphViewport.Right`, y from `GraphViewport.Bottom` to `GraphViewport.Top`. |

### Enums

| Enum | Description |
|-------|-------------|
| [`GraphBreakKind`](#graphbreakkind-enum) | Why a curve breaks. |
| [`GraphFeatureKind`](#graphfeaturekind-enum) | What a named point of a curve is. |

### Structs

| Struct | Description |
|-------|-------------|
| [`GraphBreak`](#graphbreak-struct) | Where a curve is not drawn through, and why. |
| [`GraphPoint`](#graphpoint-struct) | A point of a curve, in the units of the plane: where it is drawn, not a value the calculator shows. |

---

### `GraphAnalysis` Class

Finds the roots, the extrema and the intersections of curves across the x of a viewport.

```csharp
public static class GraphAnalysis
```

#### Remarks

Each is found where something changes sign between two samples: the expression for a root, its derivative for an extremum, the difference of two expressions for an intersection. The interval is then halved on the engine's own values until its ends are neighbouring numbers, and the x found becomes a value as any `Double` does, to fifteen significant digits: so a root at 1 is 1, not 0.999999999999999889. The y named is the expression calculated by the engine at that x, displayed as a result is.

A change of sign is not always a root. Across a pole or a jump - 1÷x at 0, Intg(x)−0.5 at 1 - the curve breaks, as the sampler sees it breaks, and nothing is named there. An extremum is found on the derivative, which the engine takes exactly (`CompiledExpression.Derivative`), because a search along a flat curve can only place its x to about half the digits of its y. A kink is an extremum too: |x| at 0, where the slope jumps from −1 to 1. A slope that grows without bound is a pole, not a turn.

What changes no sign between two samples is not found: a root where the curve only touches the axis is an extremum, and two roots closer together than a column are one change of sign or none. More columns see more. A curve that runs along the axis changes no sign either, and every x there is as much a root as any: none is named. Two curves meet only where both have a value, so a change of places through a hole in one of them is no intersection.

#### Methods

- **`Roots(CompiledExpression expression, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)`** *(static)* → `ImmutableArray<GraphFeature>`
  Finds where an expression crosses the x axis.
- **`Extrema(CompiledExpression expression, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)`** *(static)* → `ImmutableArray<GraphFeature>`
  Finds where an expression turns: its minima and maxima.
- **`Intersections(CompiledExpression first, CompiledExpression second, GraphViewport viewport, int columns, CancellationToken cancellationToken = default)`** *(static)* → `ImmutableArray<GraphFeature>`
  Finds where two expressions meet.

---

### `GraphFeature` Class (record)

A point of a curve worth naming: a root, an extremum, or where two curves meet.

```csharp
public sealed record GraphFeature(GraphFeatureKind Kind, Value X, Calculation Y)
```

#### Constructors

- **`GraphFeature(GraphFeatureKind Kind, Value X, Calculation Y)`**
  A point of a curve worth naming: a root, an extremum, or where two curves meet.

#### Properties

- **`Kind`** (`GraphFeatureKind`, init): What it is.
- **`X`** (`Value`, init): Its x, a value to 15 significant digits as every approximate value is.
- **`Y`** (`Calculation`, init): The expression calculated by the engine at `X`, displayed as a result is.

---

### `GraphSampler` Class

Samples an expression in x across a viewport into the pieces of curve to draw.

```csharp
public static class GraphSampler
```

#### Remarks

One sample per column of pixels is where it starts. A column whose middle falls more than half a pixel off the chord between its ends is halved, down to a sixteenth of a pixel, so a curve is drawn as curved as the screen can show and no more. Where the expression has no value - an error, or a complex number - the curve stops, and it is followed to the edge of where it has one, to neighbouring numbers.

A curve is never drawn across a break. Between two points more than two pixels apart, the interval is halved towards the larger step: a continuous curve's step shrinks with its interval, a jump's does not, and an asymptote's grows. That is what tells Int(x) at 1 and tan x at 90° from a steep but unbroken x³.

Where the curve is above the top of the viewport or below its bottom, it is neither refined nor tested for breaks, so that part costs one calculation per column. Measured on 24 Sep 2026: refining it too put x²−3x+1 in the square of side 20 at 17.7 ms for 2,000 columns, above the 16 ms of a frame.

Every value comes from `CompiledExpression.TryEvaluate`, calculated by the engine as the session would; the coordinates here only place it.

#### Fields

- **`MaximumColumns`** = `10000` (`int`, const): The most columns a graph is sampled across: a screen wider than any there is.
- **`MaximumRows`** = `10000` (`int`, const): The most rows: likewise.

#### Methods

- **`Sample(CompiledExpression expression, GraphViewport viewport, int columns, int rows, CancellationToken cancellationToken = default)`** *(static)* → `GraphTrace`
  Samples an expression across a viewport.

---

### `GraphTrace` Class

A curve as it is drawn in a viewport: the pieces of it there are, and where it breaks.

```csharp
public sealed class GraphTrace
```

#### Remarks

A piece is a polyline to draw as it is. It lies inside the viewport - where the curve leaves it, the piece ends on its edge - so the host draws the points it is given and clips nothing, and a curve that runs off to 10¹⁰⁰ does not reach the drawing as a coordinate it cannot hold.

#### Properties

- **`Viewport`** (`GraphViewport`): Gets the viewport the curve was sampled in.
- **`Pieces`** (`ImmutableArray<ImmutableArray<GraphPoint>>`): Gets the pieces of the curve inside the viewport, left to right, each with two points or more.
- **`Breaks`** (`ImmutableArray<GraphBreak>`): Gets where the curve breaks within the viewport's x, left to right.
- **`Evaluations`** (`int`): Gets how many times the expression was calculated.

---

### `GraphViewport` Class (record)

The region of the plane a graph shows: x from `GraphViewport.Left` to `GraphViewport.Right`, y from `GraphViewport.Bottom` to `GraphViewport.Top`.

```csharp
public sealed record GraphViewport
```

#### Remarks

A viewport is a place on the screen, so its bounds are `Double`: they position pixels, and no value the calculator shows is read from them.

#### Constructors

- **`GraphViewport(double left, double right, double bottom, double top)`**
  Creates a viewport.

#### Properties

- **`Left`** (`double`): Gets the smallest x shown.
- **`Right`** (`double`): Gets the largest x shown.
- **`Bottom`** (`double`): Gets the smallest y shown.
- **`Top`** (`double`): Gets the largest y shown.
- **`Width`** (`double`): Gets the width of the region, in units of x.
- **`Height`** (`double`): Gets the height of the region, in units of y.

---

### `GraphBreakKind` Enum

Why a curve breaks.

```csharp
public enum GraphBreakKind
```

#### Fields

- **`Asymptote`** (`0`): The values grow without bound towards the break: tan x at 90°, 1÷x at 0.
- **`Jump`** (`1`): The values jump by a finite step: Int(x) at an integer.

---

### `GraphFeatureKind` Enum

What a named point of a curve is.

```csharp
public enum GraphFeatureKind
```

#### Fields

- **`Root`** (`0`): The curve crosses the x axis.
- **`Minimum`** (`1`): The curve turns from falling to rising.
- **`Maximum`** (`2`): The curve turns from rising to falling.
- **`Intersection`** (`3`): Two curves cross.

---

### `GraphBreak` Struct

Where a curve is not drawn through, and why.

```csharp
public readonly struct GraphBreak : IEquatable<GraphBreak>
```

#### Constructors

- **`GraphBreak(double X, GraphBreakKind Kind)`**
  Where a curve is not drawn through, and why.

#### Properties

- **`X`** (`double`, init): Where it breaks, to the resolution the sampler narrowed it to.
- **`Kind`** (`GraphBreakKind`, init): An asymptote or a jump.

---

### `GraphPoint` Struct

A point of a curve, in the units of the plane: where it is drawn, not a value the calculator shows.

```csharp
public readonly struct GraphPoint : IEquatable<GraphPoint>
```

#### Constructors

- **`GraphPoint(double X, double Y)`**
  A point of a curve, in the units of the plane: where it is drawn, not a value the calculator shows.

#### Properties

- **`X`** (`double`, init): Its x.
- **`Y`** (`double`, init): Its y.
