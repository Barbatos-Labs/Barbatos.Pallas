# Barbatos.Pallas.Engine

The Barbatos.Pallas calculation engine: binding, a function and constant plugin registry, compiled evaluation, calculator memory and sessions, result formatting, calculus, Verify, Base-N, matrices, vectors, statistics, distributions, equations, inequalities and ratios.

> **Status: 1.0.** The API below is tested on .NET 8, 9 and 10, and it follows semantic versioning: an incompatible
> change waits for 2.0.

## Installing

```bash
dotnet add package Barbatos.Pallas.Engine
```

The package has no dependencies. It carries the libraries the engine is built on, each an assembly and a namespace of
its own, whose public types can be used directly: `Barbatos.Pallas.Expressions` (the lexer, the parser and the linear
and LaTeX printers), `Barbatos.Pallas.Numerics`, `Barbatos.Pallas.LinearAlgebra`, `Barbatos.Pallas.Statistics` and
`Barbatos.Pallas.Solvers`. The reference data - CODATA constants, NIST unit conversions, CIAAW atomic weights - and the
spreadsheet come with
[Barbatos.Pallas.DependencyInjection](https://www.nuget.org/packages/Barbatos.Pallas.DependencyInjection), which
depends on this package.

## One calculation

```csharp
using Barbatos.Pallas.Engine;

PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
CalculatorSession session = engine.CreateSession();          // Calculate, Standard profile

session.Calculate("2⌟3+1⌟1⌟2").Display.Text;                 // "13⌟6"
session.Calculate("√(2)×3").Display.Text;                    // "3√(2)"
session.Calculate("10√(2)+15×3√(3)").Display.Text;           // "45√(3)+10√(2)"
session.Calculate("d/dx(x³,0.1)").Result.ToDecimal();        // exactly 0.03
session.Calculate("0.1+0.2").Result.ToDecimal();             // exactly 0.3
```

Input is [Canonical Linear Syntax](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/LINEAR-SYNTAX.md),
the one text form of an expression. A calculation answers with a value or a `CalcError`: the kind the calculator
shows and the span it would place the cursor at. Nothing throws for bad input.

```csharp
Calculation calculation = session.Calculate("14÷0×2");
calculation.Succeeded;              // false
calculation.Error!.Value.Kind;      // CalcErrorKind.MathError
calculation.Error!.Value.Span;      // (0, 4), the division
```

## Values

A `Value` holds a real number as `decimal` or `double`, a complex number, a 32-bit Base-N integer, or a matrix or
vector of real numbers, following the
precision rule of
[docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md) §3: decimal input
stays `decimal`, and a `double` result becomes a `decimal` of 15 significant digits unless its magnitude is below
10⁻¹⁴ or beyond 7.9×10²⁸.

A value computed exactly from square roots and π also remembers its exact form, so `√(2)×√(2)` is exactly 2 and
`π÷2` reaches the trigonometric functions as exactly `double.Pi / 2`.

## Settings, profiles and formatting

```csharp
session.Settings = session.Settings with
{
    AngleUnit = AngleUnit.Radian,
    NumberFormat = NumberFormat.Fix(3),
    InputOutput = InputOutput.MathIDecimalO,
};

Calculation result = session.Calculate("π÷6");
session.Format(result, FormatTarget.DecimalValue)!.Text;     // "0.524"
session.Format(result, FormatTarget.Standard)!.Text;         // "1⌟6π"
result.Display.Latex;                                        // LaTeX for copy and export
```

`CalculatorProfile.Standard` keeps the calculator's calculation range, function domains and display bounds;
`CalculatorProfile.Extended` reaches the range of `double` and shows wider forms.

## Memory, Verify and Base-N

```csharp
session.Calculate("3+5");
session.Store(MemoryVariable.A);                             // STO A
session.Calculate("A×10").Display.Text;                      // "80"
session.Define(DefinedFunction.F, "x²+1");
session.Calculate("f(5)").Display.Text;                      // "26"

session.Settings = session.Settings with { Verify = true };
session.Calculate("2²=2+2=4").IsTrue;                        // true

session.SwitchApp(CalculatorApp.BaseN);
session.Settings = session.Settings with { BaseMode = NumberBase.Bin };
session.Calculate("Not(1010)").Display.Text;                 // "11111111111111111111111111110101"
```

## Matrices and vectors

The Matrix and Vector applications hold MatA-MatD and VctA-VctD, and the last matrix or vector result in `MatAns`
or `VctAns`. Every entry is a `Value`, so it follows the precision rule and keeps its exact form. A determinant and an
inverse of `decimal` entries are exact: the matrix is eliminated without fractions on `BigInteger`
(Barbatos.Pallas.LinearAlgebra) and rounded once, so a singular matrix is never mistaken for an invertible one.

```csharp
session.SwitchApp(CalculatorApp.Matrix);
session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,]
{
    { Value.FromDecimal(2m), Value.One },
    { Value.One, Value.One },
}));
session.Calculate("MatA⁻¹").Display.Text;                    // "[[1, -1], [-1, 2]]"
session.Calculate("Det(MatA)").Display.Text;                 // "1"

session.SwitchApp(CalculatorApp.Vector);
session.SetVector(VectorVariable.VctA, new VectorValue(Value.FromDecimal(3m), Value.FromDecimal(4m)));
session.Calculate("Abs(VctA)").Display.Text;                 // "5"
session.Calculate("UnitV(VctA)").Display.Text;               // "[0.6, 0.8]"
```

The `Standard` profile holds matrices up to 4×4 and vectors of 2 or 3 dimensions; the `Extended` profile up to 64.

## Statistics

The Statistics application calculates from `StatisticsData`: x values, or (x, y) pairs, with an optional frequency
column. Its statistic variables are names in the input, as on the calculator's Statistics Calc screen. Sums, means,
variances and the linear and quadratic regressions are exact (Barbatos.Pallas.Statistics) and rounded once, and a
statistic result is displayed as a decimal.

```csharp
session.SwitchApp(CalculatorApp.Statistics);
session.SetStatisticsData(new StatisticsData(
    x: [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(3m)],
    frequencies: [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(1m)]));
session.Calculate("x̄").Display.Text;                         // "2"
session.Calculate("σx").Display.Text;                        // "0.7071067812"
session.Calculate("P(3▶t)").Display.Text;                    // "0.9213503965", the normal distribution

session.SetStatisticsData(new StatisticsData(
    [Value.FromDecimal(1m), Value.FromDecimal(2m), Value.FromDecimal(3m)],
    [Value.FromDecimal(6m), Value.FromDecimal(12m), Value.FromDecimal(24m)]));
session.Regression = RegressionModel.ExponentialAB;          // y = a·b^x
session.Calculate("b").Display.Text;                         // "2"
session.Calculate("4ŷ").Display.Text;                        // "48"
```

The `Standard` profile holds 160, 80 or 53 rows of one, two or three columns; the `Extended` profile 10,000.

## Distributions

The Distribution application is a form: a calculation type and its parameters. A binomial probability is exact, rounded
once; the normal distribution and the Poisson probability are accurate to the last digits of `double`.

```csharp
session.SwitchApp(CalculatorApp.Distribution);
DistributionParameters parameters = new() { Trials = Value.FromDecimal(5m), Probability = Value.FromDecimal(0.5m) };

// The List input method: one result per x, and Ans unchanged.
session.CalculateDistribution(DistributionKind.BinomialCD, parameters,
    [Value.FromDecimal(2m), Value.FromDecimal(3m)]);           // "0.5", "0.8125"

// The Variable input method: the result goes to Ans.
session.CalculateDistribution(DistributionKind.NormalPD, new DistributionParameters
{
    X = Value.FromDecimal(36m), Mean = Value.FromDecimal(35m), StandardDeviation = Value.FromDecimal(2m),
}).Display.Text;                                             // "0.1760326634"
```

A parameter outside its domain is a Math ERROR of that calculation, and a binomial too long for the budget is a Time Out.

## Equations, inequalities and ratios

Simultaneous equations and polynomials are solved exactly wherever they can be: a system is eliminated without
fractions, and a polynomial keeps the exact form of its roots wherever it factors over the rationals
(Barbatos.Pallas.Solvers). The Solver is Newton's method from an initial value, and reports Left − Right.

```csharp
static Value N(decimal number) => Value.FromDecimal(number);

session.SwitchApp(CalculatorApp.Equation);

// x − y + z = 2, x + y − z = 0, −x + y + z = 4: the rows of the augmented matrix.
Value[,] system = { { N(1), N(-1), N(1), N(2) }, { N(1), N(1), N(-1), N(0) }, { N(-1), N(1), N(1), N(4) } };
session.SolveSimultaneous(system).Unknowns.Select(unknown => unknown.Display.Text);   // "1", "2", "3" for x, y, z

// x² + 2x − 2, from the highest degree down: the roots are −1 ± √3 and the minimum is at (−1, −3).
PolynomialSolution quadratic = session.SolvePolynomial([N(1), N(2), N(-2)]);
quadratic.Roots[0].Real.Display.Text;                        // "-1+√(3)"
quadratic.Extrema[0].Y.Display.Text;                         // "-3"

// The Solver: the solution goes to the variable, and Left − Right comes back as the second result.
session.SetVariable(MemoryVariable.B, N(4));
Calculation solved = session.SolveEquation("x²-B²=0", MemoryVariable.X, N(1));
solved.Display.Text;                                         // "4", with Left − Right of 0

session.SwitchApp(CalculatorApp.Inequality);
session.SolveInequality([N(1), N(2), N(-3)], RelationOperator.GreaterOrEqual).Text;   // "x≤-3, 1≤x"

session.SwitchApp(CalculatorApp.Ratio);
session.SolveRatio(RatioForm.XInSecondRatio, N(3), N(8), N(12)).Display.Text;         // "9⌟2" for 3:8 = X:12
```

A polynomial with complex roots gives them as a real and an imaginary part, each with its own exact form
(`-3⌟4+√(23)⌟4i`), because a complex value holds two `double` numbers and cannot carry those forms. With Complex Roots
off, only the real roots are shown, and `SolutionOutcome.NoRealRoots` says there are none.

## Spreadsheet cells and tables

The Spreadsheet application's grid is Barbatos.Pallas.Spreadsheet; the engine reads its cells. A formula of the
Spreadsheet application may name a cell (`A1`, `$A$1`) or a range through `Min(`, `Max(`, `Mean(` and `Sum(`, and the
values come from `CellValues`. `Evaluate` calculates without storing anything — no Ans, no history — which is how an
application calculates a cell or a table row on the user's behalf.

```csharp
CalculatorSession sheet = engine.CreateSession(CalculatorApp.Spreadsheet);
sheet.CellValues = address => Value.FromDecimal(address.Row + 1);   // A1 = 1, A2 = 2, …

sheet.Evaluate("A2+7").Display.Text;                         // "9"
sheet.Evaluate("Sum(A1:A4)").Display.Text;                   // "10"
sheet.Ans;                                                   // still 0: Evaluate stores nothing
```

## Plugins and data

Scientific constants, unit conversions and atomic weights come from data sets, such as those of
`Barbatos.Pallas.Data`. A plugin function joins the vocabulary and is evaluated like a built-in one.

```csharp
public sealed class BeamDeflection : IMathFunction
{
    public FunctionSignature Signature { get; } = new("beam(", 2, 2);

    public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
    {
        return arguments[0].ToDouble() < 0d
            ? EvalResult.Failure(CalcErrorKind.MathError)
            : Value.FromDecimal(arguments[0].ToDecimal() * arguments[1].ToDecimal());
    }
}

PallasEngine engine = PallasEngineBuilder.CreateDefault()
    .AddConstantSet(ConstantSets.Codata2022)      // Barbatos.Pallas.Data
    .AddUnitSet(UnitSets.NistSp811)
    .AddFunction(new BeamDeflection())
    .Build();
```

## Budget

Every calculation takes a `CancellationToken` and an `EngineBudget`. Exceeding either is a `TimeOut` error, never a
hang: `Σ`, `Π`, `∫` and `d/dx` count their evaluations.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
