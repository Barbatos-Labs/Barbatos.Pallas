// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Numerics;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.LinearAlgebra;
using Barbatos.Pallas.Solvers;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The calculations of the Equation and Inequality applications (manual pp. 114-125).
/// </summary>
/// <remarks>
/// <para>
/// Simultaneous equations are solved exactly: each row is scaled into integers, which leaves the solution where it is,
/// and eliminated without fractions (<see cref="ExactLinearAlgebra"/>). A singular system has infinitely many solutions
/// when its constants do not raise the rank, and none otherwise.
/// </para>
/// <para>
/// A polynomial is scaled into integer coefficients the same way. Its rational roots are recovered exactly, from the
/// continued fraction of a numeric root verified against the integer polynomial, and divided out; what is left of degree
/// 2 is solved with its discriminant, so the roots keep their exact form, <c>-1+√(3)</c> or <c>-3⌟4+√(23)⌟4i</c>. Only a
/// cubic or a quartic with no rational root is left to <see cref="PolynomialRoots.Find"/>, and how many of those roots
/// are real is decided exactly, by Sturm's theorem, not by a tolerance on the imaginary part.
/// </para>
/// <para>
/// An inequality is the sign of the polynomial between its real roots, taken exactly at a rational point of each stretch
/// (<see cref="IntegerPolynomial.SignAt"/>). The stretches that satisfy it, with the roots themselves where the relation
/// allows equality, are merged into the intervals the calculator writes as <c>x≤-3, 1≤x</c>.
/// </para>
/// <para>
/// Assumption U25 (docs/CONFORMANCE.md) covers what the manual does not state: the order of the roots and of the
/// extrema, a leading coefficient of 0, which roots Complex Roots off leaves, and a repeated root.
/// </para>
/// </remarks>
internal static class EquationSolver
{
    /// <summary>The unknowns of a system, as the calculator names them (p. 114).</summary>
    private static readonly string[] UnknownNames = ["x", "y", "z", "t"];

    /// <summary>The roots of a polynomial, as the calculator names them (p. 116).</summary>
    private static readonly string[] RootNames = ["x₁", "x₂", "x₃", "x₄"];

    private static readonly Value Two = Value.FromDecimal(2m);
    private static readonly Value Three = Value.FromDecimal(3m);
    private static readonly Value Four = Value.FromDecimal(4m);

    /// <summary>Solves a system of linear equations, given as the rows of its augmented matrix.</summary>
    public static SimultaneousSolution Simultaneous(Value[,] augmented, EvaluationContext context, Func<string, Value, Calculation> line)
    {
        int size = augmented.GetLength(0);
        BigInteger[,] integers = new BigInteger[size, size + 1];
        bool exact = true;
        for (int row = 0; row < size; row++)
        {
            Value[] values = new Value[size + 1];
            for (int column = 0; column <= size; column++)
            {
                values[column] = augmented[row, column];
                if (!values[column].IsReal)
                {
                    return new SimultaneousSolution(SolutionOutcome.Solved, [], Failure(CalcErrorKind.MathError));
                }

                exact &= values[column].IsExact;
            }

            (BigInteger[] scaled, _) = ScaledValues.Scaled(values);
            for (int column = 0; column <= size; column++)
            {
                integers[row, column] = scaled[column];
            }
        }

        if (!ExactLinearAlgebra.TrySolve(integers, out BigInteger[] numerators, out BigInteger denominator))
        {
            // Dropping the constants lowers the rank exactly when the system contradicts itself (p. 116).
            BigInteger[,] coefficients = new BigInteger[size, size];
            for (int row = 0; row < size; row++)
            {
                for (int column = 0; column < size; column++)
                {
                    coefficients[row, column] = integers[row, column];
                }
            }

            bool infinite = ExactLinearAlgebra.Rank(coefficients) == ExactLinearAlgebra.Rank(integers);
            return new SimultaneousSolution(infinite ? SolutionOutcome.InfiniteSolutions : SolutionOutcome.NoSolution, [], null);
        }

        ImmutableArray<Calculation>.Builder unknowns = ImmutableArray.CreateBuilder<Calculation>(size);
        for (int i = 0; i < size; i++)
        {
            EvalResult value = ValueMath.FromRatio(numerators[i], denominator, exact, context);
            if (!value.Succeeded)
            {
                return new SimultaneousSolution(SolutionOutcome.Solved, [], Failure(value.Error!.Value));
            }

            unknowns.Add(line(UnknownNames[i], value.Value));
        }

        return new SimultaneousSolution(SolutionOutcome.Solved, unknowns.MoveToImmutable(), null);
    }

