// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// One stretch of the numbers that satisfy an inequality, as the calculator writes it: <c>x≤-3</c>, <c>1≤x</c> or
/// <c>-3&lt;x&lt;1</c> (manual pp. 124-125).
/// </summary>
public sealed class SolutionInterval
{
    internal SolutionInterval(Calculation? lower, bool lowerIncluded, Calculation? upper, bool upperIncluded, string text)
    {
        Lower = lower;
        LowerIncluded = lowerIncluded;
        Upper = upper;
        UpperIncluded = upperIncluded;
        Text = text;
    }

    /// <summary>Gets the lower bound, or <see langword="null"/> when the stretch has none.</summary>
    public Calculation? Lower { get; }

    /// <summary>Gets whether the lower bound is part of the solution.</summary>
    public bool LowerIncluded { get; }

    /// <summary>Gets the upper bound, or <see langword="null"/> when the stretch has none.</summary>
    public Calculation? Upper { get; }

    /// <summary>Gets whether the upper bound is part of the solution.</summary>
    public bool UpperIncluded { get; }

    /// <summary>Gets the stretch as the calculator writes it, with the settings of the calculation.</summary>
    public string Text { get; }

    /// <inheritdoc/>
    public override string ToString() => Text;
}
