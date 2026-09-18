// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Numerics;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Evaluates the built-in operations: the calculator's functions on <see cref="System.Math"/>, <see cref="double"/>'s π
/// functions, <see cref="Trigonometry"/> and <see cref="IntegerFunctions"/>, with the reference calculator's domains (pp. 170-171) in
/// the <see cref="CalculatorProfile.Standard"/> profile.
/// </summary>
internal static class Operations
{
    public static EvalResult Evaluate(Operation operation, ReadOnlySpan<Value> arguments, EvaluationContext context)
    {
        return operation switch
        {
            Operation.Negate => ValueMath.Negate(arguments[0], context),
            Operation.Add => ValueMath.Add(arguments[0], arguments[1], context),
            Operation.Subtract => ValueMath.Subtract(arguments[0], arguments[1], context),
            Operation.Multiply => ValueMath.Multiply(arguments[0], arguments[1], context),
            Operation.Divide => ValueMath.Divide(arguments[0], arguments[1], context),
            Operation.Power => ValueMath.Power(arguments[0], arguments[1], context),
            Operation.Root => ValueMath.Root(arguments[0], arguments[1], context),
            Operation.Square => ValueMath.Multiply(arguments[0], arguments[0], context),
            Operation.Cube => Cube(arguments[0], context),
            Operation.Reciprocal => ValueMath.Divide(Value.One, arguments[0], context),
            Operation.Percent => ValueMath.Divide(arguments[0], Value.FromDecimal(100m), context),
            Operation.MixedFraction => MixedFraction(arguments[0], arguments[1], arguments[2], context),
            Operation.RemainderQuotient => DivisionWithRemainder(arguments[0], arguments[1], context) is { } division
                ? division.Quotient
                : ValueMath.Divide(arguments[0], arguments[1], context),
            Operation.Factorial => ValueMath.Factorial(arguments[0], context),
            Operation.Permutation => ValueMath.Permutation(arguments[0], arguments[1], context),
            Operation.Combination => ValueMath.Combination(arguments[0], arguments[1], context),
            Operation.Random => Value.FromDecimal(context.Random.Next(1000) / 1000m),
            Operation.RandomInteger => RandomInteger(arguments[0], arguments[1], context),
            Operation.GreatestCommonDivisor => GreatestCommonDivisor(arguments[0], arguments[1], context),
            Operation.LeastCommonMultiple => LeastCommonMultiple(arguments[0], arguments[1], context),
            Operation.Absolute => Absolute(arguments[0], context),
            Operation.IntegerPart => IntegerPart(arguments[0], floor: false, context),
            Operation.LargestInteger => IntegerPart(arguments[0], floor: true, context),
            Operation.Round => Round(arguments[0], context),
            Operation.SquareRoot => ValueMath.SquareRoot(arguments[0], context),
            Operation.Exp => Real(arguments[0], context, Math.Exp),
            // log and ln need x > 0 (p. 170): Math returns NaN below 0 and −∞ at 0, both a Math ERROR.
            Operation.Log10 => Real(arguments[0], context, Math.Log10),
            Operation.Ln => Real(arguments[0], context, Math.Log),
            Operation.LogBase => LogBase(arguments[0], arguments[1], context),
            Operation.DegreesAngle => ValueMath.ConvertAngle(arguments[0], AngleUnit.Degree, context.AngleUnit, context),
            Operation.RadiansAngle => ValueMath.ConvertAngle(arguments[0], AngleUnit.Radian, context.AngleUnit, context),
            Operation.GradiansAngle => ValueMath.ConvertAngle(arguments[0], AngleUnit.Gradian, context.AngleUnit, context),
            Operation.Sexagesimal => Sexagesimal(arguments[0], arguments[1], arguments[2], context),
            Operation.Sin => Trigonometric(arguments[0], context, Trigonometry.Sin),
            Operation.Cos => Trigonometric(arguments[0], context, Trigonometry.Cos),
            Operation.Tan => Trigonometric(arguments[0], context, Trigonometry.Tan),
            Operation.Asin => InverseTrigonometric(arguments[0], context, Trigonometry.Asin),
            Operation.Acos => InverseTrigonometric(arguments[0], context, Trigonometry.Acos),
            Operation.Atan => InverseTrigonometric(arguments[0], context, Trigonometry.Atan),
            Operation.Sinh => Hyperbolic(arguments[0], context, Math.Sinh, 230.2585092d),
            Operation.Cosh => Hyperbolic(arguments[0], context, Math.Cosh, 230.2585092d),
            Operation.Tanh => Real(arguments[0], context, Math.Tanh),
            Operation.Asinh => Hyperbolic(arguments[0], context, Math.Asinh, 4.999999999e99),
            Operation.Acosh => Hyperbolic(arguments[0], context, Math.Acosh, 4.999999999e99),
            Operation.Atanh => Hyperbolic(arguments[0], context, Math.Atanh, 0.9999999999d),
            Operation.Polar => Polar(arguments[0], arguments[1], context),
            Operation.Conjugate => arguments[0].Kind == ValueKind.Complex
                ? ValueMath.Complex(Complex.Conjugate(arguments[0].ToComplex()), context)
                : arguments[0],
            Operation.Argument => Argument(arguments[0], context),
            Operation.RealPart => arguments[0].Kind == ValueKind.Complex ? ValueMath.Real(arguments[0].ToComplex().Real, null, context) : arguments[0],
            Operation.ImaginaryPart => arguments[0].Kind == ValueKind.Complex ? ValueMath.Real(arguments[0].ToComplex().Imaginary, null, context) : Value.Zero,
            Operation.And => Bitwise(arguments[0], arguments[1], static (a, b) => a & b),
            Operation.Or => Bitwise(arguments[0], arguments[1], static (a, b) => a | b),
            Operation.Xor => Bitwise(arguments[0], arguments[1], static (a, b) => a ^ b),
            Operation.Xnor => Bitwise(arguments[0], arguments[1], static (a, b) => ~(a ^ b)),
            // The logic operations exist in Base-N only (p. 51), where every value is a Base-N integer.
            Operation.Not => Value.FromBaseN(~arguments[0].ToInt32()),
            Operation.Neg => ValueMath.Negate(arguments[0], context),
            Operation.AtomicWeight => AtomicWeight(arguments[0], context),
            Operation.IntegerPartSlope or Operation.LargestIntegerSlope => IntegerSlope(arguments[0]),
            Operation.RoundSlope => RoundSlope(arguments[0], context),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Not a built-in operation."),
        };
    }

