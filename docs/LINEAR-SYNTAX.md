# Canonical Linear Syntax

Canonical Linear Syntax (CLS) is the single text form of a Pallas expression, implemented by
`Barbatos.Pallas.Expressions` (Phase 2).

- It is the one road into the parser: the on-screen math editor serializes to it, and Web API calls, tests and saved
  history all use it.
- It is the output of `LinearPrinter`, which writes canonical spellings only.

It follows the reference calculator's LineI notation where one exists, so a calculator user recognizes it.

- [1. Design rules](#1-design-rules)
- [2. Tokens](#2-tokens)
- [3. Priority levels](#3-priority-levels)
- [4. Context: application and Verify](#4-context-application-and-verify)
- [5. Decisions where text cannot tell keys apart](#5-decisions-where-text-cannot-tell-keys-apart)
- [6. Errors](#6-errors)
- [7. Output notation used by expectations](#7-output-notation-used-by-expectations)

---

## 1. Design rules

1. **Plain Unicode text.** Input is normalized to Unicode form C before it is read, so `ȳ` typed as `y` + U+0304 is the
   same token. Spans in results index the normalized text.
2. **A token means one thing in a given application.** Base-N and Spreadsheet read some text differently (§4); that is
   inherited from the calculator.
3. **Round-trips with the math editor's templates.** Every template has exactly one CLS spelling.
4. **ASCII aliases for input only.** The printer always writes the canonical spelling.
5. **Longest match.** At each position the lexer takes the longest spelling available in the application: `÷R` over
   `÷`, `sinh(` over `sin(`, `@R_K-90` over `@R_K`. White space separates tokens and is otherwise ignored.
6. **Numbers stay text.** The parser keeps `0.1` as written; turning it into `decimal` or `double` is the engine's
   decision (PRECISION.md §3).

## 2. Tokens

| Construct | Canonical | Input aliases | Example |
|---|---|---|---|
| Number | `123`, `1.25`, `.5`, `3.` | - | `3.25` |
| Recurring decimal | `digits.digits(digits)` - the recurring part follows the decimal point directly | - | `3.(021)+0.(312)`, `0.1(6)` |
| Fraction, mixed fraction | `a⌟b`, `a⌟b⌟c` | - | `2⌟3+1⌟1⌟2` |
| Arithmetic | `+ - × ÷ ÷R` | `−` (U+2212) for `-`, `*` for `×`, `/` for `÷` | `7×8-4×5`, `5÷R2` |
| Power, root | `^`, `²`, `³`, `⁻¹`, `√(x)`, `nˣ√(x)` | `sqrt(x)`, `root(n,x)` | `5ˣ√(32)` |
| Postfix | `!`, `%` | - | `(5+3)!`, `150×20%` |
| Angle units | `°`, `ʳ`, `ᵍ` | - | `sin(30°)`, `(π÷2)ʳ` |
| Degrees-minutes-seconds | `d°m′s″` - minutes and seconds are numbers | `'` for `′`, `"` for `″` | `2°20′30″` |
| Constants | `π`, `e`, `Ran#`; `i` in Complex | `pi` | `π÷6` |
| Scientific constants (47) | `@` + the CATALOG symbol, subscripts after `_` | Greek letters in ASCII (`@epsilon_0`, `@hbar`, `@R_inf`); subscript digits (`@ε₀`) | `@h`, `@N_A`, `@ε_0`, `@R_K-90` |
| Memory, variables | `Ans`, `PreAns`, `A`-`F`, `x`, `y`, `z` | - | `Ans+PreAns`, `3A+B` |
| Functions | name and `(`: `sin(`, `sin⁻¹(`, `log(`, `ln(`, `Abs(`, `Int(`, `Intg(`, `Rnd(`, `GCD(`, `LCM(`, `Pol(`, `Rec(`, `RanInt#(`, `AtWt(`, `f(`, `g(` … | `asin(`, `acos(`, `atan(`, `asinh(`, `acosh(`, `atanh(` | `log(2,16)` |
| Calculus | `d/dx(f,a)`, `∫(f,a,b)`, `Σ(f,a,b)`, `Π(f,a,b)` | `diff(`, `integral(`, `sum(`, `product(` | `Σ(x+1,1,5)` |
| Permutation, combination | `nPr`, `nCr` written `10P4`, `10C4` | - | `10C4` |
| Engineering symbols (11) | `_m _μ _n _p _f _k _M _G _T _P _E` | `_u` and `_µ` (U+00B5) for `_μ` | `999_k+25_k` |
| Unit conversions (40) | value followed by `from▶to`, as in the CATALOG | `->` for `▶`, `2` for `²`, `15` for `₁₅`, `.` for `·` | `5cm▶in`, `5n mile▶m`, `2kgf·m▶J` |
| Complex | `i`, `∠`, `Conjg(`, `Arg(`, `ReP(`, `ImP(` | - | `2∠45` |
| Base-N | prefixes `d h b o`; `and`, `or`, `xor`, `xnor`, `Not(`, `Neg(` | - | `d10+h1F`, `1010 and 1100` |
| Matrix, vector | `MatA`-`MatD`, `MatAns`, `Det(`, `Trn(`, `Identity(`; `VctA`-`VctD`, `VctAns`, `•`, `Angle(`, `UnitV(` | `.` is **not** an alias of `•` | `VctA•VctB` |
| Statistics | `x̄ ȳ σ²x σx s²x sx n Σx Σx² Σxy … min(x) max(x) Q1 Med Q3 a b c r`; postfix `x̂ ŷ x̂₁ x̂₂ ▶t`; `P( Q( R(` | `->t` for `▶t` | `5.5ŷ`, `P(Ans)` |
| Spreadsheet | cells `A1`, `$A1`, `A$1`, `$A$1` (columns A-E, rows up to two digits); ranges `A1:B5`; `Min( Max( Mean( Sum(` | - | `Sum(A1:A5)` |
| Relations (Verify) | `= ≠ < > ≤ ≥` | `<=`, `>=`. **No** `!=`: `3!=6` is a factorial compared with 6 | `1≤1<1+1` |

The complete list, with the applications each spelling is available in, is `SyntaxVocabulary.Standard`. A plugin
adds names through `SyntaxVocabulary.With`.

**Why engineering symbols take an underscore.** On the calculator they are separate tokens entered from a menu.
Written bare, `E` (exa) would collide with the variable E and `P` (peta) with the permutation `P`.

**Why scientific constants take `@`.** The elementary charge `e` would collide with Euler's number `e`, and `h` with
the hexadecimal prefix. `@` marks the CATALOG list the name comes from.

**Parentheses.** A closing parenthesis may be omitted at the end of the text (manual p. 28): `sin(30` reads as
`sin(30)`. The printer always closes them.

## 3. Priority levels

The reference calculator's priority (manual p. 168), from the tightest. Operators of one level are evaluated left to right.

| Level | Operators | Example |
|---|---|---|
| 1 | parentheses | |
| 2 | functions with parentheses | `sin(30)` |
| 3 | postfix `² ³ ⁻¹ ! ° ʳ ᵍ % ▶t`, engineering symbols, `^`, `ˣ√` | `-2²` is `-(2²)`; `2^3^2` is `(2^3)^2` |
| 4 | fractions `⌟` | `2⌟3²` is `2⌟(3²)` |
| 5 | negative sign, Base-N prefixes | `-2⌟3` is `-(2⌟3)` |
| 6 | unit conversions, `x̂ ŷ x̂₁ x̂₂` | `2πcm▶in` is `2(πcm▶in)` |
| 7 | multiplication with the sign omitted | `6÷2(1+2)` is `6÷(2(1+2))`; `1⌟6π` is `(1⌟6)π` |
| 8 | `P`, `C`, `∠` | `2×10P4` is `2×(10P4)` |
| 9 | `•` | |
| 10 | `× ÷ ÷R` | |
| 11 | `+ -` | |
| 12 | `and` | |
| 13 | `or xor xnor` | |
| - | Verify relations `= ≠ < > ≤ ≥`, loosest | `2²=2+2=4` |

Consequences:

- **Level 3 left to right** makes `2^3^2` equal to 64 and `2^3²` equal to `(2^3)²`. This follows the manual's
  general rule; a physical calculator was not available to confirm it (assumption U2).
- **Implicit multiplication has its own level**, so the automatic parentheses of manual p. 29 need no special case.
- **Relation chains.** Every inequality in a chain points the same way (`5≤6≥4` is a Syntax ERROR), and `≠` does not
  combine with `< > ≤ ≥` (`4<6≠8` is a Syntax ERROR); `=` combines with either. The manual lists the forbidden
  combination in an image, so the second rule is assumption U10.

## 4. Context: application and Verify

Parsing takes a `SyntaxContext`: the application, and whether relations are allowed (Verify on).

| Application | What changes |
|---|---|
| Base-N | A run of `0`-`9` and `A`-`F` is a number, unless a longer name starts there (`Ans`). `d h b o` directly before such a digit is a prefix. No decimal point. `and or xor xnor Not( Neg(` are available. The variables A-F cannot be typed, and neither can the combination operator `C`. |
| Spreadsheet | `A1`-style cell references win over the variables A-E; `:` forms ranges. |
| Complex | `i`, `∠` and the complex functions are available. |
| Statistics | Statistic variables, estimates, `▶t` and `P( Q( R(` are available. `P(` is then the distribution function, so a permutation with a parenthesized right operand is written `10P (4)`. |
| Matrix, Vector | Matrix and vector names and functions; `•` in Vector. |

Symbols not available in the context are not recognized: `i` in Calculate is an unexpected character.

## 5. Decisions where text cannot tell keys apart

On the calculator these are different keys; in text they share spellings. Decided 17 Sep 2026.

| # | Question | Rule |
|---|---|---|
| 1 | `^` associativity | Left to right, as level 3 (§3). |
| 2 | Letter `C`: variable or combination? | `C` between two operands is the combination operator (`10C4`, `ACB`); elsewhere it is the variable C (`2C`, `2C+1`). To multiply by the variable, write `2×C×3`. |
| 3 | `°`: angle unit or degrees-minutes-seconds? | `°` followed by a number and `′` or `″` is sexagesimal (`2°20′30″`, `2°30″`); `°` alone is the degree unit (`sin(30°)`). In Radian mode the two differ: `30°` is 30 degrees, `30°0′0″` is the number 30. The printer writes all three parts. |
| 4 | Scientific constants | Prefixed with `@` (§2). |
| 5 | Verify chains | §3. |

## 6. Errors

Parsing never throws for bad input. It stops at the first error and reports a `SyntaxErrorCode` with the span where
the calculator would place the cursor (manual p. 162).

| Code | Example | Calculator |
|---|---|---|
| `EmptyExpression` | `` | Syntax ERROR |
| `UnexpectedCharacter` | `2q`, `sin` without `(` | Syntax ERROR |
| `MissingOperand` | `2+`, `×3`, `sin()` | Syntax ERROR |
| `UnexpectedToken` | `2,3` | Syntax ERROR |
| `UnmatchedClosingParenthesis` | `2)` | Syntax ERROR |
| `AdjacentNumbers` | `2 3`, `1.2.3` | Syntax ERROR |
| `RelationNotAllowed` | `1=1` with Verify off | Syntax ERROR |
| `MixedRelationDirections` | `5≤6≥4` | Syntax ERROR |
| `NotEqualWithInequality` | `4<6≠8` | Syntax ERROR |
| `TooManyFractionParts` | `1⌟2⌟3⌟4` | Syntax ERROR |
| `MisplacedSexagesimalMark` | `20′` | Syntax ERROR |
| `InvalidRootArguments` | `root(2)` | Syntax ERROR |
| `NestingTooDeep` | more than 128 nested parentheses, functions or signs | Stack ERROR |

The manual gives no stack size; the fixed limit of 128 is assumption U11.

## 7. Output notation used by expectations

| Result | Form | Example |
|---|---|---|
| Exponent | `m×10^e` | `1.67×10^-1`, `1.234567892×10^10` |
| π and surd forms | Coefficient first, as the calculator shows them | `1⌟6π`, `45√(3)+10√(2)`, `-3⌟4+√(23)⌟4i` |
| Polar complex | `r∠θ` | `2∠45` |
| Inequality solutions | Comma-separated intervals | `x≤-3, 1≤x` |
