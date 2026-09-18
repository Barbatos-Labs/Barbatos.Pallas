// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The kinds of the Number Format setting (manual p. 23).
/// </summary>
public enum NumberFormatKind
{
    /// <summary>Norm: 10 significant digits, in exponent form outside a range that depends on the digit count (1 or 2).</summary>
    Norm = 0,

    /// <summary>Fix: a fixed number of decimal places, 0 to 9.</summary>
    Fix = 1,

    /// <summary>Sci: a fixed number of significant digits in exponent form, 1 to 10.</summary>
    Sci = 2,
}
