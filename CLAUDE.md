# Barbatos.Pallas - working agreement

A precise scientific calculation engine for .NET built on `decimal`, `double` and `BigInteger`, published as two NuGet
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
| `src/core/*` | The ten engine libraries: net8.0;net9.0;net10.0, platform-neutral; no `float`, `Half` or `MathF`. Two are packages - Engine (carrying Expressions, Numerics, LinearAlgebra, Statistics, Solvers) and DependencyInjection (carrying Data, Spreadsheet, Graphing) |
| `src/app/*` | Presentation (MVVM with no UI framework, so it is unit-tested), the WPF host (Barbatos.Wpf.Core, Aquarius, AquariusRouter, Barbatos.i18n.Wpf, WpfMath) |
| `tests/Barbatos.Pallas.Architecture.Tests` | Dependency graph (`ArchitectureMap`), which projects are packages and what each carries (`PackagingRulesTests`), the public surface of every core library tracked (`PublicApiTrackingTests`) and described in its package's API reference (`ApiReferenceTests`), and the floating-point IL/metadata scanner |
| `tests/Barbatos.Pallas.Conformance.Tests` | Worked examples of the reference calculator's manual as JSON data (`Data/calculator`) |
| `tests/Barbatos.Pallas.Numerics.Tests` | Accuracy against PeterO.Numbers 40-digit references (50-digit for erf, erfc and Poisson), CsCheck properties, manual values |
| `tests/Barbatos.Pallas.Expressions.Tests` | Precedence as tree shapes, print-and-reparse properties per application, fuzzing, lexer allocations |
| `tests/Barbatos.Pallas.Engine.Tests` | The precision rule value by value, display forms, calculus, Verify, Base-N, matrices and vectors, statistics, distributions, Math Box (the Same Result presets pinned value by value) and its properties over every throw, hour and set of bounds, compiled expressions against the line (constant folding included), plugins, integrals against PeterO.Numbers, evaluation properties |
| `tests/Barbatos.Pallas.LinearAlgebra.Tests` | Exact determinants, inverses and linear systems against the Leibniz formula and multiplication back |
| `tests/Barbatos.Pallas.Statistics.Tests` | Exact sums, variances and fits against the manual's fractions, two-pass definitions and normal equations; quartile ranks |
| `tests/Barbatos.Pallas.Solvers.Tests` | Integer polynomials against polynomials built from known roots (sign, square-free part, Sturm count, division); iterated roots against those roots |
| `tests/Barbatos.Pallas.Spreadsheet.Tests` | The sheet's constants, formulas, references, ranges, fills and capacity, a sheet whose calculation is stopped (`SpreadsheetCancellationTests`), and number tables with their row limits and Verify |
| `tests/Barbatos.Pallas.Graphing.Tests` | Curves whose shape is known: pieces clipped to the viewport, chords within half a pixel, asymptotes, jumps and domain edges; roots, extrema and intersections against their closed forms and the engine's values there; CsCheck properties over generated curves and viewports: every point inside the viewport and at the engine's value, every root and intersection a change of sign, the work bounded by the columns |
| `tests/Barbatos.Pallas.Data.Tests` | CODATA, NIST and CIAAW data against their defining relations and the vocabulary |
| `tests/Barbatos.Pallas.DependencyInjection.Tests` | `AddPallas()` through a real service provider |
| `tests/Barbatos.Pallas.Presentation.Tests` | The shell without a window: the application registry against the engine, every setting, the stored session written, read back and restored, the keypad table, the math input with its two writers and its reader, the Calculate screen, the ten application screens over one grid of values, the four tools of Math Box with the manual's examples, the graph of the Table screen fitted to its rows, zoomed and read at the pointer, worked out off the thread with a superseded result never shown (`TableGraphViewModelTests`, under `OneThread`), what a screen calculates run off the thread one work after another on the one session, AC stopping it, and the shell neither switching nor saving a session it has lent (`SessionWorkTests`, `OffThreadScreenTests`), every key of every application typed and read by that application's parser (`ApplicationKeypadTests`), every CATALOG entry of every application likewise with where the manual puts it (`CatalogTests`), STO, RCL and FORMAT, the history kept between runs, what the window's shortcuts reach, and properties over random sequences of keys in a random application |
| `tests/Barbatos.Pallas.Wpf.Tests` | The only test project that needs Windows: every LaTeX the printers and the math input emit is drawn by WpfMath - every key and every CATALOG entry of every application included -, every screen is built with the theme loaded and every Math Box tool and the graph of a table laid out and drawn, the cover over the screen while the session calculates and a value whose calculation failed shown as its error included (`ScreenResourceTests`), the session, the window's place and the language in the preferences, the crash reports and their message in both languages, and the csproj against the packaging profile (`PackagingProfileTests`) |
| `benchmarks/Barbatos.Pallas.Benchmarks` | BenchmarkDotNet, net10.0, in process: keypad expressions, the Table application at its largest, a graph of 2,000 columns and its named points, and every other application at its slowest, each with its target (`[Target]`); `--gate` fails one whose p99 is beyond its target × `Gate.Margin` |
| `build/BannedSymbols.FloatingPoint.txt` | Banned single-precision types: `float`, `Half`, `MathF` |
| `docs/ARCHITECTURE.md` | Packages, graph, pipeline, plugin API, roadmap and **decision log** |
| `docs/PRECISION.md` | The precision contract |
| `docs/CALCULATOR-CATALOG.md` | Everything the calculator does, with manual pages |
| `docs/CONFORMANCE.md` | Conformance data format, unverified behaviors and working assumptions (U1-U34), deliberate deviations (D1-D8) |
| `docs/LINEAR-SYNTAX.md` | Canonical Linear Syntax: tokens, priority levels, contexts, the text-vs-keys decisions |
| `docs/reference-manual_VI.pdf` | The reference calculator's manual, kept locally. The manufacturer's copyright: gitignored (`docs/*.pdf`), never commit or redistribute it |
| `build/Render-ManualPages.ps1` | Renders pages of the manual to PNG with Windows' own PDF API, to read pages whose content is only an image |
| `build/Test-Packages.ps1` | Installs the two packed packages into programs outside the repository and calculates with them on net8.0, net9.0 and net10.0; with `-PublicKeyToken`, also checks every assembly they hold is strong-named with it |
| `build/Move-PublicApiToShipped.ps1` | Moves every core library's `PublicAPI.Unshipped.txt` into its `PublicAPI.Shipped.txt`, as a release does |
| `docs/RELEASING.md` | How a release is published: the one-time setup (the `production` environment, `STRONG_NAME_KEY`, the nuget.org trusted publishing policy) and the steps of each release |
| `build/New-AppIcon.ps1` | Draws the app icon (`Assets/Pallas.ico`) from the mark in `build/nuget.svg` |
| `packaging/` | The installer: the barbatos-pack profile, the AppGuid ledger, the Vietnamese wizard text; in `certificates/` the key and its password are gitignored and the two public `.cer` committed (packaging/README.md) |

