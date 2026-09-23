// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Text;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Where the cursor would be for a place in the written text.
/// </summary>
/// <param name="Offset">How many characters of the text come before it.</param>
/// <param name="Cursor">The cursor of the input at that place.</param>
public readonly record struct MathTextPosition(int Offset, MathCursor Cursor);

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
        Write(text, document.Root, [], null);
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
        Write(text, row, [], null);
        return text.ToString();
    }

    /// <summary>
    /// Returns where the cursor would be for each place in the written text, in the order the text is written.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The places, each with the cursor that belongs to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This is how an error finds its place: the engine reports the span of the text it could not calculate, and the
    /// calculator puts the cursor there (manual p. 162).
    /// </remarks>
    public static ImmutableArray<MathTextPosition> Positions(MathDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        List<MathTextPosition> positions = [];
        Write(new StringBuilder(), document.Root, [], positions);
        return [.. positions];
    }

    private static void Write(StringBuilder text, MathRow row, ImmutableArray<MathStep> path, List<MathTextPosition>? positions)
    {
        for (int index = 0; index < row.Count; index++)
        {
            positions?.Add(new MathTextPosition(text.Length, new MathCursor(path, index)));
            switch (row[index])
            {
                case MathSymbol symbol:
                    text.Append(symbol.Text);
                    break;
                case MathStructure structure:
                    Write(text, structure, path.Add(new MathStep(index, 0)), positions);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(row), row[index], "Not a MathSymbol or a MathStructure.");
            }
        }

        positions?.Add(new MathTextPosition(text.Length, new MathCursor(path, row.Count)));
    }

    private static void Write(StringBuilder text, MathStructure structure, ImmutableArray<MathStep> path, List<MathTextPosition>? positions)
    {
        switch (structure.Kind)
        {
            case MathTemplateKind.Parentheses:
                Call(text, "(", structure, path, positions, 0);
                break;
            case MathTemplateKind.Fraction:
                Operand(text, structure, 0, path, positions);
                text.Append('⌟');
                Operand(text, structure, 1, path, positions);
                break;
            case MathTemplateKind.MixedFraction:
                Operand(text, structure, 0, path, positions);
                text.Append('⌟');
                Operand(text, structure, 1, path, positions);
                text.Append('⌟');
                Operand(text, structure, 2, path, positions);
                break;
            case MathTemplateKind.SquareRoot:
                Call(text, "√(", structure, path, positions, 0);
                break;
            case MathTemplateKind.Root:
                Operand(text, structure, 0, path, positions);
                text.Append("ˣ√(");
                Slot(text, structure, 1, path, positions);
                text.Append(')');
                break;
            case MathTemplateKind.Power:
                Operand(text, structure, 0, path, positions);
                text.Append("^(");
                Slot(text, structure, 1, path, positions);
                text.Append(')');
                break;
            case MathTemplateKind.Abs:
                Call(text, "Abs(", structure, path, positions, 0);
                break;
            case MathTemplateKind.LogBase:
                Call(text, "log(", structure, path, positions, 0, 1);
                break;
            case MathTemplateKind.Integral:
                Call(text, "∫(", structure, path, positions, 0, 1, 2);
                break;
            case MathTemplateKind.Sum:
                Call(text, "Σ(", structure, path, positions, 0, 1, 2);
                break;
            case MathTemplateKind.Product:
                Call(text, "Π(", structure, path, positions, 0, 1, 2);
                break;
            case MathTemplateKind.Derivative:
                Call(text, "d/dx(", structure, path, positions, 0, 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(structure), structure.Kind, "Not a defined MathTemplateKind value.");
        }
    }

    private static void Call(StringBuilder text, string name, MathStructure structure, ImmutableArray<MathStep> path, List<MathTextPosition>? positions, params int[] slots)
    {
        text.Append(name);
        for (int index = 0; index < slots.Length; index++)
        {
            if (index > 0)
            {
                text.Append(',');
            }

            Slot(text, structure, slots[index], path, positions);
        }

        text.Append(')');
    }

    private static void Slot(StringBuilder text, MathStructure structure, int slot, ImmutableArray<MathStep> path, List<MathTextPosition>? positions)
    {
        Write(text, structure.Slots[slot], path.SetItem(path.Length - 1, path[^1] with { Slot = slot }), positions);
    }

    private static void Operand(StringBuilder text, MathStructure structure, int slot, ImmutableArray<MathStep> path, List<MathTextPosition>? positions)
    {
        if (IsAtomic(structure.Slots[slot]))
        {
            Slot(text, structure, slot, path, positions);
            return;
        }

        text.Append('(');
        Slot(text, structure, slot, path, positions);
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
        return row.Elements.All(element => element is MathSymbol { Text.Length: 1 } symbol && (char.IsAsciiDigit(symbol.Text[0]) || symbol.Text[0] == '.'));
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
