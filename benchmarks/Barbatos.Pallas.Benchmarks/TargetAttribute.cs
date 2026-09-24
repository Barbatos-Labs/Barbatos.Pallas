// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Benchmarks;

/// <summary>
/// The time a benchmark is to stay under: the 99th percentile of its measurements, in milliseconds.
/// </summary>
/// <remarks>
/// The targets of docs/ARCHITECTURE.md §9, and for the other applications a time measured on 24 Sep 2026 and given room
/// to spare. The gate fails a benchmark beyond its target times <see cref="Gate.Margin"/>: a shared build machine is
/// slower than a developer's and its timings move between runs, and a target it missed by chance would teach everyone
/// to ignore the gate.
/// </remarks>
/// <param name="milliseconds">The target.</param>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TargetAttribute(double milliseconds) : Attribute
{
    /// <summary>Gets the target, in milliseconds.</summary>
    public double Milliseconds { get; } = milliseconds;
}
