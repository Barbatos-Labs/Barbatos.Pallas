# Barbatos.Pallas

A precise scientific calculation engine for .NET 8, 9 and 10, and the desktop calculator built on it.

- **`0.1 + 0.2 = 0.3`,** exactly. Arithmetic uses `decimal`, never binary floating point.
- **About 15 correct significant digits** for sin, ln, √ and the other transcendental functions, from `System.Math`,
  measured against 40-digit references. The reference calculator documents ±1 at the 10th digit.
- **No silent loss.** Where a built-in type would quietly drop digits, Pallas detects it.
- **Functionally standardized against a reference scientific calculator:** arithmetic, fractions, surds, calculus, statistics,
  distributions, equations, inequalities, complex numbers, Base-N, matrices, vectors, spreadsheet, tables and more,
  checked by a conformance suite built from the calculator's own worked examples.

> **Status: Phase 5 - the desktop app - in progress.** The engine is complete for every application but Math Box -
> Calculate, Complex, Base-N, Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio, Spreadsheet and
> Table - and every one of their conformance cases passes. The WPF app has its shell: the home screen, the settings
> and a session that survives a restart; the screens follow milestone by milestone. See the
> [roadmap](docs/ARCHITECTURE.md#11-roadmap).

```csharp
CalculatorSession session = PallasEngineBuilder.CreateDefault().Build().CreateSession();

session.Calculate("2⌟3+1⌟1⌟2").Display.Text;        // "13⌟6"
session.Calculate("10√(2)+15×3√(3)").Display.Text;  // "45√(3)+10√(2)"
session.Calculate("d/dx(x³,0.1)").Result;           // exactly 0.03
session.Calculate("14÷0×2").Error;                  // MathError at the division
```

## Packages

| Package | Purpose | Phase |
|---|---|---|
| `Barbatos.Pallas.Numerics` | The calculator math .NET lacks: trigonometry in angle units, factorial, nPr, nCr, LCM, prime factors, fraction recognition, degrees-minutes-seconds, erf and erfc, the Poisson probability | 1 |
| `Barbatos.Pallas.Expressions` | Lexer, Pratt parser with the reference calculator's priority, syntax tree, error spans, linear and LaTeX printers | 2 |
| `Barbatos.Pallas.Engine` | Evaluation, plugin registry, memory, formatting, calculus, Verify, Base-N, Complex, and the Matrix, Vector, Statistics, Distribution, Equation, Inequality and Ratio applications | 3 |
| `Barbatos.Pallas.Data` | CODATA constants, NIST SP 811 units, atomic weights | 3 |
| `Barbatos.Pallas.DependencyInjection` | `AddPallas()` for Microsoft.Extensions.DependencyInjection | 3 |
| `Barbatos.Pallas.LinearAlgebra` | Exact determinants, inverses and linear systems of decimal matrices | 4 |
| `Barbatos.Pallas.Statistics` | Exact sums, variances and least-squares fits of decimal data; quartile ranks | 4 |
| `Barbatos.Pallas.Solvers` | Exact integer polynomials and the roots of a polynomial: the sign at a rational point, Sturm's count of the real roots, division by a rational root, Aberth's iteration | 4 |
| `Barbatos.Pallas.Spreadsheet` | The sheet: constants and formulas, relative and absolute references, ranges, fills, circular-reference detection; and number tables of f(x) and g(x) | 4 |
| `Barbatos.Pallas.Graphing` | Platform-neutral function graphing | 6 |

## Documentation

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

## License

[MIT](LICENSE.md). Barbatos.Pallas is an independent project, not affiliated with or endorsed by any calculator
manufacturer.
