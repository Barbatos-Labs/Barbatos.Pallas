# Architecture

- [1. Solution layout](#1-solution-layout)
- [2. Dependency graph](#2-dependency-graph)
- [3. Package responsibilities](#3-package-responsibilities)
- [4. Clean Architecture rules](#4-clean-architecture-rules)
- [5. The engine pipeline](#5-the-engine-pipeline)
- [6. Extending the engine: functions, constants, units](#6-extending-the-engine-functions-constants-units)
- [7. Growing without breaking the core](#7-growing-without-breaking-the-core)
- [8. Presentation: MVVM, keypad, math input, graphs](#8-presentation-mvvm-keypad-math-input-graphs)
- [9. Performance](#9-performance)
- [10. Build and packaging conventions](#10-build-and-packaging-conventions)
- [11. Roadmap](#11-roadmap)
- [12. Decision log](#12-decision-log)

Precision rules live in [PRECISION.md](PRECISION.md); the functional reference in
[CALCULATOR-CATALOG.md](CALCULATOR-CATALOG.md); the conformance data format in [CONFORMANCE.md](CONFORMANCE.md).

---

## 1. Solution layout

```
Barbatos.Pallas/
├─ Barbatos.Pallas.slnx · Directory.Build.props/.targets · Directory.Packages.props · global.json
├─ build/        package icon, BannedSymbols.FloatingPoint.txt
├─ docs/         this document, PRECISION, CALCULATOR-CATALOG, CONFORMANCE, LINEAR-SYNTAX
├─ src/
│  ├─ core/      net8.0;net9.0;net10.0 · published to nuget.org · no float/Half/MathF
│  │  ├─ Barbatos.Pallas.Numerics            ├─ Barbatos.Pallas.Solvers
│  │  ├─ Barbatos.Pallas.LinearAlgebra       ├─ Barbatos.Pallas.Spreadsheet
│  │  ├─ Barbatos.Pallas.Statistics          ├─ Barbatos.Pallas.Data
│  │  ├─ Barbatos.Pallas.Expressions         ├─ Barbatos.Pallas.Graphing
│  │  ├─ Barbatos.Pallas.Engine              └─ Barbatos.Pallas.DependencyInjection
│  └─ app/
│     ├─ Barbatos.Pallas.Presentation        net8.0;net9.0;net10.0 · no UI framework
│     ├─ Barbatos.Pallas.Rendering.Skia      net8.0;net9.0;net10.0 · SkiaSharp
│     ├─ Barbatos.Pallas.Wpf                 net10.0-windows10.0.17763.0 · WinExe
│     └─ Barbatos.Pallas.Maui                (Phase 8)
└─ tests/
   ├─ Barbatos.Pallas.Architecture.Tests     dependency graph + floating-point scan
   ├─ Barbatos.Pallas.Conformance.Tests      worked examples of the manual, as data
   └─ Barbatos.Pallas.<Package>.Tests        one per package, added with the package's phase
```

## 2. Dependency graph

```
 src/app ─────────────────────────────────────────────────────────────────────────
   Barbatos.Pallas.Wpf  (Barbatos.Wpf.Core · Aquarius · AquariusRouter · i18n.Wpf)
   Barbatos.Pallas.Maui (Phase 8)
          │
          ▼
   Barbatos.Pallas.Rendering.Skia ──► Barbatos.Pallas.Presentation (CommunityToolkit.Mvvm)
                                                 │
 src/core ───────────────────────────────────────┼────────────────────────────────
                    Barbatos.Pallas.DependencyInjection  (references all below)
                                                 ▼
                   Spreadsheet        Graphing (→ Solvers)        Data
                       └─────────────────────┬─────────────────────┘
                                             ▼
                                          Engine ────────────────────────────────┐
            ┌──────────────────┬─────────────┼─────────────┐                     │
            ▼                  ▼             ▼             ▼                     │
       Expressions       LinearAlgebra   Statistics     Solvers                   │
    (no dependencies)          └──────────────┴─────────┐ (no dependencies)       │
                                                        ▼                         │
                                                    Numerics ◄────────────────────┘
                                                (no dependencies)
```

`ArchitectureMap` in `Barbatos.Pallas.Architecture.Tests` holds the same graph as data. The tests fail when:

- a `.csproj` references anything other than what the map declares;
- the map contains a cycle;
- a compiled assembly references a Pallas assembly outside its transitive dependencies, or a UI/GDI framework
  assembly.

Change the map and this diagram together.

## 3. Package responsibilities

| Package | Contents |
|---|---|
| **Numerics** | Only the calculator math .NET does not provide (PRECISION.md §3): `Trigonometry` (sin, cos, tan and inverses in degrees, radians and gradians, on `double.SinPi` and `System.Math`); `IntegerFunctions` (factorial, nPr, nCr, LCM, prime factors on `BigInteger`); `Fractions` (display-time fraction recognition); `Sexagesimal`; `AngleUnit`; `ErrorFunction` (erf, erfc and the inverse of erfc, for the normal distribution); `PoissonDistribution` (Loader's saddle-point form) |
| **LinearAlgebra** | Exact linear algebra of `decimal` matrices: determinants, inverses and solutions of linear systems by fraction-free elimination on `BigInteger` (`ExactLinearAlgebra`), no `double`. The matrix and vector values and their operations are in Engine (`MatrixValue`, `VectorValue`), where every entry follows the precision rule |
| **Statistics** | Exact statistics of `decimal` data scaled to integers, no `double`: sums, means, variances, the linear and quadratic least-squares fits and r² (`ExactSample`), and the quartile ranks (`Quartiles`, assumption U1). The Statistics application - its data, the seven regressions, x̂/ŷ, ▶t and P( Q( R( - is in Engine (`StatisticsData`, `RegressionModel`) |
| **Expressions** | `ExpressionLexer` (allocation-free, context-aware: Base-N digits, Spreadsheet cells); `ExpressionParser` (Pratt, the calculator's priority levels, implicit multiplication, Verify chains, first error with its span); immutable syntax tree with `SyntaxEquivalence`; `SyntaxVocabulary` (the reference calculator names, extensible by plugins); `LinearPrinter` (Canonical Linear Syntax that parses back) and `LatexPrinter`. No dependencies, no `double` |
| **Engine** | Binder; function/constant/unit registry (plugin API); RPN bytecode compiler and evaluator; `Value` model; settings and profiles; calculator memory (Ans, PreAns, A-F, x, y, z, MatA-D, VctA-D, f and g); CALC; formatter (the FORMAT menu); automatic differentiation, ∫, Σ, Π; ÷R; Verify; Base-N; session snapshot contracts |
| **Solvers** | Polynomial algebra, with no dependencies: exact integer polynomials - the sign at a rational point, the number of distinct real roots by Sturm, the square-free part and division by a rational root (`IntegerPolynomial`) - and every complex root by the method of Aberth, with a rational root recovered from its continued fraction and verified exactly (`PolynomialRoots`). The Equation, Inequality and Ratio applications are in Engine (`CalculatorSession.SolveSimultaneous`, `SolvePolynomial`, `SolveEquation`, `SolveInequality`, `SolveRatio`) |
| **Spreadsheet** | The Spreadsheet application (`SpreadsheetGrid`, `SpreadsheetCell`): constants fixed when entered and formulas calculated again, relative and absolute references, the ranges of `Min(`, `Max(`, `Mean(` and `Sum(`, copy, cut and fill, Auto Calc, the byte capacity of the sheet, and a Circular ERROR for a cell that reads itself. The Table application (`NumberTable`): f(x) and g(x) over a range of x, its rows editable and checkable with Verify. Both calculate through a session; the engine only reads cells (`CalculatorSession.CellValues`) |
| **Data** | CODATA constant sets; NIST SP 811 unit conversions with exact factors; CIAAW standard atomic weights (embedded resources) |
| **Graphing** | Viewport; adaptive sampling; asymptote and discontinuity detection; roots, extrema and intersections |
| **DependencyInjection** | `AddPallas()`, validated options, module registration |
| **Presentation** | View models; keypad model; structural math-input editor; calculator-app registry; history |
| **Rendering.Skia** | Math layout engine (box model, OpenType MATH font); math and graph renderers shared by WPF and MAUI |
| **Wpf** | The desktop host on Barbatos.Wpf.Core |

## 4. Clean Architecture rules

| Ring | Packages | Rule |
|---|---|---|
| Domain | Numerics, LinearAlgebra, Statistics, Solvers | Pure mathematics: no I/O, no state, no knowledge of calculators |
| Application | Expressions, Engine, Spreadsheet, Graphing | Calculator semantics; depend inward only |
| Infrastructure | Data, DependencyInjection, session persistence | Datasets, composition, storage adapters |
| Presentation | Presentation, Rendering.Skia, Wpf, Maui | Never referenced by `src/core` |

Rules that cut across all rings:

- **The core is language-neutral.** Errors are a `CalcErrorKind` plus arguments and a source span; English and
  Vietnamese text is produced in the app through Barbatos.i18n.
- **No reflection-based discovery.** iOS requires full AOT, so plugins are registered explicitly and JSON uses
  source generators.
- **No `InternalsVisibleTo`** (same rule as Barbatos.Wpf). What a test needs to reach is public API or is tested
  through public API.

## 5. The engine pipeline

```
On-screen keypad / hardware keys ─► InputCommandRouter ─► MathInput (template tree + cursor)
                                                               │ serialize
Web API / tests / history ─────────────────────────► Canonical Linear Syntax (docs/LINEAR-SYNTAX.md)
                                                               ▼
Lexer      ref struct over ReadOnlySpan<char>; tokens are (Kind, Span, Symbol); no allocation (measured)
Parser     Pratt; the calculator's priority (13 levels + Verify); implicit multiplication; 128-level depth; first error + span
AST        immutable; numbers kept as text ─► linear/LaTeX printers · automatic differentiation · exact-form analysis
Binder     frozen function table; arity and type checks; constants, variables, f(x), g(x)
Lowering   RPN bytecode (array of structs; no recursion)
Evaluator  explicit stack · CancellationToken · EngineBudget ─► Value | CalcError(kind, span)
Formatter  profile + settings + FORMAT target ─► FormattedResult (display tree, text, LaTeX)
```

**Why a Pratt parser and an AST *and* RPN bytecode, rather than Shunting-yard alone.**

- **The calculator's grammar mixes every operator shape.** It has postfix operators (`² ! % °′″`, engineering symbols,
  `x̂`), prefix and infix operators, multi-argument functions, Verify's relational chains, and implicit
  multiplication that binds tighter than `÷`. Binding powers express each of these directly; Shunting-yard needs a
  special case for each.
- **Several features need the tree:**
  - textbook display;
  - LaTeX export;
  - automatic differentiation;
  - exact-form recognition;
  - error positions.
- **Bytecode (lowered from the tree) does the repeated evaluation** needed by tables, graphs, Σ, integration nodes,
  Solver iterations and spreadsheets. It also removes recursion from evaluation.

## 6. Extending the engine: functions, constants, units

The contract, as built in Phase 3.

```csharp
public interface IMathFunction
{
    FunctionSignature Signature { get; }   // name as written ("beam("), arity, applications
    EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context);
}

PallasEngine engine = PallasEngineBuilder.CreateDefault()   // without DI: console, Web API, tests
    .AddConstantSet(ConstantSets.Codata2022)                // Barbatos.Pallas.Data
    .AddUnitSet(UnitSets.NistSp811)
    .AddAtomicWeights(AtomicWeightTables.Ciaaw)
    .AddFunction(new BeamDeflection())
    .Build();

services.AddPallas(options =>                               // Barbatos.Pallas.DependencyInjection
        {
            options.Profile = CalculatorProfile.Extended;
            options.Budget = new EngineBudget(MaxIterations: 10_000_000, Timeout: TimeSpan.FromSeconds(5));
        })
        .AddFunction<BeamDeflection>();                     // the data sets are included unless turned off
```

- **Frozen registry.** Functions, constants and unit conversions are built into `FrozenDictionary` tables when the
  engine is built, so the engine is immutable and safe to share; sessions hold the state.
- **No data of its own.** The engine has the calculator's functions but no scientific constants, unit conversions or
  atomic weights until a data set is added: reference data versions separately from the engine (decision of
  18 Sep 2026).
- **Values and errors.** A function receives `Value` arguments, which carry their own type and exactness, and answers
  with a value or a `CalcErrorKind`; the engine checks the arity before the call and the profile's range after it.
- **Names.** A signature's name joins the vocabulary of `Barbatos.Pallas.Expressions`, so a plugin function is parsed,
  printed and bound exactly like a built-in one.

## 7. Growing without breaking the core

- **History and sessions.** A `CalculationRecord` stores:
  - the input as Canonical Linear Syntax;
  - the result as round-trip text (`decimal` or `double` "R" format);
  - settings, profile, engine version and `SchemaVersion`.

  It stores no binary numbers and no internal objects, so any later version can re-evaluate it. `ISessionStore` is
  an Engine abstraction; JSON with source generation is one implementation.
- **Graphs.** Graphing consumes only the public `CompiledExpression` API, so graph features need no Engine change.
- **Public API discipline** (Phase 7 gate, before the first nuget.org release):
  - `Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks the public surface;
  - `EnablePackageValidation` compares against the last published version;
  - a renamed member stays as `[Obsolete]`;
  - 0.x prerelease versions until the API is frozen.

## 8. Presentation: MVVM, keypad, math input, graphs

- **Keypad as data.**
  - `KeyDefinition { Id, Primary, Shift, Label (i18n key), Glyph }`, loaded from JSON layouts (standard and
    extended).
  - Every key binds one `IRelayCommand<KeyId>`, routed by `InputCommandRouter`.
  - Hardware keys map into the *same* table, so the on-screen keypad and the keyboard cannot behave differently.
- **MathInput.**
  - A template tree (fraction, mixed fraction, root, power, abs, logₐb, ∫, Σ, Π, d/dx) with a cursor path,
    undo/redo, and insert/overwrite modes.
  - Serializes to Canonical Linear Syntax: one road into the parser.
- **Applications.** `ICalculatorApp` has thirteen implementations. The home screen lists the registry, so adding an
  application touches no other.
- **Shared memory.** A-F, x, y, z, MatA-D, VctA-D, f and g live in the Engine's observable `CalculatorSession`: on
  the reference calculator they persist across applications.
- **Rendering.**
  - One `MathLayoutEngine` (box model over an OpenType MATH font such as STIX Two Math) drawn with SkiaSharp by
    `SKElement` in WPF and `SKCanvasView` in MAUI.
  - LaTeX is for copy and export only.
  - WpfMath 2.1.0 is the WPF-only fallback. CSharpMath is not chosen: stable 0.5.1, 1.0 still prerelease as of Sep
    2026.
  - The choice is confirmed by a spike in Phase 5.
- **Graphs.**
  - Core sampling and asymptote detection, with `double` screen coordinates (an allow-list entry for Graphing, added
    in Phase 6).
  - Trace, root, min/max and intersection values are always recomputed by the Engine.
- **WPF host.**
  - Barbatos.Wpf.Core: `WpfApp`, DI, Preferences for settings, SingleInstance.
  - AquariusRouter: home → thirteen applications, settings, CATALOG as a modal route.
  - Barbatos.i18n.Wpf for English and Tiếng Việt.
  - Installer through Barbatos.PackagingEngine.

## 9. Performance

1. **Allocation-free lexing** (measured by `LexerTests` on all three runtimes).
   - Vocabulary symbols are grouped by first character and matched with `ReadOnlySpan<char>.StartsWith`, so no
     lookup key is allocated and no `GetAlternateLookup` (.NET 9+) is needed.
   - Number literals stay text in the tree; the engine parses them with
     `decimal.TryParse(ReadOnlySpan<char>, NumberStyles.Float, CultureInfo.InvariantCulture)`.
2. **Value types, no allocation per operation.** `decimal` (16 bytes) and `double` are structs with hardware or
   runtime-optimized arithmetic. `BigInteger` appears only in integer functions such as `69!`.
3. **Compile once, evaluate many.** RPN bytecode for Table, Graph, Σ/Π, integration nodes, Solver iterations and
   Spreadsheet.
4. **Task-level parallelism.** Table rows, graph samples, spreadsheet dependency levels, large matrix products and
   large data sets. Values are immutable, so no locks; results merge in a fixed order, preserving determinism.
5. **SIMD where the values are `double`.** `Vector<double>` can sample graphs in batches (Graphing). `decimal` has no
   SIMD support, so `decimal` arithmetic stays scalar.
6. **Constants come from .NET.** `double.Pi`, `double.E` and `double.Tau` are compile-time constants; nothing is
   computed or cached at run time.
7. **Measured, not assumed.** BenchmarkDotNet with committed baselines and a CI regression gate (Phase 7). Initial
   targets, to be adjusted against measurements:
   - typical keypad expression: p99 below 1 ms;
   - 45-row, two-function table: below 20 ms;
   - 2,000 graph samples: below 16 ms per frame.

## 10. Build and packaging conventions

- **Solution and packages.** `.slnx`. Central Package Management: versions live only in `Directory.Packages.props`.
  `nuget.config` restores from nuget.org only.
- **Props chain.**
  - Root `Directory.Build.props` → `src/core`, `src/app` and `tests`, each importing the root with
    `GetPathOfFileAbove`.
  - Target frameworks are set per tier, never in a core `.csproj` (Architecture.Tests enforces it). A singular
    `<TargetFramework>` does not override an inherited plural one.
  - `Directory.Build.targets` must exist, and `.editorconfig` has `root = true`.
- **Compiler settings.**
  - `LangVersion 14.0`, nullable, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, `AnalysisMode Recommended`.
  - Culture analyzers (CA1304/1305/1310) are errors: a decimal comma is a wrong result.
  - Runtime-dependent APIs (`System.Threading.Lock`, `GetAlternateLookup`) are guarded with
    `#if NET9_0_OR_GREATER`.
- **Code style.**
  - English code, comments and docs; conversation with the maintainer is in Vietnamese.
  - Explicit types instead of `var`.
  - MIT file header (IDE0073 is an error).
  - XML docs on every public member.
- **Packages.**
  - `Barbatos.Pallas.*`, version `0.1.0-preview.1`, MIT license expression, per-package README, icon.
  - snupkg symbols.
  - SourceLink in CI/release builds only.
  - Strong naming when the release workflow writes `src/barbatos.snk` from the `STRONG_NAME_KEY` secret.
- **Tests.**
  - xunit.v3 on Microsoft.Testing.Platform (`global.json`); AwesomeAssertions.
  - Run on net8.0, net9.0 and net10.0.

## 11. Roadmap

| Phase | Scope | Exit criteria |
|---|---|---|
| **0 Foundation** ✅ | Repository, build, analyzers, both floating-point locks, project skeletons, conformance data (164 cases) and domain table, docs, CI | Build and tests green on three TFMs |
| **1 Numerics** ✅ | The calculator math .NET lacks: trigonometry in angle units, integer functions, fraction recognition, sexagesimal. Everything .NET has (`System.Math`, `System.Double`, `System.Decimal`, `BigInteger`, `System.Numerics.Complex`, `System.Random`) is used directly by the engine instead | Every branch covered or the exception documented; accuracy tests against PeterO.Numbers on Windows; mutation score ≥ 90% |
| **2 Expressions** ✅ | Lexer, Pratt parser, syntax tree, diagnostics with spans, vocabulary, linear and LaTeX printers; Canonical Linear Syntax final (docs/LINEAR-SYNTAX.md) | Every conformance input parses in its application; print-and-reparse property in 7 application contexts; fuzzing never throws or hangs; lexing allocates nothing; every branch covered but one documented; mutation score ≥ 90% |
| **3 Engine** ✅ | Binder, plugins, RPN evaluator, exact display forms, formatter, memory and sessions, calculus, Verify, Base-N, Complex, the CODATA/NIST/CIAAW data sets and `AddPallas()` | The 111 Calculate, Complex and Base-N conformance cases pass; evaluation of any generated tree ends in a value or a named error within its budget; integrals agree with 50-digit references; mutation score ≥ 90% |
| **4 Domain apps** ✅ (M1 Matrix and Vector, M2 Statistics, M3 Distribution, M4 Equation, Inequality and Ratio, M5 Spreadsheet and Table, M6 hardening) | Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio, Spreadsheet, Table | Met: every conformance case of those applications passes (only the four Math Box cases are skipped, for Phase 6); the normal distribution, the Poisson probability and the polynomial roots agree with 50-digit PeterO.Numbers references; every package with code is above the 90% mutation gate |
| 5 WPF | Presentation, keypad, MathInput, rendering, thirteen applications, settings, i18n, history | Every manual workflow runs in the app |
| 6 Graph and Math Box | Graphing; Dice, Coin, Number Line, Circle | Math Box conformance cases pass |
| 7 Hardening | Benchmarks and gates, API docs, public API tracking, publish pipeline, installer | 0.x preview on nuget.org |
| 8 MAUI | Android and iOS | Same conformance results on device |

## 12. Decision log

| Date | Decision |
|---|---|
| 17 Sep 2026 | Plan approved as proposed. |
| 17 Sep 2026 | Constants: the newest CODATA set by default (also the newest atomic-weight data). |
| 17 Sep 2026 | Display rounding defaults to half up (`MidpointRounding.AwayFromZero`); to-even selectable. Make full use of .NET numeric APIs (System.Numerics, `System.Math`). The "floating point only as hints" part was superseded the same day (below). |
| 17 Sep 2026 | Unit conversions use exact definitions where they exist. |
| 17 Sep 2026 | No physical reference calculator available: behaviors the manual leaves unspecified are recorded as assumptions (CONFORMANCE.md §6). |
| 17 Sep 2026 | Core packages published under MIT on nuget.org. |
| 17 Sep 2026 | Repository local for now; remote will be `Barbatos-Labs/Barbatos.Pallas`. |
| 17 Sep 2026 | Mutation-score gate for Numerics: ≥ 90%. Enforced in CI with Stryker.NET (`stryker-config.json`, break at 90); string mutations of exception messages are ignored. |
| 17 Sep 2026 | Tests run on Microsoft.Testing.Platform: xunit.v3 4.x refuses VSTest on the .NET 10 SDK. |
| 17 Sep 2026 | **Built-in numeric types instead of a custom number tower.** The first Phase 1 implementation (`BigRational`, `BigDecimal`, `RoundingMode`, `MathContext`, a number parser, planned certified reals) was deleted. The maintainer's direction: the reference calculator is a functional reference, not an implementation model, and .NET already provides the types and rounding. Now: `decimal` for arithmetic, `Math.Round` with `MidpointRounding`, `double` with `System.Math` for transcendental functions, `BigInteger` for integer functions, `int` for Base-N, `System.Numerics.Complex` for complex numbers; fractions, surds and π forms recognized at display time. Accepted cost: about 15 significant digits for transcendental results instead of certified digits, and no arbitrary precision. `double` and `Complex` are confined to Numerics (amended below); `float`, `Half` and `MathF` are banned (PRECISION.md §7). |
| 17 Sep 2026 | **No wrappers around the BCL.** The maintainer's review of Numerics: remove every piece of code that `Math`, `MathF` or `System.Double` already provide. Deleted `MathConstants` (use `double.Pi`, `double.E`), `DecimalArithmetic` (use `decimal` operators, `decimal.TryParse`, `(decimal)value`, `Math.Round` and format strings; the 10⁻¹⁴ precision rule moves to the engine) and `ScientificMath` (the engine calls `System.Math` directly and turns NaN or ∞ into Math ERROR in one place). The special-angle table became `double.SinPi`, `double.CosPi` and `double.TanPi`. Complex and random numbers use `System.Numerics.Complex` and `System.Random` in the engine rather than Phase 1 code. `double` is therefore allowed per assembly: Numerics now, Engine from Phase 3. |
| 17 Sep 2026 | Phase 2 plan approved as proposed. Text cannot tell some calculator keys apart, so: `^` is left-associative (level 3, manual p. 168); `C` between two operands is the combination operator, elsewhere the variable C; `°` followed by minutes or seconds is sexagesimal, alone the degree unit; scientific constants take `@`; in a Verify chain inequalities point one way and `≠` does not combine with them (U10); names live in a vocabulary the engine can extend; parsing stops at the first error; nesting deeper than 128 is a Stack ERROR (U11). Details in docs/LINEAR-SYNTAX.md §5. |
| 17 Sep 2026 | Expressions has no dependencies: the parser keeps numbers as text and uses nothing from Numerics. |
| 17 Sep 2026 | Phase 1 closes without a Linux run of the accuracy tests: the maintainer does not require one. The CI workflow keeps its Linux x64 and ARM64 test job, which runs once the repository has a remote. |
| 17 Sep 2026 | Mutation-score gate extended to Expressions (≥ 90%, enforced in CI). `StandardVocabulary.cs` is excluded from mutation: it only builds the static `SyntaxVocabulary.Standard`, and the MTP runner cannot switch mutants in static initialization, so all 98 of them were reported as survivors although deleting a line by hand fails 13 to 18 tests. First score with the exclusion: 100% (738 killed, 108 timeouts, no survivors); a timeout is a failing CsCheck property still shrinking. |
| 18 Sep 2026 | Phase 3 plan approved as proposed. (1) MathO forms: values computed exactly carry an exact form (rational multiples of 1, square roots of integers and π, with `decimal` coefficients) through + − × ÷, integer powers and √ of rationals, stored with Ans and variables; results of other functions are recognized from their value at display time, single-term forms only. Arithmetic stays `decimal`/`double`. (2) Two exact values compare exactly; otherwise values are equal within 10⁻¹³ relative, for Verify and for form recognition (measured: `(decimal)double` is off by up to 5.07×10⁻¹⁵ relative in 2 million samples). (3) Complex integer powers by repeated multiplication and `r∠θ` through `Trigonometry`, because `Complex.Pow(i, 2)` is −1 + 1.2×10⁻¹⁶i. (4) ∫ by adaptive Gauss–Kronrod 7/15 on `double`, refined to 10⁻¹⁴ relative and answered while the estimate is within 10⁻¹⁰ (or `tol`), Time Out otherwise; d/dx by differentiating the syntax tree, `tol` validated but unused. (5) Data: CODATA 2022, NIST SP 811 definitions, CIAAW atomic weights, stored as `decimal` mantissa and exponent so Data needs no `double`; the engine has no data sets unless added, `AddPallas()` adds all three. (6) Working assumptions U12–U17 (CONFORMANCE.md §6). (7) Public API: `PallasEngineBuilder`, `PallasEngine`, `CalculatorSession`, `Calculation`, `Value`, `IMathFunction`. (8) Milestones M1–M6, each reported to the maintainer. |
| 18 Sep 2026 | The engine has no reference data of its own: scientific constants, unit conversions and atomic weights come from data sets (`Barbatos.Pallas.Data`), which `AddPallas()` registers by default. A data set holds published values as a `decimal` mantissa and a power of ten, so the data package needs no `double`. |
| 18 Sep 2026 | Base-N offers only what the manual leaves it: numbers, base prefixes, the four operations, the logic operators, `Not(`, `Neg(`, parentheses and the memories. The CATALOG commands of pp. 51-69 are unavailable there (p. 51), which the Phase 2 vocabulary had allowed. Assumption U12. |
| 18 Sep 2026 | The `Standard` profile refuses a result that would already display as 1×10^100: the calculator's range ends at 9.999999999×10⁹⁹ (p. 169), so rounding for display must not carry a value past it. Found by the property test that reparses every displayed result. |
| 18 Sep 2026 | An operand with an exact form is taken as `double` from that form rather than from its 15-digit `decimal`: √2 is 1.4142135623731 as a decimal, 3.5×10⁻¹⁵ away from the truth, so `Rec(√(2),45)` gave x = 1.0000000000000042. e carries a form for the same reason, though it is not a display form. |
| 18 Sep 2026 | An exact base is multiplied out only while the product stays a `decimal`; beyond it, `Math.Pow`. Squaring out 10⁹⁹ in `double` drifted a unit below it, so 10⁻⁹⁹ (and 2×10⁻⁹⁹) became 0, and 10⁻¹²⁸ was a Math ERROR through the square 10¹²⁸ instead of 0. Found by mutation testing. |
| 18 Sep 2026 | Both ends of the `Standard` range are taken at ten significant digits: a result that displays as 1×10⁻⁹⁹ is in range (assumption U18), as one that displays as 1×10^100 is not. |
| 18 Sep 2026 | Base-N subtracts in 32 bits directly: `FFFFFFFF−80000000` is 7FFFFFFF, where subtracting as `left + (−right)` failed on −(−2³¹). Verify is refused outside Calculate, Table, Equation and Complex (p. 73); in Base-N with Verify on, a relation used to be verified. |
| 18 Sep 2026 | `AddPallas()` may be called more than once and returns the same builder: a library and its host can both call it. Functions added through a second call used to be dropped silently. |
| 18 Sep 2026 | Mutation-score gate extended to Engine and DependencyInjection (≥ 90%, enforced in CI). Data is not mutated: it is static initialization, which the MTP runner cannot switch, and its tests check every value against its defining relation instead. Code whose mutants could not change a result was simplified rather than kept: an unreachable Base-N comparison, a tolerance comparison in `decimal` that `double` answers to 10⁻¹⁶, and guards that `System.Math` already applies. |
| 18 Sep 2026 | ∫ answers while its error estimate is within 10⁻⁹ relative, or a looser `tol` (was 10⁻¹⁰, or `tol` itself). It still refines to 10⁻¹⁴. A `tol` down to 10⁻²² is accepted as on the calculator, but one tighter than 10⁻⁹ no longer ends in Time Out when `double` cannot reach it: `∫(x,0,1,1×10^-21)` is 1⌟2. The maintainer: 10⁻⁹ is plenty. |
| 18 Sep 2026 | **No brand or model name of the reference calculator anywhere in the repository** (legal risk, maintainer): not in identifiers, file or folder names, comments, XML docs, READMEs, package metadata, docs or test data. The profile is `CalculatorProfile.Standard`, the vocabulary `SyntaxVocabulary.Standard` (built by `StandardVocabulary`); the conformance data live in `Data/calculator` and are run by `CalculatorConformanceTests`; the catalog is docs/CALCULATOR-CATALOG.md; conformance cases name the profile `Standard`; prose says *the reference calculator* and *the manual*, which stays local as `docs/reference-manual_VI.pdf` (gitignored as `docs/*.pdf`). |
| 18 Sep 2026 | Phase 4 plan approved as proposed. (1) `double` allowed per package where a domain needs it (Statistics, Solvers), each entry reviewed like PRECISION.md; `erf`, `erfc` and the inverse normal go to Numerics, which .NET lacks. (2) Matrices of `decimal` entries are eliminated exactly on `BigInteger`; approximate ones are singular within 10⁻¹³ of the Hadamard bound; `Value` gains matrix and vector kinds. (3) Statistics sums are `decimal`; the linear and quadratic regressions are solved exactly; r and the logarithmic, exponential and power models use `double`. (4) Binomial probabilities exactly on `BigInteger`; normal through `erfc`; Poisson in `double`, in log space for large λ. (5) Solvers exact whenever a root is rational: exact linear systems, quadratic roots as surds, rational roots of cubics and quartics split off on `BigInteger`, the rest by simultaneous iteration on `Complex` polished by Newton. (6) Matrix, Vector and Statistics are expression applications of `CalculatorSession`; Distribution a form in Engine; Equation, Inequality and Ratio forms in Solvers; Spreadsheet and Table in Spreadsheet; every form works on a session. (7) Size limits per profile. (8) Milestones M1-M6, each reported. The maintainer also allowed reading the image-only pages of the local manual to settle U1, U5 and U8, without copying them into the repository. |
| 18 Sep 2026 | M1 (Matrix and Vector). `ExactLinearAlgebra` scales each row of a `decimal` matrix to integers and eliminates without fractions (Bareiss determinant, fraction-free Gauss–Jordan inverse and solve), so every division is exact and it needs no `double`: the allow-list entry decision (1) foresaw for LinearAlgebra's vectors was not needed, because vectors are computed in the engine through `ValueMath` (|(3, 4)| is exactly 5, |(1, 1)| is √2 with its form). The engine rounds the exact quotient once (`ValueMath.FromRatio`). MatA-MatD and VctA-VctD persist across applications; a matrix or vector result goes to MatAns or VctAns, not to Ans, and leaving the application clears it. Reading p. 137 settled U8 and corrected `matrix-add-001`; operations the manual does not list and the display of entries are assumptions U20 and U21. |
| 18 Sep 2026 | The matrix and vector types of System.Numerics are not used (maintainer's question): measured on .NET 8 and 10, `Matrix3x2`, `Matrix4x4`, `Vector2`, `Vector3`, `Vector4`, `Quaternion` and `Plane` hold `float`, which is banned in `src/core` (a 4×4 determinant in `float` keeps about 7 digits), and have fixed shapes; `Vector<T>` is a SIMD register, not a math vector, and does not take `decimal`. Matrices and vectors of the calculator stay `MatrixValue` and `VectorValue` over `Value`, with `ExactLinearAlgebra` on `BigInteger`. |
| 18 Sep 2026 | M2 (Statistics). The maintainer confirmed assumption U22 (frequencies). Sums, means, variances and the linear and quadratic fits are exact: each column of `decimal` values is scaled to integers and summed on `BigInteger` (`ExactSample`), so Sxx is 0 exactly for equal x values, where the one-pass formula in `decimal` leaves 10⁻²⁷; a value held as `double` enters at its shortest round-trip digits. The `double` allow-list entry the plan foresaw for Statistics was not needed: σ, s and r are square roots of exact values in the engine, and the logarithmic, exponential, power and inverse regressions fit ln x, ln y or 1/x computed by the engine. erf, erfc and the inverse of erfc are in Numerics (`ErrorFunction`): the Maclaurin series below 1 and the continued fraction of Γ(½, x²) above, evaluated backward, because forward (Lentz) was 6.5×10⁻¹⁵ off near 1; within 2×10⁻¹⁵ of 50-digit PeterO.Numbers references. The inverse is Newton's method on ln erfc from √(−ln y), which needs no fitted coefficients. The statistic variables are names in the input, the estimates bind to the formulas of pp. 93-95, and a statistic result is displayed as a decimal (U23). Reading pp. 84 and 93-95 settled U5 and supports U1. |
| 18 Sep 2026 | M3 (Distribution). The Distribution application is a form of `CalculatorSession`: `CalculateDistribution` with a `DistributionKind` and one `DistributionParameters` record for every type, which gives a host the calculator's shared parameters (p. 98); the Variable input method writes Ans, a list does not. Binomial probabilities are exact fractions on `BigInteger`, rounded once, with the work estimated against the budget before it starts (Time Out beyond, as the calculator reports for Distribution). The Poisson probability is Loader's saddle-point form in Numerics, because exp(x·ln λ − λ − ln x!) loses 10⁻⁹ at λ = 10⁶; its error still grows with |ln P| in the far tail, 10⁻¹⁵·(1 + |ln P|), which no algorithm in `double` avoids. Normal CD subtracts the tails on their small side; Normal PD splits the exponent. `ValueMath.FromRatio` now starts at the scale the magnitude calls for, dividing once or twice instead of up to 29 times, which long binomial fractions needed. Parameter domains the manual does not state are assumption U24; distribution results display as decimals (U23). |
| 22 Sep 2026 | M4 (Equation, Inequality and Ratio), and a change to the dependency graph that deviates from plan decision 6, reported to the maintainer: **Solvers no longer references Engine; Engine references Solvers.** The plan put the three applications in Solvers, but every calculation on a `Value` - the precision rule, the exact display forms, the calculation range - is `internal` to Engine, so the forms there would have meant making the whole of `ValueMath` public, and the app layer (`Presentation`) would have needed a second package reference for three applications the session already hosts. Solvers is now a domain package with no dependencies at all (`IntegerPolynomial`, `PolynomialRoots`), like LinearAlgebra and Statistics, and the applications are forms of `CalculatorSession` (`SolveSimultaneous`, `SolvePolynomial`, `SolveEquation`, `SolveInequality`, `SolveRatio`), as M1-M3 are. `ArchitectureMap`, the §2 diagram and the ring table changed together. |
| 22 Sep 2026 | M4 roots. A polynomial is scaled into integer coefficients (the roots do not move), and everything that can be exact is: a rational root is recovered from the continued fraction of an iterated root and verified with `IntegerPolynomial.SignAt`, then divided out, so a cubic or a quartic that factors over the rationals keeps the exact roots - and the display forms `-1+√(3)` and `-3⌟4+√(23)⌟4i` - of the quadratic that is left. After the rational roots, a repeated factor can only be the square of an irreducible quadratic at these degrees, which is solved exactly and counted twice. Only a cubic or a quartic with no rational root is iterated, by Aberth on `Complex`, and how many of those roots are real is decided by Sturm's theorem on the integer polynomial, not by a tolerance on the imaginary part. That needed the `Barbatos.Pallas.Solvers` allow-list entry (PRECISION.md §7), for `PolynomialRoots` only. |
| 22 Sep 2026 | M4 iteration, measured. Aberth evaluated the polynomial at the starting circle of Cauchy, which overflows `double` when the roots are large: for (x − 10¹²⁰)(x² + 1) every step was NaN and the roots never moved from where they started. The polynomial is now scaled by the geometric mean of its root magnitudes, |a₀/aₙ| to the power 1/n, with the coefficients built through their logarithms, so the roots sit around the unit circle and Horner's scheme cannot overflow. The iteration runs a fixed number of sweeps (a sweep of a root that has settled moves it by nothing), which removed the convergence bookkeeping and the mutants that came with it. |
| 22 Sep 2026 | M4 Solver. Newton's method on Left − Right, with the slope from a central difference over |x|·10⁻¹⁰ in `Value` arithmetic rather than a symbolic derivative, because the variable is a memory slot and `Differentiator` differentiates local slots. The equation is evaluated through the compiled program with the variable read from a callback, which also answers the Variable ERROR of p. 165: the iteration reports it when the program never asks for that slot. It settles when a step no longer reaches the digits the solution is held to, and reports Cannot Solve after 200 steps (assumption U25). Below 10⁻¹⁴ the steps continue in `double` under the precision rule, so a solution that is no decimal is as accurate as `double`. |
| 22 Sep 2026 | M4 plumbing. A formula such as (−b ± √(b² − 4ac))/2a is a dozen `ValueMath` calls, each able to fail; passing every error down by hand was a conditional per call that no input ever took, and mutation testing found them all alive. `ValueChain` runs the steps one after another instead - a step that failed hands on 0, and the first error is the error of the whole calculation - which is both shorter and covered. Mutation scores after M4: Solvers 96.5%, Engine 94.1%, LinearAlgebra 98.2%. |
| 22 Sep 2026 | M5 (Spreadsheet and Table), in the package the plan put them in: Spreadsheet keeps its reference to Engine, because a sheet drives the engine rather than adding to it. What the engine gained is the smallest thing that makes a formula work: a cell reference and a range bind to one instruction, and the values come back through `CalculatorSession.CellValues`, a callback of `CellAddress`. The grid, its recalculation and everything a paste does are the application's. |
| 22 Sep 2026 | M5 calculation of a sheet. A formula is calculated where it stands rather than after a topological sort: the cells it reads are calculated first, each once per pass, and a cell asked for while it is being calculated is the Circular ERROR of p. 165. It needs no graph, finds a cycle however long it is, and calculates only what the sheet asks for. A constant is calculated once when it is entered, as the calculator fixes it, and only a constant typed with 11 or more significant digits is converted, to the ten the calculator stores. |
| 22 Sep 2026 | M5 references. A paste moves the relative references of a formula by re-lexing its text: a cell reference is a token of its own in the Spreadsheet context, so nothing can hide one, and everything else in the text stays exactly as it was typed. A reference that would leave the sheet becomes `?`, which the manual shows on p. 103, and the cell is in error. |
| 22 Sep 2026 | M5 storing. `CalculatorSession.Evaluate` calculates without storing anything - no Ans, no history, no E and F of ÷R - because a sheet and a table calculate on the user's behalf rather than at their command (pp. 102, 109). `Calculate` is unchanged. |
| 22 Sep 2026 | M5 measured. The eleven Spreadsheet and Table conformance cases pass, which leaves only Math Box skipped (4 cases, Phase 6). Mutation scores after M5: Spreadsheet 95.9%, Engine 94.0%. Three pieces of the sheet were simplified because mutation testing found nothing could tell them apart: clearing the values of a sheet that has just been emptied, re-reading which cells hold formulas after a pass, and the row guards of a table, which the list itself already makes. |
| 22 Sep 2026 | M6 (hardening) closes Phase 4 against its exit criteria. The one that was still open is measured now: the iterated roots of a polynomial agree with 50-digit PeterO.Numbers references to better than 10⁻¹⁴ relative, where every reference is a closed form (√2, 2^(1/3), √(2 ± √2), the golden ratio) or 50-digit bisection, never the iteration itself. Mutation scores across the repository: Statistics 100%, Expressions 99.9%, LinearAlgebra 98.2%, DependencyInjection 96.9%, Solvers 96.5%, Numerics 96.3%, Spreadsheet 95.9%, Engine 94.1%. |
| 22 Sep 2026 | M6 robustness. Every application of Phase 4 now has a property that holds for any input: a polynomial, an inequality, a system, a distribution and a statistic each end in a value or an error the calculator has a name for, never an exception; a sheet of random constants and formulas, cycles included, ends the same way and shows the same values however often it is calculated. Cancellation was found missing between the rows of a number table - each row is a short calculation, so the budget's own check was never reached - and the token is now read between rows, as it already is inside a long one. |
| 22 Sep 2026 | M6 documentation. Every package whose README shows an example now runs that example as a test (`ReadmeTests`), which closed the four that did not: Expressions, LinearAlgebra, Data and DependencyInjection. The Data tests gained a reference to Engine for it, because the README shows the data sets in use. |
| 22 Sep 2026 | M6 leaves two things as they are, with a reason. A form of an application (Distribution, Equation, Spreadsheet) reports its error without a span, because the calculator puts the cursor on a parameter screen rather than in a line of text; a span of 0 would name a place that is not where the user is. And the Extended profile keeps the calculator's 4 unknowns and degree 4 for Equation, because the calculator names x, y, z and t, and the exact factoring of a polynomial is designed for those degrees. |
