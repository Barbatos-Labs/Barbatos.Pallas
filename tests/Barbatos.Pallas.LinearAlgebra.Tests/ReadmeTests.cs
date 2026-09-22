// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.LinearAlgebra.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheExamples()
    {
        ExactLinearAlgebra.Determinant(new decimal[,] { { 0.5m, 0.25m }, { 1, 3 } })
            .Should().Be((new BigInteger(5), new BigInteger(4)));

        ExactLinearAlgebra.TryInvert(new decimal[,] { { 1, 2 }, { 3, 4 } }, out BigInteger[,] inverse, out BigInteger common).Should().BeTrue();
        inverse.Should().BeEquivalentTo(new BigInteger[,] { { -4, 2 }, { 3, -1 } });
        common.Should().Be(new BigInteger(2));

        ExactLinearAlgebra.TrySolve(new decimal[,] { { 1, -1, 1 }, { 1, 1, -1 }, { -1, 1, 1 } }, [2, 0, 4], out BigInteger[] x, out BigInteger d).Should().BeTrue();
        x.Should().Equal(BigInteger.One, new BigInteger(2), new BigInteger(3));
        d.Should().Be(BigInteger.One);

        ExactLinearAlgebra.TryInvert(new decimal[,] { { 0.1m, 0.2m }, { 0.3m, 0.6m } }, out _, out _).Should().BeFalse();
    }
}
