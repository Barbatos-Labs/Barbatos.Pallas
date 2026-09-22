// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What a <see cref="Calculation"/> produced.
/// </summary>
public enum CalculationKind
{
    /// <summary>One value.</summary>
    Value = 0,

    /// <summary>A Verify result: <see cref="Calculation.IsTrue"/>, with 1 or 0 in Ans (p. 76).</summary>
    Verify = 1,

    /// <summary>A division with remainder: the quotient and the remainder (p. 56).</summary>
    Remainder = 2,

    /// <summary>Pol(: r and θ (p. 62).</summary>
    Polar = 3,

    /// <summary>Rec(: x and y (p. 62).</summary>
    Rectangular = 4,

    /// <summary>A Solver result: the solution, with Left − Right in <see cref="Calculation.Second"/> (p. 121).</summary>
    Solution = 5,
}
