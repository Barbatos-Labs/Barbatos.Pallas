// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The spellings the lexer recognizes: the fixed grammar (operators, punctuation) and the names of functions,
/// constants, variables and commands.
/// </summary>
/// <remarks>
/// <para>
/// A vocabulary holds names only, with no meaning attached: whether <c>sin(</c> takes one argument, and what it
/// computes, is the engine's business. The parser needs the names because text alone cannot tell <c>sinh(</c> from
/// <c>sin(</c> followed by <c>h</c>, or know that <c>AtWt(</c> is one token.
/// </para>
/// <para>
/// Matching is longest-first among the symbols available in the application, so <c>÷R</c> wins over <c>÷</c> and
/// <c>d/dx(</c> over <c>d</c>. Symbols are grouped by first character, which keeps lexing free of allocations on
/// every target framework.
/// </para>
/// </remarks>
public sealed class SyntaxVocabulary
{
    private readonly FrozenDictionary<char, SyntaxSymbol[]> _byFirstCharacter;

    private SyntaxVocabulary(ImmutableArray<SyntaxSymbol> symbols)
    {
        string? duplicate = symbols.GroupBy(symbol => symbol.Text, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException($"'{duplicate}' is already in the vocabulary.", nameof(symbols));
        }

        Symbols = symbols;
        _byFirstCharacter = symbols
            .GroupBy(symbol => symbol.Text[0])
            .ToFrozenDictionary(
                group => group.Key,
                group => group.OrderByDescending(symbol => symbol.Text.Length).ThenBy(symbol => symbol.Text, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Gets the vocabulary of the reference calculator: its functions, commands, constants, variables, the 40 unit
    /// conversions and the 47 scientific constants, with the input aliases of docs/LINEAR-SYNTAX.md.
    /// </summary>
    public static SyntaxVocabulary Standard { get; } = new([.. StandardVocabulary.Create()]);

    /// <summary>Gets every symbol, canonical spellings and aliases alike.</summary>
    public ImmutableArray<SyntaxSymbol> Symbols { get; }

    /// <summary>
    /// Returns a vocabulary that also recognizes <paramref name="names"/>, for example functions registered by a plugin.
    /// </summary>
    /// <param name="names">Names created with <see cref="SyntaxSymbol.CreateName"/> or their aliases.</param>
    /// <returns>A new vocabulary; this one is unchanged.</returns>
    /// <exception cref="ArgumentException">A symbol is not a name, or its spelling is already in the vocabulary.</exception>
    public SyntaxVocabulary With(params IEnumerable<SyntaxSymbol> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        ImmutableArray<SyntaxSymbol>.Builder builder = Symbols.ToBuilder();
        foreach (SyntaxSymbol name in names)
        {
            ArgumentNullException.ThrowIfNull(name, nameof(names));
            if (name.Kind > SymbolKind.UnitConversion)
            {
                throw new ArgumentException($"'{name.Text}' is {name.Kind}; only names can be added.", nameof(names));
            }

            builder.Add(name);
        }

        return new SyntaxVocabulary(builder.ToImmutable());
    }

    /// <summary>
    /// Finds the longest symbol available in <paramref name="app"/> that <paramref name="text"/>, never empty, starts with.
    /// </summary>
    internal SyntaxSymbol? LongestMatch(ReadOnlySpan<char> text, CalculatorApp app)
    {
        if (!_byFirstCharacter.TryGetValue(text[0], out SyntaxSymbol[]? candidates))
        {
            return null;
        }

        foreach (SyntaxSymbol candidate in candidates)
        {
            if (text.StartsWith(candidate.Text, StringComparison.Ordinal) && candidate.IsAvailableIn(app))
            {
                return candidate;
            }
        }

        return null;
    }
}