## Current phase

**Phase 5 - the WPF app - in progress** (roadmap in docs/ARCHITECTURE.md §11). Milestones: M1 the shell ✅,
M2 keypad and math input ✅, M3 Calculate end to end ✅, M4 the other screens ✅, M4.5 the calculator's face (a layout
and look ✅, b a keypad per application ✅, c CATALOG, FORMAT, RCL and STO ✅), M5 persistence and shortcuts ✅,
M6 hardening and the installer (in progress: the installer is built, signed and verified; the installed app is still to
be walked). Phase 4 is complete.

**Phase 7 - Hardening and 1.0.0 - in progress** (plan approved 24 Sep 2026): M1 the public API frozen and tracked ✅
(`PublicAPI.*.txt` per core library), M2 `API-REFERENCE.md` per package, hand-written and checked complete by a
test ✅, M3 BenchmarkDotNet against the targets of docs/ARCHITECTURE.md §9 with a CI gate ✅ (every benchmark six
times inside its target or more, on one thread: no parallelism or SIMD), M4 calculations off the window's thread
✅ (`SessionWork`, AC stops a calculation, the sheet takes a token; walked in the app), M5 the
release pipeline ✅ (a GitHub Release, NuGet trusted publishing, strong naming with `barbatos.snk`, the key of
Barbatos.Pallas alone - token `1c94c30b213a8345`, maintainer, 25 Sep 2026: each Barbatos library has its own), M6 the
release candidate, walked installed, and 1.0.0 - the app too - published by the maintainer (in progress: the public
API shipped, the app at 1.0.0, the remote, the secret and the nuget.org policy set up, the app released by a workflow
of its own from a tag `app-v…`; the app's three secrets, the rehearsals on GitHub and the installed walk remain).

**Phase 6 - Graph and Math Box - complete ✅** (plan approved 24 Sep 2026): M1 Math Box in Engine, M2 its screens, M3
Graphing (a library carried by the DependencyInjection package, with an `AllowList` entry for screen coordinates, on a
compiled-expression API in Engine), M4 the graph as a view of the Table application (Presentation references
Graphing), M5 hardening. Every application has its engine and every conformance case runs, none skipped.

- The app: `CalculatorShellViewModel` owns the one session; `CalculatorApps` is the registry every screen and the
  route table read; `SettingsViewModel` is Calc Settings; `SessionSnapshotJson` and `ISessionStore` keep the session
  between runs (`PreferencesSessionStore` in the host), with the history beside the engine's snapshot
  (`SessionHistory`, `StoredSession`): the engine's snapshot leaves it out on purpose. The host is `WpfProgram` +
  `AppRoutes` + `Views/`, with the text in `Locales/*.yaml`; it also keeps the window's place (`WindowPlacement`) and
  the language (`AppLanguage`, over `CalculatorShellViewModel.Language`) in the preferences, and binds the window's
  shortcuts through Barbatos.Wpf.Core's input system (`Shortcuts`, reaching a screen's line through
  `CalculatorShellViewModel.LineOf`). `CrashGuard` reports what nothing caught (`CrashReports`), saves the session and
  says so, and saves it too whenever the window loses the focus. The keypad has the input method off, or Windows'
  Vietnamese one swallows the digit row. Every application has its own view model over that session (`MatrixViewModel`,
  `VectorViewModel`, `StatisticsViewModel`, `DistributionViewModel`, `EquationViewModel`, `InequalityViewModel`,
  `RatioViewModel`, `TableViewModel`, `SpreadsheetViewModel`, `BaseNViewModel`, `MathBoxViewModel`), built the first
  time it is opened and kept; numbers are typed into the one `ValueGridViewModel`, whose cells calculate what was
  typed through the session. Math Box is a menu of four screens (`SimulationViewModel` for dice and coins,
  `NumberLineViewModel`, `CircleViewModel`), drawn by `NumberLineDrawing` and `CircleDrawing`, which place lines and
  calculate nothing the screen shows. The Table screen shows its rows or its graph (`TableGraphViewModel`: the
  session's f(x) and g(x) compiled once, sampled by Graphing at the pixels of `GraphDrawing`, the view fitted to the
  rows of the table and zoomed from there, roots, extrema and intersections named, and the value at the pointer
  calculated at the x it shows), all of it worked out off the window's thread (`GraphWork`) on sessions restored from
  a snapshot of the one the table was generated in. The look is
  `Theme/Tokens.xaml` (colours, roundings, sizes) and `Theme/Controls.xaml` (every style); `KeypadView` draws the
  key table and `CalculationPanelView` the one display, input above and answer below.
