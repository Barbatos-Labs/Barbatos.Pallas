// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using CsCheck;

namespace Barbatos.Pallas.Solvers.Tests;

/// <summary>
/// Iterated roots, checked against the roots the polynomial was built from and against its value there.
/// </summary>
public sealed class PolynomialRootsTests
{
    [Fact]
    public void AQuadraticWithTwoRealRoots()
    {
        // x² − 3x + 2 = (x − 1)(x − 2).
        double[] roots = [.. PolynomialRoots.Find([2d, -3d, 1d]).Select(root => root.Real).Order()];

        PolynomialRoots.Find([2d, -3d, 1d]).Should().OnlyContain(root => Math.Abs(root.Imaginary) < 1e-15d);
        roots[0].Should().BeApproximately(1d, 1e-14d);
        roots[1].Should().BeApproximately(2d, 1e-14d);
    }

    [Fact]
    public void AQuadraticWithAConjugatePair()
    {
        // x² + 1: ±i, and the two roots must be each other's conjugate.
        Complex[] roots = [.. PolynomialRoots.Find([1d, 0d, 1d]).OrderBy(root => root.Imaginary)];

        roots[0].Real.Should().BeApproximately(0d, 1e-15d);
        roots[0].Imaginary.Should().BeApproximately(-1d, 1e-14d);
        roots[1].Imaginary.Should().BeApproximately(1d, 1e-14d);
    }

    [Fact]
    public void AQuarticWithFourRealRoots()
    {
        // (x − 1)(x − 2)(x − 3)(x − 4) = x⁴ − 10x³ + 35x² − 50x + 24.
        double[] roots = [.. PolynomialRoots.Find([24d, -50d, 35d, -10d, 1d]).Select(root => root.Real).Order()];

        roots.Should().HaveCount(4);
        for (int i = 0; i < roots.Length; i++)
        {
            roots[i].Should().BeApproximately(i + 1, 1e-12d);
        }
    }

    [Fact]
    public void ARepeatedRootIsFoundAsOftenAsItIsARoot()
    {
        // (x − 2)³: three roots at 2, each of them only as accurate as the cube root of the precision of a double,
        // because a value near a triple root is the cube of the distance to it.
        Complex[] roots = PolynomialRoots.Find([-8d, 12d, -6d, 1d]);

        roots.Should().HaveCount(3);
        roots.Should().OnlyContain(root => Complex.Abs(root - 2d) < 1e-4d);
    }

    [Fact]
    public void RootsFarOutsideTheUnitCircle()
    {
        // (x − 10⁶)(x + 2·10⁶)(x − 3): the iteration has to start on a circle that holds them, not on the unit circle.
        double[] coefficients = [6e12d, -2e12d - 3e6d, 1e6d - 3d, 1d];

        double[] roots = [.. PolynomialRoots.Find(coefficients).Select(root => root.Real).Order()];

        roots[0].Should().BeApproximately(-2e6d, 1d);
        roots[1].Should().BeApproximately(3d, 1e-5d);
        roots[2].Should().BeApproximately(1e6d, 1d);
    }