    /// <summary>Divides with remainder (p. 56), or returns <see langword="null"/> where the calculator divides normally.</summary>
    /// <remarks>
    /// The calculator divides normally when the operands are too large (|x| ≥ 10¹⁰ here, assumption U13), when the quotient
    /// is not a positive integer, or when the remainder is not positive.
    /// </remarks>
    public static (Value Quotient, Value Remainder)? DivisionWithRemainder(Value dividend, Value divisor, EvaluationContext context)
    {
        if (dividend.Kind != ValueKind.DecimalReal || divisor.Kind != ValueKind.DecimalReal || divisor.IsZero)
        {
            return null;
        }

        decimal a = dividend.ToDecimal();
        decimal b = divisor.ToDecimal();
        if (Math.Abs(a) >= 10_000_000_000m || Math.Abs(b) >= 10_000_000_000m)
        {
            return null;
        }

        decimal quotient = Math.Floor(a / b);
        decimal remainder = a - (quotient * b);
        if (quotient < 1m || remainder <= 0m)
        {
            return null;
        }

        _ = context;
        bool exact = dividend.IsExact && divisor.IsExact;
        return (Value.FromDecimal(quotient, exact, form: null), Value.FromDecimal(remainder, exact, form: null));
    }

    /// <summary>Rounds a value the way <c>Rnd(</c> does: to the display format (p. 60).</summary>
    public static EvalResult Round(Value value, EvaluationContext context)
    {
        NumberFormat format = context.Settings.NumberFormat;
        switch (value.Kind)
        {
            case ValueKind.DecimalReal:
                decimal d = value.ToDecimal();
                decimal rounded = format.Kind == NumberFormatKind.Fix
                    ? Math.Round(d, format.Digits, MidpointRounding.AwayFromZero)
                    : DecimalDigits.RoundToSignificantDigits(d, format.Kind == NumberFormatKind.Sci ? format.Digits : 10);
                return Value.FromDecimal(rounded);
            case ValueKind.DoubleReal:
                // Beyond decimal's range Fix changes nothing; significant digits still apply.
                return format.Kind == NumberFormatKind.Fix
                    ? value
                    : ValueMath.Real(DecimalDigits.RoundToSignificantDigits(value.ToDouble(), format.Kind == NumberFormatKind.Sci ? format.Digits : 10), null, context);
            default:
                return ValueMath.MathError;
        }
    }

    private static EvalResult Cube(Value value, EvaluationContext context)
    {
        EvalResult square = ValueMath.Multiply(value, value, context);
        return square.Succeeded ? ValueMath.Multiply(square.Value, value, context) : square;
    }

    private static EvalResult MixedFraction(Value whole, Value numerator, Value denominator, EvaluationContext context)
    {
        EvalResult fraction = ValueMath.Divide(numerator, denominator, context);
        if (!fraction.Succeeded)
        {
            return fraction;
        }

        // −1⌟1⌟2 is −1½: the fraction takes the sign of the whole part.
        return whole.IsReal && whole.ToDouble() < 0d
            ? ValueMath.Subtract(whole, fraction.Value, context)
            : ValueMath.Add(whole, fraction.Value, context);
    }

