// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The base a Base-N prefix gives to the literal after it (<c>d10</c>, <c>h10</c>, <c>b10</c>, <c>o10</c>).
/// </summary>
public enum NumberBase
{
    /// <summary>Prefix <c>d</c>: decimal, shown as Dec on the calculator.</summary>
    Dec = 10,

    /// <summary>Prefix <c>h</c>: hexadecimal, shown as Hex.</summary>
    Hex = 16,

    /// <summary>Prefix <c>b</c>: binary, shown as Bin.</summary>
    Bin = 2,

    /// <summary>Prefix <c>o</c>: octal, shown as Oct.</summary>
    Oct = 8,
}
