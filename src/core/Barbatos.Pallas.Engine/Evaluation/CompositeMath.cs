// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Numerics;
using Barbatos.Pallas.LinearAlgebra;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Matrix and vector operations (manual pp. 132-145), entry by entry through <see cref="ValueMath"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sums and products of entries use <see cref="ValueMath"/>, so every entry follows the precision rule and keeps its exact
/// form. A determinant or inverse of a matrix of decimals is exact (<see cref="ExactLinearAlgebra"/>) and rounded once;
/// one with an entry held as <see cref="double"/> is eliminated in <see cref="double"/> with partial pivoting.
/// </para>
/// <para>
/// A matrix of approximate entries is singular when its determinant is within 10⁻¹³ of the Hadamard bound (the product of
/// the rows' lengths, the largest a determinant of those rows can be): the relative tolerance values compare with
/// (decision of 18 Sep 2026). An exact matrix is singular only when its determinant is exactly 0.
/// </para>
/// <para>
/// Sizes that do not fit an operation are a Dimension ERROR (p. 163); an Identity( size outside the profile's range is an
/// Argument ERROR; an operation with operands it does not take, such as <c>MatA+1</c> or <c>sin(MatA)</c>, is a Math
/// ERROR (assumption U20).
/// </para>
/// </remarks>
internal static class CompositeMath
{
    /// <summary>The largest matrix size and vector dimension of the <see cref="CalculatorProfile.Extended"/> profile.</summary>
    public const int ExtendedLimit = 64;

    private static readonly EvalResult DimensionError = EvalResult.Failure(CalcErrorKind.DimensionError);

    /// <summary>Whether an operation is the business of this class: it has a matrix or vector operand, or makes one.</summary>
    public static bool Handles(Operation operation, ReadOnlySpan<Value> arguments)
    {
        if (operation is Operation.Determinant or Operation.Transpose or Operation.Identity or Operation.DotProduct
            or Operation.VectorAngle or Operation.UnitVector)
        {
            return true;
        }

        foreach (Value argument in arguments)
        {
            if (argument.IsComposite)
            {
                return true;
            }
        }

        return false;
    }

    public static EvalResult Evaluate(Operation operation, ReadOnlySpan<Value> arguments, EvaluationContext context)
    {
        return operation switch
        {
            Operation.Negate => Map(arguments[0], entry => ValueMath.Negate(entry, context)),
            Operation.Add => Combine(arguments[0], arguments[1], context, ValueMath.Add),
            Operation.Subtract => Combine(arguments[0], arguments[1], context, ValueMath.Subtract),
            Operation.Multiply => Multiply(arguments[0], arguments[1], context),
            Operation.Divide => Divide(arguments[0], arguments[1], context),
            Operation.Square => Power(arguments[0], 2, context),
            Operation.Cube => Power(arguments[0], 3, context),
            Operation.Reciprocal => Inverse(arguments[0], context),
            Operation.Absolute => Absolute(arguments[0], context),
            Operation.Determinant => arguments[0].Kind == ValueKind.Matrix ? Determinant(arguments[0].ToMatrix(), context) : ValueMath.MathError,
            Operation.Transpose => arguments[0].Kind == ValueKind.Matrix ? Transpose(arguments[0].ToMatrix()) : ValueMath.MathError,
            Operation.Identity => Identity(arguments[0], context),
            Operation.DotProduct => Dot(arguments[0], arguments[1], context),
            Operation.VectorAngle => Angle(arguments[0], arguments[1], context),
            Operation.UnitVector => Unit(arguments[0], context),
            _ => ValueMath.MathError,
        };
    }

    /// <summary>The size limit of a matrix, or the dimension limit of a vector, in a profile (p. 132: 4×4; p. 139: 2 or 3).</summary>
    public static int Limit(CalculatorProfile profile, bool vector) => profile == CalculatorProfile.Standard ? (vector ? 3 : 4) : ExtendedLimit;

    private static EvalResult Map(Value value, Func<Value, EvalResult> function)
    {
        if (value.Kind == ValueKind.Matrix)
        {
            MatrixValue matrix = value.ToMatrix();
            ImmutableArray<Value>.Builder entries = ImmutableArray.CreateBuilder<Value>(matrix.Rows * matrix.Columns);
            for (int row = 0; row < matrix.Rows; row++)
            {
                for (int column = 0; column < matrix.Columns; column++)
                {
                    EvalResult entry = function(matrix[row, column]);
                    if (!entry.Succeeded)
                    {
                        return entry;
                    }

                    entries.Add(entry.Value);
                }
            }

            return Value.FromMatrix(MatrixValue.FromEntries(matrix.Rows, matrix.Columns, entries.MoveToImmutable()));
        }

        if (value.Kind == ValueKind.Vector)
        {
            VectorValue vector = value.ToVector();
            ImmutableArray<Value>.Builder elements = ImmutableArray.CreateBuilder<Value>(vector.Dimension);
            foreach (Value element in vector.Elements)
            {
                EvalResult mapped = function(element);
                if (!mapped.Succeeded)
                {
                    return mapped;
                }

                elements.Add(mapped.Value);
            }

            return Value.FromVector(VectorValue.FromElements(elements.MoveToImmutable()));
        }

        return ValueMath.MathError;
    }

