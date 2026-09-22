# Barbatos.Pallas.Statistics

Exact statistics of decimal data for Barbatos.Pallas: sums, means, variances, linear and quadratic least-squares fits
and the correlation coefficient on `BigInteger`, and the ranks of the quartiles.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.

## Why exact

The one-pass formulas statistics are usually written with cancel. Sxx = Σx² − (Σx)²/n of equal 20-digit decimals comes
out as about 10⁻²⁷ in `decimal` instead of 0, and the slope of a vertical line becomes a huge number instead of an error.
A `decimal` is an integer over a power of ten, so each column is scaled to integers once and every sum is exact: Sxx is
0 exactly when the x values are all equal, and each result is a fraction the caller rounds once. There is no rational
number type; a result is its numerator and a positive denominator, in lowest terms.

```csharp
using System.Numerics;
using Barbatos.Pallas.Statistics;

// x = 1.0, 1.2, …, 3.0 and y = 1.0, 1.1, …, 2.0 (ten pairs).
ExactSample sample = ExactSample.FromDecimals(x, y, frequencies: []);

sample.Sum(1, 1);                                         // Σxy = (774, 25), that is 30.96
sample.TryGetLinearFit(
    out (BigInteger Numerator, BigInteger Denominator) a,
    out (BigInteger Numerator, BigInteger Denominator) b); // a = 10009/19845, b = 1906/3969
sample.TryGetCorrelationSquared(
    out (BigInteger Numerator, BigInteger Denominator) r2,
    out int sign);                                        // r² = 908209/916839, sign 1

ExactSample.FromDecimals([1, 1], [1, 2], []).TryGetLinearFit(out _, out _);   // false: every x is equal

// Frequencies weight the rows; a value can also be given as an integer over a power of ten.
ExactSample weighted = ExactSample.FromScaledIntegers([15, 25], 1, [3, 5], 0, [5, 15], 1);   // x = 1.5, 2.5; f = 0.5, 1.5

// Where the quartiles of 20 sorted values are: Q1 is the mean of the 5th and 6th.
Quartiles.Ranks(20, Quartile.First);                      // (5, 6)
```

The quartile rule - the medians of the lower and upper halves, leaving the median out when the count is odd - is the
one the reference calculator's results agree with (docs/CONFORMANCE.md, assumption U1).

The Statistics application of the calculator, with data that may hold values beyond `decimal`, regressions on ln x, ln y
and 1/x and the normal distribution, is in Barbatos.Pallas.Engine (`StatisticsData`, `RegressionModel`).

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger. The precision guarantees every package upholds are
described in [docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).
