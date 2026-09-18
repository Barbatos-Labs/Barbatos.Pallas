// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Fraction Result setting (manual p. 24).
/// </summary>
public enum FractionResult
{
    /// <summary>Improper fractions, the initial setting: 13⌟4.</summary>
    Improper = 0,

    /// <summary>Mixed fractions: 3⌟1⌟4.</summary>
    Mixed = 1,
}