    /// <summary>Solves a polynomial equation, given its coefficients from the highest degree down.</summary>
    public static PolynomialSolution Polynomial(IReadOnlyList<Value> coefficients, EvaluationContext context, Func<string, Value, Calculation> line)
    {
        if (!TryRoots(coefficients, context, out ImmutableArray<RootValue> roots, out CalcError? error))
        {
            return new PolynomialSolution(SolutionOutcome.Solved, [], [], error);
        }

        // Complex Roots off leaves the real roots, and none of them is the calculator's "No Real Roots" (p. 118).
        ImmutableArray<RootValue> shown = context.Settings.ComplexRoots ? roots : [.. roots.Where(root => root.Imaginary is null)];
        if (shown.IsEmpty)
        {
            return new PolynomialSolution(SolutionOutcome.NoRealRoots, [], [], null);
        }

        ImmutableArray<PolynomialRoot>.Builder built = ImmutableArray.CreateBuilder<PolynomialRoot>(shown.Length);
        for (int i = 0; i < shown.Length; i++)
        {
            Calculation real = line(RootNames[i], shown[i].Real);
            built.Add(new PolynomialRoot(real, shown[i].Imaginary is { } imaginary ? line(RootNames[i] + "i", imaginary) : null));
        }

        if (!TryExtrema(coefficients, context, line, out ImmutableArray<PolynomialExtremum> extrema, out CalcError? extremumError))
        {
            return new PolynomialSolution(SolutionOutcome.Solved, [], [], extremumError);
        }

        return new PolynomialSolution(SolutionOutcome.Solved, built.MoveToImmutable(), extrema, null);
    }

    /// <summary>Solves a polynomial inequality, given its coefficients from the highest degree down.</summary>
    public static InequalitySolution Inequality(IReadOnlyList<Value> coefficients, RelationOperator relation, EvaluationContext context, Func<string, Value, Calculation> line)
    {
        if (!TryRoots(coefficients, context, out ImmutableArray<RootValue> roots, out CalcError? error))
        {
            return new InequalitySolution(SolutionOutcome.Solved, [], string.Empty, error);
        }

        // Where the sign can change: the real roots, each of them once, in increasing order.
        List<Value> points = [];
        foreach (Value root in roots.Where(root => root.Imaginary is null).Select(root => root.Real).Order(RealOrder.Instance))
        {
            if (points.Count == 0 || ValueMath.CompareReal(points[^1], root) != 0)
            {
                points.Add(root);
            }
        }

        (BigInteger[] scaled, _) = ScaledValues.Scaled(coefficients);
        BigInteger[] ascending = [.. Enumerable.Reverse(scaled)];
        bool equality = relation is RelationOperator.LessOrEqual or RelationOperator.GreaterOrEqual;
        bool wantPositive = relation is RelationOperator.Greater or RelationOperator.GreaterOrEqual;

        // The stretches of the line in order: an interval, a root, an interval, …, an interval.
        bool[] satisfied = new bool[(2 * points.Count) + 1];
        for (int i = 0; i <= points.Count; i++)
        {
            EvalResult sample = Sample(points, i, context);
            if (!sample.Succeeded)
            {
                return new InequalitySolution(SolutionOutcome.Solved, [], string.Empty, Failure(sample.Error!.Value));
            }

            int sign = SignAt(ascending, sample.Value);
            satisfied[2 * i] = wantPositive ? sign > 0 : sign < 0;
        }

        for (int i = 0; i < points.Count; i++)
        {
            satisfied[(2 * i) + 1] = equality;
        }

        // Every stretch, the roots included, is the calculator's "All Real Numbers"; none of them is "No Solution" (p. 125).
        if (satisfied.All(piece => piece))
        {
            return new InequalitySolution(SolutionOutcome.AllRealNumbers, [], string.Empty, null);
        }

        ImmutableArray<SolutionInterval> intervals = Merge(satisfied, points, line);
        if (intervals.IsEmpty)
        {
            return new InequalitySolution(SolutionOutcome.NoSolution, [], string.Empty, null);
        }

        string separator = context.Settings.DecimalMark == DecimalMark.Comma ? "; " : ", ";
        return new InequalitySolution(SolutionOutcome.Solved, intervals, string.Join(separator, intervals.Select(interval => interval.Text)), null);
    }

