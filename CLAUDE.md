# Barbatos.Pallas - working agreement

A precise scientific calculation engine for .NET built on `decimal`, `double` and `BigInteger`, published as NuGet
packages, plus the desktop (WPF) calculator built on it. Its functional reference is a scientific
calculator, called *the reference calculator* everywhere, and its manual: *what* the calculator does, never *how*
Pallas implements it.

**Precision is the product.** The engine is used in accounting, audit, structural and mechanical engineering and
industrial simulation, where a wrong digit has consequences. Read [docs/PRECISION.md](docs/PRECISION.md) before
touching anything under `src/core`. Any change that weakens it is a design change, discussed first.

Conversation with the maintainer is in Vietnamese. Code, comments, XML docs and `docs/` are in English.

---

## Where things are

| Path | What |
|---|---|
| `src/core/*` | The ten NuGet packages: net8.0;net9.0;net10.0, platform-neutral; no `float`, `Half` or `MathF` |
| `src/app/*` | Presentation (MVVM with no UI framework, so it is unit-tested), the WPF host (Barbatos.Wpf.Core, Aquarius, AquariusRouter, Barbatos.i18n.Wpf, WpfMath) |
| `tests/Barbatos.Pallas.Architecture.Tests` | Dependency graph (`ArchitectureMap`) and the floating-point IL/metadata scanner |
| `tests/Barbatos.Pallas.Conformance.Tests` | Worked examples of the reference calculator's manual as JSON data (`Data/calculator`) |
| `tests/Barbatos.Pallas.Numerics.Tests` | Accuracy against PeterO.Numbers 40-digit references (50-digit for erf, erfc and Poisson), CsCheck properties, manual values |
| `tests/Barbatos.Pallas.Expressions.Tests` | Precedence as tree shapes, print-and-reparse properties per application, fuzzing, lexer allocations |
| `tests/Barbatos.Pallas.Engine.Tests` | The precision rule value by value, display forms, calculus, Verify, Base-N, matrices and vectors, statistics, distributions, plugins, integrals against PeterO.Numbers, evaluation properties |
| `tests/Barbatos.Pallas.LinearAlgebra.Tests` | Exact determinants, inverses and linear systems against the Leibniz formula and multiplication back |
| `tests/Barbatos.Pallas.Statistics.Tests` | Exact sums, variances and fits against the manual's fractions, two-pass definitions and normal equations; quartile ranks |
| `tests/Barbatos.Pallas.Solvers.Tests` | Integer polynomials against polynomials built from known roots (sign, square-free part, Sturm count, division); iterated roots against those roots |
| `tests/Barbatos.Pallas.Spreadsheet.Tests` | The sheet's constants, formulas, references, ranges, fills and capacity, and number tables with their row limits and Verify |
| `tests/Barbatos.Pallas.Data.Tests` | CODATA, NIST and CIAAW data against their defining relations and the vocabulary |
| `tests/Barbatos.Pallas.DependencyInjection.Tests` | `AddPallas()` through a real service provider |
| `tests/Barbatos.Pallas.Presentation.Tests` | The shell without a window: the application registry against the engine, every setting, the stored session written, read back and restored, the keypad table, the math input with its two writers and its reader, the Calculate screen, the ten application screens over one grid of values, every key of every application typed and read by that application's parser (`ApplicationKeypadTests`), and properties over random sequences of keys in a random application |
| `tests/Barbatos.Pallas.Wpf.Tests` | The only test project that needs Windows: every LaTeX the printers and the math input emit is drawn by WpfMath, every screen is built with the theme loaded (`ScreenResourceTests`), and the session in the preferences |
| `build/BannedSymbols.FloatingPoint.txt` | Banned single-precision types: `float`, `Half`, `MathF` |
| `docs/ARCHITECTURE.md` | Packages, graph, pipeline, plugin API, roadmap and **decision log** |
| `docs/PRECISION.md` | The precision contract |
| `docs/CALCULATOR-CATALOG.md` | Everything the calculator does, with manual pages |
| `docs/CONFORMANCE.md` | Conformance data format, unverified behaviors and working assumptions (U1-U26), deliberate deviations (D1-D7) |
| `docs/LINEAR-SYNTAX.md` | Canonical Linear Syntax: tokens, priority levels, contexts, the text-vs-keys decisions |
| `docs/reference-manual_VI.pdf` | The reference calculator's manual, kept locally. The manufacturer's copyright: gitignored (`docs/*.pdf`), never commit or redistribute it |
| `build/Render-ManualPages.ps1` | Renders pages of the manual to PNG with Windows' own PDF API, to read pages whose content is only an image |

