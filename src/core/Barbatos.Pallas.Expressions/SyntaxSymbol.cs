// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Text;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// One spelling the lexer recognizes: a name such as <c>sin(</c> or <c>Ans</c>, or an operator such as <c>×</c>.
/// </summary>
/// <remarks>
/// An alias (<c>*</c> for <c>×</c>, <c>sqrt(</c> for <c>√(</c>) is a symbol of its own whose <see cref="Canonical"/>
/// is the symbol the printers write. Syntax trees always hold canonical symbols.
/// </remarks>
public sealed class SyntaxSymbol
{
    private SyntaxSymbol(
        string text,
        SymbolKind kind,
        ImmutableArray<CalculatorApp> applications,
        SyntaxSymbol? canonical,
        BinaryOperator? binaryOperator,
        PostfixOperator? postfixOperator,
        RelationOperator? relationOperator)
    {
        // Input is normalized to form C before lexing, so spellings are too: "ȳ" may arrive as y + U+0304.
        Text = text.Normalize(NormalizationForm.FormC);
        Kind = kind;
        Applications = applications;
        Canonical = canonical ?? this;
        BinaryOperator = binaryOperator;
        PostfixOperator = postfixOperator;
        RelationOperator = relationOperator;
    }

    /// <summary>Gets the exact text that is matched in the input.</summary>
    public string Text { get; }

    /// <summary>Gets what the symbol is.</summary>
    public SymbolKind Kind { get; }

    /// <summary>Gets the applications the symbol is available in; empty means every application.</summary>
    public ImmutableArray<CalculatorApp> Applications { get; }

    /// <summary>Gets the symbol printers write for this one: itself, unless this is an alias.</summary>
    public SyntaxSymbol Canonical { get; }

    /// <summary>Gets the operator, when <see cref="Kind"/> is <see cref="SymbolKind.BinaryOperator"/>.</summary>
    public BinaryOperator? BinaryOperator { get; }

    /// <summary>Gets the operator, when <see cref="Kind"/> is <see cref="SymbolKind.PostfixOperator"/>.</summary>
    public PostfixOperator? PostfixOperator { get; }

    /// <summary>Gets the operator, when <see cref="Kind"/> is <see cref="SymbolKind.RelationOperator"/>.</summary>
    public RelationOperator? RelationOperator { get; }

    /// <summary>
    /// Creates a name that a vocabulary can recognize, such as a plugin function.
    /// </summary>
    /// <param name="text">The spelling. A function ends with <c>(</c>, a scientific constant starts with <c>@</c>, an
    /// engineering symbol with <c>_</c>, and a unit conversion contains <c>▶</c>.</param>
    /// <param name="kind">A name kind, from <see cref="SymbolKind.Function"/> to <see cref="SymbolKind.UnitConversion"/>.</param>
    /// <param name="applications">The applications the name is available in; none means every application.</param>
    /// <returns>The canonical symbol.</returns>
    /// <exception cref="ArgumentException"><paramref name="text"/> does not follow the spelling rule of <paramref name="kind"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a name kind.</exception>
    public static SyntaxSymbol CreateName(string text, SymbolKind kind, params ReadOnlySpan<CalculatorApp> applications)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (kind is < SymbolKind.Function or > SymbolKind.UnitConversion)
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only names can be created; operators and punctuation are fixed grammar.");
        }

        ValidateSpelling(text, kind);
        return new SyntaxSymbol(text, kind, [.. applications], null, null, null, null);
    }

    /// <summary>
    /// Creates another spelling of this symbol, recognized on input and printed as this symbol's canonical text.
    /// </summary>
    /// <param name="text">The alias spelling, following the same spelling rule.</param>
    /// <returns>The alias.</returns>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty or does not follow the spelling rule.</exception>
    public SyntaxSymbol CreateAlias(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (Kind <= SymbolKind.UnitConversion)
        {
            ValidateSpelling(text, Kind);
        }

        return new SyntaxSymbol(text, Kind, Applications, Canonical, BinaryOperator, PostfixOperator, RelationOperator);
    }

    /// <summary>Returns whether the symbol can be used in <paramref name="app"/>.</summary>
    /// <param name="app">The application.</param>
    /// <returns><see langword="true"/> if <see cref="Applications"/> is empty or contains <paramref name="app"/>.</returns>
    public bool IsAvailableIn(CalculatorApp app) => Applications.IsEmpty || Applications.Contains(app);

    /// <inheritdoc/>
    public override string ToString() => Text;

    internal static SyntaxSymbol CreateBinary(string text, BinaryOperator binaryOperator, params ReadOnlySpan<CalculatorApp> applications)
    {
        return new SyntaxSymbol(text, SymbolKind.BinaryOperator, [.. applications], null, binaryOperator, null, null);
    }

    internal static SyntaxSymbol CreatePostfix(string text, PostfixOperator postfixOperator, params ReadOnlySpan<CalculatorApp> applications)
    {
        return new SyntaxSymbol(text, SymbolKind.PostfixOperator, [.. applications], null, null, postfixOperator, null);
    }

    internal static SyntaxSymbol CreateRelation(string text, RelationOperator relationOperator)
    {
        return new SyntaxSymbol(text, SymbolKind.RelationOperator, [], null, null, null, relationOperator);
    }

    internal static SyntaxSymbol CreatePunctuation(string text, SymbolKind kind, params ReadOnlySpan<CalculatorApp> applications)
    {
        return new SyntaxSymbol(text, kind, [.. applications], null, null, null, null);
    }

    private static void ValidateSpelling(string text, SymbolKind kind)
    {
        // A name starting with a digit or a decimal point would never be matched: the lexer reads a number first.
        if (char.IsAsciiDigit(text[0]) || text[0] == '.' || char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]))
        {
            throw new ArgumentException("A name cannot start with a digit or a decimal point, or start or end with white space.", nameof(text));
        }

        bool valid = kind switch
        {
            SymbolKind.Function => text.Length > 1 && text[^1] == '(',
            SymbolKind.ScientificConstant => text.Length > 1 && text[0] == '@',
            SymbolKind.EngineeringSymbol => text.Length > 1 && text[0] == '_',
            SymbolKind.UnitConversion => text.Contains('▶', StringComparison.Ordinal) || text.Contains("->", StringComparison.Ordinal),
            _ => text[^1] != '(',
        };

        if (!valid)
        {
            throw new ArgumentException($"'{text}' does not follow the spelling rule of {kind}.", nameof(text));
        }
    }
}