    [Fact]
    public void APolynomialWithoutRoots_IsRefused()
    {
        Action constant = () => PolynomialRoots.Find([1d]);
        Action zero = () => PolynomialRoots.Find([0d, 0d]);
        Action infinite = () => PolynomialRoots.Find([1d, double.PositiveInfinity]);
        Action missing = () => PolynomialRoots.Find(null!);

        constant.Should().Throw<ArgumentException>().WithParameterName("coefficients");
        zero.Should().Throw<ArgumentException>().WithParameterName("coefficients");
        infinite.Should().Throw<ArgumentException>().WithParameterName("coefficients");
        missing.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ARationalRootIsRecoveredFromItsApproximation()
    {
        // 6x² − 5x + 1 = (2x − 1)(3x − 1): the approximations are 0.5 and 0.3333…, the rationals are 1/2 and 1/3.
        BigInteger[] quadratic = [1, -5, 6];

        PolynomialRoots.TryGetRational(0.5d, quadratic, out BigInteger half, out BigInteger two).Should().BeTrue();
        PolynomialRoots.TryGetRational(1d / 3d, quadratic, out BigInteger one, out BigInteger three).Should().BeTrue();
        (half, two).Should().Be((BigInteger.One, new BigInteger(2)));
        (one, three).Should().Be((BigInteger.One, new BigInteger(3)));
    }

    [Fact]
    public void AnApproximationOfAWholeRootIsRecovered()
    {
        // The iteration lands beside 4, not on it: 3.9999999999999996 and 4.000000000000001 are both the root 4.
        BigInteger[] quadratic = [-16, 0, 1];

        PolynomialRoots.TryGetRational(3.9999999999999996d, quadratic, out BigInteger below, out BigInteger one).Should().BeTrue();
        PolynomialRoots.TryGetRational(4.000000000000001d, quadratic, out BigInteger above, out _).Should().BeTrue();
        below.Should().Be(new BigInteger(4));
        above.Should().Be(new BigInteger(4));
        one.Should().Be(BigInteger.One);
    }

    [Theory]
    // √2 is no rational; neither is a root of another polynomial, nor anything a double cannot stand for.
    [InlineData(1.4142135623730951d)]
    [InlineData(0.30000000000000004d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AnApproximationThatIsNoRationalRoot(double approximation)
    {
        PolynomialRoots.TryGetRational(approximation, [-2, 0, 1], out BigInteger numerator, out BigInteger denominator).Should().BeFalse();
        numerator.Should().Be(BigInteger.Zero);
        denominator.Should().Be(BigInteger.One);
    }

    [Fact]
    public void TheCoefficientsMayNotBeNull()
    {
        Action rational = () => PolynomialRoots.TryGetRational(1d, null!, out _, out _);

        rational.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EveryRootOfAPolynomialBuiltFromRoots_IsFound()
    {
        // Each root of (x − r₁)···(x − rₙ) must turn up among the roots that come back.
        Gen.Double[-20d, 20d].Array[1, 4]
            .Sample(
                roots =>
                {
                    Complex[] found = PolynomialRoots.Find(FromRoots(roots));

                    found.Should().HaveCount(roots.Length);
                    foreach (double root in roots)
                    {
                        found.Should().Contain(candidate => Complex.Abs(candidate - root) < 1e-6d * (1d + Math.Abs(root)));
                    }
                },
                iter: 500);
    }

    [Fact]
    public void ThePolynomialIsNegligibleAtEveryRootItReturns()
    {
        Gen.Double[-20d, 20d].Array[2, 5]
            .Sample(
                coefficients =>
                {
                    if (Math.Abs(coefficients[^1]) < 1e-3d)
                    {
                        return;
                    }

                    foreach (Complex root in PolynomialRoots.Find(coefficients))
                    {
                        // Compared with the size the terms reach, which is what a root of a polynomial can promise.
                        Complex value = Complex.Zero;
                        double scale = 0d;
                        for (int i = coefficients.Length - 1; i >= 0; i--)
                        {
                            value = (value * root) + coefficients[i];
                            scale = (scale * Complex.Abs(root)) + Math.Abs(coefficients[i]);
                        }

                        Complex.Abs(value).Should().BeLessThan(1e-8d * (scale + 1d));
                    }
                },
                iter: 500);
    }

    /// <summary>(x − r₁)···(x − rₙ), ascending.</summary>
    private static double[] FromRoots(double[] roots)
    {
        double[] polynomial = [1d];
        foreach (double root in roots)
        {
            double[] next = new double[polynomial.Length + 1];
            for (int i = 0; i < polynomial.Length; i++)
            {
                next[i + 1] += polynomial[i];
                next[i] -= polynomial[i] * root;
            }

            polynomial = next;
        }

        return polynomial;
    }
}
