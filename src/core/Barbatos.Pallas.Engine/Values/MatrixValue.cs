// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// An immutable matrix of real values, as the Matrix application holds in MatA-MatD and MatAns (manual pp. 132-139).
/// </summary>
/// <remarks>
/// Each entry is a <see cref="Value"/>, so it follows the precision rule and keeps an exact form: the entries of
/// <c>MatA⁻¹</c> for an exact MatA are exact decimals, and <c>√(2)</c> squared is exactly 2 inside a matrix too. The size
/// limits belong to the profile and are checked where matrices enter a session.
/// </remarks>
public sealed class MatrixValue : IEquatable<MatrixValue>
{
    private readonly ImmutableArray<Value> _entries;

    /// <summary>Initializes a matrix.</summary>
    /// <param name="entries">The entries, by row and column; real numbers only.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The matrix is empty, or an entry is not a real number.</exception>
    public MatrixValue(Value[,] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Rows = entries.GetLength(0);
        Columns = entries.GetLength(1);
        if (Rows == 0 || Columns == 0)
        {
            throw new ArgumentException("A matrix has at least one row and one column.", nameof(entries));
        }

        ImmutableArray<Value>.Builder builder = ImmutableArray.CreateBuilder<Value>(Rows * Columns);
        foreach (Value entry in entries)
        {
            builder.Add(entry.IsReal ? entry : throw new ArgumentException("A matrix holds real numbers only.", nameof(entries)));
        }

        _entries = builder.MoveToImmutable();
    }

    private MatrixValue(int rows, int columns, ImmutableArray<Value> entries)
    {
        Rows = rows;
        Columns = columns;
        _entries = entries;
    }

    /// <summary>Gets the number of rows.</summary>
    public int Rows { get; }

    /// <summary>Gets the number of columns.</summary>
    public int Columns { get; }

    /// <summary>Gets whether the matrix has as many rows as columns.</summary>
    public bool IsSquare => Rows == Columns;

    /// <summary>Gets an entry.</summary>
    /// <param name="row">The row, from 0.</param>
    /// <param name="column">The column, from 0.</param>
    /// <returns>The entry.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The position is outside the matrix.</exception>
    public Value this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
            return _entries[(row * Columns) + column];
        }
    }

    /// <summary>Returns the entries as a new array.</summary>
    /// <returns>The entries, by row and column.</returns>
    public Value[,] ToArray()
    {
        Value[,] result = new Value[Rows, Columns];
        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                result[row, column] = _entries[(row * Columns) + column];
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public bool Equals(MatrixValue? other)
    {
        return other is not null && Rows == other.Rows && Columns == other.Columns && _entries.SequenceEqual(other._entries);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as MatrixValue);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // The entries in order and the number of columns determine the number of rows.
        HashCode hash = default;
        hash.Add(Columns);
        foreach (Value entry in _entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns the entries in the invariant culture, row by row, for diagnostics.</summary>
    /// <returns>The text, as <c>[[2, 1], [1, 1]]</c>.</returns>
    public override string ToString()
    {
        IEnumerable<string> rows = Enumerable.Range(0, Rows)
            .Select(row => "[" + string.Join(", ", Enumerable.Range(0, Columns).Select(column => this[row, column].ToString())) + "]");
        return "[" + string.Join(", ", rows) + "]";
    }

    /// <summary>Creates a matrix from entries already known to be real, without copying.</summary>
    internal static MatrixValue FromEntries(int rows, int columns, ImmutableArray<Value> entries) => new(rows, columns, entries);
}
