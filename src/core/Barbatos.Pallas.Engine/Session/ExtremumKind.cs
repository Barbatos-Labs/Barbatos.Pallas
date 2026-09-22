// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Which extremum of a polynomial a <see cref="PolynomialExtremum"/> is (manual p. 117).
/// </summary>
public enum ExtremumKind
{
    /// <summary>A local minimum.</summary>
    Minimum = 0,

    /// <summary>A local maximum.</summary>
    Maximum = 1,
}
