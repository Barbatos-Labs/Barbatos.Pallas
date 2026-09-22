// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What <see cref="CalculatorSession.SolveSimultaneous"/> found for a system of linear equations (manual pp. 114-116).
/// </summary>
public sealed class SimultaneousSolution
{
    internal SimultaneousSolution(SolutionOutcome outcome, ImmutableArray<Calculation> unknowns, CalcError? error)
    {
        Outcome = outcome;
        Unknowns = unknowns;
        Error = error;
    }

    /// <summary>Gets what the system has: one solution, none, or infinitely many.</summary>
    public SolutionOutcome Outcome { get; }

    /// <summary>Gets the value of each unknown, named x, y, z and t; empty unless <see cref="Outcome"/> is solved.</summary>
    public ImmutableArray<Calculation> Unknowns { get; }

    /// <summary>Gets the error, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the calculation ran to an answer, which may be that there is no solution.</summary>
    public bool Succeeded => Error is null;
}
