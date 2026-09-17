// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;

// Deliberately bad code: the scanner must find every one of these. A scanner that silently finds nothing would
// report the core assemblies as clean forever, so FloatingPointScannerTests runs it against this namespace first.
#pragma warning disable CS0649 // Fixture fields exist to be found in metadata, never to be read.

namespace Barbatos.Pallas.Architecture.Tests.FloatingPoint.Fixtures;

internal sealed class DoubleField
{
    public double Value;
}

internal static class DoubleParameter
{
    public static int Truncate(double value) => (int)value;
}

internal static class SingleReturn
{
    public static float Half(int divisor) => 1f / divisor;
}

internal static class DoubleLocal
{
    public static int Run(int value)
    {
        double scaled = value * 0.5;
        return (int)scaled;
    }
}

internal static class BoxedLiteral
{
    public static object Run() => 0.1;
}

internal static class MathSqrtCall
{
    public static int Run(int value) => (int)Math.Sqrt(value);
}

internal static class MathFCall
{
    public static int Run(int value) => (int)MathF.Sqrt(value);
}

internal static class ComplexLocal
{
    public static int Run(int value)
    {
        Complex complex = new(value, value);
        return complex.GetHashCode();
    }
}

internal static class NullableDoubleField
{
    public static double? Maybe;
}

internal static class GenericOverDouble
{
    public static int Count(int capacity) => new List<double>(capacity).Count;
}

internal static class BigIntegerToDouble
{
    public static object Run(int value) => (double)new BigInteger(value);
}

internal sealed class ExactArithmetic
{
    // Everything here is exact decimal or integer arithmetic. None of it may be reported.
    public decimal Money;

    public static decimal Run(long value, decimal amount)
    {
        long absolute = Math.Abs(value);
        long quotient = Math.DivRem(absolute, 7, out long remainder);
        long high = Math.BigMul(absolute, 3, out long low);
        BigInteger cube = BigInteger.Pow(new BigInteger(absolute), 3);
        decimal rounded = Math.Round((0.1m + 0.2m) * amount, 2, MidpointRounding.AwayFromZero);
        return quotient + remainder + high + low + Math.Max(absolute, 1) + (long)(cube % 1000) + Math.Sign(value) + rounded + decimal.Parse("1.5", System.Globalization.CultureInfo.InvariantCulture);
    }
}