- The line the user types on is `MathDocument` (immutable, a cursor path through template slots), written as
  Canonical Linear Syntax for the engine and as LaTeX for the screen, and read back by `MathDocumentReader` through
  the engine's own parser. The keypad is the table in `Keypad`, and each application draws its own rows of it
  (`Keypad.RowsFor`): the keys of what it reads and no others - Base-N has no functions, only Matrix has MatA. The
  keyboard maps onto the same table (`KeyboardMap`) through the same filter (`Keypad.Has`), and
  `InputCommandRouter` is the only place a key becomes an edit. CATALOG, FORMAT and RCL are menus drawn over the
  keys (`CalculateViewModel.Menu`); the CATALOG is built from the engine's vocabulary (`CalculatorCatalog`), and the
  editor draws a name through `LatexPrinter.Print(SyntaxSymbol)`, as the history does. `CalculateViewModel`
  calculates, shows the result (FORMAT and S⇔D), walks the history, copies the answer and pastes a line, and puts the
  cursor where an error says it is.
- Numerics holds only what .NET lacks: `Trigonometry`, `IntegerFunctions`, `Fractions`, `Sexagesimal`, `AngleUnit`,
  `ErrorFunction` and `PoissonDistribution`.
- Expressions reads Canonical Linear Syntax into an immutable syntax tree and prints it back, as linear text or LaTeX.
  It evaluates nothing.
