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
| `kind` | yes | `expression`, `sequence`, `calc`, `property`, `statistics`, `distribution`, `spreadsheet`, `table`, `simultaneous`, `polynomial`, `solver`, `inequality`, `ratio` |
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
- **Verify and errors:** `verify` (`True`/`False`); `error` (a `CalcErrorKind` name); `message` (`NoRealRoots`,
  `NoSolution`, `AllRealNumbers`).
- **Structured results:** `matrix`, `vector`, `roots`, `extremum`, `solution`, `rows`, `values`, `formulas`, and
  statistic names such as `Σx`, `σx`, `a`, `r`.
- **Random functions** (`kind: property`): `range` and `step`.

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
| U1 | Quartiles Q1/Q3 with frequencies | Median of the lower and upper halves of the frequency-expanded data, excluding the median when n is odd (the convention of the same calculator series) | Formula pages 93-95 (images), or the manufacturer's official emulator |
| U2 | Associativity of `^` and `ˣ√` in linear input (`2^3^2`) | Left to right, by the general rule of p. 168: 64 | The manufacturer's official emulator |
| U3 | Display rounding at an exact tie (`0.125` in Fix 2; negative ties) | Half away from zero | The manufacturer's official emulator |
| U4 | Sign of `Q(t)` for t < 0 | `Q(t)` is the area between 0 and \|t\|, non-negative | Formula pages 93-95 |
| U5 | Regression formulas (pp. 93-95 are images) | Least squares on linearized data: `ln x` (logarithmic, power), `ln y` (both exponential forms, power), `1/x` (inverse) | Visual check of pp. 93-95 |
| U6 | Verify of an irrational identity such as `(√2)² = 2` | `True` | The manufacturer's official emulator |
| U7 | Exact display of cubic and quartic roots | Surd form only when the polynomial factors over Q into factors of degree ≤ 2 and the forms fit the display bounds; decimals otherwise | The manufacturer's official emulator |
| U8 | Matrices of Examples 3-9 (p. 137, images) | Equal to the matrices entered in Examples 1-2 | Visual check of p. 137 |
| U9 | Operator of the second Ans example (p. 37, key icons) | `789 − Ans` | Visual check of p. 37 |

## 7. Deliberate deviations from the calculator

These are decisions, not bugs. They apply to both profiles unless stated.

| # | Deviation | Reason |
|---|---|---|
| D1 | Arithmetic on decimal input has no rounding error at the 10th digit, and transcendental results carry about 15 significant digits instead of ±1 at the 10th | `decimal` and `System.Math` are more precise than the calculator ([PRECISION.md §1](PRECISION.md#1-what-pallas-promises)) |
| D2 | Newest CODATA constants (the calculator ships CODATA 2018) | Decision of 17 Sep 2026 |
| D3 | Newest CIAAW atomic weights (the calculator ships IUPAC 2019) | Same decision |
| D4 | Exact unit definitions where they exist (the calculator's rounding is unknown) | Decision of 17 Sep 2026 |
| D5 | `d/dx` at a non-differentiable point is a Math ERROR; the calculator's numerical derivative returns a number | Automatic differentiation is exact and refuses rather than guesses |
| D6 | *Withdrawn 17 Sep 2026.* Was: Verify of an undecidable irrational comparison is `Undetermined`. Without certified arithmetic nothing is undecidable; values compare at 15 significant digits ([PRECISION.md §9](PRECISION.md#9-equality-and-verify)) | The id stays reserved |
| D7 | Prime factorization is complete | `Extended` profile only; `Standard` keeps `2×(1018081)` |

## 8. Adding a case

1. Transcribe the example with its page number. If a value comes from an image, set `needs-visual-check` and state
   the assumption in `note`.
2. If the expected value is not printed, derive it exactly (§5) and set `derived-exact`. If that is impossible
   without a transcendental function, set `needs-oracle`.
3. Run the data tests:

   ```bash
   dotnet test --project tests/Barbatos.Pallas.Conformance.Tests -f net10.0
   ```
