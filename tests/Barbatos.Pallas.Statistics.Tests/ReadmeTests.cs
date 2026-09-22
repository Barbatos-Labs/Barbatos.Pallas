// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Statistics.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheExamples()
    {
        decimal[] x = [1.0m, 1.2m, 1.5m, 1.6m, 1.9m, 2.1m, 2.4m, 2.5m, 2.7m, 3.0m];
        decimal[] y = [1.0m, 1.1m, 1.2m, 1.3m, 1.4m, 1.5m, 1.6m, 1.7m, 1.8m, 2.0m];
        ExactSample sample = ExactSample.FromDecimals(x, y, frequencies: []);

        sample.Sum(1, 1).Should().Be((774, 25));
        sample.TryGetLinearFit(
            out (BigInteger Numerator, BigInteger Denominator) a,
            out (BigInteger Numerator, BigInteger Denominator) b).Should().BeTrue();
        a.Should().Be((10009, 19845));
        b.Should().Be((1906, 3969));
        sample.TryGetCorrelationSquared(
            out (BigInteger Numerator, BigInteger Denominator) r2,
            out int sign).Should().BeTrue();
        r2.Should().Be((908209, 916839));
        sign.Should().Be(1);

        ExactSample.FromDecimals([1, 1], [1, 2], []).TryGetLinearFit(out _, out _).Should().BeFalse();

        ExactSample weighted = ExactSample.FromScaledIntegers([15, 25], 1, [3, 5], 0, [5, 15], 1);
        weighted.Count.Should().Be((2, 1));

        Quartiles.Ranks(20, Quartile.First).Should().Be((new BigInteger(5), new BigInteger(6)));
    }
}
