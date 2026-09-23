// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>What the Relative Freq screen of a simulation counts (manual pp. 148, 150, 153).</summary>
public enum SimulationTally
{
    /// <summary>The face of one die, or the sum of two or three: 1-6, 2-12 or 3-18.</summary>
    Sum = 0,

    /// <summary>The difference between two dice, 0 to 5; two dice only.</summary>
    Difference = 1,

    /// <summary>How many coins came up heads, 0 to the number of coins.</summary>
    Heads = 2,
}