## Current phase

**Phase 5 - the WPF app - in progress** (roadmap in docs/ARCHITECTURE.md §11). Milestones: M1 the shell ✅,
M2 keypad and math input ✅, M3 Calculate end to end ✅, M4 the other screens ✅, M4.5 the calculator's face (a layout
and look ✅, b a keypad per application ✅, c CATALOG), M5 persistence and shortcuts,
M6 hardening and the installer. Phase 4 is complete: every application but Math Box, which is Phase 6, works end to
end.

- The app: `CalculatorShellViewModel` owns the one session; `CalculatorApps` is the registry every screen and the
  route table read; `SettingsViewModel` is Calc Settings; `SessionSnapshotJson` and `ISessionStore` keep the session
  between runs (`PreferencesSessionStore` in the host). The host is `WpfProgram` + `AppRoutes` + `Views/`, with the
  text in `Locales/*.yaml`. Every application has its own view model over that session (`MatrixViewModel`,
  `VectorViewModel`, `StatisticsViewModel`, `DistributionViewModel`, `EquationViewModel`, `InequalityViewModel`,
  `RatioViewModel`, `TableViewModel`, `SpreadsheetViewModel`, `BaseNViewModel`), built the first time it is opened
  and kept; numbers are typed into the one `ValueGridViewModel`, whose cells calculate what was typed through the
  session. Screens of applications whose own screen is still to come render `AppScreenView`. The look is
  `Theme/Tokens.xaml` (colours, roundings, sizes) and `Theme/Controls.xaml` (every style); `KeypadView` draws the
  key table and `CalculationPanelView` the one display, input above and answer below.
- The line the user types on is `MathDocument` (immutable, a cursor path through template slots), written as
  Canonical Linear Syntax for the engine and as LaTeX for the screen, and read back by `MathDocumentReader` through
  the engine's own parser. The keypad is the table in `Keypad`, and each application draws its own rows of it
  (`Keypad.RowsFor`): the keys of what it reads and no others - Base-N has no functions, only Matrix has MatA. The
  keyboard maps onto the same table (`KeyboardMap`) through the same filter (`Keypad.Has`), and
  `InputCommandRouter` is the only place a key becomes an edit. `CalculateViewModel` calculates, shows the result
  (FORMAT and S⇔D), walks the history, and puts the cursor where an error says it is.
- Numerics holds only what .NET lacks: `Trigonometry`, `IntegerFunctions`, `Fractions`, `Sexagesimal`, `AngleUnit`,
  `ErrorFunction` and `PoissonDistribution`.
- Expressions reads Canonical Linear Syntax into an immutable syntax tree and prints it back, as linear text or LaTeX.
  It evaluates nothing.
- Engine binds, compiles and evaluates that tree: `Value` with the precision rule, exact display forms, sessions and
  memory, the formatter and the FORMAT conversions, calculus, Verify, Complex and Base-N, and the plugin API.
- Data ships the CODATA 2022 constants, the NIST SP 811 unit conversions and the CIAAW atomic weights;
  DependencyInjection ships `AddPallas()`.
