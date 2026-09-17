# Barbatos.Pallas - working agreement

A precise scientific calculation engine for .NET built on `decimal`, `double` and `BigInteger`, published as NuGet
packages, plus the desktop (WPF, later MAUI) calculator built on it. Its functional reference is a scientific
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
| `src/app/*` | Presentation (platform-neutral MVVM), Rendering.Skia, the WPF host |
| `tests/Barbatos.Pallas.Architecture.Tests` | Dependency graph (`ArchitectureMap`) and the floating-point IL/metadata scanner |
| `tests/Barbatos.Pallas.Conformance.Tests` | Worked examples of the reference calculator's manual as JSON data (`Data/calculator`) |
| `tests/Barbatos.Pallas.Numerics.Tests` | Accuracy against PeterO.Numbers 40-digit references, CsCheck properties, manual values |
| `tests/Barbatos.Pallas.Expressions.Tests` | Precedence as tree shapes, print-and-reparse properties per application, fuzzing, lexer allocations |
| `build/BannedSymbols.FloatingPoint.txt` | Banned single-precision types: `float`, `Half`, `MathF` |
| `docs/ARCHITECTURE.md` | Packages, graph, pipeline, plugin API, roadmap and **decision log** |
| `docs/PRECISION.md` | The precision contract |
| `docs/CALCULATOR-CATALOG.md` | Everything the calculator does, with manual pages |
| `docs/CONFORMANCE.md` | Conformance data format, unverified behaviors (U1-U11), deliberate deviations (D1-D7) |
| `docs/LINEAR-SYNTAX.md` | Canonical Linear Syntax: tokens, priority levels, contexts, the text-vs-keys decisions |
| `docs/reference-manual_VI.pdf` | The reference calculator's manual, kept locally. The manufacturer's copyright: gitignored (`docs/*.pdf`), never commit or redistribute it |

## Current phase

**Phase 2 - Expressions is complete** (roadmap in docs/ARCHITECTURE.md §11).

- Numerics holds only what .NET lacks: `Trigonometry`, `IntegerFunctions`, `Fractions`, `Sexagesimal` and
  `AngleUnit`.
- Expressions reads Canonical Linear Syntax into an immutable syntax tree and prints it back, as linear text or LaTeX.
  It evaluates nothing.

Next: **Phase 3 - Engine**. The other core packages are empty skeletons; their conformance cases are skipped with the
phase that implements them.

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
  (a net8.0-only compile error). A full run is 4 test projects × 3 frameworks = 12 assemblies.
- **Packing.** `dotnet pack Barbatos.Pallas.slnx -c Release -o artifacts/packages` packs the ten core packages.
  Packing one project does not pack its project references.
- **Mutation testing** (Stryker.NET, pinned in `dotnet-tools.json`; gate ≥ 90% per package):

  ```bash
  dotnet tool restore
  ```

  ```bash
  dotnet stryker --skip-version-check
  ```

  Run both from `tests/Barbatos.Pallas.Numerics.Tests` or `tests/Barbatos.Pallas.Expressions.Tests`, whose
  `stryker-config.json` selects the MTP runner and ignores string mutations (exception messages are not results). Three
  traps:
  - a `while (true)` makes Stryker skip every mutant in the method, because `while (false)` does not compile; write
    `for (;;)`;
  - a domain check that `System.Math` would also reject survives unless the test asserts `WithParameterName`;
  - the MTP runner cannot switch mutants inside static initialization, so they are all reported as survivors.
    `StandardVocabulary.cs` is data built once for `SyntaxVocabulary.Standard` and is excluded from mutation.
    Deleting one of its lines by hand fails 13 to 18 tests (17 Sep 2026); the vocabulary tests and the conformance
    inputs are what guard it.
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
- **An assumption is a `note`,** listed under U1-U11 in docs/CONFORMANCE.md. A deliberate difference from the
  calculator is a D-entry there, never a silent engine change.

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
- **A public API change updates the package's `README.md`** (and later `API-REFERENCE.md`).

### Identity

- **The WPF app's `AppInfo.AppId` `{5E50D3E6-0148-428F-88C9-F824709C75C4}` and `Company` "Barbatos Labs" are
  immutable after the first release.** They name the user-data folder.

## Repository state

- Local git repository, **no remote yet**. The planned remote is `Barbatos-Labs/Barbatos.Pallas`.
- Commit or push only when the maintainer asks.
