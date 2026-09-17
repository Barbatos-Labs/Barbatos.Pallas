// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using PeterO.Numbers;

namespace Barbatos.Pallas.Numerics.Tests.Support;

/// <summary>
/// High-precision reference values from PeterO.Numbers, an arbitrary-precision library that shares no code with
/// System.Math. Used to pin the accuracy PRECISION.md claims for the double-based functions.
/// </summary>
/// <remarks>
/// Three PeterO.Numbers 1.8.2 defects, all found by Pallas tests on 17 Sep 2026, are avoided here:
/// <list type="bullet">
/// <item><description><c>EDecimal.RoundToExponent</c> is wrong for <c>ERounding.Down</c> (it halved
/// 11120217850809733414095251570457 rounded to exponent 1). Nothing here uses it.</description></item>
/// <item><description><c>Sqrt</c>, <c>Log</c> and <c>Log10</c> are wrong by exact powers of 1000 for inputs with a long
/// mantissa: the exact 94-digit expansion of the double 2.093E-18 gave √ = 4.57E-11 instead of 1.4467E-9, and
/// ln(5E-70) was off by ln 1000. The same values rounded to 20-60 significant digits agreed with their short forms
/// in 1,200 of 1,200 samples. So a double enters as its exact value rounded to 40 significant digits. Its round-trip
/// ("R") text is not good enough: half an ulp of input error becomes 1.4×10⁻¹⁴ in 47^(1137/31) and 7.7×10⁻¹⁴ in
/// exp(700), more than the tolerance being tested.</description></item>
/// <item><description><c>EDecimal.Remainder(divisor, null)</c> throws <see cref="NullReferenceException"/>; angles are
/// reduced with <c>Divide</c> and <c>ToEInteger</c> instead.</description></item>
/// </list>
/// </remarks>
internal static class Reference
{
    private static readonly EContext Working = EContext.ForPrecision(40);

    /// <summary>π = 16 arctan(1/5) − 4 arctan(1/239) (Machin), at 40 digits.</summary>
    public static readonly EDecimal Pi = ArctanOfReciprocal(5).Multiply(EDecimal.FromInt32(16)).Subtract(ArctanOfReciprocal(239).Multiply(EDecimal.FromInt32(4)));

    public static EDecimal Exact(double value) => ERational.FromDouble(value).ToEDecimal(Working);

    public static EDecimal Exp(double x) => Exact(x).Exp(Working);

    public static EDecimal Ln(double x) => Exact(x).Log(Working);

    public static EDecimal Log10(double x) => Exact(x).Log10(Working);

    public static EDecimal Sqrt(double x) => Exact(x).Sqrt(Working);

    public static EDecimal Pow(double x, double y) => Exact(x).Pow(Exact(y), Working);

    /// <summary>sin x by its Taylor series at 40 digits; independent of System.Math. Intended for |x| ≤ 10.</summary>
    public static EDecimal Sin(double x) => Series(Exact(x), sine: true);

    /// <summary>cos x by its Taylor series at 40 digits; independent of System.Math. Intended for |x| ≤ 10.</summary>
    public static EDecimal Cos(double x) => Series(Exact(x), sine: false);

    /// <summary>
    /// sin of an angle in a unit with <paramref name="fullTurn"/> units per turn (360 for degrees, 400 for gradians). The
    /// angle is reduced exactly before conversion, so the reference is accurate at any magnitude.
    /// </summary>
    public static EDecimal SinOfTurns(double angle, int fullTurn) => Series(ToRadians(angle, fullTurn), sine: true);

    /// <summary>cos of an angle in a unit with <paramref name="fullTurn"/> units per turn; see <see cref="SinOfTurns"/>.</summary>
    public static EDecimal CosOfTurns(double angle, int fullTurn) => Series(ToRadians(angle, fullTurn), sine: false);

    /// <summary>tan of an angle in a unit with <paramref name="fullTurn"/> units per turn; see <see cref="SinOfTurns"/>.</summary>
    public static EDecimal TanOfTurns(double angle, int fullTurn) => SinOfTurns(angle, fullTurn).Divide(CosOfTurns(angle, fullTurn), Working);

    /// <summary>|actual − expected| ≤ tolerance × max(1, |expected|).</summary>
    public static bool Close(double actual, EDecimal expected, string tolerance)
    {
        EDecimal scale = expected.Abs().CompareToValue(EDecimal.One) > 0 ? expected.Abs() : EDecimal.One;
        EDecimal difference = Exact(actual).Subtract(expected).Abs();
        return difference.CompareToValue(EDecimal.FromString(tolerance).Multiply(scale)) <= 0;
    }

    private static EDecimal ToRadians(double angle, int fullTurn)
    {
        // Reduced by whole turns without EDecimal.Remainder, which throws NullReferenceException with a null context.
        EDecimal turn = EDecimal.FromInt32(fullTurn);
        EDecimal exact = Exact(angle);
        EDecimal wholeTurns = EDecimal.FromEInteger(exact.Divide(turn, Working).ToEInteger());
        return exact.Subtract(wholeTurns.Multiply(turn)).Multiply(Pi).Multiply(EDecimal.FromInt32(2)).Divide(turn, Working);
    }

    private static EDecimal ArctanOfReciprocal(int n)
    {
        EDecimal x = EDecimal.One.Divide(EDecimal.FromInt32(n), Working);
        EDecimal xSquared = x.Multiply(x);
        EDecimal power = x;
        EDecimal sum = EDecimal.Zero;
        EDecimal threshold = EDecimal.FromString("1E-45");
        for (int k = 0; power.Abs().CompareToValue(threshold) > 0; k++)
        {
            EDecimal term = power.Divide(EDecimal.FromInt32((2 * k) + 1), Working);
            sum = k % 2 == 0 ? sum.Add(term) : sum.Subtract(term);
            power = power.Multiply(xSquared, Working);
        }

        return sum;
    }

    private static EDecimal Series(EDecimal x, bool sine)
    {
        EDecimal term = sine ? x : EDecimal.One;
        EDecimal sum = term;
        EDecimal xSquared = x.Multiply(x);
        EDecimal threshold = EDecimal.FromString("1E-45");
        for (int n = sine ? 1 : 0; term.Abs().CompareToValue(threshold) > 0; n += 2)
        {
            term = term.Multiply(xSquared).Negate().Divide(EDecimal.FromInt32((n + 1) * (n + 2)), Working);
            sum = sum.Add(term);
        }

        return sum;
    }
}
