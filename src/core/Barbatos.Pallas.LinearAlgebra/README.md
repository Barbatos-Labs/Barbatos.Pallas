# Barbatos.Pallas.LinearAlgebra

Exact linear algebra of `decimal` matrices for Barbatos.Pallas: determinants, inverses and solutions of linear systems,
by fraction-free elimination on `BigInteger`.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.
>
> **Not a package of its own.** It ships inside [Barbatos.Pallas.Engine](https://www.nuget.org/packages/Barbatos.Pallas.Engine),
> which carries its assembly: reference that package to use it.

## Why exact

Elimination in `decimal` rounds at every division. A singular matrix such as `[[0.1, 0.2], [0.3, 0.6]]` then gets a
determinant of about 10⁻²⁸ instead of 0, and no threshold can tell that from a small nonzero determinant. A `decimal`
is an integer over a power of ten, so each row is scaled to integers and eliminated without fractions (Bareiss): every
intermediate division is exact, and a determinant is 0 exactly when the matrix is singular.

Results come back as numerators over a common denominator, and the caller rounds once. There is no rational number
type.

```csharp
using System.Numerics;
using Barbatos.Pallas.LinearAlgebra;

(BigInteger numerator, BigInteger denominator) = ExactLinearAlgebra.Determinant(new decimal[,] { { 0.5m, 0.25m }, { 1, 3 } });
// 5 / 4

ExactLinearAlgebra.TryInvert(new decimal[,] { { 1, 2 }, { 3, 4 } }, out BigInteger[,] inverse, out BigInteger common);
// inverse = [[-4, 2], [3, -1]], common = 2: the inverse is [[-2, 1], [1.5, -0.5]]

ExactLinearAlgebra.TrySolve(new decimal[,] { { 1, -1, 1 }, { 1, 1, -1 }, { -1, 1, 1 } }, [2, 0, 4], out BigInteger[] x, out BigInteger d);
// x = [1, 2, 3], d = 1

ExactLinearAlgebra.TryInvert(new decimal[,] { { 0.1m, 0.2m }, { 0.3m, 0.6m } }, out _, out _);
// false: singular, exactly
```

The matrix and vector values of the calculator, with entries that follow the precision rule, are in
Barbatos.Pallas.Engine (`MatrixValue`, `VectorValue`).

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger. The precision guarantees every library upholds are
described in [docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).