    private static EvalResult Combine(Value left, Value right, EvaluationContext context, Func<Value, Value, EvaluationContext, EvalResult> operation)
    {
        if (left.Kind != right.Kind || !left.IsComposite)
        {
            return ValueMath.MathError;
        }

        if (left.Kind == ValueKind.Matrix)
        {
            MatrixValue a = left.ToMatrix();
            MatrixValue b = right.ToMatrix();
            if (a.Rows != b.Rows || a.Columns != b.Columns)
            {
                return DimensionError;
            }

            return Build(a.Rows, a.Columns, (row, column) => operation(a[row, column], b[row, column], context));
        }

        VectorValue u = left.ToVector();
        VectorValue v = right.ToVector();
        return u.Dimension == v.Dimension
            ? BuildVector(u.Dimension, index => operation(u[index], v[index], context))
            : DimensionError;
    }

    private static EvalResult Multiply(Value left, Value right, EvaluationContext context)
    {
        if (left.IsReal)
        {
            return Map(right, entry => ValueMath.Multiply(left, entry, context));
        }

        if (right.IsReal)
        {
            return Map(left, entry => ValueMath.Multiply(entry, right, context));
        }

        return (left.Kind, right.Kind) switch
        {
            (ValueKind.Matrix, ValueKind.Matrix) => MatrixProduct(left.ToMatrix(), right.ToMatrix(), context),
            (ValueKind.Vector, ValueKind.Vector) => Cross(left.ToVector(), right.ToVector(), context),
            _ => ValueMath.MathError,
        };
    }

    private static EvalResult Divide(Value left, Value right, EvaluationContext context)
    {
        // A matrix or vector may be divided by a number, entry by entry, as the ÷ key offers on the MatAns and VctAns
        // screens (pp. 136, 144; assumption U20).
        return right.IsReal ? Map(left, entry => ValueMath.Divide(entry, right, context)) : ValueMath.MathError;
    }

    private static EvalResult MatrixProduct(MatrixValue a, MatrixValue b, EvaluationContext context)
    {
        return a.Columns == b.Rows
            ? Build(a.Rows, b.Columns, (row, column) => DotSum(a.Columns, k => a[row, k], k => b[k, column], context))
            : DimensionError;
    }

    private static EvalResult Power(Value value, int exponent, EvaluationContext context)
    {
        if (value.Kind != ValueKind.Matrix)
        {
            return ValueMath.MathError;
        }

        // A matrix that is not square is a Dimension ERROR from the product itself.
        MatrixValue matrix = value.ToMatrix();
        EvalResult square = MatrixProduct(matrix, matrix, context);
        return exponent == 3 ? Then(square, result => MatrixProduct(result.ToMatrix(), matrix, context)) : square;
    }

    private static EvalResult Absolute(Value value, EvaluationContext context)
    {
        // Abs of a matrix is taken entry by entry (p. 139); of a vector, it is its length (p. 145).
        if (value.Kind == ValueKind.Vector)
        {
            VectorValue vector = value.ToVector();
            return Length(vector, context);
        }

        return Map(value, entry => Operations.Evaluate(Operation.Absolute, [entry], context));
    }

    private static EvalResult Transpose(MatrixValue matrix)
    {
        ImmutableArray<Value>.Builder entries = ImmutableArray.CreateBuilder<Value>(matrix.Rows * matrix.Columns);
        for (int row = 0; row < matrix.Columns; row++)
        {
            for (int column = 0; column < matrix.Rows; column++)
            {
                entries.Add(matrix[column, row]);
            }
        }

        return Value.FromMatrix(MatrixValue.FromEntries(matrix.Columns, matrix.Rows, entries.MoveToImmutable()));
    }

    private static EvalResult Identity(Value size, EvaluationContext context)
    {
        // p. 139: from 1 to 4.
        if (!ValueMath.TryGetInteger(size, out long n) || n < 1 || n > Limit(context.Profile, vector: false))
        {
            return EvalResult.Failure(CalcErrorKind.ArgumentError);
        }

        return Build((int)n, (int)n, (row, column) => row == column ? Value.One : Value.Zero);
    }

