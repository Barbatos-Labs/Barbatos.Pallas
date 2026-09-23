// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Writes math input as Canonical Linear Syntax: the one road from the screen into the parser.
/// </summary>
/// <remarks>
/// A slot is parenthesized unless it cannot be read as anything else - a number, a single symbol, or a structure
/// that carries its own brackets. Without that, a fraction inside a fraction would print as <c>1⌟2⌟3</c>, which is
/// the mixed fraction one and a half.
/// </remarks>
public static class MathLinearWriter
{
    /// <summary>Writes a document.</summary>
    /// <param name="document">The document.</param>
    /// <returns>Its Canonical Linear Syntax.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public static string Write(MathDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        StringBuilder text = new();
        Write(text, document.Root);
        return text.ToString();
    }

    /// <summary>Writes a row.</summary>
    /// <param name="row">The row.</param>
    /// <returns>Its Canonical Linear Syntax.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> is <see langword="null"/>.</exception>
    public static string Write(MathRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        StringBuilder text = new();
        Write(text, row);
        return text.ToString();
    }

    private static void Write(StringBuilder text, MathRow row)
    {
        foreach (MathElement element in row.Elements)
        {
            switch (element)
            {
                case MathSymbol symbol:
                    text.Append(symbol.Text);
                    break;
                case MathStructure structure:
                    Write(text, structure);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(row), element, "Not a MathSymbol or a MathStructure.");
            }
        }
    }

    private static void Write(StringBuilder text, MathStructure structure)
    {
        switch (structure.Kind)
        {
            case MathTemplateKind.Parentheses:
                Call(text, "(", structure, 0);
                break;
            case MathTemplateKind.Fraction:
                Operand(text, structure.Slots[0]);
                text.Append('⌟');
                Operand(text, structure.Slots[1]);
                break;
            case MathTemplateKind.MixedFraction:
                Operand(text, structure.Slots[0]);
                text.Append('⌟');
                Operand(text, structure.Slots[1]);
                text.Append('⌟');
                Operand(text, structure.Slots[2]);
                break;
            case MathTemplateKind.SquareRoot:
                Call(text, "√(", structure, 0);
                break;
            case MathTemplateKind.Root:
                Operand(text, structure.Slots[0]);
                text.Append("ˣ√(");
                Write(text, structure.Slots[1]);
                text.Append(')');
                break;
            case MathTemplateKind.Power:
                Operand(text, structure.Slots[0]);
                text.Append("^(");
                Write(text, structure.Slots[1]);
                text.Append(')');
                break;
            case MathTemplateKind.Abs:
                Call(text, "Abs(", structure, 0);
                break;
            case MathTemplateKind.LogBase:
                Call(text, "log(", structure, 0, 1);
                break;
            case MathTemplateKind.Integral:
                Call(text, "∫(", structure, 0, 1, 2);
                break;
            case MathTemplateKind.Sum:
                Call(text, "Σ(", structure, 0, 1, 2);
                break;
            case MathTemplateKind.Product:
                Call(text, "Π(", structure, 0, 1, 2);
                break;
            case MathTemplateKind.Derivative:
                Call(text, "d/dx(", structure, 0, 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(structure), structure.Kind, "Not a defined MathTemplateKind value.");
        }
    }

    private static void Call(StringBuilder text, string name, MathStructure structure, params int[] slots)
    {
        text.Append(name);
        for (int index = 0; index < slots.Length; index++)
        {
            if (index > 0)
            {
                text.Append(',');
            }

            Write(text, structure.Slots[slots[index]]);
        }

        text.Append(')');
    }

    private static void Operand(StringBuilder text, MathRow row)
    {
        if (IsAtomic(row))
        {
            Write(text, row);
            return;
        }

        text.Append('(');
        Write(text, row);
        text.Append(')');
    }

    private static bool IsAtomic(MathRow row)
    {
        if (row.IsEmpty)
        {
            // A slot the user has not filled in is written as nothing, which the parser reports where it stands.
            return true;
        }

        if (row.Count == 1)
        {
            return row[0] is MathSymbol || (row[0] is MathStructure structure && IsSelfDelimiting(structure.Kind));
        }

        // A number is as atomic as one symbol: 12⌟5 needs no brackets around the 12.
        return !row.IsEmpty && row.Elements.All(element => element is MathSymbol { Text.Length: 1 } symbol && (char.IsAsciiDigit(symbol.Text[0]) || symbol.Text[0] == '.'));
    }

    private static bool IsSelfDelimiting(MathTemplateKind kind)
    {
        return kind is MathTemplateKind.Parentheses
            or MathTemplateKind.SquareRoot
            or MathTemplateKind.Abs
            or MathTemplateKind.LogBase
            or MathTemplateKind.Integral
            or MathTemplateKind.Sum
            or MathTemplateKind.Product
            or MathTemplateKind.Derivative;
    }
}
