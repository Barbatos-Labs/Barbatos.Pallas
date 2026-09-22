// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Matrix application (manual pp. 132-139), with the matrices printed on p. 137.
/// </summary>
public sealed class MatrixTests
{
    private static MatrixValue Matrix(params decimal[][] rows)
    {
        Value[,] entries = new Value[rows.Length, rows[0].Length];
        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[0].Length; column++)
            {
                entries[row, column] = Value.FromDecimal(rows[row][column]);
            }
        }

        return new MatrixValue(entries);
    }

    private static MatrixValue Doubles(double scale, params int[][] rows)
    {
        // Entries below 10⁻¹⁴ are held as double, so the matrix is eliminated in double.
        Value[,] entries = new Value[rows.Length, rows[0].Length];
        for (int row = 0; row < rows.Length; row++)
        {
            for (int column = 0; column < rows[0].Length; column++)
            {
                entries[row, column] = rows[row][column] == 0 ? Value.Zero : Value.FromDouble(rows[row][column] * scale);
            }
        }

        return new MatrixValue(entries);
    }

    private static CalculatorSession Session(CalculatorProfile profile = CalculatorProfile.Standard)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Matrix, profile);
        session.SetMatrix(MatrixVariable.MatA, Matrix([2, 1], [1, 1]));
        session.SetMatrix(MatrixVariable.MatB, Matrix([2, 3], [2, 1]));
        session.SetMatrix(MatrixVariable.MatC, Matrix([1, 0, -1], [0, -1, 1]));
        session.SetMatrix(MatrixVariable.MatD, Matrix([1, 2, 3], [4, 5, 6], [7, 8, 9]));
        return session;
    }

    [Theory]
    // pp. 137-139, with MatA-MatD of p. 137.
    [InlineData("MatA+MatB", "[[4, 4], [3, 2]]")]
    [InlineData("MatA-MatB", "[[0, -2], [-1, 0]]")]
    [InlineData("MatA²", "[[5, 3], [3, 2]]")]
    [InlineData("MatA³", "[[13, 8], [8, 5]]")]
    [InlineData("MatA⁻¹", "[[1, -1], [-1, 2]]")]
    [InlineData("Trn(MatC)", "[[1, 0], [0, -1], [-1, 1]]")]
    [InlineData("Identity(2)+MatA", "[[3, 1], [1, 2]]")]
    [InlineData("Abs(MatC)", "[[1, 0, 1], [0, 1, 1]]")]
    [InlineData("MatA×MatC", "[[2, -1, -1], [1, -1, 0]]")]
    [InlineData("2MatA", "[[4, 2], [2, 2]]")]
    [InlineData("MatA×3", "[[6, 3], [3, 3]]")]
    [InlineData("MatA÷2", "[[1, 0.5], [0.5, 0.5]]")]
    [InlineData("-MatA", "[[-2, -1], [-1, -1]]")]
    [InlineData("MatB⁻¹", "[[-0.25, 0.75], [0.5, -0.5]]")]
    public void TheExamplesOfTheManual(string input, string expected)
    {
        Session().Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("Det(MatA)", "1")]
    [InlineData("Det(MatB)", "-4")]
    [InlineData("Det(MatD)", "0")]
    [InlineData("Det(MatA)+Det(MatB)", "-3")]
    public void DeterminantsAreNumbers(string input, string expected)
    {
        Session().Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // Sizes that do not fit are a Dimension ERROR (p. 163).
    [InlineData("MatA+MatC")]
    [InlineData("MatC×MatA")]
    [InlineData("MatC²")]
    [InlineData("MatC³")]
    [InlineData("MatC⁻¹")]
    [InlineData("Det(MatC)")]
    public void IncompatibleSizes_AreDimensionErrors(string input)
    {
        Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.DimensionError);
    }

    [Theory]
    // Operands an operation does not take are a Math ERROR (assumption U20).
    [InlineData("MatA+1")]
    [InlineData("1-MatA")]
    [InlineData("1÷MatA")]
    [InlineData("MatA÷MatB")]
    [InlineData("MatD⁻¹")]
    [InlineData("MatA^2")]
    [InlineData("Det(2)")]
    [InlineData("Trn(2)")]
    [InlineData("2⁻¹×MatA⁻¹×MatD⁻¹")]
    public void OperationsAMatrixDoesNotTake_AreMathErrors(string input)
    {
        Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    [InlineData("Identity(0)")]
    [InlineData("Identity(5)")]
    [InlineData("Identity(1.5)")]
    public void IdentityTakesOneToFour(string input)
    {
        // p. 139: "from 1 to 4".
        Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.ArgumentError);
    }

    [Fact]
    public void TheStandardProfileHoldsFourByFour()
    {
        CalculatorSession session = Session();
        Action fiveRows = () => session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new Value[5, 4].Fill(Value.One)));
        Action fiveColumns = () => session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new Value[4, 5].Fill(Value.One)));

        session.Calculate("Identity(1)").Display.Text.Should().Be("[[1]]");
        session.Calculate("Det(Identity(4))").Display.Text.Should().Be("1");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new Value[4, 4].Fill(Value.One)));
        session.Calculate("Det(MatA)").Display.Text.Should().Be("0");
        fiveRows.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("matrix");
        fiveColumns.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("matrix");
    }

    [Theory]
    // An entry beyond the range fails the whole result, whichever operation computes it.
    [InlineData("MatA÷0")]
    [InlineData("MatB+MatB")]
    [InlineData("MatB×MatB")]
    [InlineData("MatB³")]
    public void AnEntryError_IsTheResult(string input)
    {
        CalculatorSession session = Session();
        Value large = Value.FromDouble(1e60);
        session.SetMatrix(MatrixVariable.MatB, new MatrixValue(new[,] { { input == "MatB+MatB" ? Value.FromDouble(9e99) : large } }));

        session.Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheExtendedProfileHoldsLargerMatrices()
    {
        CalculatorSession session = Session(CalculatorProfile.Extended);

        session.Calculate("Det(Identity(5))").Display.Text.Should().Be("1");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new Value[6, 6].Fill(Value.One)));
        session.Calculate("Det(MatA)").Display.Text.Should().Be("0");
        session.Calculate("Identity(65)").Error!.Value.Kind.Should().Be(CalcErrorKind.ArgumentError);
    }

    [Fact]
    public void AnInverseOfExactEntries_IsExact()
    {
        CalculatorSession session = Session();
        session.SetMatrix(MatrixVariable.MatA, Matrix([1, 2], [3, 4]));
        session.SetMatrix(MatrixVariable.MatB, Matrix([3, 0], [0, 3]));

        MatrixValue inverse = session.Calculate("MatA⁻¹").Result.ToMatrix();
        inverse[1, 0].ToDecimal().Should().Be(1.5m);
        inverse[1, 0].IsExact.Should().BeTrue();

        MatrixValue third = session.Calculate("MatB⁻¹").Result.ToMatrix();
        third[0, 0].ToDecimal().Should().Be(1m / 3m, "a decimal quotient, rounded once at the 28th digit");
        session.Calculate("MatB⁻¹").Display.Text.Should().Be("[[0.3333333333, 0], [0, 0.3333333333]]");
        session.Calculate("Det(MatB⁻¹)").Display.Text.Should().Be("1⌟9");
    }

    [Fact]
    public void ADecimalMatrixIsSingularExactly()
    {
        // Elimination in decimal would leave about 10⁻²⁸ and invert it; exact elimination leaves 0.
        CalculatorSession session = Session();
        session.SetMatrix(MatrixVariable.MatA, Matrix([0.1m, 0.2m], [0.3m, 0.6m]));

        session.Calculate("Det(MatA)").Result.Should().Be(Value.Zero);
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnApproximateMatrixIsSingularWithinTheTolerance()
    {
        // [[√2, 2], [1, √2]] is singular; its 15-digit entries give a determinant of about 3×10⁻¹⁵.
        CalculatorSession session = Session();
        Value root = Calculator.Evaluate("√(2)");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, Value.FromDecimal(2m) }, { Value.One, root } }));

        session.Calculate("Det(MatA)").Display.Text.Should().Be("0");
        session.Calculate("Det(MatA)").Result.IsExact.Should().BeFalse();
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnApproximateInvertibleMatrix_IsInverted()
    {
        CalculatorSession session = Session();
        Value root = Calculator.Evaluate("√(2)");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, Value.Zero }, { Value.Zero, root } }));

        session.Calculate("MatA⁻¹").Display.Text.Should().Be("[[0.7071067812, 0], [0, 0.7071067812]]");
        session.Calculate("Det(MatA)").Display.Text.Should().Be("2");
    }

    [Fact]
    public void EntriesBeyondDecimal_AreEliminatedInDouble()
    {
        CalculatorSession session = Session();
        Value large = Calculator.Evaluate("10^30");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { large, Value.Zero }, { Value.Zero, Value.FromDecimal(2m) } }));
        session.SetMatrix(MatrixVariable.MatB, new MatrixValue(new[,] { { large, large }, { large, large } }));

        session.Calculate("Det(MatA)").Display.Text.Should().Be("2×10^30");
        session.Calculate("MatA⁻¹").Display.Text.Should().Be("[[1×10^-30, 0], [0, 0.5]]");
        session.Calculate("Det(MatB)").Display.Text.Should().Be("0");
        session.Calculate("MatB⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnApproximateDeterminantOfExactlyZero_IsZero()
    {
        // Exact elimination of the 15-digit entries leaves exactly 0.
        CalculatorSession session = Session();
        Value root = Calculator.Evaluate("√(2)");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, root }, { root, root } }));

        session.Calculate("Det(MatA)").Result.ToDouble().Should().Be(0d);
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheToleranceScalesWithTheEntries()
    {
        // [[√2·10⁵, 2·10⁵], [10⁵, √2·10⁵]] is singular; rounding √2·10⁵ to 15 digits leaves a determinant of about
        // 1.4×10⁻⁴, far below 10⁻¹³ of its Hadamard bound of about 4.2×10¹⁰.
        CalculatorSession session = Session();
        Value root = Calculator.Evaluate("√(2)×10^5");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, Value.FromDecimal(200000m) }, { Value.FromDecimal(100000m), root } }));

        session.Calculate("Det(MatA)").Display.Text.Should().Be("0");
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void DoubleEliminationSwapsRows()
    {
        // [[0, t], [t, 0]] with t = 2⁻⁶⁰: the first pivot is 0 and a row swap changes the sign of the determinant.
        CalculatorSession session = Session();
        double t = Math.ScaleB(1d, -60);
        session.SetMatrix(MatrixVariable.MatA, Doubles(t, [0, 1], [1, 0]));

        session.Calculate("Det(MatA)").Result.ToDouble().Should().Be(-t * t);
        MatrixValue inverse = session.Calculate("MatA⁻¹").Result.ToMatrix();
        inverse[0, 0].ToDouble().Should().Be(0d);
        inverse[0, 1].ToDouble().Should().BeApproximately(1d / t, 1d / t * 1e-14);
        inverse[1, 0].ToDouble().Should().BeApproximately(1d / t, 1d / t * 1e-14);
    }

    [Fact]
    public void DoubleEliminationStopsAtAZeroColumn()
    {
        // The second column is twice the first; its pivot is exactly 0 after the first step, and dividing by it would
        // turn the determinant into NaN.
        CalculatorSession session = Session();
        session.SetMatrix(MatrixVariable.MatA, Doubles(Math.ScaleB(1d, -60), [1, 2, 3], [2, 4, 7], [4, 8, 10]));

        session.Calculate("Det(MatA)").Result.ToDouble().Should().Be(0d);
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void ADoubleMatrixCanBeSingularWithinTheTolerance()
    {
        // [[√2, 2], [1, √2]]·2⁻⁶⁰ in double: the determinant is a few units in the last place, not 0.
        CalculatorSession session = Session();
        double t = Math.ScaleB(1d, -60);
        Value root = Value.FromDouble(Math.Sqrt(2d) * t);
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, Value.FromDouble(2d * t) }, { Value.FromDouble(t), root } }));

        session.Calculate("Det(MatA)").Result.ToDouble().Should().Be(0d);
        session.Calculate("MatA⁻¹").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Theory]
    // An exact determinant keeps every digit a decimal holds, up to decimal.MaxValue.
    [InlineData("12345678901234567890123456789")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("0.00000000000001")]
    public void ExactDeterminantsKeepEveryDigit(string entry)
    {
        CalculatorSession session = Session();
        decimal value = decimal.Parse(entry, System.Globalization.CultureInfo.InvariantCulture);
        session.SetMatrix(MatrixVariable.MatA, Matrix([value]));

        Value determinant = session.Calculate("Det(MatA)").Result;

        determinant.Kind.Should().Be(ValueKind.DecimalReal);
        determinant.IsExact.Should().BeTrue();
        determinant.ToDecimal().Should().Be(value);
    }

    [Theory]
    // (1 + 10⁻¹⁴)(1 + 5·10⁻¹⁵) = 1.00000000000001500000000000005 exactly: the 29th decimal is a tie, rounded away from 0.
    [InlineData("1.00000000000001", "1.0000000000000150000000000001")]
    [InlineData("-1.00000000000001", "-1.0000000000000150000000000001")]
    public void AnExactDeterminantIsRoundedOnceHalfAwayFromZero(string first, string expected)
    {
        CalculatorSession session = Session();
        decimal entry = decimal.Parse(first, System.Globalization.CultureInfo.InvariantCulture);
        session.SetMatrix(MatrixVariable.MatA, Matrix([entry, 0], [0, 1.000000000000005m]));

        session.Calculate("Det(MatA)").Result.ToDecimal().Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    // An exact determinant is rounded once, into decimal or, beyond it, double (the precision rule).
    [InlineData("100000000000000000000", "1×10^40")]
    [InlineData("0.0000001", "1×10^-14")]
    [InlineData("0.00000001", "1×10^-16")]
    public void ExactDeterminantsOfEveryMagnitude(string diagonal, string expected)
    {
        CalculatorSession session = Session();
        decimal entry = decimal.Parse(diagonal, System.Globalization.CultureInfo.InvariantCulture);
        session.SetMatrix(MatrixVariable.MatA, Matrix([entry, 0], [0, entry]));

        Value determinant = session.Calculate("Det(MatA)").Result;
        determinant.Kind.Should().Be(expected == "1×10^-14" ? ValueKind.DecimalReal : ValueKind.DoubleReal);
        session.Calculate("Det(MatA)").Display.Text.Should().Be(expected);
    }

    [Fact]
    public void ExactFormsSurviveInsideAMatrix()
    {
        CalculatorSession session = Session();
        Value root = Calculator.Evaluate("√(2)");
        session.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { root, Value.Zero }, { Value.Zero, root } }));

        MatrixValue square = session.Calculate("MatA²").Result.ToMatrix();

        square[0, 0].ToDecimal().Should().Be(2m);
        square[0, 0].IsExact.Should().BeTrue("√2 × √2 is exactly 2 through the form");
    }

    [Fact]
    public void AMatrixVariableWithoutAMatrix_IsNotDefined()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Matrix);

        session.Calculate("MatA").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
        session.Calculate("MatAns").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void MatAnsHoldsTheLastMatrixAndAnsTheLastNumber()
    {
        CalculatorSession session = Session();

        session.Calculate("MatA×MatB");
        session.MatAns.Should().Be(Matrix([6, 7], [4, 4]));
        session.Ans.Should().Be(Value.Zero, "a matrix does not replace Ans");
        session.Calculate("Det(MatA)");
        session.Ans.ToDecimal().Should().Be(1m);
        session.MatAns.Should().Be(Matrix([6, 7], [4, 4]), "a number does not replace MatAns");
        session.Calculate("MatAns-MatA").Display.Text.Should().Be("[[4, 6], [3, 3]]");
        session.Ans.ToDecimal().Should().Be(1m);
    }

    [Fact]
    public void LeavingMatrixClearsMatAnsButNotTheVariables()
    {
        // pp. 135, 137: the variables persist; MatAns is cleared when another application starts.
        CalculatorSession session = Session();
        session.Calculate("MatA+MatB");

        session.SwitchApp(CalculatorApp.Calculate);
        session.SwitchApp(CalculatorApp.Matrix);

        session.MatAns.Should().BeNull();
        session.GetMatrix(MatrixVariable.MatB).Should().Be(Matrix([2, 3], [2, 1]));
    }

    [Fact]
    public void MatricesAreNamesOfTheMatrixApplicationOnly()
    {
        Calculator.Error("MatA", CalculatorApp.Calculate).Kind.Should().Be(CalcErrorKind.SyntaxError);
        Calculator.Error("Det(2)", CalculatorApp.Calculate).Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void AMatrixIsDisplayedWithTheNumberFormatAndDecimalMark()
    {
        CalculatorSession session = Session();
        session.Settings = session.Settings with { NumberFormat = NumberFormat.Fix(2) };
        session.Calculate("MatA÷2").Display.Text.Should().Be("[[1.00, 0.50], [0.50, 0.50]]");

        session.Settings = session.Settings with { NumberFormat = NumberFormat.Norm1, DecimalMark = DecimalMark.Comma };
        session.Calculate("MatA÷2").Display.Text.Should().Be("[[1; 0,5]; [0,5; 0,5]]");
    }

    [Fact]
    public void AMatrixIsWrittenAsAPmatrixInLatex()
    {
        Session().Calculate("MatA⁻¹").Display.Latex.Should().Be(@"\begin{pmatrix}1&-1\\-1&2\end{pmatrix}");
    }

    [Fact]
    public void FormatConversionsOfAMatrix()
    {
        CalculatorSession session = Session();
        Calculation calculation = session.Calculate("MatA÷2");

        session.Format(calculation, FormatTarget.Standard)!.Text.Should().Be("[[1, 0.5], [0.5, 0.5]]");
        session.Format(calculation, FormatTarget.DecimalValue)!.Text.Should().Be("[[1, 0.5], [0.5, 0.5]]");
        session.Format(calculation, FormatTarget.ImproperFraction).Should().BeNull();
        session.Format(calculation, FormatTarget.Engineering).Should().BeNull();
    }

    [Fact]
    public void TheSessionChecksMatrices()
    {
        CalculatorSession session = Session();
        MatrixValue five = new(new Value[5, 5].Fill(Value.One));

        Action tooLarge = () => session.SetMatrix(MatrixVariable.MatA, five);
        Action undefined = () => session.SetMatrix((MatrixVariable)4, null);
        Action composite = () => session.SetVariable(MemoryVariable.A, Value.FromMatrix(five));

        tooLarge.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("matrix");
        undefined.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("variable");
        composite.Should().Throw<ArgumentException>().WithParameterName("value");
        session.SetMatrix(MatrixVariable.MatA, null);
        session.GetMatrix(MatrixVariable.MatA).Should().BeNull();
    }

    [Fact]
    public void AMatrixValueChecksItsEntries()
    {
        Action none = () => _ = new MatrixValue(null!);
        Action empty = () => _ = new MatrixValue(new Value[0, 2]);
        Action complex = () => _ = new MatrixValue(new[,] { { Value.FromComplex(System.Numerics.Complex.ImaginaryOne) } });
        MatrixValue matrix = Matrix([1, 2], [3, 4]);
        Action rowBelow = () => _ = matrix[-1, 0];
        Action rowAbove = () => _ = matrix[2, 0];
        Action columnBelow = () => _ = matrix[1, -1];
        Action columnAbove = () => _ = matrix[0, 2];

        none.Should().Throw<ArgumentNullException>().WithParameterName("entries");
        empty.Should().Throw<ArgumentException>().WithParameterName("entries");
        complex.Should().Throw<ArgumentException>().WithParameterName("entries");
        rowBelow.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("row");
        rowAbove.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("row");
        columnBelow.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
        columnAbove.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("column");
        matrix.IsSquare.Should().BeTrue();
        matrix.ToArray()[1, 0].Should().Be(Value.FromDecimal(3m));
        matrix.ToString().Should().Be("[[1, 2], [3, 4]]");
        matrix.Should().Be(Matrix([1, 2], [3, 4]));
        matrix.GetHashCode().Should().Be(Matrix([1, 2], [3, 4]).GetHashCode());
        matrix.GetHashCode().Should().NotBe(Matrix([1, 2], [3, 5]).GetHashCode());
        Matrix([1, 2]).GetHashCode().Should().NotBe(Matrix([1], [2]).GetHashCode());
        matrix.Equals(Matrix([1, 2])).Should().BeFalse();
        matrix.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void AMatrixIsAValue()
    {
        MatrixValue matrix = Matrix([1, 2], [3, 4]);
        Value value = Value.FromMatrix(matrix);
        Action none = () => Value.FromMatrix(null!);
        Action toDouble = () => value.ToDouble();
        Action toVector = () => value.ToVector();
        Action toMatrix = () => Value.One.ToMatrix();

        value.Kind.Should().Be(ValueKind.Matrix);
        value.IsReal.Should().BeFalse();
        value.ToMatrix().Should().BeSameAs(matrix);
        value.Should().Be(Value.FromMatrix(Matrix([1, 2], [3, 4])));
        value.Should().NotBe(Value.FromMatrix(Matrix([1, 2], [3, 5])));
        value.ToString().Should().Be("[[1, 2], [3, 4]]");
        none.Should().Throw<ArgumentNullException>().WithParameterName("matrix");
        toDouble.Should().Throw<InvalidOperationException>();
        toVector.Should().Throw<InvalidOperationException>();
        toMatrix.Should().Throw<InvalidOperationException>();
    }
}

internal static class ArrayFill
{
    public static Value[,] Fill(this Value[,] array, Value value)
    {
        for (int row = 0; row < array.GetLength(0); row++)
        {
            for (int column = 0; column < array.GetLength(1); column++)
            {
                array[row, column] = value;
            }
        }

        return array;
    }
}
