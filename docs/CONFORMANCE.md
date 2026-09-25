# Conformance to the reference calculator

`tests/Barbatos.Pallas.Conformance.Tests` checks Barbatos.Pallas against the worked examples of the reference
calculator's user's guide. The examples are data, not code, so the suite grows without touching C#.

- [1. What "conforms" means](#1-what-conforms-means)
- [2. Data files](#2-data-files)
- [3. Case format](#3-case-format)
- [4. Expectation sources and statuses](#4-expectation-sources-and-statuses)
- [5. How derived values were computed](#5-how-derived-values-were-computed)
- [6. Unverified behaviors and working assumptions](#6-unverified-behaviors-and-working-assumptions)
- [7. Deliberate deviations from the calculator](#7-deliberate-deviations-from-the-calculator)
- [8. Adding a case](#8-adding-a-case)

---

## 1. What "conforms" means

The calculator itself is accurate to ±1 at the 10th digit (manual p. 169). A Pallas result therefore conforms when
its display, in the `Standard` profile with the case's settings, equals **the mathematically correct value rounded
for that display**. Pallas is not required to reproduce the calculator's own rounding errors.

Where the calculator is known or suspected to differ from the correct value, the difference is recorded in §7
rather than copied into the engine.

## 2. Data files

`Data/calculator/`, one file per group of chapters:

| File | Pages | Cases |
|---|---|---|
| `calculate-basic.json` | 23-50 | Input rules, arithmetic, fractions, powers and roots, constants, history, Ans/PreAns, variables, CALC, number formats, FORMAT conversions |
| `calculate-advanced.json` | 51-72 | Calculus, logarithms, probability, numeric functions, angles, trigonometry, engineering symbols, units, atomic weights, f(x)/g(x) |
| `verify.json` | 73-76, 166 | Verify in Calculate |
| `statistics-distribution.json` | 79-100 | Statistics editor, summaries, regressions, estimates, distributions |
| `spreadsheet-table.json` | 100-113, 164-165 | Spreadsheet and Table |
| `equation-inequality-ratio.json` | 114-125, 145-146 | Equation, Inequality, Ratio |
| `complex-basen.json` | 125-132 | Complex, Base-N |
| `matrix-vector.json` | 132-145 | Matrix, Vector |
| `mathbox-errors-precedence.json` | 146-169 | Math Box, errors, calculation priority |
| `domains.json` | 169-171 | Calculation range and every function input domain (different shape, see the file) |

164 cases in Phase 0.

## 3. Case format

```json
{ "id": "format-standard-decimal-001", "page": 44, "app": "Calculate", "kind": "expression", "status": "ready",
  "settings": { "inputOutput": "MathI/MathO" },
  "input": "π÷6",
  "expect": { "result": "1⌟6π", "decimal": "0.5235987756", "standard": "1⌟6π" },
  "expectationSource": "manual" }
```

| Member | Required | Meaning |
|---|---|---|
| `id` | yes | Unique, kebab-case, stable forever (a case is referred to by id) |
| `page` | yes | Printed page of the manual (1-174) |
| `app` | yes | `Calculate`, `Statistics`, `Distribution`, `Spreadsheet`, `Table`, `Equation`, `Inequality`, `Complex`, `BaseN`, `Matrix`, `Vector`, `Ratio`, `MathBox` |
| `kind` | yes | `expression`, `sequence`, `calc`, `property`, `statistics`, `distribution`, `spreadsheet`, `table`, `simultaneous`, `polynomial`, `solver`, `inequality`, `ratio`, `mathbox` |
| `status` | yes | §4 |
| `profile` | no | `Standard` (default) or `Extended` |
| `settings` | no | Calc settings differing from the initial state (see below) |
| `input` | for `expression`/`calc` | Canonical Linear Syntax ([LINEAR-SYNTAX.md](LINEAR-SYNTAX.md)) |
| `given` | no | Kind-specific inputs: data rows, matrices, coefficients, cells, defined functions, CALC values |
| `steps` | for `sequence` | Ordered `{ "input", "store"?, "switchBaseMode"?, "expect" }` run in one session |
| `expect` | unless `needs-oracle` | Expected outcomes (below) |
| `expectationSource` | yes | §4 |
| `note` | when not `ready` | Why, and what was assumed |

**`settings` keys.** Values are validated against a closed list:

- `inputOutput`, `angleUnit`, `numberFormat` (`Norm1`, `Norm2`, `Fix0`-`Fix9`, `Sci1`-`Sci10`)
- `engineerSymbol`, `fractionResult` (`Improper`/`Mixed`), `complexResult` (`a+bi`/`r∠θ`)
- `decimalMark`, `digitSeparator`
- `baseMode`, `frequency`, `complexRoots`, `verify`

A setting that is not listed has its initial value (manual p. 22).

**`expect` members:**

- **Display in the case's settings:** `result`.
- **Each FORMAT target:** `standard`, `decimal`, `primeFactor`, `recurringDecimal`, `improperFraction`,
  `mixedFraction`, `eng`, `sexagesimal`, `polar`, `rectangular`.
- **Parsing:** `equivalentTo`, an expression that must parse to the same tree.
- **State after evaluation:** `variables`, e.g. `{ "E": "2", "F": "1", "Ans": "2" }`.
- **Verify and errors:** `verify` (`True`/`False`); `error` (a `CalcErrorKind` name); `message` (a `SolutionOutcome`
  name: `NoRealRoots`, `NoSolution`, `InfiniteSolutions`, `AllRealNumbers`).
- **Structured results:** `matrix`, `vector`, `roots`, `extremum`, `solution`, `rows`, `values`, `formulas`, and
  statistic names such as `Σx`, `σx`, `a`, `r`.
- **Random functions** (`kind: property`): `range` and `step`.
- **Math Box forms** (`kind: mathbox`, whose `given` names one form: `simulation` with `dice` or `coins` and `attempts`; `numberLine` with `a` and `b`, or `numberLines`; `viewWindow` with `center` and `scale`; `circle` with `angle`; `clock`): `view` (`scale`, `center`, `minimum`, `maximum`), `sin`, `cos`, `tan`, `angles` (θ1 and θ2), and `error`.

The JSON reader is strict. An unknown member (a typo such as `expcet`) fails the data tests instead of silently
dropping an expectation.

## 4. Expectation sources and statuses

| `expectationSource` | Meaning |
|---|---|
| `manual` | The value is printed in the manual's text |
| `derived-exact` | Computed from data the manual prints, with exact rational arithmetic and integer square roots - never with floating point (§5) |
| `reference` | Needs a transcendental function; computed with series at 60 digits in PeterO.Numbers, never with `System.Math` (§5) |
| `pending-oracle` | Needs data or a reference that does not exist yet |

| `status` | Meaning |
|---|---|
| `ready` | Fully specified |
| `needs-oracle` | Waiting for data or a reference value (`expectationSource` is `pending-oracle`) |
| `needs-visual-check` | An input or output exists only as an image in the manual; the `note` states the assumption made. The maintainer confirms by looking at the page |

Until an application's engine exists, every one of its cases is reported as **skipped**, naming the phase that
implements it. A case is never reported as passed before the code it describes exists.

**Syntax checks (Phase 2).** `ConformanceSyntaxTests` parses every input of every case in its application, with
relations allowed when the case sets `"verify": "On"`:

- an input must parse, unless the case expects `SyntaxError`, in which case it must fail with a syntax error code;
- printing the tree and parsing it again must give an equivalent tree;
- `equivalentTo` must parse to the same tree as `input`.

These tests do not change a case's status: parsing an expression is not computing the calculator's result.

**Engine runs (Phases 3 and 4).** `CalculatorConformanceTests` runs every case whose application has an engine
through a `CalculatorSession` with the case's settings and profile (`ConformanceRunner`). Each expectation is checked:
the display, the FORMAT conversions, the variables after the calculation, the Verify result, the error kind, the rows
of a table or a sheet, and the range and step of a random function. An expectation the runner does not know fails the
case, so nothing passes because part of it was ignored.

Since Phase 6 M1 every application runs and no case is skipped: Calculate, Complex and Base-N (Phase 3), then
Matrix, Vector, Statistics, Distribution, Equation, Inequality, Ratio, Spreadsheet and Table (Phase 4), and Math Box
(Phase 6), whose four cases grew to eleven: the two errors that were the only Math Box forms in the data became cases
of `kind: mathbox`, and the Circle, Clock and Number Line examples of pp. 155-161 joined them.

## 5. How derived values were computed

Statistics, regressions and several decimals were computed on 17 Sep 2026:

- **Arithmetic** with a throwaway `BigInteger` rational program: exact sums, products and quotients.
- **Square roots** by integer square root of `q × 10⁸⁰`, i.e. 40 guard digits, then rounded half away from zero to
  10 significant digits.

The method reproduces every value the manual prints and that can be checked this way:

| Expression | Value | Page |
|---|---|---|
| `3√2` | 4.242640687 | 33 |
| `99√999` | 3129.089165 | 35 |
| `1⌟1⌟1234567` | 1.00000081 | 33 |

**Reference values** (`expectationSource: reference`) were computed on 17 Sep 2026 with PeterO.Numbers at 60
significant digits, using series written for the purpose rather than the library's own transcendental functions:

- π by Machin's formula, arctan by its Taylor series, exp by its Taylor series, erf by its Maclaurin series.
- Each was cross-checked against a known constant: π and e to 40 digits, and Φ(−1) = 0.158655253931457051414767….

| Case | Value |
|---|---|
| `vector-angle-001` | arctan(2/11) = 10.30484646876603194528…° |
| `stats-1var-normdist-001` | t = −79/√2579 = −1.55561248564866246429…; Φ(t) = 0.05990013396322900464… |
| `dist-normal-pd-001` | e^(−1/8) / (2√(2π)) = 0.17603266338214973888… |

Examples of derived values:

- 1-variable data of p. 83: `σx = √(2579/400) = 2.539192785`.
- Linear regression of p. 84: `a = 10009/19845`, `b = 1906/3969`, `r² = 908209/916839`.
- `5.5ŷ = 2312/735 = 3.145578231`.

## 6. Unverified behaviors and working assumptions

No physical reference calculator is available (decided 17 Sep 2026). The behaviors below are not fully specified by the manual.
Each has an explicit working assumption; cases depending on one are omitted or marked `needs-visual-check`.

| # | Behavior | Working assumption | How to settle it |
|---|---|---|---|
| U1 | Quartiles Q1/Q3 with frequencies | Median of the lower and upper halves of the frequency-expanded data, excluding the median when n is odd (the convention of the same calculator series). *Partly confirmed 18 Sep 2026:* the 1-Var Results screen of p. 84 shows Q1 = 4, Med = 6.5 and Q3 = 8 for the 20 values of Example 3, which this rule gives; pp. 93-95 give no quartile formula, and no example has an odd n. A single value is all three quartiles | The manufacturer's official emulator, for an odd n |
| U2 | Associativity of `^` and `ˣ√` in linear input (`2^3^2`) | Left to right, by the general rule of p. 168: 64 | The manufacturer's official emulator |
| U3 | Display rounding at an exact tie (`0.125` in Fix 2; negative ties) | Half away from zero | The manufacturer's official emulator |
| U4 | Sign of `Q(t)` for t < 0 | `Q(t)` is the area between 0 and \|t\|, non-negative | The manufacturer's official emulator (the figure of p. 91 shows only t > 0, and pp. 93-95 have no distribution formulas) |
| U5 | Regression formulas (pp. 93-95 are images) | *Resolved 18 Sep 2026 by reading pp. 93-95:* least squares on linearized data, as assumed: `ln x` (logarithmic, power), `ln y` (both exponential forms, power), `1/x` (inverse), with a and b of the exponential and power forms recovered through `exp`; the quadratic regression solves its normal equations; r is Pearson's coefficient of the linearized data | Done |
| U6 | Verify of an irrational identity such as `(√2)² = 2` | `True` | The manufacturer's official emulator |
| U7 | Exact display of cubic and quartic roots | Surd form only when the polynomial factors over Q into factors of degree ≤ 2 and the forms fit the display bounds; decimals otherwise | The manufacturer's official emulator |
| U8 | Matrices of Examples 3-9 (p. 137, images) | *Resolved 18 Sep 2026 by reading p. 137:* MatA-MatD are printed there, and MatB is [[2, 3], [2, 1]], not the MatB of Example 1; `matrix-add-001` was corrected to it | Done |
| U9 | Operator of the second Ans example (p. 37, key icons) | `789 − Ans` | Visual check of p. 37 |
| U10 | Which relational operators may not combine in a Verify chain (p. 75 lists them in an image) | `≠` does not combine with `< > ≤ ≥`; the manual's example is `4<6≠8` | Visual check of p. 75, or the manufacturer's official emulator |
| U11 | Stack size behind Stack ERROR (not stated in the manual) | 128 nested parentheses, functions or signs | The manufacturer's official emulator |
| U12 | Base-N beyond the four operations and the logic operators (p. 51 says only that the CATALOG commands of pp. 51-69 are unavailable) | Base-N has numbers, base prefixes, `+ − × ÷`, `and or xor xnor`, `Not(`, `Neg(`, parentheses and the memories x, y, z and Ans (A to F are hex digits there, in every number mode; confirmed by the maintainer on 18 Sep 2026); a result outside the signed 32-bit range is a Math ERROR in every number mode, and division drops the fractional part toward zero | The manufacturer's official emulator |
| U13 | `÷R` inside a larger expression, and what "too large" means (p. 56) | Only the quotient passes on, and nothing is stored in E or F; a dividend or divisor of 10¹⁰ or more divides normally | The manufacturer's official emulator |
| U14 | `Pol(` and `Rec(` inside a larger expression, and what reaches Ans | They stand on their own, as the manual's examples show; Ans takes the first result (r, or x) | The manufacturer's official emulator |
| U15 | The order of the terms of a two-term display form (p. 35 shows only `45√3+10√2`) | The rational part first, then square roots by decreasing radicand | The manufacturer's official emulator |
| U16 | Whether a result is displayed in degrees-minutes-seconds (p. 49 shows the conversion, not the rule) | A sum or difference of sexagesimal values is displayed as one | The manufacturer's official emulator |
| U17 | Which engineering symbol a result is displayed with (p. 64) | The one that leaves the mantissa in [1, 1000); outside f…E the result is displayed normally | The manufacturer's official emulator |
| U18 | A result whose magnitude is below the calculation range, 10⁻⁹⁹ (p. 169) | It becomes 0, which the domain of xʸ (p. 171) implies by allowing y·log x down to −10¹⁰⁰. Both ends of the range are taken at ten significant digits: a result that displays as 1×10⁻⁹⁹ is in range, so a `double` one unit below 10⁻⁹⁹ is not lost | The manufacturer's official emulator |
| U19 | The value of the scientific constant `t` (p. 66 lists it without a value) | The zero of the Celsius scale, 273.15 K | The manufacturer's official emulator |
| U20 | Matrix and vector operations the manual does not list, and their errors (pp. 132-145, 163) | A matrix or vector may be divided by a number, entry by entry (the ÷ key is offered on the MatAns and VctAns screens); any other operation with operands it does not take (`MatA+1`, `1÷MatA`, `MatA^2`, `Det(2)`) is a Math ERROR; sizes that do not fit are a Dimension ERROR; an `Identity(` size outside 1-4 is an Argument ERROR | The manufacturer's official emulator |
| U21 | How matrix and vector entries are displayed | As decimals in the number format, never as fractions or surds: the VctAns screen of p. 145 shows UnitV of (3, 4) as (0.6, 0.8) in MathI/MathO | Other screens of pp. 132-145 |
| U22 | Which frequencies the Statistics editor takes (pp. 80-83 give no rule) | Any real number. A negative frequency makes every statistic a Math ERROR; a row with frequency 0 is left out, of the extremes and the quartiles too; a fraction weights the sums and means, and makes the quartiles a Math ERROR, because they count values (confirmed by the maintainer, 18 Sep 2026) | The manufacturer's official emulator |
| U23 | How statistic and distribution results are displayed | As decimals in the number format, never as fractions or surds, when the input uses a statistic variable, an estimate, ▶t or P( Q( R(, and for every Distribution result: every screen of pp. 83-100 shows a decimal where MathI/MathO would give a fraction (x̄ = 5.95, not 119⌟20; a = 0.5043587805, not 10009⌟19845; a binomial 0.8125, not 13⌟16). Other calculations in Statistics display as in Calculate, and FORMAT still converts | Other Statistics Calc screens, or the manufacturer's official emulator |
| U24 | The domains of the Distribution parameters the manual does not give (p. 98 gives 0 ≤ p ≤ 1, σ > 0, 0 ≤ Area ≤ 1) | x and N are whole numbers with 0 ≤ x ≤ N; Poisson's x is a whole number of 0 or more and λ > 0; Lower ≤ Upper; Area 0 and 1 give an infinite x, which is a Math ERROR. A value outside is a Math ERROR of that calculation, "ERROR" in its row of a list (p. 97). A binomial whose fraction has too many digits for the budget is a Time Out (p. 165 lists Time Out for Distribution) | The manufacturer's official emulator |
| U25 | What the Equation and Inequality applications do where the manual only shows a screen (pp. 114-125) | The roots of a polynomial come by decreasing real part, a conjugate pair with the positive imaginary part first, and a repeated root as often as it is a root; a leading coefficient of 0 is a Math ERROR, because the degree is the one the application asked for; Complex Roots off leaves the real roots, and none of them is "No Real Roots"; a cubic has its two extrema by increasing x, and a double root of the derivative is "No Local Max/Min"; the solution of a system stays in the result, and only the Solver stores what it found, in the variable it solved for; the Solver settles when a step no longer reaches the digits the solution is held to, and reports Cannot Solve after 200 steps; an inequality satisfied only at a root is written `x=1` | The manufacturer's official emulator |
| U26 | What the Spreadsheet and Table applications do where the manual only shows a screen (pp. 100-113) | An empty cell reads as 0, and every cell of a range counts in Mean; a cell displays its value with the settings in effect, so MathI/MathO shows a fraction where the calculator's screens show whole numbers; a range may be written either way round (`Sum(A3:A1)`); a cell in error carries its error to the cells that read it; the sheet holds 1,700 bytes, an input 49, and more is a Memory ERROR; a table steps through decimals, so a Start, End or Step that is no decimal is a Range ERROR, as are a step of 0 and one that leads away from the end; generating a table leaves Ans and the history alone, as entering a cell does | The manufacturer's official emulator |
| U27 | How a relative frequency of a simulation is shown (pp. 148-153) | As a decimal under every Input/Output setting: p. 150 shows 46 of 250 as 0.184 where MathI/MathO would give 23⌟125. The value is the exact decimal quotient; the application formats it with decimal output. With one coin, the Relative Freq rows are tails then heads, the order in which two and three coins count their heads (from ●×0 up) | The manufacturer's official emulator |
| U28 | Number Line bounds that are equal (p. 165 names only a > b, as 10<x≤5) | a = b is a Range ERROR in every form with two bounds: it is an empty set, or the one point x=a already draws | The manufacturer's official emulator |
| U29 | The View-Window the Number Line application sets by itself (p. 156 shows one example, not the rule) | The smallest scale of 1, 2 or 5 times a power of ten whose eight ticks on each side span the bounds of every expression, and the center the middle of the bounds rounded to a tick: the example of p. 156 (x≤-1.5, x>-1.0, -2.0<x≤-0.5) gives Scale 0.2 and Center -1.2, as the manual shows. A single bound spans the larger of its distance from 0 and 1; no expression is the view of 0 with Scale 1 | The manufacturer's official emulator, with other expressions |
| U30 | A trigonometric value of the Circle application that does not exist (tan 90°) | That value is its Math ERROR, and the angle is still drawn with its sine and cosine | The manufacturer's official emulator |
| U31 | The Clock at 12:00 and at 6:00 (pp. 158, 161 show 3:00) | At 12:00, θ1 = 0 and θ2 is a full turn; at 6:00 both are half a turn | The manufacturer's official emulator |
| U32 | The keys while a calculation runs, and a way to stop it (pp. 162-166 name Time Out as a calculation's only end short of its result) | Every key waits, as the application's window takes none; AC - Escape on the keyboard - stops the calculation. What was typed stays on the line, nothing is shown or stored, and Ans is what it was; a sheet is left as it was before the change | The manufacturer's official emulator |
| U33 | What is entered in the sheet while Auto Calc is off (p. 107 says only that the sheet is then calculated again by Recalculate) | A formula entered is calculated as it is entered, reading every other cell as it holds its value; the formulas that refer to a cell that changed - a formula or a constant entered, a cell cleared - keep their values until Recalculate. A formula entered is thus never shown without its value or its error | The manufacturer's official emulator |
| U34 | What a value of a table or a cell of the sheet shows where its calculation failed (pp. 102-112 show no such screen) | The error, named as the line names it (Math ERROR, Time Out, …), in the place of the value; the rest of the row or the sheet is shown as it is. A cell that reads a cell in error ends in that error too (U26), and shows it | The manufacturer's official emulator |

Assumption U2 (`^` left to right) is implemented by the parser (docs/LINEAR-SYNTAX.md §3), U1, U4, U11-U13, U18, U20, U22,
U24-U31 and U33 by the engine and the applications, U14-U17, U21 and U23 by the formatter, U32 and U34 by the application
(`SessionWork`, the window's cover; `TableCell` and `SheetCell`, which carry the error of a value).

## 7. Deliberate deviations from the calculator

These are decisions, not bugs. They apply to both profiles unless stated.

| # | Deviation | Reason |
|---|---|---|
| D1 | Arithmetic on decimal input has no rounding error at the 10th digit, and transcendental results carry about 15 significant digits instead of ±1 at the 10th | `decimal` and `System.Math` are more precise than the calculator ([PRECISION.md §1](PRECISION.md#1-what-pallas-promises)) |
| D2 | Newest CODATA constants (the calculator ships CODATA 2018) | Decision of 17 Sep 2026 |
| D3 | Newest CIAAW atomic weights (the calculator ships IUPAC 2019): scandium is 44.955907 rather than 44.955908 | Same decision |
| D4 | Exact unit definitions where they exist (the calculator's rounding is unknown) | Decision of 17 Sep 2026 |
| D5 | `d/dx` at a non-differentiable point is a Math ERROR; the calculator's numerical derivative returns a number | Automatic differentiation is exact and refuses rather than guesses |
| D6 | *Withdrawn 17 Sep 2026.* Was: Verify of an undecidable irrational comparison is `Undetermined`. Without certified arithmetic nothing is undecidable; values compare at 15 significant digits ([PRECISION.md §9](PRECISION.md#9-equality-and-verify)) | The id stays reserved |
| D7 | Prime factorization is complete | `Extended` profile only; `Standard` keeps `2×(1018081)` |
| D8 | "Same Result" #1-#3 of Dice Roll and Coin Toss give Pallas's own results: the same on every copy of Pallas and every version of .NET, but not the reference calculator's | The calculator does not publish its sequences (p. 150). Each preset is a `System.Random` seeded with its number, whose seeded sequence .NET keeps stable; `MathBoxTests` pins it (decision of 24 Sep 2026) |

## 8. Adding a case

1. Transcribe the example with its page number. If a value comes from an image, set `needs-visual-check` and state
   the assumption in `note`.
2. If the expected value is not printed, derive it exactly (§5) and set `derived-exact`. If that is impossible
   without a transcendental function, set `needs-oracle`.
3. Run the data tests:

   ```bash
   dotnet test --project tests/Barbatos.Pallas.Conformance.Tests -f net10.0
   ```
