// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A local minimum or maximum of a polynomial, where the calculator shows its coordinates (manual p. 117).
/// </summary>
public sealed class PolynomialExtremum
{
    internal PolynomialExtremum(ExtremumKind kind, Calculation x, Calculation y)
    {
        Kind = kind;
        X = x;
        Y = y;
    }

    /// <summary>Gets whether this is the minimum or the maximum.</summary>
    public ExtremumKind Kind { get; }

    /// <summary>Gets where it is.</summary>
    public Calculation X { get; }

    /// <summary>Gets the value of the polynomial there.</summary>
    public Calculation Y { get; }
}
