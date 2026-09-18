// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Arithmetic on <see cref="Value"/>, applying the precision rule of docs/PRECISION.md §3 and the calculation range of the
/// profile.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="decimal"/> arithmetic continues in <see cref="double"/> where <see cref="decimal"/> would lose digits: on
/// overflow, and when a product or quotient falls below 10⁻¹⁴, where <see cref="decimal"/>'s 28 decimal places hold fewer
/// than 15 significant digits. The <see cref="double"/> is computed from the operands, not from the rounded
/// <see cref="decimal"/>.
/// </para>
/// <para>
/// NaN and infinity are Math ERROR here and nowhere else. In the <see cref="CalculatorProfile.Standard"/> profile a result
/// that would display as 1×10¹⁰⁰ is a Math ERROR too, and a nonzero result that would not display as at least 1×10⁻⁹⁹ is
/// 0, as the calculator's range (p. 169) and the domain of xʸ (p. 171) imply (assumption U18).
/// </para>
/// </remarks>
internal static class ValueMath
{
    /// <summary>10⁻¹³: approximate values closer than this, relative to their magnitude, are equal (decision of 18 Sep 2026).</summary>
    public const decimal RelativeTolerance = 0.0000000000001m;

    // The calculator's range is 1×10⁻⁹⁹ to 9.999999999×10⁹⁹ (p. 169), and both ends are taken at ten significant digits:
    // a value that would display as 1×10^100 is out of range, not a result with a misleading display, and one that
    // displays as 1×10^-99 is in range even when double puts it a unit below 10⁻⁹⁹.
    private const double StandardLimit = 9.9999999995e99;
    private const double StandardSmallest = 9.9999999995e-100;

    public static readonly EvalResult MathError = EvalResult.Failure(CalcErrorKind.MathError);

    public static EvalResult Add(Value left, Value right, EvaluationContext context)
    {
        switch (Promote(left, right))
        {
            case ValueKind.BaseN:
                return BaseN((long)left.ToInt32() + right.ToInt32());
            case ValueKind.Complex:
                return Complex(left.ToComplex() + right.ToComplex(), context);
            case ValueKind.DoubleReal:
                return Real(left.ToDouble() + right.ToDouble(), CombineForms(left, right, ExactForm.Add), context);
        }

        ExactForm? form = CombineForms(left, right, ExactForm.Add);
        if (FromForms(left, right, form, left.ToDouble() + right.ToDouble(), context) is { } byForm)
        {
            return byForm;
        }

        decimal sum;
        try
        {
            sum = left.ToDecimal() + right.ToDecimal();
        }
        catch (OverflowException)
        {
            return Real(left.ToDouble() + right.ToDouble(), form, context);
        }

        return sum != 0m && Math.Abs(sum) < Value.SmallestPreciseDecimal
            ? Real((double)sum, form, context)
            : Value.FromDecimal(sum, left.IsExact && right.IsExact, form);
    }

    public static EvalResult Subtract(Value left, Value right, EvaluationContext context)
    {
        // Not left + (−right) in Base-N: −(−2³¹) is out of range, although FFFFFFFF − 80000000 is 7FFFFFFF.
        if (Promote(left, right) == ValueKind.BaseN)
        {
            return BaseN((long)left.ToInt32() - right.ToInt32());
        }

        EvalResult negated = Negate(right, context);
        return negated.Succeeded ? Add(left, negated.Value, context) : negated;
    }

    public static EvalResult Negate(Value value, EvaluationContext context)
    {
        return value.Kind switch
        {
            ValueKind.BaseN => BaseN(-(long)value.ToInt32()),
            ValueKind.Complex => Complex(-value.ToComplex(), context),
            ValueKind.DoubleReal => Real(-value.ToDouble(), value.Form?.Negate(), context),
            _ => Value.FromDecimal(-value.ToDecimal(), value.IsExact, value.Form?.Negate()),
        };
    }

    public static EvalResult Multiply(Value left, Value right, EvaluationContext context)
    {
        switch (Promote(left, right))
        {
            case ValueKind.BaseN:
                return BaseN((long)left.ToInt32() * right.ToInt32());
            case ValueKind.Complex:
                return Complex(left.ToComplex() * right.ToComplex(), context);
            case ValueKind.DoubleReal:
                return Real(left.ToDouble() * right.ToDouble(), CombineForms(left, right, ExactForm.Multiply), context);
        }

        ExactForm? form = CombineForms(left, right, ExactForm.Multiply);
        if (FromForms(left, right, form, left.ToDouble() * right.ToDouble(), context) is { } byForm)
        {
            return byForm;
        }

        decimal a = left.ToDecimal();
        decimal b = right.ToDecimal();
        decimal product;
        try
        {
            product = a * b;
        }
        catch (OverflowException)
        {
            return Real(left.ToDouble() * right.ToDouble(), form, context);
        }

        return Math.Abs(product) < Value.SmallestPreciseDecimal && a != 0m && b != 0m
            ? Real(left.ToDouble() * right.ToDouble(), form, context)
            : Value.FromDecimal(product, left.IsExact && right.IsExact, form);
    }

