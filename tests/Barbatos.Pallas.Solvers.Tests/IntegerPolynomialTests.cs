// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using CsCheck;

namespace Barbatos.Pallas.Solvers.Tests;

/// <summary>
/// Exact polynomial algebra, checked against polynomials built from roots that are known beforehand.
/// </summary>
public sealed class IntegerPolynomialTests
{
    [Theory]
    [InlineData("0", -1)]
    [InlineData("1", 0)]
    [InlineData("0 0 0", -1)]
    [InlineData("1 2 0 0", 1)]
    [InlineData("-6 -5 -2 1", 3)]
    public void TheDegreeIsTheHighestCoefficientThatIsNotZero(string coefficients, int degree)
    {
        IntegerPolynomial.Degree(Parse(coefficients)).Should().Be(degree);
    }

    [Theory]
    // x³ − 2x² − 5x + 6 = (x − 1)(x + 2)(x − 3): 0 at its roots, and it changes sign between them.
    [InlineData("1 1", 0)]
    [InlineData("-2 1", 0)]
    [InlineData("3 1", 0)]
    [InlineData("1 2", 1)]
    [InlineData("5 2", -1)]
    [InlineData("-4 1", -1)]
    [InlineData("4 1", 1)]
    public void TheSignAtARationalPointIsExact(string point, int sign)
    {
        BigInteger[] parts = Parse(point);

        IntegerPolynomial.SignAt(Parse("6 -5 -2 1"), parts[0], parts[1]).Should().Be(sign);
    }

    [Fact]
    public void TheSignOfAValueDecimalWouldRound()
    {
        // (3x − 1)³ has one root, 1/3, which no decimal is: the sign just below and just above it is still exact.
        BigInteger[] cube = Parse("-1 9 -27 27");

        IntegerPolynomial.SignAt(cube, 1, 3).Should().Be(0);
        IntegerPolynomial.SignAt(cube, 33333333333333333, 100000000000000000).Should().Be(-1);
        IntegerPolynomial.SignAt(cube, 33333333333333334, 100000000000000000).Should().Be(1);
    }

    [Fact]
    public void TheDerivativeOfEachDegree()
    {
        IntegerPolynomial.Derivative(Parse("6 -5 -2 1")).Should().Equal(Parse("-5 -4 3"));
        IntegerPolynomial.Derivative(Parse("7")).Should().Equal(BigInteger.Zero);
        IntegerPolynomial.Derivative(Parse("0")).Should().Equal(BigInteger.Zero);
    }

    [Theory]
    // The square-free part keeps every root once, and a polynomial that has none to lose keeps its own coefficients.
    [InlineData("4 0 -4 0 1", "-2 0 1")]
    [InlineData("-1 3 -3 1", "-1 1")]
    [InlineData("6 -5 -2 1", "6 -5 -2 1")]
    [InlineData("4 -4 1", "-2 1")]
    [InlineData("2 4 2", "1 1")]
    // 4x² − 4x + 1 = (2x − 1)²: dividing by 2x − 1 has a factor of 2 to divide by, not 1.
    [InlineData("1 -4 4", "-1 2")]
    // A constant keeps no sign of its own, whichever it had.
    [InlineData("-5", "1")]
    [InlineData("7", "1")]
    public void TheSquareFreePartDropsRepeatedFactors(string coefficients, string expected)
    {
        IntegerPolynomial.SquareFree(Parse(coefficients)).Should().Equal(Parse(expected));
    }

    [Fact]
    public void TheSquareFreePartOfTheZeroPolynomialIsEmpty()
    {
        IntegerPolynomial.SquareFree(Parse("0")).Should().BeEmpty();
        IntegerPolynomial.Degree(IntegerPolynomial.SquareFree(Parse("0"))).Should().Be(-1);
    }

    [Theory]
    [InlineData("-2 0 1", 2)]
    [InlineData("1 0 1", 0)]
    [InlineData("4 -4 1", 1)]
    [InlineData("6 -5 -2 1", 3)]
    [InlineData("-2 0 0 1", 1)]
    [InlineData("4 0 -4 0 1", 2)]
    [InlineData("24 -50 35 -10 1", 4)]
    [InlineData("5", 0)]
    [InlineData("0", 0)]
    public void TheNumberOfDistinctRealRoots(string coefficients, int count)
    {
        IntegerPolynomial.CountRealRoots(Parse(coefficients)).Should().Be(count);
    }

