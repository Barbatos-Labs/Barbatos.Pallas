// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions.Tests.Support;
using CsCheck;

namespace Barbatos.Pallas.Expressions.Tests;

/// <summary>
/// Arbitrary text, the way a user, a paste or a Web API caller can produce it. The parser must answer every input
/// with a tree or an error inside the text, never an exception or a hang, and a tree must survive printing.
/// </summary>
public sealed class FuzzTests
{
    // Characters that mean something to the lexer, plus surrogates, combining marks and characters that mean nothing.
    private static readonly char[] Alphabet =
    [
        .. "0123456789.()+-×÷*/^,:$ '\"%!=<>≠≤≥⌟√ˣ²³⁻¹°′″ʳᵍ∠•▶_@#",
        .. "ABCDEFPxyzeiπnabcrhkmsinlogtdQRSVMΣΠ∫",
        '\u0302', '\u0304', '\u2212', '\uD83D', '\uDE00', '\uD800', 'ŷ', 'x', 'q', '\t',
    ];

    public static TheoryData<SyntaxContext> Contexts => [.. SyntaxTrees.Contexts];

    [Theory]
    [MemberData(nameof(Contexts))]
    public void ArbitraryText_ParsesOrFailsWithinTheText(SyntaxContext context)
    {
        Gen.OneOfConst(Alphabet).Array[0, 60].Select(characters => new string(characters)).Sample(text =>
        {
            ParseResult result = ExpressionParser.Parse(text, context);
            if (!result.Succeeded)
            {
                SourceSpan span = result.Diagnostic!.Value.Span;
                return span.Start >= 0 && span.Length >= 0 && span.End <= result.Text.Length;
            }

            string printed = LinearPrinter.Print(result.Root, context);
            ParseResult reparsed = ExpressionParser.Parse(printed, context);
            return reparsed.Succeeded && SyntaxEquivalence.AreEquivalent(result.Root, reparsed.Root) && LatexPrinter.Print(result.Root).Length > 0;
        }, iter: 15_000, print: text => $"{context.App}: \"{text}\"");
    }

    [Fact]
    public void TokenSoup_FromTheVocabulary_ParsesOrFailsWithinTheText()
    {
        // Whole symbols rather than characters reach deeper into the grammar.
        string[] pieces = [.. SyntaxVocabulary.Standard.Symbols.Select(symbol => symbol.Text), "1", "23", "4.5", "0.(6)", " ", "(", ")"];
        SyntaxContext context = new(CalculatorApp.Calculate, AllowRelations: true);

        Gen.OneOfConst(pieces).Array[1, 25].Select(parts => string.Concat(parts)).Sample(text =>
        {
            ParseResult result = ExpressionParser.Parse(text, context);
            return result.Succeeded
                ? SyntaxEquivalence.AreEquivalent(result.Root, ExpressionParser.Parse(LinearPrinter.Print(result.Root, context), context).Root!)
                : result.Diagnostic!.Value.Span.End <= result.Text.Length;
        }, iter: 25_000, print: text => $"\"{text}\"");
    }
}
