// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Globalization;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The data of the Statistics application: x values, or (x, y) pairs, with a frequency for each row when the Frequency
/// setting is on (manual pp. 79-83).
/// </summary>
/// <remarks>
/// <para>
/// The data are immutable: editing and sorting make new data, which <see cref="CalculatorSession.SetStatisticsData"/>
/// stores. The editor's own rules - switching between one and two variables, or turning the frequency column on or off,
/// clears the data (p. 80) - belong to the host, which decides when to start again from <see cref="Empty"/>.
/// </para>
/// <para>
/// Every value is a real number. A frequency may be any real number here; a negative one is a Math ERROR when a statistic
/// is calculated, a row with frequency 0 counts for nothing, and quartiles need whole frequencies (assumption U22).
/// </para>
/// </remarks>
public sealed class StatisticsData : IEquatable<StatisticsData>
{
    /// <summary>Initializes data.</summary>
    /// <param name="x">The x values.</param>
    /// <param name="y">The y values, one per x value, for two-variable data; <see langword="null"/> for one variable.</param>
    /// <param name="frequencies">The frequency of each row; <see langword="null"/> when the Frequency setting is off.</param>
    /// <exception cref="ArgumentNullException"><paramref name="x"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A value is not a real number, or a column has the wrong length.</exception>
    public StatisticsData(IEnumerable<Value> x, IEnumerable<Value>? y = null, IEnumerable<Value>? frequencies = null)
    {
        ArgumentNullException.ThrowIfNull(x);
        X = Column(x, -1, nameof(x));
        Y = y is null ? [] : Column(y, X.Length, nameof(y));
        Frequencies = frequencies is null ? [] : Column(frequencies, X.Length, nameof(frequencies));
        IsTwoVariable = y is not null;
        HasFrequencies = frequencies is not null;
    }

    /// <summary>Gets one-variable data with no rows and no frequency column.</summary>
    public static StatisticsData Empty { get; } = new([]);

    /// <summary>Gets the x values.</summary>
    public ImmutableArray<Value> X { get; }

    /// <summary>Gets the y values; empty for one-variable data.</summary>
    public ImmutableArray<Value> Y { get; }

    /// <summary>Gets the frequencies; empty without the frequency column.</summary>
    public ImmutableArray<Value> Frequencies { get; }

    /// <summary>Gets whether the data are (x, y) pairs.</summary>
    public bool IsTwoVariable { get; }

    /// <summary>Gets whether each row has a frequency.</summary>
    public bool HasFrequencies { get; }

    /// <summary>Gets the number of rows.</summary>
    public int Rows => X.Length;

    /// <summary>Gets the number of columns, 1 to 3, which sets how many rows the editor holds (p. 80).</summary>
    public int Columns => 1 + (IsTwoVariable ? 1 : 0) + (HasFrequencies ? 1 : 0);

    /// <summary>Returns the data sorted by a column, rows kept together (p. 82). Equal values keep their order.</summary>
    /// <param name="column">The column to sort by.</param>
    /// <param name="descending">Whether to sort from the largest value down.</param>
    /// <returns>The sorted data.</returns>
    /// <exception cref="ArgumentException">The data have no such column.</exception>
    public StatisticsData Sort(StatisticsColumn column, bool descending = false)
    {
        ImmutableArray<Value> key = column switch
        {
            StatisticsColumn.X => X,
            StatisticsColumn.Y when IsTwoVariable => Y,
            StatisticsColumn.Frequency when HasFrequencies => Frequencies,
            _ => throw new ArgumentException($"The data have no {column} column.", nameof(column)),
        };

        IEnumerable<int> rows = Enumerable.Range(0, Rows);
        int[] order = [.. descending ? rows.OrderByDescending(row => key[row], RealOrder.Instance) : rows.OrderBy(row => key[row], RealOrder.Instance)];
        return new StatisticsData(
            order.Select(row => X[row]),
            IsTwoVariable ? order.Select(row => Y[row]) : null,
            HasFrequencies ? order.Select(row => Frequencies[row]) : null);
    }

    /// <inheritdoc/>
    public bool Equals(StatisticsData? other)
    {
        return other is not null && IsTwoVariable == other.IsTwoVariable && HasFrequencies == other.HasFrequencies
            && X.SequenceEqual(other.X) && Y.SequenceEqual(other.Y) && Frequencies.SequenceEqual(other.Frequencies);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as StatisticsData);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        // The flags and the values in order; the columns' lengths follow from the number of values.
        HashCode hash = default;
        hash.Add(Columns);
        foreach (Value value in X.Concat(Y).Concat(Frequencies))
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns the rows in the invariant culture, for diagnostics.</summary>
    /// <returns>The text, as <c>[[170, 66], [179, 75]]</c>.</returns>
    public override string ToString()
    {
        return "[" + string.Join(", ", Enumerable.Range(0, Rows).Select(row => "[" + string.Join(", ", Row(row)) + "]")) + "]";
    }

    private IEnumerable<string> Row(int row)
    {
        yield return X[row].ToString();
        if (IsTwoVariable)
        {
            yield return Y[row].ToString();
        }

        if (HasFrequencies)
        {
            yield return Frequencies[row].ToString();
        }
    }

    private static ImmutableArray<Value> Column(IEnumerable<Value> values, int length, string name)
    {
        ImmutableArray<Value> column = [.. values];
        if (length >= 0 && column.Length != length)
        {
            throw new ArgumentException(string.Create(CultureInfo.InvariantCulture, $"There must be one value per x value, {length}."), name);
        }

        foreach (Value value in column)
        {
            if (!value.IsReal)
            {
                throw new ArgumentException("Statistics data are real numbers.", name);
            }
        }

        return column;
    }
}
