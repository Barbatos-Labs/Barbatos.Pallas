// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One entry of the CATALOG: what is written in the list, and what choosing it puts on the line.
/// </summary>
/// <param name="Label">What the list shows, such as <c>sin(</c>, <c>nCr</c> or <c>h</c> for Planck's constant.</param>
/// <param name="Text">The Canonical Linear Syntax it stands for, such as <c>sin(</c>, <c>C</c> or <c>@h</c>.</param>
/// <param name="Action">What choosing it does to the line: a symbol, or a structure where the line has one.</param>
public sealed record CatalogItem(string Label, string Text, KeyAction Action);

/// <summary>
/// One group of the CATALOG and its entries.
/// </summary>
/// <param name="Group">Which group.</param>
/// <param name="NameKey">The localization key of its name; the application's own group is named after it.</param>
/// <param name="Items">Its entries, in the order the vocabulary has them.</param>
public sealed record CatalogSection(CatalogGroup Group, string NameKey, ImmutableArray<CatalogItem> Items);

/// <summary>
/// The CATALOG of an application (manual pp. 51-69), built from the vocabulary the engine reads.
/// </summary>
/// <remarks>
/// <para>
/// There is no list of names here: every entry is a symbol of the vocabulary that the application reads, so a
/// scientific constant a data set adds, or a function a plugin adds, is in the CATALOG without anyone writing it
/// down twice. What is written here is where the manual puts a name - which group - and that is a table, because
/// the manual's groups are not something a symbol knows about itself.
/// </para>
/// <para>
/// A name that the line has a structure for - the integral, the square root, the absolute value - puts that
/// structure on the line rather than its linear spelling, so what is chosen is edited as if it had been typed on
/// its key.
/// </para>
/// </remarks>
public static class CalculatorCatalog
{
    // The tables below are switches rather than dictionaries built as the type starts: a table filled in a static
    // initializer is invisible to mutation testing (CLAUDE.md), and a switch over strings is the same table written
    // out.

    /// <summary>Where the manual puts a name that is not application-specific (pp. 51-69).</summary>
    /// <remarks>A name in none of these goes to Other, and a test says which those are, so a new function is placed.</remarks>
    private static CatalogGroup ManualGroupOf(string text) => text switch
    {
        "d/dx(" or "∫(" or "Σ(" or "Π(" or "÷R" or "log(" or "ln(" => CatalogGroup.FunctionAnalysis,
        "%" or "!" or "P" or "Ran#" or "RanInt#(" => CatalogGroup.Probability,
        "GCD(" or "LCM(" or "Abs(" or "Int(" or "Intg(" or "Rnd(" => CatalogGroup.NumericCalc,
        "°" or "ʳ" or "ᵍ" or "′" or "″" or "Pol(" or "Rec(" => CatalogGroup.AngleCoordinates,
        "sin(" or "cos(" or "tan(" or "sin⁻¹(" or "cos⁻¹(" or "tan⁻¹(" => CatalogGroup.Trigonometry,
        "sinh(" or "cosh(" or "tanh(" or "sinh⁻¹(" or "cosh⁻¹(" or "tanh⁻¹(" => CatalogGroup.Trigonometry,
        "AtWt(" => CatalogGroup.AtomicTable,
        _ => CatalogGroup.Other,
    };

    /// <summary>The structure the line has for a name, which is what choosing the name puts there.</summary>
    private static MathTemplateKind? TemplateOf(string text) => text switch
    {
        "√(" => MathTemplateKind.SquareRoot,
        "ˣ√" => MathTemplateKind.Root,
        "^" => MathTemplateKind.Power,
        "⌟" => MathTemplateKind.Fraction,
        "Abs(" => MathTemplateKind.Abs,
        "∫(" => MathTemplateKind.Integral,
        "Σ(" => MathTemplateKind.Sum,
        "Π(" => MathTemplateKind.Product,
        "d/dx(" => MathTemplateKind.Derivative,
        _ => null,
    };

    /// <summary>
    /// Whether a symbol is on every keypad already: the four operations - the variables and memories, on the digit
    /// keys, are left out by their kind.
    /// </summary>
    private static bool IsOnTheKeys(string text) => text is "+" or "-" or "−" or "×" or "÷";

