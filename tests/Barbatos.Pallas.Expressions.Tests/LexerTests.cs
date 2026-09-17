// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class LexerTests
{
    [Theory]
    [InlineData(CalculatorApp.Calculate, "12.5+x", "Number:12.5 Symbol:+ Symbol:x")]
    [InlineData(CalculatorApp.Calculate, " 2 ×\t3 ", "Number:2 Symbol:× Number:3")]
    [InlineData(CalculatorApp.Calculate, "3.(021)", "Number:3.(021)")]
    [InlineData(CalculatorApp.Calculate, "2(3)", "Number:2 Symbol:( Number:3 Symbol:)")]
    [InlineData(CalculatorApp.Calculate, "3.(2+1)", "Number:3. Symbol:( Number:2 Symbol:+ Number:1 Symbol:)")]
    [InlineData(CalculatorApp.Calculate, "0.()", "Number:0. Symbol:( Symbol:)")]
    [InlineData(CalculatorApp.Calculate, ".5 3.", "Number:.5 Number:3.")]
    [InlineData(CalculatorApp.Calculate, ".", "Invalid:.")]
    [InlineData(CalculatorApp.Calculate, "÷R2÷2", "Symbol:÷R Number:2 Symbol:÷ Number:2")]
    [InlineData(CalculatorApp.Calculate, "sinh(sin(", "Symbol:sinh( Symbol:sin(")]
    [InlineData(CalculatorApp.Calculate, "d/dx(x", "Symbol:d/dx( Symbol:x")]
    [InlineData(CalculatorApp.Calculate, "5n mile▶m", "Number:5 Symbol:n mile▶m")]
    [InlineData(CalculatorApp.Calculate, "@R_K-90-@R_K", "Symbol:@R_K-90 Symbol:- Symbol:@R_K")]
    [InlineData(CalculatorApp.Calculate, "AnsA", "Symbol:Ans Symbol:A")]
    [InlineData(CalculatorApp.Calculate, "x̄", "Symbol:x Invalid:\u0304")]
    [InlineData(CalculatorApp.Statistics, "x̄x̂x", "Symbol:x̄ Symbol:x̂ Symbol:x")]
    [InlineData(CalculatorApp.BaseN, "1F+Ans", "Number:1F Symbol:+ Symbol:Ans")]
    [InlineData(CalculatorApp.BaseN, "ABC", "Number:ABC")]
    [InlineData(CalculatorApp.BaseN, "A", "Number:A")]
    [InlineData(CalculatorApp.BaseN, "b101 or d9", "BasePrefix:b Number:101 Symbol:or BasePrefix:d Number:9")]
    [InlineData(CalculatorApp.BaseN, "xor", "Symbol:xor")]
    [InlineData(CalculatorApp.BaseN, "1.5", "Number:1 Invalid:. Number:5")]
    [InlineData(CalculatorApp.Spreadsheet, "$A$1:B23", "CellReference:$A$1 Symbol:: CellReference:B23")]
    [InlineData(CalculatorApp.Spreadsheet, "A1234", "CellReference:A12 Number:34")]
    [InlineData(CalculatorApp.Spreadsheet, "A+Abs(F1", "Symbol:A Symbol:+ Symbol:Abs( Symbol:F Number:1")]
    [InlineData(CalculatorApp.Calculate, "😀", "Invalid:😀")]
    public void Tokens(CalculatorApp app, string text, string expected)
    {
        Describe(text, new SyntaxContext(app)).Should().Be(expected);
    }

    [Fact]
    public void Aliases_AreReturnedAsTheAliasSymbol_WithTheirCanonicalSymbol()
    {
        ExpressionLexer lexer = new("*", SyntaxContext.Calculate, SyntaxVocabulary.Standard);

        Token token = lexer.Next();

        token.Symbol!.Text.Should().Be("*");
        token.Symbol.Canonical.Text.Should().Be("×");
        token.Symbol.BinaryOperator.Should().Be(BinaryOperator.Multiply);
    }

    [Fact]
    public void End_IsReturnedRepeatedly_AtTheEndOfTheText()
    {
        ExpressionLexer lexer = new("7 ", SyntaxContext.Calculate, SyntaxVocabulary.Standard);

        lexer.Next().Kind.Should().Be(TokenKind.Number);
        lexer.Next().Should().Be(new Token(TokenKind.End, new SourceSpan(2, 0)));
        lexer.Next().Should().Be(new Token(TokenKind.End, new SourceSpan(2, 0)));
    }

    [Fact]
    public void Lexer_RequiresAVocabulary()
    {
        Action act = () => _ = new ExpressionLexer("1", SyntaxContext.Calculate, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Lexing_DoesNotAllocate()
    {
        // Allocation-free lexing is what makes re-reading an expression on every key press cheap (ARCHITECTURE.md §9).
        const string Text = "4×sin(30)×(30+10×3)-2⌟3+@N_A÷5cm▶in+Ans²+3.(021)";
        Count(Text);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int count = Count(Text);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        count.Should().BeGreaterThan(20);
        allocated.Should().Be(0);
    }

    private static int Count(string text)
    {
        ExpressionLexer lexer = new(text, SyntaxContext.Calculate, SyntaxVocabulary.Standard);
        int count = 0;
        while (lexer.Next().Kind != TokenKind.End)
        {
            count++;
        }

        return count;
    }

    private static string Describe(string text, SyntaxContext context)
    {
        ExpressionLexer lexer = new(text, context, SyntaxVocabulary.Standard);
        List<string> tokens = [];
        for (Token token = lexer.Next(); token.Kind != TokenKind.End; token = lexer.Next())
        {
            tokens.Add($"{token.Kind}:{text.Substring(token.Span.Start, token.Span.Length)}");
        }

        return string.Join(' ', tokens);
    }
}
