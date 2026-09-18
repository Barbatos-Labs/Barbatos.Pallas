// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The .NET type that holds a <see cref="Value"/> (docs/PRECISION.md §3).
/// </summary>
public enum ValueKind
{
    /// <summary>A real number held as <see cref="decimal"/>: the calculator's number.</summary>
    DecimalReal = 0,

    /// <summary>A real number held as <see cref="double"/>, because <see cref="decimal"/> would keep fewer than 15 significant digits of it.</summary>
    DoubleReal = 1,

    /// <summary>A complex number, held as <see cref="System.Numerics.Complex"/>, in the Complex application.</summary>
    Complex = 2,

    /// <summary>A 32-bit two's complement integer, in the Base-N application.</summary>
    BaseN = 3,
}
