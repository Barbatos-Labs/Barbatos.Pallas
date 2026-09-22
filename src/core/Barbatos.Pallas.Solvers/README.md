# Barbatos.Pallas.Solvers

Polynomial algebra for Barbatos.Pallas: exact integer polynomials, with the sign at a rational point, the number of real
roots by Sturm, division by a rational root, and every complex root by the method of Aberth.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.

## Exact where it can be, iterated where it cannot

A polynomial of a calculator has decimal coefficients, so multiplying it by a power of ten makes every coefficient an
integer and leaves the roots where they are. `IntegerPolynomial` works on those integers, and everything it answers is
exact: the sign of the polynomial at a rational point, how many distinct real roots it has, what it looks like without
its repeated factors, and the quotient by a rational root.

Roots of degree 3 and up have no closed form a calculator can display, so `PolynomialRoots.Find` iterates them in
`double` and `System.Numerics.Complex`, all at once, by Aberth's method: every root is refined by a Newton step
corrected for the pull of the other roots, so no root carries the error of the ones found before it. A root that is
rational is then recovered exactly — the continued fraction of the approximation proposes it, and the integer
polynomial confirms it — and divided out, which is what lets a cubic keep the exact roots of the quadratic it leaves
behind.

```csharp
using System.Numerics;
using Barbatos.Pallas.Solvers;

// x³ − 2x² − 5x + 6 = (x − 1)(x + 2)(x − 3), ascending: coefficients[i] multiplies x to the power i.
BigInteger[] cubic = [6, -5, -2, 1];

IntegerPolynomial.CountRealRoots(cubic);          // 3
IntegerPolynomial.SignAt(cubic, 1, 2);            // 1: the polynomial is positive at ½
IntegerPolynomial.Deflate(cubic, 1, 1);           // [-6, -1, 1]: x² − x − 6, after dividing by (x − 1)

Complex[] roots = PolynomialRoots.Find([6d, -5d, -2d, 1d]);
PolynomialRoots.TryGetRational(roots[0].Real, cubic, out BigInteger p, out BigInteger q);
// true, and p/q is one of 1, −2 and 3

IntegerPolynomial.SquareFree([4, 0, -4, 0, 1]);   // [-2, 0, 1]: (x² − 2)² without its repeated factor
```

The Equation and Inequality applications of the calculator — the display forms of the roots, the local extrema, the
Newton solver and the intervals of an inequality — are in Barbatos.Pallas.Engine (`CalculatorSession.SolvePolynomial`,
`SolveSimultaneous`, `SolveEquation`, `SolveInequality`).

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger. The precision guarantees every package upholds are
described in [docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).