    public static EvalResult Divide(Value left, Value right, EvaluationContext context)
    {
        if (right.IsZero)
        {
            return MathError;
        }

        switch (Promote(left, right))
        {
            case ValueKind.BaseN:
                // Base-N drops the fractional part (p. 130); integer division truncates toward zero.
                return BaseN((long)left.ToInt32() / right.ToInt32());
            case ValueKind.Complex:
                return Complex(left.ToComplex() / right.ToComplex(), context);
            case ValueKind.DoubleReal:
                return Real(left.ToDouble() / right.ToDouble(), CombineForms(left, right, ExactForm.Divide), context);
        }

        ExactForm? form = CombineForms(left, right, ExactForm.Divide);
        if (FromForms(left, right, form, left.ToDouble() / right.ToDouble(), context) is { } byForm)
        {
            return byForm;
        }

        decimal a = left.ToDecimal();
        decimal quotient;
        try
        {
            quotient = a / right.ToDecimal();
        }
        catch (OverflowException)
        {
            return Real(left.ToDouble() / right.ToDouble(), form, context);
        }

        return Math.Abs(quotient) < Value.SmallestPreciseDecimal && a != 0m
            ? Real(left.ToDouble() / right.ToDouble(), form, context)
            : Value.FromDecimal(quotient, left.IsExact && right.IsExact, form);
    }

    public static EvalResult Power(Value power, Value exponent, EvaluationContext context)
    {
        if (power.Kind == ValueKind.Complex || exponent.Kind == ValueKind.Complex)
        {
            return ComplexPower(power, exponent, context);
        }

        if (TryGetInteger(exponent, out long n))
        {
            return IntegerPower(power, n, context);
        }

        if (exponent.IsExact && exponent.ToDecimal() == 0.5m && power.Kind == ValueKind.DecimalReal)
        {
            return SquareRoot(power, context);
        }

        double x = power.ToDouble();
        double y = exponent.ToDouble();
        if (x >= 0d)
        {
            // The domain of 10ˣ (p. 170) ends at 99.99999999, below 10¹⁰⁰ itself.
            if (context.Profile == CalculatorProfile.Standard && x == 10d && y > 99.99999999d)
            {
                return MathError;
            }

            // Math.Pow(0, y) is 0 for y > 0 and infinity, a Math ERROR, for y < 0 (p. 171).
            return Real(Math.Pow(x, y), null, context);
        }

        // A negative base needs an exponent m/(2n+1) (p. 171): (−8)^(1⌟3) is the real cube root, −2.
        if (TryGetOddDenominatorFraction(exponent, out long numerator, out long denominator))
        {
            return Real(Math.Pow(double.RootN(x, (int)denominator), numerator), null, context);
        }

        return context.App == CalculatorApp.Complex ? Complex(System.Numerics.Complex.Pow(x, y), context) : MathError;
    }

    public static EvalResult Root(Value index, Value radicand, EvaluationContext context)
    {
        if (index.IsZero || index.Kind == ValueKind.Complex)
        {
            return MathError;
        }

        // An index beyond int (a round trip changes it) is taken as the power 1/n.
        if (TryGetInteger(index, out long n) && (int)n == n)
        {
            if (n == 2)
            {
                return SquareRoot(radicand, context);
            }

            if (radicand.Kind == ValueKind.Complex)
            {
                return Complex(System.Numerics.Complex.Pow(radicand.ToComplex(), 1d / n), context);
            }

            double x = radicand.ToDouble();
            if (x < 0d && n % 2 == 0)
            {
                return context.App == CalculatorApp.Complex ? Complex(System.Numerics.Complex.Pow(x, 1d / n), context) : MathError;
            }

            return Real(double.RootN(x, (int)n), null, context);
        }

        EvalResult reciprocal = Divide(Value.One, index, context);
        return reciprocal.Succeeded ? Power(radicand, reciprocal.Value, context) : reciprocal;
    }

