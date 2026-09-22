// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

namespace Barbatos.Pallas.Statistics;

/// <summary>
/// Where the quartiles of sorted data are: the ranks whose values' mean is the quartile.
/// </summary>
/// <remarks>
/// <para>
/// The median is the middle value, or the mean of the two middle values. The first and third quartiles are the medians of
/// the lower and upper halves, without the median itself when the count is odd. The manual gives no formula; its 1-Var
/// Results screen of p. 84 shows Q1 = 4, Med = 6.5 and Q3 = 8 for 20 values, which this rule gives (assumption U1 of
/// docs/CONFORMANCE.md). A single value is all three quartiles.
/// </para>
/// <para>
/// .NET has no quartiles. The ranks are <see cref="BigInteger"/> because a frequency can be large: 1 with frequency 10³⁰
/// is 10³⁰ values.
/// </para>
/// </remarks>
public static class Quartiles
{
    /// <summary>Returns the ranks, from 1, of the sorted values whose mean is a quartile.</summary>
    /// <param name="count">The number of values, 1 or more.</param>
    /// <param name="quartile">The quartile.</param>
    /// <returns>The two ranks; equal when the quartile is one value.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1, or <paramref name="quartile"/> is not defined.</exception>
    public static (BigInteger Lower, BigInteger Upper) Ranks(BigInteger count, Quartile quartile)
    {
        if (count.Sign <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "There must be at least one value.");
        }

        BigInteger half = count / 2;
        return quartile switch
        {
            Quartile.Median => Middle(count, BigInteger.Zero),
            Quartile.First => half.IsZero ? (BigInteger.One, BigInteger.One) : Middle(half, BigInteger.Zero),
            Quartile.Third => half.IsZero ? (BigInteger.One, BigInteger.One) : Middle(half, count - half),
            _ => throw new ArgumentOutOfRangeException(nameof(quartile), quartile, "Not a quartile."),
        };
    }

    /// <summary>The middle rank or ranks of <paramref name="count"/> values that follow <paramref name="offset"/> others.</summary>
    private static (BigInteger Lower, BigInteger Upper) Middle(BigInteger count, BigInteger offset)
    {
        return count.IsEven ? (offset + (count / 2), offset + (count / 2) + 1) : (offset + ((count + 1) / 2), offset + ((count + 1) / 2));
    }
}
