// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using BenchmarkDotNet.Running;

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// <c>dotnet run -c Release</c> runs the benchmarks BenchmarkDotNet's arguments select; with <c>--gate</c> first, every
/// benchmark (or those the arguments after it select) runs a fixed number of iterations and the run fails when one
/// misses its target (<see cref="Gate"/>).
/// </summary>
public static class Program
{
    /// <summary>Runs the benchmarks.</summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The gate's verdict, or 0.</returns>
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--gate")
        {
            return Gate.Run(args[1..]);
        }

        _ = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, Gate.Config(fixedIterations: false));
        return 0;
    }
}
