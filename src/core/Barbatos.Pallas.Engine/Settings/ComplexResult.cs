// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Complex Result setting (manual p. 24).
/// </summary>
public enum ComplexResult
{
    /// <summary>a+bi, the initial setting.</summary>
    Rectangular = 0,

    /// <summary>r∠θ, with θ in (−180°, 180°].</summary>
    Polar = 1,
}