    public static EvalResult SquareRoot(Value value, EvaluationContext context)
    {
        if (value.Kind == ValueKind.Complex)
        {
            return Complex(System.Numerics.Complex.Sqrt(value.ToComplex()), context);
        }

        double x = value.ToDouble();
        if (x < 0d)
        {
            // √(−4) is 2i: the square root of the magnitude, not Complex.Sqrt, whose real part is 1.2×10⁻¹⁶.
            return context.App == CalculatorApp.Complex ? Complex(new System.Numerics.Complex(0d, Math.Sqrt(-x)), context) : MathError;
        }

        ExactForm? form = value.FormOrRational is { IsRational: true } rational ? ExactForm.SquareRoot(rational.RationalValue) : null;
        return Real(Math.Sqrt(x), form, context);
    }

    public static EvalResult Factorial(Value value, EvaluationContext context)
    {
        // 171! overflows double; the calculator's range already ends at 69! (70! > 10¹⁰⁰).
        if (!TryGetInteger(value, out long n) || n < 0 || n > 170)
        {
            return MathError;
        }

        return FromBigInteger(IntegerFunctions.Factorial((int)n), value.IsExact, context);
    }

    public static EvalResult Permutation(Value n, Value r, EvaluationContext context)
    {
        if (!TryGetSelection(n, r, context, out long items, out long selected))
        {
            return MathError;
        }

        // n!/(n−r)! ≥ r!, and 171! overflows double: a larger r cannot be in range, and is not worth multiplying out.
        if (selected > 170)
        {
            return MathError;
        }

        return FromBigInteger(IntegerFunctions.Permutations(items, selected), n.IsExact && r.IsExact, context);
    }

    public static EvalResult Combination(Value n, Value r, EvaluationContext context)
    {
        if (!TryGetSelection(n, r, context, out long items, out long selected))
        {
            return MathError;
        }

        // C(n, k) ≥ C(2k, k) ≥ 4ᵏ/(2√k) for k = min(r, n − r): above double's range from k = 1030, and not worth
        // multiplying out.
        long k = Math.Min(selected, items - selected);
        if (k >= 1030)
        {
            return MathError;
        }

        return FromBigInteger(IntegerFunctions.Combinations(items, selected), n.IsExact && r.IsExact, context);
    }

    /// <summary>Converts an angle between units, keeping π exact: 90° in radians is π/2.</summary>
    public static EvalResult ConvertAngle(Value angle, AngleUnit from, AngleUnit to, EvaluationContext context)
    {
        if (angle.Kind == ValueKind.Complex)
        {
            return MathError;
        }

        if (from == to)
        {
            return angle;
        }

        // Degrees and gradians convert by a rational factor.
        if (from != AngleUnit.Radian && to != AngleUnit.Radian)
        {
            return from == AngleUnit.Degree
                ? Divide(Multiply(angle, Value.FromDecimal(10m), context).Value, Value.FromDecimal(9m), context)
                : Divide(Multiply(angle, Value.FromDecimal(9m), context).Value, Value.FromDecimal(10m), context);
        }

        // To radians multiplies by π/180 or π/200; from radians divides by π, which needs π in the form: (π÷2)ʳ is 90°.
        decimal halfTurn = (from == AngleUnit.Degree || to == AngleUnit.Degree) ? 180m : 200m;
        ExactForm? form = angle.FormOrRational;
        ExactForm? converted = null;
        if (form is not null)
        {
            converted = to == AngleUnit.Radian
                ? ExactForm.Multiply(form, ExactForm.Multiply(ExactForm.Pi, ExactForm.Rational(1m / halfTurn))!)
                : ExactForm.Multiply(form, ExactForm.Rational(halfTurn)) is { } scaled ? ExactForm.Divide(scaled, ExactForm.Pi) : null;
        }

        return Real(Trigonometry.ConvertAngle(angle.ToDouble(), from, to), converted, context);
    }

    /// <summary>
    /// Compares two real values: exactly when both are exact or have exact forms whose difference is rational, otherwise
    /// within <see cref="RelativeTolerance"/>.
    /// </summary>
    /// <remarks>
    /// The tolerance is applied in <see cref="double"/>: an approximate value holds 15 significant digits, and the
    /// conversion adds at most 1.1×10⁻¹⁶ relative, a thousandth of the tolerance. Verify, the only caller, is not
    /// available in Base-N (p. 73).
    /// </remarks>
    /// <returns>−1, 0 or 1.</returns>
    public static int CompareReal(Value left, Value right)
    {
        if (left.FormOrRational is { } a
            && right.FormOrRational is { } b
            && ExactForm.Add(a, b.Negate()) is { IsRational: true } difference)
        {
            return Math.Sign(difference.RationalValue);
        }

        double p = left.ToDouble();
        double q = right.ToDouble();
        double tolerance = (double)RelativeTolerance * Math.Max(Math.Abs(p), Math.Abs(q));
        return Math.Abs(p - q) <= tolerance ? 0 : p.CompareTo(q);
    }

