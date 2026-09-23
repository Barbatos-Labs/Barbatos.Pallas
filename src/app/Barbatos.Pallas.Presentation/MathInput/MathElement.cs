// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One thing in a row of math input: a symbol, or a structure with slots of its own.
/// </summary>
public abstract record MathElement;

/// <summary>
/// A symbol as one keystroke leaves it: a digit, an operator, a variable, or a whole name such as <c>sin(</c>.
/// </summary>
/// <param name="Text">The Canonical Linear Syntax the symbol stands for.</param>
/// <remarks>
/// A symbol is what the cursor steps over and what one Delete removes, so a key that inserts <c>sin(</c> inserts one
/// symbol and not four. Digits are separate symbols, because a calculator deletes a number one digit at a time.
/// </remarks>
public sealed record MathSymbol(string Text) : MathElement;

/// <summary>
/// A structure: a template and one row per slot.
/// </summary>
/// <param name="Kind">Which template.</param>
/// <param name="Slots">Its slots, in the order <see cref="MathTemplates.SlotCount"/> gives them.</param>
public sealed record MathStructure(MathTemplateKind Kind, ImmutableArray<MathRow> Slots) : MathElement
{
    /// <summary>Gets whether every slot of the structure is empty.</summary>
    public bool IsEmpty => Slots.All(slot => slot.IsEmpty);
}

/// <summary>
/// A row of elements: the whole input, or one slot of a structure.
/// </summary>
/// <param name="Elements">The elements, left to right.</param>
public sealed record MathRow(ImmutableArray<MathElement> Elements)
{
    /// <summary>Gets the empty row.</summary>
    public static MathRow Empty { get; } = new([]);

    /// <summary>Gets whether the row holds nothing.</summary>
    public bool IsEmpty => Elements.IsEmpty;

    /// <summary>Gets how many elements the row holds.</summary>
    public int Count => Elements.Length;

    /// <summary>Gets the element at a position.</summary>
    /// <param name="index">The position.</param>
    public MathElement this[int index] => Elements[index];
}
