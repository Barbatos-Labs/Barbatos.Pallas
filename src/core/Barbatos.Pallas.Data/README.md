# Barbatos.Pallas.Data

Reference data for Barbatos.Pallas: CODATA physical constants, NIST SP 811 unit conversions with exact factors, and CIAAW standard atomic weights.

> **Status: 1.0.** The API below is tested on .NET 8, 9 and 10, and it follows semantic versioning: an incompatible
> change waits for 2.0.
>
> **Not a package of its own.** It ships inside
> [Barbatos.Pallas.DependencyInjection](https://www.nuget.org/packages/Barbatos.Pallas.DependencyInjection), which
> carries its assembly: reference that package to use it.

## Adding the data to an engine

```csharp
using Barbatos.Pallas.Data;
using Barbatos.Pallas.Engine;

PallasEngine engine = PallasEngineBuilder.CreateDefault()
    .AddConstantSet(ConstantSets.Codata2022)
    .AddUnitSet(UnitSets.NistSp811)
    .AddAtomicWeights(AtomicWeightTables.Ciaaw)
    .Build();

CalculatorSession session = engine.CreateSession();
session.Calculate("@N_A×@k").Display.Text;    // "8.314462618", the molar gas constant
session.Calculate("AtWt(21)").Display.Text;   // "44.955907", scandium
session.Calculate("5cm▶in").Result;           // exactly 5 ÷ 2.54
```

`AddPallas()` from `Barbatos.Pallas.DependencyInjection` adds all three by default.

## What is in the box

| Set | Contents | Source |
|---|---|---|
| `ConstantSets.Codata2022` | The 47 scientific constants of the reference calculator's CATALOG, with their units and standard uncertainties | [2022 CODATA adjustment](https://physics.nist.gov/cuu/Constants/Table/allascii.txt), retrieved 18 Sep 2026 |
| `UnitSets.NistSp811` | The 40 unit conversion commands, as exact definitions | NIST Special Publication 811 |
| `AtomicWeightTables.Ciaaw` | The 118 elements | [CIAAW](https://ciaaw.org/atomic-weights.htm), retrieved 18 Sep 2026 |

## No floating point

A value is a `ScaledDecimal`: a `decimal` mantissa and a power of ten. The Planck constant is
`new(6.62607015m, -34)`, which keeps every published digit where `decimal` alone would be 0 and `double` is not
allowed in the data library
([docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md) §7). The engine
turns it into a value with the precision rule.

A unit conversion carries a multiplier and a divisor rather than one factor, so exact definitions stay exact:
`cm▶in` divides by 2.54, and `5cm▶in` can therefore be displayed as `250⌟127` as well as 1.968503937.
`°F▶°C` also carries the offset: `(value − 32) × 5 ÷ 9`.

Two factors are not exact decimals and carry the digits of their definitions: the parsec, defined through π, and the
15 °C calorie.

## Differences from the calculator

The reference calculator ships CODATA 2018 and the IUPAC 2019 atomic weights; Pallas ships the newest values (deviations D2 and
D3 of
[docs/CONFORMANCE.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/CONFORMANCE.md)). An element
whose standard atomic weight is an interval, such as hydrogen, carries its abridged value; an element without one
carries the mass number of its longest-lived isotope.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
