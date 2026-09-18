// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Number Format setting (manual p. 23): Norm 1, Norm 2, Fix 0-9 or Sci 1-10.
/// </summary>
public readonly record struct NumberFormat
{
    private NumberFormat(NumberFormatKind kind, int digits)
    {
        Kind = kind;
        Digits = digits;
    }

    /// <summary>Gets Norm 1, the initial setting: exponent form when |x| &lt; 10⁻² or |x| ≥ 10¹⁰.</summary>
    public static NumberFormat Norm1 { get; } = new(NumberFormatKind.Norm, 1);

    /// <summary>Gets Norm 2: exponent form when |x| &lt; 10⁻⁹ or |x| ≥ 10¹⁰.</summary>
    public static NumberFormat Norm2 { get; } = new(NumberFormatKind.Norm, 2);

    /// <summary>Gets the kind.</summary>
    public NumberFormatKind Kind { get; }

    /// <summary>Gets the digit count: 1 or 2 for Norm, the decimal places for Fix, the significant digits for Sci.</summary>
    public int Digits { get; }

    /// <summary>Returns Fix with the given number of decimal places.</summary>
    /// <param name="decimalPlaces">0 to 9.</param>
    /// <returns>The setting.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="decimalPlaces"/> is outside 0-9.</exception>
    public static NumberFormat Fix(int decimalPlaces)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, 9);
        return new NumberFormat(NumberFormatKind.Fix, decimalPlaces);
    }

    /// <summary>Returns Sci with the given number of significant digits.</summary>
    /// <param name="significantDigits">1 to 10.</param>
    /// <returns>The setting.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="significantDigits"/> is outside 1-10.</exception>
    public static NumberFormat Sci(int significantDigits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(significantDigits, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(significantDigits, 10);
        return new NumberFormat(NumberFormatKind.Sci, significantDigits);
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Kind}{Digits}";
}
