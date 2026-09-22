// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using CsCheck;

namespace Barbatos.Pallas.LinearAlgebra.Tests;

/// <summary>
/// Exact determinants, inverses and solutions, checked against exact integer arithmetic.
/// </summary>
public sealed class ExactLinearAlgebraTests
{
    // Entries with two decimals, so 100·A is an integer matrix the tests can check exactly.
    private static readonly Gen<decimal> Entry = Gen.Int[-9999, 9999].Select(hundredths => hundredths / 100m);

    private static readonly Gen<decimal[,]> Square = Gen.Int[1, 5].SelectMany(size => Entry.Array2D[size, size]);

    [Fact]
    public void TheDeterminantsOfTheManual()
    {
        // p. 138: det of MatA is 1; MatB of p. 137 has det 2 − 6 = −4.
        ExactLinearAlgebra.Determinant(new decimal[,] { { 2, 1 }, { 1, 1 } }).Should().Be((BigInteger.One, BigInteger.One));
        ExactLinearAlgebra.Determinant(new decimal[,] { { 2, 3 }, { 2, 1 } }).Should().Be((new BigInteger(-4), BigInteger.One));
        ExactLinearAlgebra.Determinant(new decimal[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } }).Should().Be((BigInteger.Zero, BigInteger.One));
    }

    [Theory]
    // A row swap changes the sign; a zero pivot is swapped away, not a zero determinant.
    [InlineData(new[] { 0, 1, 1, 0 }, -1)]
    [InlineData(new[] { 0, 0, 1, 0 }, 0)]
    [InlineData(new[] { 0, 1, 0, 2 }, 0)]
    [InlineData(new[] { 5 }, 5)]
    [InlineData(new[] { 0, 2, 1, 1, 1, 1, 2, 1, 3 }, -3)]
    // Two swaps: the sign changes back.
    [InlineData(new[] { 0, 1, 0, 0, 0, 1, 1, 0, 0 }, 1)]
    public void DeterminantsNeedingPivots(int[] entries, int expected)
    {
        int size = (int)Math.Sqrt(entries.Length);
        decimal[,] matrix = new decimal[size, size];
        for (int i = 0; i < entries.Length; i++)
        {
            matrix[i / size, i % size] = entries[i];
        }

        ExactLinearAlgebra.Determinant(matrix).Should().Be((new BigInteger(expected), BigInteger.One));
    }

    [Fact]
    public void ADecimalMatrixIsSingularExactly()
    {
        // Elimination in decimal leaves 0.6 − 0.2·(0.3/0.1)… about 10⁻²⁸ here; the integers leave exactly 0.
        decimal[,] matrix = { { 0.1m, 0.2m }, { 0.3m, 0.6m } };

        ExactLinearAlgebra.Determinant(matrix).Numerator.Should().Be(BigInteger.Zero);
        ExactLinearAlgebra.TryInvert(matrix, out BigInteger[,] numerators, out BigInteger denominator).Should().BeFalse();
        numerators.Length.Should().Be(0);
        denominator.Should().Be(BigInteger.Zero);
    }

    [Fact]
    public void EveryBitOfTheMantissaIsRead()
    {
        // A decimal's mantissa has 96 bits; 2⁴⁰ + 0.5 needs the middle 32 of them, decimal.MaxValue all three words.
        BigInteger largest = BigInteger.Pow(2, 96) - 1;

        ExactLinearAlgebra.Determinant(new[,] { { decimal.MaxValue } }).Should().Be((largest, BigInteger.One));
        ExactLinearAlgebra.Determinant(new[,] { { decimal.MinValue } }).Should().Be((-largest, BigInteger.One));
        ExactLinearAlgebra.Determinant(new[,] { { 1099511627776.5m } }).Should().Be((new BigInteger(2199023255553), new BigInteger(2)));
    }

    [Fact]
    public void ADeterminantIsInLowestTerms()
    {
        // det [[0.5, 0.25], [1, 3]] = 1.5 − 0.25 = 1.25 = 5/4.
        ExactLinearAlgebra.Determinant(new decimal[,] { { 0.5m, 0.25m }, { 1, 3 } }).Should().Be((new BigInteger(5), new BigInteger(4)));
        ExactLinearAlgebra.Determinant(new decimal[,] { { -0.5m } }).Should().Be((BigInteger.MinusOne, new BigInteger(2)));
    }

    [Fact]
    public void TheInverseOfTheManual()
    {
        // p. 138: MatA⁻¹ = [[1, −1], [−1, 2]].
        ExactLinearAlgebra.TryInvert(new decimal[,] { { 2, 1 }, { 1, 1 } }, out BigInteger[,] numerators, out BigInteger denominator).Should().BeTrue();

        denominator.Should().Be(BigInteger.One);
        numerators.Should().BeEquivalentTo(new BigInteger[,] { { 1, -1 }, { -1, 2 } });
    }

    [Fact]
    public void AnInverseHasAPositiveDenominator()
    {
        // [[1, 2], [3, 4]]⁻¹ = [[−2, 1], [1.5, −0.5]] = [[−4, 2], [3, −1]] / 2, although the determinant is −2.
        ExactLinearAlgebra.TryInvert(new decimal[,] { { 1, 2 }, { 3, 4 } }, out BigInteger[,] numerators, out BigInteger denominator).Should().BeTrue();

        denominator.Should().Be(new BigInteger(2));
        numerators.Should().BeEquivalentTo(new BigInteger[,] { { -4, 2 }, { 3, -1 } });
    }

    [Fact]
    public void AnInverseNeedingRowSwaps()
    {
        ExactLinearAlgebra.TryInvert(new decimal[,] { { 0, 1 }, { 1, 0 } }, out BigInteger[,] swap, out BigInteger one).Should().BeTrue();
        one.Should().Be(BigInteger.One);
        swap.Should().BeEquivalentTo(new BigInteger[,] { { 0, 1 }, { 1, 0 } });

        // The second pivot is 0 after the first step: [[1, 1, 0], [1, 1, 1], [0, 1, 1]]⁻¹ = [[0, 1, −1], [1, −1, 1], [−1, 1, 0]].
        ExactLinearAlgebra.TryInvert(new decimal[,] { { 1, 1, 0 }, { 1, 1, 1 }, { 0, 1, 1 } }, out BigInteger[,] numerators, out BigInteger denominator).Should().BeTrue();
        denominator.Should().Be(BigInteger.One);
        numerators.Should().BeEquivalentTo(new BigInteger[,] { { 0, 1, -1 }, { 1, -1, 1 }, { -1, 1, 0 } });
    }

    [Fact]
    public void AZeroColumnIsSingular()
    {
        decimal[,] matrix = { { 0, 1 }, { 0, 2 } };

        ExactLinearAlgebra.TryInvert(matrix, out _, out _).Should().BeFalse();
        ExactLinearAlgebra.TrySolve(matrix, [1, 2], out _, out _).Should().BeFalse();
    }

    [Fact]
    public void AnInverseOfDecimalsScalesItsColumns()
    {
        // [[0.5, 0], [0, 4]]⁻¹ = [[2, 0], [0, 0.25]] = [[8, 0], [0, 1]] / 4.
        ExactLinearAlgebra.TryInvert(new decimal[,] { { 0.5m, 0 }, { 0, 4 } }, out BigInteger[,] numerators, out BigInteger denominator).Should().BeTrue();

        denominator.Should().Be(new BigInteger(4));
        numerators.Should().BeEquivalentTo(new BigInteger[,] { { 8, 0 }, { 0, 1 } });
    }

    [Fact]
    public void ASystemIsSolvedExactly()
    {
        // p. 115: x − y + z = 2, x + y − z = 0, −x + y + z = 4 has x = 1, y = 2, z = 3.
        decimal[,] coefficients = { { 1, -1, 1 }, { 1, 1, -1 }, { -1, 1, 1 } };

        ExactLinearAlgebra.TrySolve(coefficients, [2, 0, 4], out BigInteger[] numerators, out BigInteger denominator).Should().BeTrue();

        denominator.Should().Be(BigInteger.One);
        numerators.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void ASystemWithDecimalConstants_IsScaledWithItsRows()
    {
        // 2x = 0.5 and 0.1y = 3: x = 1/4, y = 30.
        ExactLinearAlgebra.TrySolve(new decimal[,] { { 2, 0 }, { 0, 0.1m } }, [0.5m, 3], out BigInteger[] numerators, out BigInteger denominator).Should().BeTrue();

        denominator.Should().Be(new BigInteger(4));
        numerators.Should().Equal(1, 120);
    }

    [Fact]
    public void ASingularSystem_HasNoSolution()
    {
        ExactLinearAlgebra.TrySolve(new decimal[,] { { 1, 2 }, { 2, 4 } }, [1, 2], out BigInteger[] numerators, out BigInteger denominator).Should().BeFalse();

        numerators.Should().BeEmpty();
        denominator.Should().Be(BigInteger.Zero);
    }

    [Fact]
    public void ArgumentsAreChecked()
    {
        Action none = () => ExactLinearAlgebra.Determinant(null!);
        Action empty = () => ExactLinearAlgebra.Determinant(new decimal[0, 0]);
        Action rectangular = () => ExactLinearAlgebra.TryInvert(new decimal[2, 3], out _, out _);
        Action noConstants = () => ExactLinearAlgebra.TrySolve(new decimal[,] { { 1 } }, null!, out _, out _);
        Action wrongLength = () => ExactLinearAlgebra.TrySolve(new decimal[,] { { 1 } }, [1, 2], out _, out _);
        Action noIntegers = () => ExactLinearAlgebra.TrySolve(null!, out _, out _);
        Action noColumn = () => ExactLinearAlgebra.TrySolve(new BigInteger[2, 2], out _, out _);
        Action noRows = () => ExactLinearAlgebra.TrySolve(new BigInteger[0, 1], out _, out _);
        Action noRank = () => ExactLinearAlgebra.Rank(null!);

        none.Should().Throw<ArgumentNullException>().WithParameterName("matrix");
        empty.Should().Throw<ArgumentException>().WithParameterName("matrix");
        rectangular.Should().Throw<ArgumentException>().WithParameterName("matrix");
        noConstants.Should().Throw<ArgumentNullException>().WithParameterName("constants");
        wrongLength.Should().Throw<ArgumentException>().WithParameterName("constants");
        noIntegers.Should().Throw<ArgumentNullException>().WithParameterName("augmented");
        noColumn.Should().Throw<ArgumentException>().WithParameterName("augmented");
        noRows.Should().Throw<ArgumentException>().WithParameterName("augmented");
        noRank.Should().Throw<ArgumentNullException>().WithParameterName("matrix");
    }

    [Fact]
    public void AnIntegerSystemIsSolvedWithoutDecimals()
    {
        // The rows may be scaled however the caller likes: 10x + 20y = 30 with x − y = 1 is x = 5/3, y = 2/3.
        BigInteger[,] augmented = { { 10, 20, 30 }, { 1, -1, 1 } };

        ExactLinearAlgebra.TrySolve(augmented, out BigInteger[] numerators, out BigInteger denominator).Should().BeTrue();

        numerators.Should().Equal(new BigInteger(5), new BigInteger(2));
        denominator.Should().Be(new BigInteger(3));
        augmented[0, 0].Should().Be(new BigInteger(10), "solving may not destroy the matrix it is given");
    }

    [Fact]
    public void AnIntegerSystemBeyondWhatDecimalHolds()
    {
        // 10³⁵·x = 10³⁵ has the solution 1, although neither coefficient is a decimal.
        BigInteger huge = BigInteger.Pow(10, 35);
        BigInteger[,] augmented = { { huge, huge } };

        ExactLinearAlgebra.TrySolve(augmented, out BigInteger[] numerators, out BigInteger denominator).Should().BeTrue();

        numerators.Should().Equal(BigInteger.One);
        denominator.Should().Be(BigInteger.One);
    }

    [Theory]
    // The rank counts the independent rows: a repeated row, a zero row and a zero column all lower it.
    [InlineData(new[] { 1, 0, 0, 1 }, 2, 2, 2)]
    [InlineData(new[] { 1, 2, 2, 4 }, 2, 2, 1)]
    [InlineData(new[] { 0, 0, 0, 0 }, 2, 2, 0)]
    [InlineData(new[] { 0, 1, 0, 2 }, 2, 2, 1)]
    [InlineData(new[] { 1, 2, 3, 2, 4, 6 }, 2, 3, 1)]
    [InlineData(new[] { 1, 1, 1, 2, 2, 3 }, 2, 3, 2)]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 3, 3, 2)]
    public void TheRankCountsTheIndependentRows(int[] entries, int rows, int columns, int rank)
    {
        BigInteger[,] matrix = new BigInteger[rows, columns];
        for (int i = 0; i < entries.Length; i++)
        {
            matrix[i / columns, i % columns] = entries[i];
        }

        ExactLinearAlgebra.Rank(matrix).Should().Be(rank);
    }

    [Fact]
    public void TheRankTellsNoSolutionFromInfinitelyMany()
    {
        // x + y = 1 with 2x + 2y = 3 contradicts itself: the constants raise the rank. With 2x + 2y = 2 they do not.
        BigInteger[,] coefficients = { { 1, 1 }, { 2, 2 } };
        BigInteger[,] contradiction = { { 1, 1, 1 }, { 2, 2, 3 } };
        BigInteger[,] repetition = { { 1, 1, 1 }, { 2, 2, 2 } };

        ExactLinearAlgebra.Rank(coefficients).Should().Be(1);
        ExactLinearAlgebra.Rank(contradiction).Should().Be(2);
        ExactLinearAlgebra.Rank(repetition).Should().Be(1);
        ExactLinearAlgebra.TrySolve(contradiction, out _, out _).Should().BeFalse();
        ExactLinearAlgebra.TrySolve(repetition, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TheRankIsTheSizeExactlyWhenTheSystemHasOneSolution()
    {
        Square.Sample(matrix =>
        {
            int size = matrix.GetLength(0);
            BigInteger[,] integers = Hundredfold(matrix);
            BigInteger[,] augmented = new BigInteger[size, size + 1];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    augmented[row, column] = integers[row, column];
                }

                augmented[row, size] = row + 1;
            }

            return ExactLinearAlgebra.Rank(integers) == size == ExactLinearAlgebra.TrySolve(augmented, out _, out _);
        });
    }

    [Fact]
    public void TheDeterminantIsTheLeibnizFormula()
    {
        Square.Sample(matrix =>
        {
            int size = matrix.GetLength(0);
            (BigInteger numerator, BigInteger denominator) = ExactLinearAlgebra.Determinant(matrix);

            // det(100·A) = 100ⁿ·det(A), and 100·A is an integer matrix.
            BigInteger expected = Leibniz(Hundredfold(matrix), size);
            return denominator.Sign > 0
                && BigInteger.GreatestCommonDivisor(numerator, denominator).IsOne
                && numerator * BigInteger.Pow(100, size) == expected * denominator;
        });
    }

    [Fact]
    public void AnInverseTimesTheMatrix_IsTheIdentity()
    {
        Square.Sample(matrix =>
        {
            int size = matrix.GetLength(0);
            bool invertible = ExactLinearAlgebra.TryInvert(matrix, out BigInteger[,] numerators, out BigInteger denominator);
            if (!invertible)
            {
                return ExactLinearAlgebra.Determinant(matrix).Numerator.IsZero;
            }

            // (100·A)·N = 100·d·I.
            BigInteger[,] scaled = Hundredfold(matrix);
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    BigInteger sum = BigInteger.Zero;
                    for (int k = 0; k < size; k++)
                    {
                        sum += scaled[i, k] * numerators[k, j];
                    }

                    if (sum != (i == j ? 100 * denominator : BigInteger.Zero))
                    {
                        return false;
                    }
                }
            }

            return denominator.Sign > 0;
        });
    }

    [Fact]
    public void ASolutionSatisfiesTheSystem()
    {
        Square.SelectMany(matrix => Entry.Array[matrix.GetLength(0)].Select(constants => (matrix, constants))).Sample(pair =>
        {
            (decimal[,] matrix, decimal[] constants) = pair;
            int size = matrix.GetLength(0);
            if (!ExactLinearAlgebra.TrySolve(matrix, constants, out BigInteger[] numerators, out BigInteger denominator))
            {
                return ExactLinearAlgebra.Determinant(matrix).Numerator.IsZero;
            }

            // (100·A)·x·d = 100·b·d, with x·d the numerators.
            BigInteger[,] scaled = Hundredfold(matrix);
            for (int i = 0; i < size; i++)
            {
                BigInteger sum = BigInteger.Zero;
                for (int k = 0; k < size; k++)
                {
                    sum += scaled[i, k] * numerators[k];
                }

                if (sum != (BigInteger)(constants[i] * 100m) * denominator)
                {
                    return false;
                }
            }

            return denominator.Sign > 0;
        });
    }

    [Fact]
    public void AMatrixWithDependentRows_IsSingular()
    {
        Gen.Int[2, 5].SelectMany(size => Entry.Array2D[size, size]).SelectMany(matrix => Gen.Int[-9, 9].Select(factor => (matrix, factor))).Sample(pair =>
        {
            (decimal[,] matrix, int factor) = pair;
            int size = matrix.GetLength(0);
            for (int j = 0; j < size; j++)
            {
                matrix[size - 1, j] = matrix[0, j] * factor;
            }

            return ExactLinearAlgebra.Determinant(matrix).Numerator.IsZero && !ExactLinearAlgebra.TryInvert(matrix, out _, out _);
        });
    }

    private static BigInteger[,] Hundredfold(decimal[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        BigInteger[,] result = new BigInteger[rows, columns];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                result[i, j] = (BigInteger)(matrix[i, j] * 100m);
            }
        }

        return result;
    }

    private static BigInteger Leibniz(BigInteger[,] matrix, int size)
    {
        // Laplace expansion along the first row, which the Leibniz formula is; fine up to 5×5.
        if (size == 1)
        {
            return matrix[0, 0];
        }

        BigInteger sum = BigInteger.Zero;
        for (int column = 0; column < size; column++)
        {
            BigInteger[,] minor = new BigInteger[size - 1, size - 1];
            for (int i = 1; i < size; i++)
            {
                int target = 0;
                for (int j = 0; j < size; j++)
                {
                    if (j != column)
                    {
                        minor[i - 1, target++] = matrix[i, j];
                    }
                }
            }

            BigInteger term = matrix[0, column] * Leibniz(minor, size - 1);
            sum += column % 2 == 0 ? term : -term;
        }

        return sum;
    }
}
