// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What <see cref="CalculatorSession.SolveInequality"/> found for a polynomial inequality (manual pp. 124-125).
/// </summary>
public sealed class InequalitySolution
{
    internal InequalitySolution(SolutionOutcome outcome, ImmutableArray<SolutionInterval> intervals, string text, CalcError? error)
    {
        Outcome = outcome;
        Intervals = intervals;
        Text = text;
        Error = error;
    }

    /// <summary>Gets whether the inequality has a solution, none, or every real number.</summary>
    public SolutionOutcome Outcome { get; }

    /// <summary>Gets the stretches that satisfy it, in increasing order; empty unless <see cref="Outcome"/> is solved.</summary>
    public ImmutableArray<SolutionInterval> Intervals { get; }

    /// <summary>Gets the stretches as the calculator writes them, separated by the list separator; empty unless solved.</summary>
    public string Text { get; }

    /// <summary>Gets the error, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the calculation ran to an answer, which may be that nothing satisfies it.</summary>
    public bool Succeeded => Error is null;
}
