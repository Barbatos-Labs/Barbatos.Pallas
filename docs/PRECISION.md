# Precision strategy

Barbatos.Pallas is used where a wrong digit has consequences: accounting and audit, mechanical and structural
engineering, construction, industrial simulation. This document is the contract every package upholds. A change
that weakens any rule here is a design change, reviewed as one, not a refactoring.

- [1. What Pallas promises](#1-what-pallas-promises)
- [2. The invariants](#2-the-invariants)
- [3. Which type holds a value, and where .NET already has the code](#3-which-type-holds-a-value-and-where-net-already-has-the-code)
- [4. Measured behavior of the built-in types](#4-measured-behavior-of-the-built-in-types)
- [5. Rounding](#5-rounding)
- [6. Scientific functions](#6-scientific-functions)
- [7. Where double may appear](#7-where-double-may-appear)
- [8. Controlling accumulated error, domain by domain](#8-controlling-accumulated-error-domain-by-domain)
- [9. Equality and Verify](#9-equality-and-verify)
- [10. Profiles](#10-profiles)
- [11. Budgets and honest failure](#11-budgets-and-honest-failure)
- [12. How accuracy is tested](#12-how-accuracy-is-tested)

---

## 1. What Pallas promises

Pallas computes with the numeric types .NET already provides, each where it is strongest. It does not implement
its own number types (decided 17 Sep 2026, see the decision log in [ARCHITECTURE.md](ARCHITECTURE.md#12-decision-log)).

1. **Decimal input stays decimal.** `0.1 + 0.2` is `0.3` exactly, because arithmetic uses `System.Decimal`, a
   base-10 type with 28-29 significant digits.
2. **Transcendental results are accurate to about 15 significant digits.** sin, ln, exp, √ and powers come from
   `System.Math` on `double` and return to `decimal` at 15 significant digits. The 10 digits the calculator displays
   are correct, except when the exact value lies within about 2×10⁻¹⁵ (relative) of a rounding tie at the 10th digit.
3. **Special angles are exact.** `sin 30° = 0.5`, `cos 90° = 0`, and `tan 90°` is a Math ERROR, not `1.6×10¹⁶`.
4. **Integer functions are exact at any size.** `69!`, `nCr` and `GCD` use `BigInteger`.
5. **A lost digit is never silent.** Wherever a built-in type would silently round away significant digits, Pallas
   detects it and continues with a type that keeps them, or reports an error.

For comparison, the reference calculator documents an accuracy of "±1 at the 10th digit for a single calculation", with
errors accumulating in consecutive calculations (user's guide, p. 169).

What Pallas does **not** promise: arbitrary precision, or a proof that every displayed digit is correct. Those need
a custom number tower; the maintainer chose the built-in types instead (§12 of ARCHITECTURE.md).

## 2. The invariants

| # | Invariant | Enforced by |
|---|---|---|
| I1 | Pallas never re-implements what `Math`, `System.Double`, `System.Decimal` or `BigInteger` provide, and never wraps a BCL function only to change how it reports errors (§6). | Code review; the §3 table |
| I2 | A decimal literal is parsed into `decimal`, never through `double`: `0.1` is exactly one tenth. | `Binder.TryParseNumber`, `ValueTests` |
| I3 | Rounding happens only at an explicit boundary (§5), never to an intermediate result. | Code review, tests |
| I4 | No silent loss: a value `decimal` would hold with fewer than 15 significant digits is kept as `double` (§3). | `Value.FromApproximation`, `ValueMath`, `ValueTests` |
| I5 | `decimal` and `BigInteger` results are identical on every runtime, OS and architecture. `double` results agree to the tested accuracy (§6) on Windows x64, where they are measured; elsewhere they come from that platform's C runtime and are not measured. | `decimal` and `BigInteger` are managed code, the same on every platform; `double` by CI on Windows x64 (§6) |
| I6 | `float`, `Half` and `MathF` appear nowhere in `src/core`; `double` and `System.Numerics.Complex` only in allow-listed assemblies (§7). | Two locks, §7 |
| I7 | A numerical method (integration, root finding) reports an error estimate or a named failure, never a bare number it cannot stand behind. | `Calculation.Integrals`, Time Out; `AccuracyTests` |

## 3. Which type holds a value, and where .NET already has the code

In C#, `double` is the keyword for `System.Double`, `decimal` for `System.Decimal`, `int` for `System.Int32`, `long`
for `System.Int64` and `float` for `System.Single`: the same types under two names. The repository writes the keyword
(IDE0049, as in Barbatos.Wpf and Barbatos.i18n), so `double.SinPi` is the `System.Double` member.

| Type | Used for | Why |
|---|---|---|
| `decimal` | **The calculator's number**: input, + − × ÷, %, fractions, sexagesimal values, statistics sums, unit factors, results | Base 10, so decimal input is exact; 28-29 significant digits, more than the reference calculator's 23 internal digits |
| `double` | sin, cos, tan and their inverses, sinh…, exp, ln, log, √, ˣ√, powers with non-integer exponents; values `decimal` cannot hold precisely | `decimal` has no transcendental functions; `double` spans ±1.8×10³⁰⁸ |
| `BigInteger` | `x!`, nPr, nCr, GCD, LCM, prime factorization; exact elimination of matrices and exact statistics sums and fits, on `decimal` values scaled to integers | Exact at any size: `28!` already overflows `decimal`, the calculator goes to `69!`; and sums cannot cancel |
| `int` | Base-N | The reference calculator's Base-N is 32-bit |
| `System.Numerics.Complex` | Complex mode | Built on `double` |

**Before writing numeric code, look here.** Everything in this table exists in .NET 8, 9 and 10:

| Need | .NET API |
|---|---|
| π, e, τ | `double.Pi` (= `Math.PI`), `double.E` (= `Math.E`), `double.Tau`. `MathF.PI` is the `float` version, 8.7×10⁻⁸ from π |
| Degrees ↔ radians | `double.DegreesToRadians`, `double.RadiansToDegrees` |
| sin, cos, tan of a multiple of π, exact at multiples of ½ | `double.SinPi`, `double.CosPi`, `double.TanPi`; inverses `double.AsinPi`, `double.AcosPi`, `double.AtanPi`, `double.Atan2Pi` |
| Exact remainder (removing whole turns) | `double.Ieee754Remainder` (= `Math.IEEERemainder`) |
| Powers, roots, hypotenuse | `Math.Pow`, `Math.Sqrt`, `Math.Cbrt`, `double.RootN` (real root of a negative base), `double.Hypot`, `double.Exp10` |
| Logarithms | `Math.Log`, `Math.Log10`, `Math.Log2`, `Math.Log(value, newBase)` |
| Hyperbolic functions | `Math.Sinh`, `Math.Cosh`, `Math.Tanh`, `Math.Asinh`, `Math.Acosh`, `Math.Atanh` |
| Classifying a result | `double.IsFinite`, `double.IsNaN`, `double.IsInfinity`, `double.IsInteger` |
| Rounding to decimal places | `Math.Round(decimal, int, MidpointRounding)` |
| Rounding to significant digits for display | `decimal.ToString("G10")`, `"E9"`, `"F3"` with `CultureInfo.InvariantCulture` |
| Parsing | `decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)` |
| `double` → `decimal` at 15 significant digits | the built-in conversion `(decimal)value` |
| GCD | `BigInteger.GreatestCommonDivisor` |

What .NET does not have, and `Barbatos.Pallas.Numerics` therefore provides:

- angle units including gradians (`Trigonometry`);
- factorial, permutations, combinations, LCM and prime factors (`IntegerFunctions`);
- fraction recognition (`Fractions`);
- degrees-minutes-seconds (`Sexagesimal`).

**Fractions, surds and π forms are display forms, not types.** A value computed exactly from square roots and π also
carries its *exact form*, a short sum of rational multiples of 1, √b and π recorded beside the number (decision of
18 Sep 2026). It is not a number type: arithmetic is still `decimal` and `double`. It does two things no search over
15 digits can do: it shows `10√(2)+15×3√(3)` as `45√(3)+10√(2)` (manual p. 35), and it makes `√(2)×√(2)` exactly 2,
where the 15-digit decimal of √2 squared is 2.000000000000014. A value with a form is also computed from the form in
`double`, which is why `Rec(√(2),45)` gives x = 1 rather than 1.0000000000000042.

Everything else is recognized from the value at display time. `Fractions.TryFromDecimal` finds the simplest fraction
within a tolerance that matches the value's precision: 10⁻²⁵ for an exact `decimal`, and 10⁻¹³ relative with
denominators of at most 10⁴ for a value that went through `double`, so that a transcendental result is not dressed up
as a fraction by coincidence. The calculator recognizes forms from its 23-digit value.

**The precision rule.** A value stays `decimal` only while it holds at least 15 significant digits.
- That is when it is zero or its magnitude is at least 10⁻¹⁴: `decimal` keeps 28 decimal places, so from 10⁻¹⁴ down to
  10⁻²⁸ exactly 15 digits remain.
- Below that, or beyond ±7.9×10²⁸, the value is `double`.
- The engine applies the rule where values are created (`Value`, `ValueMath`), using the APIs above: `decimal.TryParse`
  followed by a magnitude check, and the `OverflowException` of `decimal` arithmetic. A `decimal` product or quotient
  that lands below 10⁻¹⁴ is recomputed from the operands in `double`, not converted from the rounded `decimal`.
- Division by zero is not a precision question: `DivideByZeroException` is the calculator's Math ERROR.

## 4. Measured behavior of the built-in types

Measured on .NET 8 and .NET 10 (10.0.9), 17 Sep 2026. Each row is pinned by `BuiltInBehaviorTests`, so a platform
change is noticed.

| Behavior | Measurement | Consequence in Pallas |
|---|---|---|
| `decimal` adds decimal fractions exactly | `0.1m + 0.2m == 0.3m`; in `double`, `0.1 + 0.2` is `0.30000000000000004` | The arithmetic type |
| `decimal` rounds silently at its last digit | `1m / 3m * 3m` is `0.9999999999999999999999999999`; `0.1234567890123456789012345678m + 10m` is `10.123456789012345678901234568` | Fraction recognition turns the first back into `1`; display at 10 digits never shows the difference |
| **`decimal` loses small values silently** | `decimal.Parse("6.62607015E-34")` is `0`; `1.66053906660E-27` keeps 2 significant digits; `1.23456789E-20m * 1E-5m` keeps 4; `(decimal)6.62607015e-34` is `0` | The precision rule (§3) |
| `decimal` overflow is loud | Beyond ±7.9×10²⁸ it throws `OverflowException`; `28!` overflows; `(decimal)double.NaN` throws too | Integers use `BigInteger` |
| `(decimal)double` rounds to 15 significant digits | `Math.Sin(Math.PI / 6)` (0.49999999999999994) becomes `0.5`; `Math.Tan(Math.PI / 4)` becomes `1`; `1.005` becomes `1.005` | Removes binary noise |
| **…but not always correctly** | `Math.E` (2.71828182845904509…) becomes `2.71828182845904`, not `…905` | A converted value may be off by 1 in the 15th digit |
| `System.Math` near special angles | `Math.Cos(Math.PI / 2)` is `6.123233995736766E-17`; `Math.Tan(Math.PI / 2)` is `16331239353195370` | `double.CosPi(0.5)` is `0` and `double.TanPi(0.5)` is `+∞` (§6) |
| BCL results the calculator rejects or computes differently | `Math.Pow(0, 0)` is `1`; `Math.Log(1, 0)` is `-0`; `Math.Pow(-8, 1.0 / 3)` is `NaN` (the calculator gives −2) | The engine's domain checks (§6) |
| Rounding `double` to decimal places | `Math.Round(1.005, 2, MidpointRounding.AwayFromZero)` is `1`, because the double is 1.00499999999999989… | Round only `decimal` (§5) |
| Default midpoint rules differ | `Math.Round(2.5m)` is `2` (to even); `2.5m.ToString("F0")` is `3` and `2.345m.ToString("F2")` is `2.35` (away from zero) | Always pass a `MidpointRounding` explicitly |
| `(√2)²` in `double` | `Math.Sqrt(2) * Math.Sqrt(2)` is `2.0000000000000004`, which converts to `2` | 15-digit conversion is also what makes Verify usable (§9) |

## 5. Rounding

Rounding uses .NET only: `Math.Round(decimal, int, MidpointRounding)` for decimal places, and the standard numeric
format strings for significant digits.

| `MidpointRounding` | 2.5 → 0 decimals | −2.5 → 0 decimals | 2.45 → 1 decimal | 2.41 → 1 decimal |
|---|---|---|---|---|
| `AwayFromZero` - **display default** | 3 | −3 | 2.5 | 2.4 |
| `ToEven` (banker's) | 2 | −2 | 2.4 | 2.4 |
| `ToZero` (truncation) | 2 | −2 | 2.4 | 2.4 |
| `ToPositiveInfinity` (ceiling) | 3 | −2 | 2.5 | 2.5 |
| `ToNegativeInfinity` (floor) | 2 | −3 | 2.4 | 2.4 |

- **The default is `AwayFromZero`** ("half up", decided 17 Sep 2026), as the calculator displays.
  - The format strings `"G10"`, `"E9"` and `"F3"` round a `decimal` half away from zero, so Norm, Sci and Fix displays
    need no rounding code.
  - Whether another mode is needed for significant digits is decided with the formatter in Phase 3; the reference calculator
    has none.
- **Only `decimal` is rounded.** A `double` result is first converted with `(decimal)value`, which makes `1.005` a real
  tie before `Math.Round` sees it.
- **Always name the mode.** The .NET defaults disagree with each other (§4).

Rounding is applied **only** at these boundaries:

1. **Display** (Fix, Sci, Norm; 10 digits in the Standard profile). The stored value is unchanged.
2. **Explicit `Rnd(x)` or `Round(x, n, mode)`.** The rounded value replaces the original. With Fix 3,
   `10÷3×3` displays `10.000`, while `Rnd(10÷3)×3` displays `9.999` (manual pp. 60-61), measured with `decimal`.
3. **Converting `double` to `decimal`** (15 significant digits), the one rounding the type system forces.
4. **Entering a bounded domain.** Base-N drops fractional parts, as the reference calculator does. A spreadsheet constant with
   more than 10 significant digits is rounded in the Standard profile only.
5. **Never to an intermediate result** beyond what `decimal` itself does at 28 decimal places.

## 6. Scientific functions

**The engine calls .NET directly** (`Math.Exp`, `Math.Log`, `Math.Sinh`, `double.RootN`, …; see the §3 table). Pallas
does not wrap them. An earlier `ScientificMath` class re-exposed each function with exceptions instead of NaN; it was
removed on 17 Sep 2026 (decision log).

**Errors are decided once, in the engine's evaluator (Phase 3)**, from the result and the calculator's domain table
(`tests/Barbatos.Pallas.Conformance.Tests/Data/calculator/domains.json`):

- a NaN result is a Math ERROR;
- an infinite result is a Math ERROR (overflow, or tan of an odd multiple of a right angle);
- where .NET returns a number the calculator rejects, the domain table decides:
  - `0⁰` is a Math ERROR although `Math.Pow(0, 0)` is 1 (manual p. 171);
  - `log₀ 1` is a Math ERROR although `Math.Log(1, 0)` is -0;
- where .NET returns NaN for a value the calculator computes, the engine chooses the .NET function that computes it:
  `(−8)^(1/3)` is `double.RootN(-8, 3)`, −2.

**Trigonometry in calculator angle units** is the one thing .NET lacks, so `Trigonometry` provides it, entirely with
.NET calls.

- **Degrees and gradians.**
  - Whole turns are removed first with `double.Ieee754Remainder`, which is exact. Without it, 10⁶° / 180 loses 6×10⁻¹³
    of a half-turn; before the reduction was added, `tan 465°` measured 2.5×10⁻¹⁵ (relative) from −(2 + √3).
  - Then `double.SinPi`, `double.CosPi` or `double.TanPi` take the angle as a multiple of π. They are exact at multiples
    of a right angle: cos 90° is 0 and tan 90° is ±∞, where `Math.Cos(Math.PI / 2)` is 6.1×10⁻¹⁷.
  - Other values are within an ulp, and the 15-digit conversion to `decimal` makes sin 30° exactly 0.5 and tan 45°
    exactly 1.
- **Radians.**
  - `double.SinPi` and its siblings are used only when `angle / π` lands exactly on a multiple of ½, as `Math.PI / 2` and
    `3 * Math.PI / 2` do. Otherwise `Math.Sin` takes the radians directly.
  - Measured: `double.SinPi(13 * Math.PI / 6 / Math.PI)` is 0.5000000000000008, where `Math.Sin(13 * Math.PI / 6)` is 0.5.
  - `3.14159265358979`, π typed with 15 digits, keeps its sine of 3.2×10⁻¹⁵, as the calculator shows.
- **Inverse functions** use `double.AsinPi`, `double.AcosPi` and `double.AtanPi` times 180 or 200. asin 0.5 is
  30.000000000000004, and exactly 30 after conversion to `decimal`.
- **Conversions** use `double.DegreesToRadians` and `double.RadiansToDegrees`; 200 gradians are 180 degrees.

**Accuracy.** .NET does not document an accuracy bound for `System.Math`, which calls the platform C runtime
(UCRT on Windows, glibc on Linux). Pallas therefore measures it (§12):
- exp, ln, log₁₀, √, powers, sin and cos agree with 40-digit references within 5×10⁻¹⁵ (relative) over the tested
  ranges, including sin and cos of degree and gradian angles up to ±10⁶;
- tan at every multiple of 15° agrees within 2×10⁻¹⁵;
- combined with the 15-digit conversion (§4), a transcendental result carries about 14-15 correct significant digits.

**Cross-platform.** Different C runtimes may return results that differ in the last ulp. The accuracy tests run on
Windows x64 in CI (I5). The workflow also had Linux x64 and Linux ARM64 jobs, which never ran - the repository has
no remote yet - and the maintainer dropped them on 23 Sep 2026, because the application ships on Windows alone.
The packages still build for any platform - net8.0,
net9.0 and net10.0, with CA1416 an error on a Windows-only call - but a `double` result on another OS is the C
runtime's there, and nobody measures it.

## 7. Where double may appear

**Lock 1 - compile time.** `Microsoft.CodeAnalysis.BannedApiAnalyzers` runs on every `src/core` project with
`build/BannedSymbols.FloatingPoint.txt`, which bans `float` (`System.Single`), `Half` and `MathF` outright.
Single-precision types add nothing to a calculator but lost digits.

> **Measured limitation (17 Sep 2026).** A probe file showed the analyzer catches *member use* (`Math.Sqrt(double)`,
> `MathF`, `double.Parse`, casts) but **not declarations**. A `double` field, parameter or local and a literal such as
> `1.5` all compiled with no diagnostic. The analyzer alone is not a guarantee.

**Lock 2 - test time.** `Barbatos.Pallas.Architecture.Tests` reads every compiled core assembly with
`System.Reflection.Metadata` and reports every use of `double`, `float`, `Half`, `MathF` and
`System.Numerics.Complex`:

- field, parameter, return and local signatures, including inside generic arguments;
- the IL opcodes `ldc.r4/r8`, `conv.r4/r8/r.un`, `ldelem/stelem/ldind/stind` of r4 and r8;
- tokens referencing those types or their members.

`decimal`, `BigInteger` and the `decimal` and integer overloads of `System.Math` are not reported. The scanner is
itself tested: it must find every deliberately planted usage, and must not flag `decimal` or integer arithmetic.

**The allow-list.** `CoreFloatingPointTests.AllowList` names the assemblies where `double` may appear, each with its
reason.
- `Barbatos.Pallas.Numerics` (`Trigonometry`).
- `Barbatos.Pallas.Engine`: it calls `System.Math` and `System.Numerics.Complex` directly (§6), holds values `decimal`
  cannot keep to 15 significant digits, and integrates numerically. Graphing's screen coordinates get an entry in
  Phase 6.
- `Barbatos.Pallas.Data` needs none: a published value is a `decimal` mantissa and a power of ten (`ScaledDecimal`).
- `Barbatos.Pallas.LinearAlgebra` and `Barbatos.Pallas.Statistics` need none: they work on `decimal` values scaled to
  integers. The Phase 4 plan allowed `double` in Statistics; it was not needed, because square roots, logarithms and
  the normal distribution are computed in the engine and in Numerics (decisions of 18 Sep 2026).
- `Barbatos.Pallas.Solvers`, in `PolynomialRoots` only: the roots of a polynomial of degree 3 or more have no closed
  form a calculator can display, so they are iterated in `double` and `System.Numerics.Complex` (Aberth). A root that
  is rational is recovered from the iteration exactly, and `IntegerPolynomial` - the sign at a rational point, the
  number of real roots, division by a root - stays on `BigInteger` (decision of 22 Sep 2026).
- `float`, `Half` and `MathF` stay banned everywhere.
- Adding an entry needs the same review as changing this document.

## 8. Controlling accumulated error, domain by domain

The engine for each domain is designed in its phase. These are the rules it starts from.

**Arithmetic chains.**
- `decimal` chains accumulate nothing for + and − on decimal input, and at most one rounding at the 28th decimal
  place per × or ÷. Ten digits are displayed, so that rounding is invisible.
- Chains through transcendental functions accumulate about 10⁻¹⁵ relative per step. The calculator accumulates
  error at the 10th digit (p. 169).

**Matrices and vectors** (Phase 4).
- Entries are `Value`s: sums and products go through `ValueMath`, so each entry follows the precision rule and keeps
  its exact form (`√2` squared is exactly 2 inside a matrix too).
- A determinant of exactly 0 comes out as about 10⁻²⁷ after elimination in `decimal`, and no threshold can tell that
  from a small nonzero one. A matrix of `decimal` entries is therefore eliminated exactly: each row is scaled to
  integers and eliminated without fractions (Bareiss) on `BigInteger` (`ExactLinearAlgebra`), and the determinant
  and inverse are rounded once. An exact matrix is singular only when its determinant is exactly 0.
- A matrix with an approximate entry is singular when its determinant is within 10⁻¹³ of the Hadamard bound (the
  product of the rows' lengths): the tolerance values compare with (§9). `[[√2, 2], [1, √2]]` has a determinant of
  about 3×10⁻¹⁵ from its 15-digit entries; it is 0 and the matrix has no inverse.
- A matrix with an entry held as `double` is eliminated in `double` with partial pivoting.
- A vector's length is `ValueMath.SquareRoot` of its dot product, so |(3, 4)| is exactly 5 and |(1, 1)| is √2 with its
  form; the angle is `cos⁻¹` of the cosine, which rounding may put a little outside [−1, 1] and is then clamped.

**Statistics** (Phase 4).
- Sums (Σx to Σx⁴, Σy, Σy², Σxy, Σx²y), means, variances and the coefficients of the linear and quadratic regressions
  are exact: each column is scaled to integers once and summed on `BigInteger` (`ExactSample`), and each result is
  rounded once (`ValueMath.FromRatio`). The one-pass formulas of the manual cancel in `decimal`: Sxx of equal
  20-digit values comes out as about 10⁻²⁷ instead of 0, and a vertical line gets a slope instead of a Math ERROR. A
  value held as `double` enters as its shortest round-trip decimal, the digits it was entered with.
- σ, s and r are square roots of exact values (`ValueMath.SquareRoot`); r² itself is exact.
- The logarithmic, exponential, power and inverse regressions fit a line to ln x, ln y or 1/x (pp. 94-95), whose values
  follow the precision rule, and recover a and b through exp.
- A frequency is a weight: a negative one is a Math ERROR, 0 leaves the row out, and the quartiles need whole
  frequencies (assumption U22). A statistic result is displayed as a decimal (U23).

**Calculus.**
- `Σ` and `Π` evaluate in `decimal` while the terms are decimal, so `Σ(1⌟x,1,4)` is exactly 25⌟12.
- `d/dx` differentiates the bound tree and evaluates the derivative like any expression, so it is exact where the
  operations are: `d/dx(x³,0.1)` is exactly 0.03 and `d/dx(sin(x),π÷2)` is exactly 0. Where there is no derivative it
  is a Math ERROR (deviation D5), because the derivative expression divides by zero there. The `tol` argument of the
  calculator is accepted and validated (≥ 10⁻²²), and changes nothing.
- `∫` is adaptive Gauss–Kronrod 7/15 on `double`, the method the manual names (p. 52). It refines until the estimated
  error is below 10⁻¹⁴ relative to ∫|f| (or below a looser `tol`), answers while the estimate is within 10⁻⁹ or a
  looser `tol`, and reports Time Out otherwise. A `tol` tighter than 10⁻⁹ is accepted (down to 10⁻²², as on the
  calculator) but promises nothing more: `double` cannot guarantee it for a difficult integrand, and it used to end in
  Time Out (decision of 18 Sep 2026). Every integral of a calculation reports its estimate in
  `Calculation.Integrals` (I7).

**Equation, Inequality and Ratio** (Phase 4).
- A system of linear equations is exact: each row is scaled into integers, which leaves the solution where it is, and
  eliminated without fractions, so a solution such as 1/2 is a fraction and not 0.4999999999999999999999999999. A
  singular system has infinitely many solutions when its constants do not raise the rank (`ExactLinearAlgebra.Rank`),
  and none otherwise.
- A polynomial is scaled into integer coefficients the same way. Its rational roots are recovered exactly - the
  continued fraction of an iterated root proposes them, `IntegerPolynomial.SignAt` confirms them - and divided out, so
  a cubic or a quartic that factors over the rationals keeps the exact roots of the quadratic that is left, with its
  display form: `-1+√(3)`, `-3⌟4+√(23)⌟4i`. Only a cubic or a quartic with no rational root is iterated, and how many
  of its roots are real is decided exactly by Sturm's theorem, never by a tolerance on the imaginary part.
- A repeated root is kept: after the rational roots, a repeated factor can only be the square of a quadratic, which is
  solved exactly and counted twice.
- The extremum of a quadratic or a cubic is the polynomial evaluated at a root of its derivative, both of them exact
  where the coefficients are.
- An inequality is the sign of the polynomial at a rational point of each stretch between its real roots
  (`IntegerPolynomial.SignAt`), which is exact: no sampled sign can be off, however close two roots are.
- The Solver is Newton's method on Left − Right, with the slope from a central difference over |x|·10⁻¹⁰, all in
  `Value` arithmetic: an equation whose solution is a decimal lands on it exactly, and `x² − B² = 0` with B = 4 gives
  Left − Right of exactly 0. Once the steps fall below 10⁻¹⁴ the precision rule continues them in `double`, so a
  solution that is no decimal, such as √2, is as accurate as `double` - past the ten digits displayed.

**Spreadsheet and Table** (Phase 4).
- A constant is calculated once and keeps that value, as on the calculator; only a constant typed with 11 or more
  significant digits is converted, to the ten the calculator stores (p. 101).
- A formula is calculated through the same engine as any other input, so a cell holds a `Value` with its precision and
  its exact form: A1 = 1÷3 and B1 = A1×3 is exactly 1, not 0.9999999999.
- A range (`Sum`, `Mean`, `Min`, `Max`) adds and divides through `ValueMath`, and every cell of it counts against the
  budget: a range of a large sheet is a Time Out, never a hang.
- A number table steps in `decimal` and takes every x as start + row·step, so a step of 0.1 does not drift: the row
  after ten steps from 0 is exactly 1.

**Distributions** (Phase 4).
- A binomial probability of a decimal p = m/10^s is a fraction: Σ C(N, k)·m^k·(10^s − m)^(N−k)/10^(sN), summed on
  `BigInteger` and rounded once, so 0.8125 is exactly 13/16. Its size is about N·(s + 1) digits and the work grows as
  its square, counted against the budget before it starts: a binomial beyond the budget is Time Out (p. 165), never a
  rounded guess. N = 1,000 with p = 0.5 takes milliseconds; N = 10⁶ is a Time Out.
- The Poisson probability is Loader's saddle-point form (`PoissonDistribution` in Numerics), which avoids the
  cancellation of exp(x·ln λ − λ − ln x!): a relative error below 10⁻¹⁵·(1 + |ln P|) against 50-digit references.
  Poisson CD adds the terms from x towards the side where they vanish, stopping when a geometric bound on the rest is
  below 10⁻¹⁷ of the sum, and takes the complement above the mean; each term counts against the budget.
- Normal PD takes e^(−z²/2) with z²/2 in `decimal` split into its whole part and fraction, so that exp does not
  carry the rounding of a large exponent: e^(−453.005)/√(2π) is within 2×10⁻¹⁵ (Extended), where exp of 453.005
  rounded to double would be 5×10⁻¹⁴ off. Normal CD subtracts the two
  tails on the side where they are small, so that Φ(11) − Φ(10) = 7.6×10⁻²⁴ keeps its digits instead of becoming 0.
  Inverse Normal is μ + σ·(−√2·erfc⁻¹(2·Area)).
- The normal distribution is the complementary error function, which .NET lacks: `ErrorFunction` in Numerics. erf is
  its Maclaurin series below 1; erfc is the continued fraction of Γ(½, x²) above, evaluated backward (forward, Lentz's
  method was measured 6.5×10⁻¹⁵ off near 1), with e^(−x²) split so that x² is exact. Both agree with 50-digit
  PeterO.Numbers references within 2×10⁻¹⁵ relative wherever erfc is a normal `double` (ErrorFunctionTests).
- P(t) = erfc(−t/√2)/2, Q(t) = erf(|t|/√2)/2 and R(t) = erfc(t/√2)/2. The division by √2 is rounded, which moves the
  result by about 2x²·2⁻⁵³ relative with x = t/√2: 1.4×10⁻¹⁴ at t = −8, invisible at 10 digits.
- The inverse of erfc, for Inverse Normal, is Newton's method on ln erfc from x = √(−ln y), which decreases to the root
  without overshooting because ln erfc is concave.

**Spreadsheet.** Cells hold calculator values; recalculation runs in a deterministic topological order.

**Units.** Conversion factors are exact where the unit is defined exactly (decided 17 Sep 2026). `decimal` holds
every factor below exactly; only the 5/9 of °F → °C is a rounded division:

| Unit | Exact factor |
|---|---|
| inch | 2.54 cm |
| foot | 0.3048 m |
| pound | 0.45359237 kg |
| gallon (US) | 3.785411784 L |
| gallon (UK) | 4.54609 L |
| atmosphere | 101325 Pa |
| kgf | 9.80665 N |
| horsepower | 550 ft·lbf/s |
| mmHg (conventional) | 13.5951 × 9.80665 Pa |
| °F → °C | (F − 32) × 5/9 |

NIST SP 811 prints some of these rounded (hp, psi, mmHg, acre). Pallas uses the definitions. An empirically
defined unit (the 15 °C calorie) uses its tabulated value.

**Constants.**
- The newest CODATA recommended values (decided 17 Sep 2026; the reference calculator ships CODATA 2018).
- Constants exact in the 2019 SI (`c`, `h`, `e`, `k`, `N_A`, …) keep all their defined digits:
  - as `decimal` where they fit the precision rule;
  - as `double` otherwise. `h = 6.62607015×10⁻³⁴` would be 0 in `decimal`, and a `double` round-trips those
    9 digits.
- The others carry their standard uncertainty as data.
- π and e are `double.Pi` and `double.E`. There is no `decimal` π: an expression containing π is irrational, so its
  result is `double` and 15 digits anyway.

## 9. Equality and Verify

- **Exact values compare exactly.** Two `decimal` values that never went through `double` are equal only if they are
  the same number, so `1.0000000001 = 1` is False.
- **Exact forms compare symbolically.** `(√2)² = 2` is True because the form of the left side is the rational 2, not
  because of a tolerance (working assumption U6 in [CONFORMANCE.md](CONFORMANCE.md)).
- **Values that went through `double` are equal within 10⁻¹³ relative** (decision of 18 Sep 2026), which is where the
  engine also stops recognizing display forms. `sin(45)²+cos(45)²=1` is therefore True. The measurements behind the
  figure: `(decimal)double` is off by up to 5.07×10⁻¹⁵ relative (2 million samples, 18 Sep 2026), `System.Math` adds up
  to 5×10⁻¹⁵ (§6), and a comparison has two sides; 10⁻¹⁴ would already fail U6. It is still a thousand times finer than
  the ten digits displayed.
- **An inequality is strict about equality:** `<` is False where `=` is True. An inequality with a complex operand is a
  Math ERROR (manual p. 128).

## 10. Profiles

One engine, two presentations. A profile changes limits and formatting, never how values are computed.

| | `Standard` | `Extended` |
|---|---|---|
| Display | 10 digits, Norm 1/2, Fix 0-9, Sci 1-10 | Up to 15 significant digits |
| Limits | pp. 169-171: \|x\| < 10¹⁰⁰, `x! ≤ 69`, 4×4 matrices, 32-bit Base-N, A1:E45 | The range of `double` (±1.8×10³⁰⁸); `BigInteger` for integer functions |
| Application limits | 4×4 matrices, 2- and 3-dimensional vectors, 160/80/53 statistics rows, a Distribution list of 45, a sheet of A1:E45 and 1,700 bytes, tables of 45 or 30 rows | 64×64 matrices, 64-dimensional vectors, 10,000 statistics rows, a Distribution list of 10,000, a sheet of A1:E99 with no byte limit, tables of 10,000 rows |
| Equation and Inequality | 2 to 4 unknowns, degree 2 to 4 | The same: the calculator names four unknowns, and the exact factoring is designed for those degrees |
| Fraction, surd and π forms | The calculator's display bounds | Wider bounds |
| `2036162` → Prime Factor | `2 × (1018081)` | `2 × 1009²` |
| Used by | The conformance suite; users who want "exactly like the calculator" | Engineering and accounting work |

## 11. Budgets and honest failure

Every long operation takes a `CancellationToken` and an `EngineBudget { MaxIterations, Timeout }`.

- Exceeding the budget yields Time Out or Stack ERROR: a named outcome, never a hang, never a crash, never a silently
  rounded answer.
- Parser nesting depth is bounded. `StackOverflowException` cannot be caught in .NET and would kill the host
  process, so deep nesting is a Stack ERROR instead.

## 12. How accuracy is tested

`tests/Barbatos.Pallas.Numerics.Tests` uses oracles that share no code with `System.Math`:

- **PeterO.Numbers**, an arbitrary-precision decimal library, checks `System.Math` and `Trigonometry`
  (`MathAccuracyTests`). A `double` input enters as its exact binary value rounded to 40 significant digits, and the
  reference is computed at 40 digits. sin, cos and π come from series written in the test project.
- **`BuiltInBehaviorTests`** pins every measurement in §4 that the design depends on.
- **Property tests** (CsCheck), e.g. `n! = n × (n − 1)!` and prime factors multiplying back to the input.
- **The manual's printed values**, reproduced by .NET calls and the `"G10"` format: `3√2 = 4.242640687`,
  `99√999 = 3129.089165`, `sinh 1 = 1.175201194`, `ln 90 = 4.49980967`, `Pol(√2, √2) = (2, 45°)`, special angles,
  `2036162 = 2 × 1009²`.
- **Coverage**, identical on net8.0, net9.0 and net10.0: 177 of 179 lines and 55 of 56 branches. The rest is one
  defensive exit in `Fractions.TryFromDecimal` that 9.2 million searched inputs never reached, documented in the
  source.
- **The engine's own checks** (`tests/Barbatos.Pallas.Engine.Tests`): the precision rule value by value; the manual's
  worked examples through the conformance suite; integrals against 50-digit PeterO.Numbers references; and CsCheck
  properties over generated syntax trees, where evaluating any tree in any application gives a value or a
  `CalcError`, never an exception and never a run past its budget.
- **Mutation testing.** Stryker.NET gives a mutation score of 96.55% (17 Sep 2026; gate 90%, enforced in CI). The
  4 undetected mutants change no observable result:
  - `checked` removed, twice, where the continued fraction fails either way after an overflow;
  - the sign of a zero numerator;
  - the defensive exit above.

**Defects found in PeterO.Numbers 1.8.2** while building the oracle (17 Sep 2026); the tests avoid all three:

- `EDecimal.RoundToExponent` is wrong for `ERounding.Down`: it halved 11120217850809733414095251570457 rounded to
  exponent 1.
- `Sqrt`, `Log` and `Log10` are wrong by exact powers of 1000 for inputs with a long mantissa. The exact 94-digit
  expansion of the double 2.093×10⁻¹⁸ gave √ = 4.57×10⁻¹¹ instead of 1.4467×10⁻⁹. Inputs rounded to 20-60 digits
  agreed in 1,200 of 1,200 samples, hence the 40-digit input.
- `EDecimal.Remainder(divisor, null)` throws `NullReferenceException`. The reference reduces angles with
  `Divide` and `ToEInteger` instead.