    /// <summary>Builds the CATALOG of an application.</summary>
    /// <param name="vocabulary">What the engine reads: the standard names and whatever data and plugins added.</param>
    /// <param name="app">The application.</param>
    /// <returns>Its groups in the calculator's order, each with at least one entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="vocabulary"/> is <see langword="null"/>.</exception>
    public static ImmutableArray<CatalogSection> For(SyntaxVocabulary vocabulary, CalculatorApp app)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);

        Dictionary<CatalogGroup, List<CatalogItem>> groups = [];
        foreach (SyntaxSymbol symbol in vocabulary.Symbols)
        {
            if (!ReferenceEquals(symbol.Canonical, symbol) || !IsIn(symbol, app) || GroupOf(symbol) is not { } group)
            {
                continue;
            }

            Add(groups, group, symbol.Text, Label(symbol));
        }

        // Combinations are not a symbol: C between two operands is read as the operator (docs/LINEAR-SYNTAX.md §5).
        // Base-N has neither - there C is a digit - and its prefixes are read by the lexer, not the vocabulary.
        if (app is CalculatorApp.BaseN)
        {
            foreach (string prefix in (string[])["d", "h", "b", "o"])
            {
                Add(groups, CatalogGroup.Application, prefix, prefix);
            }
        }
        else if (groups.ContainsKey(CatalogGroup.Probability))
        {
            Add(groups, CatalogGroup.Probability, "C", "nCr");
        }

        return
        [
            .. Enum.GetValues<CatalogGroup>()
                .Where(groups.ContainsKey)
                .Select(group => new CatalogSection(group, NameKey(group, app), [.. groups[group]])),
        ];
    }

    /// <summary>Returns what choosing a name does to the line.</summary>
    /// <param name="text">The name, as Canonical Linear Syntax.</param>
    /// <returns>The structure the line has for it, or the name itself as a symbol.</returns>
    public static KeyAction ActionOf(string text) =>
        TemplateOf(text) is { } kind ? new InsertTemplate(kind) : new InsertSymbol(text);

    private static void Add(Dictionary<CatalogGroup, List<CatalogItem>> groups, CatalogGroup group, string text, string label)
    {
        if (!groups.TryGetValue(group, out List<CatalogItem>? items))
        {
            items = [];
            groups[group] = items;
        }

        items.Add(new CatalogItem(label, text, ActionOf(text)));
    }

    private static bool IsIn(SyntaxSymbol symbol, CalculatorApp app) => symbol.Applications.IsEmpty || symbol.Applications.Contains(app);

    private static CatalogGroup? GroupOf(SyntaxSymbol symbol)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Variable or SymbolKind.Memory or SymbolKind.OpenParenthesis or SymbolKind.CloseParenthesis
                or SymbolKind.Comma or SymbolKind.Colon:
                return null;
            case SymbolKind.BinaryOperator when IsOnTheKeys(symbol.Text):
                return null;
        }

        // A name only some applications have, and Calculate is not one of them, is that application's own.
        if (!symbol.Applications.IsEmpty && !symbol.Applications.Contains(CalculatorApp.Calculate))
        {
            return CatalogGroup.Application;
        }

        return symbol.Kind switch
        {
            SymbolKind.ScientificConstant => CatalogGroup.ScientificConstants,
            SymbolKind.UnitConversion => CatalogGroup.UnitConversions,
            SymbolKind.EngineeringSymbol => CatalogGroup.EngineerSymbols,
            SymbolKind.RelationOperator => CatalogGroup.Relations,
            _ => ManualGroupOf(symbol.Text),
        };
    }

    private static string Label(SyntaxSymbol symbol)
    {
        // Permutations are nPr on the calculator's menu. A scientific constant is spelled @h and an engineering symbol
        // _k so that they cannot be read as a variable; the menu shows the name itself.
        if (symbol.Text is "P")
        {
            return "nPr";
        }

        return symbol.Kind is SymbolKind.ScientificConstant or SymbolKind.EngineeringSymbol ? symbol.Text[1..] : symbol.Text;
    }

    private static string NameKey(CatalogGroup group, CalculatorApp app) =>
        group is CatalogGroup.Application ? CalculatorApps.Of(app).NameKey : "catalog." + group;
}
