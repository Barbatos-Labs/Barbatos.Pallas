// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A calculator value: a real number held as <see cref="decimal"/> or <see cref="double"/>, a complex number, a 32-bit
/// Base-N integer, or a matrix or vector of real numbers.
/// </summary>
/// <remarks>
/// <para>
/// Which type holds a value follows docs/PRECISION.md §3. Arithmetic on decimal input stays <see cref="decimal"/>. A
/// <see cref="double"/> result, such as a sine, becomes a <see cref="decimal"/> of 15 significant digits when its magnitude
/// is at least 10⁻¹⁴ and below 7.9×10²⁸. Outside that range it stays <see cref="double"/>, because <see cref="decimal"/>
/// would keep fewer than 15 significant digits of it, or overflow.
/// </para>
/// <para>
/// A value computed exactly from square roots and π also remembers its exact form, so that <c>√(2)×√(2)</c> is exactly 2
/// and <c>π÷2</c> is exactly a right angle for the trigonometric functions (decision of 18 Sep 2026).
/// </para>
/// </remarks>
public readonly struct Value : IEquatable<Value>
{
    /// <summary>10⁻¹⁴: below this magnitude a <see cref="decimal"/> keeps fewer than 15 significant digits (PRECISION.md §3).</summary>
    internal const decimal SmallestPreciseDecimal = 0.00000000000001m;

    private const double SmallestDecimalMagnitude = 1e-14;

    // decimal.MaxValue is 7.9228…×10²⁸; staying below 7.9×10²⁸ keeps the conversion from throwing.
    private const double LargestDecimalMagnitude = 7.9e28;

    private readonly decimal _decimal;
    private readonly double _real;
    private readonly double _imaginary;
    private readonly int _baseN;
    private readonly ExactForm? _form;
    private readonly object? _composite;

    private Value(ValueKind kind, decimal decimalValue, double real, double imaginary, int baseN, bool isExact, ExactForm? form)
    {
        Kind = kind;
        _decimal = decimalValue;
        _real = real;
        _imaginary = imaginary;
        _baseN = baseN;
        IsExact = isExact;
        _form = form;
    }

    private Value(ValueKind kind, object composite)
    {
        Kind = kind;
        _composite = composite;
    }

    /// <summary>Gets exact zero.</summary>
    public static Value Zero { get; } = FromDecimal(0m);

    /// <summary>Gets exact one.</summary>
    public static Value One { get; } = FromDecimal(1m);

    /// <summary>Gets π, from <see cref="double.Pi"/>, with its exact form.</summary>
    public static Value Pi { get; } = FromApproximation(double.Pi, ExactForm.Pi);

    /// <summary>Gets e, from <see cref="double.E"/>.</summary>
    public static Value E { get; } = FromApproximation(double.E, ExactForm.E);

    /// <summary>Gets the type that holds the value.</summary>
    public ValueKind Kind { get; }

    /// <summary>
    /// Gets whether the value is exact: a <see cref="decimal"/> computed from decimal input without passing through
    /// <see cref="double"/> (rounded at most at <see cref="decimal"/>'s 28th digit), or a Base-N integer.
    /// </summary>
    public bool IsExact { get; }

    /// <summary>Gets whether the value is a real number, held as <see cref="decimal"/> or <see cref="double"/>.</summary>
    public bool IsReal => Kind is ValueKind.DecimalReal or ValueKind.DoubleReal;

    internal ExactForm? Form => _form;

    /// <summary>Gets the exact form, taking an exact <see cref="decimal"/> as a rational; <see langword="null"/> when the value has none.</summary>
    internal ExactForm? FormOrRational => _form ?? (Kind == ValueKind.DecimalReal && IsExact ? ExactForm.Rational(_decimal) : null);

    internal bool IsZero => Kind switch
    {
        ValueKind.DecimalReal => _decimal == 0m,
        ValueKind.BaseN => _baseN == 0,
        ValueKind.Matrix or ValueKind.Vector => false,
        _ => _real == 0d && _imaginary == 0d,
    };

    /// <summary>Gets whether the value is a matrix or a vector.</summary>
    internal bool IsComposite => _composite is not null;

    /// <summary>Compares two values for equality of kind, exactness and number.</summary>
    public static bool operator ==(Value left, Value right) => left.Equals(right);

    /// <summary>Compares two values for inequality.</summary>
    public static bool operator !=(Value left, Value right) => !left.Equals(right);

    /// <summary>Creates an exact value from a <see cref="decimal"/>.</summary>
    /// <param name="value">The number.</param>
    /// <returns>An exact <see cref="ValueKind.DecimalReal"/> value.</returns>
    public static Value FromDecimal(decimal value)
    {
        return new Value(ValueKind.DecimalReal, value, 0d, 0d, 0, isExact: true, form: null);
    }

    /// <summary>Creates an approximate value from a <see cref="double"/>, applying the precision rule.</summary>
    /// <param name="value">A finite number.</param>
    /// <returns>A <see cref="ValueKind.DecimalReal"/> of 15 significant digits when the magnitude allows it; otherwise <see cref="ValueKind.DoubleReal"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or infinite.</exception>
    public static Value FromDouble(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A calculator value is finite.");
        }

        return FromApproximation(value, form: null);
    }

    /// <summary>Creates a complex value.</summary>
    /// <param name="value">A complex number with finite parts.</param>
    /// <returns>A <see cref="ValueKind.Complex"/> value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A part of <paramref name="value"/> is NaN or infinite.</exception>
    public static Value FromComplex(Complex value)
    {
        if (!double.IsFinite(value.Real) || !double.IsFinite(value.Imaginary))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A calculator value is finite.");
        }

        return CreateComplex(value.Real, value.Imaginary);
    }

    /// <summary>Creates a matrix value.</summary>
    /// <param name="matrix">The matrix.</param>
    /// <returns>A <see cref="ValueKind.Matrix"/> value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is <see langword="null"/>.</exception>
    public static Value FromMatrix(MatrixValue matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);
        return new Value(ValueKind.Matrix, matrix);
    }

    /// <summary>Creates a vector value.</summary>
    /// <param name="vector">The vector.</param>
    /// <returns>A <see cref="ValueKind.Vector"/> value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vector"/> is <see langword="null"/>.</exception>
    public static Value FromVector(VectorValue vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        return new Value(ValueKind.Vector, vector);
    }

    /// <summary>Creates a Base-N value.</summary>
    /// <param name="value">The 32-bit two's complement integer.</param>
    /// <returns>A <see cref="ValueKind.BaseN"/> value.</returns>
    public static Value FromBaseN(int value)
    {
        return new Value(ValueKind.BaseN, 0m, 0d, 0d, value, isExact: true, form: null);
    }

    /// <summary>Returns the number held as <see cref="decimal"/>.</summary>
    /// <returns>The <see cref="decimal"/>; a Base-N integer converts exactly.</returns>
    /// <exception cref="InvalidOperationException">The value is a <see cref="double"/> or complex number.</exception>
    public decimal ToDecimal()
    {
        return Kind switch
        {
            ValueKind.DecimalReal => _decimal,
            ValueKind.BaseN => _baseN,
            _ => throw new InvalidOperationException($"A {Kind} value is not held as decimal."),
        };
    }

    /// <summary>Returns the real number as <see cref="double"/>.</summary>
    /// <returns>The number; a value with an exact form is computed from the form, so π÷2 is exactly <c>double.Pi / 2</c>.</returns>
    /// <exception cref="InvalidOperationException">The value is a complex number, a matrix or a vector.</exception>
    public double ToDouble()
    {
        return Kind switch
        {
            ValueKind.DecimalReal => _form?.ToDouble() ?? (double)_decimal,
            ValueKind.DoubleReal => _real,
            ValueKind.BaseN => _baseN,
            _ => throw new InvalidOperationException($"A {Kind} value has no real double."),
        };
    }

    /// <summary>Returns the value as a complex number.</summary>
    /// <returns>The complex number; a real value has imaginary part 0.</returns>
    public Complex ToComplex()
    {
        return Kind == ValueKind.Complex ? new Complex(_real, _imaginary) : new Complex(ToDouble(), 0d);
    }

    /// <summary>Returns the matrix.</summary>
    /// <returns>The matrix.</returns>
    /// <exception cref="InvalidOperationException">The value is not a matrix.</exception>
    public MatrixValue ToMatrix()
    {
        return _composite as MatrixValue ?? throw new InvalidOperationException($"A {Kind} value is not a matrix.");
    }

    /// <summary>Returns the vector.</summary>
    /// <returns>The vector.</returns>
    /// <exception cref="InvalidOperationException">The value is not a vector.</exception>
    public VectorValue ToVector()
    {
        return _composite as VectorValue ?? throw new InvalidOperationException($"A {Kind} value is not a vector.");
    }

    /// <summary>Returns the Base-N integer.</summary>
    /// <returns>The 32-bit integer.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Base-N integer.</exception>
    public int ToInt32()
    {
        return Kind == ValueKind.BaseN ? _baseN : throw new InvalidOperationException($"A {Kind} value is not a Base-N integer.");
    }

    /// <inheritdoc/>
    public bool Equals(Value other)
    {
        return Kind == other.Kind
            && IsExact == other.IsExact
            && _decimal == other._decimal
            && _real.Equals(other._real)
            && _imaginary.Equals(other._imaginary)
            && _baseN == other._baseN
            && Equals(_composite, other._composite);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Value other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, IsExact, _decimal, _real, _imaginary, _baseN, _composite);

    /// <summary>Returns the number in the invariant culture, for diagnostics; calculator display is the formatter's job.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        return Kind switch
        {
            ValueKind.DecimalReal => _decimal.ToString(CultureInfo.InvariantCulture),
            ValueKind.DoubleReal => _real.ToString("R", CultureInfo.InvariantCulture),
            ValueKind.Complex => string.Create(CultureInfo.InvariantCulture, $"({_real:R}, {_imaginary:R})"),
            ValueKind.Matrix or ValueKind.Vector => _composite!.ToString()!,
            _ => _baseN.ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>Creates a <see cref="decimal"/> value with an exactness and a form.</summary>
    /// <remarks>
    /// The form is never rational: a rational result is created as an exact decimal (<see cref="FromApproximation"/> and
    /// ValueMath.FromForms), so a value's form always stands for a square root or π.
    /// </remarks>
    internal static Value FromDecimal(decimal value, bool isExact, ExactForm? form)
    {
        return new Value(ValueKind.DecimalReal, value, 0d, 0d, 0, isExact && form is null, form);
    }

    /// <summary>Creates an approximate value from a finite <see cref="double"/>, applying the precision rule.</summary>
    internal static Value FromApproximation(double value, ExactForm? form)
    {
        if (form is not null && form.IsRational)
        {
            return FromDecimal(form.RationalValue);
        }

        double magnitude = Math.Abs(value);
        if (magnitude == 0d)
        {
            return new Value(ValueKind.DecimalReal, 0m, 0d, 0d, 0, isExact: false, form);
        }

        return magnitude is >= SmallestDecimalMagnitude and < LargestDecimalMagnitude
            ? new Value(ValueKind.DecimalReal, (decimal)value, 0d, 0d, 0, isExact: false, form)
            : new Value(ValueKind.DoubleReal, 0m, value, 0d, 0, isExact: false, form);
    }

    internal static Value CreateComplex(double real, double imaginary)
    {
        return new Value(ValueKind.Complex, 0m, real, imaginary, 0, isExact: false, form: null);
    }
}
