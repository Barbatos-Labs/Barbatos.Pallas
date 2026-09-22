// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.LinearAlgebra;

/// <summary>
/// Exact determinants, inverses and solutions of linear systems of <see cref="decimal"/> matrices.
/// </summary>
/// <remarks>
/// <para>
/// .NET has no linear algebra on <see cref="decimal"/>, and elimination in <see cref="decimal"/> rounds at every division:
/// a determinant of exactly 0 comes out as about 10⁻²⁷, and no threshold can tell that from a small nonzero one. A
/// <see cref="decimal"/> is an integer mantissa over a power of ten, so each row is multiplied by its own power of ten
/// into integers, and the integer matrix is eliminated without fractions (Bareiss, 1968) on <see cref="BigInteger"/>.
/// Every intermediate division is exact, so a determinant is 0 exactly when the matrix is singular.
/// </para>
/// <para>
/// Results are returned as numerators over a common denominator, and rounding to <see cref="decimal"/> happens once, in
/// the caller. There is no rational number type: the fraction exists only as the two integers of a result.
/// </para>
/// </remarks>
public static class ExactLinearAlgebra
{
    /// <summary>Returns the determinant of a square matrix, exactly.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The determinant as <c>Numerator / Denominator</c>, in lowest terms, with a positive denominator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="matrix"/> is empty or not square.</exception>
    public static (BigInteger Numerator, BigInteger Denominator) Determinant(decimal[,] matrix)
    {
        int size = RequireSquare(matrix);
        (BigInteger[,] integers, BigInteger[] scales) = ToIntegers(matrix, null);
        BigInteger denominator = BigInteger.One;
        foreach (BigInteger scale in scales)
        {
            denominator *= scale;
        }

        return Reduced(EliminatedDeterminant(integers, size), denominator);
    }

    /// <summary>Returns the inverse of a square matrix, exactly.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="numerators">The inverse times <paramref name="denominator"/>; empty when the matrix is singular.</param>
    /// <param name="denominator">The common denominator, positive; 0 when the matrix is singular.</param>
    /// <returns><see langword="true"/> unless the matrix is singular.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="matrix"/> is empty or not square.</exception>
    public static bool TryInvert(decimal[,] matrix, out BigInteger[,] numerators, out BigInteger denominator)
    {
        int size = RequireSquare(matrix);
        (BigInteger[,] integers, BigInteger[] scales) = ToIntegers(matrix, null);

        // [N | I]: the right half ends as det(N)·N⁻¹. With N = S·A for the row scales S, A⁻¹ = N⁻¹·S, so column j of the
        // result is multiplied by the scale of row j.
        BigInteger[,] augmented = new BigInteger[size, 2 * size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                augmented[row, column] = integers[row, column];
            }

            augmented[row, size + row] = BigInteger.One;
        }

        BigInteger diagonal = EliminateGaussJordan(augmented, size);
        if (diagonal.IsZero)
        {
            numerators = new BigInteger[0, 0];
            denominator = BigInteger.Zero;
            return false;
        }

