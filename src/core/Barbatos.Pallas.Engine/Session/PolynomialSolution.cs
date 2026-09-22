// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What <see cref="CalculatorSession.SolvePolynomial"/> found for a polynomial of degree 2 to 4 (manual pp. 116-119).
/// </summary>
public sealed class PolynomialSolution
{
    internal PolynomialSolution(
        SolutionOutcome outcome,
        ImmutableArray<PolynomialRoot> roots,
        ImmutableArray<PolynomialExtremum> extrema,
        CalcError? error)
    {
        Outcome = outcome;
        Roots = roots;
        Extrema = extrema;
        Error = error;
    }

    /// <summary>Gets whether the polynomial has roots to show, or only complex ones while Complex Roots is off.</summary>
    public SolutionOutcome Outcome { get; }

    /// <summary>
    /// Gets the roots, as many as the degree, by decreasing real part and then by decreasing imaginary part; a repeated
    /// root appears as many times as it is a root.
    /// </summary>
    public ImmutableArray<PolynomialRoot> Roots { get; }

    /// <summary>
    /// Gets the local extrema, by increasing x: one for degree 2, two or none for degree 3, none for degree 4. Empty is
    /// the calculator's "No Local Max/Min" (p. 117).
    /// </summary>
    public ImmutableArray<PolynomialExtremum> Extrema { get; }

    /// <summary>Gets the error, or <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the calculation ran to an answer.</summary>
    public bool Succeeded => Error is null;
}
