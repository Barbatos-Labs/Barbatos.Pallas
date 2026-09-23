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
│     └─ Barbatos.Pallas.Wpf                 net10.0-windows10.0.17763.0 · WinExe
└─ tests/
   ├─ Barbatos.Pallas.Architecture.Tests     dependency graph + floating-point scan
   ├─ Barbatos.Pallas.Conformance.Tests      worked examples of the manual, as data
   └─ Barbatos.Pallas.<Package>.Tests        one per package, added with the package's phase
```

## 2. Dependency graph

```
 src/app ─────────────────────────────────────────────────────────────────────────
   Barbatos.Pallas.Wpf  (Barbatos.Wpf.Core · Aquarius · AquariusRouter · i18n.Wpf · WpfMath)
          │             └──────────────────────────────────► DependencyInjection: AddPallas()
          ▼
   Barbatos.Pallas.Presentation (CommunityToolkit.Mvvm) ──────► Engine · Spreadsheet
 src/core ────────────────────────────────────────────────────────────────────────
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
| **Presentation** | The shell (`CalculatorShellViewModel`: the one session, which application is open, the stored session) and the Calc Settings screen (`SettingsViewModel`); the calculator-app registry (`CalculatorApps`); the stored session as text (`SessionSnapshotJson`, `ISessionStore`); the keypad as data (`Keypad`, `KeyboardMap`, `InputCommandRouter`) and the structural math input (`MathDocument`, `MathLinearWriter`, `MathLatexWriter`, `MathInputViewModel`); the history as it lands. No UI framework, so every one of them is unit-tested and mutation-tested like the core |
| **Wpf** | The desktop host on Barbatos.Wpf.Core: the composition root (`WpfProgram`), the route table built from the registry (`AppRoutes`), the screens, the session in the preferences (`PreferencesSessionStore`) and the English and Vietnamese text (`Locales/`) |

## 4. Clean Architecture rules