- LinearAlgebra holds exact elimination of `decimal` matrices (`ExactLinearAlgebra`); the Matrix and Vector
  applications live in Engine (`MatrixValue`, `VectorValue`, MatA-MatD, VctA-VctD).
- Statistics holds exact statistics of `decimal` data scaled to integers (`ExactSample`, `Quartiles`), with no
  `double`; the Statistics application lives in Engine (`StatisticsData`, `RegressionModel`, the statistic variables
  as names in the input).
- The Distribution application is a form of `CalculatorSession` (`CalculateDistribution`, `DistributionKind`,
  `DistributionParameters`): binomial exact on `BigInteger`, normal through `ErrorFunction`, Poisson through
  `PoissonDistribution`.
- Spreadsheet holds the sheet (`SpreadsheetGrid`, `SpreadsheetCell`) and the number table (`NumberTable`,
  `TableType`), both driven through a session; the engine only reads cells, through `CalculatorSession.CellValues`
  and `CellAddress`, and calculates without storing anything through `CalculatorSession.Evaluate`.
- Solvers holds polynomial algebra and nothing else, with no dependencies: exact integer polynomials
  (`IntegerPolynomial`: the sign at a rational point, Sturm's count of the real roots, the square-free part, division
  by a rational root) and the iterated roots of a polynomial (`PolynomialRoots`, Aberth, the only place `double` is
  allowed there). The Equation, Inequality and Ratio applications live in Engine (`SolveSimultaneous`,
  `SolvePolynomial`, `SolveEquation`, `SolveInequality`, `SolveRatio`), which is why Engine references Solvers and
  not the other way round (decision of 22 Sep 2026).

Math Box, the last application, has its conformance cases skipped with Phase 6, which implements it.

## Build and test

```bash
dotnet build Barbatos.Pallas.slnx
```

```bash
dotnet test --solution Barbatos.Pallas.slnx
```

One project, one framework, while iterating:

```bash
dotnet test --project tests/Barbatos.Pallas.Architecture.Tests -f net10.0
```

Things that will save time:

- **Tests run on Microsoft.Testing.Platform, not VSTest.** `global.json` opts in, because xunit.v3 4.x refuses
  VSTest on the .NET 10 SDK. That means:
  - `dotnet test` takes `--project` / `--solution`, not a positional path;
  - there is no `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`;
  - coverage comes from `Microsoft.Testing.Extensions.CodeCoverage`.
- **Count the test assemblies, not just the summary.** With `--no-build`, a test project that failed to compile for
  one framework is silently missing from the run and the summary still says "Passed!". This happened on 17 Sep 2026
  (a net8.0-only compile error). A full run is 12 test projects × 3 frameworks + Wpf.Tests, which is Windows-only,
  = 37 assemblies.
- **Packing.** `dotnet pack Barbatos.Pallas.slnx -c Release -o artifacts/packages` packs the ten core packages.
  Packing one project does not pack its project references.
