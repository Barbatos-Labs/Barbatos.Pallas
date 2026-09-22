// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The Ratio application (manual pp. 145-146): A:B = X:D and A:B = C:X.
/// </summary>
public sealed class RatioTests
{
    private static CalculatorSession Session() => Calculator.Session(CalculatorApp.Ratio);

    private static Value Number(decimal value) => Value.FromDecimal(value);

    [Fact]
    public void TheExampleOfTheManual()
    {
        // p. 145: 3:8 = X:12 gives X = 9/2, and the result goes to Ans.
        CalculatorSession session = Session();

        Calculation calculation = session.SolveRatio(RatioForm.XInSecondRatio, Number(3), Number(8), Number(12));

        calculation.Display.Text.Should().Be("9⌟2");
        calculation.Input.Should().Be("A:B=X:D");
        calculation.Result.IsExact.Should().BeTrue();
        session.Ans.Should().Be(calculation.Result);
    }

    [Fact]
    public void XLastInTheSecondRatio()
    {
        // 3:8 = 12:X gives X = 32, and the input names the form.
        Calculation calculation = Session().SolveRatio(RatioForm.XLastInSecondRatio, Number(3), Number(8), Number(12));

        calculation.Display.Text.Should().Be("32");
        calculation.Input.Should().Be("A:B=C:X");
    }

    [Fact]
    public void AZeroAnywhere_IsAMathError()
    {
        // p. 146: a Math ERROR happens when 0 is entered for a coefficient, not only where it would divide.
        Session().SolveRatio(RatioForm.XInSecondRatio, Number(0), Number(2), Number(10)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().SolveRatio(RatioForm.XInSecondRatio, Number(1), Number(0), Number(10)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().SolveRatio(RatioForm.XInSecondRatio, Number(1), Number(2), Number(0)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        Session().SolveRatio(RatioForm.XLastInSecondRatio, Number(0), Number(2), Number(10)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AValueThatIsNotReal_IsAMathError()
    {
        Value complex = Calculator.Evaluate("2+3i", CalculatorApp.Complex);

        Session().SolveRatio(RatioForm.XInSecondRatio, complex, Number(2), Number(10)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void AResultOutsideTheRange_IsAMathError()
    {
        // 10⁹⁹ × 10 leaves the range of the Standard profile (p. 169).
        Value large = Value.FromDouble(1e99);

        Session().SolveRatio(RatioForm.XInSecondRatio, large, Number(1), Number(10)).Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void TheRatioApplicationRefusesWhatItDoesNotSolve()
    {
        CalculatorSession ratio = Session();
        CalculatorSession calculate = Calculator.Session();

        ratio.Invoking(session => session.SolveRatio((RatioForm)7, Number(1), Number(2), Number(3))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("form");
        calculate.Invoking(session => session.SolveRatio(RatioForm.XInSecondRatio, Number(1), Number(2), Number(3))).Should().Throw<InvalidOperationException>();
    }
}
