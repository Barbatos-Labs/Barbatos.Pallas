// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// What the Spreadsheet application reads out of the text of a cell: its references, its size in the calculator's
/// bytes, and how many digits a number was typed with (manual pp. 101, 103).
/// </summary>
/// <remarks>
/// The references are found with <see cref="ExpressionLexer"/> rather than by walking a syntax tree: a cell reference is
/// a token of its own in the Spreadsheet context, so nothing can hide one, and the text keeps everything else exactly as
/// it was entered.
/// </remarks>
internal static class CellFormula
{
    /// <summary>The bytes a formula costs beyond what is typed (p. 101).</summary>
    public const int FormulaBytes = 15;

    /// <summary>The bytes a constant costs, however many digits it has (p. 101).</summary>
    public const int ConstantBytes = 14;

    /// <summary>Moves every relative reference by an offset, as a paste does; a reference off the grid becomes <c>?</c>.</summary>
    public static string Shift(string input, int columns, int rows, int gridColumns, int gridRows, SyntaxVocabulary vocabulary)
    {
        StringBuilder text = new(input.Length);
        int copied = 0;
        foreach ((SourceSpan span, string reference) in References(input, vocabulary))
        {
            text.Append(input, copied, span.Start - copied).Append(Moved(reference, columns, rows, gridColumns, gridRows));
            copied = span.Start + span.Length;
        }

        return text.Append(input, copied, input.Length - copied).ToString();
    }

    /// <summary>
    /// The bytes the calculator counts for an input: one per character of a number, a variable or a symbol, and one for
    /// a whole command or function, so that <c>√(</c> and <c>Sum(</c> are one byte each (p. 101).
    /// </summary>
    public static int Bytes(string input, SyntaxVocabulary vocabulary)
    {
        int bytes = 0;
        ExpressionLexer lexer = new(input, Context, vocabulary);
        for (Token token = lexer.Next(); token.Kind != TokenKind.End; token = lexer.Next())
        {
            bytes += token.Kind == TokenKind.Symbol && token.Symbol!.Kind is SymbolKind.Function or SymbolKind.Constant
                ? 1
                : token.Span.Length;
        }

        return bytes;
    }

    /// <summary>The significant digits of an input that is one number, or 0 for anything else (p. 101).</summary>
    public static int SignificantDigits(string input)
    {
        string digits = input.Trim();
        if (!digits.All(character => char.IsAsciiDigit(character) || character == '.'))
        {
            return 0;
        }

        // Leading zeros are not significant; trailing zeros of a whole number are, as the calculator counts 12345678915.
        return digits.TrimStart('0', '.').Count(char.IsAsciiDigit);
    }

    private static SyntaxContext Context => new(CalculatorApp.Spreadsheet, AllowRelations: false);

    private static List<(SourceSpan Span, string Reference)> References(string input, SyntaxVocabulary vocabulary)
    {
        List<(SourceSpan Span, string Reference)> found = [];
        ExpressionLexer lexer = new(input, Context, vocabulary);
        for (Token token = lexer.Next(); token.Kind != TokenKind.End; token = lexer.Next())
        {
            if (token.Kind == TokenKind.CellReference)
            {
                found.Add((token.Span, input.Substring(token.Span.Start, token.Span.Length)));
            }
        }

        return found;
    }

    /// <summary>One reference moved: a part written with <c>$</c> stays where it is (p. 103).</summary>
    private static string Moved(string reference, int columns, int rows, int gridColumns, int gridRows)
    {
        bool absoluteColumn = reference.StartsWith('$');
        string rest = absoluteColumn ? reference[1..] : reference;
        char column = rest[0];
        rest = rest[1..];
        bool absoluteRow = rest.StartsWith('$');
        int row = int.Parse(absoluteRow ? rest[1..] : rest, NumberStyles.None, CultureInfo.InvariantCulture);

        CellAddress moved = new(column - 'A' + (absoluteColumn ? 0 : columns), row - 1 + (absoluteRow ? 0 : rows));
        string letter = moved.Column >= 0 && moved.Column < gridColumns ? ((char)('A' + moved.Column)).ToString() : "?";
        string number = moved.Row >= 0 && moved.Row < gridRows ? (moved.Row + 1).ToString(CultureInfo.InvariantCulture) : "?";
        return (absoluteColumn ? "$" : string.Empty) + letter + (absoluteRow ? "$" : string.Empty) + number;
    }
}
