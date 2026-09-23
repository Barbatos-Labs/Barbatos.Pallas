# Barbatos.Pallas.DependencyInjection

Microsoft.Extensions.DependencyInjection integration for Barbatos.Pallas: AddPallas(), validated options, and registration of function, constant and unit modules.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.

## Installing

```bash
dotnet add package Barbatos.Pallas.DependencyInjection --prerelease
```

It depends on [Barbatos.Pallas.Engine](https://www.nuget.org/packages/Barbatos.Pallas.Engine),
`Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Options`, and it carries two
libraries of its own, each an assembly and a namespace whose public types can be used directly:
`Barbatos.Pallas.Data` (the reference data below) and `Barbatos.Pallas.Spreadsheet` (the Spreadsheet and Table
applications). A program without a container can take this package for the data alone and add it to
`PallasEngineBuilder` itself.

## One call

```csharp
using Barbatos.Pallas.DependencyInjection;
using Barbatos.Pallas.Engine;

services.AddPallas();

// Later, from the provider:
CalculatorSession session = provider.GetRequiredService<CalculatorSession>();
session.Calculate("2⌟3+1⌟1⌟2").Display.Text;   // "13⌟6"
```

- `PallasEngine` is registered as a **singleton**: it is immutable and safe to share.
- `CalculatorSession` is registered as a **transient**: each one has its own memory, history and settings.
- The reference data of `Barbatos.Pallas.Data` (CODATA constants, NIST SP 811 units, CIAAW atomic weights) is
  included by default.

## Options and modules

```csharp
services.AddPallas(options =>
        {
            options.Profile = CalculatorProfile.Extended;
            options.App = CalculatorApp.Complex;
            options.Budget = new EngineBudget(MaxIterations: 10_000_000, Timeout: TimeSpan.FromSeconds(5));
            options.IncludeReferenceData = true;
        })
        .AddFunction<BeamDeflection>()          // no reflection: a parameterless constructor
        .AddConstantSet(myConstants)            // a later set replaces constants with the same symbol
        .AddUnitSet(myUnits);
```

Options are validated when the engine is first resolved: a budget that is not positive, or a profile or application
that is not a defined value, throws there rather than producing a silently odd calculator.

`AddPallas` may be called more than once, for example by a library and by the application that hosts it. Every call
returns the same builder and the options of every call apply in turn, so functions and data sets registered through
any of the calls reach the one engine.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