- Engine binds, compiles and evaluates that tree: `Value` with the precision rule, exact display forms, sessions and
  memory, the formatter and the FORMAT conversions, calculus, Verify, Complex and Base-N, the plugin API, and the Math
  Box application: `CalculatorSession.Simulate` (Dice Roll and Coin Toss, with the Same Result presets of D8),
  `NumberLine` and `CalculatorSession.CircleAngle` and `Clock`. `CalculatorSession.Compile` gives a
  `CompiledExpression` in x, read and bound once, calculated as the line would at many x (`Evaluate`, `TryEvaluate`
  for drawing at the value `ValueOf` says an x becomes, `Derivative`), with its constant calls folded once
  (`ConstantFolder`).
- Graphing samples a `CompiledExpression` across a `GraphViewport` (`GraphSampler`: pieces clipped to it, breaks told
  apart as asymptotes or jumps) and finds roots, extrema and intersections on the engine's values (`GraphAnalysis`,
  `GraphFeature`: x a `Value`, y the engine's `Calculation`). Its `double` only places points.
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

Math Box, the last application, has its engine since Phase 6 M1 and its screens since M2.

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
  (a net8.0-only compile error). A full run is 13 test projects × 3 frameworks + Wpf.Tests, which is Windows-only,
  = 40 assemblies.
- **Packing.** `dotnet pack Barbatos.Pallas.slnx -c Release -o artifacts/packages` packs the two published packages,
  Barbatos.Pallas.Engine and Barbatos.Pallas.DependencyInjection; the other eight libraries travel inside
  them (docs/ARCHITECTURE.md §10). Then `./build/Test-Packages.ps1 -PackageDirectory artifacts/packages` installs them
  into programs outside the repository and calculates on net8.0, net9.0 and net10.0, which is the only check of what a
  user gets: a package that leaves out an assembly packs, builds and tests cleanly. Pack into an empty folder - the
  script refuses any package but the two. Never pack with `--no-build`: a package finds what it carries by resolving
  its references, which builds them, so `--no-build` fails with NETSDK1085, and switching that build off drops the
  carried libraries' XML documentation and symbols (25 Sep 2026).
- **Benchmarks** run in Release, in process (a project BenchmarkDotNet generated inside the repository would be built
  with its analyzers). The gate, as CI runs it after the tests:

  ```bash
  dotnet run --project benchmarks/Barbatos.Pallas.Benchmarks -c Release -- --gate
  ```

  Arguments after `--gate` select benchmarks as BenchmarkDotNet's do (`--filter *Graph*`); without `--gate` they
  run BenchmarkDotNet's own way, for a closer look. A benchmark checks in its setup that its input calculates, so a
  benchmark never measures an error. A new benchmark needs a `[Target]`, or the gate fails it; a target is the
  application's (docs/ARCHITECTURE.md §9), never the last measurement, and parallelism or SIMD is added only where a
  measurement misses one (maintainer, 24 Sep 2026).
