// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Splits Canonical Linear Syntax into tokens, without allocating.
/// </summary>
/// <remarks>
/// <para>At each position, after skipping white space, the lexer reads the first of:</para>
/// <list type="number">
/// <item><description>In Base-N, a run of digits and <c>A</c>-<c>F</c>, unless a longer vocabulary name starts there
/// (<c>Ans</c>); or a prefix <c>d h b o</c> directly followed by such a digit.</description></item>
/// <item><description>In Spreadsheet, a cell reference such as <c>$A$1</c>, unless a longer name starts there.</description></item>
/// <item><description>Elsewhere, a number: digits with an optional decimal point, and after the decimal point an optional
/// recurring part in parentheses, <c>3.(021)</c>. A recurring part needs the decimal point, so <c>2(3)</c> stays
/// a multiplication.</description></item>
/// <item><description>The longest vocabulary symbol available in the application.</description></item>
/// <item><description>Otherwise an <see cref="TokenKind.Invalid"/> token for one character (or surrogate pair).</description></item>
/// </list>
/// <para>The text should already be in Unicode normalization form C; <see cref="ExpressionParser"/> takes care of that.</para>
/// </remarks>
public ref struct ExpressionLexer
{
    private readonly ReadOnlySpan<char> _text;
    private readonly SyntaxContext _context;
    private readonly SyntaxVocabulary _vocabulary;
    private int _position;

    /// <summary>Initializes a lexer over <paramref name="text"/>.</summary>
    /// <param name="text">The text to split.</param>
    /// <param name="context">The context, which decides Base-N digits and Spreadsheet cell references.</param>
    /// <param name="vocabulary">The symbols to recognize.</param>
    public ExpressionLexer(ReadOnlySpan<char> text, SyntaxContext context, SyntaxVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        _text = text;
        _context = context;
        _vocabulary = vocabulary;
        _position = 0;
    }

    /// <summary>Reads the next token.</summary>
    /// <returns>The token; <see cref="TokenKind.End"/> once the text is exhausted.</returns>
    public Token Next()
    {
        while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
        {
            _position++;
        }

        if (_position >= _text.Length)
        {
            return new Token(TokenKind.End, new SourceSpan(_text.Length, 0));
        }

        int start = _position;
        ReadOnlySpan<char> rest = _text[start..];
        SyntaxSymbol? symbol = _vocabulary.LongestMatch(rest, _context.App);
        int symbolLength = symbol?.Text.Length ?? 0;

        if (_context.App == CalculatorApp.BaseN)
        {
            int digits = CountHexadecimalDigits(rest);
            if (digits > 0 && digits >= symbolLength)
            {
                return Take(TokenKind.Number, digits);
            }

            if (rest.Length > 1 && rest[0] is 'd' or 'h' or 'b' or 'o' && IsHexadecimalDigit(rest[1]) && symbolLength <= 1)
            {
                return Take(TokenKind.BasePrefix, 1);
            }
        }
        else if (_context.App == CalculatorApp.Spreadsheet)
        {
            int reference = MeasureCellReference(rest);
            if (reference > 0 && reference >= symbolLength)
            {
                return Take(TokenKind.CellReference, reference);
            }
        }

        if (_context.App != CalculatorApp.BaseN)
        {
            int number = MeasureNumber(rest);
            if (number > 0)
            {
                return Take(TokenKind.Number, number);
            }
        }

        if (symbol is not null)
        {
            return Take(TokenKind.Symbol, symbolLength, symbol);
        }

        return Take(TokenKind.Invalid, char.IsHighSurrogate(rest[0]) && rest.Length > 1 && char.IsLowSurrogate(rest[1]) ? 2 : 1);
    }

    private static bool IsHexadecimalDigit(char c) => char.IsAsciiDigit(c) || c is >= 'A' and <= 'F';

    private static int CountHexadecimalDigits(ReadOnlySpan<char> text)
    {
        int length = 0;
        while (length < text.Length && IsHexadecimalDigit(text[length]))
        {
            length++;
        }

        return length;
    }

    /// <summary>Measures <c>$?[A-E]$?[0-9]{1,2}</c>: columns A-E and rows 1-45 of the reference calculator's spreadsheet.</summary>
    /// <param name="text">The rest of the input, never empty.</param>
    private static int MeasureCellReference(ReadOnlySpan<char> text)
    {
        int index = text[0] == '$' ? 1 : 0;

        if (index >= text.Length || text[index] is < 'A' or > 'E')
        {
            return 0;
        }

        index++;
        if (index < text.Length && text[index] == '$')
        {
            index++;
        }

        int digits = 0;
        while (index < text.Length && char.IsAsciiDigit(text[index]) && digits < 2)
        {
            index++;
            digits++;
        }

        return digits == 0 ? 0 : index;
    }

    private static int MeasureNumber(ReadOnlySpan<char> text)
    {
        int index = 0;
        int digits = 0;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
            digits++;
        }

        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && char.IsAsciiDigit(text[index]))
            {
                index++;
                digits++;
            }

            int recurring = MeasureRecurringPart(text[index..]);
            index += recurring;
            digits += recurring;
        }

        return digits == 0 ? 0 : index;
    }

    /// <summary>Measures <c>([0-9]+)</c> at the start of <paramref name="text"/>, or returns 0.</summary>
    private static int MeasureRecurringPart(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty || text[0] != '(')
        {
            return 0;
        }

        int index = 1;
        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        return index > 1 && index < text.Length && text[index] == ')' ? index + 1 : 0;
    }

    private Token Take(TokenKind kind, int length, SyntaxSymbol? symbol = null)
    {
        Token token = new(kind, new SourceSpan(_position, length), symbol);
        _position += length;
        return token;
    }
}
