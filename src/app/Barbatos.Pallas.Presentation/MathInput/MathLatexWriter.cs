// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Text;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Writes math input as LaTeX, the way the screen shows it, with the cursor drawn into the formula.
/// </summary>
/// <remarks>
/// <para>
/// The renderer of the desktop application has no hit-testing, so the cursor cannot be placed on top of the drawn
/// formula afterwards: it is part of what is drawn, and the formula is written again on every keystroke.
/// </para>
/// <para>
/// What it emits is what WpfMath can draw, which is less than LaTeX: there is no <c>\operatorname</c>, no
/// <c>\quad</c>, and <c>\#</c> and <c>\$</c> exist only inside <c>\text</c> (measured 23 Sep 2026). A function name
/// keeps its own bracket rather than <c>\left(</c>, because a <c>\left(</c> that the user has not closed yet is not
/// a formula at all.
/// </para>
/// </remarks>
public static class MathLatexWriter
{
    /// <summary>The cursor, drawn where it stands.</summary>
    private const string Caret = @"\color{red}{|}";

    /// <summary>An empty slot, which the calculator draws as a box.</summary>
    private const string Placeholder = @"\square";

    private static readonly FrozenDictionary<string, string> Spellings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["×"] = @"\times ",
        ["÷"] = @"\div ",
        ["−"] = "-",
        ["•"] = @"\cdot ",
        ["∠"] = @"\angle ",
        ["π"] = @"\pi ",
        ["≠"] = @"\neq ",
        ["≤"] = @"\leq ",
        ["≥"] = @"\geq ",
        ["▶"] = @"\blacktriangleright ",
        ["²"] = "^{2}",
        ["³"] = "^{3}",
        ["⁻¹"] = "^{-1}",
        ["°"] = @"^{\circ}",
        ["ʳ"] = @"^{\mathrm{r}}",
        ["ᵍ"] = @"^{\mathrm{g}}",
        ["′"] = "'",
        ["″"] = "''",
        ["%"] = @"\%",
        ["Ran#"] = @"\text{Ran\#}",
        ["RanInt#("] = @"\text{RanInt\#}(",
        ["sin("] = @"\sin(",
        ["cos("] = @"\cos(",
        ["tan("] = @"\tan(",
        ["sin⁻¹("] = @"\sin^{-1}(",
        ["cos⁻¹("] = @"\cos^{-1}(",
        ["tan⁻¹("] = @"\tan^{-1}(",
        ["sinh("] = @"\sinh(",
        ["cosh("] = @"\cosh(",
        ["tanh("] = @"\tanh(",
        ["sinh⁻¹("] = @"\sinh^{-1}(",
        ["cosh⁻¹("] = @"\cosh^{-1}(",
        ["tanh⁻¹("] = @"\tanh^{-1}(",
        ["ln("] = @"\ln(",
        ["log("] = @"\log(",
        ["x̄"] = @"\bar{x}",
        ["ȳ"] = @"\bar{y}",
        ["x̂"] = @"\hat{x}",
        ["ŷ"] = @"\hat{y}",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Writes a document, with the cursor where it stands.</summary>
    /// <param name="document">The document.</param>
    /// <returns>The LaTeX the screen draws.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public static string Write(MathDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Write(document, caret: true);
    }

    /// <summary>Writes a document.</summary>
    /// <param name="document">The document.</param>
    /// <param name="caret">Whether the cursor is drawn.</param>
    /// <returns>The LaTeX the screen draws.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public static string Write(MathDocument document, bool caret)
    {
        ArgumentNullException.ThrowIfNull(document);
        StringBuilder latex = new();
        Write(latex, document.Root, caret ? document.Cursor : null, 0);
        return latex.Length == 0 ? Placeholder : latex.ToString();
    }

    private static void Write(StringBuilder latex, MathRow row, MathCursor? cursor, int depth)
    {
        bool here = cursor is not null && cursor.Depth == depth;
        if (row.IsEmpty)
        {
            if (here)
            {
                latex.Append(Caret);
            }

            latex.Append(Placeholder);
            return;
        }

        int start = latex.Length;
        for (int index = 0; index < row.Count; index++)
        {
            if (here && cursor!.Index == index)
            {
                latex.Append(Caret);
            }

            Write(latex, row[index], Descend(cursor, depth, index), depth + 1, hasBase: latex.Length > start);
        }

        if (here && cursor!.Index == row.Count)
        {
            latex.Append(Caret);
        }
    }

    private static MathCursor? Descend(MathCursor? cursor, int depth, int element)
    {
        // The cursor is only handed on to the element it is inside: everything else is drawn without it.
        return cursor is not null && cursor.Depth > depth && cursor.Path[depth].Element == element ? cursor : null;
    }

    private static void Write(StringBuilder latex, MathElement element, MathCursor? cursor, int depth, bool hasBase = true)
    {
        switch (element)
        {
            case MathSymbol symbol:
                WriteSymbol(latex, symbol.Text, hasBase);
                break;
            case MathStructure structure:
                WriteStructure(latex, structure, cursor, depth);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, "Not a MathSymbol or a MathStructure.");
        }
    }

    private static void WriteStructure(StringBuilder latex, MathStructure structure, MathCursor? cursor, int depth)
    {
        switch (structure.Kind)
        {
            case MathTemplateKind.Parentheses:
                latex.Append(@"\left(");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"\right)");
                break;
            case MathTemplateKind.Fraction:
                latex.Append(@"\frac{");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append("}{");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append('}');
                break;
            case MathTemplateKind.MixedFraction:
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"\frac{");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append("}{");
                Slot(latex, structure, 2, cursor, depth);
                latex.Append('}');
                break;
            case MathTemplateKind.SquareRoot:
                latex.Append(@"\sqrt{");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append('}');
                break;
            case MathTemplateKind.Root:
                latex.Append(@"\sqrt[");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append("]{");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append('}');
                break;
            case MathTemplateKind.Power:
                latex.Append('{');
                Slot(latex, structure, 0, cursor, depth);
                latex.Append("}^{");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append('}');
                break;
            case MathTemplateKind.Abs:
                latex.Append(@"\left|");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"\right|");
                break;
            case MathTemplateKind.LogBase:
                latex.Append(@"\log_{");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"}\left(");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append(@"\right)");
                break;
            case MathTemplateKind.Integral:
                latex.Append(@"\int_{");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append("}^{");
                Slot(latex, structure, 2, cursor, depth);
                latex.Append("} ");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"\,dx");
                break;
            case MathTemplateKind.Sum or MathTemplateKind.Product:
                latex.Append(structure.Kind == MathTemplateKind.Sum ? @"\sum_{x=" : @"\prod_{x=");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append("}^{");
                Slot(latex, structure, 2, cursor, depth);
                latex.Append("} ");
                Slot(latex, structure, 0, cursor, depth);
                break;
            case MathTemplateKind.Derivative:
                latex.Append(@"\left.\frac{d}{dx}\left(");
                Slot(latex, structure, 0, cursor, depth);
                latex.Append(@"\right)\right|_{x=");
                Slot(latex, structure, 1, cursor, depth);
                latex.Append('}');
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(structure), structure.Kind, "Not a defined MathTemplateKind value.");
        }
    }

    private static void Slot(StringBuilder latex, MathStructure structure, int slot, MathCursor? cursor, int depth)
    {
        MathCursor? inside = cursor is not null && cursor.Depth >= depth && cursor.Path[depth - 1].Slot == slot ? cursor : null;
        Write(latex, structure.Slots[slot], inside, depth);
    }

    private static void WriteSymbol(StringBuilder latex, string text, bool hasBase)
    {
        if (Spellings.TryGetValue(text, out string? spelling))
        {
            if (!hasBase && spelling[0] is '^' or '_')
            {
                // A square with nothing to square is still on the screen while it is being typed, and TeX refuses a
                // script without a base, so it is given an empty one.
                latex.Append("{}");
            }

            latex.Append(spelling);
            return;
        }

        if (text.Length > 1 && text.All(char.IsAsciiLetterOrDigit))
        {
            latex.Append(@"\mathrm{").Append(text).Append('}');
            return;
        }

        if (text.Length > 1 && text[^1] == '(' && text[..^1].All(char.IsAsciiLetterOrDigit))
        {
            latex.Append(@"\mathrm{").Append(text[..^1]).Append("}(");
            return;
        }

        foreach (char character in text)
        {
            if (character is '#' or '$' or '&' or '_' or '{' or '}')
            {
                // Characters TeX reads as its own: drawn as themselves, upright, rather than obeyed.
                latex.Append(@"\text{\").Append(character).Append('}');
                continue;
            }

            latex.Append(character);
        }
    }
}