- **CI is Windows only** (maintainer, 23 Sep 2026: the app ships on Windows alone). The packages must stay
  platform-neutral all the same: no Windows TFM in `src/core`, CA1416 is an error, and Architecture.Tests bans the
  UI and GDI assemblies. Nothing measures a `double` result on another OS (docs/PRECISION.md I5).
- **The installer** is `barbatos-pack release --profile packaging/Barbatos.Pallas.json --strict`, run from a checkout of
  Barbatos.PackagingEngine beside this one (packaging/README.md). It needs Inno Setup 6, the Windows SDK's signtool and
  the signing leaf's `.pfx` in `packaging/certificates/`, which is gitignored and must stay so (only the two public `.cer`
  are committed); in CI it is `barbatos-pallas-release-app.yml`, on a tag `app-v<version>`. The app's version is numeric
  (the engine refuses a prerelease label for an app), so a release changes `<Version>` in the Wpf csproj and
  `Identity.Version` in the profile together; `PackagingProfileTests` fails otherwise. Close the running app first:
  the publish writes over files it holds.
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
  `tests/Barbatos.Pallas.Spreadsheet.Tests`, `tests/Barbatos.Pallas.Graphing.Tests`,
  `tests/Barbatos.Pallas.DependencyInjection.Tests` or `tests/Barbatos.Pallas.Presentation.Tests`, whose
  `stryker-config.json` selects the MTP runner and ignores string mutations (exception messages are not results). Data
  has no Stryker run: it is all static initialization (below). Stryker builds the test project it runs from, so a
  `dotnet test` of that project during a run replaces the mutated assembly, and a deliberately failing scratch test
  aborts the run. Four traps:
  - a `while (true)` makes Stryker skip every mutant in the method, because `while (false)` does not compile; write
    `for (;;)`;
  - so does a pattern variable in a condition (`fa is not { } ya || fb is not { } yb`): a mutant of the condition
    leaves the variable unassigned, and Stryker puts the whole method in "Safe Mode", testing nothing there. Read the
    value out instead (`if (fa is null) … double ya = fa.Value;`). Graphing's `Interval` and `Crossings` went
    untested this way until 24 Sep 2026; look for "Safe Mode" in the log;
  - a domain check that `System.Math` would also reject survives unless the test asserts `WithParameterName`;
  - the MTP runner cannot switch mutants inside static initialization, so they are all reported as survivors.
    `StandardVocabulary.cs` is data built once for `SyntaxVocabulary.Standard` and is excluded from mutation.
    Deleting one of its lines by hand fails 13 to 18 tests (17 Sep 2026); the vocabulary tests and the conformance
    inputs are what guard it. A lookup table is written out rather than built by a loop (`DecimalDigits.PowersOfTen`),
    and a small table of strings is a `switch` in a method rather than a dictionary in a field; a filter over the
    vocabulary belongs in the method that uses it, not in a static `.Where` (Presentation fell to 89% on 23 Sep 2026
    from exactly these, and came back to 92.6% once they were switches);
  - a property test that samples thousands of cases makes Stryker's *initial* run report failures that a plain
    `dotnet test` never shows (Presentation, 23 Sep 2026: four CsCheck properties at 2000 iterations). Keep a
    property at a few hundred iterations, or the run never starts;
  - a mutant that cannot change a result (a guard `System.Math` already applies, a branch no input reaches) is a
    reason to simplify the code, not to write a test that pins an implementation detail. The Engine went from 75% to
    92.5% on 18 Sep 2026 that way and by edge tests, and three real bugs surfaced (docs/ARCHITECTURE.md §12).
