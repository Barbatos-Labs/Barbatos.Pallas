// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The parameters of the Distribution application (manual p. 98). Each calculation type reads the ones it needs.
/// </summary>
/// <remarks>
/// On the calculator the last value entered for a parameter is kept for every type that uses it: N entered for Binomial
/// PD is also N of Binomial CD (p. 98). One record for all the types gives a host that behavior with <c>with</c>. A
/// parameter left out is 0.
/// </remarks>
public sealed record DistributionParameters
{
    /// <summary>Gets the data x, for the Variable input method.</summary>
    public Value X { get; init; }

    /// <summary>Gets N, the number of trials of a binomial distribution: a whole number, 0 or more.</summary>
    public Value Trials { get; init; }

    /// <summary>Gets p, the probability of success of a binomial distribution: from 0 to 1.</summary>
    public Value Probability { get; init; }

    /// <summary>Gets μ, the mean of a normal distribution.</summary>
    public Value Mean { get; init; }

    /// <summary>Gets σ, the standard deviation of a normal distribution: more than 0.</summary>
    public Value StandardDeviation { get; init; }

    /// <summary>Gets the lower bound of Normal CD.</summary>
    public Value Lower { get; init; }

    /// <summary>Gets the upper bound of Normal CD.</summary>
    public Value Upper { get; init; }

    /// <summary>Gets the left-tail area of Inverse Normal: from 0 to 1.</summary>
    public Value Area { get; init; }

    /// <summary>Gets λ, the mean of a Poisson distribution: more than 0.</summary>
    public Value Lambda { get; init; }
}