        numerators = new BigInteger[size, size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                numerators[row, column] = augmented[row, size + column] * scales[column];
            }
        }

        denominator = Reduce(numerators, diagonal);
        return true;
    }

    /// <summary>Solves the linear system <c>A·x = b</c> exactly.</summary>
    /// <param name="coefficients">The square matrix <c>A</c>.</param>
    /// <param name="constants">The right-hand side <c>b</c>, one entry per row.</param>
    /// <param name="numerators">The solution times <paramref name="denominator"/>; empty when <c>A</c> is singular.</param>
    /// <param name="denominator">The common denominator, positive; 0 when <c>A</c> is singular.</param>
    /// <returns><see langword="true"/> when the system has exactly one solution.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="coefficients"/> is empty or not square, or <paramref name="constants"/> has the wrong length.</exception>
    public static bool TrySolve(decimal[,] coefficients, decimal[] constants, out BigInteger[] numerators, out BigInteger denominator)
    {
        int size = RequireSquare(coefficients);
        ArgumentNullException.ThrowIfNull(constants);
        if (constants.Length != size)
        {
            throw new ArgumentException("There must be one constant per row of the coefficients.", nameof(constants));
        }

        // Each row, constant included, is scaled by the same power of ten, which leaves the solution unchanged.
        (BigInteger[,] integers, _) = ToIntegers(coefficients, constants);
        return TrySolve(integers, out numerators, out denominator);
    }

    /// <summary>Solves the linear system whose augmented matrix is given in integers.</summary>
    /// <param name="augmented">n rows of n + 1 integers: the coefficients of the row and its constant.</param>
    /// <param name="numerators">The solution times <paramref name="denominator"/>; empty when the system is singular.</param>
    /// <param name="denominator">The common denominator, positive; 0 when the system is singular.</param>
    /// <returns><see langword="true"/> when the system has exactly one solution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="augmented"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="augmented"/> is empty, or is not one column wider than it is tall.</exception>
    /// <remarks>
    /// Multiplying a row by a nonzero number leaves the solution where it is, so a caller whose values are not
    /// <see cref="decimal"/> can scale each row into integers itself and keep the solution exact.
    /// </remarks>
    public static bool TrySolve(BigInteger[,] augmented, out BigInteger[] numerators, out BigInteger denominator)
    {
        ArgumentNullException.ThrowIfNull(augmented);
        int size = augmented.GetLength(0);
        if (size == 0 || augmented.GetLength(1) != size + 1)
        {
            throw new ArgumentException("The augmented matrix must have one more column than it has rows, and not be empty.", nameof(augmented));
        }

        BigInteger[,] work = (BigInteger[,])augmented.Clone();
        BigInteger diagonal = EliminateGaussJordan(work, size);
        if (diagonal.IsZero)
        {
            numerators = [];
            denominator = BigInteger.Zero;
            return false;
        }

        BigInteger[,] solution = new BigInteger[size, 1];
        for (int row = 0; row < size; row++)
        {
            solution[row, 0] = work[row, size];
        }

        denominator = Reduce(solution, diagonal);
        numerators = new BigInteger[size];
        for (int row = 0; row < size; row++)
        {
            numerators[row] = solution[row, 0];
        }

        return true;
    }

    /// <summary>Returns the rank of an integer matrix, exactly.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>The number of independent rows.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Row echelon form without divisions: a row is combined with the pivot row after both are multiplied, which keeps
    /// the entries integers and the rank where it was. It tells a system with no solution from one with infinitely many:
    /// the coefficients and the augmented matrix have the same rank exactly when a solution exists.
    /// </remarks>
    public static int Rank(BigInteger[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        BigInteger[,] work = (BigInteger[,])matrix.Clone();
        int rank = 0;
        for (int column = 0; column < columns && rank < rows; column++)
        {
            int pivot = PivotRow(work, rank, rows, column);
            if (pivot == rows)
            {
                continue;
            }

            SwapRows(work, rank, pivot);
            for (int i = rank + 1; i < rows; i++)
            {
                BigInteger divisor = BigInteger.GreatestCommonDivisor(work[rank, column], work[i, column]);
                BigInteger scale = work[rank, column] / divisor;
                BigInteger factor = work[i, column] / divisor;
                for (int j = column; j < columns; j++)
                {
                    work[i, j] = (work[i, j] * scale) - (work[rank, j] * factor);
                }
            }

            rank++;
        }

        return rank;
    }

    private static int RequireSquare(decimal[,] matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        int rows = matrix.GetLength(0);
        if (rows == 0 || rows != matrix.GetLength(1))
        {
            throw new ArgumentException("The matrix must be square and not empty.", nameof(matrix));
        }

        return rows;
    }

    /// <summary>Multiplies each row by the power of ten that makes all of its entries integers.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <param name="column">An extra column that shares the row scales, or <see langword="null"/>.</param>
    private static (BigInteger[,] Integers, BigInteger[] Scales) ToIntegers(decimal[,] matrix, decimal[]? column)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        int width = columns + (column is null ? 0 : 1);
        BigInteger[,] integers = new BigInteger[rows, width];
        BigInteger[] scales = new BigInteger[rows];
        for (int row = 0; row < rows; row++)
        {
            int scale = column is null ? 0 : column[row].Scale;
            for (int j = 0; j < columns; j++)
            {
                scale = Math.Max(scale, matrix[row, j].Scale);
            }

            scales[row] = BigInteger.Pow(10, scale);
            for (int j = 0; j < width; j++)
            {
                decimal entry = j < columns ? matrix[row, j] : column![row];
                integers[row, j] = Mantissa(entry) * BigInteger.Pow(10, scale - entry.Scale);
            }
        }

        return (integers, scales);
    }

    /// <summary>The integer a <see cref="decimal"/> is, before its power of ten: 2.50 is 250.</summary>
    private static BigInteger Mantissa(decimal value)
    {
        Span<int> bits = stackalloc int[4];
        _ = decimal.GetBits(value, bits);
        BigInteger magnitude = ((BigInteger)(uint)bits[2] << 64) | ((BigInteger)(uint)bits[1] << 32) | (uint)bits[0];
        return decimal.IsNegative(value) ? -magnitude : magnitude;
    }

    /// <summary>Bareiss elimination: the determinant of an integer matrix, destroying it.</summary>
    /// <remarks>
    /// Each step divides by the previous pivot, and Bareiss's theorem makes that division exact: the entries after step k
    /// are minors of the original matrix. The last pivot is the determinant, up to the sign of the row swaps.
    /// </remarks>
    private static BigInteger EliminatedDeterminant(BigInteger[,] matrix, int size)
    {
        BigInteger previous = BigInteger.One;
        bool negate = false;
        for (int k = 0; k < size; k++)
        {
            int pivot = PivotRow(matrix, k, size, k);
            if (pivot == size)
            {
                return BigInteger.Zero;
            }

            SwapRows(matrix, k, pivot);
            negate ^= pivot != k;
            for (int i = k + 1; i < size; i++)
            {
                for (int j = k + 1; j < size; j++)
                {
                    matrix[i, j] = ((matrix[i, j] * matrix[k, k]) - (matrix[i, k] * matrix[k, j])) / previous;
                }
            }

            previous = matrix[k, k];
        }

        return negate ? -previous : previous;
    }

    /// <summary>
    /// Fraction-free Gauss–Jordan elimination of the first <paramref name="size"/> columns, destroying the matrix.
    /// </summary>
    /// <returns>
    /// The common value of the diagonal, ± the determinant, with every other column then that value times the solution;
    /// 0 when the matrix is singular.
    /// </returns>
    private static BigInteger EliminateGaussJordan(BigInteger[,] matrix, int size)
    {
        int width = matrix.GetLength(1);
        BigInteger previous = BigInteger.One;
        for (int k = 0; k < size; k++)
        {
            int pivot = PivotRow(matrix, k, size, k);
            if (pivot == size)
            {
                return BigInteger.Zero;
            }

            SwapRows(matrix, k, pivot);
            for (int i = 0; i < size; i++)
            {
                if (i == k)
                {
                    continue;
                }

                for (int j = 0; j < width; j++)
                {
                    if (j != k)
                    {
                        // Exact, as in Bareiss elimination.
                        matrix[i, j] = ((matrix[k, k] * matrix[i, j]) - (matrix[i, k] * matrix[k, j])) / previous;
                    }
                }

                matrix[i, k] = BigInteger.Zero;
            }

            previous = matrix[k, k];
        }

        return previous;
    }

    /// <summary>The first row from <paramref name="row"/> down with a nonzero entry in <paramref name="column"/>, or <paramref name="lastRow"/>.</summary>
    private static int PivotRow(BigInteger[,] matrix, int row, int lastRow, int column)
    {
        int pivot = row;
        while (pivot < lastRow && matrix[pivot, column].IsZero)
        {
            pivot++;
        }

        return pivot;
    }

    private static void SwapRows(BigInteger[,] matrix, int first, int second)
    {
        for (int j = 0; j < matrix.GetLength(1); j++)
        {
            (matrix[first, j], matrix[second, j]) = (matrix[second, j], matrix[first, j]);
        }
    }

    /// <summary>Divides the numerators and the denominator by their common factor, and makes the denominator positive.</summary>
    private static BigInteger Reduce(BigInteger[,] numerators, BigInteger denominator)
    {
        BigInteger divisor = denominator;
        foreach (BigInteger numerator in numerators)
        {
            divisor = BigInteger.GreatestCommonDivisor(divisor, numerator);
        }

        divisor = BigInteger.CopySign(divisor, denominator);
        for (int row = 0; row < numerators.GetLength(0); row++)
        {
            for (int column = 0; column < numerators.GetLength(1); column++)
            {
                numerators[row, column] /= divisor;
            }
        }

        return denominator / divisor;
    }

    private static (BigInteger Numerator, BigInteger Denominator) Reduced(BigInteger numerator, BigInteger denominator)
    {
        BigInteger divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        return (numerator / divisor, denominator / divisor);
    }
}
