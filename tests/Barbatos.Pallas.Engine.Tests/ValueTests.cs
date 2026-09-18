// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Engine.Tests.Support;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The precision rule of docs/PRECISION.md §3, value by value: which type holds a number, and when it is exact.
/// </summary>
public sealed class ValueTests
{
    [Fact]
    public void FromDecimal_IsExact()
    {
        Value value = Value.FromDecimal(0.1m);

        value.Kind.Should().Be(ValueKind.DecimalReal);
        value.IsExact.Should().BeTrue();
        value.ToDecimal().Should().Be(0.1m);
    }

    [Theory]
    // A double in decimal's precise range becomes a decimal of 15 significant digits: binary noise goes.
    [InlineData(0.49999999999999994, ValueKind.DecimalReal, "0.5")]
    [InlineData(1e-14, ValueKind.DecimalReal, "0.00000000000001")]
    [InlineData(1.4142135623730951, ValueKind.DecimalReal, "1.4142135623731")]
    // Below 10⁻¹⁴ and beyond 7.9×10²⁸ decimal would keep fewer than 15 significant digits, or overflow.
    [InlineData(1e-15, ValueKind.DoubleReal, "1E-15")]
    [InlineData(1e30, ValueKind.DoubleReal, "1E+30")]
    [InlineData(6.62607015e-34, ValueKind.DoubleReal, "6.62607015E-34")]
    public void FromDouble_AppliesThePrecisionRule(double number, ValueKind kind, string text)
    {
        Value value = Value.FromDouble(number);

        value.Kind.Should().Be(kind);
        value.IsExact.Should().BeFalse();
        value.ToString().Should().Be(text);
    }

    [Fact]
    public void FromDouble_RejectsNaNAndInfinity()
    {
        Action nan = () => Value.FromDouble(double.NaN);
        Action infinity = () => Value.FromDouble(double.PositiveInfinity);

        nan.Should().Throw<ArgumentOutOfRangeException>();
        infinity.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromComplex_KeepsBothParts()
    {
        Value value = Value.FromComplex(new Complex(2d, -3d));

        value.Kind.Should().Be(ValueKind.Complex);
        value.IsReal.Should().BeFalse();
        value.ToComplex().Should().Be(new Complex(2d, -3d));
        value.ToString().Should().Be("(2, -3)");
    }

    [Fact]
    public void FromComplex_RejectsNonFiniteParts()
    {
        Action act = () => Value.FromComplex(new Complex(1d, double.NaN));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromBaseN_IsAnExactInteger()
    {
        Value value = Value.FromBaseN(-11);

        value.Kind.Should().Be(ValueKind.BaseN);
        value.IsExact.Should().BeTrue();
        value.ToInt32().Should().Be(-11);
        value.ToDecimal().Should().Be(-11m);
        value.ToDouble().Should().Be(-11d);
        value.ToString().Should().Be("-11");
    }

    [Fact]
    public void Conversions_RefuseWhatTheTypeCannotHold()
    {
        Action decimalOfDouble = () => Value.FromDouble(1e30).ToDecimal();
        Action doubleOfComplex = () => Value.FromComplex(Complex.ImaginaryOne).ToDouble();
        Action intOfDecimal = () => Value.FromDecimal(1m).ToInt32();

        decimalOfDouble.Should().Throw<InvalidOperationException>();
        doubleOfComplex.Should().Throw<InvalidOperationException>();
        intOfDecimal.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToComplex_TakesARealValueAsItsRealPart()
    {
        Value.FromDecimal(2.5m).ToComplex().Should().Be(new Complex(2.5d, 0d));
        Value.FromBaseN(7).ToComplex().Should().Be(new Complex(7d, 0d));
    }

    [Fact]
    public void Pi_IsExactlyTheDoubleNearestPi()
    {
        // The decimal of π keeps 15 digits, but the value stays exactly double.Pi for the trigonometric functions,
        // which is what makes sin(π) zero rather than 3.2×10⁻¹⁶ (docs/PRECISION.md §6).
        Value.Pi.ToDouble().Should().Be(double.Pi);
        Value.Pi.ToDecimal().Should().Be(3.14159265358979m);
        Value.E.ToDouble().Should().Be(double.E);
        Value.E.ToDecimal().Should().Be(2.71828182845904m, "the decimal of e keeps 15 digits, one of which is the measured off-by-one of (decimal)double");
    }

    [Fact]
    public void Equality_ComparesKindExactnessAndNumber()
    {
        Value.FromDecimal(2m).Should().Be(Value.FromDecimal(2.0m));
        (Value.FromDecimal(2m) == Value.FromDouble(2d)).Should().BeFalse("one is exact and the other is not");
        (Value.FromDecimal(2m) != Value.FromBaseN(2)).Should().BeTrue();
        Value.FromDecimal(2m).GetHashCode().Should().Be(Value.FromDecimal(2m).GetHashCode());
        Value.Zero.Equals((object)Value.Zero).Should().BeTrue();
        Value.Zero.Equals("0").Should().BeFalse();
    }

    [Theory]
    // A literal is read into decimal, never through double (invariant I2): a tenth is exactly a tenth.
    [InlineData("0.1", ValueKind.DecimalReal, true)]
    [InlineData("123456789012345678901234567890", ValueKind.DoubleReal, false)]
    [InlineData("0.0000000000000012", ValueKind.DoubleReal, false)]
    [InlineData("1.2345678901234567890123456789012", ValueKind.DecimalReal, false)]
    public void Literals_FollowThePrecisionRule(string literal, ValueKind kind, bool exact)
    {
        Value value = Calculator.Evaluate(literal);

        value.Kind.Should().Be(kind);
        value.IsExact.Should().Be(exact);
    }

    [Fact]
    public void RecurringLiterals_AreExactFractions()
    {
        // 3.(021) + 0.(312) is 10⌟3 (manual p. 46), so the sum of the two literals must be exactly 10/3.
        Value value = Calculator.Evaluate("3.(021)+0.(312)");

        value.IsExact.Should().BeTrue();
        value.ToDecimal().Should().Be(10m / 3m);
    }
}