- **What a screen calculates runs off the window's thread** and applies its result where it was started from. A
  session is used from one thread at a time, so there are two ways:
  - the one session is lent to one work at a time (`SessionWork`, the shell's `Work`): the line, a table, a
    distribution, a change of the sheet. While it is busy the window takes no input but AC (the cover, `BusyView`,
    and `MainWindow`'s keys), the router opens no screen and a save writes the session as it was before the work.
    A screen made without a work (tests, mostly) calculates where it is asked (`SessionWork.Immediate`);
  - a work that may be superseded gets a session of its own, restored from a snapshot (`Capture`, `Restore`): the
    Table graph, through `GraphWork`.

  A cell of a form is calculated where it is typed, on the window's thread: the command a click away reads its value.
  What a form calculates from its values is bounded by the form (M3 measured the slowest at 1.5 ms) and stays there
  too. Their tests run under `OneThread` (Presentation.Tests), which runs what a work posts back on the test's own
  thread between its steps, as the window does: without it, whether a superseded or stopped result shows up is a race.
  `ScreenResourceTests` pumps the dispatcher until a work is back (`Settle`). A test that stops a work starts one of
  seconds (`Σ(x,1,10^7)`) and cancels it before awaiting it: what the test asserts must not depend on how far the work
  got, only on what was applied.
- **SourceLink** is referenced only in CI or with `-p:SourceLinkEnabled=true`. A local repository without a remote
  would otherwise warn three times per project per framework.
- **Releasing** is the maintainer's, and the packages and the app are released apart (docs/RELEASING.md): a GitHub
  Release tagged `v<version>` runs `barbatos-pallas-cd-nuget.yml`, which signs, tests, packs and pushes the packages;
  a pushed tag `app-v<version>` runs `barbatos-pallas-release-app.yml`, which builds, signs and checks the installer
  and creates its release. Run by hand, either rehearses and publishes nothing. Barbatos.PackagingEngine is private
  and this repository public, and GitHub lets no public repository call a private one's reusable workflow: the app
  workflow installs barbatos-pack from the private feed itself (maintainer, 25 Sep 2026). The run logs of a public
  repository are public - a step never prints a secret, and only the installer is uploaded. The packages' release
  refuses a commit that has not had `./build/Move-PublicApiToShipped.ps1` run. Strong naming
  needs `src/barbatos.snk`: each workflow writes it from the `STRONG_NAME_KEY` secret, and the maintainer's machine
  keeps it there (25 Sep 2026), so every build on it - tests and installer included - is signed. It is gitignored and
  is never committed, copied, read or moved. Every assembly of the packages carries its token, `1c94c30b213a8345`
  (`Test-Packages.ps1 -PublicKeyToken`); a key made for testing elsewhere is deleted afterwards and followed by a
  `--no-incremental` build, so that nothing signed with it is left in `bin/`.

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
  `CoreFloatingPointTests.AllowList`** (Numerics, `PolynomialRoots` in Solvers, Engine, and Graphing for screen
  coordinates since Phase 6). Two locks enforce it:
  1. `BannedApiAnalyzers` (RS0030, an error) bans the single-precision types;
  2. `CoreFloatingPointTests` scans compiled IL and metadata against `AllowList`.
- **The analyzer alone is not enough.** It was measured to catch member use but *not* declarations: a `double` field,
  parameter or local and a literal `1.5` compile cleanly. Only the scanner sees those. Never weaken or skip the
  scanner; if it misbehaves, fix it and keep its self-tests (`FloatingPointScannerTests`) green.
- **A new `AllowList` entry** is reviewed like a change to PRECISION.md, as Graphing's was (PRECISION.md §7: its
  `double` places points and never shows a value of its own; every y is the engine's calculation).
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
- **A behavior the manual does not state is an assumption**, listed as U12-U34 in docs/CONFORMANCE.md, not a quiet
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
- **Every public member of `src/core` is declared** in the `PublicAPI.Unshipped.txt` beside its csproj until a release
  moves it to `PublicAPI.Shipped.txt` (`Microsoft.CodeAnalysis.PublicApiAnalyzers`; RS0016 and RS0017 are errors); the
  1.0.0 release candidate shipped all 1,177, so a new member is the first line of an Unshipped file. The
  analyzer's code fix writes the line, and so does
  `dotnet format analyzers src/core/<Library>/<Library>.csproj --diagnostics RS0016 --severity info`. A line in
  `PublicAPI.Shipped.txt` is a promise until 2.0.0: removing or changing it is an incompatible change.
- **Core `.csproj` files never set `TargetFramework(s)`;** `src/core/Directory.Build.props` owns it (tested).
- **Two packages: Engine and DependencyInjection** (maintainer, 23 Sep 2026). Every other library ships inside the
  one published package that references it, and has no `PackageId` or `PackageTags`. Making a third package, or
  moving a library from one package to the other, is the maintainer's decision; `PackagingRulesTests` fails until
  `ArchitectureMap.PublishedPackages` says so. A PackageReference in Engine would be dropped from its package without
  a word (it suppresses its dependencies), which is why that test also fails on one. Graphing ships inside
  DependencyInjection, as Data and Spreadsheet do (Phase 6 plan, 24 Sep 2026).
- **No reflection-based discovery, no `InternalsVisibleTo`.** iOS needs full AOT, and test needs are met through
  public API.
- **The core is language-neutral:** errors are `CalcErrorKind` + span, and text is localized in the app.

### Conformance data

- **A case is never marked passed before its engine exists.** Skip with the phase.
- **Derived expected values are computed exactly** (rationals, integer square roots), never with `double`.
  Transcendental values (`expectationSource: reference`) come from PeterO.Numbers series at 60 digits, cross-checked
  against a known constant (docs/CONFORMANCE.md §5), never from `System.Math` or the code under test.
- **An assumption is a `note`,** listed under U1-U34 in docs/CONFORMANCE.md. A deliberate difference from the
  calculator is a D-entry there, never a silent engine change.

### The window

- **A box an expression is typed into is a `CellBox` or a `ValueCellBox`,** whose input method is off, as the keypad's
  is. Windows' Vietnamese input method holds typed digits in a composition that WPF writes back only when it ends,
  and a click on a button - which takes no focus - does not end it: Enter and Solve took what the box held before the
  last keys (25 Sep 2026). `ScreenResourceTests` checks both styles.
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
- **A public API change updates the package's `README.md` and its `API-REFERENCE.md`.** Every example a README shows
  is run by that package's `ReadmeTests`, and `ApiReferenceTests` fails a public type or member - an overload with its
  parameters named - that the API reference of the package carrying it does not describe, or a type it describes that
  is gone. The reference is written by hand in the manner of Barbatos.i18n and Barbatos.Wpf: namespaces, then types,
  then members, each with its signature and the summary of its XML documentation.

### Identity

- **No brand or model name of the reference calculator anywhere in the repository** (maintainer, 18 Sep 2026: a
  legal risk). Not in identifiers, file or folder names, comments, XML docs, READMEs, package metadata, docs or test
  data. Write *the reference calculator*, *the manual*, or the neutral names in use: `CalculatorProfile.Standard`,
  `SyntaxVocabulary.Standard`, `Data/calculator`, docs/CALCULATOR-CATALOG.md.
- **The WPF app's `AppInfo.AppId` `{5E50D3E6-0148-428F-88C9-F824709C75C4}` and `Company` "Barbatos Labs" are
  immutable after the first release.** They name the user-data folder.

## Repository state

- `origin` is `Barbatos-Labs/Barbatos.Pallas` on GitHub (set up by the maintainer, 25 Sep 2026), where CI runs on every
  push to `main` and a published release runs the release workflow.
- Commit or push only when the maintainer asks.