    private static EvalResult Determinant(MatrixValue matrix, EvaluationContext context)
    {
        if (!matrix.IsSquare)
        {
            return DimensionError;
        }

        if (TryGetDecimals(matrix, out decimal[,] entries, out bool exact))
        {
            (BigInteger numerator, BigInteger denominator) = ExactLinearAlgebra.Determinant(entries);
            return !exact && IsNegligible(ValueMath.RatioToDouble(numerator, denominator), matrix)
                ? Value.FromDouble(0d)
                : ValueMath.FromRatio(numerator, denominator, exact, context);
        }

        double determinant = Eliminate(matrix, out _);
        return IsNegligible(determinant, matrix) ? Value.FromDouble(0d) : ValueMath.Real(determinant, null, context);
    }

    private static EvalResult Inverse(Value value, EvaluationContext context)
    {
        if (value.Kind != ValueKind.Matrix)
        {
            return ValueMath.MathError;
        }

        MatrixValue matrix = value.ToMatrix();
        if (!matrix.IsSquare)
        {
            return DimensionError;
        }

        // p. 138: a matrix whose determinant is 0 has no inverse.
        if (TryGetDecimals(matrix, out decimal[,] entries, out bool exact))
        {
            (BigInteger determinantNumerator, BigInteger determinantDenominator) = ExactLinearAlgebra.Determinant(entries);
            if (!ExactLinearAlgebra.TryInvert(entries, out BigInteger[,] numerators, out BigInteger denominator)
                || (!exact && IsNegligible(ValueMath.RatioToDouble(determinantNumerator, determinantDenominator), matrix)))
            {
                return ValueMath.MathError;
            }

            return Build(matrix.Rows, matrix.Columns, (row, column) => ValueMath.FromRatio(numerators[row, column], denominator, exact, context));
        }

        double determinant = Eliminate(matrix, out double[,] inverse);
        return IsNegligible(determinant, matrix)
            ? ValueMath.MathError
            : Build(matrix.Rows, matrix.Columns, (row, column) => ValueMath.Real(inverse[row, column], null, context));
    }

    private static EvalResult Dot(Value left, Value right, EvaluationContext context)
    {
        if (left.Kind != ValueKind.Vector || right.Kind != ValueKind.Vector)
        {
            return ValueMath.MathError;
        }

        VectorValue u = left.ToVector();
        VectorValue v = right.ToVector();
        return u.Dimension == v.Dimension ? DotSum(u.Dimension, index => u[index], index => v[index], context) : DimensionError;
    }

    private static EvalResult Cross(VectorValue u, VectorValue v, EvaluationContext context)
    {
        // p. 144: both of two or both of three dimensions; (a₁, a₂) × (b₁, b₂) = (0, 0, a₁b₂ − a₂b₁).
        if (u.Dimension != v.Dimension || u.Dimension is not (2 or 3))
        {
            return DimensionError;
        }

        static Value Component(VectorValue vector, int index) => index < vector.Dimension ? vector[index] : Value.Zero;
        return BuildVector(3, index =>
        {
            int next = (index + 1) % 3;
            int last = (index + 2) % 3;
            return Then(ValueMath.Multiply(Component(u, next), Component(v, last), context), first =>
                Then(ValueMath.Multiply(Component(u, last), Component(v, next), context), second => ValueMath.Subtract(first, second, context)));
        });
    }

    private static EvalResult Angle(Value left, Value right, EvaluationContext context)
    {
        // Dot checks that both are vectors of one dimension before their lengths are taken.
        EvalResult cosine = Then(Dot(left, right, context), dot =>
            Then(Length(left.ToVector(), context), first =>
            Then(Length(right.ToVector(), context), second =>
            Then(ValueMath.Multiply(first, second, context), product => ValueMath.Divide(dot, product, context)))));

        // Cauchy–Schwarz keeps the cosine in [−1, 1], so a cosine outside is rounding: (√2, cos 40°) and twice it give
        // 1.0000000000000022 from their 15-digit entries.
        return Then(cosine, value =>
        {
            double c = value.ToDouble();
            return Operations.Evaluate(Operation.Acos, [Math.Abs(c) > 1d ? Value.FromDecimal(Math.Sign(c)) : value], context);
        });
    }

    private static EvalResult Unit(Value value, EvaluationContext context)
    {
        if (value.Kind != ValueKind.Vector)
        {
            return ValueMath.MathError;
        }

        return Then(Length(value.ToVector(), context), length => Map(value, element => ValueMath.Divide(element, length, context)));
    }

