// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Reads Canonical Linear Syntax into a syntax tree, following the calculation priority of the reference calculator (manual p. 168).
/// </summary>
/// <remarks>
/// <para>
/// A hand-written Pratt parser. .NET has no expression parser (<c>System.Linq.Expressions</c> builds trees but does not
/// read text; <c>DataTable.Compute</c> reads SQL-like text into <c>double</c>), and the calculator's grammar is
/// context-dependent in ways parser generators handle poorly: multiplication with the sign omitted binds tighter than
/// <c>÷</c>, a closing parenthesis may be omitted at the end, and Base-N reads <c>b10</c> as binary.
/// </para>
/// <para>
/// Parsing never throws for bad input: it stops at the first error and reports it with its span, as the calculator
/// places the cursor at the error (p. 162).
/// </para>
/// </remarks>
public static class ExpressionParser
{
    /// <summary>
    /// The deepest nesting of parentheses, functions and signs accepted; deeper input is a Stack ERROR.
    /// </summary>
    /// <remarks>
    /// The manual gives no stack size (assumption U11 in docs/CONFORMANCE.md). A fixed limit makes the outcome identical on
    /// every platform, where running out of thread stack would not be.
    /// </remarks>
    public const int MaxNestingDepth = 128;

    /// <summary>Parses an expression.</summary>
    /// <param name="text">The expression in Canonical Linear Syntax. It is normalized to Unicode form C first.</param>
    /// <param name="context">The application and whether relations are allowed.</param>
    /// <param name="vocabulary">The symbols to recognize; <see cref="SyntaxVocabulary.Standard"/> when omitted.</param>
    /// <returns>The tree, or the first error.</returns>
    public static ParseResult Parse(string text, SyntaxContext context, SyntaxVocabulary? vocabulary = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        return new Parser(Normalize(text), context, vocabulary ?? SyntaxVocabulary.Standard).Parse();
    }

    private static string Normalize(string text)
    {
        try
        {
            return text.IsNormalized(NormalizationForm.FormC) ? text : text.Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            // Invalid UTF-16 (a lone surrogate) cannot be normalized; the lexer reports it as an unexpected character.
            return text;
        }
    }
}
