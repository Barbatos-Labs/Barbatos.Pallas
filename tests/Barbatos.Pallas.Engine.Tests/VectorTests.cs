// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Vector application (manual pp. 139-145), with VctA = (1, 2), VctB = (3, 4) and VctC = (2, −1, 2) of p. 144.
/// </summary>
public sealed class VectorTests
{
    private static VectorValue Vector(params decimal[] elements) => new([.. elements.Select(Value.FromDecimal)]);

    private static CalculatorSession Session(CalculatorProfile profile = CalculatorProfile.Standard, Func<CalculatorSettings, CalculatorSettings>? settings = null)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Vector, profile, settings);
        session.SetVector(VectorVariable.VctA, Vector(1, 2));
        session.SetVector(VectorVariable.VctB, Vector(3, 4));
        session.SetVector(VectorVariable.VctC, Vector(2, -1, 2));
        session.SetVector(VectorVariable.VctD, Vector(1, 2, 3));
        return session;
    }

    [Theory]
    [InlineData("VctA+VctB", "[4, 6]")]
    [InlineData("VctB-VctA", "[2, 2]")]
    [InlineData("VctA×VctB", "[0, 0, -2]")]
    [InlineData("VctC×VctD", "[-7, -4, 5]")]
    [InlineData("3VctA", "[3, 6]")]
    [InlineData("VctA×3", "[3, 6]")]
    [InlineData("VctB÷2", "[1.5, 2]")]
    [InlineData("-VctC", "[-2, 1, -2]")]
    [InlineData("UnitV(VctB)", "[0.6, 0.8]")]
    public void VectorResults(string input, string expected)
    {
        Session().Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("VctA•VctB", "11")]
    [InlineData("Abs(VctC)", "3")]
    [InlineData("Abs(VctA)", "√(5)")]
    [InlineData("Abs(VctB)", "5")]
    [InlineData("VctC•VctD", "6")]
    public void NumberResults(string input, string expected)
    {
        Session().Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // Angle is in the current angle unit (p. 145: 10.305 in Fix 3, degrees).
    [InlineData(AngleUnit.Degree, "Angle(VctA,VctB)", "10.30484647")]
    [InlineData(AngleUnit.Degree, "Angle(VctA,2VctA)", "0")]
    [InlineData(AngleUnit.Degree, "Angle(VctA,-VctA)", "180")]
    [InlineData(AngleUnit.Degree, "Angle(VctC,VctC)", "0")]
    [InlineData(AngleUnit.Radian, "Angle(VctA,VctB)", "0.1798534998")]
    [InlineData(AngleUnit.Gradian, "Angle(VctB,VctB×VctA)", "100")]
    public void AnglesFollowTheAngleUnit(AngleUnit unit, string input, string expected)
    {
        CalculatorSession session = Session(settings: settings => settings with { AngleUnit = unit, InputOutput = InputOutput.MathIDecimalO });

        session.SetVector(VectorVariable.VctB, Vector(3, 4, 0));
        session.SetVector(VectorVariable.VctA, input.Contains("VctB×", StringComparison.Ordinal) ? Vector(1, 2, 0) : Vector(1, 2));
        if (!input.Contains("VctB×", StringComparison.Ordinal))
        {
            session.SetVector(VectorVariable.VctB, Vector(3, 4));
        }

        session.Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    // (√2, cos 40°) and twice it: their 15-digit elements give a cosine of 1.0000000000000022, which acos refuses.
    [InlineData("2", "0")]
    [InlineData("-2", "180")]
    public void ACosineRoundedPastOne_IsClamped(string factor, string expected)
    {
        CalculatorSession session = Session();
        session.SetVector(VectorVariable.VctA, new VectorValue(Calculator.Evaluate("√(2)"), Calculator.Evaluate("cos(40)")));
        session.SetVector(VectorVariable.VctB, new VectorValue(Calculator.Evaluate(factor + "×√(2)"), Calculator.Evaluate(factor + "×cos(40)")));

        session.Calculate("Angle(VctA,VctB)").Display.Text.Should().Be(expected);
    }

    [Theory]
    // A product beyond the range fails the whole result, whichever step computes it.
    [InlineData("Abs(VctA)")]
    [InlineData("VctA•VctA")]
    [InlineData("UnitV(VctA)")]
    [InlineData("Angle(VctA,VctB)")]
    [InlineData("VctC×VctD")]
    [InlineData("VctD×VctC")]
    [InlineData("VctA+VctA")]
    public void AnElementError_IsTheResult(string input)
    {
        CalculatorSession session = Session();
        Value large = Value.FromDouble(1e60);
        session.SetVector(VectorVariable.VctA, new VectorValue(input == "VctA+VctA" ? Value.FromDouble(9e99) : large, Value.Zero));
        session.SetVector(VectorVariable.VctB, Vector(0, 1));
        session.SetVector(VectorVariable.VctC, new VectorValue(Value.Zero, large, Value.One));
        session.SetVector(VectorVariable.VctD, new VectorValue(Value.Zero, Value.One, large));

        session.Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AnAngleOfApproximateParallelVectors_IsZero()
    {
        // (√2, √2) and (1, 1): the cosine may land a little above 1, and is taken as 1.
        CalculatorSession session = Session(settings: settings => settings with { InputOutput = InputOutput.MathIDecimalO });
        Value root = Calculator.Evaluate("√(2)");
        Value third = Calculator.Evaluate("sin(30)×2⌟3");
        session.SetVector(VectorVariable.VctA, new VectorValue(root, root));
        session.SetVector(VectorVariable.VctB, Vector(1, 1));
        session.SetVector(VectorVariable.VctC, new VectorValue(third, third, third));
        session.SetVector(VectorVariable.VctD, Vector(7, 7, 7));

        session.Calculate("Angle(VctA,VctB)").Display.Text.Should().Be("0");
        session.Calculate("Angle(VctC,VctD)").Display.Text.Should().Be("0");
    }

    [Theory]
    [InlineData("VctA+VctC")]
    [InlineData("VctA•VctC")]
    [InlineData("VctA×VctC")]
    [InlineData("Angle(VctA,VctC)")]
    public void DifferentDimensions_AreDimensionErrors(string input)
    {
        Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.DimensionError);
    }

    [Theory]
    [InlineData("VctA+1")]
    [InlineData("1÷VctA")]
    [InlineData("VctA÷VctB")]
    [InlineData("VctA²")]
    [InlineData("VctA⁻¹")]
    [InlineData("UnitV(2)")]
    [InlineData("Angle(VctA,2)")]
    [InlineData("2•VctA")]
    [InlineData("UnitV(VctA-VctA)")]
    [InlineData("Angle(VctA,VctA-VctA)")]
    public void OperationsAVectorDoesNotTake_AreMathErrors(string input)
    {
        Session().Calculate(input).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheCrossProductNeedsTwoOrThreeDimensions()
    {
        CalculatorSession session = Session(CalculatorProfile.Extended);
        session.SetVector(VectorVariable.VctA, Vector(1, 2, 3, 4));
        session.SetVector(VectorVariable.VctB, Vector(4, 3, 2, 1));

        session.Calculate("VctA×VctB").Error!.Value.Kind.Should().Be(CalcErrorKind.DimensionError);
        session.Calculate("VctA•VctB").Display.Text.Should().Be("20");
    }

    [Fact]
    public void VctAnsHoldsTheLastVectorAndAnsTheLastNumber()
    {
        CalculatorSession session = Session();

        session.Calculate("VctA+VctB");
        session.VctAns.Should().Be(Vector(4, 6));
        session.Ans.Should().Be(Value.Zero, "a vector does not replace Ans");
        session.Calculate("VctA•VctB");
        session.Ans.ToDecimal().Should().Be(11m);
        session.Calculate("VctAns-VctA").Display.Text.Should().Be("[3, 4]");

        session.SwitchApp(CalculatorApp.Vector);
        session.VctAns.Should().Be(Vector(3, 4), "staying in Vector keeps VctAns");
        session.SwitchApp(CalculatorApp.Matrix);
        session.VctAns.Should().BeNull("starting another application clears it (p. 144)");
        session.GetVector(VectorVariable.VctA).Should().Be(Vector(1, 2));
    }

    [Fact]
    public void AVectorVariableWithoutAVector_IsNotDefined()
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.Vector);

        session.Calculate("VctA").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
        session.Calculate("VctAns").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void AVectorIsAColumnInLatex()
    {
        Session().Calculate("VctA+VctB").Display.Latex.Should().Be(@"\begin{pmatrix}4\\6\end{pmatrix}");
    }

    [Fact]
    public void TheSessionChecksVectors()
    {
        CalculatorSession session = Session();
        Action four = () => session.SetVector(VectorVariable.VctA, Vector(1, 2, 3, 4));
        Action one = () => session.SetVector(VectorVariable.VctA, Vector(1));
        Action undefined = () => session.SetVector((VectorVariable)(-1), null);

        four.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vector");
        one.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("vector");
        undefined.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("variable");
        session.SetVector(VectorVariable.VctA, null);
        session.GetVector(VectorVariable.VctA).Should().BeNull();
        Calculator.Session(CalculatorApp.Vector, CalculatorProfile.Extended).Invoking(extended => extended.SetVector(VectorVariable.VctA, Vector(1, 2, 3, 4)))
            .Should().NotThrow();
    }

    [Fact]
    public void AVectorValueChecksItsElements()
    {
        Action empty = () => _ = new VectorValue();
        Action complex = () => _ = new VectorValue(Value.FromComplex(System.Numerics.Complex.ImaginaryOne));
        VectorValue vector = Vector(1, 2);
        Action outside = () => _ = vector[2];
        Action negative = () => _ = vector[-1];

        empty.Should().Throw<ArgumentException>().WithParameterName("elements");
        complex.Should().Throw<ArgumentException>().WithParameterName("elements");
        outside.Should().Throw<ArgumentOutOfRangeException>();
        negative.Should().Throw<ArgumentOutOfRangeException>();
        vector.Dimension.Should().Be(2);
        vector[1].Should().Be(Value.FromDecimal(2m));
        vector.ToString().Should().Be("[1, 2]");
        vector.Should().Be(Vector(1, 2));
        vector.GetHashCode().Should().Be(Vector(1, 2).GetHashCode());
        vector.GetHashCode().Should().NotBe(Vector(1, 3).GetHashCode());
        vector.Equals(Vector(1, 3)).Should().BeFalse();
        vector.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void AVectorIsAValue()
    {
        Value value = Value.FromVector(Vector(1, 2));
        Action none = () => Value.FromVector(null!);
        Action toMatrix = () => value.ToMatrix();

        value.Kind.Should().Be(ValueKind.Vector);
        value.ToVector().Should().Be(Vector(1, 2));
        value.Should().Be(Value.FromVector(Vector(1, 2)));
        value.ToString().Should().Be("[1, 2]");
        none.Should().Throw<ArgumentNullException>().WithParameterName("vector");
        toMatrix.Should().Throw<InvalidOperationException>();
    }
}