    /// <summary>The roots of a polynomial, exact wherever it factors over the rationals into factors of degree 2 or less.</summary>
    private static bool TryRoots(IReadOnlyList<Value> coefficients, EvaluationContext context, out ImmutableArray<RootValue> roots, out CalcError? error)
    {
        roots = [];
        error = null;
        if (coefficients.Any(coefficient => !coefficient.IsReal) || coefficients[0].IsZero)
        {
            error = Failure(CalcErrorKind.MathError);
            return false;
        }

        bool exact = coefficients.All(coefficient => coefficient.IsExact);
        (BigInteger[] scaled, _) = ScaledValues.Scaled(coefficients);
        BigInteger[] rest = [.. Enumerable.Reverse(scaled)];

        ValueChain chain = new();
        List<RootValue> found = [];
        while (IntegerPolynomial.Degree(rest) > 2 && TryRationalRoot(rest, out BigInteger numerator, out BigInteger denominator))
        {
            found.Add(new RootValue(chain.Step(ValueMath.FromRatio(numerator, denominator, exact, context)), null));
            rest = IntegerPolynomial.Deflate(rest, numerator, denominator);
        }

        // With the rational roots gone, a repeated factor can only be the square of a quadratic: a repeated linear factor
        // over the rationals would have a rational root, and an irreducible quadratic cannot repeat inside degree 4.
        BigInteger[] squareFree = IntegerPolynomial.SquareFree(rest);
        int repeat = IntegerPolynomial.Degree(rest) / IntegerPolynomial.Degree(squareFree);
        if (!TryRemainingRoots(squareFree, repeat, exact, context, found, out error) || !chain.Succeeded)
        {
            error ??= chain.Error;
            return false;
        }

        // The calculator shows x₁ before x₂: by decreasing real part, and a conjugate pair with the positive one first.
        found.Sort(static (left, right) =>
        {
            int byReal = RealOrder.Instance.Compare(right.Real, left.Real);
            return byReal != 0 ? byReal : RealOrder.Instance.Compare(right.Imaginary ?? Value.Zero, left.Imaginary ?? Value.Zero);
        });

        roots = [.. found];
        return true;
    }

    /// <summary>The roots of what is left after the rational ones: exact for degree 2, iterated for degree 3 and 4.</summary>
    private static bool TryRemainingRoots(BigInteger[] rest, int repeat, bool exact, EvaluationContext context, List<RootValue> found, out CalcError? error)
    {
        error = null;
        if (IntegerPolynomial.Degree(rest) == 2)
        {
            ValueChain chain = new();
            Value a = chain.Step(ValueMath.FromBigInteger(rest[2], exact, context));
            Value b = chain.Step(ValueMath.FromBigInteger(rest[1], exact, context));
            Value c = chain.Step(ValueMath.FromBigInteger(rest[0], exact, context));
            if (!TryQuadraticRoots(a, b, c, context, out RootValue first, out RootValue second, out error) || !chain.Succeeded)
            {
                error ??= chain.Error;
                return false;
            }

            for (int i = 0; i < repeat; i++)
            {
                found.Add(first);
                found.Add(second);
            }

            return true;
        }

        // A square-free polynomial has exactly as many real roots as Sturm's theorem counts, so the roots with the
        // smallest imaginary parts are those, and no tolerance decides what is real.
        Complex[] numeric = [.. PolynomialRoots.Find(ToDoubles(rest)).OrderBy(root => Math.Abs(root.Imaginary))];
        int realCount = IntegerPolynomial.CountRealRoots(rest);
        ValueChain values = new();
        for (int i = 0; i < numeric.Length; i++)
        {
            Value real = values.Step(ValueMath.Real(numeric[i].Real, null, context));
            Value imaginary = i < realCount ? Value.Zero : values.Step(ValueMath.Real(numeric[i].Imaginary, null, context));
            for (int copy = 0; copy < repeat; copy++)
            {
                found.Add(new RootValue(real, i < realCount ? null : imaginary));
            }
        }

        error = values.Error;
        return values.Succeeded;
    }