    /// <summary>Returns the integer a value holds, if it is an integer.</summary>
    /// <remarks>
    /// Only a <see cref="decimal"/> can hold one: by the precision rule a <see cref="double"/> value is below 10⁻¹⁴ or
    /// beyond 7.9×10²⁸, and Base-N has no function that takes an integer argument. <c>CreateSaturating</c> truncates and
    /// clamps at the ends of <see cref="long"/>, so the value is such an integer exactly when it comes back unchanged.
    /// </remarks>
    public static bool TryGetInteger(Value value, out long integer)
    {
        integer = 0;
        if (value.Kind != ValueKind.DecimalReal)
        {
            return false;
        }

        decimal d = value.ToDecimal();
        integer = long.CreateSaturating(d);
        return integer == d;
    }

    /// <summary>Makes a real value from a <see cref="double"/>: Math ERROR for NaN, infinity or the profile's range.</summary>
    public static EvalResult Real(double value, ExactForm? form, EvaluationContext context)
    {
        if (!double.IsFinite(value))
        {
            return MathError;
        }

        if (context.Profile == CalculatorProfile.Standard)
        {
            double magnitude = Math.Abs(value);
            if (magnitude >= StandardLimit)
            {
                return MathError;
            }

            if (magnitude < StandardSmallest && magnitude != 0d)
            {
                return Value.FromApproximation(0d, null);
            }
        }

        return Value.FromApproximation(value, form);
    }

    public static EvalResult Complex(Complex value, EvaluationContext context)
    {
        EvalResult real = Real(value.Real, null, context);
        EvalResult imaginary = Real(value.Imaginary, null, context);
        if (!real.Succeeded || !imaginary.Succeeded)
        {
            return MathError;
        }

        return imaginary.Value.IsZero ? real : Value.CreateComplex(real.Value.ToDouble(), imaginary.Value.ToDouble());
    }

    public static EvalResult FromBigInteger(BigInteger integer, bool isExact, EvaluationContext context)
    {
        try
        {
            return Value.FromDecimal((decimal)integer, isExact, form: null);
        }
        catch (OverflowException)
        {
            return Real((double)integer, null, context);
        }
    }

    private static EvalResult BaseN(long result)
    {
        // The 32-bit range applies in every number mode (p. 130; assumption U12).
        return result is >= int.MinValue and <= int.MaxValue ? Value.FromBaseN((int)result) : MathError;
    }

    private static EvalResult IntegerPower(Value power, long n, EvaluationContext context)
    {
        if (power.IsZero)
        {
            // 0⁰ and 0⁻ⁿ are Math ERROR (p. 171), although Math.Pow(0, 0) is 1.
            return n > 0 ? Value.Zero : MathError;
        }

        // Exact bases are multiplied out, so 1.1³ is exactly 1.331; square roots and π keep their forms.
        if ((power.IsExact || power.Form is not null) && MultiplyOut(power, n, context) is { Succeeded: true, Value.Kind: ValueKind.DecimalReal } product)
        {
            return product;
        }

        // Beyond decimal, Math.Pow: a product of rounded squares drifts by several units in the last place, which put
        // 10⁻⁹⁹ below 10⁻⁹⁹ and 10⁻¹²⁸ out of range, through the square 10¹²⁸.
        return Real(Math.Pow(power.ToDouble(), n), null, context);
    }

    private static EvalResult MultiplyOut(Value power, long n, EvaluationContext context)
    {
        Value result = Value.One;
        Value square = power;
        ulong remaining = (ulong)Int128.Abs(n);
        while (remaining > 0)
        {
            if (remaining % 2 == 1)
            {
                EvalResult multiplied = Multiply(result, square, context);
                if (!multiplied.Succeeded)
                {
                    return multiplied;
                }

                result = multiplied.Value;
            }

            // The last square is not needed; it cannot fail where the product is a decimal, since it is below the
            // product's square, and a failed one falls back to Math.Pow.
            remaining /= 2;
            EvalResult squared = Multiply(square, square, context);
            if (!squared.Succeeded)
            {
                return squared;
            }

            square = squared.Value;
        }

        return n < 0 ? Divide(Value.One, result, context) : result;
    }

