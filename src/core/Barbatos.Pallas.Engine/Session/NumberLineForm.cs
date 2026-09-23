// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The nine forms of a Number Line expression, in the order of the calculator's list (manual p. 153).</summary>
public enum NumberLineForm
{
    /// <summary>x&lt;a.</summary>
    Less = 0,

    /// <summary>x≤a.</summary>
    LessOrEqual = 1,

    /// <summary>x=a.</summary>
    Equal = 2,

    /// <summary>x&gt;a.</summary>
    Greater = 3,

    /// <summary>x≥a.</summary>
    GreaterOrEqual = 4,

    /// <summary>a&lt;x&lt;b.</summary>
    Between = 5,

    /// <summary>a≤x&lt;b.</summary>
    FromIncluded = 6,

    /// <summary>a&lt;x≤b.</summary>
    ToIncluded = 7,

    /// <summary>a≤x≤b.</summary>
    BetweenIncluded = 8,
}