    /// <summary>The two roots of a·x² + b·x + c, with their exact forms: (−b ± √(b² − 4ac)) / 2a.</summary>
    private static bool TryQuadraticRoots(Value a, Value b, Value c, EvaluationContext context, out RootValue first, out RootValue second, out CalcError? error)
    {
        first = default;
        second = default;
        ValueChain chain = new();
        Value square = chain.Step(ValueMath.Multiply(b, b, context));
        Value product = chain.Step(ValueMath.Multiply(Four, chain.Step(ValueMath.Multiply(a, c, context)), context));
        Value discriminant = chain.Step(ValueMath.Subtract(square, product, context));
        Value twice = chain.Step(ValueMath.Multiply(Two, a, context));
        Value center = chain.Step(ValueMath.Divide(chain.Step(ValueMath.Negate(b, context)), twice, context));

        // A negative discriminant gives the imaginary part √(−D)/2a: √ of a negative number is a Math ERROR outside Complex.
        bool complex = ValueMath.CompareReal(discriminant, Value.Zero) < 0;
        Value magnitude = complex ? chain.Step(ValueMath.Negate(discriminant, context)) : discriminant;
        Value offset = chain.Step(ValueMath.Divide(chain.Step(ValueMath.SquareRoot(magnitude, context)), twice, context));
        Value below = chain.Step(complex ? ValueMath.Negate(offset, context) : ValueMath.Subtract(center, offset, context));
        Value above = complex ? center : chain.Step(ValueMath.Add(center, offset, context));
        if (!chain.Succeeded)
        {
            error = chain.Error;
            return false;
        }

        error = null;
        first = complex ? new RootValue(center, offset) : new RootValue(above, null);
        second = complex ? new RootValue(center, below) : new RootValue(below, null);
        return true;
    }

    /// <summary>The local extrema: one for a quadratic, two or none for a cubic, none beyond (p. 117).</summary>
    private static bool TryExtrema(
        IReadOnlyList<Value> coefficients,
        EvaluationContext context,
        Func<string, Value, Calculation> line,
        out ImmutableArray<PolynomialExtremum> extrema,
        out CalcError? error)
    {
        extrema = [];
        error = null;
        int degree = coefficients.Count - 1;
        List<Value> positions = [];
        ValueChain chain = new();
        if (degree == 2)
        {
            Value twice = chain.Step(ValueMath.Multiply(Two, coefficients[0], context));
            positions.Add(chain.Step(ValueMath.Divide(chain.Step(ValueMath.Negate(coefficients[1], context)), twice, context)));
        }
        else if (degree == 3)
        {
            // The derivative 3a·x² + 2b·x + c: two distinct real roots are a maximum and a minimum, and anything else,
            // a double root or a complex pair, is the calculator's "No Local Max/Min".
            Value a = chain.Step(ValueMath.Multiply(Three, coefficients[0], context));
            Value b = chain.Step(ValueMath.Multiply(Two, coefficients[1], context));
            if (!TryQuadraticRoots(a, b, coefficients[2], context, out RootValue first, out RootValue second, out error))
            {
                return false;
            }

            if (first.Imaginary is null && ValueMath.CompareReal(first.Real, second.Real) != 0)
            {
                positions.Add(first.Real);
                positions.Add(second.Real);
            }
        }

        positions.Sort(RealOrder.Instance);
        ImmutableArray<PolynomialExtremum>.Builder builder = ImmutableArray.CreateBuilder<PolynomialExtremum>(positions.Count);
        bool opensUpward = RealOrder.Instance.Compare(coefficients[0], Value.Zero) > 0;
        for (int i = 0; i < positions.Count; i++)
        {
            Value y = chain.Step(ValueAt(coefficients, positions[i], context));

            // A quadratic has its minimum when it opens upward; a cubic that rises has its maximum first.
            bool minimum = degree == 2 ? opensUpward : opensUpward == (i > 0);
            builder.Add(new PolynomialExtremum(minimum ? ExtremumKind.Minimum : ExtremumKind.Maximum, line("x", positions[i]), line("y", y)));
        }

        if (!chain.Succeeded)
        {
            error = chain.Error;
            return false;
        }

        extrema = builder.MoveToImmutable();
        return true;
    }

    /// <summary>A rational root of the integer polynomial, recovered from an approximation and verified exactly.</summary>
    private static bool TryRationalRoot(BigInteger[] coefficients, out BigInteger numerator, out BigInteger denominator)
    {
        foreach (Complex candidate in PolynomialRoots.Find(ToDoubles(coefficients)))
        {
            // The rational is verified against the integer polynomial, so the tolerance only chooses which roots to try.
            if (Math.Abs(candidate.Imaginary) <= 1e-6d * (1d + Math.Abs(candidate.Real))
                && PolynomialRoots.TryGetRational(candidate.Real, coefficients, out numerator, out denominator))
            {
                return true;
            }
        }

        numerator = BigInteger.Zero;
        denominator = BigInteger.One;
        return false;
    }

