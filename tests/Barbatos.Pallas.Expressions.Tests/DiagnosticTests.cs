// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions.Tests;

public sealed class DiagnosticTests
{
    [Theory]
    [InlineData("", SyntaxErrorCode.EmptyExpression, 0, 0)]
    [InlineData("   ", SyntaxErrorCode.EmptyExpression, 3, 0)]
    [InlineData("2+", SyntaxErrorCode.MissingOperand, 2, 0)]
    [InlineData("×3", SyntaxErrorCode.MissingOperand, 0, 1)]
    [InlineData(")", SyntaxErrorCode.MissingOperand, 0, 1)]
    [InlineData("sin()", SyntaxErrorCode.MissingOperand, 4, 1)]
    [InlineData("log(2,,3)", SyntaxErrorCode.MissingOperand, 6, 1)]
    [InlineData(",3", SyntaxErrorCode.MissingOperand, 0, 1)]
    [InlineData("P3", SyntaxErrorCode.MissingOperand, 0, 1)]
    [InlineData("2,3", SyntaxErrorCode.UnexpectedToken, 1, 1)]
    [InlineData("sin(1,2(", SyntaxErrorCode.MissingOperand, 8, 0)]
    [InlineData("2)", SyntaxErrorCode.UnmatchedClosingParenthesis, 1, 1)]
    [InlineData("(1))", SyntaxErrorCode.UnmatchedClosingParenthesis, 3, 1)]
    [InlineData("2 3", SyntaxErrorCode.AdjacentNumbers, 2, 1)]
    [InlineData("1.2.3", SyntaxErrorCode.AdjacentNumbers, 3, 2)]
    [InlineData("2q", SyntaxErrorCode.UnexpectedCharacter, 1, 1)]
    [InlineData("sin", SyntaxErrorCode.UnexpectedCharacter, 0, 1)]
    [InlineData("(2#", SyntaxErrorCode.UnexpectedCharacter, 2, 1)]
    [InlineData("\uD800", SyntaxErrorCode.UnexpectedCharacter, 0, 1)]
    [InlineData("2😀", SyntaxErrorCode.UnexpectedCharacter, 1, 2)]
    [InlineData("1=1", SyntaxErrorCode.RelationNotAllowed, 1, 1)]
    [InlineData("1⌟2⌟3⌟4", SyntaxErrorCode.TooManyFractionParts, 5, 1)]
    [InlineData("20′", SyntaxErrorCode.MisplacedSexagesimalMark, 2, 1)]
    [InlineData("2°(3)′", SyntaxErrorCode.MisplacedSexagesimalMark, 5, 1)]
    [InlineData("root(2)", SyntaxErrorCode.InvalidRootArguments, 0, 7)]
    [InlineData("i", SyntaxErrorCode.UnexpectedCharacter, 0, 1)]
    public void Errors_AreReportedWithTheirPosition(string text, SyntaxErrorCode code, int start, int length)
    {
        ParseResult result = ExpressionParser.Parse(text, SyntaxContext.Calculate);

        result.Succeeded.Should().BeFalse();
        result.Root.Should().BeNull();
        result.Diagnostic.Should().Be(new SyntaxDiagnostic(code, new SourceSpan(start, length)));
    }

    [Theory]
    // Manual p. 75: relations must point one way, and ≠ does not combine with an inequality (assumption U10).
    [InlineData("5≤6≥4", SyntaxErrorCode.MixedRelationDirections, 3)]
    [InlineData("4>6<8", SyntaxErrorCode.MixedRelationDirections, 3)]
    [InlineData("4<6≠8", SyntaxErrorCode.NotEqualWithInequality, 3)]
    [InlineData("4≠6<8", SyntaxErrorCode.NotEqualWithInequality, 3)]
    [InlineData("4≠6>8", SyntaxErrorCode.NotEqualWithInequality, 3)]
    [InlineData("4>6≠8", SyntaxErrorCode.NotEqualWithInequality, 3)]
    [InlineData("(1<2)", SyntaxErrorCode.RelationNotAllowed, 2)]
    [InlineData("1<", SyntaxErrorCode.MissingOperand, 2)]
    public void VerifyChains_FollowTheManualsRules(string text, SyntaxErrorCode code, int start)
    {
        ParseResult result = ExpressionParser.Parse(text, new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true));

        result.Diagnostic!.Value.Code.Should().Be(code);
        result.Diagnostic!.Value.Span.Start.Should().Be(start);
    }

    [Theory]
    [InlineData(CalculatorApp.BaseN, "bAns", SyntaxErrorCode.MissingOperand, 1, 3)]
    [InlineData(CalculatorApp.BaseN, "1.5", SyntaxErrorCode.UnexpectedCharacter, 1, 1)]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(A1:)", SyntaxErrorCode.MissingOperand, 7, 1)]
    [InlineData(CalculatorApp.Spreadsheet, "A1:", SyntaxErrorCode.MissingOperand, 3, 0)]
    [InlineData(CalculatorApp.Calculate, "A1:B2", SyntaxErrorCode.UnexpectedCharacter, 2, 1)]
    public void ContextDependentErrors(CalculatorApp app, string text, SyntaxErrorCode code, int start, int length)
    {
        ExpressionParser.Parse(text, new SyntaxContext(app)).Diagnostic.Should().Be(new SyntaxDiagnostic(code, new SourceSpan(start, length)));
    }

    [Fact]
    public void NestingBeyondTheLimit_IsAStackError()
    {
        string text = new string('(', ExpressionParser.MaxNestingDepth + 10) + "1";

        SyntaxDiagnostic diagnostic = ExpressionParser.Parse(text, SyntaxContext.Calculate).Diagnostic!.Value;

        diagnostic.Code.Should().Be(SyntaxErrorCode.NestingTooDeep);
        diagnostic.Span.Start.Should().BeLessThan(text.Length);
    }

    [Fact]
    public void LongChainsOfSigns_AreBoundedToo()
    {
        string text = new string('-', 10_000) + "1";

        ExpressionParser.Parse(text, SyntaxContext.Calculate).Diagnostic!.Value.Code.Should().Be(SyntaxErrorCode.NestingTooDeep);
    }

    [Fact]
    public void LongFlatExpressions_AreNotNesting()
    {
        string text = string.Join("+", Enumerable.Repeat("1", 10_000));

        ExpressionParser.Parse(text, SyntaxContext.Calculate).Succeeded.Should().BeTrue();
    }
}
