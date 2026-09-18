// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A physical constant of a <see cref="ConstantSet"/>.
/// </summary>
public sealed class ScientificConstant
{
    /// <summary>Initializes a constant.</summary>
    /// <param name="symbol">The name as written, starting with <c>@</c>: <c>@h</c>.</param>
    /// <param name="value">The value.</param>
    /// <param name="standardUncertainty">The standard uncertainty; zero for a constant exact by definition.</param>
    /// <param name="unit">The SI unit, such as <c>J Hz^-1</c>; empty for a dimensionless constant.</param>
    /// <exception cref="ArgumentException"><paramref name="symbol"/> is not a valid scientific constant name.</exception>
    public ScientificConstant(string symbol, ScaledDecimal value, ScaledDecimal standardUncertainty, string unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        Symbol = SyntaxSymbol.CreateName(symbol, SymbolKind.ScientificConstant).Text;
        Value = value;
        StandardUncertainty = standardUncertainty;
        Unit = unit;
    }

    /// <summary>Gets the name as written, starting with <c>@</c>.</summary>
    public string Symbol { get; }

    /// <summary>Gets the value.</summary>
    public ScaledDecimal Value { get; }

    /// <summary>Gets the standard uncertainty; zero when the value is exact.</summary>
    public ScaledDecimal StandardUncertainty { get; }

    /// <summary>Gets the SI unit.</summary>
    public string Unit { get; }

    /// <summary>Gets whether the value is exact by definition.</summary>
    public bool IsExact => StandardUncertainty.Mantissa == 0m;
}
