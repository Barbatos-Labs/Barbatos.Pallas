# Barbatos.Pallas.Engine

The Barbatos.Pallas calculation engine: binding, a function and constant plugin registry, compiled evaluation, calculator memory and sessions, result formatting, calculus, Verify and Base-N.

> **Status: preview.** The API below is tested on .NET 8, 9 and 10 but may still change before the first release.

## One calculation

```csharp
using Barbatos.Pallas.Engine;

PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
CalculatorSession session = engine.CreateSession();          // Calculate, Standard profile

session.Calculate("2⌟3+1⌟1⌟2").Display.Text;                 // "13⌟6"
session.Calculate("√(2)×3").Display.Text;                    // "3√(2)"
session.Calculate("10√(2)+15×3√(3)").Display.Text;           // "45√(3)+10√(2)"
session.Calculate("d/dx(x³,0.1)").Result.ToDecimal();        // exactly 0.03
session.Calculate("0.1+0.2").Result.ToDecimal();             // exactly 0.3
```

Input is [Canonical Linear Syntax](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/LINEAR-SYNTAX.md),
the one text form of an expression. A calculation answers with a value or a `CalcError`: the kind the calculator
shows and the span it would place the cursor at. Nothing throws for bad input.

```csharp
Calculation calculation = session.Calculate("14÷0×2");
calculation.Succeeded;              // false
calculation.Error!.Value.Kind;      // CalcErrorKind.MathError
calculation.Error!.Value.Span;      // (0, 4), the division
```

## Values

A `Value` holds a real number as `decimal` or `double`, a complex number, or a 32-bit Base-N integer, following the
precision rule of
[docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md) §3: decimal input
stays `decimal`, and a `double` result becomes a `decimal` of 15 significant digits unless its magnitude is below
10⁻¹⁴ or beyond 7.9×10²⁸.

A value computed exactly from square roots and π also remembers its exact form, so `√(2)×√(2)` is exactly 2 and
`π÷2` reaches the trigonometric functions as exactly `double.Pi / 2`.

## Settings, profiles and formatting

```csharp
session.Settings = session.Settings with
{
    AngleUnit = AngleUnit.Radian,
    NumberFormat = NumberFormat.Fix(3),
    InputOutput = InputOutput.MathIDecimalO,
};

Calculation result = session.Calculate("π÷6");
session.Format(result, FormatTarget.DecimalValue)!.Text;     // "0.524"
session.Format(result, FormatTarget.Standard)!.Text;         // "1⌟6π"
result.Display.Latex;                                        // LaTeX for copy and export
```

`CalculatorProfile.Standard` keeps the calculator's calculation range, function domains and display bounds;
`CalculatorProfile.Extended` reaches the range of `double` and shows wider forms.

## Memory, Verify and Base-N

```csharp
session.Calculate("3+5");
session.Store(MemoryVariable.A);                             // STO A
session.Calculate("A×10").Display.Text;                      // "80"
session.Define(DefinedFunction.F, "x²+1");
session.Calculate("f(5)").Display.Text;                      // "26"

session.Settings = session.Settings with { Verify = true };
session.Calculate("2²=2+2=4").IsTrue;                        // true

session.SwitchApp(CalculatorApp.BaseN);
session.Settings = session.Settings with { BaseMode = NumberBase.Bin };
session.Calculate("Not(1010)").Display.Text;                 // "11111111111111111111111111110101"
```

## Plugins and data

Scientific constants, unit conversions and atomic weights come from data sets, such as those of
`Barbatos.Pallas.Data`. A plugin function joins the vocabulary and is evaluated like a built-in one.

```csharp
public sealed class BeamDeflection : IMathFunction
{
    public FunctionSignature Signature { get; } = new("beam(", 2, 2);

    public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
    {
        return arguments[0].ToDouble() < 0d
            ? EvalResult.Failure(CalcErrorKind.MathError)
            : Value.FromDecimal(arguments[0].ToDecimal() * arguments[1].ToDecimal());
    }
}

PallasEngine engine = PallasEngineBuilder.CreateDefault()
    .AddConstantSet(ConstantSets.Codata2022)      // Barbatos.Pallas.Data
    .AddUnitSet(UnitSets.NistSp811)
    .AddFunction(new BeamDeflection())
    .Build();
```

## Budget

Every calculation takes a `CancellationToken` and an `EngineBudget`. Exceeding either is a `TimeOut` error, never a
hang: `Σ`, `Π`, `∫` and `d/dx` count their evaluations.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
