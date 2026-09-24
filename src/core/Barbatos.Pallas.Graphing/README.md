# Barbatos.Pallas.Graphing

Platform-neutral function graphing for Barbatos.Pallas: adaptive sampling clipped to a viewport, asymptote and jump
detection, and roots, extrema and intersections placed on the engine's own values.

> **Status: 1.0.** It ships inside
> [Barbatos.Pallas.DependencyInjection](https://www.nuget.org/packages/Barbatos.Pallas.DependencyInjection), whose
> version it has, and it follows semantic versioning with it.

## A curve

An expression in x is compiled once by the engine and sampled across a viewport: one sample per column of pixels to
start with, and finer where a chord would stray more than half a pixel from the curve. What comes back is ready to
draw: pieces of polyline inside the viewport, and where the curve breaks.

```csharp
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Graphing;

CalculatorSession session = engine.CreateSession(CalculatorApp.Table);
session.Settings = session.Settings with { AngleUnit = AngleUnit.Degree };

GraphViewport view = new(left: 0, right: 360, bottom: -10, top: 10);
GraphTrace tangent = GraphSampler.Sample(session.Compile("tan(x)"), view, columns: 400, rows: 300);

tangent.Pieces.Length;                 // 3: the curve is never drawn across an asymptote
tangent.Breaks[0];                     // GraphBreak { X ≈ 90, Kind = Asymptote }
```

A piece ends where the curve leaves the viewport, on its edge, so the host draws the points as they are and clips
nothing. Where the expression has no value - an error, or a complex number - the curve stops. A jump is told from a
steep curve by halving towards it: Int(x) breaks at every integer but 0, and the cube root does not break at 0.

## Named points

A root, an extremum or an intersection is found where something changes sign between two samples, and placed by
halving on the engine's values to the fifteen digits a value holds. Its x is a `Value`, and its y is the engine's
calculation there, displayed as any result is.

```csharp
GraphAnalysis.Roots(session.Compile("x^2−2"), new GraphViewport(-3, 3, -1, 1), columns: 100)[1].X;   // 1.41421356237310

GraphFeature top = GraphAnalysis.Extrema(session.Compile("sin(x)"), view, columns: 36)[0];
(top.Kind, top.X, top.Y.Display.Text);                                                             // (Maximum, 90, "1")

GraphAnalysis.Intersections(session.Compile("sin(x)"), session.Compile("cos(x)"), view, 90)[0].Y.Display.Text;  // "√(2)⌟2"
```

An extremum is found on the engine's exact derivative, so the maximum of sin x is at exactly 90°. A change of sign at
a pole or a jump is no root, and a curve that only touches the axis has an extremum there, not a root. A curve that
runs along the axis, as Int(x) does from -1 to 1, names no root there: every x is one. Two curves meet only where
both have a value.

Every coordinate here is a `double`: it places a point on a screen. No value this library shows is its own; see
[docs/PRECISION.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/PRECISION.md) §7.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
