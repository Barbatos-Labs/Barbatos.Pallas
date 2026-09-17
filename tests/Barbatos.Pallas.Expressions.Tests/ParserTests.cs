// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions.Tests.Support;

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class ParserTests
{
    [Theory]
    // Manual p. 169: x² (level 3) binds tighter than the negative sign (level 5).
    [InlineData("-2²", "(neg (² 2))")]
    [InlineData("(-2)²", "(² [(neg 2)])")]
    // Manual p. 29: an omitted multiplication sign binds tighter than ÷.
    [InlineData("6÷2(1+2)", "(÷ 6 (· 2 [(+ 1 2)]))")]
    [InlineData("6÷2π", "(÷ 6 (· 2 π))")]
    // Fractions (level 4) bind tighter than implicit multiplication (level 7): 1⌟6π is (1/6)π, as p. 44 prints it.
    [InlineData("1⌟6π", "(· (⌟ 1 6) π)")]
    [InlineData("2⌟3+1⌟1⌟2", "(+ (⌟ 2 3) (mixed 1 1 2))")]
    [InlineData("2⌟3²", "(⌟ 2 (² 3))")]
    [InlineData("-2⌟3", "(neg (⌟ 2 3))")]
    // Level 3 is evaluated left to right (assumption U2): 2^3^2 is (2^3)^2 = 64.
    [InlineData("2^3^2", "(^ (^ 2 3) 2)")]
    [InlineData("2^3²", "(² (^ 2 3))")]
    [InlineData("2^-3", "(^ 2 (neg 3))")]
    [InlineData("-2π", "(· (neg 2) π)")]
    [InlineData("150×20%", "(× 150 (% 20))")]
    // Unit conversions (level 6) bind tighter than implicit multiplication and looser than the negative sign.
    [InlineData("5cm▶in", "(cm▶in 5)")]
    [InlineData("2×5cm▶in", "(× 2 (cm▶in 5))")]
    [InlineData("2πcm▶in", "(· 2 (cm▶in π))")]
    [InlineData("-5cm▶in", "(cm▶in (neg 5))")]
    // P and C (level 8) bind looser than implicit multiplication, tighter than ×.
    [InlineData("10C4", "(C 10 4)")]
    [InlineData("2×10P4", "(× 2 (P 10 4))")]
    [InlineData("2A P 3", "(P (· 2 A) 3)")]
    [InlineData("4×sin(30)×(30+10×3)", "(× (× 4 (sin( 30)) [(+ 30 (× 10 3))])")]
    [InlineData("7×8-4×5", "(- (× 7 8) (× 4 5))")]
    [InlineData("5÷R2", "(÷R 5 2)")]
    [InlineData("10⁻¹", "(⁻¹ 10)")]
    [InlineData("(5²)³", "(³ [(² 5)])")]
    [InlineData("1.23×10^3", "(× 1.23 (^ 10 3))")]
    [InlineData("3A+B", "(+ (· 3 A) B)")]
    [InlineData("2xy", "(· (· 2 x) y)")]
    [InlineData("(x+1)(x+5)", "(· [(+ x 1)] [(+ x 5)])")]
    [InlineData("10√(2)+15×3√(3)", "(+ (· 10 (√( 2)) (× 15 (· 3 (√( 3))))")]
    public void Precedence_FollowsTheCalculator(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    // Decision of 17 Sep 2026: C between two operands is the combination operator; elsewhere it is the variable C.
    [InlineData("2C", "(· 2 C)")]
    [InlineData("2C+1", "(+ (· 2 C) 1)")]
    [InlineData("ACB", "(C A B)")]
    [InlineData("C3", "(· C 3)")]
    [InlineData("2C-3", "(- (· 2 C) 3)")]
    [InlineData("2C(3)", "(C 2 [3])")]
    public void LetterC_IsTheCombinationOperatorOnlyBetweenTwoOperands(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("2°20′30″+0°9′30″", "(+ (dms 2 20 30) (dms 0 9 30))")]
    [InlineData("2°30″", "(dms 2 _ 30)")]
    [InlineData("2°20′", "(dms 2 20 _)")]
    [InlineData("sin(30°)", "(sin( (° 30))")]
    [InlineData("30°+20", "(+ (° 30) 20)")]
    [InlineData("2°3", "(· (° 2) 3)")]
    [InlineData("2°3°4′", "(· (° 2) (dms 3 4 _))")]
    [InlineData("(1+1)°20′", "(dms [(+ 1 1)] 20 _)")]
    [InlineData("(π÷2)ʳ+100ᵍ", "(+ (ʳ [(÷ π 2)]) (ᵍ 100))")]
    public void DegreeMark_IsSexagesimalOnlyWhenMinutesOrSecondsFollow(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("12", "12")]
    [InlineData("1.25", "1.25")]
    [InlineData(".5", ".5")]
    [InlineData("3.", "3.")]
    [InlineData("3.(021)+0.(312)", "(+ 3.(021) 0.(312))")]
    [InlineData("1.2(34)", "1.2(34)")]
    [InlineData("2(3)", "(· 2 [3])")]
    [InlineData("1.2 (3)", "(· 1.2 [3])")]
    [InlineData("1.5(2+1)", "(· 1.5 [(+ 2 1)])")]
    public void Numbers_AreKeptAsWritten_AndARecurringPartNeedsADecimalPoint(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("log(2,16)", "(log( 2 16)")]
    [InlineData("d/dx(sin(x),π÷2)", "(d/dx( (sin( x) (÷ π 2))")]
    [InlineData("Σ(x+1,1,5)", "(Σ( (+ x 1) 1 5)")]
    [InlineData("RanInt#(1,6)+Ran#", "(+ (RanInt#( 1 6) Ran#)")]
    [InlineData("5ˣ√(32)", "(root 5 [32])")]
    [InlineData("root(5,32)", "(root 5 32)")]
    [InlineData("999_k+25_k", "(+ (_k 999) (_k 25))")]
    [InlineData("@h×@N_A", "(× @h @N_A)")]
    [InlineData("Ans+PreAns", "(+ Ans PreAns)")]
    [InlineData("f(g(3))", "(f( (g( 3))")]
    public void FunctionsAndNames(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("sin(30", "(sin( 30)")]
    [InlineData("(1+2", "[(+ 1 2)]")]
    [InlineData("2×(3+sin(4", "(× 2 [(+ 3 (sin( 4))])")]
    public void ClosingParentheses_MayBeOmittedAtTheEnd(string input, string expected)
    {
        // Manual p. 28.
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("2*3/4", "(÷ (× 2 3) 4)")]
    [InlineData("2−3", "(- 2 3)")]
    [InlineData("sqrt(4)+pi", "(+ (√( 4) π)")]
    [InlineData("asin(1)+atanh(0)", "(+ (sin⁻¹( 1) (tanh⁻¹( 0))")]
    [InlineData("diff(x,1)+integral(x,0,1)+sum(x,1,2)+product(x,1,2)", "(+ (+ (+ (d/dx( x 1) (∫( x 0 1)) (Σ( x 1 2)) (Π( x 1 2))")]
    [InlineData("5cm->in", "(cm▶in 5)")]
    [InlineData("@hbar+@epsilon_0+@ε₀+@R∞", "(+ (+ (+ @ħ @ε_0) @ε_0) @R_∞)")]
    [InlineData("3_u+3_µ", "(+ (_μ 3) (_μ 3))")]
    [InlineData("2°20'30\"", "(dms 2 20 30)")]
    public void AsciiAliases_ParseToCanonicalSymbols(string input, string expected)
    {
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("1≤1<1+1", "(chain 1 ≤ 1 < (+ 1 1))")]
    [InlineData("3<π<4", "(chain 3 < π < 4)")]
    [InlineData("2²=2+2=4", "(chain (² 2) = (+ 2 2) = 4)")]
    [InlineData("2+3=5≠2+5=8", "(chain (+ 2 3) = 5 ≠ (+ 2 5) = 8)")]
    [InlineData("5≥4>3=3", "(chain 5 ≥ 4 > 3 = 3)")]
    [InlineData("1<=2", "(chain 1 ≤ 2)")]
    [InlineData("3!=6", "(chain (! 3) = 6)")]
    public void Relations_FormChains_WhenVerifyIsOn(string input, string expected)
    {
        // Manual p. 75. "3!=6" is a factorial: ≠ deliberately has no "!=" alias.
        TreeShape.Parse(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(CalculatorApp.Complex, "2∠45", "(∠ 2 45)")]
    [InlineData(CalculatorApp.Complex, "√(2)+√(2)i", "(+ (√( 2) (· (√( 2) i))")]
    [InlineData(CalculatorApp.Complex, "(1+i)^4+Conjg(2+3i)", "(+ (^ [(+ 1 i)] 4) (Conjg( (+ 2 (· 3 i))))")]
    [InlineData(CalculatorApp.BaseN, "1F+1", "(+ 1F 1)")]
    [InlineData(CalculatorApp.BaseN, "d10+h10+b10+o10", "(+ (+ (+ Dec:10 Hex:10) Bin:10) Oct:10)")]
    [InlineData(CalculatorApp.BaseN, "1010 and 1100 or 1", "(or (and 1010 1100) 1)")]
    [InlineData(CalculatorApp.BaseN, "Not(1010)+Neg(1)", "(+ (Not( 1010) (Neg( 1))")]
    [InlineData(CalculatorApp.BaseN, "ABC+Ans", "(+ ABC Ans)")]
    [InlineData(CalculatorApp.Matrix, "Identity(2)+MatA²", "(+ (Identity( 2) (² MatA))")]
    [InlineData(CalculatorApp.Vector, "VctA•VctB+1", "(+ (• VctA VctB) 1)")]
    [InlineData(CalculatorApp.Vector, "2VctA•VctB", "(• (· 2 VctA) VctB)")]
    [InlineData(CalculatorApp.Statistics, "5.5ŷ+2▶t", "(+ (ŷ 5.5) (▶t 2))")]
    [InlineData(CalculatorApp.Statistics, "3x̂₁-P(Ans)", "(- (x̂₁ 3) (P( Ans))")]
    [InlineData(CalculatorApp.Statistics, "x̄+σ²x+Σx²+min(x)", "(+ (+ (+ x̄ σ²x) Σx²) min(x))")]
    [InlineData(CalculatorApp.Statistics, "10P(4)", "(· 10 (P( 4))")]
    [InlineData(CalculatorApp.Statistics, "10P (4)", "(P 10 [4])")]
    [InlineData(CalculatorApp.Calculate, "10P(4)", "(P 10 [4])")]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:B3)+$C$2", "(+ (Sum( A1:B3) $C$2)")]
    [InlineData(CalculatorApp.Spreadsheet, "A1×A", "(× A1 A)")]
    public void ApplicationContext_DecidesWhatTokensMean(CalculatorApp app, string input, string expected)
    {
        TreeShape.Parse(input, new SyntaxContext(app)).Should().Be(expected);
    }

    [Fact]
    public void Input_IsNormalizedToFormC_AndSpansIndexTheNormalizedText()
    {
        // ȳ typed as y + U+0304 COMBINING MACRON.
        ParseResult result = ExpressionParser.Parse("2+y\u0304", new SyntaxContext(CalculatorApp.Statistics));

        result.Succeeded.Should().BeTrue();
        result.Text.Should().Be("2+ȳ");
        TreeShape.Of(result.Root!).Should().Be("(+ 2 ȳ)");
        result.Root!.Span.Should().Be(new SourceSpan(0, 3));
    }

    [Fact]
    public void Spans_CoverEachNode()
    {
        ParseResult result = ExpressionParser.Parse("sin(30)+(1", SyntaxContext.Calculate);

        BinaryExpression sum = (BinaryExpression)result.Root!;
        sum.Span.Should().Be(new SourceSpan(0, 10));
        sum.Left.Span.Should().Be(new SourceSpan(0, 7));
        ((FunctionCall)sum.Left).Arguments[0].Span.Should().Be(new SourceSpan(4, 2));
        sum.Right.Span.Should().Be(new SourceSpan(8, 2), "an omitted closing parenthesis ends the span at the end of the text");
        result.Diagnostic.Should().BeNull();
    }

    [Fact]
    public void Nesting_UpToTheLimit_Parses()
    {
        string text = new string('(', ExpressionParser.MaxNestingDepth - 1) + "1";

        ExpressionParser.Parse(text, SyntaxContext.Calculate).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Parse_UsesAPluginVocabulary()
    {
        SyntaxSymbol beam = SyntaxSymbol.CreateName("beam(", SymbolKind.Function);
        SyntaxVocabulary vocabulary = SyntaxVocabulary.Standard.With(beam, beam.CreateAlias("deflection("));

        ParseResult result = ExpressionParser.Parse("2deflection(3,4)", SyntaxContext.Calculate, vocabulary);

        TreeShape.Of(result.Root!).Should().Be("(· 2 (beam( 3 4))");
        LinearPrinter.Print(result.Root!, SyntaxContext.Calculate, vocabulary).Should().Be("2beam(3,4)");
    }

    [Fact]
    public void Parse_RejectsNullText()
    {
        Action act = () => ExpressionParser.Parse(null!, SyntaxContext.Calculate);
        act.Should().Throw<ArgumentNullException>();
    }
}
