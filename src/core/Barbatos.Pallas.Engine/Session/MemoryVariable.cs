// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The variables of the calculator (manual p. 38), shared by every application.
/// </summary>
public enum MemoryVariable
{
    /// <summary>A.</summary>
    A = 0,

    /// <summary>B.</summary>
    B = 1,

    /// <summary>C.</summary>
    C = 2,

    /// <summary>D.</summary>
    D = 3,

    /// <summary>E; also the quotient of ÷R.</summary>
    E = 4,

    /// <summary>F; also the remainder of ÷R.</summary>
    F = 5,

    /// <summary>x; also r of Pol( and x of Rec(.</summary>
    X = 6,

    /// <summary>y; also θ of Pol( and y of Rec(.</summary>
    Y = 7,

    /// <summary>z.</summary>
    Z = 8,
}
