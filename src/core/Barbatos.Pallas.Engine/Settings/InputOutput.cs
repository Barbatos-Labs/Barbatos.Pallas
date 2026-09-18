// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Input/Output setting (manual p. 22). Input is always Canonical Linear Syntax in the engine; the setting decides the
/// forms a result may be displayed in.
/// </summary>
public enum InputOutput
{
    /// <summary>MathI/MathO, the initial setting: fractions, square roots and π forms where possible.</summary>
    MathIMathO = 0,

    /// <summary>MathI/DecimalO: decimals.</summary>
    MathIDecimalO = 1,

    /// <summary>LineI/LineO: decimals or fractions.</summary>
    LineILineO = 2,

    /// <summary>LineI/DecimalO: decimals.</summary>
    LineIDecimalO = 3,
}
