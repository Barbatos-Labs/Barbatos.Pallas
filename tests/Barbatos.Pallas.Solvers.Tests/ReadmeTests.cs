// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Solvers.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void TheExamples()
    {
        BigInteger[] cubic = [6, -5, -2, 1];

        IntegerPolynomial.CountRealRoots(cubic).Should().Be(3);
        IntegerPolynomial.SignAt(cubic, 1, 2).Should().Be(1);
        IntegerPolynomial.Deflate(cubic, 1, 1).Should().Equal([new BigInteger(-6), BigInteger.MinusOne, BigInteger.One]);

        Complex[] roots = PolynomialRoots.Find([6d, -5d, -2d, 1d]);
        PolynomialRoots.TryGetRational(roots[0].Real, cubic, out BigInteger p, out BigInteger q).Should().BeTrue();
        (p / q).Should().BeOneOf(BigInteger.One, new BigInteger(-2), new BigInteger(3));
        q.Should().Be(BigInteger.One);

        IntegerPolynomial.SquareFree([4, 0, -4, 0, 1]).Should().Equal([new BigInteger(-2), BigInteger.Zero, BigInteger.One]);
    }
}
