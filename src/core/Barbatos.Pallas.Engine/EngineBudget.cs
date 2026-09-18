// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The limits on one calculation's work (docs/PRECISION.md §11). Exceeding either is a Time Out, never a hang.
/// </summary>
/// <param name="MaxIterations">The most evaluations of a Σ, Π or ∫ body, summed over the calculation.</param>
/// <param name="Timeout">The longest a calculation may run.</param>
public sealed record EngineBudget(long MaxIterations, TimeSpan Timeout)
{
    /// <summary>Gets the default budget: 10⁸ iterations and 10 seconds.</summary>
    public static EngineBudget Default { get; } = new(100_000_000, TimeSpan.FromSeconds(10));
}