- **Mutation testing** (Stryker.NET, pinned in `dotnet-tools.json`; gate ≥ 90% per package):

  ```bash
  dotnet tool restore
  ```

  ```bash
  dotnet stryker --skip-version-check
  ```

  Run both from `tests/Barbatos.Pallas.Numerics.Tests`, `tests/Barbatos.Pallas.Expressions.Tests`,
  `tests/Barbatos.Pallas.Engine.Tests`, `tests/Barbatos.Pallas.LinearAlgebra.Tests`,
  `tests/Barbatos.Pallas.Statistics.Tests`, `tests/Barbatos.Pallas.Solvers.Tests`,
  `tests/Barbatos.Pallas.Spreadsheet.Tests`, `tests/Barbatos.Pallas.DependencyInjection.Tests` or
  `tests/Barbatos.Pallas.Presentation.Tests`, whose
  `stryker-config.json` selects the MTP runner and ignores string mutations (exception messages are not results). Data
  has no Stryker run: it is all static initialization (below). Stryker builds the test project it runs from, so a
  `dotnet test` of that project during a run replaces the mutated assembly, and a deliberately failing scratch test
  aborts the run. Four traps:
  - a `while (true)` makes Stryker skip every mutant in the method, because `while (false)` does not compile; write
    `for (;;)`;
  - a domain check that `System.Math` would also reject survives unless the test asserts `WithParameterName`;
  - the MTP runner cannot switch mutants inside static initialization, so they are all reported as survivors.
    `StandardVocabulary.cs` is data built once for `SyntaxVocabulary.Standard` and is excluded from mutation.
    Deleting one of its lines by hand fails 13 to 18 tests (17 Sep 2026); the vocabulary tests and the conformance
    inputs are what guard it. A lookup table is written out rather than built by a loop (`DecimalDigits.PowersOfTen`);
  - a property test that samples thousands of cases makes Stryker's *initial* run report failures that a plain
    `dotnet test` never shows (Presentation, 23 Sep 2026: four CsCheck properties at 2000 iterations). Keep a
    property at a few hundred iterations, or the run never starts;
  - a mutant that cannot change a result (a guard `System.Math` already applies, a branch no input reaches) is a
    reason to simplify the code, not to write a test that pins an implementation detail. The Engine went from 75% to
    92.5% on 18 Sep 2026 that way and by edge tests, and three real bugs surfaced (docs/ARCHITECTURE.md §12).
- **SourceLink** is referenced only in CI or with `-p:SourceLinkEnabled=true`. A local repository without a remote
  would otherwise warn three times per project per framework.

## Hard rules

### Numbers: use what .NET provides

- **Never re-implement, or wrap, what the BCL already has.** Check `Math`, the static members of `System.Double`
  (`double.Pi`, `double.SinPi`, `double.DegreesToRadians`, `double.RootN`, `double.Ieee754Remainder`, …),
  `System.Decimal` and `BigInteger` first: the table in PRECISION.md §3 lists them. The maintainer had two layers
  deleted on 17 Sep 2026: a custom number tower (`BigRational`, `BigDecimal`, `RoundingMode`), then wrappers
  (`MathConstants`, `DecimalArithmetic`, `ScientificMath`).
  - `decimal` for arithmetic;
  - `Math.Round(decimal, int, MidpointRounding)` for rounding;
  - `double` + `System.Math` for transcendental functions;
  - `BigInteger` for integer functions;
  - `int` for Base-N;
  - `System.Numerics.Complex` for complex numbers.

  Add only what the BCL lacks (angle units with gradians, factorial, fraction recognition), and say why in
  `<remarks>`. A wrapper that only turns NaN into an exception is not something the BCL lacks: the engine checks
  `double.IsFinite` once.
- **`double` is `System.Double`.** C# keywords are aliases: `double` = `System.Double`, `int` = `System.Int32`,
  `long` = `System.Int64`, `float` = `System.Single`, `decimal` = `System.Decimal`. Write the keyword (IDE0049 is a
  warning, so an error here), as Barbatos.Wpf and Barbatos.i18n do: `double.Pi`, not `Double.Pi`.
- **`decimal` loses small values silently** (`decimal.Parse("6.62607015E-34")` is 0). Where values are created,
  apply the 10⁻¹⁴ rule of PRECISION.md §3 after `decimal.TryParse`.
- **`(decimal)double` keeps 15 significant digits, and the 15th can be off by one** (`Math.E` → 2.71828182845904).
  Tests compare such values within a tolerance or at the 10 displayed digits. Exact comparisons are only for
  special angles and for tests that pin a measured platform behavior.
- **`float`, `Half` and `MathF` nowhere in `src/core`; `double` and `System.Numerics.Complex` only in assemblies on
  `CoreFloatingPointTests.AllowList`** (Numerics now, Engine from Phase 3). Two locks enforce it:
  1. `BannedApiAnalyzers` (RS0030, an error) bans the single-precision types;
  2. `CoreFloatingPointTests` scans compiled IL and metadata against `AllowList`.
