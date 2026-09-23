# Reference calculator: functional catalog

The functional reference Barbatos.Pallas is standardized against, extracted from its *User's Guide*
(Vietnamese edition, SA2207-A, 176 pages). Page numbers are the printed ones. Values from the manual are
transcribed as data in `tests/Barbatos.Pallas.Conformance.Tests/Data/calculator` (format:
[CONFORMANCE.md](CONFORMANCE.md)).

The PDF itself is the manufacturer's copyrighted work. It stays out of the repository (`.gitignore`) and is not redistributed.

- [1. Applications](#1-applications)
- [2. Calc settings](#2-calc-settings)
- [3. Commands and functions](#3-commands-and-functions)
- [4. FORMAT conversions](#4-format-conversions)
- [5. Memory, history and defined functions](#5-memory-history-and-defined-functions)
- [6. Calculation priority](#6-calculation-priority)
- [7. Errors](#7-errors)
- [8. Ranges, digits and accuracy](#8-ranges-digits-and-accuracy)
- [9. Algorithms: the reference calculator compared with Pallas](#9-algorithms-the-reference-calculator-compared-with-pallas)
- [10. Out of scope](#10-out-of-scope)

---

## 1. Applications

| Application | Functions | Reference calculator limits | Pages |
|---|---|---|---|
| Calculate | Arithmetic, fractions, functions, CALC, f(x)/g(x), Verify, history, FORMAT | 10 display digits, 23 internal | 28-76 |
| Statistics | 1-Variable (x, Freq), 2-Variable (x, y, Freq); sort; 1-Var/2-Var/Reg Results; Statistics Calc; seven regressions; x̂, ŷ, x̂₁, x̂₂; Norm Dist P( Q( R( ▶t. In Pallas: `StatisticsData` and `CalculatorSession.Regression`, with the statistic variables as names in the input; the Results screens are those names | 160 / 80 / 53 rows for 1 / 2 / 3 columns (Extended: 10,000) | 79-95 |
| Distribution | Binomial PD/CD, Normal PD/CD, Inverse Normal (left tail), Poisson PD/CD; List or Variable input. In Pallas: `CalculatorSession.CalculateDistribution` with `DistributionKind` and `DistributionParameters`; binomial exact, the others to the digits of `double` | List ≤ 45 items (Extended: 10,000); the calculator promises 6 significant digits | 95-100 |
| Spreadsheet | Constants and `=` formulas; relative/absolute references (`$A1`, `A$1`, `$A$1`); Grab; Cut & Paste (references unchanged); Copy & Paste (relative references move); Min( Max( Mean( Sum( over ranges; Fill Formula; Fill Value; Auto Calc; Show Cell (Formula/Value); Recalculate. In Pallas: `SpreadsheetGrid` and `SpreadsheetCell` (Barbatos.Pallas.Spreadsheet), whose formulas the engine calculates through `CalculatorSession.CellValues`; Show Cell is the application's, which has both the formula and the value | A1:E45; 49 bytes per cell; 1,700 bytes total; constants rounded to 10 significant digits | 100-107 |
| Table | Number table of f(x) and/or g(x) from Start, End, Step; edit x cells; ± step entry; Recalculate; Verify of f/g values. In Pallas: `NumberTable` with `TableType`, whose rows are calculations of the session; ± step entry is the application adding or subtracting Step from the row above | 45 rows (one function) / 30 rows (two) | 108-113 |
| Equation | Simultaneous linear equations, 2-4 unknowns; polynomials of degree 2-4 with local min/max (degree 2 and 3; "No Local Max/Min"); Complex Roots On/Off ("No Real Roots"); Solver (Newton, initial value, shows Left − Right, Continue/Exit). In Pallas: `CalculatorSession.SolveSimultaneous`, `SolvePolynomial` and `SolveEquation`, with `SimultaneousSolution`, `PolynomialSolution` and `CalculationKind.Solution`; the roots keep their exact form wherever the polynomial factors over the rationals | Solver returns one root | 114-124 |
| Inequality | Degree 2-4 with `> < ≥ ≤`; "No Solution", "All Real Numbers". In Pallas: `CalculatorSession.SolveInequality` with `InequalitySolution`, whose intervals carry both the bounds and the text the calculator writes | | 124-125 |
| Complex | `a+bi` and `r∠θ` input; arithmetic; Conjugate, Absolute Value, Argument, Real Part, Imaginary Part; rectangular/polar display | θ in (−180°, 180°]; `(a+bi)ⁿ` needs \|n\| < 10¹⁰ | 125-129 |
| Base-N | Decimal/Hexadecimal/Binary/Octal modes; prefixes d h b o; Neg, Not, and, or, xor, xnor | 32-bit two's complement; fractional parts dropped | 129-132 |
| Matrix | MatA-MatD, MatAns; `+ − ×`, scalar product, ², ³, ⁻¹, Det, Trn, Identity(n), Abs (element-wise) | Up to 4×4 | 132-139 |
| Vector | VctA-VctD, VctAns; `+ − ×`, scalar product, dot, cross, Angle, Unit Vector, Abs | 2 or 3 dimensions | 139-145 |
| Ratio | `A:B = X:D`, `A:B = C:X`. In Pallas: `CalculatorSession.SolveRatio` with `RatioForm`; the result goes to Ans | A zero coefficient is a Math ERROR | 145-146 |
| Math Box | Dice Roll and Coin Toss (1-3 dice/coins, 1-250 attempts, List/Relative Freq, "Same Result" #1-#3 seeds); Number Line (three axes, nine forms `x<a` … `a≤x≤b`, View-Window); Circle (Unit Circle, Half Circle, Clock). In Pallas: `CalculatorSession.Simulate` with `Simulation` and its `Frequencies`, `NumberLine.Define`, `Fit` and `View`, and `CalculatorSession.CircleAngle` and `Clock`, whose values are calculations of the session | Attempts 1-250; number-line bounds and center ±10¹⁰, scale 10⁻¹⁰-10¹⁰; the Unit Circle -10000 < θ < 10000, the Half Circle 0-180°, π or 200 grad | 146-161 |

Verify is available in Calculate, Table, Equation and Complex (p. 73).

## 2. Calc settings

Initial values are marked ◆ (pp. 22-25).

| Setting | Options |
|---|---|
| Input/Output | MathI/MathO◆, MathI/DecimalO, LineI/LineO, LineI/DecimalO |
| Angle Unit | Degree◆, Radian, Gradian |
| Number Format | Fix 0-9 (decimal places); Sci 1-10 (significant digits); Norm 1◆ (exponent form when \|x\| < 10⁻² or \|x\| ≥ 10¹⁰); Norm 2 (\|x\| < 10⁻⁹ or \|x\| ≥ 10¹⁰) |
| Engineer Symbol | On, Off◆ |
| Fraction Result | Mixed Fraction, Improper Fraction◆ |
| Complex Result | a+bi◆, r∠θ |
| Decimal Mark | Dot◆, Comma (the list separator changes from `,` to `;`) |
| Digit Separator | On, Off◆ |

System settings (contrast, auto power off, QR code version) have no Pallas equivalent. Language (English/Tiếng Việt)
is provided by the app through Barbatos.i18n.

## 3. Commands and functions

CATALOG groups shared by the applications (pp. 51-69):

| Group | Items |
|---|---|
| Func Analysis | `d/dx(f(x), a[, tol])`; `∫(f(x), a, b[, tol])` (Gauss-Kronrod); `Σ(f(x), a, b)` and `Π(f(x), a, b)` with integer bounds, \|a\|, \|b\| < 10¹⁰; `÷R` (quotient to E, remainder to F, Ans = quotient; falls back to ordinary division for non-positive-integer cases); `log(a, b)`, `logₐb`, `ln`. tol ≥ 10⁻²² when given |
| Probability | `%` (postfix, ÷100); `x!`; `nPr`; `nCr`; `Ran#` (0.000-0.999, a fraction in MathO); `RanInt#(a, b)` |
| Numeric Calc | GCD, LCM, Absolute Value, Recurring Decimal input `0.(312)`, Integer Part (Int, truncation), Round Off (Rnd, to the display format; Norm rounds at the 11th mantissa digit), Largest Integer (Intg, floor) |
| Angle/Coord/Sexa | `°`, `ʳ`, `ᵍ` unit markers; `Pol(x, y)` and `Rec(r, θ)` (results in x and y); degrees-minutes-seconds `°′″` |
| Hyperbolic/Trig | sin, cos, tan, sin⁻¹, cos⁻¹, tan⁻¹; sinh, cosh, tanh, sinh⁻¹, cosh⁻¹, tanh⁻¹ (angle unit ignored) |
| Engineer Symbol | m, μ, n, p, f, k, M, G, T, P, E |
| Sci Constants | 47 constants, CODATA 2018 on the calculator. Universal: h, ħ, c, ε₀, μ₀, Z₀, G, l_P, t_P · Electromagnetic: μ_N, μ_B, e, Φ₀, G₀, K_J, R_K · Atomic & Nuclear: m_p, m_n, m_e, m_μ, a₀, α, r_e, λ_C, γ_p, λ_Cp, λ_Cn, R∞, μ_p, μ_e, μ_n, μ_μ, m_τ · Physico-Chem: m_u, F, N_A, k, V_m, R, c₁, c₂, σ · Adopted: g_n, atm, R_K-90, K_J-90 · Other: t. **Pallas ships the newest CODATA set.** |
| Unit Conversions | 40 commands, NIST SP 811 (2008). Length: in↔cm, ft↔m, yd↔m, mile↔km, n mile↔m, pc↔km · Area: acre↔m² · Volume: gal(US)↔L, gal(UK)↔L · Mass: oz↔g, lb↔kg · Velocity: km/h↔m/s · Pressure: atm↔Pa, mmHg↔Pa, kgf/cm²↔Pa, lbf/in²↔kPa · Energy: kgf·m↔J, J↔cal₁₅ · Power: hp↔kW · Temperature: °F↔°C |
| Atomic Table | Periodic table; `AtWt(n)` for 118 elements, IUPAC 2019 on the calculator. **Pallas ships the newest CIAAW values.** |
| Other | Ans, PreAns, π (internal 3.1415926535897932384626), e (internal 2.7182818284590452353602), √(, ˣ√(, ⁻¹, ², ^(, (−) |

Application-specific CATALOG items (Statistics variables, Spreadsheet commands, Complex functions, Base-N logic,
Matrix and Vector commands) are listed with their applications in §1.

## 4. FORMAT conversions

Standard · Decimal · Prime Factor · Recurring Decimal · Rectangular Coord · Polar Coord · Improper Fraction ·
Mixed Fraction · ENG Notation · Sexagesimal (pp. 42-50). Display bounds of the reference calculator:

- **Fractions:** ≤ 10 digits including separators.
- **Surd forms:** `±a√b`, `±d ± a√b`, `±a√b/c ± d√e/f` with 1 ≤ a < 100, 1 < b < 1000, 1 ≤ c < 100,
  0 ≤ d < 100, 0 ≤ e < 1000, 1 ≤ f < 100.
- **π forms:** \|x\| < 10⁶.
- **Prime factorization:** integers of ≤ 10 digits. A factor ≥ 1,018,081 is left unfactored in parentheses, and so
  is a value with two or more factors of more than three digits.
- **Recurring decimals:** ≤ 99 bytes of display data.
- **Sexagesimal:** ≤ 9,999,999°59′59″.

## 5. Memory, history and defined functions

- **Ans** holds the last result. **PreAns** holds the one before it (Calculate only; cleared when leaving Calculate).
- **Variables** A-F, x, y, z are stored and recalled from the variable list; the list always displays in Norm 1.
- **History and replay** exist in Calculate, Complex and Base-N. History is cleared by AC, by changing Input/Output,
  and by Reset.
- **CALC** substitutes variable values into an expression (Calculate only). Values are always entered in linear
  form.
- **f(x) and g(x)** are defined once and shared with Table. One may reference the other; mutual reference is a
  Circular ERROR.
- **Undo/redo** of the last key operation.

## 6. Calculation priority

Evaluated left to right within a level (pp. 168-169):

1. Parenthesized expressions
2. Functions with parentheses: `sin(`, `log(`, `f(`, `g(` …
3. Postfix functions `² ³ ⁻¹ ! °′″ ° ʳ ᵍ % ▶t`, engineering symbols, `^`, `ˣ√`
4. Fractions
5. Negative sign `(−)`, base prefixes d h b o
6. Unit conversions, statistical estimates `x̂ ŷ x̂₁ x̂₂`
7. Multiplication with the sign omitted
8. `nPr`, `nCr`, `∠`
9. Dot product `•`
10. `×`, `÷`, `÷R`
11. `+`, `−`
12. `and`
13. `or`, `xor`, `xnor`

Consequences:

- `−2² = −4` and `(−2)² = 4`.
- A multiplication with the sign omitted is parenthesized automatically: `6÷2(1+2)` means `6÷(2(1+2))`, and `6÷2π`
  means `6÷(2π)` (p. 29).
- Verify's relational operators bind loosest. A chain must point in one direction; mixing `<` with `≠` is a
  Syntax ERROR (p. 75).

## 7. Errors

| Error | Cause | Pallas `CalcErrorKind` |
|---|---|---|
| Syntax ERROR | Malformed expression | `SyntaxError` |
| Math ERROR | Out-of-range intermediate or final result, input outside a function's domain, illegal operation (÷0), complex value where not allowed | `MathError` |
| Stack ERROR | Numeric/command/matrix/vector stack exceeded | `StackError` |
| Argument ERROR | Bad argument | `ArgumentError` |
| Dimension ERROR | Incompatible matrix or vector sizes | `DimensionError` |
| Variable ERROR | Solver expression contains no variable | `VariableError` |
| Cannot Solve | Solver did not converge | `CannotSolve` |
| Range ERROR | Table too long; bad Spreadsheet fill range; Math Box input out of range | `RangeError` |
| Time Out | Derivative, integral or distribution did not meet its end condition | `TimeOut` |
| Circular ERROR | f/g or cell circular reference | `CircularError` |
| Memory ERROR | Spreadsheet over 1,700 bytes or chained references | `MemoryError` |
| No Operator | Verify on an expression without a relational operator | `NoOperator` |
| Not Defined | f(x)/g(x) or a matrix/vector used before definition | `NotDefined` |

The calculator places the cursor at the error (p. 162), so every Pallas error carries a source span.

## 8. Ranges, digits and accuracy

- **Calculation range:** ±1×10⁻⁹⁹ to ±9.999999999×10⁹⁹, or 0.
- **Internal digits:** 23.
- **Accuracy:** ±1 at the 10th digit for a single calculation, with errors accumulating in consecutive calculations
  (p. 169).
- **Function input domains** (pp. 170-171): transcribed in full as data in `Data/calculator/domains.json`.

In Pallas these limits belong to the `Standard` profile. The accuracy clause is deliberately not reproduced
([PRECISION.md §1](PRECISION.md#1-what-pallas-promises)).

## 9. Algorithms: the reference calculator compared with Pallas

Pallas computes with .NET's built-in types ([PRECISION.md §3](PRECISION.md#3-which-type-holds-a-value)). Rows
marked *planned* are designed in their phase and may change.

| Function | Reference calculator | Pallas |
|---|---|---|
| Arithmetic | 23-digit decimal, rounded | `decimal` (28-29 digits); `double` outside decimal's precise range |
| Transcendental functions | ±1 at the 10th digit | `System.Math` on `double`, about 15 significant digits |
| Fraction, √ and π forms | Recognized from a 23-digit value | Recognized at display time from the `decimal` value |
| Special angles | Approximate | Exact table (`cos 90° = 0`; `tan 90°` is a Math ERROR) |
| Integer functions (`!`, nPr, nCr, GCD, LCM) | `x! ≤ 69` | `BigInteger`, exact at any size |
| d/dx | Numerical, with tolerance | The bound tree is differentiated and the derivative evaluated like any expression: exact where the operations are, Math ERROR where there is no derivative |
| ∫ | Gauss-Kronrod with tolerance | Adaptive Gauss-Kronrod 7/15 on `double`, with the error estimate reported |
| Solver | Newton; one root; may miss | Newton on `Value` arithmetic, with the slope from a central difference: exact where the solution is a decimal, and as accurate as `double` where it is not |
| Polynomials 2-4 | Approximate | Exact rational roots and exact quadratic factors (surds and complex surds); a cubic or quartic with no rational root by Aberth on `Complex`, with its real roots counted by Sturm |
| Spreadsheet | 1,700 bytes; recalculated as needed | Cells calculated where they stand, each once per pass, with a Circular ERROR for a cell that is asked for while it is being calculated |
| Prime factorization | ≤ 10 digits; large factors left unfactored | Trial division on `long` (the `Standard` profile reproduces the calculator's display) |
| Matrices | ≤ 4×4; accuracy suffers near det = 0 | Entries follow the precision rule; determinant and inverse exact by fraction-free elimination on `BigInteger`, rounded once; up to 64×64 in the `Extended` profile |
| Distributions | 6 significant digits | Binomial exactly on `BigInteger`; normal through `ErrorFunction`; Poisson by Loader's saddle-point form |
| Random numbers | Device PRNG; "Same Result" presets | `System.Random`: the session's own for Ran#, RanInt# and a simulation, and one seeded with its number for each Same Result preset, which .NET keeps stable across versions - the same results on every copy of Pallas, not the calculator's (D8) |

## 10. Out of scope

- **QR Code output** (pp. 77-78): replaced by export and share in the app.
- **Hardware items:** battery, contrast, auto power off, the front cover.
- **The "Get Started" screen and the calculator ID.**
