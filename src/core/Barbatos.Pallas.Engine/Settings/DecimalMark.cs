// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Decimal Mark setting (manual p. 24-25).
/// </summary>
public enum DecimalMark
{
    /// <summary>A dot, the initial setting; several results are separated by commas.</summary>
    Dot = 0,

    /// <summary>A comma; several results are separated by semicolons.</summary>
    Comma = 1,
}
