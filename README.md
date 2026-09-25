# Barbatos.Pallas

A precise scientific calculation engine for .NET 8, 9 and 10, and the desktop calculator built on it.

- **`0.1 + 0.2 = 0.3`,** exactly. Arithmetic uses `decimal`, never binary floating point.
- **About 15 correct significant digits** for sin, ln, √ and the other transcendental functions, from `System.Math`,
  measured against 40-digit references. The reference calculator documents ±1 at the 10th digit.
- **No silent loss.** Where a built-in type would quietly drop digits, Pallas detects it.
- **Functionally standardized against a reference scientific calculator:** arithmetic, fractions, surds, calculus, statistics,
  distributions, equations, inequalities, complex numbers, Base-N, matrices, vectors, spreadsheet, tables and more,
  checked by a conformance suite built from the calculator's own worked examples.

> **Status: Phase 7 - hardening and 1.0.0 - in progress.** The engine runs every application of the reference
> calculator - Calculate, Complex, Base-N, Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio,
> Spreadsheet, Table and Math Box - and every one of their conformance cases passes. The Windows app runs all of them,
> with the calculator's keypad per application, its menus, the graph of a table, and the session, history, window and
> language kept between runs, and it calculates off the window's thread, so a long calculation can be stopped with AC.
> The public API is tracked and described member by member, the calculations are measured against their targets, and
> a GitHub Release publishes the signed packages ([releasing](docs/RELEASING.md)); what remains before 1.0.0 is on
> nuget.org is the release candidate, walked installed. See the [roadmap](docs/ARCHITECTURE.md#11-roadmap).

```csharp
CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

session.Calculate("2⌟3+1⌟1⌟2").Display.Text;        // "13⌟6"
session.Calculate("10√(2)+15×3√(3)").Display.Text;  // "45√(3)+10√(2)"
session.Calculate("d/dx(x³,0.1)").Result;           // exactly 0.03
session.Calculate("14÷0×2").Error;                  // MathError at the division
```

## Packages

Two packages are published, for .NET 8, 9 and 10 on any platform:

```bash
dotnet add package Barbatos.Pallas.Engine
```

```bash
dotnet add package Barbatos.Pallas.DependencyInjection
```

| Package | What it is | Carries |
|---|---|---|
| `Barbatos.Pallas.Engine` | The calculation engine: evaluation, plugin registry, memory, formatting, calculus, Verify, Base-N, Complex, the Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio and Math Box applications, and expressions compiled once to be calculated at many x. No dependencies | Expressions, Numerics, LinearAlgebra, Statistics, Solvers |
| `Barbatos.Pallas.DependencyInjection` | `AddPallas()` for Microsoft.Extensions.DependencyInjection, on Engine | Data, Spreadsheet, Graphing |

The engine is built from smaller libraries, each an assembly and a namespace of its own. They are too small to be worth
a package, so each travels inside the package that references it, and its public types are there to use:

| Library | Purpose | Ships in |
|---|---|---|
| `Barbatos.Pallas.Numerics` | The calculator math .NET lacks: trigonometry in angle units, factorial, nPr, nCr, LCM, prime factors, fraction recognition, degrees-minutes-seconds, erf and erfc, the Poisson probability | Engine |
| `Barbatos.Pallas.Expressions` | Lexer, Pratt parser with the reference calculator's priority, syntax tree, error spans, linear and LaTeX printers | Engine |
| `Barbatos.Pallas.LinearAlgebra` | Exact determinants, inverses and linear systems of decimal matrices | Engine |
| `Barbatos.Pallas.Statistics` | Exact sums, variances and least-squares fits of decimal data; quartile ranks | Engine |
| `Barbatos.Pallas.Solvers` | Exact integer polynomials and the roots of a polynomial: the sign at a rational point, Sturm's count of the real roots, division by a rational root, Aberth's iteration | Engine |
| `Barbatos.Pallas.Data` | CODATA constants, NIST SP 811 units, atomic weights | DependencyInjection |
| `Barbatos.Pallas.Spreadsheet` | The sheet: constants and formulas, relative and absolute references, ranges, fills, circular-reference detection; and number tables of f(x) and g(x) | DependencyInjection |
| `Barbatos.Pallas.Graphing` | Curves sampled across a viewport, with their asymptotes and jumps, and their roots, extrema and intersections on the engine's values | DependencyInjection |

## Documentation

- API reference, every public type and member: [Barbatos.Pallas.Engine](src/core/Barbatos.Pallas.Engine/API-REFERENCE.md) and
  [Barbatos.Pallas.DependencyInjection](src/core/Barbatos.Pallas.DependencyInjection/API-REFERENCE.md)
- [Architecture](docs/ARCHITECTURE.md): packages, dependency graph, engine pipeline, plugin API, roadmap, decisions
- [Precision strategy](docs/PRECISION.md): what Pallas promises, which built-in type holds a value, rounding, where `double` may appear
- [Calculator catalog](docs/CALCULATOR-CATALOG.md): the functional reference
- [Conformance](docs/CONFORMANCE.md): test data format, assumptions, deliberate deviations
- [Canonical Linear Syntax](docs/LINEAR-SYNTAX.md): the text form of expressions

## Build

Requires the .NET 8, 9 and 10 SDKs (the WPF app additionally requires Windows).

```bash
dotnet build Barbatos.Pallas.slnx
```

```bash
dotnet test --solution Barbatos.Pallas.slnx
```

The benchmarks, against the performance targets of [the architecture](docs/ARCHITECTURE.md#9-performance):

```bash
dotnet run --project benchmarks/Barbatos.Pallas.Benchmarks -c Release -- --gate
```

## License

[MIT](LICENSE.md). Barbatos.Pallas is an independent project, not affiliated with or endorsed by any calculator
manufacturer.
