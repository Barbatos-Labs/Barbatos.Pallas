// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions.Tests.Support;
using CsCheck;

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class LatexPrinterTests
{
    [Theory]
    [InlineData("2⌟3+1⌟1⌟2", @"\frac{2}{3}+1\frac{1}{2}")]
    [InlineData("(1+2)⌟3", @"\frac{1+2}{3}")]
    [InlineData("√(2)×3", @"\sqrt{2}\times 3")]
    [InlineData("√((1+2))", @"\sqrt{1+2}")]
    [InlineData("5ˣ√(32)", @"\sqrt[5]{32}")]
    [InlineData("(1+1)^(2+2)", @"{\left(1+1\right)}^{2+2}")]
    [InlineData("-2²", @"-{2}^{2}")]
    [InlineData("(5²)³", @"{\left({5}^{2}\right)}^{3}")]
    [InlineData("10⁻¹+30°+(π÷2)ʳ+100ᵍ", @"{10}^{-1}+{30}^{\circ}+{\left(\pi \div 2\right)}^{\mathrm{r}}+{100}^{\mathrm{g}}")]
    [InlineData("5!+20%", @"5!+20\%")]
    [InlineData("Σ(x+1,1,5)", @"\sum_{x=1}^{5} \left(x+1\right)")]
    [InlineData("Π(x,1,5)", @"\prod_{x=1}^{5} x")]
    [InlineData("∫(ln(x),1,e)", @"\int_{1}^{e} \ln\left(x\right)\,dx")]
    [InlineData("d/dx(sin(x),π÷2)", @"\left.\frac{d}{dx}\left(\sin\left(x\right)\right)\right|_{x=\pi \div 2}")]
    [InlineData("log(2,16)+log(1000)", @"\log_{2}\left(16\right)+\log\left(1000\right)")]
    [InlineData("sin⁻¹(1)+tanh(0)", @"\sin^{-1}\left(1\right)+\tanh\left(0\right)")]
    [InlineData("Abs(2-7)", @"\left|2-7\right|")]
    [InlineData("GCD(28,35)", @"\mathrm{GCD}\left(28, 35\right)")]
    [InlineData("RanInt#(1,6)+Ran#", @"\text{RanInt\#}\left(1, 6\right)+\text{Ran\#}")]
    [InlineData("f(3)", @"f\left(3\right)")]
    [InlineData("10C4+10P4", @"{}_{10}\mathrm{C}_{4}+{}_{10}\mathrm{P}_{4}")]
    [InlineData("3.(021)", @"3.\overline{021}")]
    [InlineData("2°20′30″", @"2^{\circ}20'30''")]
    [InlineData("2π+2(3)+A×B", @"2 \pi +2 \left(3\right)+A\times B")]
    [InlineData("-(-2)", @"-\left(-2\right)")]
    [InlineData("5÷R2", @"5\div_{\mathrm{R}} 2")]
    [InlineData("@ε_0+@N_A+@atm+@R_K-90+@ħ+@R_∞", @"\varepsilon _{0}+N_{A}+\mathrm{atm}+R_{K-90}+\hbar +R_{\infty }")]
    [InlineData("5cm▶in+999_k+1_μ", @"5\,\mathrm{cm\blacktriangleright in}+999\,\mathrm{k}+1\,\mathrm{\mu }")]
    [InlineData("2J▶cal₁₅+3kgf·m▶J+5n mile▶m", @"2\,\mathrm{J\blacktriangleright cal_{15}}+3\,\mathrm{kgf\cdot m\blacktriangleright J}+5\,\mathrm{n\;mile\blacktriangleright m}")]
    [InlineData("Ans+PreAns", @"\mathrm{Ans}+\mathrm{PreAns}")]
    [InlineData("4≠3", @"4\neq 3")]
    [InlineData("1≤2<3", @"1\leq 2<3")]
    [InlineData("3≥2>1=1", @"3\geq 2>1=1")]
    public void Print_TypesetsLikeTheCalculator(string input, string expected)
    {
        LatexPrinter.Print(Parse(input, new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true))).Should().Be(expected);
    }

    [Theory]
    [InlineData(CalculatorApp.Complex, "2∠45+3i", @"2\angle 45+3 i")]
    [InlineData(CalculatorApp.BaseN, "1010 and 1100 or h1F xor b1 xnor d9-o7", @"1010\;\mathrm{and}\;1100\;\mathrm{or}\;\mathrm{h1F}\;\mathrm{xor}\;\mathrm{b1}\;\mathrm{xnor}\;\mathrm{d9}-\mathrm{o7}")]
    [InlineData(CalculatorApp.Vector, "VctA•VctB", @"\mathrm{VctA}\cdot \mathrm{VctB}")]
    [InlineData(CalculatorApp.Statistics, "5.5ŷ+2x̂+3x̂₁+4x̂₂+2▶t", @"5.5\,\hat{y}+2\,\hat{x}+3\,\hat{x}_{1}+4\,\hat{x}_{2}+2\blacktriangleright t")]
    [InlineData(CalculatorApp.Statistics, "x̄+ȳ+σ²x+Σx²+Q1", @"\bar{x}+\bar{y}+\sigma ^{2}x+\Sigma x^{2}+\mathrm{Q1}")]
    [InlineData(CalculatorApp.Spreadsheet, "Sum($A$1:B2)+C3", @"\mathrm{Sum}\left(\text{\$A\$1:B2}\right)+\mathrm{C3}")]
    public void Print_TypesetsApplicationSyntax(CalculatorApp app, string input, string expected)
    {
        LatexPrinter.Print(Parse(input, new SyntaxContext(app))).Should().Be(expected);
    }

    public static TheoryData<string, SyntaxNode> ConstructedTrees => new()
    {
        // A negative whole part keeps its parentheses before the fraction.
        { @"\left(-1\right)\frac{1}{2}", new MixedFractionExpression(new NegationExpression(new NumberLiteral("1")), new NumberLiteral("1"), new NumberLiteral("2")) },
        // A relation chain inside a function argument is parenthesized.
        { @"\mathrm{GCD}\left(\left(1<2\right)\right)", new FunctionCall(Symbol("GCD("), [new RelationChain([new NumberLiteral("1"), new NumberLiteral("2")], [RelationOperator.Less])]) },
        // A scientific constant whose name mixes ASCII and Greek letters is written letter by letter.
        { @"k\alpha _{0}", new NameReference(SyntaxSymbol.CreateName("@kα_0", SymbolKind.ScientificConstant)) },
    };

    [Theory]
    [MemberData(nameof(ConstructedTrees))]
    public void Print_ParenthesizesConstructedTrees(string expected, SyntaxNode tree)
    {
        LatexPrinter.Print(tree).Should().Be(expected);
    }

    [Theory]
    [InlineData("Σ(2x,1,5)", @"\sum_{x=1}^{5} \left(2 x\right)")]
    [InlineData("∫(2x,0,1)", @"\int_{0}^{1} \left(2 x\right)\,dx")]
    [InlineData("Σ(x²,1,5)", @"\sum_{x=1}^{5} {x}^{2}")]
    public void Print_ParenthesizesAProductInsideASumOrIntegral(string input, string expected)
    {
        LatexPrinter.Print(Parse(input, SyntaxContext.Calculate)).Should().Be(expected);
    }

    [Fact]
    public void Print_ImplicitMultiplicationOfNumbers_UsesADot()
    {
        SyntaxNode tree = new BinaryExpression(BinaryOperator.ImplicitMultiply, new NumberLiteral("2"),
            new PostfixExpression(PostfixOperator.Square, new NumberLiteral("3")));

        LatexPrinter.Print(tree).Should().Be(@"2\cdot {3}^{2}");
    }

    [Fact]
    public void Print_BalancesBracesForEveryGeneratedTree()
    {
        foreach (SyntaxContext context in SyntaxTrees.Contexts)
        {
            SyntaxTrees.For(context).Sample(tree =>
            {
                string latex = LatexPrinter.Print(tree);
                int depth = 0;
                foreach (char character in latex.Replace(@"\{", string.Empty, StringComparison.Ordinal).Replace(@"\}", string.Empty, StringComparison.Ordinal))
                {
                    depth += character switch { '{' => 1, '}' => -1, _ => 0 };
                    if (depth < 0)
                    {
                        return false;
                    }
                }

                return depth == 0
                    && latex.Split(@"\left").Length == latex.Split(@"\right").Length;
            }, iter: 1_000, print: tree => $"{context.App}: {LatexPrinter.Print(tree)}");
        }
    }

    [Fact]
    public void Print_RejectsNull()
    {
        Action act = () => LatexPrinter.Print(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    private static SyntaxSymbol Symbol(string text) => SyntaxVocabulary.Standard.Symbols.Single(symbol => symbol.Text == text);

    private static SyntaxNode Parse(string text, SyntaxContext context)
    {
        ParseResult result = ExpressionParser.Parse(text, context);
        result.Succeeded.Should().BeTrue("'{0}' should parse, but failed with {1}", text, result.Diagnostic);
        return result.Root!;
    }
}
