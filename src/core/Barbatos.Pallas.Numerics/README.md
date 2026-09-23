# Barbatos.Pallas.Numerics

The calculator math .NET does not already provide: trigonometry in degrees, radians and gradians built on System.Double and System.Math, exact integer functions on BigInteger (factorial, permutations, combinations, LCM, prime factors), fraction recognition, degrees-minutes-seconds, the error function erf, erfc and its inverse, and the Poisson probability.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.
>
> **Not a package of its own.** It ships inside [Barbatos.Pallas.Engine](https://www.nuget.org/packages/Barbatos.Pallas.Engine),
> which carries its assembly: reference that package to use it.

## Use .NET first

This library deliberately contains nothing .NET already has. Use these directly:

| Need | .NET |
|---|---|
| π, e | `double.Pi`, `double.E` (or `Math.PI`, `Math.E`) |
| exp, ln, log, √, powers, hyperbolic functions | `Math.Exp`, `Math.Log`, `Math.Log10`, `Math.Log(value, newBase)`, `Math.Sqrt`, `Math.Pow`, `Math.Sinh`, … |
| Real root of a negative number | `double.RootN(-32, 5)` is −2 (`Math.Pow(-32, 0.2)` is NaN) |
| 10ˣ, hypotenuse | `double.Exp10`, `double.Hypot` |
| Degrees ↔ radians | `double.DegreesToRadians`, `double.RadiansToDegrees` |
| GCD | `BigInteger.GreatestCommonDivisor` |
| Decimal arithmetic and rounding | `decimal` operators, `Math.Round(decimal, int, MidpointRounding)` |
| 10 significant digits for display | `value.ToString("G10", CultureInfo.InvariantCulture)` |

## What this library adds

```csharp
using System.Numerics;
using Barbatos.Pallas.Numerics;

// Trigonometry in calculator angle units, exact at multiples of a right angle.
Trigonometry.Cos(90, AngleUnit.Degree);                   // 0 (Math.Cos(Math.PI / 2) is 6.1E-17)
Trigonometry.Tan(90, AngleUnit.Degree);                   // +∞, the calculator's Math ERROR
Trigonometry.Sin(100, AngleUnit.Gradian);                 // 1
(decimal)Trigonometry.Sin(30, AngleUnit.Degree);          // 0.5
(decimal)Trigonometry.Asin(0.5, AngleUnit.Degree);        // 30
Trigonometry.Atan2(1, 1, AngleUnit.Degree);               // 45, in (-180, 180]
Trigonometry.ConvertAngle(Math.PI / 2, AngleUnit.Radian, AngleUnit.Degree); // 90

// Exact integer functions.
BigInteger factorial = IntegerFunctions.Factorial(69);    // all 99 digits
IntegerFunctions.Combinations(10, 4);                     // 210
IntegerFunctions.LeastCommonMultiple(9, 15);              // 45
IntegerFunctions.PrimeFactors(2036162);                   // [(2, 1), (1009, 2)]

// Recognize the fraction a decimal result stands for.
Fractions.TryFromDecimal((2m / 3m) + 1.5m, 9_999_999_999, 0.0000000000000000000000001m,
    out long numerator, out long denominator);            // true, 13/6

// Degrees, minutes and seconds.
decimal angle = Sexagesimal.ToDegrees(2, 20, 30) + Sexagesimal.ToDegrees(0, 9, 30);      // == 2.5m
(bool negative, decimal degrees, decimal minutes, decimal seconds) =
    Sexagesimal.FromDegrees(angle, 0, MidpointRounding.AwayFromZero);                    // false, 2, 30, 0

// The error function, for the normal distribution: Φ(t) = erfc(−t/√2)/2.
ErrorFunction.Erf(1);                                     // 0.8427007929497149
ErrorFunction.Erfc(10);                                   // 2.0884875837625446E-45, no cancellation
ErrorFunction.InverseErfc(0.5);                           // 0.47693627620446993

// The Poisson probability e^(−λ)·λ^x/x!, without the cancellation of exp(x·ln λ − λ − ln x!).
PoissonDistribution.Probability(2, 1);                    // 0.18393972058572122, e⁻¹/2
PoissonDistribution.Probability(1_000_000, 1_000_000);    // 0.00039894224715624404
```

`Trigonometry` returns what .NET returns for invalid input: NaN for asin 2 or an infinite angle, ±∞ for tan 90°. A
caller that needs errors checks `double.IsFinite` once.

## Accuracy

- sin, cos and tan agree with 40-digit references within 5×10⁻¹⁵ (relative), including degree and gradian angles up
  to ±10⁶, because whole turns are removed exactly with `double.Ieee754Remainder` before `double.SinPi` is called.
- erf and erfc agree with 50-digit references within 2×10⁻¹⁵ (relative) wherever erfc is a normal `double`, down to
  erfc(26.5) = 4×10⁻³⁰⁷; the inverse of erfc is accurate to what erfc's own accuracy allows.
- The Poisson probability P agrees with 50-digit references within 10⁻¹⁵·(1 + |ln P|) relative: 3×10⁻¹⁵ for P = 0.1,
  and more in the far tail, where e's exponent carries its own rounding.
- Integer functions are exact at any size.

The full contract, with measurements, is
[docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md).

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