    /// <summary>Raises a complex number to a power.</summary>
    /// <remarks>
    /// An integer power is a product, because <c>Complex.Pow(i, 2)</c> was measured at −1 + 1.2×10⁻¹⁶i, which makes the
    /// Verify of <c>i²=-1</c> (p. 128) false; multiplication gives exactly −1. The calculator allows integer powers
    /// below 10¹⁰ (p. 126). Other powers use <see cref="System.Numerics.Complex.Pow(System.Numerics.Complex, System.Numerics.Complex)"/>.
    /// </remarks>
    private static EvalResult ComplexPower(Value power, Value exponent, EvaluationContext context)
    {
        if (exponent.Kind != ValueKind.Complex && TryGetInteger(exponent, out long n))
        {
            if (Math.Abs(n) >= 10_000_000_000)
            {
                return MathError;
            }

            // The base is complex here, and a complex value is never zero.
            Complex result = System.Numerics.Complex.One;
            Complex square = power.ToComplex();
            ulong remaining = (ulong)Math.Abs(n);
            while (remaining > 0)
            {
                if (remaining % 2 == 1)
                {
                    result *= square;
                }

                remaining /= 2;
                square *= square;
            }

            return Complex(n < 0 ? System.Numerics.Complex.One / result : result, context);
        }

        return power.IsZero ? MathError : Complex(System.Numerics.Complex.Pow(power.ToComplex(), exponent.ToComplex()), context);
    }

    private static bool TryGetOddDenominatorFraction(Value value, out long numerator, out long denominator)
    {
        numerator = 0;
        denominator = 1;
        if (value.Kind != ValueKind.DecimalReal)
        {
            return false;
        }

        decimal d = value.ToDecimal();
        // An exact decimal is off by its 28th digit; an approximate one holds 15 significant digits.
        decimal tolerance = value.IsExact ? 0.0000000000000000000000001m * Math.Max(1m, Math.Abs(d)) : RelativeTolerance * Math.Abs(d);
        return Fractions.TryFromDecimal(d, 1_000_000, tolerance, out numerator, out denominator) && denominator % 2 == 1;
    }

    private static bool TryGetSelection(Value n, Value r, EvaluationContext context, out long items, out long selected)
    {
        selected = 0;
        return TryGetInteger(n, out items)
            && TryGetInteger(r, out selected)
            && items >= 0
            && selected >= 0
            && selected <= items
            && (context.Profile != CalculatorProfile.Standard || items < 10_000_000_000);
    }

    /// <summary>
    /// Computes an operation from the exact forms when either operand has one, or returns <see langword="null"/> to
    /// continue in <see cref="decimal"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="decimal"/> of a value with a square root or π is only its 15 significant digits: √2 is
    /// 1.4142135623731, 3.5×10⁻¹⁵ from the truth. Whatever the form of the result is, the operands are therefore better
    /// taken as <see cref="double"/>, where √2 has all its digits: <c>Rec(√(2),45)</c> then gives x = 1 rather than
    /// 1.0000000000000042.
    /// </remarks>
    private static EvalResult? FromForms(Value left, Value right, ExactForm? form, double approximation, EvaluationContext context)
    {
        if (form is not null)
        {
            // A rational form becomes an exact decimal in Value.FromApproximation.
            return Real(form.ToDouble(), form, context);
        }

        // A value's form is never rational: a rational result is held as an exact decimal instead.
        return left.Form is not null || right.Form is not null
            ? Real(approximation, null, context)
            : null;
    }

    private static ExactForm? CombineForms(Value left, Value right, Func<ExactForm, ExactForm, ExactForm?> operation)
    {
        if (left.Form is null && right.Form is null)
        {
            return null;
        }

        ExactForm? a = left.FormOrRational;
        ExactForm? b = right.FormOrRational;
        return a is null || b is null ? null : operation(a, b);
    }

    private static ValueKind Promote(Value left, Value right)
    {
        if (left.Kind == ValueKind.BaseN && right.Kind == ValueKind.BaseN)
        {
            return ValueKind.BaseN;
        }

        if (left.Kind == ValueKind.Complex || right.Kind == ValueKind.Complex)
        {
            return ValueKind.Complex;
        }

        return left.Kind == ValueKind.DoubleReal || right.Kind == ValueKind.DoubleReal ? ValueKind.DoubleReal : ValueKind.DecimalReal;
    }
}
