// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The Same Result setting of a simulation (manual p. 150).</summary>
/// <remarks>
/// With a preset, a simulation of the same number of dice or coins and the same number of attempts gives the same
/// results every time and on every copy of Pallas, which is what the setting is for: a class whose calculators all show
/// one result. They are Pallas's own results, not the reference calculator's, whose sequences are not published
/// (deviation D8 of docs/CONFORMANCE.md).
/// </remarks>
public enum SameResult
{
    /// <summary>Off, the initial setting: every simulation is random.</summary>
    Off = 0,

    /// <summary>#1.</summary>
    First = 1,

    /// <summary>#2.</summary>
    Second = 2,

    /// <summary>#3.</summary>
    Third = 3,
}
