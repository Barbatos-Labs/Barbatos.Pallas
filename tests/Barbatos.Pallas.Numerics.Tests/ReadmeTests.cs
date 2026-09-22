// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics.Tests;

/// <summary>
/// The values the package README shows for the error function and the Poisson probability, as written: a README that
/// drifts from the code is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheErrorFunctionAndPoissonExamples()
    {
        ErrorFunction.Erf(1).Should().Be(0.8427007929497149);
        ErrorFunction.Erfc(10).Should().Be(2.0884875837625446E-45);
        ErrorFunction.InverseErfc(0.5).Should().Be(0.47693627620446993);
        PoissonDistribution.Probability(2, 1).Should().Be(0.18393972058572122);
        PoissonDistribution.Probability(1_000_000, 1_000_000).Should().Be(0.00039894224715624404);
    }
}