- **The analyzer alone is not enough.** It was measured to catch member use but *not* declarations: a `double` field,
  parameter or local and a literal `1.5` compile cleanly. Only the scanner sees those. Never weaken or skip the
  scanner; if it misbehaves, fix it and keep its self-tests (`FloatingPointScannerTests`) green.
- **A new `AllowList` entry** (Graphing's screen coordinates in Phase 6 is the expected one) is reviewed like a change
  to PRECISION.md.
- **Always pass a `MidpointRounding`.** .NET's defaults disagree: `Math.Round(2.5m)` is 2, `2.5m.ToString("F0")` is 3.
  The display default is `AwayFromZero`.
- **Rounding only at explicit boundaries:** display, `Rnd`/`Round`, `double` → `decimal` conversion, and entering a
  bounded domain (PRECISION.md §5). Round `decimal`, never `double`: `Math.Round(1.005, 2, AwayFromZero)` is 1.
- **Accuracy claims are measured.** A function on `double` gets an accuracy test against PeterO.Numbers
  (`tests/Barbatos.Pallas.Numerics.Tests/Support/Reference.cs`). Mind the three PeterO 1.8.2 defects documented there.

### Engine

- **The precision rule lives in `Value` and `ValueMath`,** nowhere else: a literal is read into `decimal`; a
  `double` result becomes a 15-digit `decimal` unless its magnitude is below 10⁻¹⁴ or beyond 7.9×10²⁸; a `decimal`
  product or quotient that lands below 10⁻¹⁴ is recomputed from the operands in `double`.
- **NaN, infinity and the profile's range are Math ERROR in one place** (`ValueMath.Real`). A function returns what
  `System.Math` returns; it does not invent its own error.
- **An exact form is display metadata, not a number type.** It is dropped, never guessed, when an operation leaves
  what it can represent. A value that has one is converted to `double` through the form.
- **Errors are values:** `EvalResult` carries a `CalcErrorKind`; nothing throws for calculator input, and every error
  reaches the caller with the span the calculator would put the cursor at.
- **Every long loop counts against the budget** (`EvaluationContext.TryIterate`): Σ, Π, ∫ and the integrator. A
  budget that runs out is Time Out, never a hang.
- **A behavior the manual does not state is an assumption**, listed as U12-U26 in docs/CONFORMANCE.md, not a quiet
  choice in the code.

### Expressions

- **Canonical Linear Syntax is a stored format.** History and sessions keep expressions as CLS text, so a spelling or
  priority change is a decision-log entry with a plan for text already saved. The text-vs-keys decisions are in
  docs/LINEAR-SYNTAX.md §5.
- **Numbers stay text in the tree.** Expressions has no dependencies and no `double`; turning a literal into a value is
  the engine's job (PRECISION.md §3).
- **A new spelling goes into `SyntaxVocabulary`,** never into the lexer as a special case, and must not make an
  existing input read differently under longest match. The print-and-reparse property in `LinearPrinterTests` and the
  fuzz tests are what catch that; keep them passing rather than narrowing their generators.
- **Parsing never throws for input.** Errors are a `SyntaxErrorCode` with a span; only null arguments throw.

### Architecture

- **`ArchitectureMap` is the dependency graph.** A new `ProjectReference` means updating the map *and* the diagram
  in docs/ARCHITECTURE.md §2, or the tests fail.
- **Core `.csproj` files never set `TargetFramework(s)`;** `src/core/Directory.Build.props` owns it (tested).
- **No reflection-based discovery, no `InternalsVisibleTo`.** iOS needs full AOT, and test needs are met through
  public API.
- **The core is language-neutral:** errors are `CalcErrorKind` + span, and text is localized in the app.

### Conformance data

- **A case is never marked passed before its engine exists.** Skip with the phase.
- **Derived expected values are computed exactly** (rationals, integer square roots), never with `double`.
  Transcendental values (`expectationSource: reference`) come from PeterO.Numbers series at 60 digits, cross-checked
  against a known constant (docs/CONFORMANCE.md §5), never from `System.Math` or the code under test.
- **An assumption is a `note`,** listed under U1-U26 in docs/CONFORMANCE.md. A deliberate difference from the
  calculator is a D-entry there, never a silent engine change.

### The window

- **A screen is a `Grid` with explicit rows, never a `DockPanel`.** A `DockPanel` gives a docked child the height it
  asks for and squeezes the rest out: at the default window size that cost the Base-N screen its number-base row and
  cut a fraction on the line in half (23 Sep 2026). Auto for the header, the controls and the calculation panel; `*`
  for the content, which is what scrolls.
- **The calculator shares the screen by weight and its keys share the keypad's height.** An Auto keypad cannot give
  way: ten rows under the Matrix grid lost the row with = off the window. The keypad rows are a `UniformGrid`, the
  panel row is `5*` or `6*` under `PanelMaxHeight`, and a key's content only ever shrinks (`Viewbox` DownOnly).
- **A key an application cannot read is not on its keypad.** Adding a key means adding it to the rows of the
  applications that read it; `ApplicationKeypadTests` types it there and fails if that application has no token for
  it. A key whose meaning changes in an application (the brackets and × ÷ in Base-N) is an entry in
  `Keypad.Of(id, app)`, not a second key.
- **No view sets a colour, a corner or a font size of its own.** They are in `Theme/Tokens.xaml`, and every style is
  in `Theme/Controls.xaml`. What a key looks like follows its `KeyGroup`, which is part of the keypad table.
- **A new style or token is used through `StaticResource` and covered by `ScreenResourceTests`,** which builds every
  screen with the theme: a key no dictionary has compiles and throws only when that screen is opened.

### Build and style

- **Central Package Management:** versions only in `Directory.Packages.props`.
- **`TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisMode Recommended`.** Culture analyzers
  CA1304/CA1305/CA1310 are errors, because a decimal comma is a wrong result.
- **C# 14 on three runtimes.** Compiler-only features are free; runtime types (`System.Threading.Lock`,
  `FrozenDictionary.GetAlternateLookup`) need `#if NET9_0_OR_GREATER`.