    [Fact]
    public void DividingByARationalRootIsExact()
    {
        // 6x² − 5x + 1 = (2x − 1)(3x − 1), so dividing by (2x − 1) leaves 3x − 1.
        IntegerPolynomial.Deflate(Parse("1 -5 6"), 1, 2).Should().Equal(Parse("-1 3"));
        IntegerPolynomial.Deflate(Parse("6 -5 -2 1"), 1, 1).Should().Equal(Parse("-6 -1 1"));
        IntegerPolynomial.Deflate(Parse("6 -5 -2 1"), -2, 1).Should().Equal(Parse("3 -4 1"));

        // A rational that is not in lowest terms is the same root: 2/4 is 1/2, and −6/3 is −2.
        IntegerPolynomial.Deflate(Parse("1 -5 6"), 2, 4).Should().Equal(Parse("-1 3"));
        IntegerPolynomial.Deflate(Parse("6 -5 -2 1"), -6, 3).Should().Equal(Parse("3 -4 1"));
    }

    [Fact]
    public void ARationalThatIsNotARoot_IsRefused()
    {
        Action notARoot = () => IntegerPolynomial.Deflate(Parse("6 -5 -2 1"), 2, 1);
        Action constant = () => IntegerPolynomial.Deflate(Parse("0"), 1, 1);

        notARoot.Should().Throw<ArgumentException>().WithParameterName("numerator");
        constant.Should().Throw<ArgumentException>().WithParameterName("numerator");
    }

    [Fact]
    public void NoArgumentMayBeNullAndADenominatorIsPositive()
    {
        Action degree = () => IntegerPolynomial.Degree(null!);
        Action derivative = () => IntegerPolynomial.Derivative(null!);
        Action squareFree = () => IntegerPolynomial.SquareFree(null!);
        Action count = () => IntegerPolynomial.CountRealRoots(null!);
        Action sign = () => IntegerPolynomial.SignAt(null!, 1, 1);
        Action deflate = () => IntegerPolynomial.Deflate(null!, 1, 1);
        Action zero = () => IntegerPolynomial.SignAt(Parse("1 1"), 1, 0);
        Action negative = () => IntegerPolynomial.SignAt(Parse("1 1"), 1, -2);

        degree.Should().Throw<ArgumentNullException>();
        derivative.Should().Throw<ArgumentNullException>();
        squareFree.Should().Throw<ArgumentNullException>();
        count.Should().Throw<ArgumentNullException>();
        sign.Should().Throw<ArgumentNullException>();
        deflate.Should().Throw<ArgumentNullException>();
        zero.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("denominator");
        negative.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("denominator");
    }

    [Fact]
    public void APolynomialBuiltFromRoots_HasThoseRoots()
    {
        // Up to four integer roots, some of them repeated: the count is the distinct ones, the sign is 0 at each of
        // them, and dividing by one leaves a polynomial with the rest.
        Gen.Int[-9, 9].Array[1, 4]
            .Sample(
                roots =>
                {
                    BigInteger[] polynomial = FromRoots(roots);

                    IntegerPolynomial.CountRealRoots(polynomial).Should().Be(roots.Distinct().Count());
                    foreach (int root in roots)
                    {
                        IntegerPolynomial.SignAt(polynomial, root, 1).Should().Be(0);
                        bool repeated = roots.Count(other => other == root) > 1;
                        int after = IntegerPolynomial.SignAt(IntegerPolynomial.Deflate(polynomial, root, 1), root, 1);
                        (after == 0).Should().Be(repeated, "a root survives division only when it was there twice");
                    }

                    IntegerPolynomial.Degree(polynomial).Should().Be(roots.Length);
                },
                iter: 500);
    }

    [Fact]
    public void ASquareFreePartHasEveryRootOnce()
    {
        Gen.Int[-9, 9].Array[1, 4]
            .Sample(
                roots =>
                {
                    BigInteger[] squareFree = IntegerPolynomial.SquareFree(FromRoots(roots));

                    IntegerPolynomial.Degree(squareFree).Should().Be(roots.Distinct().Count());
                    foreach (int root in roots.Distinct())
                    {
                        IntegerPolynomial.SignAt(squareFree, root, 1).Should().Be(0);
                    }
                },
                iter: 500);
    }

    /// <summary>(x − r₁)···(x − rₙ), ascending.</summary>
    private static BigInteger[] FromRoots(int[] roots)
    {
        BigInteger[] polynomial = [BigInteger.One];
        foreach (int root in roots)
        {
            BigInteger[] next = new BigInteger[polynomial.Length + 1];
            for (int i = 0; i < polynomial.Length; i++)
            {
                next[i + 1] += polynomial[i];
                next[i] -= polynomial[i] * root;
            }

            polynomial = next;
        }

        return polynomial;
    }

    private static BigInteger[] Parse(string coefficients)
    {
        return [.. coefficients.Split(' ').Select(part => BigInteger.Parse(part, CultureInfo.InvariantCulture))];
    }
}
