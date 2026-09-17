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
          Solvers        Spreadsheet        Graphing (→ Solvers)        Data
             └───────────────┴─────────────────┬─────────────────────────┘
                                               ▼
                                            Engine ──────────────────────────────┐
                       ┌───────────────────────┼───────────────────────┐         │
                       ▼                       ▼                       ▼         │
                  Expressions            LinearAlgebra             Statistics    │
               (no dependencies)               └───────────┬───────────┘         │
                                                           ▼                     │
                                                       Numerics ◄────────────────┘
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
| **Numerics** | Only the calculator math .NET does not provide (PRECISION.md §3): `Trigonometry` (sin, cos, tan and inverses in degrees, radians and gradians, on `double.SinPi` and `System.Math`); `IntegerFunctions` (factorial, nPr, nCr, LCM, prime factors on `BigInteger`); `Fractions` (display-time fraction recognition); `Sexagesimal`; `AngleUnit` |
| **LinearAlgebra** | Matrices and vectors of `decimal`; determinant, inverse; dot, cross, angle, unit vector; row-parallel multiplication for large sizes |
| **Statistics** | One- and two-variable summaries (eight Σ sums, mean, σ, s); quartiles; min/max; seven regressions with x̂/ŷ; P( Q( R( ▶t; binomial, normal and Poisson PD/CD; inverse normal |
| **Expressions** | `ExpressionLexer` (allocation-free, context-aware: Base-N digits, Spreadsheet cells); `ExpressionParser` (Pratt, the calculator's priority levels, implicit multiplication, Verify chains, first error with its span); immutable syntax tree with `SyntaxEquivalence`; `SyntaxVocabulary` (the reference calculator names, extensible by plugins); `LinearPrinter` (Canonical Linear Syntax that parses back) and `LatexPrinter`. No dependencies, no `double` |
| **Engine** | Binder; function/constant/unit registry (plugin API); RPN bytecode compiler and evaluator; `Value` model; settings and profiles; calculator memory (Ans, PreAns, A-F, x, y, z, MatA-D, VctA-D, f and g); CALC; formatter (the FORMAT menu); automatic differentiation, ∫, Σ, Π; ÷R; Verify; Base-N; session snapshot contracts |
| **Solvers** | Simultaneous linear equations; polynomials of degree 2-4; Newton Solver; polynomial inequalities; ratios |
| **Spreadsheet** | Cell grid; relative and absolute references; dependency graph; topological recalculation; circular-reference detection; Fill; the Table application |
| **Data** | CODATA constant sets; NIST SP 811 unit conversions with exact factors; CIAAW standard atomic weights (embedded resources) |
| **Graphing** | Viewport; adaptive sampling; asymptote and discontinuity detection; roots, extrema and intersections |
| **DependencyInjection** | `AddPallas()`, validated options, module registration |
| **Presentation** | View models; keypad model; structural math-input editor; calculator-app registry; history |
| **Rendering.Skia** | Math layout engine (box model, OpenType MATH font); math and graph renderers shared by WPF and MAUI |
| **Wpf** | The desktop host on Barbatos.Wpf.Core |

## 4. Clean Architecture rules

| Ring | Packages | Rule |
|---|---|---|
| Domain | Numerics, LinearAlgebra, Statistics | Pure mathematics: no I/O, no state, no knowledge of calculators |
| Application | Expressions, Engine, Solvers, Spreadsheet, Graphing | Calculator semantics; depend inward only |
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

The contract below is indicative and is finalized in Phase 3.

```csharp
public interface IMathFunction
{
    FunctionSignature Signature { get; }   // name, arity, parameter kinds, domain, CATALOG group, LaTeX template, i18n key
    EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context);
}

services.AddPallas(options =>
        {
            options.Profile = CalculatorProfile.Extended;
            options.Budget.Timeout = TimeSpan.FromSeconds(5);
        })
        .AddConstantSet(ConstantSets.CodataLatest)
        .AddUnitSet(UnitSets.NistSp811)
        .AddFunction<BeamDeflectionFunction>()
        .AddConstant("γ_bt", value: 25m, unit: "kN/m³");      // decimal, so a typed constant stays exact

PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();   // without DI: console, Web API, tests
```

- **Frozen registry.** It is built into a `FrozenDictionary`, so the hot path takes no locks. Names are bound
  during parsing, so evaluation never looks up a string.
- **Declared exactness.** A function declares whether it maps `decimal` to `decimal` exactly, so the formatter
  only tries fraction and surd recognition where it can be right.

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
| **1 Numerics** ✅ | The calculator math .NET lacks: trigonometry in angle units, integer functions, fraction recognition, sexagesimal. Everything .NET has (`System.Math`, `System.Double`, `System.Decimal`, `BigInteger`, `System.Numerics.Complex`, `System.Random`) is used directly by the engine instead | Every branch covered or the exception documented; accuracy tests against PeterO.Numbers on Windows and Linux (x64, ARM64); mutation score ≥ 90% |
| **2 Expressions** ✅ | Lexer, Pratt parser, syntax tree, diagnostics with spans, vocabulary, linear and LaTeX printers; Canonical Linear Syntax final (docs/LINEAR-SYNTAX.md) | Every conformance input parses in its application; print-and-reparse property in 7 application contexts; fuzzing never throws or hangs; lexing allocates nothing; every branch covered but one documented; mutation score ≥ 90% |
| 3 Engine | Binder, plugins, evaluator, formatter, memory, calculus, Verify, Base-N, Data, DI | Calculate, Complex and Base-N conformance cases pass |
| 4 Domain apps | Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio, Spreadsheet, Table | Their conformance cases pass |
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
| 17 Sep 2026 | Phase 2 plan approved as proposed. Text cannot tell some calculator keys apart, so: `^` is left-associative (level 3, manual p. 168); `C` between two operands is the combination operator, elsewhere the variable C; `°` followed by minutes or seconds is sexagesimal, alone the degree unit; scientific constants take `@`; in a Verify chain inequalities point one way and `≠` does not combine with them (U10); names live in a vocabulary the engine can extend; parsing stops at the first error; nesting deeper than 128 is a Stack ERROR (U11). Details in docs/LINEAR-SYNTAX.md §5. |
| 17 Sep 2026 | Expressions has no dependencies: the parser keeps numbers as text and uses nothing from Numerics. |
| 17 Sep 2026 | Mutation-score gate extended to Expressions (≥ 90%, enforced in CI). `StandardVocabulary.cs` is excluded from mutation: it only builds the static `SyntaxVocabulary.Standard`, and the MTP runner cannot switch mutants in static initialization, so all 98 of them were reported as survivors although deleting a line by hand fails 13 to 18 tests. First score with the exclusion: 100% (738 killed, 108 timeouts, no survivors); a timeout is a failing CsCheck property still shrinking. |
| 17 Sep 2026 | **No wrappers around the BCL.** The maintainer's review of Numerics: remove every piece of code that `Math`, `MathF` or `System.Double` already provide. Deleted `MathConstants` (use `double.Pi`, `double.E`), `DecimalArithmetic` (use `decimal` operators, `decimal.TryParse`, `(decimal)value`, `Math.Round` and format strings; the 10⁻¹⁴ precision rule moves to the engine) and `ScientificMath` (the engine calls `System.Math` directly and turns NaN or ∞ into Math ERROR in one place). The special-angle table became `double.SinPi`, `double.CosPi` and `double.TanPi`. Complex and random numbers use `System.Numerics.Complex` and `System.Random` in the engine rather than Phase 1 code. `double` is therefore allowed per assembly: Numerics now, Engine from Phase 3. |
