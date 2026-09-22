// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What an Equation or Inequality calculation found, where the calculator shows a message instead of values
/// (manual pp. 116, 118, 125).
/// </summary>
/// <remarks>The message itself belongs to the application: the core is language-neutral.</remarks>
public enum SolutionOutcome
{
    /// <summary>The calculation produced values.</summary>
    Solved = 0,

    /// <summary>Simultaneous equations with no solution, or an inequality no number satisfies.</summary>
    NoSolution = 1,

    /// <summary>Simultaneous equations satisfied by infinitely many values (p. 116).</summary>
    InfiniteSolutions = 2,

    /// <summary>A polynomial whose roots are all complex, with Complex Roots off (p. 118).</summary>
    NoRealRoots = 3,

    /// <summary>An inequality every real number satisfies (p. 125).</summary>
    AllRealNumbers = 4,
}