- **Style:**
  - explicit types rather than `var`;
  - file-scoped namespaces;
  - `_camelCase` private fields;
  - the MIT header from `.editorconfig` on every `.cs` file (IDE0073 is an error).
- **XML docs on every public member** (CS1591). `<remarks>` carries the *why*, including the measurement or
  failure that produced a rule.
- **`Directory.Build.targets` must exist** even where it adds nothing. **`.editorconfig` keeps `root = true`.**
- **A public API change updates the package's `README.md`** (and later `API-REFERENCE.md`), and every example a
  README shows is run by that package's `ReadmeTests`: a README that drifts from the API is a defect.

### Identity

- **No brand or model name of the reference calculator anywhere in the repository** (maintainer, 18 Sep 2026: a
  legal risk). Not in identifiers, file or folder names, comments, XML docs, READMEs, package metadata, docs or test
  data. Write *the reference calculator*, *the manual*, or the neutral names in use: `CalculatorProfile.Standard`,
  `SyntaxVocabulary.Standard`, `Data/calculator`, docs/CALCULATOR-CATALOG.md.
- **The WPF app's `AppInfo.AppId` `{5E50D3E6-0148-428F-88C9-F824709C75C4}` and `Company` "Barbatos Labs" are
  immutable after the first release.** They name the user-data folder.

## Repository state

- Local git repository, **no remote yet**. The planned remote is `Barbatos-Labs/Barbatos.Pallas`.
- Commit or push only when the maintainer asks.
