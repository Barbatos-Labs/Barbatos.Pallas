// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using CsCheck;

namespace Barbatos.Pallas.Numerics.Tests;

public sealed class IntegerFunctionsTests
{
    [Theory]
    [InlineData(0, "1")]
    [InlineData(1, "1")]
    [InlineData(5, "120")]
    [InlineData(8, "40320")]
    [InlineData(20, "2432902008176640000")]
    [InlineData(25, "15511210043330985984000000")]
    public void Factorial_OfKnownValues(int n, string expected)
    {
        // Manual p. 58: (5 + 3)! = 40320.
        IntegerFunctions.Factorial(n).Should().Be(BigInteger.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Factorial_OfTheCalculatorsLargestArgument_Has99Digits()
    {
        IntegerFunctions.Factorial(69).ToString(CultureInfo.InvariantCulture).Should().HaveLength(99);

        Action negative = () => IntegerFunctions.Factorial(-1);
        negative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PermutationsAndCombinations_OfTheManualsExample()
    {
        // Manual p. 58: choosing 4 people from 10.
        IntegerFunctions.Permutations(10, 4).Should().Be(5040);
        IntegerFunctions.Combinations(10, 4).Should().Be(210);
    }

    [Fact]
    public void PermutationsAndCombinations_AgreeWithTheFactorialDefinitions()
    {
        Gen.Int[0, 80].SelectMany(n => Gen.Int[0, n].Select(r => (n, r))).Sample(pair =>
        {
            (int n, int r) = pair;
            BigInteger nFactorial = IntegerFunctions.Factorial(n);
            BigInteger nMinusRFactorial = IntegerFunctions.Factorial(n - r);

            return IntegerFunctions.Permutations(n, r) == nFactorial / nMinusRFactorial
                && IntegerFunctions.Combinations(n, r) == nFactorial / (IntegerFunctions.Factorial(r) * nMinusRFactorial);
        }, iter: 2_000);
    }

    [Fact]
    public void PermutationsAndCombinations_AtTheEdgesOfLong()
    {
        // A loop that ran a factor up to n would never end at long.MaxValue.
        IntegerFunctions.Permutations(long.MaxValue, 1).Should().Be(long.MaxValue);
        IntegerFunctions.Combinations(long.MaxValue, 1).Should().Be(long.MaxValue);
        IntegerFunctions.Combinations(long.MaxValue, long.MaxValue).Should().Be(BigInteger.One);
        IntegerFunctions.Permutations(5, 0).Should().Be(BigInteger.One);
    }

    [Fact]
    public void PermutationsAndCombinations_RejectInvalidSelections()
    {
        Action negativeN = () => IntegerFunctions.Permutations(-1, 0);
        Action negativeR = () => IntegerFunctions.Permutations(5, -1);
        Action rAboveN = () => IntegerFunctions.Combinations(3, 4);

        negativeN.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("n");
        negativeR.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("r");
        rAboveN.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("r");
    }

    [Theory]
    [InlineData("9", "15", "45")]
    [InlineData("-4", "6", "12")]
    [InlineData("0", "5", "0")]
    [InlineData("5", "0", "0")]
    [InlineData("0", "0", "0")]
    [InlineData("7", "7", "7")]
    public void LeastCommonMultiple_OfKnownValues(string left, string right, string expected)
    {
        // Manual p. 59: LCM(9, 15) = 45.
        IntegerFunctions.LeastCommonMultiple(BigInteger.Parse(left, CultureInfo.InvariantCulture), BigInteger.Parse(right, CultureInfo.InvariantCulture))
            .Should().Be(BigInteger.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PrimeFactors_OfTheManualsExamples()
    {
        // Manual p. 45: 1014 = 2 × 3 × 13², and 2036162 = 2 × 1009².
        IntegerFunctions.PrimeFactors(1014).Should().Equal((2L, 1), (3L, 1), (13L, 2));
        IntegerFunctions.PrimeFactors(2036162).Should().Equal((2L, 1), (1009L, 2));
        IntegerFunctions.PrimeFactors(1).Should().BeEmpty();
        IntegerFunctions.PrimeFactors(1024).Should().Equal((2L, 10));
        IntegerFunctions.PrimeFactors(999_999_937).Should().Equal((999_999_937L, 1));
        IntegerFunctions.PrimeFactors(600_851_475_143).Should().Equal((71L, 1), (839L, 1), (1471L, 1), (6857L, 1));
    }

    [Fact]
    public void PrimeFactors_MultiplyBackToTheValue()
    {
        Gen.Long[1, 1_000_000_000_000].Sample(value =>
        {
            IReadOnlyList<(long Prime, int Exponent)> factors = IntegerFunctions.PrimeFactors(value);
            BigInteger product = factors.Aggregate(BigInteger.One, (total, factor) => total * BigInteger.Pow(factor.Prime, factor.Exponent));
            return product == value && factors.Select(factor => factor.Prime).SequenceEqual(factors.Select(factor => factor.Prime).Order());
        }, iter: 500);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-5L)]
    public void PrimeFactors_RejectsNonPositiveValues(long value)
    {
        Action act = () => IntegerFunctions.PrimeFactors(value);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(nameof(value));
    }
}
