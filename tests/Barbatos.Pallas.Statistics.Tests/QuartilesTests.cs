// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using CsCheck;

namespace Barbatos.Pallas.Statistics.Tests;

/// <summary>
/// The quartile rule of assumption U1, against the 1-Var Results screen of p. 84 and by definition.
/// </summary>
public sealed class QuartilesTests
{
    [Theory]
    // p. 84: 20 values give Q1 = 4 (ranks 5 and 6), Med = 6.5 (10 and 11) and Q3 = 8 (15 and 16).
    [InlineData(20, Quartile.First, 5, 6)]
    [InlineData(20, Quartile.Median, 10, 11)]
    [InlineData(20, Quartile.Third, 15, 16)]
    // An odd count leaves the median out of both halves.
    [InlineData(7, Quartile.First, 2, 2)]
    [InlineData(7, Quartile.Median, 4, 4)]
    [InlineData(7, Quartile.Third, 6, 6)]
    [InlineData(5, Quartile.First, 1, 2)]
    [InlineData(5, Quartile.Third, 4, 5)]
    [InlineData(2, Quartile.First, 1, 1)]
    [InlineData(2, Quartile.Median, 1, 2)]
    [InlineData(2, Quartile.Third, 2, 2)]
    // One value is every quartile.
    [InlineData(1, Quartile.First, 1, 1)]
    [InlineData(1, Quartile.Median, 1, 1)]
    [InlineData(1, Quartile.Third, 1, 1)]
    public void Ranks(int count, Quartile quartile, int lower, int upper)
    {
        Quartiles.Ranks(count, quartile).Should().Be((new BigInteger(lower), new BigInteger(upper)));
    }

    [Fact]
    public void RanksOfAHugeCount()
    {
        BigInteger count = BigInteger.Pow(10, 30);

        Quartiles.Ranks(count, Quartile.Median).Should().Be((count / 2, (count / 2) + 1));
        Quartiles.Ranks(count + 1, Quartile.Third).Should().Be((((count / 2) * 3 / 2) + 1, ((count / 2) * 3 / 2) + 2));
    }

    [Fact]
    public void QuartilesAreOrderedAndInRange()
    {
        Gen.Long[1, 1_000_000].Sample(count =>
        {
            (BigInteger Lower, BigInteger Upper) first = Quartiles.Ranks(count, Quartile.First);
            (BigInteger Lower, BigInteger Upper) median = Quartiles.Ranks(count, Quartile.Median);
            (BigInteger Lower, BigInteger Upper) third = Quartiles.Ranks(count, Quartile.Third);
            return first.Lower >= 1 && third.Upper <= count
                && first.Upper <= median.Lower && median.Upper <= third.Lower
                && first.Upper - first.Lower <= 1 && median.Upper - median.Lower <= 1 && third.Upper - third.Lower <= 1
                && (count == 1 || third.Lower - median.Upper == median.Lower - first.Upper);
        });
    }

    [Fact]
    public void ArgumentsAreChecked()
    {
        Action none = () => Quartiles.Ranks(0, Quartile.Median);
        Action undefined = () => Quartiles.Ranks(4, (Quartile)3);

        none.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
        undefined.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("quartile");
    }
}
