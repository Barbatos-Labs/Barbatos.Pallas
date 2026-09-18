// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A number written as a <see cref="decimal"/> mantissa times a power of ten, so reference data keeps every published digit
/// without <see cref="double"/>.
/// </summary>
/// <param name="Mantissa">The mantissa: <c>6.62607015</c> for the Planck constant.</param>
/// <param name="Exponent">The power of ten: <c>-34</c> for the Planck constant.</param>
/// <remarks>
/// <see cref="decimal"/> alone cannot hold 6.62607015×10⁻³⁴ (it would be 0), and <see cref="double"/> is not allowed in
/// data packages (docs/PRECISION.md §7). The engine turns the number into a <see cref="Value"/> with the precision rule.
/// </remarks>
public readonly record struct ScaledDecimal(decimal Mantissa, int Exponent)
{
    /// <summary>Returns a scaled decimal with exponent 0.</summary>
    /// <param name="value">The number.</param>
    /// <returns>The scaled decimal.</returns>
    public static ScaledDecimal FromDecimal(decimal value) => new(value, 0);

    /// <summary>Returns a scaled decimal with exponent 0.</summary>
    /// <param name="value">The number.</param>
    public static implicit operator ScaledDecimal(decimal value) => FromDecimal(value);
}