    private static EvalResult RandomInteger(Value low, Value high, EvaluationContext context)
    {
        // a < b; |a|, |b| < 10¹⁰; b − a < 10¹⁰ (p. 171).
        if (!ValueMath.TryGetInteger(low, out long a) || !ValueMath.TryGetInteger(high, out long b) || a >= b
            || Math.Abs(a) >= 10_000_000_000 || Math.Abs(b) >= 10_000_000_000 || b - a >= 10_000_000_000)
        {
            return ValueMath.MathError;
        }

        return Value.FromDecimal(context.Random.NextInt64(a, b + 1));
    }

    private static EvalResult GreatestCommonDivisor(Value left, Value right, EvaluationContext context)
    {
        return TryGetIntegers(left, right, context, allowNegative: true, out long a, out long b)
            ? ValueMath.FromBigInteger(BigInteger.GreatestCommonDivisor(a, b), left.IsExact && right.IsExact, context)
            : ValueMath.MathError;
    }

    private static EvalResult LeastCommonMultiple(Value left, Value right, EvaluationContext context)
    {
        return TryGetIntegers(left, right, context, allowNegative: context.Profile != CalculatorProfile.Standard, out long a, out long b)
            ? ValueMath.FromBigInteger(IntegerFunctions.LeastCommonMultiple(a, b), left.IsExact && right.IsExact, context)
            : ValueMath.MathError;
    }

    private static bool TryGetIntegers(Value left, Value right, EvaluationContext context, bool allowNegative, out long a, out long b)
    {
        // GCD: |a|, |b| < 10¹⁰; LCM: 0 ≤ a, b < 10¹⁰ (p. 171).
        b = 0;
        if (!ValueMath.TryGetInteger(left, out a) || !ValueMath.TryGetInteger(right, out b))
        {
            return false;
        }

        if (!allowNegative && (a < 0 || b < 0))
        {
            return false;
        }

        return context.Profile != CalculatorProfile.Standard || (Math.Abs(a) < 10_000_000_000 && Math.Abs(b) < 10_000_000_000);
    }

    private static EvalResult Absolute(Value value, EvaluationContext context)
    {
        return value.Kind switch
        {
            ValueKind.Complex => ValueMath.Real(Complex.Abs(value.ToComplex()), null, context),
            ValueKind.BaseN => ValueMath.MathError,
            _ => value.ToDouble() < 0d ? ValueMath.Negate(value, context) : value,
        };
    }

    private static EvalResult IntegerPart(Value value, bool floor, EvaluationContext context)
    {
        return value.Kind switch
        {
            ValueKind.DecimalReal => Value.FromDecimal(floor ? Math.Floor(value.ToDecimal()) : Math.Truncate(value.ToDecimal()), value.IsExact, form: null),
            ValueKind.DoubleReal => ValueMath.Real(floor ? Math.Floor(value.ToDouble()) : Math.Truncate(value.ToDouble()), null, context),
            _ => ValueMath.MathError,
        };
    }

    private static EvalResult Real(Value value, EvaluationContext context, Func<double, double> function)
    {
        return value.IsReal ? ValueMath.Real(function(value.ToDouble()), null, context) : ValueMath.MathError;
    }

    private static EvalResult LogBase(Value logBase, Value value, EvaluationContext context)
    {
        if (!logBase.IsReal || !value.IsReal)
        {
            return ValueMath.MathError;
        }

        // A base must be positive and not 1, and x positive (p. 170). Math returns NaN or an infinity for all of them but
        // a zero base: ln 5 / ln 0 is −0.
        double b = logBase.ToDouble();
        double x = value.ToDouble();
        return b > 0d ? ValueMath.Real(Math.Log(x) / Math.Log(b), null, context) : ValueMath.MathError;
    }

    private static EvalResult Trigonometric(Value angle, EvaluationContext context, Func<double, AngleUnit, double> function)
    {
        if (!angle.IsReal)
        {
            return ValueMath.MathError;
        }

        double x = angle.ToDouble();
        if (context.Profile == CalculatorProfile.Standard && Math.Abs(x) >= MaximumAngle(context.AngleUnit))
        {
            return ValueMath.MathError;
        }

        return ValueMath.Real(function(x, context.AngleUnit), null, context);
    }

    private static double MaximumAngle(AngleUnit unit)
    {
        // p. 170: 9×10⁹ degrees, 157079632.7 radians, 10¹⁰ gradians.
        return unit switch
        {
            AngleUnit.Degree => 9e9,
            AngleUnit.Radian => 157079632.7,
            _ => 1e10,
        };
    }

