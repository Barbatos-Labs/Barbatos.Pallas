// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The View-Window of the Number Line application: the middle of the x axis and its scale (manual pp. 156-157).</summary>
/// <param name="Center">The value in the middle of the axis.</param>
/// <param name="Scale">The distance between two ticks.</param>
/// <param name="Error">The Range ERROR of p. 165 when the scale or the center is out of its range; otherwise <see langword="null"/>.</param>
/// <remarks>The axis shows eight ticks on either side of the center: from Center − 8 × Scale to Center + 8 × Scale (p. 157).</remarks>
public sealed record NumberLineView(decimal Center, decimal Scale, CalcError? Error = null)
{
    /// <summary>The ticks on either side of the center.</summary>
    public const int Ticks = 8;

    /// <summary>Gets the value at the left end of the axis.</summary>
    public decimal Minimum => Center - (Ticks * Scale);

    /// <summary>Gets the value at the right end of the axis.</summary>
    public decimal Maximum => Center + (Ticks * Scale);

    /// <summary>Gets whether the view is one the calculator draws.</summary>
    public bool Succeeded => Error is null;
}