    /// <summary>The polynomial in <see cref="double"/>, divided by a power of two where its coefficients are too large for one.</summary>
    private static double[] ToDoubles(BigInteger[] coefficients)
    {
        long bits = 0;
        foreach (BigInteger coefficient in coefficients)
        {
            bits = Math.Max(bits, coefficient.GetBitLength());
        }

        // Dividing every coefficient by the same number leaves the roots where they are, and 900 bits is about 271
        // digits, far more than double keeps and far less than the exponent it overflows at.
        int shift = (int)Math.Max(0, bits - 900);
        return [.. coefficients.Select(coefficient => (double)(coefficient >> shift))];
    }

    /// <summary>A point inside stretch <paramref name="index"/> of the line the roots cut into.</summary>
    private static EvalResult Sample(List<Value> points, int index, EvaluationContext context)
    {
        if (points.Count == 0)
        {
            return Value.Zero;
        }

        if (index == 0)
        {
            return ValueMath.Subtract(points[0], Value.One, context);
        }

        if (index == points.Count)
        {
            return ValueMath.Add(points[^1], Value.One, context);
        }

        EvalResult sum = ValueMath.Add(points[index - 1], points[index], context);
        return sum.Succeeded ? ValueMath.Divide(sum.Value, Two, context) : sum;
    }

    /// <summary>The sign of the integer polynomial at a real value, exactly.</summary>
    private static int SignAt(BigInteger[] ascending, Value sample)
    {
        (BigInteger mantissa, int scale) = ScaledValues.Decompose(sample);
        return IntegerPolynomial.SignAt(
            ascending,
            mantissa * BigInteger.Pow(10, Math.Max(0, -scale)),
            BigInteger.Pow(10, Math.Max(0, scale)));
    }

    /// <summary>Merges the stretches that satisfy the inequality into the intervals the calculator displays; not every stretch satisfies it.</summary>
    private static ImmutableArray<SolutionInterval> Merge(bool[] satisfied, List<Value> points, Func<string, Value, Calculation> line)
    {
        ImmutableArray<SolutionInterval>.Builder intervals = ImmutableArray.CreateBuilder<SolutionInterval>();
        for (int piece = 0; piece < satisfied.Length; piece++)
        {
            if (!satisfied[piece])
            {
                continue;
            }

            int begin = piece;
            while (piece + 1 < satisfied.Length && satisfied[piece + 1])
            {
                piece++;
            }

            // An even piece is an open interval between roots, an odd one is a root; a run that starts or ends at a root
            // has that bound, and one that starts or ends at an interval has the root beside it, excluded.
            Calculation? lower = begin == 0 ? null : line("x", points[(begin - 1) / 2]);
            Calculation? upper = piece == satisfied.Length - 1 ? null : line("x", points[piece / 2]);
            intervals.Add(new SolutionInterval(lower, begin % 2 != 0, upper, piece % 2 != 0, Text(lower, begin % 2 != 0, upper, piece % 2 != 0)));
        }

        return intervals.ToImmutable();
    }

    /// <summary>One interval as the calculator writes it: <c>x≤-3</c>, <c>1≤x</c>, <c>-3&lt;x&lt;1</c> or <c>x=1</c>.</summary>
    private static string Text(Calculation? lower, bool lowerIncluded, Calculation? upper, bool upperIncluded)
    {
        if (lower is null)
        {
            return "x" + (upperIncluded ? "≤" : "<") + upper!.Display.Text;
        }

        if (upper is null)
        {
            return lower.Display.Text + (lowerIncluded ? "≤" : "<") + "x";
        }

        return lowerIncluded && upperIncluded && lower.Display.Text == upper.Display.Text
            ? "x=" + lower.Display.Text
            : lower.Display.Text + (lowerIncluded ? "≤" : "<") + "x" + (upperIncluded ? "≤" : "<") + upper.Display.Text;
    }

    /// <summary>The value of the polynomial at a point, by the scheme of Horner.</summary>
    private static EvalResult ValueAt(IReadOnlyList<Value> coefficients, Value x, EvaluationContext context)
    {
        EvalResult value = coefficients[0];
        for (int i = 1; i < coefficients.Count; i++)
        {
            EvalResult product = ValueMath.Multiply(value.Value, x, context);
            value = product.Succeeded ? ValueMath.Add(product.Value, coefficients[i], context) : product;
            if (!value.Succeeded)
            {
                return value;
            }
        }

        return value;
    }

    private static CalcError Failure(CalcErrorKind kind) => new(kind, default);

    /// <summary>A root as the two values the calculator displays: a real root has no imaginary part at all.</summary>
    private readonly record struct RootValue(Value Real, Value? Imaginary);
}
