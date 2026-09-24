// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using BenchmarkDotNet.Attributes;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// The Table application at its largest: 30 rows of f(x) and g(x), and 45 rows of f(x) alone (pp. 108-113).
/// </summary>
public class TableBenchmarks
{
    private CalculatorSession _session = null!;

    /// <summary>Defines f and g in the Table application.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _session = Sessions.In(CalculatorApp.Table);
        CalcError? error = _session.Define(DefinedFunction.F, "sin(x)+x²÷3") ?? _session.Define(DefinedFunction.G, "√(x)×ln(x+1)");
        if (error is not null)
        {
            throw new InvalidOperationException("f(x) or g(x) is not an expression the Table application reads.");
        }
    }

    /// <summary>Generates 30 rows of both functions.</summary>
    [Benchmark]
    [Target(20)]
    public NumberTable TwoFunctions() => Generated(TableType.FunctionsFAndG, 30);

    /// <summary>Generates 45 rows of f(x).</summary>
    [Benchmark]
    [Target(20)]
    public NumberTable OneFunction() => Generated(TableType.FunctionF, 45);

    private NumberTable Generated(TableType type, int rows)
    {
        NumberTable table = NumberTable.Generate(_session, type, Sessions.N(1), Sessions.N(rows), Sessions.N(1));
        return table.Rows.Count == rows ? table : throw new InvalidOperationException($"The table has {table.Rows.Count} rows, not {rows}.");
    }
}
