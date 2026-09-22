// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using PeterO.Numbers;

namespace Barbatos.Pallas.Numerics.Tests.Support;

/// <summary>
/// erf and erfc from PeterO.Numbers at 50 digits, by two methods that share no code with <see cref="ErrorFunction"/>'s
/// double arithmetic: the Maclaurin series, and above 5 the continued fraction of Γ(½, x²).
/// </summary>
/// <remarks>
/// The Maclaurin series alternates, and its largest term at x = 6 is about e³⁶ ≈ 4×10¹⁵, so 50 digits leave more than 30
/// after the cancellation. erfc = 1 − erf loses more: at x = 5 (erfc = 1.5×10⁻¹², largest term 7×10¹⁰) it agrees with the
/// continued fraction to 4×10⁻³⁰ relative, at x = 3 to 3×10⁻⁴³, far below the 2×10⁻¹⁵ being tested. The two methods are
/// checked to agree to 10⁻²⁵, and erf(1) and erfc(3) are checked against published digits. π comes from
/// Machin's formula; PeterO's square root is used on it at 50 digits, where its defect (Reference.cs) does not show: erf(1)
/// would be wrong otherwise. 130 digits were measured first and made each test take minutes.
/// </remarks>
internal static class ErrorFunctionReference
{
    private static readonly EContext Wide = EContext.ForPrecision(50);

    private static readonly EDecimal SqrtPi = MachinPi().Sqrt(Wide);

    public static EDecimal Erf(double x)
    {
        EDecimal value = Exact(x);
        EDecimal square = value.Multiply(value, Wide);
        EDecimal power = value;
        EDecimal sum = value;
        EDecimal threshold = EDecimal.FromString("1E-60");
        for (int n = 1; ; n++)
        {
            power = power.Multiply(square, Wide).Negate().Divide(EDecimal.FromInt32(n), Wide);
            EDecimal term = power.Divide(EDecimal.FromInt32((2 * n) + 1), Wide);
            sum = sum.Add(term, Wide);
            if (term.Abs().CompareToValue(threshold) < 0)
            {
                return sum.Multiply(EDecimal.FromInt32(2), Wide).Divide(SqrtPi, Wide);
            }
        }
    }

    public static EDecimal Erfc(double x) => x <= 5d ? EDecimal.One.Subtract(Erf(x), Wide) : ContinuedFraction(x);

    /// <summary>erfc(x) = e^(−x²)·x·K/√π by the continued fraction of Γ(½, x²), evaluated forward (modified Lentz).</summary>
    public static EDecimal ContinuedFraction(double x)
    {
        EDecimal value = Exact(x);
        EDecimal z = value.Multiply(value, Wide);
        EDecimal half = EDecimal.FromString("0.5");
        EDecimal b = z.Add(half);
        EDecimal c = EDecimal.FromString("1E200");
        EDecimal d = EDecimal.One.Divide(b, Wide);
        EDecimal fraction = d;

        // Above the rounding of 50 digits, 10⁻⁴⁹: a step that rounds to within it of 1 may never reach 1 exactly.
        EDecimal threshold = EDecimal.FromString("1E-45");
        for (int i = 1; ; i++)
        {
            EDecimal numerator = EDecimal.FromInt32(i).Multiply(EDecimal.FromInt32(i).Subtract(half)).Negate();
            b = b.Add(EDecimal.FromInt32(2));
            d = EDecimal.One.Divide(numerator.Multiply(d, Wide).Add(b, Wide), Wide);
            c = b.Add(numerator.Divide(c, Wide), Wide);
            EDecimal step = d.Multiply(c, Wide);
            fraction = fraction.Multiply(step, Wide);
            if (step.Subtract(EDecimal.One).Abs().CompareToValue(threshold) < 0)
            {
                return z.Negate().Exp(Wide).Multiply(value, Wide).Multiply(fraction, Wide).Divide(SqrtPi, Wide);
            }
        }
    }

    /// <summary>|actual − expected| ≤ tolerance × |expected|.</summary>
    public static bool CloseRelative(double actual, EDecimal expected, double tolerance)
    {
        return RelativeError(actual, expected) <= tolerance;
    }

    /// <summary>|actual − expected| / |expected|; 0 when both are 0.</summary>
    public static double RelativeError(double actual, EDecimal expected)
    {
        EDecimal difference = Exact(actual).Subtract(expected, Wide).Abs();
        if (expected.IsZero)
        {
            return difference.IsZero ? 0d : double.PositiveInfinity;
        }

        return difference.Divide(expected.Abs(), Wide).ToDouble();
    }

    private static EDecimal Exact(double value) => ERational.FromDouble(value).ToEDecimal(Wide);

    private static EDecimal MachinPi()
    {
        return ArctanOfReciprocal(5).Multiply(EDecimal.FromInt32(16)).Subtract(ArctanOfReciprocal(239).Multiply(EDecimal.FromInt32(4)), Wide);
    }

    private static EDecimal ArctanOfReciprocal(int n)
    {
        EDecimal x = EDecimal.One.Divide(EDecimal.FromInt32(n), Wide);
        EDecimal square = x.Multiply(x, Wide);
        EDecimal power = x;
        EDecimal sum = EDecimal.Zero;
        EDecimal threshold = EDecimal.FromString("1E-55");
        for (int k = 0; power.Abs().CompareToValue(threshold) > 0; k++)
        {
            EDecimal term = power.Divide(EDecimal.FromInt32((2 * k) + 1), Wide);
            sum = k % 2 == 0 ? sum.Add(term, Wide) : sum.Subtract(term, Wide);
            power = power.Multiply(square, Wide);
        }

        return sum;
    }
}
