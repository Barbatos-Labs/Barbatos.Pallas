// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics.Tests;

public sealed class FractionsTests
{
    private const long TenDigits = 9_999_999_999;
    private const decimal DecimalQuotientTolerance = 0.0000000000000000000000001m;

    public static TheoryData<decimal, long, long> RecognizedValues => new()
    {
        // Manual p. 32: 2/3 + 1 1/2 = 13/6.
        { (2m / 3m) + 1.5m, 13, 6 },
        // p. 32: 1 1/123456 = 123457/123456.
        { 1m + (1m / 123456m), 123457, 123456 },
        // p. 46: 3.(021) + 0.(312) = 10/3.
        { 3m + (21m / 999m) + (312m / 999m), 10, 3 },
        // 1 ÷ 3 × 3 is 0.9999999999999999999999999999 in decimal; it stands for 1.
        { 1m / 3m * 3m, 1, 1 },
        { 0.1m, 1, 10 },
        { -1.5m, -3, 2 },
        { 0m, 0, 1 },
        { 42m, 42, 1 },
        { 1m / 7m, 1, 7 },
    };

    [Theory]
    [MemberData(nameof(RecognizedValues))]
    public void TryFromDecimal_RecognizesTheFractionAQuotientStandsFor(decimal value, long expectedNumerator, long expectedDenominator)
    {
        Fractions.TryFromDecimal(value, TenDigits, DecimalQuotientTolerance, out long numerator, out long denominator).Should().BeTrue();
        (numerator, denominator).Should().Be((expectedNumerator, expectedDenominator));
    }

    [Fact]
    public void TryFromDecimal_RefusesIrrationalValuesAndDenominatorsBeyondTheLimit()
    {
        Fractions.TryFromDecimal(1.4142135623730950488016887242m, TenDigits, DecimalQuotientTolerance, out _, out _).Should().BeFalse("√2 has no fraction");
        Fractions.TryFromDecimal(3.1415926535897932384626433833m, TenDigits, DecimalQuotientTolerance, out _, out _).Should().BeFalse("π has no fraction");
        Fractions.TryFromDecimal((2m / 3m) + 1.5m, 5, DecimalQuotientTolerance, out _, out _).Should().BeFalse("13/6 needs a denominator of 6");
    }

    [Fact]
    public void TryFromDecimal_ToleranceMatchesThePrecisionOfTheValue()
    {
        // A value from double carries 15 significant digits: 1/3 recognized only with a matching tolerance.
        decimal fromDouble = 0.333333333333333m;

        Fractions.TryFromDecimal(fromDouble, TenDigits, DecimalQuotientTolerance, out _, out _).Should().BeFalse();
        Fractions.TryFromDecimal(fromDouble, TenDigits, 0.000000000000001m, out long numerator, out long denominator).Should().BeTrue();
        (numerator, denominator).Should().Be((1L, 3L));
    }

    [Fact]
    public void TryFromDecimal_WithZeroTolerance_AcceptsOnlyExactFractions()
    {
        Fractions.TryFromDecimal(0.375m, 10, 0m, out long numerator, out long denominator).Should().BeTrue();
        (numerator, denominator).Should().Be((3L, 8L));

        Fractions.TryFromDecimal(long.MaxValue, 1, 0m, out long whole, out long one).Should().BeTrue("the largest long is still a whole number");
        (whole, one).Should().Be((long.MaxValue, 1L));
    }

    [Fact]
    public void TryFromDecimal_StopsWhenLongOverflows()
    {
        Fractions.TryFromDecimal(decimal.MaxValue, long.MaxValue, 0m, out _, out _).Should().BeFalse();
        Fractions.TryFromDecimal(9223372036854775806.5m, long.MaxValue, 0m, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void TryFromDecimal_RejectsInvalidArguments()
    {
        Action denominator = () => Fractions.TryFromDecimal(0.5m, 0, 0m, out _, out _);
        Action tolerance = () => Fractions.TryFromDecimal(0.5m, 10, -1m, out _, out _);

        denominator.Should().Throw<ArgumentOutOfRangeException>();
        tolerance.Should().Throw<ArgumentOutOfRangeException>();
    }
}
