// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The calculation types of the Distribution application (manual p. 96).</summary>
public enum DistributionKind
{
    /// <summary>Binomial PD: the probability of x successes in N trials, P(X = x).</summary>
    BinomialPD = 0,

    /// <summary>Binomial CD: the probability of at most x successes in N trials, P(X ≤ x).</summary>
    BinomialCD = 1,

    /// <summary>Normal PD: the normal probability density at x.</summary>
    NormalPD = 2,

    /// <summary>Normal CD: the normal probability of Lower ≤ X ≤ Upper.</summary>
    NormalCD = 3,

    /// <summary>Inverse Normal: the x whose left-tail area is Area.</summary>
    InverseNormal = 4,

    /// <summary>Poisson PD: the probability P(X = x) of a Poisson variable with mean λ.</summary>
    PoissonPD = 5,

    /// <summary>Poisson CD: the probability P(X ≤ x) of a Poisson variable with mean λ.</summary>
    PoissonCD = 6,
}