| Ring | Packages | Rule |
|---|---|---|
| Domain | Numerics, LinearAlgebra, Statistics, Solvers | Pure mathematics: no I/O, no state, no knowledge of calculators |
| Application | Expressions, Engine, Spreadsheet, Graphing | Calculator semantics; depend inward only |
| Infrastructure | Data, DependencyInjection, session persistence | Datasets, composition, storage adapters |
| Presentation | Presentation, Wpf | Never referenced by `src/core` |

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
  - `KeyDefinition { Id, Glyph, Primary, Shift, ShiftGlyph, Alpha, AlphaGlyph, Group }` in `Keypad.Keys`, with
    `Keypad.RowsFor(app)` for where they sit. A C# table rather than a JSON layout (23 Sep 2026): a key types
    Canonical Linear Syntax and names a template, and the compiler reads both, where a JSON typo would only show up
    as a dead key in the running application.
  - **Each application has the keys of what it reads, and no others.** Statistics, Complex, Matrix and Vector keep
    every key of Calculate and add one row of their own under the cursor keys (n x̄ ȳ x̂ P(; i ∠ Conjg; MatA-MatD
    Det Iden; VctA-VctD • Angle). Base-N replaces the function rows with the hexadecimal digits and the logic
    operators, and has no point, exponent or S⇔D (p. 51, assumption U12); where a key means less there,
    `Keypad.Of(id, app)` says so - its brackets have no |x| or comma, and × and ÷ no nPr or nCr. `Keypad.Has`
    filters the keyboard through the same rows, so a key the screen does not show cannot be typed either.
  - `ApplicationKeypadTests` types every key of every application in every mode and asks the parser of that
    application whether it has a token for the result, with a negative control (√ in Base-N) that proves the check
    sees what it is looking for.
  - What a key does is an `InsertSymbol`, an `InsertTemplate`, an `InsertSequence` (the ×10ˣ key is three edits) or
    a `RunCommand`. Every key binds one `IRelayCommand<KeyId>`, routed by `InputCommandRouter`, the only place a key
    becomes an edit; what the line cannot do itself - Execute, the screens - it hands back as a request.
  - Hardware keys map into the *same* table (`KeyboardMap`), with the input method off on the keypad (§12, 23 Sep
    2026: the Vietnamese one took the digit row), so the on-screen keypad and the keyboard cannot behave
    differently.
- **MathInput.**
  - `MathDocument`: an immutable template tree (parentheses, fraction, mixed fraction, square root, root, power,
    abs, logₐb, ∫, Σ, Π, d/dx) with a cursor path. Every edit returns a new document, which is what makes undo a
    stack of documents rather than a log of reversible operations; nothing throws for a keystroke, and a move with
    nowhere to go returns the document itself.
  - A structure takes the operand before it: 12 and then the fraction key is twelve over something.
  - `MathLinearWriter` writes Canonical Linear Syntax - one road into the parser - and brackets a slot only where
    it has to, or a fraction inside a fraction would read as a mixed fraction.
  - `MathLatexWriter` writes what the screen draws, with the cursor in the formula and an empty box for an empty
    slot.
  - `MathDocumentReader` reads Canonical Linear Syntax back onto the line through the engine's own parser, so a
    calculation out of the history comes back as the structures it was typed with and there is no second parser to
    keep in step. What has no structure of its own comes back as its characters.
  - `MathLinearWriter.Positions` says where the cursor would be for each place in the written text, which is how an
    error finds its place: the engine reports the span it could not calculate, and `MathDocument.MoveTo` puts the
    cursor there (manual p. 162).
- **The Calculate screen.** `CalculateViewModel` calculates what is on the line, keeps what it came to, shows it
  another way through the FORMAT conversions and the S⇔D key, and walks the session's history. The up and down keys
  move the cursor; where it cannot move, they step through the history instead, which is what the calculator does
  when the cursor is already at the top of the input. An error is a `CalcErrorKind` and a span: the screen looks the
  words up under `error.<kind>` and the cursor waits where the engine stopped.
- **The menus of the display: CATALOG, FORMAT and RCL.**
  - They are drawn over the keys, on the display the calculation is on, as the calculator draws them - not in a
    window of their own: what is chosen goes onto the line being typed, so the menu belongs to that line
    (`CalculateViewModel.Menu`). Escape closes a menu, and the keys take the keyboard back when it does.
  - `CalculatorCatalog.For(vocabulary, app)` builds an application's CATALOG from the vocabulary the engine reads, so
    every CODATA constant, NIST conversion and plugin function is in it without being written down twice. What the
    catalog adds is where the manual puts a name (pp. 51-69), a table; a name in no group lands in Other, and a test
    names what may be there, so a new function is placed rather than lost. A name the line has a structure for (∫,
    √, Abs) puts the structure on the line. Base-N's CATALOG is its own names and its base prefixes.
  - STO is the next key's alpha meaning: Shift, RCL and then the key of A stores Ans in A, and in Base-N - where A is
    a digit - only x, y and z are variables. RCL lists the variables with what they hold; FORMAT, S⇔D after Shift,
    lists the conversions of pp. 42-50.
  - The editor draws a name through `LatexPrinter.Print(SyntaxSymbol)`, so a constant chosen from the CATALOG looks
    the same on the line as in the history. `Barbatos.Pallas.Wpf.Tests` draws every entry of every CATALOG.
- **The application screens.** One view model per application over the same session (`MatrixViewModel`,
  `VectorViewModel`, `EquationViewModel`, `InequalityViewModel`, `RatioViewModel`, `DistributionViewModel`,
  `TableViewModel`, `SpreadsheetViewModel`, `StatisticsViewModel`, `BaseNViewModel`), each holding only what its
  screen shows: what is typed, what the session made of it, and the localization key of the error. The mathematics
  stays in the engine - a screen calls one form of `CalculatorSession` and shows what comes back.
  - **Numbers are typed, not parsed twice.** `ValueGridViewModel` is the one grid of numbers every application uses
    - the entries of a matrix, the coefficients of an equation, the columns of a statistic, the x values of a
    distribution - and each cell calculates what was typed through the session, so `2^5` and `√(2)` are entries
    like any other and a cell that is not a number shows what was typed and names its error.
  - **A screen exists once it is opened.** The shell builds a screen the first time its application is asked for and
    keeps it, so what was typed is still there when the user comes back, and a session that never leaves Calculate
    builds nothing else.
  - **Screens of applications whose own screen is still to come** render `AppScreenView`: the application is in the
    registry, reachable and disabled where it has no engine, rather than missing from the window.
- **Applications.** `CalculatorApps.All` is the registry: one entry per application, with its route and the
  localization key of its name, in the order of the calculator's own home screen. The home screen, the route table
  and the application menu all read it, so adding an application touches no other. An application this build has no
  engine for (Math Box, until Phase 6) is listed and disabled, never hidden.
- **The shell.** `CalculatorShellViewModel` owns the one session every screen works on and opens applications
  through it; `SettingsViewModel` is the Calc Settings screen over `CalculatorSettings`. Which application the
  session is in follows the route, through a single navigation guard in the host, so a link, a shortcut and a
  restored session all reach it the same way.
- **The stored session.** `SessionSnapshotJson` writes a `SessionSnapshot` as JSON of plain strings and flags and
  reads it back; `ISessionStore` is where the host puts it (`PreferencesSessionStore` over Barbatos.Wpf's
  `IPreferences`). It is a stored format: names are fixed, enums are written by name rather than by number, and
  what a later build cannot read falls back to what a new calculator has rather than refusing to start.
  - **The history is the application's, kept beside the snapshot.** The engine's `SessionSnapshot` leaves the
    history out on purpose, so `StoredSession` is the snapshot and the lines of `SessionHistory`: what was typed and
    what the screen showed, the newest 200. Every screen of the shell shows the one history - this run's
    calculations, then the earlier run's - and leaving an application forgets it, as the engine forgets its own
    (pp. 35, 37). A line of an earlier run recalls its input only: the result is shown as it was, and it is
    calculated again when = is pressed, since only the text of what the screen showed was kept.
  - The history is an optional field of the same JSON document, so a session written before it reads back with an
    empty history and no version number changed.
- **Shared memory.** A-F, x, y, z, MatA-D, VctA-D, f and g live in the Engine's observable `CalculatorSession`: on
  the reference calculator they persist across applications.
- **The look, and the layout rule.**
  - `Theme/Tokens.xaml` holds the colours, roundings and type sizes; `Theme/Controls.xaml` holds every style. A view
    sets no colour of its own. The body is dark and the display pale, as the reference calculator's own is - which
    also puts the mathematics WpfMath draws, black ink it will not recolour, on the one light surface in the window.
  - A key is drawn as a cap, coloured by its `KeyGroup`: the group is part of the keypad table, so the host keeps no
    second list of which key is a digit. The second and third meanings are printed above the face in the
    calculator's own two colours (p. 18).
  - **A screen is a `Grid` with explicit rows, never a `DockPanel`.** A `DockPanel` gives a docked child the height
    it asks for and squeezes the rest out: at the default window size the Base-N screen lost its number-base row
    entirely and a fraction on the line was cut in half (measured 23 Sep 2026). The rows are Auto for the header
    and the controls; the content above the calculator and the calculator itself share what is left by weight
    (`1*` and `6*`, or `2*` and `5*` where the data is the point), and the calculator never takes more than
    `PanelMaxHeight`, so a tall window gives its extra room to the history or the grid.
  - **The keys share the height the keypad is given** (a `UniformGrid` of rows), and each key draws its three lines
    smaller rather than cutting one off when it is short (a `Viewbox` that only shrinks). An Auto keypad of ten rows
    - Matrix, with its grid above - lost its last row, the one with =, off the bottom of the window.
  - `Barbatos.Pallas.Wpf.Tests` builds every screen with the theme loaded (`ScreenResourceTests`), because a
    `StaticResource` naming a key no dictionary has compiles and only throws when the screen is opened.
- **Rendering: WpfMath 2.1.0** (maintainer, 22 Sep 2026), which draws LaTeX into WPF visuals.
  - The road to the screen is the tree: `LatexPrinter` prints what the editor holds, WpfMath draws it. The same tree
    prints to Canonical Linear Syntax for the engine, so what is displayed and what is calculated cannot drift.
  - WpfMath has no hit-testing of its own, so the caret is part of what is printed: the editor puts a marker at the
    cursor path and the formula is drawn again on every keystroke.
  - WpfMath draws a subset of LaTeX, and what is missing shows up only when something tries to draw it, so
    `Barbatos.Pallas.Wpf.Tests` draws every formula the printers and the math input can emit. Measured on
    23 Sep 2026: no `\operatorname`, `\mathbin`, `\quad`, `\textcolor`, `\rule` or `\phantom`; `\#` and `\$` exist
    only inside `\text`; and a script needs a base, so a square typed before anything else is drawn with an empty
    one.
  - There is no `MathLayoutEngine` of our own and no SkiaSharp: an own box model over an OpenType MATH font would
    only have paid for itself with a second host, and there is none (Phase 8 dropped).
- **Graphs.**
  - Core sampling and asymptote detection, with `double` screen coordinates (an allow-list entry for Graphing, added
    in Phase 6); the drawing itself is WPF's own, in the host.
  - Trace, root, min/max and intersection values are always recomputed by the Engine.
- **WPF host.**
  - Barbatos.Wpf.Core: `WpfApp`, DI, Preferences for the session, the window and the language, SingleInstance.
  - AquariusRouter: home → thirteen applications and the settings; the menus of the display are drawn over the keys.
  - **The window's own shortcuts** go through Barbatos.Wpf.Core's input system (`Shortcuts`): Ctrl+Z and Ctrl+Y undo
    and redo the line, Ctrl+C copies the answer as the screen shows it, Ctrl+V reads the first line of the clipboard
    onto the line through the engine's parser, Ctrl+K opens the CATALOG and Ctrl+, the settings.
    `CalculatorShellViewModel.LineOf(route)` says which line a screen has; a form has none. The input system sees a
    key before the focused element and does not stop it, so a shortcut that edits leaves the key alone while a text
    box has the focus - Ctrl+V in a cell of the sheet pastes into the cell - or while a menu is on the display.
  - **The window opens where it was closed** (`WindowPlacement`): its bounds in the normal state and whether it was
    maximized, one invariant-culture string, used only while the whole height of its title bar and 120 pixels of its
    width are on the virtual screen - a window kept on a monitor that has been unplugged opens centred instead.
  - Barbatos.i18n.Wpf for English and Tiếng Việt. **The language is the application's, not the calculator's**:
    `CalculatorShellViewModel.Language` holds the choice (Windows' own, English or Tiếng Việt), `AppLanguage` keeps
    it in the preferences and switches the localizer as it changes, and Reset leaves it alone. Each language is named
    in itself on the settings screen, so it can be found by someone who cannot read the other.
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
| 5 WPF (in progress: **M1 shell ✅**, **M2 keypad and math input ✅**, **M3 Calculate ✅**, **M4 the other screens ✅**, M4.5 the calculator's face - **a layout and look ✅**, **b a keypad per application ✅**, **c CATALOG, FORMAT, RCL and STO ✅** -, **M5 persistence and shortcuts ✅**, M6 hardening and installer) | Presentation, keypad, MathInput, rendering, thirteen applications, settings, i18n, history | Every manual workflow runs in the app |
| 6 Graph and Math Box | Graphing; Dice, Coin, Number Line, Circle | Math Box conformance cases pass |
| 7 Hardening | Benchmarks and gates, API docs, public API tracking, publish pipeline, installer | 0.x preview on nuget.org |

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
| 22 Sep 2026 | Phase 5 plan approved, with two changes by the maintainer. (3) **WpfMath 2.1.0 draws the mathematics**, not a box model of our own on SkiaSharp: the tree the editor holds prints to LaTeX for the screen and to Canonical Linear Syntax for the engine, and the caret is drawn as part of the formula, because WpfMath hit-tests nothing. (8) **Phase 8 (MAUI) is dropped**, which is what an own layout engine would have been for. Consequently `Barbatos.Pallas.Rendering.Skia` is removed, SkiaSharp with it, and the graphs of Phase 6 are drawn by WPF itself; Presentation still references no UI framework, now so that its view models can be tested without a window. The other decisions stand: view models and the math-input editor in Presentation, the keypad as data with the hardware keys in the same table, a session snapshot in Engine, English and Vietnamese through Barbatos.i18n.Wpf, twelve application screens (Math Box with its engine in Phase 6), and a Presentation test project under the same 90% mutation gate. |
| 22 Sep 2026 | **Phase 5 M1: the shell.** The application registry (`CalculatorApps`) is the only list of what the calculator does: the home screen, the route table and the session all read it, and the routes are built from it rather than written out. One navigation guard in the host switches the session to the application a route names and refuses a route this build has no engine for, so a link, a shortcut and a restored session cannot disagree. The window starts on the home screen even when a session was restored: the session says which application the memories belong to, not which screen the user asked for this time. |
| 22 Sep 2026 | **A stored session is JSON in Presentation, not in Engine.** `SessionSnapshotJson` turns the engine's `SessionSnapshot` into one string of plain strings and flags, and `ISessionStore` is where a host puts it - over `IPreferences` in the WPF app. Engine stays free of a serializer (AOT, no reflection); the format is fixed - names spelled out, enums written by name, a number where a name is expected refused - and anything a later build cannot read falls back to what a new calculator has rather than refusing to start. The serializer is source-generated for the same reason the engine takes no reflection-based shortcuts. |
| 22 Sep 2026 | **One session, resolved from the engine.** `AddPallas()` registers a session per request, which is right for a service and wrong for a calculator - the memories are one set - so the host registers it as a singleton. It is resolved from the engine and not read back off the shell that owns it: a factory that asks the container for the shell while the container is building the shell is a startup that never finishes, measured the same day. |
| 23 Sep 2026 | **Phase 5 M3: Calculate end to end.** A calculation comes back onto the line through the engine's own parser (`MathDocumentReader`), never through a second one: the history is Canonical Linear Syntax, and what it writes back says the same thing it read - measured as a property over random sequences of keys, with the one deliberate difference that reading closes a bracket the user left open, as the calculator itself does (p. 28). An error puts the cursor where the engine stopped, by mapping its span through the positions the linear writer records. The up and down keys move the cursor first and reach the history only where the cursor cannot move. A result always has mathematics to draw and an error never does (measured over the manual's own examples), so one flag governs the whole result line. |
| 23 Sep 2026 | **What the printers emit, WpfMath draws, and a test project says so.** `Barbatos.Pallas.Wpf.Tests` renders every formula `LatexPrinter` prints for the manual's own inputs, every symbol the keypad can type and every template of the math input. It found three things the printer used that WpfMath has not - `\operatorname`, `\mathbin`, and `\#` and `\$` outside `\text` - written a phase earlier and drawn nowhere until now; the printer writes `\mathrm`, `\;\mathrm{and}\;` and `\text{...}` instead. It is the only test project that needs Windows, and the only one on a single framework. |
| 23 Sep 2026 | **The keypad is a C# table, not a JSON layout** (a change from the plan in §8). A key types Canonical Linear Syntax and names a template, so a table the compiler reads catches a wrong spelling, where a JSON file would only show a dead key in the running application; the layout it needs - which keys, in which rows - is the same table. A JSON layout comes back the day a user is meant to rearrange the keypad. |
| 23 Sep 2026 | **The math input is an immutable tree, and undo is a stack of it.** An edit returns a new document, so nothing has to be reversible and no edit can half-apply; moving the cursor is not an edit, because what undo takes back is what was typed. A structure takes the operand before it, which is the only place the editor guesses, and it guesses what the calculator itself does. |
| 22 Sep 2026 | **The locale files are embedded under the assembly name, with `WithCulture=false`.** A file named for a culture (`Locales.en-US.yaml`) is otherwise taken for a satellite resource and compiled into `en-USBarbatos.Pallas.resources.dll`, where Barbatos.i18n - which reads the assembly itself - cannot find it. Measured the same day, after the application refused to start. |
| 23 Sep 2026 | **Phase 5 M4: the other screens.** Ten view models over the one session - Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio, Table, Spreadsheet and Base-N - each holding only what its screen shows: what was typed, what the session made of it, and the localization key of the error. No mathematics moved into the app layer: a screen calls one form of `CalculatorSession` and displays what comes back. A screen is built the first time its application is opened and then kept, which is what makes what was typed still be there on the way back, and a session that never leaves Calculate builds nothing else. Measured: 403 tests in Presentation, mutation score 91.23%; 9244 tests across 37 assemblies, none failing. |
| 23 Sep 2026 | **A number on a screen is an expression.** Every application that takes numbers takes them through the one `ValueGridViewModel`, whose cells calculate what was typed through the session: the entries of a matrix, the coefficients of an equation, the columns of a statistic and the x values of a distribution are all `2^5` or `√(2)` if the user types that, as they are on the calculator, and a cell that is not a number keeps the text and names its error rather than becoming a zero. One grid is also one place where a value is read, instead of ten. |
| 23 Sep 2026 | **Presentation references Spreadsheet as well as Engine** (`ArchitectureMap` and the §2 diagram changed together). The sheet and the number table are driven objects - `SpreadsheetGrid` and `NumberTable` own the cells, the recalculation and the byte capacity - and the screen owns only which cell is selected and what is being typed into it. Reaching them through Engine instead would have meant a second sheet inside the session, which is what Phase 4 M5 decided against. |
| 23 Sep 2026 | **The window is walked, not only unit-tested.** Before a milestone is reported the real application is started with a listener on WPF's binding trace and a walk that opens every route, types into every box, moves every choice and presses every button that is not a link. It found four things neither the compiler nor a view-model test saw: a distribution of the normal family calculated through the list form, which the engine refuses; the Poisson parameter passed as the mean rather than λ; the frequency column of a one-variable statistic passed as y; and a missing list of kinds on the Equation screen, which is a binding error and not a crash. What it found is now in the Presentation tests; the harness itself is temporary and is deleted with the report. The last walk of M4: fifteen routes, no binding error and no exception. |
| 23 Sep 2026 | **Reading a calculation back leaves its brackets where they were.** `MathDocumentReader` unwrapped a parenthesized slot wherever it read one, so `sin(.6((7))` came back as `sin(.6(7))`; the print-and-reparse property found it after a few hundred random key sequences. Only the base of a power and the radicand of a root are unwrapped now, where the structure itself brackets what it holds. Everywhere else the brackets are the user's. |
| 23 Sep 2026 | **A root of higher multiplicity is held to fewer digits.** The property that checks the iterated roots against the roots they were built from failed now and then on a random triple root. Simultaneous iteration converges linearly at a repeated root, and a root of multiplicity m is only determined to about the m-th root of the precision of the arithmetic: a triple root comes back as three values 10⁻⁴ apart in `double`, which is the arithmetic and not the iteration. The tolerance of that property now scales with the multiplicity instead of being one number for every polynomial. |
| 23 Sep 2026 | **Phase 5 M4.5a: the window is laid out again, and the calculator gets a face.** The app had no resource dictionary at all - 905 lines of XAML over stock Windows controls - and every screen was a `DockPanel`. Both are gone: `Theme/Tokens.xaml` and `Theme/Controls.xaml` hold the colours and the styles, and every screen is a `Grid` whose rows say what gives way. The `DockPanel` was not a matter of taste: it hands a docked child the height it asks for, so the keypad took the window and at the **default** size the Base-N screen lost its number-base row and a fraction on the line was cut in half. The rows are now Auto for the header, the controls and the calculation panel, and `*` for the content, which is the part that scrolls. |
| 23 Sep 2026 | **What a key looks like belongs to the keypad table.** `KeyDefinition` gained a `KeyGroup` - digit, operator, function, modifier, control, clear, execute - and the host colours by it, rather than keeping a second list of which key is a digit. The face, the two legend colours of p. 18 and the cap that sinks when it is pressed are one style; the keypad view is the table and nothing else, which is why `MathInputView` became `KeypadView` and the display moved into `CalculationPanelView`: the calculator has one display, showing what is being typed and what it came to, not two boxes. |
| 23 Sep 2026 | **Every screen is built in a test, with the theme loaded** (`ScreenResourceTests`). A `StaticResource` that names a key no dictionary has compiles and throws only when that screen is opened, which nothing else in the repository would see: the view models are tested without a window and the LaTeX tests draw formulas, not screens. It is also the first test that needs the shell to be in the right application before a screen is built - the sheet refuses a session that is not in Spreadsheet - which is the router's guard, written down. |
| 23 Sep 2026 | **Phase 5 M4.5b: each application has the keys of what it reads.** M4 shipped one keypad for all of them, which meant Base-N offered √, sin and ∫ - each answered with a Syntax ERROR - and Matrix, Vector, Complex and Statistics had no way to type MatA, `i` or x̄ at all, from the screen or the keyboard (found by driving the application, 23 Sep 2026). `Keypad.RowsFor(app)` now gives each its own: the four that calculate expressions of their own keep every key of Calculate and add one row under the cursor keys; Base-N replaces the function rows with the six hexadecimal digits and the logic operators, and loses the point, the exponent and S⇔D. There is still one table of keys: an application chooses rows from it, and where a key means less in an application, `Keypad.Of(id, app)` says so rather than a second definition. The keyboard goes through `Keypad.Has`, so it reaches exactly the keys the screen shows. Measured: `Det(MatA)` = −2, `i×i` = −1 and `F and A` = A, typed on the application's own keys. |
| 23 Sep 2026 | **The test M4 did not have.** `ApplicationKeypadTests` types every key of every application in every mode on an empty line and asks that application's parser whether it has a token for what came out - a key of an unfinished calculation (`sin(`) is fine, a character the application cannot read at all is not - with a negative control that shows the check catching √ in Base-N. Its first run found two keys the plan had missed: in Base-N, Shift and × still typed nPr, which Base-N cannot read, and Shift and ÷ typed nCr - which Base-N reads without complaint, as the digit C, twelve, under a legend that says combinations. No parser catches that second one, so it has a test of its own and a row in docs/LINEAR-SYNTAX.md §5. The random-key properties now type in a random application on that application's keypad, instead of pressing Matrix keys on the Calculate line where they would silently do nothing. |
| 23 Sep 2026 | **S⇔D is a key.** `CalculateViewModel.ToggleDecimal` was written in M3 and reachable from nothing; it is now the `SwapForm` key of every keypad but Base-N's. The FORMAT key waits for the CATALOG milestone, because FORMAT is a menu, and so do STO and RCL, which need the engine to store into a named memory from the line. The statistic variables the five Statistics keys do not carry, the Base-N prefixes d, h, b and o, and the upright drawing of the hexadecimal digits (drawn in italics now, like variables) are left for the CATALOG milestone as well. |
| 23 Sep 2026 | **What the new keys type is drawn.** The rendering tests of `Barbatos.Pallas.Wpf.Tests` found that WpfMath cannot draw σ, Σ or ▶ as characters, so σx, Σx and ▶t on the input line failed where the same names in the history, printed by `LatexPrinter`, did not. The math input now writes those characters as the commands `LatexPrinter` writes (`\sigma`, `\Sigma`, `\blacktriangleright`), and the logic operators spaced as it spaces them, so a name looks the same on the line and in the history. |
| 23 Sep 2026 | **The keypad shares the height it is given.** A keypad of ten rows under the Matrix grid lost its last row - the one with = - off the bottom of the window, because an Auto row cannot give way. The rows of the keypad are now a `UniformGrid`, the calculator and the content above it share the screen by weight with a ceiling on the calculator, and a key too short for its three lines draws them smaller instead of cutting off the alpha legend. |
| 23 Sep 2026 | **Phase 5 M4.5c: CATALOG, FORMAT, RCL and STO - and the menus are on the display, not in a window.** The plan of M4.5 opened the CATALOG as a modal route (`RouteRecord.Window`); it is drawn over the keys instead (`CalculateViewModel.Menu`), because what is chosen goes onto the line being typed, which belongs to the screen that is open - a route of its own would have had to carry that line across the router and back. It is also what the calculator does: its menus are on its display. FORMAT is S⇔D after Shift, RCL lists the variables with what they hold, and STO is the next key's alpha meaning - Shift, RCL, then the key of A stores Ans in A (`CalculatorSession.Store`, already there). The cursor rows are six keys wide now, with CATALOG and RCL, so that ▲ stands over ▼. Measured in the application: c×2 = 599584916 from the CATALOG's c, then ENG 599.584916×10⁶ and prime factors 2²×7×73×293339; Ans→A and A in the list; 1F+A = 29 in Base-N. |
| 23 Sep 2026 | **The CATALOG is the vocabulary, placed where the manual puts it.** `CalculatorCatalog.For(vocabulary, app)` lists every symbol the application reads - every CODATA constant, every NIST conversion, every plugin function - so nothing is written down twice; what the catalog itself holds is the manual's grouping (pp. 51-69), a table, because a symbol does not know which menu the manual shows it in. A name the table does not place lands in Other, and a test says what may be there, so a function added to the vocabulary is placed rather than lost. A name the line has a structure for puts the structure there, so ∫ from the CATALOG is edited as ∫ from its key. In Base-N the CATALOG is Base-N's own names and its prefixes d, h, b and o, which the lexer reads and the vocabulary does not hold. Every entry of every application's CATALOG is typed and read by that application's parser, and drawn by WpfMath, in the tests. |
| 23 Sep 2026 | **The editor draws a name as the printer does, through the printer.** A constant chosen from the CATALOG was drawn on the line as a stray mark - @c - and nothing failed, because WpfMath draws @ without complaint; the history, printed by `LatexPrinter`, drew c. Rather than a third table of spellings in `MathLatexWriter`, Expressions gained `LatexPrinter.Print(SyntaxSymbol)`, which draws one name as a printed tree draws it, and a test that holds for every name of the vocabulary: drawn on its own, it is drawn as it is in a calculation. The editor draws every name through it. |
| 23 Sep 2026 | **Two faults older than this milestone, found by what it added.** Drawing every CATALOG entry found that `LatexPrinter` wrote °F▶°C as `\mathrm{^{\circ}F…}`, a degree sign with no base, which WpfMath refuses: a Fahrenheit conversion could not have been drawn in a result or in the history since Phase 2, because no example of the manual uses one. A degree sign inside a name now has an empty base. And the random-key property, typing on the new Statistics keys, found that `MathDocumentReader` read Σxx̂° back as Σxx̂̂)°: it cut the operand's length off the printed postfix node, and the printer brackets that operand - (Σxx̂)° - so the cut came one character early. The reader now writes the operator as the vocabulary spells it. Both have tests of their own. |
| 23 Sep 2026 | **In Base-N the letters of a number are drawn upright.** 1F and A were drawn in the italics of a variable, which Base-N does not have (assumption U12): `LatexPrinter` draws a number with letters in it as `\mathrm`, as it already drew a prefixed one, and the editor draws A to F that way when the line belongs to Base-N. |
| 23 Sep 2026 | **A comparison read back keeps the structures on either side of it.** A test written to show that `MathDocumentReader` reads relations - nothing could tell whether it did - found that it did not use them: a Verify comparison such as √(4)=2 came back from the history as characters, its square root included. The reader now reads a `RelationChain` like any other node - each side as its structures, each relation as the symbol the vocabulary spells it with. Measured after M4.5c: 37 assemblies, 9704 tests, none failing; mutation scores Presentation 92.6% (it fell to 89.1% on the tables of the CATALOG and the writer, built in static initializers that the MTP runner cannot switch, and came back once they were `switch` expressions in methods) and Expressions 99.7%. |
| 23 Sep 2026 | **Phase 5 M5: the history is the application's, kept beside the engine's snapshot.** `SessionSnapshot` leaves the history out on purpose (a calculator forgets its history when it is switched off), and the maintainer asked for it to survive a restart. So `StoredSession` is the snapshot and the lines of `SessionHistory` - what was typed and what the screen showed - and the engine is unchanged. The history is an optional field of the same JSON document: a session written by M4 reads back with an empty history, and the format kept its version number. It keeps the newest 200 lines; every screen of the shell shows the one history, and leaving an application forgets it, as the engine forgets its own (pp. 35, 37). A line of an earlier run recalls its input and shows the result as it was; = calculates it again, because only the text of the result was kept, not the value. |
| 23 Sep 2026 | **A stored format is one attribute away from being unreadable.** While the history was added, `[JsonSourceGenerationOptions]` ended up on the new `HistoryDocument` instead of the context it configures. Everything still compiled and round-tripped - and wrote PascalCase names, so every session stored by an earlier build would have read back as empty. A test now reads a session in the exact camelCase text M4 wrote, and the attribute's place is explained where it stands. |
| 23 Sep 2026 | **The window's own shortcuts go through the host's input system and give way to a text box.** Ctrl+Z, Ctrl+Y, Ctrl+C, Ctrl+V, Ctrl+K and Ctrl+, are an `InputActionMap` of Barbatos.Wpf.Core's input system (`Shortcuts`), not a key handler of our own. The input system sees a key before the element with the focus and does not stop it, so a shortcut that edits does nothing while a text box has the focus or a menu is on the display: Ctrl+V in a cell of a statistic pastes into the cell, and in the CATALOG's search into the search. Copy takes the answer as the screen shows it, after S⇔D or FORMAT; paste reads the first line of the clipboard through the engine's parser, so √(2)×3 arrives as a square root, and it is an edit that undo takes back. Measured in the application: 12+34 = 46 copied as 46; √(2)×3 pasted and calculated as 3√2; Ctrl+Z brought 12+34 back. |
| 23 Sep 2026 | **The window's place and the language are the host's, not the session's.** Where the window was, whether it was maximized and which language the application speaks are about this computer, not the calculator, so they are preferences of the host (`WindowPlacement`, `AppLanguage`) beside the session and not fields of it; Reset leaves them alone. A place is used only while the whole height of the title bar and 120 pixels of its width are on the virtual screen - a monitor that has been unplugged would otherwise open the calculator where nobody can reach it. The language is Windows' own, English or Tiếng Việt, applied at once through Barbatos.i18n's culture switch; "Windows' own" is the culture the application started in, read before a stored choice is applied. Measured: moved to 150,60 at 520×900, closed and reopened there; maximized, reopened maximized, and restored to 150,60; Tiếng Việt chosen, every label changed at once, and it was still Tiếng Việt after a restart. |
| 23 Sep 2026 | **The keypad turns the input method off.** Walking M5 on the maintainer's machine, digits typed on the keyboard never reached the line while + did: Windows' Vietnamese input method takes keys of the digit row, and WPF reports such a key as `Key.ImeProcessed`, which `KeyboardMap` has no entry for. This had been so since M2 wherever that input method is on. The keypad is not a text field, so `InputMethod.IsInputMethodEnabled` is off on it, and a key an input method still processes is read as `ImeProcessedKey`; text boxes keep the input method, where Vietnamese is typed. |
| 23 Sep 2026 | **M5 measured.** 37 assemblies, 9890 tests, none failing (the 12 skipped are Math Box's four cases on three runtimes); mutation score of Presentation 92.5%. Walked in the application: the history, the window's place and the language came back after a restart, and every shortcut did what its entry above says. |
