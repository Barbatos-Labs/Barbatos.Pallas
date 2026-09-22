// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>Orders real values exactly: two decimals by <see cref="decimal"/>, anything else by <see cref="double"/>.</summary>
/// <remarks>
/// <see cref="ValueMath.CompareReal"/> takes values within 10⁻¹³ of each other as equal, which is right for Verify and no
/// order to sort by: it is not transitive.
/// </remarks>
internal sealed class RealOrder : IComparer<Value>
{
    public static readonly RealOrder Instance = new();

    public int Compare(Value x, Value y)
    {
        return x.Kind == ValueKind.DecimalReal && y.Kind == ValueKind.DecimalReal
            ? x.ToDecimal().CompareTo(y.ToDecimal())
            : x.ToDouble().CompareTo(y.ToDouble());
    }
}
