// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// How closely the engine follows the reference calculator's limits (docs/PRECISION.md §10). A profile changes limits and
/// formatting, never how a value is computed.
/// </summary>
public enum CalculatorProfile
{
    /// <summary>
    /// The calculator's limits: the calculation range ±10⁻⁹⁹ to ±9.999999999×10⁹⁹, the function domains of pp. 170-171,
    /// and the calculator's display bounds.
    /// </summary>
    Standard = 0,

    /// <summary>The range of <see cref="double"/> and wider display bounds, for engineering and accounting work.</summary>
    Extended = 1,
}