    private static EvalResult Length(VectorValue vector, EvaluationContext context)
    {
        return Then(DotSum(vector.Dimension, index => vector[index], index => vector[index], context), squares => ValueMath.SquareRoot(squares, context));
    }

    private static EvalResult DotSum(int count, Func<int, Value> left, Func<int, Value> right, EvaluationContext context)
    {
        EvalResult sum = Value.Zero;
        for (int k = 0; k < count; k++)
        {
            int index = k;
            sum = Then(sum, total => Then(ValueMath.Multiply(left(index), right(index), context), product => ValueMath.Add(total, product, context)));
        }

        return sum;
    }

    /// <summary>Continues with a value, or passes an error on.</summary>
    private static EvalResult Then(EvalResult result, Func<Value, EvalResult> next) => result.Succeeded ? next(result.Value) : result;

    private static EvalResult Build(int rows, int columns, Func<int, int, EvalResult> entry)
    {
        ImmutableArray<Value>.Builder entries = ImmutableArray.CreateBuilder<Value>(rows * columns);
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                EvalResult value = entry(row, column);
                if (!value.Succeeded)
                {
                    return value;
                }

                entries.Add(value.Value);
            }
        }

        return Value.FromMatrix(MatrixValue.FromEntries(rows, columns, entries.MoveToImmutable()));
    }

    private static EvalResult BuildVector(int dimension, Func<int, EvalResult> element)
    {
        ImmutableArray<Value>.Builder elements = ImmutableArray.CreateBuilder<Value>(dimension);
        for (int index = 0; index < dimension; index++)
        {
            EvalResult value = element(index);
            if (!value.Succeeded)
            {
                return value;
            }

            elements.Add(value.Value);
        }

        return Value.FromVector(VectorValue.FromElements(elements.MoveToImmutable()));
    }

    /// <summary>Returns the entries as decimals when every one is held as one, and whether all are exact.</summary>
    private static bool TryGetDecimals(MatrixValue matrix, out decimal[,] entries, out bool exact)
    {
        entries = new decimal[matrix.Rows, matrix.Columns];
        exact = true;
        for (int row = 0; row < matrix.Rows; row++)
        {
            for (int column = 0; column < matrix.Columns; column++)
            {
                Value entry = matrix[row, column];
                if (entry.Kind != ValueKind.DecimalReal)
                {
                    return false;
                }

                entries[row, column] = entry.ToDecimal();
                exact &= entry.IsExact;
            }
        }

        return true;
    }

    /// <summary>Whether a determinant of approximate entries is 0 within the relative tolerance of the Hadamard bound.</summary>
    private static bool IsNegligible(double determinant, MatrixValue matrix)
    {
        double bound = 1d;
        for (int row = 0; row < matrix.Rows; row++)
        {
            double squares = 0d;
            for (int column = 0; column < matrix.Columns; column++)
            {
                double entry = matrix[row, column].ToDouble();
                squares += entry * entry;
            }

            bound *= Math.Sqrt(squares);
        }

        return Math.Abs(determinant) <= (double)ValueMath.RelativeTolerance * bound;
    }

    /// <summary>Gauss–Jordan elimination with partial pivoting in double, for matrices with an entry decimal cannot hold.</summary>
    private static double Eliminate(MatrixValue matrix, out double[,] inverse)
    {
        int size = matrix.Rows;
        double[,] a = new double[size, 2 * size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                a[row, column] = matrix[row, column].ToDouble();
            }

            a[row, size + row] = 1d;
        }

        double determinant = 1d;
        for (int k = 0; k < size; k++)
        {
            // Partial pivoting. A pivot of 0 leaves the determinant exactly 0, which IsNegligible always accepts, so the
            // inverse it spoils is never used: the rows below it are 0 in that column and are not touched.
            int pivot = Enumerable.Range(k, size - k).MaxBy(row => Math.Abs(a[row, k]));
            if (pivot != k)
            {
                for (int column = 0; column < 2 * size; column++)
                {
                    (a[k, column], a[pivot, column]) = (a[pivot, column], a[k, column]);
                }

                determinant = -determinant;
            }

            double diagonal = a[k, k];
            determinant *= diagonal;
            for (int column = 0; column < 2 * size; column++)
            {
                a[k, column] /= diagonal;
            }

            for (int row = 0; row < size; row++)
            {
                double factor = a[row, k];
                if (row != k && factor != 0d)
                {
                    for (int column = 0; column < 2 * size; column++)
                    {
                        a[row, column] -= factor * a[k, column];
                    }
                }
            }
        }

        inverse = new double[size, size];
        for (int row = 0; row < size; row++)
        {
            for (int column = 0; column < size; column++)
            {
                inverse[row, column] = a[row, size + column];
            }
        }

        return determinant;
    }
}
