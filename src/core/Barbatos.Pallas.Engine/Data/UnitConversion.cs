// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A unit conversion command, such as <c>cm▶in</c>: the result is <c>(value + OffsetBefore) × Multiplier ÷ Divisor + OffsetAfter</c>.
/// </summary>
/// <remarks>
/// A multiplier and a divisor rather than one factor keep exact definitions exact. Centimetres to inches divide by 2.54,
/// so <c>5cm▶in</c> is the <see cref="decimal"/> quotient 5 ÷ 2.54, which displays as 250⌟127; Fahrenheit to Celsius is
/// <c>(value − 32) × 5 ÷ 9</c>.
/// </remarks>
public sealed class UnitConversion
{
    /// <summary>Initializes a conversion.</summary>
    /// <param name="command">The command as written: <c>cm▶in</c>.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <param name="divisor">The divisor; not zero.</param>
    /// <param name="offsetBefore">Added to the value before scaling.</param>
    /// <param name="offsetAfter">Added after scaling.</param>
    /// <exception cref="ArgumentException"><paramref name="command"/> is not a valid unit conversion name.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="divisor"/> is zero.</exception>
    public UnitConversion(string command, decimal multiplier, decimal divisor = 1m, decimal offsetBefore = 0m, decimal offsetAfter = 0m)
    {
        ArgumentOutOfRangeException.ThrowIfZero(divisor);
        Command = SyntaxSymbol.CreateName(command, SymbolKind.UnitConversion).Text;
        Multiplier = multiplier;
        Divisor = divisor;
        OffsetBefore = offsetBefore;
        OffsetAfter = offsetAfter;
    }

    /// <summary>Gets the command as written.</summary>
    public string Command { get; }

    /// <summary>Gets the multiplier.</summary>
    public decimal Multiplier { get; }

    /// <summary>Gets the divisor.</summary>
    public decimal Divisor { get; }

    /// <summary>Gets the offset added before scaling.</summary>
    public decimal OffsetBefore { get; }

    /// <summary>Gets the offset added after scaling.</summary>
    public decimal OffsetAfter { get; }
}
