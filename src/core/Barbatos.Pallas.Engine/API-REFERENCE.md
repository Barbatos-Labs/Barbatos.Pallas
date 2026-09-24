# Barbatos.Pallas.Engine API Reference

This document lists every public type and member of the Barbatos.Pallas.Engine package, modeled after the
official .NET API documentation. The package carries six assemblies, one namespace each: the engine itself and
the five libraries it is built on. Its [README](README.md) shows how the pieces fit together; this is where each
one is described with its signature.

Left out are what the compiler gives every record and record struct - `Equals`, `GetHashCode`, `ToString`, `==`,
`!=` and `Deconstruct` - and parameterless constructors. `ApiReferenceTests` checks this document against the
public API each library declares (`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`): a public member that is
missing here, an overload whose parameters are not named here, or a type described here that no longer exists
fails the build of the repository.

How a value is held, rounded and displayed is the subject of
[docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).

## Namespaces

| Namespace | Description |
|-----------|-------------|
| **[`Barbatos.Pallas.Engine`](#barbatospallasengine-namespace)** | The calculation engine: values and their precision rule, sessions and memory, the display, calculus, the calculator's applications, compiled expressions and the plugin API. |
| **[`Barbatos.Pallas.Expressions`](#barbatospallasexpressions-namespace)** | Canonical Linear Syntax read into an immutable syntax tree and printed back as linear text or LaTeX; it evaluates nothing. |
| **[`Barbatos.Pallas.Numerics`](#barbatospallasnumerics-namespace)** | The calculator mathematics .NET lacks: trigonometry in three angle units, integer functions, fractions, sexagesimal angles, the error function and the Poisson probability. |
| **[`Barbatos.Pallas.LinearAlgebra`](#barbatospallaslinearalgebra-namespace)** | Exact determinants, inverses and linear systems of decimal matrices, on `BigInteger`. |
| **[`Barbatos.Pallas.Statistics`](#barbatospallasstatistics-namespace)** | Exact sums, means, variances, fits and quartile ranks of decimal data, with no `double`. |
| **[`Barbatos.Pallas.Solvers`](#barbatospallassolvers-namespace)** | Polynomial algebra: exact integer polynomials, and the complex roots of any polynomial. |

---

## `Barbatos.Pallas.Engine` Namespace

Binds, compiles and evaluates the syntax tree of `Barbatos.Pallas.Expressions`. A `PallasEngine` is built once
(`PallasEngineBuilder`) and creates `CalculatorSession`s, each a calculator in use: its application, settings,
memory and history. A calculation is a `Calculation` - its `Value`, its error as a `CalcError`, and its display.
The applications beyond Calculate are methods of the session (Distribution, Equation, Inequality, Ratio, Math
Box) or values it holds (Matrix, Vector, Statistics). `CompiledExpression` calculates one expression at many x,
and `IMathFunction` adds a function of one's own.

### Classes

| Class | Description |
|-------|-------------|
| [`AtomicWeight`](#atomicweight-class-record) | The atomic weight of one element, as `AtWt(n)` returns it. |
| [`AtomicWeightTable`](#atomicweighttable-class) | The atomic weights of the elements, such as the CIAAW standard atomic weights. |
| [`Calculation`](#calculation-class) | One calculation of a `CalculatorSession`: its input, result or error, and display. |
| [`CalculatorSession`](#calculatorsession-class) | A calculator in use: the application, settings, memory (Ans, PreAns, A-F, x, y, z, MatA-MatD, VctA-VctD), defined functions, statistics data and history. |
| [`CalculatorSettings`](#calculatorsettings-class-record) | The Calc Settings of the reference calculator (manual pp. 22-25), plus the Base-N number mode and Verify. |
| [`CircleAngle`](#circleangle-class) | An angle of the Unit Circle or Half Circle screen and its trigonometric values (manual pp. 157, 160). |
| [`ClockAngles`](#clockangles-class-record) | The Clock screen of the Circle application: an hour, and the two angles between the hands (manual pp. 158, 161). |
| [`CompiledExpression`](#compiledexpression-class) | An expression in x, read and bound once and calculated at as many values of x as a graph or a table needs. |
| [`ConstantSet`](#constantset-class) | A set of physical constants, such as CODATA 2022. |
| [`DistributionParameters`](#distributionparameters-class-record) | The parameters of the Distribution application (manual p. 98). Each calculation type reads the ones it needs. |
| [`EngineBudget`](#enginebudget-class-record) | The limits on one calculation's work (docs/PRECISION.md §11). Exceeding either is a Time Out, never a hang. |
| [`EvaluationContext`](#evaluationcontext-class) | What a function may know about the calculation it is evaluated in. |
| [`FormattedResult`](#formattedresult-class-record) | A result as the calculator displays it. |
| [`FunctionSignature`](#functionsignature-class) | The name, arity and availability of a plugin function. |
| [`InequalitySolution`](#inequalitysolution-class) | What `CalculatorSession.SolveInequality` found for a polynomial inequality (manual pp. 124-125). |
| [`MatrixSnapshot`](#matrixsnapshot-class-record) | A matrix or a vector of a `SessionSnapshot`: its size and its entries, row by row. |
| [`MatrixValue`](#matrixvalue-class) | An immutable matrix of real values, as the Matrix application holds in MatA-MatD and MatAns (manual pp. 132-139). |
| [`NumberLine`](#numberline-class) | The Number Line application of Math Box: up to three expressions, each drawn as a part of the x axis, and the View-Window they are drawn in (manual pp. 153-157). |
| [`NumberLineAxis`](#numberlineaxis-class) | One expression of the Number Line application and the part of the x axis it covers (manual pp. 153-155). |
| [`NumberLineView`](#numberlineview-class-record) | The View-Window of the Number Line application: the middle of the x axis and its scale (manual pp. 156-157). |
| [`PallasEngine`](#pallasengine-class) | The calculation engine: an immutable catalog of functions and data from which calculator sessions are created. |
| [`PallasEngineBuilder`](#pallasenginebuilder-class) | Configures and builds a `PallasEngine`: plugin functions, data sets and the budget. |
| [`PolynomialExtremum`](#polynomialextremum-class) | A local minimum or maximum of a polynomial, where the calculator shows its coordinates (manual p. 117). |
| [`PolynomialRoot`](#polynomialroot-class) | One root of a polynomial: its real part and, for a complex root, its imaginary part (manual pp. 116-118). |
| [`PolynomialSolution`](#polynomialsolution-class) | What `CalculatorSession.SolvePolynomial` found for a polynomial of degree 2 to 4 (manual pp. 116-119). |
| [`ScientificConstant`](#scientificconstant-class) | A physical constant of a `ConstantSet`. |
| [`SessionSnapshot`](#sessionsnapshot-class-record) | Everything a `CalculatorSession` holds, as text and settings an application can store and read back (`CalculatorSession.Capture`, `CalculatorSession.Restore`). |
| [`Simulation`](#simulation-class) | A Dice Roll or Coin Toss of the Math Box application: every attempt, and what the List and Relative Freq screens show of them (manual pp. 147-153). |
| [`SimulationFrequency`](#simulationfrequency-class-record) | One row of the Relative Freq screen of a simulation (manual pp. 150, 153). |
| [`SimultaneousSolution`](#simultaneoussolution-class) | What `CalculatorSession.SolveSimultaneous` found for a system of linear equations (manual pp. 114-116). |
| [`SolutionInterval`](#solutioninterval-class) | One stretch of the numbers that satisfy an inequality, as the calculator writes it: `x≤-3`, `1≤x` or `-3<x<1` (manual pp. 124-125). |
| [`StatisticsData`](#statisticsdata-class) | The data of the Statistics application: x values, or (x, y) pairs, with a frequency for each row when the Frequency setting is on (manual pp. 79-83). |
| [`UnitConversion`](#unitconversion-class) | A unit conversion command, such as `cm▶in`: the result is `(value + OffsetBefore) × Multiplier ÷ Divisor + OffsetAfter`. |
| [`UnitSet`](#unitset-class) | A set of unit conversion commands, such as those of NIST SP 811. |
| [`VectorValue`](#vectorvalue-class) | An immutable vector of real values, as the Vector application holds in VctA-VctD and VctAns (manual pp. 139-145). |

### Enums

| Enum | Description |
|-------|-------------|
| [`CalcErrorKind`](#calcerrorkind-enum) | The errors of the reference calculator (manual pp. 162-166), without their text. |
| [`CalculationKind`](#calculationkind-enum) | What a `Calculation` produced. |
| [`CalculatorProfile`](#calculatorprofile-enum) | How closely the engine follows the reference calculator's limits (docs/PRECISION.md §10). A profile changes limits and formatting, never how a value is computed. |
| [`CircleKind`](#circlekind-enum) | The circle an angle of the Circle application is drawn on (manual pp. 157-159). |
| [`ComplexResult`](#complexresult-enum) | The Complex Result setting (manual p. 24). |
| [`DecimalMark`](#decimalmark-enum) | The Decimal Mark setting (manual p. 24-25). |
| [`DefinedFunction`](#definedfunction-enum) | The two functions a user can define (manual pp. 70-72). |
| [`DistributionKind`](#distributionkind-enum) | The calculation types of the Distribution application (manual p. 96). |
| [`ExtremumKind`](#extremumkind-enum) | Which extremum of a polynomial a `PolynomialExtremum` is (manual p. 117). |
| [`FormatTarget`](#formattarget-enum) | The conversions of the FORMAT menu (manual pp. 42-50). |
| [`FractionResult`](#fractionresult-enum) | The Fraction Result setting (manual p. 24). |
| [`InputOutput`](#inputoutput-enum) | The Input/Output setting (manual p. 22). Input is always Canonical Linear Syntax in the engine; the setting decides the forms a result may be displayed in. |
| [`MatrixVariable`](#matrixvariable-enum) | The matrix variables of the Matrix application (manual pp. 132-136). |
| [`MemoryVariable`](#memoryvariable-enum) | The variables of the calculator (manual p. 38), shared by every application. |
| [`NumberFormatKind`](#numberformatkind-enum) | The kinds of the Number Format setting (manual p. 23). |
| [`NumberLineForm`](#numberlineform-enum) | The nine forms of a Number Line expression, in the order of the calculator's list (manual p. 153). |
| [`RatioForm`](#ratioform-enum) | Which ratio the Ratio application solves for X (manual p. 145). |
| [`RegressionModel`](#regressionmodel-enum) | The regression types of two-variable statistics (manual pp. 86-87), fitted by least squares (pp. 93-95). |
| [`SameResult`](#sameresult-enum) | The Same Result setting of a simulation (manual p. 150). |
| [`SimulationKind`](#simulationkind-enum) | A probability simulation of the Math Box application (manual pp. 147-153). |
| [`SimulationTally`](#simulationtally-enum) | What the Relative Freq screen of a simulation counts (manual pp. 148, 150, 153). |
| [`SolutionOutcome`](#solutionoutcome-enum) | What an Equation or Inequality calculation found, where the calculator shows a message instead of values (manual pp. 116, 118, 125). |
| [`StatisticsColumn`](#statisticscolumn-enum) | A column of the Statistics editor (manual p. 80). |
| [`ValueKind`](#valuekind-enum) | The .NET type that holds a `Value` (docs/PRECISION.md §3). |
| [`VectorVariable`](#vectorvariable-enum) | The vector variables of the Vector application (manual pp. 139-143). |

### Interfaces

| Interface | Description |
|-------|-------------|
| [`IMathFunction`](#imathfunction-interface) | A function added to the engine by a plugin, such as a beam deflection formula. |

### Structs

| Struct | Description |
|-------|-------------|
| [`CalcError`](#calcerror-struct) | An error of a calculation: what went wrong and where. |
| [`CellAddress`](#celladdress-struct) | One cell of the Spreadsheet application, by column and row (manual pp. 100-101). |
| [`EvalResult`](#evalresult-struct) | The outcome of evaluating one operation or function: a value, or a calculator error. |
| [`IntegralEstimate`](#integralestimate-struct) | The error estimate of one numerical integration in a calculation (invariant I7 of docs/PRECISION.md). |
| [`NumberFormat`](#numberformat-struct) | The Number Format setting (manual p. 23): Norm 1, Norm 2, Fix 0-9 or Sci 1-10. |
| [`ScaledDecimal`](#scaleddecimal-struct) | A number written as a `Decimal` mantissa times a power of ten, so reference data keeps every published digit without `Double`. |
| [`Value`](#value-struct) | A calculator value: a real number held as `Decimal` or `Double`, a complex number, a 32-bit Base-N integer, or a matrix or vector of real numbers. |

---

### `AtomicWeight` Class (record)

The atomic weight of one element, as `AtWt(n)` returns it.

```csharp
public sealed record AtomicWeight(int AtomicNumber, string Symbol, decimal Weight, bool IsMassNumber)
```

#### Constructors

- **`AtomicWeight(int AtomicNumber, string Symbol, decimal Weight, bool IsMassNumber)`**
  The atomic weight of one element, as `AtWt(n)` returns it.

#### Properties

- **`AtomicNumber`** (`int`, init): The atomic number, 1 to 118.
- **`Symbol`** (`string`, init): The chemical symbol: `Sc`.
- **`Weight`** (`decimal`, init): The standard atomic weight, or its conventional value for an element given as an interval; for an element without a standard atomic weight, the mass number of its longest-lived isotope.
- **`IsMassNumber`** (`bool`, init): Whether `Weight` is a mass number, shown in brackets on the periodic table.

---

### `AtomicWeightTable` Class

The atomic weights of the elements, such as the CIAAW standard atomic weights.

```csharp
public sealed class AtomicWeightTable
```

#### Constructors

- **`AtomicWeightTable(string name, IEnumerable<AtomicWeight> elements)`**
  Initializes a table.

#### Properties

- **`Name`** (`string`): Gets the name of the table.
- **`Elements`** (`ImmutableArray<AtomicWeight>`): Gets the elements.

---

### `Calculation` Class

One calculation of a `CalculatorSession`: its input, result or error, and display.

```csharp
public sealed class Calculation
```

#### Properties

- **`Input`** (`string`): Gets the input, in Canonical Linear Syntax; for a Distribution calculation, the name of its type, such as "Binomial CD".
- **`App`** (`CalculatorApp`): Gets the application the calculation ran in.
- **`Settings`** (`CalculatorSettings`): Gets the settings in effect.
- **`Profile`** (`CalculatorProfile`): Gets the profile.
- **`Kind`** (`CalculationKind`): Gets what the calculation produced.
- **`Result`** (`Value`): Gets the result: the value, the quotient of ÷R, r of Pol(, x of Rec(, or 1 or 0 for Verify.
- **`Second`** (`Value?`): Gets the second result: the remainder of ÷R, θ of Pol(, y of Rec( or Left − Right of the Solver; otherwise `null`.
- **`IsTrue`** (`bool?`): Gets the Verify result; `null` for other calculations.
- **`Error`** (`CalcError?`): Gets the error, or `null`.
- **`Succeeded`** (`bool`): Gets whether the calculation produced a result.
- **`Integrals`** (`ImmutableArray<IntegralEstimate>`): Gets the error estimates of the integrals computed (invariant I7 of docs/PRECISION.md).
- **`Display`** (`FormattedResult`): Gets the result as displayed with `Calculation.Settings`; empty text for an error.

---

### `CalculatorSession` Class

A calculator in use: the application, settings, memory (Ans, PreAns, A-F, x, y, z, MatA-MatD, VctA-VctD), defined functions, statistics data and history.

```csharp
public sealed class CalculatorSession
```

#### Remarks

A session is not thread-safe; the engine it comes from is. Memory and defined functions persist across applications, as on the reference calculator (manual pp. 36-40, 70-72, 135); PreAns exists only in Calculate, MatAns only in Matrix and VctAns only in Vector (pp. 137, 144). The statistics data stay until they are replaced.

#### Properties

- **`Engine`** (`PallasEngine`): Gets the engine the session belongs to.
- **`App`** (`CalculatorApp`): Gets the current application.
- **`Profile`** (`CalculatorProfile`): Gets the profile.
- **`Settings`** (`CalculatorSettings`, settable): Gets or sets the settings.
- **`Budget`** (`EngineBudget`, settable): Gets or sets the budget of each calculation.
- **`CellValues`** (`Func<CellAddress, EvalResult>?`, settable): Gets or sets where a cell reference in a Spreadsheet formula reads its value (manual pp. 102, 105).
- **`Ans`** (`Value`): Gets the last result (manual p. 36).
- **`PreAns`** (`Value`): Gets the result before the last one; Calculate only (p. 37).
- **`MatAns`** (`MatrixValue?`): Gets the last matrix result of the Matrix application, or `null` (p. 136).
- **`VctAns`** (`VectorValue?`): Gets the last vector result of the Vector application, or `null` (p. 144).
- **`StatisticsData`** (`StatisticsData`): Gets the data of the Statistics application; initially `StatisticsData.Empty`.
- **`Regression`** (`RegressionModel`, settable): Gets or sets the regression type of two-variable statistics (p. 86); initially y = a + bx.
- **`History`** (`IReadOnlyList<Calculation>`): Gets the calculations since the history was last cleared, oldest first.

#### Methods

- **`GetVariable(MemoryVariable variable)`** → `Value`
  Returns the value of a variable.
- **`SetVariable(MemoryVariable variable, Value value)`**
  Stores a value in a variable.
- **`GetMatrix(MatrixVariable variable)`** → `MatrixValue?`
  Returns a matrix variable.
- **`SetMatrix(MatrixVariable variable, MatrixValue? matrix)`**
  Stores a matrix in a matrix variable, or clears it.
- **`GetVector(VectorVariable variable)`** → `VectorValue?`
  Returns a vector variable.
- **`SetVector(VectorVariable variable, VectorValue? vector)`**
  Stores a vector in a vector variable, or clears it.
- **`SetStatisticsData(StatisticsData data)`**
  Replaces the data of the Statistics application (pp. 80-83).
- **`Store(MemoryVariable variable)`**
  Stores Ans in a variable: STO after a calculation (p. 38).
- **`Define(DefinedFunction function, string expression)`** → `CalcError?`
  Defines f(x) or g(x) (p. 70).
- **`Undefine(DefinedFunction function)`**
  Removes the definition of f(x) or g(x).
- **`SwitchApp(CalculatorApp app)`**
  Changes the application. Leaving Calculate clears PreAns, leaving Matrix clears MatAns and leaving Vector clears VctAns; the history is cleared (pp. 35, 37, 137, 144).
- **`ClearHistory()`**
  Clears the history.
- **`Calculate(string input, CancellationToken cancellationToken = default)`** → `Calculation`
  Calculates an input in the current application.
- **`Evaluate(string input, CancellationToken cancellationToken = default)`** → `Calculation`
  Calculates an input without storing anything: neither in Ans nor in the history.
- **`Compile(string input)`** → `CompiledExpression`
  Compiles an expression in x once, to be calculated at many values of x: a graph, a table.
- **`Calculate(string input, IReadOnlyDictionary<MemoryVariable, Value> values, CancellationToken cancellationToken = default)`** → `Calculation`
  Stores values in variables, then calculates: CALC (p. 40).
- **`CalculateDistribution(DistributionKind kind, DistributionParameters parameters, CancellationToken cancellationToken = default)`** → `Calculation`
  Calculates a distribution with one x, the Variable input method (manual pp. 97, 100); the result goes to Ans.
- **`CalculateDistribution(DistributionKind kind, DistributionParameters parameters, IReadOnlyList<Value> list, CancellationToken cancellationToken = default)`** → `IReadOnlyList<Calculation>`
  Calculates a binomial or Poisson distribution for each x of a list, the List input method (pp. 96-99). A value outside the domain is an error of its own row; Ans is unchanged.
- **`SolveSimultaneous(Value[,] augmented, CancellationToken cancellationToken = default)`** → `SimultaneousSolution`
  Solves a system of 2 to 4 linear equations (manual pp. 114-116).
- **`SolvePolynomial(IReadOnlyList<Value> coefficients, CancellationToken cancellationToken = default)`** → `PolynomialSolution`
  Solves a polynomial equation of degree 2 to 4 (manual pp. 116-119).
- **`SolveInequality(IReadOnlyList<Value> coefficients, RelationOperator relation, CancellationToken cancellationToken = default)`** → `InequalitySolution`
  Solves a polynomial inequality of degree 2 to 4 (manual pp. 124-125).
- **`SolveEquation(string input, MemoryVariable variable, Value initialValue, CancellationToken cancellationToken = default)`** → `Calculation`
  Solves an equation for one variable, by Newton's method from an initial value (manual pp. 120-121).
- **`SolveRatio(RatioForm form, Value a, Value b, Value other)`** → `Calculation`
  Solves a ratio for X (manual pp. 145-146); the result goes to Ans.
- **`Simulate(SimulationKind kind, int count, Value attempts, SameResult sameResult = SameResult.Off)`** → `Simulation`
  Runs a Dice Roll or Coin Toss of the Math Box application (manual pp. 147-153).
- **`CircleAngle(CircleKind kind, Value angle, CancellationToken cancellationToken = default)`** → `CircleAngle`
  Draws an angle of the Circle application on a circle, with its trigonometric values (manual pp. 157-160).
- **`Clock(int hour)`** → `ClockAngles`
  The Clock screen of the Circle application: the angles between the hands at an hour (manual pp. 158, 161).
- **`Capture()`** → `SessionSnapshot`
  Takes everything the session holds, as text an application can store (manual pp. 36-40).
- **`Restore(SessionSnapshot snapshot)`**
  Puts back what `CalculatorSession.Capture` took, over whatever the session holds now.
- **`Format(Calculation calculation, FormatTarget? target = null)`** → `FormattedResult?`
  Displays a calculation with the current settings, converted by the FORMAT menu.
- **`FormatEngineering(Calculation calculation, int shift)`** → `string?`
  Displays a result in engineering notation shifted by steps of three digits, as the ◀ and ▶ keys do in ENG mode (p. 48).

---

### `CalculatorSettings` Class (record)

The Calc Settings of the reference calculator (manual pp. 22-25), plus the Base-N number mode and Verify.

```csharp
public sealed record CalculatorSettings
```

#### Remarks

Every property starts at the calculator's initial setting, marked ◆ in the manual.

#### Properties

- **`Initial`** (`CalculatorSettings`, static): Gets the initial settings.
- **`InputOutput`** (`InputOutput`, init): Gets the Input/Output setting; initially MathI/MathO.
- **`AngleUnit`** (`AngleUnit`, init): Gets the angle unit; initially degrees.
- **`NumberFormat`** (`NumberFormat`, init): Gets the number format; initially Norm 1.
- **`EngineerSymbol`** (`bool`, init): Gets whether results are displayed with engineering symbols (k, M, …); initially off.
- **`FractionResult`** (`FractionResult`, init): Gets how fractions are displayed; initially improper.
- **`ComplexResult`** (`ComplexResult`, init): Gets how complex results are displayed; initially a+bi.
- **`DecimalMark`** (`DecimalMark`, init): Gets the decimal mark of displayed results; initially a dot.
- **`DigitSeparator`** (`bool`, init): Gets whether displayed results group digits in threes; initially off.
- **`BaseMode`** (`NumberBase`, init): Gets the Base-N number mode; initially decimal.
- **`Verify`** (`bool`, init): Gets whether Verify is on (manual p. 73): every input must then be an equation or inequality.
- **`ComplexRoots`** (`bool`, init): Gets whether a polynomial shows its complex roots (manual p. 118); initially on.

---

### `CircleAngle` Class

An angle of the Unit Circle or Half Circle screen and its trigonometric values (manual pp. 157, 160).

```csharp
public sealed class CircleAngle
```

#### Remarks

The values are calculations of the session, in its angle unit and with its Input/Output setting, so they are shown as every other result is: sin 45° as √(2)⌟2 under MathO (p. 160). A value that does not exist - tan 90° - is that calculation's Math ERROR, while the angle itself is still drawn (assumption U30 of docs/CONFORMANCE.md).

#### Properties

- **`Kind`** (`CircleKind`): Gets the circle the angle is drawn on.
- **`Angle`** (`Value`): Gets the angle, in the angle unit of the session.
- **`Sine`** (`Calculation?`): Gets sin θ, or `null` when the angle is out of range.
- **`Cosine`** (`Calculation?`): Gets cos θ, or `null` when the angle is out of range.
- **`Tangent`** (`Calculation?`): Gets tan θ, or `null` when the angle is out of range.
- **`Error`** (`CalcError?`): Gets the Range ERROR of an angle outside the range of its circle (p. 159); otherwise `null`.
- **`Succeeded`** (`bool`): Gets whether the angle is drawn.

---

### `ClockAngles` Class (record)

The Clock screen of the Circle application: an hour, and the two angles between the hands (manual pp. 158, 161).

```csharp
public sealed record ClockAngles(int Hour, Calculation Smaller, Calculation Larger)
```

#### Remarks

The angles are calculations, so they are shown as results are: at 3:00, 90 and 270 in degrees, and π⌟2 and 3⌟2π in radians under MathO (p. 161). At 12:00 they are 0 and a full turn, and at 6:00 they are equal (assumption U31).

#### Constructors

- **`ClockAngles(int Hour, Calculation Smaller, Calculation Larger)`**
  The Clock screen of the Circle application: an hour, and the two angles between the hands (manual pp. 158, 161).

#### Properties

- **`Hour`** (`int`, init): The hour on the clock, 1 to 12; the minute hand stays on 12.
- **`Smaller`** (`Calculation`, init): θ1, the smaller angle between the hour hand and the minute hand, in the angle unit of the session.
- **`Larger`** (`Calculation`, init): θ2, the larger one: a full turn less θ1.

---

### `CompiledExpression` Class

An expression in x, read and bound once and calculated at as many values of x as a graph or a table needs.

```csharp
public sealed class CompiledExpression
```

#### Remarks

`CalculatorSession.Evaluate` reads, binds and compiles its input on every call, which is what a calculation typed on the line needs and what a graph of two thousand points cannot afford. An expression compiled by `CalculatorSession.Compile` calculates exactly as the session would with the variable x holding the value, under the application and the settings in effect when it was compiled, and it stores nothing: neither x nor Ans. The other variables and memories are read from the session as they are at each calculation.

Like the session it belongs to, an instance is used from one thread at a time: a calculation that draws a random number draws it from the session's generator.

#### Properties

- **`Input`** (`string`): Gets the input, in Canonical Linear Syntax.
- **`App`** (`CalculatorApp`): Gets the application the expression was compiled in.
- **`Settings`** (`CalculatorSettings`): Gets the settings it calculates with: those in effect when it was compiled.
- **`Error`** (`CalcError?`): Gets the error that kept the input from compiling, with the span the calculator would put the cursor at, or `null`.
- **`Succeeded`** (`bool`): Gets whether the input compiled.

#### Methods

- **`Evaluate(Value x, CancellationToken cancellationToken = default)`** → `Calculation`
  Calculates the expression with x holding a value.
- **`ValueOf(double x)`** → `Value?`
  Returns the value x holds when the expression is calculated at a `Double`.
- **`TryEvaluate(double x, out double y)`** → `bool`
  Calculates the expression at an x given as a `Double`, for drawing it.
- **`Derivative()`** → `CompiledExpression`
  Compiles the derivative of the expression in x, `d/dx(`expression`,x)`, in the same application and settings.

---

### `ConstantSet` Class

A set of physical constants, such as CODATA 2022.

```csharp
public sealed class ConstantSet
```

#### Constructors

- **`ConstantSet(string name, IEnumerable<ScientificConstant> constants)`**
  Initializes a set.

#### Properties

- **`Name`** (`string`): Gets the name of the set.
- **`Constants`** (`ImmutableArray<ScientificConstant>`): Gets the constants.

---

### `DistributionParameters` Class (record)

The parameters of the Distribution application (manual p. 98). Each calculation type reads the ones it needs.

```csharp
public sealed record DistributionParameters
```

#### Remarks

On the calculator the last value entered for a parameter is kept for every type that uses it: N entered for Binomial PD is also N of Binomial CD (p. 98). One record for all the types gives a host that behavior with `with`. A parameter left out is 0.

#### Properties

- **`X`** (`Value`, init): Gets the data x, for the Variable input method.
- **`Trials`** (`Value`, init): Gets N, the number of trials of a binomial distribution: a whole number, 0 or more.
- **`Probability`** (`Value`, init): Gets p, the probability of success of a binomial distribution: from 0 to 1.
- **`Mean`** (`Value`, init): Gets μ, the mean of a normal distribution.
- **`StandardDeviation`** (`Value`, init): Gets σ, the standard deviation of a normal distribution: more than 0.
- **`Lower`** (`Value`, init): Gets the lower bound of Normal CD.
- **`Upper`** (`Value`, init): Gets the upper bound of Normal CD.
- **`Area`** (`Value`, init): Gets the left-tail area of Inverse Normal: from 0 to 1.
- **`Lambda`** (`Value`, init): Gets λ, the mean of a Poisson distribution: more than 0.

---

### `EngineBudget` Class (record)

The limits on one calculation's work (docs/PRECISION.md §11). Exceeding either is a Time Out, never a hang.

```csharp
public sealed record EngineBudget(long MaxIterations, TimeSpan Timeout)
```

#### Constructors

- **`EngineBudget(long MaxIterations, TimeSpan Timeout)`**
  The limits on one calculation's work (docs/PRECISION.md §11). Exceeding either is a Time Out, never a hang.

#### Properties

- **`MaxIterations`** (`long`, init): The most evaluations of a Σ, Π or ∫ body, summed over the calculation.
- **`Timeout`** (`TimeSpan`, init): The longest a calculation may run.
- **`Default`** (`EngineBudget`, static): Gets the default budget: 10⁸ iterations and 10 seconds.

---

### `EvaluationContext` Class

What a function may know about the calculation it is evaluated in.

```csharp
public sealed class EvaluationContext
```

#### Properties

- **`App`** (`CalculatorApp`): Gets the application the calculation runs in.
- **`Settings`** (`CalculatorSettings`): Gets the settings in effect.
- **`AngleUnit`** (`AngleUnit`): Gets the angle unit in effect.
- **`Profile`** (`CalculatorProfile`): Gets the profile, which decides the calculation range and function domains.
- **`CancellationToken`** (`CancellationToken`): Gets the token that cancels the calculation.

---

### `FormattedResult` Class (record)

A result as the calculator displays it.

```csharp
public sealed record FormattedResult(string Text, string Latex)
```

#### Constructors

- **`FormattedResult(string Text, string Latex)`**
  A result as the calculator displays it.

#### Properties

- **`Text`** (`string`, init): The display in the output notation of docs/LINEAR-SYNTAX.md §7: `13⌟6`, `45√(3)+10√(2)`, `1.67×10^-1`, `2∠45`. Several results are separated by `, ` (`; ` with a comma decimal mark).
- **`Latex`** (`string`, init): The same display in LaTeX, for copy and export.

---

### `FunctionSignature` Class

The name, arity and availability of a plugin function.

```csharp
public sealed class FunctionSignature
```

#### Constructors

- **`FunctionSignature(string name, int minimumArity, int maximumArity, params ReadOnlySpan<CalculatorApp> applications)`**
  Initializes a signature.

#### Properties

- **`Name`** (`string`): Gets the name as written.
- **`MinimumArity`** (`int`): Gets the fewest arguments.
- **`MaximumArity`** (`int`): Gets the most arguments.
- **`Applications`** (`ImmutableArray<CalculatorApp>`): Gets the applications the function is available in; empty for all.

---

### `InequalitySolution` Class

What `CalculatorSession.SolveInequality` found for a polynomial inequality (manual pp. 124-125).

```csharp
public sealed class InequalitySolution
```

#### Properties

- **`Outcome`** (`SolutionOutcome`): Gets whether the inequality has a solution, none, or every real number.
- **`Intervals`** (`ImmutableArray<SolutionInterval>`): Gets the stretches that satisfy it, in increasing order; empty unless `InequalitySolution.Outcome` is solved.
- **`Text`** (`string`): Gets the stretches as the calculator writes them, separated by the list separator; empty unless solved.
- **`Error`** (`CalcError?`): Gets the error, or `null`.
- **`Succeeded`** (`bool`): Gets whether the calculation ran to an answer, which may be that nothing satisfies it.

---

### `MatrixSnapshot` Class (record)

A matrix or a vector of a `SessionSnapshot`: its size and its entries, row by row.

```csharp
public sealed record MatrixSnapshot(int Rows, int Columns, ImmutableArray<string> Entries)
```

#### Constructors

- **`MatrixSnapshot(int Rows, int Columns, ImmutableArray<string> Entries)`**
  A matrix or a vector of a `SessionSnapshot`: its size and its entries, row by row.

#### Properties

- **`Rows`** (`int`, init): The rows; 1 for a vector.
- **`Columns`** (`int`, init): The columns; the dimension of a vector.
- **`Entries`** (`ImmutableArray<string>`, init): The entries, row by row.

---

### `MatrixValue` Class

An immutable matrix of real values, as the Matrix application holds in MatA-MatD and MatAns (manual pp. 132-139).

```csharp
public sealed class MatrixValue : IEquatable<MatrixValue>
```

#### Remarks

Each entry is a `Value`, so it follows the precision rule and keeps an exact form: the entries of `MatA⁻¹` for an exact MatA are exact decimals, and `√(2)` squared is exactly 2 inside a matrix too. The size limits belong to the profile and are checked where matrices enter a session.

#### Constructors

- **`MatrixValue(Value[,] entries)`**
  Initializes a matrix.

#### Properties

- **`Rows`** (`int`): Gets the number of rows.
- **`Columns`** (`int`): Gets the number of columns.
- **`IsSquare`** (`bool`): Gets whether the matrix has as many rows as columns.

#### Indexers

- **`this[int row, int column]`** (`Value`): Gets an entry.

#### Methods

- **`ToArray()`** → `Value[,]`
  Returns the entries as a new array.

---

### `NumberLine` Class

The Number Line application of Math Box: up to three expressions, each drawn as a part of the x axis, and the View-Window they are drawn in (manual pp. 153-157).

```csharp
public static class NumberLine
```

#### Remarks

Nothing here depends on a session - a number line calculates nothing - so it is a set of functions rather than a form of `CalculatorSession`. Which three expressions are registered, and clearing them when the angle unit changes (p. 155), is the application's.

#### Fields

- **`MaximumAxes`** = `3` (`int`, const): The most expressions a number line registers: axes A, B and C (p. 153).

#### Methods

- **`Define(NumberLineForm form, Value a, Value? b = null)`** *(static)* → `NumberLineAxis`
  Registers an expression.
- **`View(Value center, Value scale)`** *(static)* → `NumberLineView`
  Sets the View-Window by hand (p. 156).
- **`Fit(IReadOnlyList<NumberLineAxis> axes)`** *(static)* → `NumberLineView`
  The View-Window the calculator sets when the expressions are drawn, before any is set by hand (p. 156).

---

### `NumberLineAxis` Class

One expression of the Number Line application and the part of the x axis it covers (manual pp. 153-155).

```csharp
public sealed class NumberLineAxis
```

#### Remarks

What the calculator draws: a bound that is part of the set as a filled dot, one that is not as an open circle, and a side without a bound as an arrow to the edge of the view (p. 155). `NumberLineAxis.Lower` and `NumberLineAxis.Upper` carry exactly that: a bound or `null` for the arrow, and whether the bound is included.

#### Properties

- **`Form`** (`NumberLineForm`): Gets the form of the expression.
- **`A`** (`Value`): Gets a.
- **`B`** (`Value?`): Gets b, for the four forms that have one; otherwise `null`.
- **`Error`** (`CalcError?`): Gets the Range ERROR of p. 165 when a or b is beyond ±10¹⁰ or a is not below b; otherwise `null`.
- **`Succeeded`** (`bool`): Gets whether the expression is one the calculator draws.
- **`Lower`** (`Value?`): Gets the lower end of the set, or `null` when it goes on to the left.
- **`LowerIncluded`** (`bool`): Gets whether `NumberLineAxis.Lower` is part of the set.
- **`Upper`** (`Value?`): Gets the upper end of the set, or `null` when it goes on to the right.
- **`UpperIncluded`** (`bool`): Gets whether `NumberLineAxis.Upper` is part of the set.

---

### `NumberLineView` Class (record)

The View-Window of the Number Line application: the middle of the x axis and its scale (manual pp. 156-157).

```csharp
public sealed record NumberLineView(decimal Center, decimal Scale, CalcError? Error = null)
```

#### Remarks

The axis shows eight ticks on either side of the center: from Center − 8 × Scale to Center + 8 × Scale (p. 157).

#### Constructors

- **`NumberLineView(decimal Center, decimal Scale, CalcError? Error = null)`**
  The View-Window of the Number Line application: the middle of the x axis and its scale (manual pp. 156-157).

#### Fields

- **`Ticks`** = `8` (`int`, const): The ticks on either side of the center.

#### Properties

- **`Center`** (`decimal`, init): The value in the middle of the axis.
- **`Scale`** (`decimal`, init): The distance between two ticks.
- **`Error`** (`CalcError?`, init): The Range ERROR of p. 165 when the scale or the center is out of its range; otherwise `null`.
- **`Minimum`** (`decimal`): Gets the value at the left end of the axis.
- **`Maximum`** (`decimal`): Gets the value at the right end of the axis.
- **`Succeeded`** (`bool`): Gets whether the view is one the calculator draws.

---

### `PallasEngine` Class

The calculation engine: an immutable catalog of functions and data from which calculator sessions are created.

```csharp
public sealed class PallasEngine
```

#### Properties

- **`Vocabulary`** (`SyntaxVocabulary`): Gets the vocabulary: the reference calculator's names and the plugin functions.
- **`Budget`** (`EngineBudget`): Gets the default budget of a calculation.

#### Methods

- **`CreateSession(CalculatorApp app = CalculatorApp.Calculate, CalculatorProfile profile = CalculatorProfile.Standard, int? randomSeed = null)`** → `CalculatorSession`
  Creates a calculator session: its own memory, history and settings.
- **`Format(Value value, CalculatorSettings settings, CalculatorProfile profile = CalculatorProfile.Standard, FormatTarget? target = null)`** *(static)* → `FormattedResult?`
  Displays a value, such as a variable in the variable list.

---

### `PallasEngineBuilder` Class

Configures and builds a `PallasEngine`: plugin functions, data sets and the budget.

```csharp
public sealed class PallasEngineBuilder
```

#### Remarks

The engine has the reference calculator's functions but no reference data: scientific constants, unit conversions and atomic weights come from data sets, such as those of Barbatos.Pallas.Data, added here (decision of 18 Sep 2026).

#### Methods

- **`CreateDefault()`** *(static)* → `PallasEngineBuilder`
  Returns a builder for an engine with the reference calculator's functions.
- **`AddFunction(IMathFunction function)`** → `PallasEngineBuilder`
  Adds a plugin function.
- **`AddConstantSet(ConstantSet constants)`** → `PallasEngineBuilder`
  Adds a set of scientific constants; a later set replaces constants with the same symbol.
- **`AddUnitSet(UnitSet units)`** → `PallasEngineBuilder`
  Adds a set of unit conversions; a later set replaces conversions with the same command.
- **`AddAtomicWeights(AtomicWeightTable atomicWeights)`** → `PallasEngineBuilder`
  Sets the atomic weights `AtWt(` returns.
- **`WithBudget(EngineBudget budget)`** → `PallasEngineBuilder`
  Sets the default budget of each calculation.
- **`Build()`** → `PallasEngine`
  Builds the engine.

---

### `PolynomialExtremum` Class

A local minimum or maximum of a polynomial, where the calculator shows its coordinates (manual p. 117).

```csharp
public sealed class PolynomialExtremum
```

#### Properties

- **`Kind`** (`ExtremumKind`): Gets whether this is the minimum or the maximum.
- **`X`** (`Calculation`): Gets where it is.
- **`Y`** (`Calculation`): Gets the value of the polynomial there.

---

### `PolynomialRoot` Class

One root of a polynomial: its real part and, for a complex root, its imaginary part (manual pp. 116-118).

```csharp
public sealed class PolynomialRoot
```

#### Remarks

The two parts are separate values because the calculator displays them in exact form, as `-3⌟4+√(23)⌟4i` on p. 118: a complex `Value` holds two `Double` numbers and cannot carry those forms. The application writes the root as the real part, the sign of the imaginary part, its magnitude and `i`.

#### Properties

- **`Real`** (`Calculation`): Gets the real part.
- **`Imaginary`** (`Calculation?`): Gets the imaginary part, or `null` for a real root.
- **`IsReal`** (`bool`): Gets whether the root is a real number.

---

### `PolynomialSolution` Class

What `CalculatorSession.SolvePolynomial` found for a polynomial of degree 2 to 4 (manual pp. 116-119).

```csharp
public sealed class PolynomialSolution
```

#### Properties

- **`Outcome`** (`SolutionOutcome`): Gets whether the polynomial has roots to show, or only complex ones while Complex Roots is off.
- **`Roots`** (`ImmutableArray<PolynomialRoot>`): Gets the roots, as many as the degree, by decreasing real part and then by decreasing imaginary part; a repeated root appears as many times as it is a root.
- **`Extrema`** (`ImmutableArray<PolynomialExtremum>`): Gets the local extrema, by increasing x: one for degree 2, two or none for degree 3, none for degree 4. Empty is the calculator's "No Local Max/Min" (p. 117).
- **`Error`** (`CalcError?`): Gets the error, or `null`.
- **`Succeeded`** (`bool`): Gets whether the calculation ran to an answer.

---

### `ScientificConstant` Class

A physical constant of a `ConstantSet`.

```csharp
public sealed class ScientificConstant
```

#### Constructors

- **`ScientificConstant(string symbol, ScaledDecimal value, ScaledDecimal standardUncertainty, string unit)`**
  Initializes a constant.

#### Properties

- **`Symbol`** (`string`): Gets the name as written, starting with `@`.
- **`Value`** (`ScaledDecimal`): Gets the value.
- **`StandardUncertainty`** (`ScaledDecimal`): Gets the standard uncertainty; zero when the value is exact.
- **`Unit`** (`string`): Gets the SI unit.
- **`IsExact`** (`bool`): Gets whether the value is exact by definition.

---

### `SessionSnapshot` Class (record)

Everything a `CalculatorSession` holds, as text and settings an application can store and read back (`CalculatorSession.Capture`, `CalculatorSession.Restore`).

```csharp
public sealed record SessionSnapshot
```

#### Remarks

A calculator keeps its memory, its settings and its data when it is switched off; an application does the same by writing a snapshot to its preferences. Everything here is a string, an enum or a settings record, so the application can store it however it likes without knowing what a `Value` is; the engine writes the values with all of their digits, and with their exact form where they have one, so a restored session holds what was captured rather than what a display would have shown.

The history is not part of a snapshot: it is a list of what was calculated in this run, and an application that wants to keep it across runs keeps the inputs and their displays itself.

#### Fields

- **`CurrentVersion`** = `1` (`int`, const): The version of this format; a snapshot of a later version is refused.

#### Properties

- **`Version`** (`int`, init): Gets the format version.
- **`App`** (`CalculatorApp`, init): Gets the application the session was in.
- **`Settings`** (`CalculatorSettings`, init): Gets the settings.
- **`Regression`** (`RegressionModel`, init): Gets the regression of two-variable statistics.
- **`Variables`** (`ImmutableArray<string>`, init): Gets the variables A, B, C, D, E, F, x, y and z, in that order.
- **`Ans`** (`string`, init): Gets the last result (Ans).
- **`PreAns`** (`string`, init): Gets the result before the last one (PreAns).
- **`Matrices`** (`ImmutableArray<MatrixSnapshot?>`, init): Gets MatA to MatD, each `null` where no matrix is stored.
- **`Vectors`** (`ImmutableArray<MatrixSnapshot?>`, init): Gets VctA to VctD, each `null` where no vector is stored.
- **`FunctionF`** (`string?`, init): Gets f(x) as it was defined, or `null`.
- **`FunctionG`** (`string?`, init): Gets g(x) as it was defined, or `null`.
- **`StatisticsX`** (`ImmutableArray<string>`, init): Gets the x column of the statistics data.
- **`StatisticsY`** (`ImmutableArray<string>`, init): Gets the y column, empty for one-variable data.
- **`StatisticsFrequencies`** (`ImmutableArray<string>`, init): Gets the frequency column, empty when the data have none.

---

### `Simulation` Class

A Dice Roll or Coin Toss of the Math Box application: every attempt, and what the List and Relative Freq screens show of them (manual pp. 147-153).

```csharp
public sealed class Simulation
```

#### Fields

- **`MinimumCount`** = `1` (`int`, const): The fewest dice or coins a simulation throws.
- **`MaximumCount`** = `3` (`int`, const): The most dice or coins a simulation throws.
- **`MaximumAttempts`** = `250` (`int`, const): The most attempts a simulation makes (p. 148).

#### Properties

- **`Kind`** (`SimulationKind`): Gets whether the simulation threw dice or coins.
- **`Count`** (`int`): Gets how many dice or coins each attempt threw.
- **`Attempts`** (`ImmutableArray<ImmutableArray<int>>`): Gets every attempt, in order, as the List screen shows them: the face of each die (1 to 6), or each coin (1 for heads, 0 for tails), in the columns A, B and C (pp. 149, 153).
- **`Error`** (`CalcError?`): Gets the Range ERROR when the number of attempts was not a whole number from 1 to 250 (p. 164).
- **`Succeeded`** (`bool`): Gets whether the simulation ran.

#### Methods

- **`Sum(int attempt)`** → `int`
  Returns the Sum column of an attempt: its one die, or the dice added up; for coins, how many were heads.
- **`Difference(int attempt)`** → `int`
  Returns the Diff column of an attempt of two dice: how far apart they landed (p. 149).
- **`Frequencies(SimulationTally tally)`** → `ImmutableArray<SimulationFrequency>`
  Counts the attempts by their outcome, as the Relative Freq screen does (pp. 150, 153).

---

### `SimulationFrequency` Class (record)

One row of the Relative Freq screen of a simulation (manual pp. 150, 153).

```csharp
public sealed record SimulationFrequency(int Outcome, int Frequency, Value RelativeFrequency)
```

#### Constructors

- **`SimulationFrequency(int Outcome, int Frequency, Value RelativeFrequency)`**
  One row of the Relative Freq screen of a simulation (manual pp. 150, 153).

#### Properties

- **`Outcome`** (`int`, init): The sum, the difference or the number of heads the row counts.
- **`Frequency`** (`int`, init): How many attempts had that outcome (Freq).
- **`RelativeFrequency`** (`Value`, init): The frequency divided by the number of attempts (Rel Fr), a decimal.

---

### `SimultaneousSolution` Class

What `CalculatorSession.SolveSimultaneous` found for a system of linear equations (manual pp. 114-116).

```csharp
public sealed class SimultaneousSolution
```

#### Properties

- **`Outcome`** (`SolutionOutcome`): Gets what the system has: one solution, none, or infinitely many.
- **`Unknowns`** (`ImmutableArray<Calculation>`): Gets the value of each unknown, named x, y, z and t; empty unless `SimultaneousSolution.Outcome` is solved.
- **`Error`** (`CalcError?`): Gets the error, or `null`.
- **`Succeeded`** (`bool`): Gets whether the calculation ran to an answer, which may be that there is no solution.

---

### `SolutionInterval` Class

One stretch of the numbers that satisfy an inequality, as the calculator writes it: `x≤-3`, `1≤x` or `-3<x<1` (manual pp. 124-125).

```csharp
public sealed class SolutionInterval
```

#### Properties

- **`Lower`** (`Calculation?`): Gets the lower bound, or `null` when the stretch has none.
- **`LowerIncluded`** (`bool`): Gets whether the lower bound is part of the solution.
- **`Upper`** (`Calculation?`): Gets the upper bound, or `null` when the stretch has none.
- **`UpperIncluded`** (`bool`): Gets whether the upper bound is part of the solution.
- **`Text`** (`string`): Gets the stretch as the calculator writes it, with the settings of the calculation.

---

### `StatisticsData` Class

The data of the Statistics application: x values, or (x, y) pairs, with a frequency for each row when the Frequency setting is on (manual pp. 79-83).

```csharp
public sealed class StatisticsData : IEquatable<StatisticsData>
```

#### Remarks

The data are immutable: editing and sorting make new data, which `CalculatorSession.SetStatisticsData` stores. The editor's own rules - switching between one and two variables, or turning the frequency column on or off, clears the data (p. 80) - belong to the host, which decides when to start again from `StatisticsData.Empty`.

Every value is a real number. A frequency may be any real number here; a negative one is a Math ERROR when a statistic is calculated, a row with frequency 0 counts for nothing, and quartiles need whole frequencies (assumption U22).

#### Constructors

- **`StatisticsData(IEnumerable<Value> x, IEnumerable<Value>? y = null, IEnumerable<Value>? frequencies = null)`**
  Initializes data.

#### Properties

- **`Empty`** (`StatisticsData`, static): Gets one-variable data with no rows and no frequency column.
- **`X`** (`ImmutableArray<Value>`): Gets the x values.
- **`Y`** (`ImmutableArray<Value>`): Gets the y values; empty for one-variable data.
- **`Frequencies`** (`ImmutableArray<Value>`): Gets the frequencies; empty without the frequency column.
- **`IsTwoVariable`** (`bool`): Gets whether the data are (x, y) pairs.
- **`HasFrequencies`** (`bool`): Gets whether each row has a frequency.
- **`Rows`** (`int`): Gets the number of rows.
- **`Columns`** (`int`): Gets the number of columns, 1 to 3, which sets how many rows the editor holds (p. 80).

#### Methods

- **`Sort(StatisticsColumn column, bool descending = false)`** → `StatisticsData`
  Returns the data sorted by a column, rows kept together (p. 82). Equal values keep their order.

---

### `UnitConversion` Class

A unit conversion command, such as `cm▶in`: the result is `(value + OffsetBefore) × Multiplier ÷ Divisor + OffsetAfter`.

```csharp
public sealed class UnitConversion
```

#### Remarks

A multiplier and a divisor rather than one factor keep exact definitions exact. Centimetres to inches divide by 2.54, so `5cm▶in` is the `Decimal` quotient 5 ÷ 2.54, which displays as 250⌟127; Fahrenheit to Celsius is `(value − 32) × 5 ÷ 9`.

#### Constructors

- **`UnitConversion(string command, decimal multiplier, decimal divisor = 1, decimal offsetBefore = 0, decimal offsetAfter = 0)`**
  Initializes a conversion.

#### Properties

- **`Command`** (`string`): Gets the command as written.
- **`Multiplier`** (`decimal`): Gets the multiplier.
- **`Divisor`** (`decimal`): Gets the divisor.
- **`OffsetBefore`** (`decimal`): Gets the offset added before scaling.
- **`OffsetAfter`** (`decimal`): Gets the offset added after scaling.

---

### `UnitSet` Class

A set of unit conversion commands, such as those of NIST SP 811.

```csharp
public sealed class UnitSet
```

#### Constructors

- **`UnitSet(string name, IEnumerable<UnitConversion> conversions)`**
  Initializes a set.

#### Properties

- **`Name`** (`string`): Gets the name of the set.
- **`Conversions`** (`ImmutableArray<UnitConversion>`): Gets the conversions.

---

### `VectorValue` Class

An immutable vector of real values, as the Vector application holds in VctA-VctD and VctAns (manual pp. 139-145).

```csharp
public sealed class VectorValue : IEquatable<VectorValue>
```

#### Remarks

Each element is a `Value`: the length of (3, 4) is exactly 5, and that of (1, 1) is √2 with its exact form. The dimension limits belong to the profile and are checked where vectors enter a session.

#### Constructors

- **`VectorValue(params ReadOnlySpan<Value> elements)`**
  Initializes a vector.

#### Properties

- **`Dimension`** (`int`): Gets the number of elements.
- **`Elements`** (`ImmutableArray<Value>`): Gets the elements.

#### Indexers

- **`this[int index]`** (`Value`): Gets an element.

---

### `CalcErrorKind` Enum

The errors of the reference calculator (manual pp. 162-166), without their text.

```csharp
public enum CalcErrorKind
```

#### Remarks

The engine is language-neutral: an application turns a kind into English or Vietnamese text. Every error also carries the span where the calculator would place the cursor (`CalcError`).

#### Fields

- **`SyntaxError`** (`0`): Syntax ERROR: the expression is malformed, or a function has the wrong number of arguments.
- **`MathError`** (`1`): Math ERROR: a result outside the calculation range, an input outside a function's domain, or division by zero.
- **`StackError`** (`2`): Stack ERROR: the expression nests deeper than the calculator's stacks.
- **`ArgumentError`** (`3`): Argument ERROR: an argument of the wrong kind.
- **`DimensionError`** (`4`): Dimension ERROR: incompatible matrix or vector sizes.
- **`VariableError`** (`5`): Variable ERROR: a Solver expression without a variable.
- **`CannotSolve`** (`6`): Cannot Solve: the Solver did not converge.
- **`RangeError`** (`7`): Range ERROR: a table, fill range or Math Box input out of range.
- **`TimeOut`** (`8`): Time Out: a derivative, integral or other iteration did not meet its end condition within the budget.
- **`CircularError`** (`9`): Circular ERROR: f(x) and g(x), or spreadsheet cells, refer to each other.
- **`MemoryError`** (`10`): Memory ERROR: the spreadsheet's capacity is exceeded.
- **`NoOperator`** (`11`): No Operator: Verify on an expression without a relational operator.
- **`NotDefined`** (`12`): Not Defined: f(x), g(x), a matrix, a vector or a data set used before it is defined.

---

### `CalculationKind` Enum

What a `Calculation` produced.

```csharp
public enum CalculationKind
```

#### Fields

- **`Value`** (`0`): One value.
- **`Verify`** (`1`): A Verify result: `Calculation.IsTrue`, with 1 or 0 in Ans (p. 76).
- **`Remainder`** (`2`): A division with remainder: the quotient and the remainder (p. 56).
- **`Polar`** (`3`): Pol(: r and θ (p. 62).
- **`Rectangular`** (`4`): Rec(: x and y (p. 62).
- **`Solution`** (`5`): A Solver result: the solution, with Left − Right in `Calculation.Second` (p. 121).

---

### `CalculatorProfile` Enum

How closely the engine follows the reference calculator's limits (docs/PRECISION.md §10). A profile changes limits and formatting, never how a value is computed.

```csharp
public enum CalculatorProfile
```

#### Fields

- **`Standard`** (`0`): The calculator's limits: the calculation range ±10⁻⁹⁹ to ±9.999999999×10⁹⁹, the function domains of pp. 170-171, and the calculator's display bounds.
- **`Extended`** (`1`): The range of `Double` and wider display bounds, for engineering and accounting work.

---

### `CircleKind` Enum

The circle an angle of the Circle application is drawn on (manual pp. 157-159).

```csharp
public enum CircleKind
```

#### Fields

- **`UnitCircle`** (`0`): Unit Circle: the whole circle of radius 1; an angle between -10000 and 10000 in any unit.
- **`HalfCircle`** (`1`): Half Circle: its upper half; an angle from 0 to 180°, π or 200 gradians.

---

### `ComplexResult` Enum

The Complex Result setting (manual p. 24).

```csharp
public enum ComplexResult
```

#### Fields

- **`Rectangular`** (`0`): a+bi, the initial setting.
- **`Polar`** (`1`): r∠θ, with θ in (−180°, 180°].

---

### `DecimalMark` Enum

The Decimal Mark setting (manual p. 24-25).

```csharp
public enum DecimalMark
```

#### Fields

- **`Dot`** (`0`): A dot, the initial setting; several results are separated by commas.
- **`Comma`** (`1`): A comma; several results are separated by semicolons.

---

### `DefinedFunction` Enum

The two functions a user can define (manual pp. 70-72).

```csharp
public enum DefinedFunction
```

#### Fields

- **`F`** (`0`): f(x).
- **`G`** (`1`): g(x).

---

### `DistributionKind` Enum

The calculation types of the Distribution application (manual p. 96).

```csharp
public enum DistributionKind
```

#### Fields

- **`BinomialPD`** (`0`): Binomial PD: the probability of x successes in N trials, P(X = x).
- **`BinomialCD`** (`1`): Binomial CD: the probability of at most x successes in N trials, P(X ≤ x).
- **`NormalPD`** (`2`): Normal PD: the normal probability density at x.
- **`NormalCD`** (`3`): Normal CD: the normal probability of Lower ≤ X ≤ Upper.
- **`InverseNormal`** (`4`): Inverse Normal: the x whose left-tail area is Area.
- **`PoissonPD`** (`5`): Poisson PD: the probability P(X = x) of a Poisson variable with mean λ.
- **`PoissonCD`** (`6`): Poisson CD: the probability P(X ≤ x) of a Poisson variable with mean λ.

---

### `ExtremumKind` Enum

Which extremum of a polynomial a `PolynomialExtremum` is (manual p. 117).

```csharp
public enum ExtremumKind
```

#### Fields

- **`Minimum`** (`0`): A local minimum.
- **`Maximum`** (`1`): A local maximum.

---

### `FormatTarget` Enum

The conversions of the FORMAT menu (manual pp. 42-50).

```csharp
public enum FormatTarget
```

#### Fields

- **`Standard`** (`0`): Standard: fractions, square roots and π where possible.
- **`DecimalValue`** (`1`): Decimal.
- **`PrimeFactor`** (`2`): Prime factorization of a positive integer of at most 10 digits.
- **`RecurringDecimal`** (`3`): Recurring decimal: `3.(3)`.
- **`Rectangular`** (`4`): Rectangular coordinates of a complex number: a+bi.
- **`Polar`** (`5`): Polar coordinates of a complex number: r∠θ.
- **`ImproperFraction`** (`6`): Improper fraction: `13⌟4`.
- **`MixedFraction`** (`7`): Mixed fraction: `3⌟1⌟4`.
- **`Engineering`** (`8`): Engineering notation, with an exponent that is a multiple of 3.
- **`Sexagesimal`** (`9`): Degrees, minutes and seconds.

---

### `FractionResult` Enum

The Fraction Result setting (manual p. 24).

```csharp
public enum FractionResult
```

#### Fields

- **`Improper`** (`0`): Improper fractions, the initial setting: 13⌟4.
- **`Mixed`** (`1`): Mixed fractions: 3⌟1⌟4.

---

### `InputOutput` Enum

The Input/Output setting (manual p. 22). Input is always Canonical Linear Syntax in the engine; the setting decides the forms a result may be displayed in.

```csharp
public enum InputOutput
```

#### Fields

- **`MathIMathO`** (`0`): MathI/MathO, the initial setting: fractions, square roots and π forms where possible.
- **`MathIDecimalO`** (`1`): MathI/DecimalO: decimals.
- **`LineILineO`** (`2`): LineI/LineO: decimals or fractions.
- **`LineIDecimalO`** (`3`): LineI/DecimalO: decimals.

---

### `MatrixVariable` Enum

The matrix variables of the Matrix application (manual pp. 132-136).

```csharp
public enum MatrixVariable
```

#### Fields

- **`MatA`** (`0`): MatA.
- **`MatB`** (`1`): MatB.
- **`MatC`** (`2`): MatC.
- **`MatD`** (`3`): MatD.

---

### `MemoryVariable` Enum

The variables of the calculator (manual p. 38), shared by every application.

```csharp
public enum MemoryVariable
```

#### Fields

- **`A`** (`0`): A.
- **`B`** (`1`): B.
- **`C`** (`2`): C.
- **`D`** (`3`): D.
- **`E`** (`4`): E; also the quotient of ÷R.
- **`F`** (`5`): F; also the remainder of ÷R.
- **`X`** (`6`): x; also r of Pol( and x of Rec(.
- **`Y`** (`7`): y; also θ of Pol( and y of Rec(.
- **`Z`** (`8`): z.

---

### `NumberFormatKind` Enum

The kinds of the Number Format setting (manual p. 23).

```csharp
public enum NumberFormatKind
```

#### Fields

- **`Norm`** (`0`): Norm: 10 significant digits, in exponent form outside a range that depends on the digit count (1 or 2).
- **`Fix`** (`1`): Fix: a fixed number of decimal places, 0 to 9.
- **`Sci`** (`2`): Sci: a fixed number of significant digits in exponent form, 1 to 10.

---

### `NumberLineForm` Enum

The nine forms of a Number Line expression, in the order of the calculator's list (manual p. 153).

```csharp
public enum NumberLineForm
```

#### Fields

- **`Less`** (`0`): x<a.
- **`LessOrEqual`** (`1`): x≤a.
- **`Equal`** (`2`): x=a.
- **`Greater`** (`3`): x>a.
- **`GreaterOrEqual`** (`4`): x≥a.
- **`Between`** (`5`): a<x<b.
- **`FromIncluded`** (`6`): a≤x<b.
- **`ToIncluded`** (`7`): a<x≤b.
- **`BetweenIncluded`** (`8`): a≤x≤b.

---

### `RatioForm` Enum

Which ratio the Ratio application solves for X (manual p. 145).

```csharp
public enum RatioForm
```

#### Fields

- **`XInSecondRatio`** (`0`): A:B = X:D, where X is A·D/B.
- **`XLastInSecondRatio`** (`1`): A:B = C:X, where X is B·C/A.

---

### `RegressionModel` Enum

The regression types of two-variable statistics (manual pp. 86-87), fitted by least squares (pp. 93-95).

```csharp
public enum RegressionModel
```

#### Fields

- **`Linear`** (`0`): y = a + bx.
- **`Quadratic`** (`1`): y = a + bx + cx².
- **`Logarithmic`** (`2`): y = a + b·ln(x), fitted to ln x.
- **`ExponentialE`** (`3`): y = a·e^(bx), fitted to ln y.
- **`ExponentialAB`** (`4`): y = a·b^x, fitted to ln y.
- **`Power`** (`5`): y = a·x^b, fitted to ln x and ln y.
- **`Inverse`** (`6`): y = a + b/x, fitted to 1/x.

---

### `SameResult` Enum

The Same Result setting of a simulation (manual p. 150).

```csharp
public enum SameResult
```

#### Remarks

With a preset, a simulation of the same number of dice or coins and the same number of attempts gives the same results every time and on every copy of Pallas, which is what the setting is for: a class whose calculators all show one result. They are Pallas's own results, not the reference calculator's, whose sequences are not published (deviation D8 of docs/CONFORMANCE.md).

#### Fields

- **`Off`** (`0`): Off, the initial setting: every simulation is random.
- **`First`** (`1`): #1.
- **`Second`** (`2`): #2.
- **`Third`** (`3`): #3.

---

### `SimulationKind` Enum

A probability simulation of the Math Box application (manual pp. 147-153).

```csharp
public enum SimulationKind
```

#### Fields

- **`DiceRoll`** (`0`): Dice Roll: one, two or three dice, each landing on 1 to 6 (p. 147).
- **`CoinToss`** (`1`): Coin Toss: one, two or three coins, each landing heads or tails (p. 150).

---

### `SimulationTally` Enum

What the Relative Freq screen of a simulation counts (manual pp. 148, 150, 153).

```csharp
public enum SimulationTally
```

#### Fields

- **`Sum`** (`0`): The face of one die, or the sum of two or three: 1-6, 2-12 or 3-18.
- **`Difference`** (`1`): The difference between two dice, 0 to 5; two dice only.
- **`Heads`** (`2`): How many coins came up heads, 0 to the number of coins.

---

### `SolutionOutcome` Enum

What an Equation or Inequality calculation found, where the calculator shows a message instead of values (manual pp. 116, 118, 125).

```csharp
public enum SolutionOutcome
```

#### Remarks

The message itself belongs to the application: the core is language-neutral.

#### Fields

- **`Solved`** (`0`): The calculation produced values.
- **`NoSolution`** (`1`): Simultaneous equations with no solution, or an inequality no number satisfies.
- **`InfiniteSolutions`** (`2`): Simultaneous equations satisfied by infinitely many values (p. 116).
- **`NoRealRoots`** (`3`): A polynomial whose roots are all complex, with Complex Roots off (p. 118).
- **`AllRealNumbers`** (`4`): An inequality every real number satisfies (p. 125).

---

### `StatisticsColumn` Enum

A column of the Statistics editor (manual p. 80).

```csharp
public enum StatisticsColumn
```

#### Fields

- **`X`** (`0`): The x values.
- **`Y`** (`1`): The y values of two-variable data.
- **`Frequency`** (`2`): The frequencies, when the Frequency setting is on.

---

### `ValueKind` Enum

The .NET type that holds a `Value` (docs/PRECISION.md §3).

```csharp
public enum ValueKind
```

#### Fields

- **`DecimalReal`** (`0`): A real number held as `Decimal`: the calculator's number.
- **`DoubleReal`** (`1`): A real number held as `Double`, because `Decimal` would keep fewer than 15 significant digits of it.
- **`Complex`** (`2`): A complex number, held as `Complex`, in the Complex application.
- **`BaseN`** (`3`): A 32-bit two's complement integer, in the Base-N application.
- **`Matrix`** (`4`): A matrix of real values (`MatrixValue`), in the Matrix application.
- **`Vector`** (`5`): A vector of real values (`VectorValue`), in the Vector application.

---

### `VectorVariable` Enum

The vector variables of the Vector application (manual pp. 139-143).

```csharp
public enum VectorVariable
```

#### Fields

- **`VctA`** (`0`): VctA.
- **`VctB`** (`1`): VctB.
- **`VctC`** (`2`): VctC.
- **`VctD`** (`3`): VctD.

---

### `IMathFunction` Interface

A function added to the engine by a plugin, such as a beam deflection formula.

```csharp
public interface IMathFunction
```

#### Remarks

A function is registered with `PallasEngineBuilder.AddFunction`, which adds its name to the vocabulary. The engine checks the number of arguments before calling `IMathFunction.Invoke`, and checks the result: a value outside the profile's calculation range becomes a Math ERROR.

#### Properties

- **`Signature`** (`FunctionSignature`): Gets the name, arity and availability of the function.

#### Methods

- **`Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)`** → `EvalResult`
  Evaluates the function.

---

### `CalcError` Struct

An error of a calculation: what went wrong and where.

```csharp
public readonly struct CalcError : IEquatable<CalcError>
```

#### Constructors

- **`CalcError(CalcErrorKind Kind, SourceSpan Span, SyntaxErrorCode? SyntaxCode = null)`**
  An error of a calculation: what went wrong and where.

#### Properties

- **`Kind`** (`CalcErrorKind`, init): The calculator error.
- **`Span`** (`SourceSpan`, init): The part of the input the error belongs to, where the calculator places the cursor (manual p. 162).
- **`SyntaxCode`** (`SyntaxErrorCode?`, init): For an error found while parsing, the parser's more specific code; otherwise `null`.

---

### `CellAddress` Struct

One cell of the Spreadsheet application, by column and row (manual pp. 100-101).

```csharp
public readonly struct CellAddress : IEquatable<CellAddress>
```

#### Remarks

Both are counted from 0, as the grid holds them; `CellAddress.ToString` writes the calculator's name, `A1`. A reference may be written with a dollar sign before the column, the row or both (`$A$1`), which says what a paste leaves alone; the dollar signs belong to the formula, not to the cell, so they are not part of this.

#### Constructors

- **`CellAddress(int Column, int Row)`**
  One cell of the Spreadsheet application, by column and row (manual pp. 100-101).

#### Properties

- **`Column`** (`int`, init): The column, counted from 0: A is 0.
- **`Row`** (`int`, init): The row, counted from 0: row 1 is 0.

#### Methods

- **`TryParse(string? text, out CellAddress address)`** *(static)* → `bool`
  Reads a cell name, with or without dollar signs.
- **`Offset(int columns, int rows)`** → `CellAddress`
  Returns the cell this many columns and rows away, which may be outside any grid.
- **`IsInside(int columns, int rows)`** → `bool`
  Gets whether the cell is inside a grid of this size.

---

### `EvalResult` Struct

The outcome of evaluating one operation or function: a value, or a calculator error.

```csharp
public readonly struct EvalResult : IEquatable<EvalResult>
```

#### Remarks

Errors are returned rather than thrown, so a plugin function reports a Math ERROR the same way the built-in functions do. The engine attaches the source span.

#### Properties

- **`Value`** (`Value`): Gets the value; meaningful only when `EvalResult.Succeeded`.
- **`Error`** (`CalcErrorKind?`): Gets the error, or `null` on success.
- **`Succeeded`** (`bool`): Gets whether the operation produced a value.

#### Methods

- **`Success(Value value)`** *(static)* → `EvalResult`
  Returns a successful result.
- **`Failure(CalcErrorKind error)`** *(static)* → `EvalResult`
  Returns a failed result.
- **`FromValue(Value value)`** *(static)* → `EvalResult`
  Returns a successful result; the named alternative to the implicit conversion.

#### Operators

- **`implicit operator EvalResult(Value value)`**
  Returns a successful result.

---

### `IntegralEstimate` Struct

The error estimate of one numerical integration in a calculation (invariant I7 of docs/PRECISION.md).

```csharp
public readonly struct IntegralEstimate : IEquatable<IntegralEstimate>
```

#### Constructors

- **`IntegralEstimate(SourceSpan Span, double Result, double ErrorEstimate)`**
  The error estimate of one numerical integration in a calculation (invariant I7 of docs/PRECISION.md).

#### Properties

- **`Span`** (`SourceSpan`, init): The `∫(` call in the input.
- **`Result`** (`double`, init): The computed integral, in `Double`.
- **`ErrorEstimate`** (`double`, init): The estimated absolute error of `Result`.

---

### `NumberFormat` Struct

The Number Format setting (manual p. 23): Norm 1, Norm 2, Fix 0-9 or Sci 1-10.

```csharp
public readonly struct NumberFormat : IEquatable<NumberFormat>
```

#### Properties

- **`Norm1`** (`NumberFormat`, static): Gets Norm 1, the initial setting: exponent form when |x| < 10⁻² or |x| ≥ 10¹⁰.
- **`Norm2`** (`NumberFormat`, static): Gets Norm 2: exponent form when |x| < 10⁻⁹ or |x| ≥ 10¹⁰.
- **`Kind`** (`NumberFormatKind`): Gets the kind.
- **`Digits`** (`int`): Gets the digit count: 1 or 2 for Norm, the decimal places for Fix, the significant digits for Sci.

#### Methods

- **`Fix(int decimalPlaces)`** *(static)* → `NumberFormat`
  Returns Fix with the given number of decimal places.
- **`Sci(int significantDigits)`** *(static)* → `NumberFormat`
  Returns Sci with the given number of significant digits.

---

### `ScaledDecimal` Struct

A number written as a `Decimal` mantissa times a power of ten, so reference data keeps every published digit without `Double`.

```csharp
public readonly struct ScaledDecimal : IEquatable<ScaledDecimal>
```

#### Remarks

`Decimal` alone cannot hold 6.62607015×10⁻³⁴ (it would be 0), and `Double` is not allowed in data packages (docs/PRECISION.md §7). The engine turns the number into a `Value` with the precision rule.

#### Constructors

- **`ScaledDecimal(decimal Mantissa, int Exponent)`**
  A number written as a `Decimal` mantissa times a power of ten, so reference data keeps every published digit without `Double`.

#### Properties

- **`Mantissa`** (`decimal`, init): The mantissa: `6.62607015` for the Planck constant.
- **`Exponent`** (`int`, init): The power of ten: `-34` for the Planck constant.

#### Methods

- **`FromDecimal(decimal value)`** *(static)* → `ScaledDecimal`
  Returns a scaled decimal with exponent 0.

#### Operators

- **`implicit operator ScaledDecimal(decimal value)`**
  Returns a scaled decimal with exponent 0.

---

### `Value` Struct

A calculator value: a real number held as `Decimal` or `Double`, a complex number, a 32-bit Base-N integer, or a matrix or vector of real numbers.

```csharp
public readonly struct Value : IEquatable<Value>
```

#### Remarks

Which type holds a value follows docs/PRECISION.md §3. Arithmetic on decimal input stays `Decimal`. A `Double` result, such as a sine, becomes a `Decimal` of 15 significant digits when its magnitude is at least 10⁻¹⁴ and below 7.9×10²⁸. Outside that range it stays `Double`, because `Decimal` would keep fewer than 15 significant digits of it, or overflow.

A value computed exactly from square roots and π also remembers its exact form, so that `√(2)×√(2)` is exactly 2 and `π÷2` is exactly a right angle for the trigonometric functions (decision of 18 Sep 2026).

#### Properties

- **`Zero`** (`Value`, static): Gets exact zero.
- **`One`** (`Value`, static): Gets exact one.
- **`Pi`** (`Value`, static): Gets π, from `Double.Pi`, with its exact form.
- **`E`** (`Value`, static): Gets e, from `Double.E`.
- **`Kind`** (`ValueKind`): Gets the type that holds the value.
- **`IsExact`** (`bool`): Gets whether the value is exact: a `Decimal` computed from decimal input without passing through `Double` (rounded at most at `Decimal`'s 28th digit), or a Base-N integer.
- **`IsReal`** (`bool`): Gets whether the value is a real number, held as `Decimal` or `Double`.

#### Methods

- **`FromDecimal(decimal value)`** *(static)* → `Value`
  Creates an exact value from a `Decimal`.
- **`FromDouble(double value)`** *(static)* → `Value`
  Creates an approximate value from a `Double`, applying the precision rule.
- **`FromComplex(Complex value)`** *(static)* → `Value`
  Creates a complex value.
- **`FromMatrix(MatrixValue matrix)`** *(static)* → `Value`
  Creates a matrix value.
- **`FromVector(VectorValue vector)`** *(static)* → `Value`
  Creates a vector value.
- **`FromBaseN(int value)`** *(static)* → `Value`
  Creates a Base-N value.
- **`ToDecimal()`** → `decimal`
  Returns the number held as `Decimal`.
- **`ToDouble()`** → `double`
  Returns the real number as `Double`.
- **`ToComplex()`** → `Complex`
  Returns the value as a complex number.
- **`ToMatrix()`** → `MatrixValue`
  Returns the matrix.
- **`ToVector()`** → `VectorValue`
  Returns the vector.
- **`ToInt32()`** → `int`
  Returns the Base-N integer.

---

## `Barbatos.Pallas.Expressions` Namespace

Reads Canonical Linear Syntax, the text form of what is typed on the calculator, into an immutable syntax tree
(`ExpressionParser`, `SyntaxNode`), with the calculator's priorities and implicit multiplication, and prints a
tree back as linear text (`LinearPrinter`) or LaTeX (`LatexPrinter`). The spellings it knows are a
`SyntaxVocabulary`. Parsing never throws for input: an error is a `SyntaxDiagnostic` with the span it is at.

### Classes

| Class | Description |
|-------|-------------|
| [`BaseLiteral`](#baseliteral-class) | A Base-N literal with an explicit base: `d10`, `h1F`, `b101`, `o17` (priority level 5). |
| [`BinaryExpression`](#binaryexpression-class) | An operator between two operands: `a+b`, `2π`, `2⌟3`, `10C4`, `5ˣ√(32)`. |
| [`CellRange`](#cellrange-class) | A Spreadsheet range such as `A1:B5`, the argument of `Sum(` and its siblings. |
| [`CellReference`](#cellreference-class) | A Spreadsheet cell reference: `A1`, `$A1`, `A$1` or `$A$1`. |
| [`ExpressionParser`](#expressionparser-class) | Reads Canonical Linear Syntax into a syntax tree, following the calculation priority of the reference calculator (manual p. 168). |
| [`FunctionCall`](#functioncall-class) | A function with its arguments: `sin(30)`, `log(2,16)`, `Σ(x+1,1,5)`. |
| [`LatexPrinter`](#latexprinter-class) | Writes a syntax tree as LaTeX math, for copying and exporting. |
| [`LinearPrinter`](#linearprinter-class) | Writes a syntax tree as Canonical Linear Syntax. |
| [`MixedFractionExpression`](#mixedfractionexpression-class) | A mixed fraction `a⌟b⌟c`, meaning a + b/c: `1⌟1⌟2` is 1½. |
| [`NameReference`](#namereference-class) | A named value: a constant (`π`, `@h`), a variable (`A`, `x`), a memory (`Ans`), a matrix, a vector or a statistic. |
| [`NegationExpression`](#negationexpression-class) | The negative sign `-x`, priority level 5: `-2²` is `-(2²)` (manual p. 169). |
| [`NumberLiteral`](#numberliteral-class) | A number as written: `12`, `1.25`, `.5`, `3.(021)`, or Base-N digits such as `1F`. |
| [`ParenthesizedExpression`](#parenthesizedexpression-class) | An expression in parentheses, kept so that printing reproduces what was typed: `(3×6)<(2+6)×2`. |
| [`ParseResult`](#parseresult-class) | The outcome of parsing: a syntax tree, or the first error. |
| [`PostfixExpression`](#postfixexpression-class) | An operator after its operand: `x²`, `5!`, `20%`, `30°`, `5.5ŷ`. |
| [`RelationChain`](#relationchain-class) | A Verify expression: operands joined by relational operators, `1≤1<1+1` or `2+3=5≠2+5=8`. |
| [`SexagesimalExpression`](#sexagesimalexpression-class) | Degrees-minutes-seconds: `2°20′30″`. |
| [`SuffixCommandExpression`](#suffixcommandexpression-class) | A named command written after a value: an engineering symbol (`999_k`, level 3) or a unit conversion (`5cm▶in`, level 6). |
| [`SyntaxEquivalence`](#syntaxequivalence-class) | Compares syntax trees by structure. |
| [`SyntaxNode`](#syntaxnode-class) | A node of an immutable syntax tree. |
| [`SyntaxSymbol`](#syntaxsymbol-class) | One spelling the lexer recognizes: a name such as `sin(` or `Ans`, or an operator such as `×`. |
| [`SyntaxVocabulary`](#syntaxvocabulary-class) | The spellings the lexer recognizes: the fixed grammar (operators, punctuation) and the names of functions, constants, variables and commands. |

### Enums

| Enum | Description |
|-------|-------------|
| [`BinaryOperator`](#binaryoperator-enum) | An operator between two operands, with its priority level on the reference calculator (manual p. 168; 1 binds tightest). |
| [`CalculatorApp`](#calculatorapp-enum) | The calculator application an expression is entered in. |
| [`NumberBase`](#numberbase-enum) | The base a Base-N prefix gives to the literal after it (`d10`, `h10`, `b10`, `o10`). |
| [`PostfixOperator`](#postfixoperator-enum) | An operator written after its operand. |
| [`RelationOperator`](#relationoperator-enum) | A relational operator in a Verify expression. Relations bind looser than every other operator. |
| [`SymbolKind`](#symbolkind-enum) | What a `SyntaxSymbol` is. |
| [`SyntaxErrorCode`](#syntaxerrorcode-enum) | Why an expression could not be read. |
| [`TokenKind`](#tokenkind-enum) | What a `Token` is. |

### Structs

| Struct | Description |
|-------|-------------|
| [`ExpressionLexer`](#expressionlexer-struct) | Splits Canonical Linear Syntax into tokens, without allocating. |
| [`SourceSpan`](#sourcespan-struct) | A range of characters in the text that was parsed. |
| [`SyntaxContext`](#syntaxcontext-struct) | The circumstances an expression is read in. |
| [`SyntaxDiagnostic`](#syntaxdiagnostic-struct) | The first error found in an expression, and where it is. |
| [`Token`](#token-struct) | A token: a kind and where it is. The text is the span of the lexed input; nothing is copied. |

---

### `BaseLiteral` Class

A Base-N literal with an explicit base: `d10`, `h1F`, `b101`, `o17` (priority level 5).

```csharp
public sealed class BaseLiteral : SyntaxNode
```

#### Constructors

- **`BaseLiteral(NumberBase numberBase, NumberLiteral digits, SourceSpan span = default)`**
  Initializes a prefixed literal.

#### Properties

- **`Base`** (`NumberBase`): Gets the base.
- **`Digits`** (`NumberLiteral`): Gets the digits.

---

### `BinaryExpression` Class

An operator between two operands: `a+b`, `2π`, `2⌟3`, `10C4`, `5ˣ√(32)`.

```csharp
public sealed class BinaryExpression : SyntaxNode
```

#### Constructors

- **`BinaryExpression(BinaryOperator binaryOperator, SyntaxNode left, SyntaxNode right, SourceSpan span = default)`**
  Initializes a binary expression.

#### Properties

- **`Operator`** (`BinaryOperator`): Gets the operator.
- **`Left`** (`SyntaxNode`): Gets the left operand.
- **`Right`** (`SyntaxNode`): Gets the right operand.

---

### `CellRange` Class

A Spreadsheet range such as `A1:B5`, the argument of `Sum(` and its siblings.

```csharp
public sealed class CellRange : SyntaxNode
```

#### Constructors

- **`CellRange(CellReference start, CellReference end, SourceSpan span = default)`**
  Initializes a range.

#### Properties

- **`Start`** (`CellReference`): Gets the first cell.
- **`End`** (`CellReference`): Gets the last cell.

---

### `CellReference` Class

A Spreadsheet cell reference: `A1`, `$A1`, `A$1` or `$A$1`.

```csharp
public sealed class CellReference : SyntaxNode
```

#### Constructors

- **`CellReference(string text, SourceSpan span = default)`**
  Initializes a reference.

#### Properties

- **`Text`** (`string`): Gets the reference as written, including any `$`.

---

### `ExpressionParser` Class

Reads Canonical Linear Syntax into a syntax tree, following the calculation priority of the reference calculator (manual p. 168).

```csharp
public static class ExpressionParser
```

#### Remarks

A hand-written Pratt parser. .NET has no expression parser (`System.Linq.Expressions` builds trees but does not read text; `DataTable.Compute` reads SQL-like text into `double`), and the calculator's grammar is context-dependent in ways parser generators handle poorly: multiplication with the sign omitted binds tighter than `÷`, a closing parenthesis may be omitted at the end, and Base-N reads `b10` as binary.

Parsing never throws for bad input: it stops at the first error and reports it with its span, as the calculator places the cursor at the error (p. 162).

#### Fields

- **`MaxNestingDepth`** = `128` (`int`, const): The deepest nesting of parentheses, functions and signs accepted; deeper input is a Stack ERROR.

#### Methods

- **`Parse(string text, SyntaxContext context, SyntaxVocabulary? vocabulary = null)`** *(static)* → `ParseResult`
  Parses an expression.

---

### `FunctionCall` Class

A function with its arguments: `sin(30)`, `log(2,16)`, `Σ(x+1,1,5)`.

```csharp
public sealed class FunctionCall : SyntaxNode
```

#### Remarks

The parser does not check the number of arguments; which counts a function accepts is the engine's business.

#### Constructors

- **`FunctionCall(SyntaxSymbol function, ImmutableArray<SyntaxNode> arguments, SourceSpan span = default)`**
  Initializes a call.

#### Properties

- **`Function`** (`SyntaxSymbol`): Gets the canonical function symbol, whose text ends with `(`.
- **`Arguments`** (`ImmutableArray<SyntaxNode>`): Gets the arguments.

---

### `LatexPrinter` Class

Writes a syntax tree as LaTeX math, for copying and exporting.

```csharp
public static class LatexPrinter
```

#### Remarks

The output is meant to typeset the way the calculator displays the expression: fractions as `\frac`, roots as `\sqrt`, `Σ(x+1,1,5)` as a summation. It is not read back; Canonical Linear Syntax is the text form Pallas parses. Calculator-specific commands (`▶t`, unit conversions, cell references) are written in `\mathrm` and typeset plainly.

#### Methods

- **`Print(SyntaxNode node)`** *(static)* → `string`
  Prints a tree.
- **`Print(SyntaxSymbol symbol)`** *(static)* → `string`
  Prints one name as it is drawn in a printed tree: a constant, a variable, a unit conversion.

---

### `LinearPrinter` Class

Writes a syntax tree as Canonical Linear Syntax.

```csharp
public static class LinearPrinter
```

#### Remarks

The output uses canonical spellings only (`×` for an input `*`) and parses back to an equivalent tree (`SyntaxEquivalence`) in the same context. To guarantee that, the printer:


- adds parentheses where the priority levels require them, and keeps those the tree holds;
- closes every parenthesis, including one omitted at the end of the input;
- writes every sexagesimal part, a missing one as `0`;
- parenthesizes where juxtaposition would read differently: `2(3)` rather than `23`, `2(-3)` rather than a subtraction, `(2C)3` rather than a combination;
- inserts a space where two tokens would otherwise run together, as a number before a parenthesized factor would turn into a recurring decimal: `2.5 (3)`.

#### Methods

- **`Print(SyntaxNode node, SyntaxContext context, SyntaxVocabulary? vocabulary = null)`** *(static)* → `string`
  Prints a tree.

---

### `MixedFractionExpression` Class

A mixed fraction `a⌟b⌟c`, meaning a + b/c: `1⌟1⌟2` is 1½.

```csharp
public sealed class MixedFractionExpression : SyntaxNode
```

#### Constructors

- **`MixedFractionExpression(SyntaxNode whole, SyntaxNode numerator, SyntaxNode denominator, SourceSpan span = default)`**
  Initializes a mixed fraction.

#### Properties

- **`Whole`** (`SyntaxNode`): Gets the whole part.
- **`Numerator`** (`SyntaxNode`): Gets the numerator.
- **`Denominator`** (`SyntaxNode`): Gets the denominator.

---

### `NameReference` Class

A named value: a constant (`π`, `@h`), a variable (`A`, `x`), a memory (`Ans`), a matrix, a vector or a statistic.

```csharp
public sealed class NameReference : SyntaxNode
```

#### Constructors

- **`NameReference(SyntaxSymbol symbol, SourceSpan span = default)`**
  Initializes a reference.

#### Properties

- **`Symbol`** (`SyntaxSymbol`): Gets the canonical symbol.

---

### `NegationExpression` Class

The negative sign `-x`, priority level 5: `-2²` is `-(2²)` (manual p. 169).

```csharp
public sealed class NegationExpression : SyntaxNode
```

#### Constructors

- **`NegationExpression(SyntaxNode operand, SourceSpan span = default)`**
  Initializes a negation.

#### Properties

- **`Operand`** (`SyntaxNode`): Gets the negated expression.

---

### `NumberLiteral` Class

A number as written: `12`, `1.25`, `.5`, `3.(021)`, or Base-N digits such as `1F`.

```csharp
public sealed class NumberLiteral : SyntaxNode
```

#### Constructors

- **`NumberLiteral(string text, SourceSpan span = default)`**
  Initializes a literal.

#### Properties

- **`Text`** (`string`): Gets the digits as written.
- **`IsRecurring`** (`bool`): Gets a value indicating whether the literal has a recurring part, as `0.(3)` does.

---

### `ParenthesizedExpression` Class

An expression in parentheses, kept so that printing reproduces what was typed: `(3×6)<(2+6)×2`.

```csharp
public sealed class ParenthesizedExpression : SyntaxNode
```

#### Remarks

`SyntaxEquivalence` looks through parentheses, so `6÷2(1+2)` and `6÷(2(1+2))` are equivalent.

#### Constructors

- **`ParenthesizedExpression(SyntaxNode inner, SourceSpan span = default)`**
  Initializes a parenthesized expression.

#### Properties

- **`Inner`** (`SyntaxNode`): Gets the expression inside.

---

### `ParseResult` Class

The outcome of parsing: a syntax tree, or the first error.

```csharp
public sealed class ParseResult
```

#### Properties

- **`Text`** (`string`): Gets the text that was parsed, after Unicode normalization; spans index into it.
- **`Root`** (`SyntaxNode?`): Gets the syntax tree, or `null` when parsing failed.
- **`Diagnostic`** (`SyntaxDiagnostic?`): Gets the first error, or `null` when parsing succeeded.
- **`Succeeded`** (`bool`): Gets a value indicating whether `ParseResult.Root` holds a tree.

---

### `PostfixExpression` Class

An operator after its operand: `x²`, `5!`, `20%`, `30°`, `5.5ŷ`.

```csharp
public sealed class PostfixExpression : SyntaxNode
```

#### Constructors

- **`PostfixExpression(PostfixOperator postfixOperator, SyntaxNode operand, SourceSpan span = default)`**
  Initializes a postfix expression.

#### Properties

- **`Operator`** (`PostfixOperator`): Gets the operator.
- **`Operand`** (`SyntaxNode`): Gets the operand.

---

### `RelationChain` Class

A Verify expression: operands joined by relational operators, `1≤1<1+1` or `2+3=5≠2+5=8`.

```csharp
public sealed class RelationChain : SyntaxNode
```

#### Constructors

- **`RelationChain(ImmutableArray<SyntaxNode> operands, ImmutableArray<RelationOperator> operators, SourceSpan span = default)`**
  Initializes a chain.

#### Properties

- **`Operands`** (`ImmutableArray<SyntaxNode>`): Gets the operands.
- **`Operators`** (`ImmutableArray<RelationOperator>`): Gets the operators; `Operators[i]` stands between `Operands[i]` and `Operands[i + 1]`.

---

### `SexagesimalExpression` Class

Degrees-minutes-seconds: `2°20′30″`.

```csharp
public sealed class SexagesimalExpression : SyntaxNode
```

#### Remarks

On the calculator the °′″ key and the degree unit `°` are different keys: in Radian mode, `sin(30°)` is the sine of 30 degrees, while `30°0′0″` is just the number 30. In text, `°` followed by minutes or seconds is sexagesimal; `°` alone is the unit (`PostfixOperator.Degrees`). Minutes and seconds are plain numbers, which lets the parser decide with one token of lookahead. Printers write all three parts, a missing one as 0.

#### Constructors

- **`SexagesimalExpression(SyntaxNode degrees, NumberLiteral? minutes, NumberLiteral? seconds, SourceSpan span = default)`**
  Initializes a sexagesimal value.

#### Properties

- **`Degrees`** (`SyntaxNode`): Gets the degrees.
- **`Minutes`** (`NumberLiteral?`): Gets the minutes, or `null`.
- **`Seconds`** (`NumberLiteral?`): Gets the seconds, or `null`.

---

### `SuffixCommandExpression` Class

A named command written after a value: an engineering symbol (`999_k`, level 3) or a unit conversion (`5cm▶in`, level 6).

```csharp
public sealed class SuffixCommandExpression : SyntaxNode
```

#### Constructors

- **`SuffixCommandExpression(SyntaxNode operand, SyntaxSymbol command, SourceSpan span = default)`**
  Initializes a suffix command.

#### Properties

- **`Operand`** (`SyntaxNode`): Gets the value.
- **`Command`** (`SyntaxSymbol`): Gets the canonical command symbol.

---

### `SyntaxEquivalence` Class

Compares syntax trees by structure.

```csharp
public static class SyntaxEquivalence
```

#### Methods

- **`AreEquivalent(SyntaxNode left, SyntaxNode right)`** *(static)* → `bool`
  Determines whether two trees have the same structure, ignoring spans and parentheses.

---

### `SyntaxNode` Class

A node of an immutable syntax tree.

```csharp
public abstract class SyntaxNode
```

#### Remarks

The set of node types is closed (the constructor is not accessible outside this assembly), so a `switch` over them is complete. Nodes have reference identity; compare trees with `SyntaxEquivalence`. Numbers stay text: turning `0.1` into a `decimal`, or `6.62607015E-34` into a `double`, is the engine's decision (docs/PRECISION.md §3).

#### Properties

- **`Span`** (`SourceSpan`): Gets where the node is in the parsed text; `default` for a node that was not parsed.

---

### `SyntaxSymbol` Class

One spelling the lexer recognizes: a name such as `sin(` or `Ans`, or an operator such as `×`.

```csharp
public sealed class SyntaxSymbol
```

#### Remarks

An alias (`*` for `×`, `sqrt(` for `√(`) is a symbol of its own whose `SyntaxSymbol.Canonical` is the symbol the printers write. Syntax trees always hold canonical symbols.

#### Properties

- **`Text`** (`string`): Gets the exact text that is matched in the input.
- **`Kind`** (`SymbolKind`): Gets what the symbol is.
- **`Applications`** (`ImmutableArray<CalculatorApp>`): Gets the applications the symbol is available in; empty means every application.
- **`Canonical`** (`SyntaxSymbol`): Gets the symbol printers write for this one: itself, unless this is an alias.
- **`BinaryOperator`** (`BinaryOperator?`): Gets the operator, when `SyntaxSymbol.Kind` is `SymbolKind.BinaryOperator`.
- **`PostfixOperator`** (`PostfixOperator?`): Gets the operator, when `SyntaxSymbol.Kind` is `SymbolKind.PostfixOperator`.
- **`RelationOperator`** (`RelationOperator?`): Gets the operator, when `SyntaxSymbol.Kind` is `SymbolKind.RelationOperator`.

#### Methods

- **`CreateName(string text, SymbolKind kind, params ReadOnlySpan<CalculatorApp> applications)`** *(static)* → `SyntaxSymbol`
  Creates a name that a vocabulary can recognize, such as a plugin function.
- **`CreateAlias(string text)`** → `SyntaxSymbol`
  Creates another spelling of this symbol, recognized on input and printed as this symbol's canonical text.
- **`IsAvailableIn(CalculatorApp app)`** → `bool`
  Returns whether the symbol can be used in `app`.

---

### `SyntaxVocabulary` Class

The spellings the lexer recognizes: the fixed grammar (operators, punctuation) and the names of functions, constants, variables and commands.

```csharp
public sealed class SyntaxVocabulary
```

#### Remarks

A vocabulary holds names only, with no meaning attached: whether `sin(` takes one argument, and what it computes, is the engine's business. The parser needs the names because text alone cannot tell `sinh(` from `sin(` followed by `h`, or know that `AtWt(` is one token.

Matching is longest-first among the symbols available in the application, so `÷R` wins over `÷` and `d/dx(` over `d`. Symbols are grouped by first character, which keeps lexing free of allocations on every target framework.

#### Properties

- **`Standard`** (`SyntaxVocabulary`, static): Gets the vocabulary of the reference calculator: its functions, commands, constants, variables, the 40 unit conversions and the 47 scientific constants, with the input aliases of docs/LINEAR-SYNTAX.md.
- **`Symbols`** (`ImmutableArray<SyntaxSymbol>`): Gets every symbol, canonical spellings and aliases alike.

#### Methods

- **`With(params IEnumerable<SyntaxSymbol> names)`** → `SyntaxVocabulary`
  Returns a vocabulary that also recognizes `names`, for example functions registered by a plugin.

---

### `BinaryOperator` Enum

An operator between two operands, with its priority level on the reference calculator (manual p. 168; 1 binds tightest).

```csharp
public enum BinaryOperator
```

#### Fields

- **`Add`** (`0`): `a+b`, level 11.
- **`Subtract`** (`1`): `a-b`, level 11.
- **`Multiply`** (`2`): `a×b`, level 10.
- **`Divide`** (`3`): `a÷b`, level 10.
- **`DivideWithRemainder`** (`4`): `a÷Rb`, division with remainder, level 10.
- **`ImplicitMultiply`** (`5`): `ab`, multiplication with the sign omitted, level 7: `6÷2π` is `6÷(2π)`.
- **`Power`** (`6`): `a^b`, level 3.
- **`Root`** (`7`): `nˣ√(x)`, the n-th root of x, level 3. The left operand is the index.
- **`Fraction`** (`8`): `a⌟b`, level 4.
- **`Permutation`** (`9`): `nPr`, level 8.
- **`Combination`** (`10`): `nCr`, level 8.
- **`Polar`** (`11`): `r∠θ`, a complex number in polar form, level 8.
- **`DotProduct`** (`12`): `a•b`, the dot product, level 9.
- **`And`** (`13`): `a and b`, level 12.
- **`Or`** (`14`): `a or b`, level 13.
- **`Xor`** (`15`): `a xor b`, level 13.
- **`Xnor`** (`16`): `a xnor b`, level 13.

---

### `CalculatorApp` Enum

The calculator application an expression is entered in.

```csharp
public enum CalculatorApp
```

#### Remarks

The application changes how text is read. In Base-N, `b10` is binary 10 and `A`-`F` are hexadecimal digits; in Calculate they are variables. In Spreadsheet, `A1` is a cell. The names match the reference calculator conformance data.

#### Fields

- **`Calculate`** (`0`): Calculate: arithmetic, functions, CALC, Verify.
- **`Statistics`** (`1`): Statistics: statistic variables, regression estimates, normal distribution functions.
- **`Distribution`** (`2`): Distribution.
- **`Spreadsheet`** (`3`): Spreadsheet: cell references and ranges.
- **`Table`** (`4`): Table.
- **`Equation`** (`5`): Equation, including the Solver.
- **`Inequality`** (`6`): Inequality.
- **`Complex`** (`7`): Complex: the imaginary unit and polar form.
- **`BaseN`** (`8`): Base-N: hexadecimal digits, base prefixes and logic operators.
- **`Matrix`** (`9`): Matrix.
- **`Vector`** (`10`): Vector.
- **`Ratio`** (`11`): Ratio.
- **`MathBox`** (`12`): Math Box.

---

### `NumberBase` Enum

The base a Base-N prefix gives to the literal after it (`d10`, `h10`, `b10`, `o10`).

```csharp
public enum NumberBase
```

#### Fields

- **`Dec`** (`10`): Prefix `d`: decimal, shown as Dec on the calculator.
- **`Hex`** (`16`): Prefix `h`: hexadecimal, shown as Hex.
- **`Bin`** (`2`): Prefix `b`: binary, shown as Bin.
- **`Oct`** (`8`): Prefix `o`: octal, shown as Oct.

---

### `PostfixOperator` Enum

An operator written after its operand.

```csharp
public enum PostfixOperator
```

#### Fields

- **`Square`** (`0`): `x²`, level 3.
- **`Cube`** (`1`): `x³`, level 3.
- **`Reciprocal`** (`2`): `x⁻¹`, level 3.
- **`Factorial`** (`3`): `x!`, level 3.
- **`Percent`** (`4`): `x%`, level 3.
- **`Degrees`** (`5`): `x°`, an angle in degrees, level 3. Followed by minutes or seconds it is a `SexagesimalExpression` instead.
- **`Radians`** (`6`): `xʳ`, an angle in radians, level 3.
- **`Gradians`** (`7`): `xᵍ`, an angle in gradians, level 3.
- **`StandardizedVariate`** (`8`): `x▶t`, the standardized variate in Statistics, level 3.
- **`EstimateX`** (`9`): `yx̂`, the regression estimate of x, level 6.
- **`EstimateY`** (`10`): `xŷ`, the regression estimate of y, level 6.
- **`EstimateX1`** (`11`): `yx̂₁`, the first quadratic-regression estimate of x, level 6.
- **`EstimateX2`** (`12`): `yx̂₂`, the second quadratic-regression estimate of x, level 6.

---

### `RelationOperator` Enum

A relational operator in a Verify expression. Relations bind looser than every other operator.

```csharp
public enum RelationOperator
```

#### Fields

- **`Equal`** (`0`): `=`.
- **`NotEqual`** (`1`): `≠`.
- **`Less`** (`2`): `<`.
- **`Greater`** (`3`): `>`.
- **`LessOrEqual`** (`4`): `≤`, also written `<=`.
- **`GreaterOrEqual`** (`5`): `≥`, also written `>=`.

---

### `SymbolKind` Enum

What a `SyntaxSymbol` is.

```csharp
public enum SymbolKind
```

#### Remarks

The kinds up to `SymbolKind.UnitConversion` are names, which a vocabulary may add (see `SyntaxVocabulary.With`). The remaining kinds are the fixed grammar.

#### Fields

- **`Function`** (`0`): A function written with parentheses; its text ends with `(`, as in `sin(`.
- **`Constant`** (`1`): A mathematical constant or nullary command: `π`, `e`, `i`, `Ran#`.
- **`ScientificConstant`** (`2`): A scientific constant, written with `@`: `@h`, `@N_A`.
- **`Variable`** (`3`): A variable: `A`-`F`, `x`, `y`, `z`.
- **`Memory`** (`4`): An answer memory: `Ans`, `PreAns`.
- **`MatrixVariable`** (`5`): A matrix variable: `MatA`-`MatD`, `MatAns`.
- **`VectorVariable`** (`6`): A vector variable: `VctA`-`VctD`, `VctAns`.
- **`StatisticsVariable`** (`7`): A statistic value: `x̄`, `σx`, `Σx²`, `n`.
- **`EngineeringSymbol`** (`8`): An engineering symbol written after a value, with an underscore: `_k`, `_μ`.
- **`UnitConversion`** (`9`): A unit conversion command written after a value: `cm▶in`.
- **`BinaryOperator`** (`10`): An operator between two operands: `+`, `×`, `⌟`, `P`.
- **`PostfixOperator`** (`11`): An operator after its operand: `²`, `!`, `°`.
- **`RelationOperator`** (`12`): A relational operator: `=`, `≤`.
- **`SexagesimalMark`** (`13`): A minute or second mark in degrees-minutes-seconds: `′`, `″`.
- **`OpenParenthesis`** (`14`): `(`.
- **`CloseParenthesis`** (`15`): `)`.
- **`Comma`** (`16`): `,`, between function arguments.
- **`Colon`** (`17`): `:`, in a Spreadsheet cell range.

---

### `SyntaxErrorCode` Enum

Why an expression could not be read.

```csharp
public enum SyntaxErrorCode
```

#### Remarks

Every code is a Syntax ERROR on the calculator except `SyntaxErrorCode.NestingTooDeep`, which is a Stack ERROR. The engine maps codes to its language-neutral error kinds; the app turns those into text.

#### Fields

- **`EmptyExpression`** (`0`): The text is empty or white space.
- **`UnexpectedCharacter`** (`1`): A character starts no token.
- **`MissingOperand`** (`2`): An operand is missing: `2+`, `×3`, `sin()`.
- **`UnexpectedToken`** (`3`): A token cannot appear here: a comma outside a function, `P` without a left operand.
- **`UnmatchedClosingParenthesis`** (`4`): A `)` has no matching `(`.
- **`AdjacentNumbers`** (`5`): Two numbers stand side by side, `2 3`; implicit multiplication needs something between them.
- **`RelationNotAllowed`** (`6`): A relational operator appears while relations are not allowed (Verify is off).
- **`MixedRelationDirections`** (`7`): Relations point both ways, `5≤6≥4` (manual p. 75).
- **`NotEqualWithInequality`** (`8`): `≠` is combined with `< > ≤ ≥`, `4<6≠8` (manual p. 75; assumption U10).
- **`TooManyFractionParts`** (`9`): More than three parts separated by `⌟`.
- **`MisplacedSexagesimalMark`** (`10`): A minute or second mark `′ ″` without degrees before it.
- **`InvalidRootArguments`** (`11`): `root(` does not have exactly two arguments, index and radicand.
- **`NestingTooDeep`** (`12`): Parentheses, functions or signs are nested deeper than `ExpressionParser.MaxNestingDepth`: a Stack ERROR.

---

### `TokenKind` Enum

What a `Token` is.

```csharp
public enum TokenKind
```

#### Fields

- **`End`** (`0`): The end of the text. The lexer returns it repeatedly once reached.
- **`Invalid`** (`1`): A character that starts no token, such as `q` or `#` on its own.
- **`Number`** (`2`): A number: `12`, `1.25`, `.5`, `0.(3)`; in Base-N, digits `0`-`9` and `A`-`F`.
- **`BasePrefix`** (`3`): A Base-N prefix `d`, `h`, `b` or `o` directly before a number.
- **`CellReference`** (`4`): A Spreadsheet cell reference: `A1`, `$A1`, `A$1`, `$A$1`.
- **`Symbol`** (`5`): A symbol of the vocabulary; `Token.Symbol` says which.

---

### `ExpressionLexer` Struct

Splits Canonical Linear Syntax into tokens, without allocating.

```csharp
public struct ExpressionLexer
```

#### Remarks

At each position, after skipping white space, the lexer reads the first of:


- In Base-N, a run of digits and `A`-`F`, unless a longer vocabulary name starts there (`Ans`); or a prefix `d h b o` directly followed by such a digit.
- In Spreadsheet, a cell reference such as `$A$1`, unless a longer name starts there.
- Elsewhere, a number: digits with an optional decimal point, and after the decimal point an optional recurring part in parentheses, `3.(021)`. A recurring part needs the decimal point, so `2(3)` stays a multiplication.
- The longest vocabulary symbol available in the application.
- Otherwise an `TokenKind.Invalid` token for one character (or surrogate pair).

The text should already be in Unicode normalization form C; `ExpressionParser` takes care of that.

#### Constructors

- **`ExpressionLexer(ReadOnlySpan<char> text, SyntaxContext context, SyntaxVocabulary vocabulary)`**
  Initializes a lexer over `text`.

#### Methods

- **`Next()`** → `Token`
  Reads the next token.

---

### `SourceSpan` Struct

A range of characters in the text that was parsed.

```csharp
public readonly struct SourceSpan : IEquatable<SourceSpan>
```

#### Remarks

Every token, syntax node and diagnostic carries a span, because the calculator places the cursor at the error (manual p. 162). Indexes refer to `ParseResult.Text`, the text after Unicode normalization.

#### Constructors

- **`SourceSpan(int Start, int Length)`**
  A range of characters in the text that was parsed.

#### Properties

- **`Start`** (`int`, init): The index of the first character.
- **`Length`** (`int`, init): The number of characters; 0 for a position, such as the end of the text.
- **`End`** (`int`): Gets the index just past the last character.

#### Methods

- **`Covering(SourceSpan start, SourceSpan end)`** *(static)* → `SourceSpan`
  Creates the span that starts at `start` and ends at `end`.

---

### `SyntaxContext` Struct

The circumstances an expression is read in.

```csharp
public readonly struct SyntaxContext : IEquatable<SyntaxContext>
```

#### Constructors

- **`SyntaxContext(CalculatorApp App, bool AllowRelations = false)`**
  The circumstances an expression is read in.

#### Properties

- **`App`** (`CalculatorApp`, init): The calculator application, which decides context-dependent tokens.
- **`AllowRelations`** (`bool`, init): Whether the relational operators `= ≠ < > ≤ ≥` may appear, as when Verify is on (manual p. 73).
- **`Calculate`** (`SyntaxContext`, static): Gets the context of the Calculate application with Verify off.

---

### `SyntaxDiagnostic` Struct

The first error found in an expression, and where it is.

```csharp
public readonly struct SyntaxDiagnostic : IEquatable<SyntaxDiagnostic>
```

#### Constructors

- **`SyntaxDiagnostic(SyntaxErrorCode Code, SourceSpan Span)`**
  The first error found in an expression, and where it is.

#### Properties

- **`Code`** (`SyntaxErrorCode`, init): What is wrong.
- **`Span`** (`SourceSpan`, init): Where the error is, for placing the cursor; zero-length at the end of the text for a missing operand.

---

### `Token` Struct

A token: a kind and where it is. The text is the span of the lexed input; nothing is copied.

```csharp
public readonly struct Token : IEquatable<Token>
```

#### Constructors

- **`Token(TokenKind Kind, SourceSpan Span, SyntaxSymbol? Symbol = null)`**
  A token: a kind and where it is. The text is the span of the lexed input; nothing is copied.

#### Properties

- **`Kind`** (`TokenKind`, init): What the token is.
- **`Span`** (`SourceSpan`, init): Where the token is.
- **`Symbol`** (`SyntaxSymbol?`, init): The vocabulary symbol matched, when `Kind` is `TokenKind.Symbol`; may be an alias.

---

## `Barbatos.Pallas.Numerics` Namespace

Only what `System.Math`, `System.Decimal` and `BigInteger` do not already provide: sine, cosine and tangent in
degrees, radians and gradians (`Trigonometry`), factorials, permutations, combinations and prime factors
(`IntegerFunctions`), the fraction a decimal is (`Fractions`), degrees, minutes and seconds (`Sexagesimal`), the
error function and its inverse (`ErrorFunction`) and the Poisson probability (`PoissonDistribution`).

### Classes

| Class | Description |
|-------|-------------|
| [`ErrorFunction`](#errorfunction-class) | The error function, its complement and the inverse of the complement. |
| [`Fractions`](#fractions-class) | Recognizes the fraction a `Decimal` value stands for, so results can be displayed as fractions without a dedicated rational number type. |
| [`IntegerFunctions`](#integerfunctions-class) | Exact integer functions on `BigInteger` that the BCL does not provide. |
| [`PoissonDistribution`](#poissondistribution-class) | The Poisson probability e^(−λ)·λ^x/x!. |
| [`Sexagesimal`](#sexagesimal-class) | Conversions between decimal degrees and degrees-minutes-seconds, in `Decimal`. |
| [`Trigonometry`](#trigonometry-class) | Trigonometric functions of an angle in a calculator angle unit. |

### Enums

| Enum | Description |
|-------|-------------|
| [`AngleUnit`](#angleunit-enum) | The unit an angle is expressed in. |

---

### `ErrorFunction` Class

The error function, its complement and the inverse of the complement.

```csharp
public static class ErrorFunction
```

#### Remarks

The normal distribution of the Statistics and Distribution applications is the complementary error function: Φ(t) = erfc(−t/√2)/2, and the inverse normal is the inverse of erfc. .NET has neither function nor the inverse.

Below 1, erf is its Maclaurin series, whose 19 terms reach `Double` precision there. From 1 up, erfc(x) = e^(−x²)·x·K(x²)/√π, where K is the continued fraction of the incomplete gamma function Γ(½, x²), evaluated backward from a depth of 120; the other function is then the difference from 1, which loses nothing because erfc is at most 0.16 there. The same fraction evaluated forward (Lentz) was measured 6.5×10⁻¹⁵ off near x = 1, backward 5.3×10⁻¹⁶ (18 Sep 2026). e^(−x²) is taken as e^(−h²)·e^(−(x−h)(x+h)), with h the sixteenths of x, whose square is exact: `Math.Exp(-x * x)` carries the rounding of x², x²·2⁻⁵³ relative, which is 5.5×10⁻¹⁵ at x = 7.

The accuracy is measured against PeterO.Numbers series at 50 digits (ErrorFunctionTests): a relative error below 2×10⁻¹⁵ for erf and erfc wherever erfc is a normal `Double`.

#### Methods

- **`Erf(double x)`** *(static)* → `double`
  Returns the error function of a value.
- **`Erfc(double x)`** *(static)* → `double`
  Returns the complementary error function of a value, 1 − erf(x), without the cancellation of the difference.
- **`InverseErfc(double value)`** *(static)* → `double`
  Returns the value whose complementary error function is `value`.

---

### `Fractions` Class

Recognizes the fraction a `Decimal` value stands for, so results can be displayed as fractions without a dedicated rational number type.

```csharp
public static class Fractions
```

#### Methods

- **`TryFromDecimal(decimal value, long maxDenominator, decimal tolerance, out long numerator, out long denominator)`** *(static)* → `bool`
  Finds the simplest fraction within `tolerance` of `value`.

---

### `IntegerFunctions` Class

Exact integer functions on `BigInteger` that the BCL does not provide.

```csharp
public static class IntegerFunctions
```

#### Remarks

The greatest common divisor is already `BigInteger.GreatestCommonDivisor`, so it is not repeated here. These functions compute exact results of any size; limits such as the calculator's `n! < 10¹⁰⁰` belong to the caller, which should check them before asking for a result it would reject.

#### Methods

- **`Factorial(int n)`** *(static)* → `BigInteger`
  Returns `n!`.
- **`Permutations(long n, long r)`** *(static)* → `BigInteger`
  Returns the number of ordered selections of `r` items from `n`: `n! / (n − r)!`.
- **`Combinations(long n, long r)`** *(static)* → `BigInteger`
  Returns the number of unordered selections of `r` items from `n`: `n! / (r! (n − r)!)`.
- **`LeastCommonMultiple(BigInteger left, BigInteger right)`** *(static)* → `BigInteger`
  Returns the least common multiple.
- **`PrimeFactors(long value)`** *(static)* → `IReadOnlyList<(long Prime, int Exponent)>`
  Returns the prime factorization of a positive integer.

---

### `PoissonDistribution` Class

The Poisson probability e^(−λ)·λ^x/x!.

```csharp
public static class PoissonDistribution
```

#### Remarks

.NET has no Poisson distribution and no log-gamma function. The obvious exp(x·ln λ − λ − ln x!) cancels: at x = λ = 10⁶ the three terms are about 1.4×10⁷ and their sum about −7, so the rounding of each term, 10⁻⁹ absolute, becomes a relative error of 10⁻⁹ in the probability.

This is Loader's saddle-point form instead (C. Loader, Fast and Accurate Computation of Binomial Probabilities, 2000): e^(−λ)λ^x/x! = e^(−stirlerr(x) − bd0(x, λ))/√(2πx), where stirlerr(x) = ln x! − ln(√(2πx)(x/e)^x) is a table up to 15 and its Stirling series above, and bd0(x, λ) = x·ln(x/λ) + λ − x is summed as a series where x is near λ, so that nothing large cancels. Measured against PeterO.Numbers at 50 digits (PoissonDistributionTests): a relative error below 10⁻¹⁵·(1 + |ln P|) wherever the result is a normal `Double`, at most 8.7×10⁻¹⁶·(1 + |ln P|) in 3,000 samples. A small probability is e raised to a large exponent, whose own rounding is 10⁻¹⁶ of it, so no algorithm in `Double` does better in the far tail: P(1772; 637.4) = 7.6×10⁻²⁹⁷ is 3.3×10⁻¹³ off.

#### Methods

- **`Probability(double x, double mean)`** *(static)* → `double`
  Returns the probability that a Poisson variable with mean `mean` is `x`.

---

### `Sexagesimal` Class

Conversions between decimal degrees and degrees-minutes-seconds, in `Decimal`.

```csharp
public static class Sexagesimal
```

#### Methods

- **`ToDegrees(decimal degrees, decimal minutes, decimal seconds)`** *(static)* → `decimal`
  Converts degrees, minutes and seconds to decimal degrees.
- **`FromDegrees(decimal value, int secondsDecimals, MidpointRounding mode)`** *(static)* → `(bool Negative, decimal Degrees, decimal Minutes, decimal Seconds)`
  Splits decimal degrees into degrees, minutes and seconds, rounding the seconds and carrying any overflow.

---

### `Trigonometry` Class

Trigonometric functions of an angle in a calculator angle unit.

```csharp
public static class Trigonometry
```

#### Remarks

.NET provides the functions; this class only chooses among them for an `AngleUnit`, which .NET does not have. Results follow `Double` semantics: NaN outside the domain, ±∞ for the tangent of an odd multiple of a right angle.

Degrees and gradians use `Double.SinPi`, `Double.CosPi` and `Double.TanPi`, which take the angle as a multiple of π. They are exact at multiples of a right angle, where `Math.Cos(Math.PI / 2)` is 6.1×10⁻¹⁷ and `Math.Tan(Math.PI / 2)` is 1.6×10¹⁶. Whole turns are removed first with `Double.Ieee754Remainder`, which is exact: dividing 10⁶° by 180 directly loses 6×10⁻¹³ of a half-turn.

#### Methods

- **`Sin(double angle, AngleUnit unit)`** *(static)* → `double`
  Returns the sine of an angle.
- **`Cos(double angle, AngleUnit unit)`** *(static)* → `double`
  Returns the cosine of an angle.
- **`Tan(double angle, AngleUnit unit)`** *(static)* → `double`
  Returns the tangent of an angle.
- **`Asin(double value, AngleUnit unit)`** *(static)* → `double`
  Returns the angle whose sine is `value`.
- **`Acos(double value, AngleUnit unit)`** *(static)* → `double`
  Returns the angle whose cosine is `value`.
- **`Atan(double value, AngleUnit unit)`** *(static)* → `double`
  Returns the angle whose tangent is `value`.
- **`Atan2(double y, double x, AngleUnit unit)`** *(static)* → `double`
  Returns the angle of the point (`x`, `y`) from the positive x-axis.
- **`ConvertAngle(double angle, AngleUnit from, AngleUnit to)`** *(static)* → `double`
  Converts an angle from one unit to another.

---

### `AngleUnit` Enum

The unit an angle is expressed in.

```csharp
public enum AngleUnit
```

#### Fields

- **`Degree`** (`0`): Degrees: 360 per full turn.
- **`Radian`** (`1`): Radians: 2π per full turn.
- **`Gradian`** (`2`): Gradians: 400 per full turn.

---

## `Barbatos.Pallas.LinearAlgebra` Namespace

Fraction-free elimination of decimal matrices scaled to integers: determinants, inverses and solutions come out as
exact fractions of `BigInteger`s, rounded only when the engine displays them.

### Classes

| Class | Description |
|-------|-------------|
| [`ExactLinearAlgebra`](#exactlinearalgebra-class) | Exact determinants, inverses and solutions of linear systems of `Decimal` matrices. |

---

### `ExactLinearAlgebra` Class

Exact determinants, inverses and solutions of linear systems of `Decimal` matrices.

```csharp
public static class ExactLinearAlgebra
```

#### Remarks

.NET has no linear algebra on `Decimal`, and elimination in `Decimal` rounds at every division: a determinant of exactly 0 comes out as about 10⁻²⁷, and no threshold can tell that from a small nonzero one. A `Decimal` is an integer mantissa over a power of ten, so each row is multiplied by its own power of ten into integers, and the integer matrix is eliminated without fractions (Bareiss, 1968) on `BigInteger`. Every intermediate division is exact, so a determinant is 0 exactly when the matrix is singular.

Results are returned as numerators over a common denominator, and rounding to `Decimal` happens once, in the caller. There is no rational number type: the fraction exists only as the two integers of a result.

#### Methods

- **`Determinant(decimal[,] matrix)`** *(static)* → `(BigInteger Numerator, BigInteger Denominator)`
  Returns the determinant of a square matrix, exactly.
- **`TryInvert(decimal[,] matrix, out BigInteger[,] numerators, out BigInteger denominator)`** *(static)* → `bool`
  Returns the inverse of a square matrix, exactly.
- **`TrySolve(decimal[,] coefficients, decimal[] constants, out BigInteger[] numerators, out BigInteger denominator)`** *(static)* → `bool`
  Solves the linear system `A·x = b` exactly.
- **`TrySolve(BigInteger[,] augmented, out BigInteger[] numerators, out BigInteger denominator)`** *(static)* → `bool`
  Solves the linear system whose augmented matrix is given in integers.
- **`Rank(BigInteger[,] matrix)`** *(static)* → `int`
  Returns the rank of an integer matrix, exactly.

---

## `Barbatos.Pallas.Statistics` Namespace

Statistics of decimal data scaled to integers (`ExactSample`): every sum, mean, variance and fit is an exact
fraction of `BigInteger`s. `Quartiles` gives the ranks the quartiles are read at.

### Classes

| Class | Description |
|-------|-------------|
| [`ExactSample`](#exactsample-class) | Exact sums, means, variances, least-squares coefficients and correlation of one- or two-variable data with frequencies. |
| [`Quartiles`](#quartiles-class) | Where the quartiles of sorted data are: the ranks whose values' mean is the quartile. |

### Enums

| Enum | Description |
|-------|-------------|
| [`Quartile`](#quartile-enum) | A quartile of one-variable data. |
| [`SampleVariable`](#samplevariable-enum) | A variable of a sample. |

---

### `ExactSample` Class

Exact sums, means, variances, least-squares coefficients and correlation of one- or two-variable data with frequencies.

```csharp
public sealed class ExactSample
```

#### Remarks

.NET has no statistics, and the one-pass formulas the calculator documents (manual pp. 93-95) cancel: Sxx = Σx² − (Σx)²/n of equal 20-digit decimals comes out as about 10⁻²⁷ instead of 0 in `Decimal`, and the slope Sxy/Sxx of a vertical line becomes a huge number instead of a Math ERROR. Each value here is an integer over a power of ten, like a `Decimal`, so every column is scaled to integers once and every sum is exact on `BigInteger`: Sxx is 0 exactly when the x values are all equal, and each result is a fraction of integers that the caller rounds once.

There is no rational number type: a result is its numerator and a positive denominator, in lowest terms. The linear and quadratic fits solve their normal equations by Cramer's rule on the integer sums.

#### Properties

- **`HasY`** (`bool`): Gets whether the sample has a second variable, y.
- **`Count`** (`(BigInteger Numerator, BigInteger Denominator)`): Gets n, the sum of the frequencies.

#### Methods

- **`FromDecimals(ReadOnlySpan<decimal> x, ReadOnlySpan<decimal> y, ReadOnlySpan<decimal> frequencies)`** *(static)* → `ExactSample`
  Creates a sample of decimals.
- **`FromScaledIntegers(ReadOnlySpan<BigInteger> x, int xScale, ReadOnlySpan<BigInteger> y, int yScale, ReadOnlySpan<BigInteger> frequencies, int frequencyScale)`** *(static)* → `ExactSample`
  Creates a sample of values each given as an integer over a power of ten, one power per column.
- **`Sum(int xPower, int yPower)`** → `(BigInteger Numerator, BigInteger Denominator)`
  Returns a sum Σ f·x^i·y^j: Σx to Σx⁴, Σy, Σxy, Σx²y and Σy², or n for i = j = 0.
- **`TryGetMean(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) mean)`** → `bool`
  Returns the mean of a variable, Σx/n.
- **`TryGetPopulationVariance(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) variance)`** → `bool`
  Returns the population variance of a variable, σ² = Σ(x − x̄)²/n = (nΣx² − (Σx)²)/n².
- **`TryGetSampleVariance(SampleVariable variable, out (BigInteger Numerator, BigInteger Denominator) variance)`** → `bool`
  Returns the sample variance of a variable, s² = Σ(x − x̄)²/(n − 1) = (nΣx² − (Σx)²)/(n(n − 1)).
- **`TryGetLinearFit(out (BigInteger Numerator, BigInteger Denominator) intercept, out (BigInteger Numerator, BigInteger Denominator) slope)`** → `bool`
  Returns the least-squares line y = a + bx.
- **`TryGetQuadraticFit(out (BigInteger Numerator, BigInteger Denominator) a, out (BigInteger Numerator, BigInteger Denominator) b, out (BigInteger Numerator, BigInteger Denominator) c)`** → `bool`
  Returns the least-squares parabola y = a + bx + cx².
- **`TryGetCorrelationSquared(out (BigInteger Numerator, BigInteger Denominator) square, out int sign)`** → `bool`
  Returns the square of the correlation coefficient r = Sxy/√(Sxx·Syy), and its sign.

---

### `Quartiles` Class

Where the quartiles of sorted data are: the ranks whose values' mean is the quartile.

```csharp
public static class Quartiles
```

#### Remarks

The median is the middle value, or the mean of the two middle values. The first and third quartiles are the medians of the lower and upper halves, without the median itself when the count is odd. The manual gives no formula; its 1-Var Results screen of p. 84 shows Q1 = 4, Med = 6.5 and Q3 = 8 for 20 values, which this rule gives (assumption U1 of docs/CONFORMANCE.md). A single value is all three quartiles.

.NET has no quartiles. The ranks are `BigInteger` because a frequency can be large: 1 with frequency 10³⁰ is 10³⁰ values.

#### Methods

- **`Ranks(BigInteger count, Quartile quartile)`** *(static)* → `(BigInteger Lower, BigInteger Upper)`
  Returns the ranks, from 1, of the sorted values whose mean is a quartile.

---

### `Quartile` Enum

A quartile of one-variable data.

```csharp
public enum Quartile
```

#### Fields

- **`First`** (`0`): The first quartile, Q1.
- **`Median`** (`1`): The median, Med.
- **`Third`** (`2`): The third quartile, Q3.

---

### `SampleVariable` Enum

A variable of a sample.

```csharp
public enum SampleVariable
```

#### Fields

- **`X`** (`0`): The first variable, x.
- **`Y`** (`1`): The second variable, y, of a two-variable sample.

---

## `Barbatos.Pallas.Solvers` Namespace

Exact integer polynomials - the sign at a rational point, Sturm's count of the real roots, the square-free part
and division by a rational root (`IntegerPolynomial`) - and every complex root of a polynomial by Aberth's
method, with a rational root recovered exactly (`PolynomialRoots`).

### Classes

| Class | Description |
|-------|-------------|
| [`IntegerPolynomial`](#integerpolynomial-class) | Exact algebra of a polynomial with `BigInteger` coefficients, ascending: `coefficients[i]` multiplies x to the power i. |
| [`PolynomialRoots`](#polynomialroots-class) | The roots of a polynomial: all of them at once numerically, and the rational a numeric root stands for when it is one. |

---

### `IntegerPolynomial` Class

Exact algebra of a polynomial with `BigInteger` coefficients, ascending: `coefficients[i]` multiplies x to the power i.

```csharp
public static class IntegerPolynomial
```

#### Remarks

A polynomial of the calculator comes from decimal coefficients, so multiplying it by a power of ten makes every coefficient an integer and leaves the roots where they are. Everything here is then exact: the sign at a rational point, the number of distinct real roots (Sturm's theorem), the polynomial without its repeated factors, and division by a rational root. .NET has no polynomial arithmetic of any kind.

#### Methods

- **`Degree(IReadOnlyList<BigInteger> coefficients)`** *(static)* → `int`
  Returns the degree: the index of the highest coefficient that is not 0, or −1 for the zero polynomial.
- **`SignAt(IReadOnlyList<BigInteger> coefficients, BigInteger numerator, BigInteger denominator)`** *(static)* → `int`
  Returns the sign of the polynomial at a rational point, exactly.
- **`Derivative(IReadOnlyList<BigInteger> coefficients)`** *(static)* → `BigInteger[]`
  Returns the derivative.
- **`SquareFree(IReadOnlyList<BigInteger> coefficients)`** *(static)* → `BigInteger[]`
  Returns the polynomial without its repeated factors: p divided by the greatest common divisor of p and its derivative.
- **`CountRealRoots(IReadOnlyList<BigInteger> coefficients)`** *(static)* → `int`
  Returns the number of distinct real roots, by Sturm's theorem.
- **`Deflate(IReadOnlyList<BigInteger> coefficients, BigInteger numerator, BigInteger denominator)`** *(static)* → `BigInteger[]`
  Divides the polynomial by (denominator·x − numerator), a factor of it.

---

### `PolynomialRoots` Class

The roots of a polynomial: all of them at once numerically, and the rational a numeric root stands for when it is one.

```csharp
public static class PolynomialRoots
```

#### Remarks

`PolynomialRoots.Find` is the method of Aberth: every root is refined at once by a Newton step corrected for the pull of the other roots, which converges from the same starting circle for any polynomial and does not divide a root out, so no root carries the error of the ones found before it. A calculator solves polynomials of degree 2 to 4, where the iteration settles in a few dozen steps.

A root that is rational is recovered exactly rather than left as a decimal: `PolynomialRoots.TryGetRational` reads the continued fraction of the approximation, whose convergents are the only rationals a double can stand for, and `IntegerPolynomial.SignAt` says which of them is a root. That, with `IntegerPolynomial.Deflate`, is what lets a cubic or a quartic that factors over the rationals keep the exact roots of the quadratic that is left.

#### Methods

- **`Find(IReadOnlyList<double> coefficients)`** *(static)* → `Complex[]`
  Returns every complex root of the polynomial, by the method of Aberth.
- **`TryGetRational(double approximation, IReadOnlyList<BigInteger> coefficients, out BigInteger numerator, out BigInteger denominator)`** *(static)* → `bool`
  Returns the rational the approximation stands for, when that rational is a root of the polynomial.