    private static EvalResult InverseTrigonometric(Value value, EvaluationContext context, Func<double, AngleUnit, double> function)
    {
        return value.IsReal ? ValueMath.Real(function(value.ToDouble(), context.AngleUnit), null, context) : ValueMath.MathError;
    }

    private static EvalResult Hyperbolic(Value value, EvaluationContext context, Func<double, double> function, double standardLimit)
    {
        if (!value.IsReal)
        {
            return ValueMath.MathError;
        }

        double x = value.ToDouble();
        return context.Profile == CalculatorProfile.Standard && Math.Abs(x) > standardLimit
            ? ValueMath.MathError
            : ValueMath.Real(function(x), null, context);
    }

    private static EvalResult Sexagesimal(Value degrees, Value minutes, Value seconds, EvaluationContext context)
    {
        if (!degrees.IsReal || !minutes.IsReal || !seconds.IsReal)
        {
            return ValueMath.MathError;
        }

        if (degrees.Kind == ValueKind.DecimalReal && minutes.Kind == ValueKind.DecimalReal && seconds.Kind == ValueKind.DecimalReal)
        {
            decimal d = degrees.ToDecimal();
            try
            {
                decimal magnitude = Numerics.Sexagesimal.ToDegrees(Math.Abs(d), minutes.ToDecimal(), seconds.ToDecimal());
                bool exact = degrees.IsExact && minutes.IsExact && seconds.IsExact;
                return Value.FromDecimal(d < 0m ? -magnitude : magnitude, exact, form: null);
            }
            catch (OverflowException)
            {
                // Falls through to double.
            }
        }

        double x = degrees.ToDouble();
        double total = Math.Abs(x) + (minutes.ToDouble() / 60d) + (seconds.ToDouble() / 3600d);
        return ValueMath.Real(x < 0d ? -total : total, null, context);
    }

    private static EvalResult Polar(Value radius, Value angle, EvaluationContext context)
    {
        if (!radius.IsReal || !angle.IsReal)
        {
            return ValueMath.MathError;
        }

        // Trigonometry rather than Complex.FromPolarCoordinates: 2∠90 is exactly 2i, where cos(π/2) is 6.1×10⁻¹⁷.
        double r = radius.ToDouble();
        double theta = angle.ToDouble();
        return ValueMath.Complex(
            new Complex(r * Trigonometry.Cos(theta, context.AngleUnit), r * Trigonometry.Sin(theta, context.AngleUnit)),
            context);
    }

    private static EvalResult Argument(Value value, EvaluationContext context)
    {
        Complex z = value.ToComplex();
        if (z == Complex.Zero)
        {
            return Value.Zero;
        }

        return ValueMath.Real(Trigonometry.Atan2(z.Imaginary, z.Real, context.AngleUnit), null, context);
    }

    private static Value Bitwise(Value left, Value right, Func<int, int, int> operation)
    {
        return Value.FromBaseN(operation(left.ToInt32(), right.ToInt32()));
    }

    private static EvalResult AtomicWeight(Value atomicNumber, EvaluationContext context)
    {
        if (context.Catalog.AtomicWeights is not { } table)
        {
            return EvalResult.Failure(CalcErrorKind.NotDefined);
        }

        // A round trip through int keeps 2³² + 1 from reading as hydrogen.
        return ValueMath.TryGetInteger(atomicNumber, out long number)
            && (int)number == number
            && table.ByAtomicNumber.TryGetValue((int)number, out AtomicWeight? element)
            ? Value.FromDecimal(element.Weight)
            : ValueMath.MathError;
    }

    private static EvalResult IntegerSlope(Value value)
    {
        // Int and Intg are constant between integers and jump at every integer, where there is no derivative.
        return ValueMath.TryGetInteger(value, out _) ? ValueMath.MathError : Value.Zero;
    }

    private static EvalResult RoundSlope(Value value, EvaluationContext context)
    {
        if (value.Kind != ValueKind.DecimalReal)
        {
            return Value.Zero;
        }

        // Rnd jumps where the value is halfway between two rounded values.
        NumberFormat format = context.Settings.NumberFormat;
        decimal d = Math.Abs(value.ToDecimal());
        if (d == 0m)
        {
            return Value.Zero;
        }

        int decimals = format.Kind == NumberFormatKind.Fix
            ? format.Digits
            : (format.Kind == NumberFormatKind.Sci ? format.Digits : 10) - 1 - DecimalDigits.Exponent(d);
        decimal scaled = decimals >= 0 ? d * DecimalDigits.PowerOfTen(Math.Min(decimals, 28)) : d / DecimalDigits.PowerOfTen(Math.Min(-decimals, 28));
        return scaled - Math.Floor(scaled) == 0.5m ? ValueMath.MathError : Value.Zero;
    }
}
