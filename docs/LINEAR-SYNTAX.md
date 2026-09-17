# Canonical Linear Syntax (draft)

> **Status: draft.** The grammar is finalized and implemented in Phase 2 (Barbatos.Pallas.Expressions). This draft
> fixes the notation the conformance data already uses, so the parser is written against real cases.

Canonical Linear Syntax (CLS) is the single text form of a Pallas expression. It is:

- the one road into the parser: the on-screen math editor serializes to it, and Web API calls, tests and saved
  history all use it;
- the canonical output of the linear printer.

It follows the reference calculator's LineI notation where one exists, so a calculator user recognizes it.

## Design rules

1. **Plain Unicode text, unambiguous without context.** Every token means one thing regardless of the application.
2. **Round-trips with the math editor's templates.** Every template has exactly one CLS spelling.
3. **ASCII aliases for input only.** The printer always emits the canonical spelling.
4. **The minus sign is ASCII `-`**, both binary and unary. A typographic minus (U+2212) is accepted on input;
   presentation layers may render it.

## Tokens

| Construct | Canonical | Input aliases | Example |
|---|---|---|---|
| Integer, decimal | `123`, `1.25` | - | `3.25` |
| Recurring decimal | `0.(312)` | - | `3.(021)+0.(312)` |
| Fraction, mixed fraction | `a⌟b`, `a⌟b⌟c` | - | `2⌟3+1⌟1⌟2` |
| Arithmetic | `+ - × ÷` | `* /` | `7×8-4×5` |
| Power, root | `^`, `²`, `³`, `⁻¹`, `√(x)`, `nˣ√(x)` | `sqrt(x)`, `root(n,x)` | `5ˣ√(32)` |
| Postfix | `!`, `%` | - | `(5+3)!`, `150×20%` |
| Angles | `°`, `ʳ`, `ᵍ`, `d°m′s″` | `'` for `′`, `"` for `″` | `2°20′30″`, `(π÷2)ʳ` |
| Constants | `π`, `e`, `i` | `pi` | `π÷6` |
| Memory | `Ans`, `PreAns`, `A`-`F`, `x`, `y`, `z` | - | `Ans+PreAns` |
| Calculus | `d/dx(f,a)`, `∫(f,a,b)`, `Σ(f,a,b)`, `Π(f,a,b)` | `diff`, `integral`, `sum`, `product` | `Σ(x+1,1,5)` |
| Remainder division | `÷R` | - | `5÷R2` |
| Permutation, combination | `nPr`, `nCr` written `10P4`, `10C4` | - | `10C4` |
| Engineering symbol | `_k`, `_M`, `_m`, `_μ` … | `_u` for `_μ` | `999_k+25_k` |
| Unit conversion | `value unit▶unit` | `->` for `▶` | `5cm▶in` |
| Complex | `i`, `∠` | - | `2∠45` |
| Base-N prefixes, logic | `d`, `h`, `b`, `o`; `and`, `or`, `xor`, `xnor`, `Not(`, `Neg(` | - | `d10+h10+b10+o10` |
| Matrix, vector | `MatA`-`MatD`, `MatAns`, `VctA`-`VctD`, `VctAns`, `•` | `.` is **not** an alias | `VctA•VctB` |
| Relations (Verify) | `= ≠ < > ≤ ≥` | `!=`, `<=`, `>=` | `1≤1<1+1` |

**Why engineering symbols take an underscore.** On the calculator they are separate tokens entered from a menu.
Written bare, `E` (exa) would collide with the variable E and `P` (peta) with the permutation `P`; the underscore
makes the symbol unambiguous in plain text.

## Output notation used by expectations

| Result | Form | Example |
|---|---|---|
| Exponent | `m×10^e` | `1.67×10^-1`, `1.234567892×10^10` |
| π and surd forms | Coefficient first, as the calculator shows them | `1⌟6π`, `45√(3)+10√(2)`, `-3⌟4+√(23)⌟4i` |
| Polar complex | `r∠θ` | `2∠45` |
| Inequality solutions | Comma-separated intervals | `x≤-3, 1≤x` |

## Open points for Phase 2

- **Precedence of fraction versus implicit multiplication in printed output.** The calculator reads `1⌟6π` as
  `(1/6)π` (fractions bind at level 4, implicit multiplication at level 7). The printer relies on that; the parser
  must honor it.
- **Associativity of `^`** (assumption U2 in [CONFORMANCE.md](CONFORMANCE.md)).
- **Tokenizing Base-N input.** `b10` is the binary prefix, while `bx` in Calculate is implicit multiplication. The
  tokenizer takes the active application into account; this is the one deliberate exception to rule 1, inherited
  from the calculator.
