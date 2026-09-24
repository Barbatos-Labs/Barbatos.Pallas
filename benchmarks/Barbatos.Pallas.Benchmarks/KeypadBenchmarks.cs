// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using BenchmarkDotNet.Attributes;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// An expression typed on the keypad and calculated with =, as the Calculate screen does it: read, bound, calculated,
/// displayed, stored in Ans and the history.
/// </summary>
public class KeypadBenchmarks
{
    private CalculatorSession _session = null!;

    /// <summary>What a calculator is typically asked, from the manual's own kinds of example.</summary>
    public static IEnumerable<string> Inputs =>
    [
        "(1+2)×3÷4−5²",
        "123456789×987654321",
        "2⌟3+1⌟1⌟2",
        "10√(2)+15×3√(3)",
        "sin(30)+cos(60)×tan(45)",
        "log(2,1024)+ln(e^3)",
        "5!+10P3+10C3",
        "Abs(-7.5)+Int(3.7)+Intg(-2.5)",
        "6.02×10^23×1.6×10^-19",
        "d/dx(x³,0.1)",
        "Σ(x,1,100)",
        "∫(x²,0,1)",
    ];

    /// <summary>Gets or sets the expression.</summary>
    [ParamsSource(nameof(Inputs))]
    public string Input { get; set; } = "";

    /// <summary>Opens the Calculate screen and checks the expression is one it calculates.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _session = Sessions.In(CalculatorApp.Calculate);
        _session.Checked(Input);
        _session.ClearHistory();
    }

    /// <summary>Calculates the expression; the history is emptied each time, so it does not grow with the run.</summary>
    [Benchmark]
    [Target(1)]
    public Calculation Calculate()
    {
        Calculation calculation = _session.Calculate(Input);
        _session.ClearHistory();
        return calculation;
    }
}
