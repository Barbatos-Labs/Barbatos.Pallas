// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// What the user has typed, and where the cursor is: the structural input of the calculator (manual pp. 26-31).
/// </summary>
/// <remarks>
/// <para>
/// A document is immutable and every edit returns a new one, which is what makes undo a stack of documents rather
/// than a log of reversible operations. Nothing here throws for a keystroke: a move that has nowhere to go, and a
/// deletion with nothing to delete, return the document unchanged.
/// </para>
/// <para>
/// The slots of a structure are numbered in the order its Canonical Linear Syntax writes them, so the left and right
/// keys walk Σ(f, a, b) as body, lower bound, upper bound. The up and down keys use the order the screen shows
/// instead (<see cref="MathTemplates.VerticalOrder"/>), which is where the bounds of a sum sit above one another.
/// </para>
/// </remarks>
public sealed class MathDocument
{
    private const string OperatorEnds = "+-−×÷(,=<>≤≥≠∠•";

    private MathDocument(MathRow root, MathCursor cursor)
    {
        Root = root;
        Cursor = cursor;
    }

    /// <summary>Gets the empty input.</summary>
    public static MathDocument Empty { get; } = new(MathRow.Empty, MathCursor.Start);

    /// <summary>Gets the whole input.</summary>
    public MathRow Root { get; }

    /// <summary>Gets where the cursor is.</summary>
    public MathCursor Cursor { get; }

    /// <summary>Gets whether nothing has been typed.</summary>
    public bool IsEmpty => Root.IsEmpty;

    /// <summary>Gets the row the cursor is in.</summary>
    public MathRow CurrentRow => RowAt(Root, Cursor.Path, Cursor.Path.Length);

    /// <summary>Inserts a symbol where the cursor is.</summary>
    /// <param name="text">The Canonical Linear Syntax of the symbol, such as <c>7</c>, <c>+</c> or <c>sin(</c>.</param>
    /// <returns>The document with the symbol in it, the cursor after it.</returns>
    /// <exception cref="ArgumentException"><paramref name="text"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public MathDocument Insert(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        return Insert(new MathSymbol(text));
    }

    /// <summary>Inserts a structure where the cursor is, taking what was typed before it into its first slot.</summary>
    /// <param name="kind">The template.</param>
    /// <returns>The document with the structure in it, the cursor in the slot the user types next.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public MathDocument Insert(MathTemplateKind kind)
    {
        MathStructure structure = MathTemplates.Create(kind);
        MathRow row = CurrentRow;
        int capturing = MathTemplates.CapturingSlot(kind);
        int start = capturing < 0 ? Cursor.Index : OperandStart(row, Cursor.Index);
        bool captured = start < Cursor.Index;
        if (captured)
        {
            structure = structure with { Slots = structure.Slots.SetItem(capturing, new MathRow(row.Elements[start..Cursor.Index])) };
        }

        ImmutableArray<MathElement> elements = row.Elements.RemoveRange(start, Cursor.Index - start).Insert(start, structure);
        MathCursor cursor = new(Cursor.Path.Add(new MathStep(start, MathTemplates.EntrySlot(kind, captured))), 0);
        return new MathDocument(Replace(Root, Cursor.Path, 0, new MathRow(elements)), cursor);
    }

    /// <summary>Removes what is before the cursor, or steps into it when it is a structure that holds something.</summary>
    /// <returns>The document after the deletion.</returns>
    public MathDocument Backspace()
    {
        MathRow row = CurrentRow;
        if (Cursor.Index > 0)
        {
            MathElement previous = row[Cursor.Index - 1];
            if (previous is MathStructure { IsEmpty: false } structure)
            {
                // What is inside is still on the screen, so the cursor goes in rather than taking it all away.
                int slot = structure.Slots.Length - 1;
                return new MathDocument(Root, new MathCursor(Cursor.Path.Add(new MathStep(Cursor.Index - 1, slot)), structure.Slots[slot].Count));
            }

            MathRow shortened = new(row.Elements.RemoveAt(Cursor.Index - 1));
            return new MathDocument(Replace(Root, Cursor.Path, 0, shortened), Cursor with { Index = Cursor.Index - 1 });
        }

        if (Cursor.Depth == 0)
        {
            return this;
        }

        MathStep step = Cursor.Path[^1];
        ImmutableArray<MathStep> outer = Cursor.Path.RemoveAt(Cursor.Path.Length - 1);
        MathRow parent = RowAt(Root, outer, outer.Length);
        MathStructure current = (MathStructure)parent[step.Element];
        if (current.IsEmpty)
        {
            // An empty structure is what the user sees as nothing, so Delete takes it away.
            MathRow shortened = new(parent.Elements.RemoveAt(step.Element));
            return new MathDocument(Replace(Root, outer, 0, shortened), new MathCursor(outer, step.Element));
        }

        if (step.Slot > 0)
        {
            return new MathDocument(Root, new MathCursor(outer.Add(step with { Slot = step.Slot - 1 }), current.Slots[step.Slot - 1].Count));
        }

        return new MathDocument(Root, new MathCursor(outer, step.Element));
    }

    /// <summary>Moves the cursor one place to the left, into a structure where there is one.</summary>
    /// <returns>The document with the cursor moved.</returns>
    public MathDocument MoveLeft()
    {
        MathRow row = CurrentRow;
        if (Cursor.Index > 0)
        {
            if (row[Cursor.Index - 1] is MathStructure structure)
            {
                int slot = structure.Slots.Length - 1;
                return new MathDocument(Root, new MathCursor(Cursor.Path.Add(new MathStep(Cursor.Index - 1, slot)), structure.Slots[slot].Count));
            }

            return new MathDocument(Root, Cursor with { Index = Cursor.Index - 1 });
        }

        if (Cursor.Depth == 0)
        {
            return this;
        }

        MathStep step = Cursor.Path[^1];
        ImmutableArray<MathStep> outer = Cursor.Path.RemoveAt(Cursor.Path.Length - 1);
        if (step.Slot > 0)
        {
            MathStructure structure = (MathStructure)RowAt(Root, outer, outer.Length)[step.Element];
            return new MathDocument(Root, new MathCursor(outer.Add(step with { Slot = step.Slot - 1 }), structure.Slots[step.Slot - 1].Count));
        }

        return new MathDocument(Root, new MathCursor(outer, step.Element));
    }

    /// <summary>Moves the cursor one place to the right, into a structure where there is one.</summary>
    /// <returns>The document with the cursor moved.</returns>
    public MathDocument MoveRight()
    {
        MathRow row = CurrentRow;
        if (Cursor.Index < row.Count)
        {
            if (row[Cursor.Index] is MathStructure)
            {
                return new MathDocument(Root, new MathCursor(Cursor.Path.Add(new MathStep(Cursor.Index, 0)), 0));
            }

            return new MathDocument(Root, Cursor with { Index = Cursor.Index + 1 });
        }

        if (Cursor.Depth == 0)
        {
            return this;
        }

        MathStep step = Cursor.Path[^1];
        ImmutableArray<MathStep> outer = Cursor.Path.RemoveAt(Cursor.Path.Length - 1);
        MathStructure structure = (MathStructure)RowAt(Root, outer, outer.Length)[step.Element];
        if (step.Slot + 1 < structure.Slots.Length)
        {
            return new MathDocument(Root, new MathCursor(outer.Add(step with { Slot = step.Slot + 1 }), 0));
        }

        return new MathDocument(Root, new MathCursor(outer, step.Element + 1));
    }

    /// <summary>Moves the cursor to the slot above the one it is in, such as from a denominator to a numerator.</summary>
    /// <returns>The document with the cursor moved.</returns>
    public MathDocument MoveUp() => MoveVertically(-1);

    /// <summary>Moves the cursor to the slot below the one it is in.</summary>
    /// <returns>The document with the cursor moved.</returns>
    public MathDocument MoveDown() => MoveVertically(1);

    /// <summary>Moves the cursor before everything.</summary>
    /// <returns>The document with the cursor at the start of the input.</returns>
    public MathDocument MoveToStart() => new(Root, MathCursor.Start);

    /// <summary>Moves the cursor after everything.</summary>
    /// <returns>The document with the cursor at the end of the input.</returns>
    public MathDocument MoveToEnd() => new(Root, new MathCursor([], Root.Count));

    private MathDocument Insert(MathElement element)
    {
        MathRow row = CurrentRow;
        MathRow updated = new(row.Elements.Insert(Cursor.Index, element));
        return new MathDocument(Replace(Root, Cursor.Path, 0, updated), Cursor with { Index = Cursor.Index + 1 });
    }

    private MathDocument MoveVertically(int direction)
    {
        for (int depth = Cursor.Depth - 1; depth >= 0; depth--)
        {
            MathStep step = Cursor.Path[depth];
            ImmutableArray<MathStep> outer = Cursor.Path[..depth];
            MathStructure structure = (MathStructure)RowAt(Root, outer, depth)[step.Element];
            ImmutableArray<int> order = MathTemplates.VerticalOrder(structure.Kind);
            int place = order.IndexOf(step.Slot);
            if (place < 0 || place + direction < 0 || place + direction >= order.Length)
            {
                // This structure has nothing above or below the cursor; the one around it may have.
                continue;
            }

            int slot = order[place + direction];
            return new MathDocument(Root, new MathCursor(outer.Add(step with { Slot = slot }), structure.Slots[slot].Count));
        }

        return this;
    }

    private static int OperandStart(MathRow row, int index)
    {
        int start = index;
        while (start > 0 && row[start - 1] is MathSymbol digit && digit.Text.Length == 1 && (char.IsAsciiDigit(digit.Text[0]) || digit.Text[0] == '.'))
        {
            start--;
        }

        if (start < index)
        {
            return start;
        }

        return index > 0 && IsOperandEnd(row[index - 1]) ? index - 1 : index;
    }

    private static bool IsOperandEnd(MathElement element)
    {
        return element switch
        {
            MathStructure => true,
            MathSymbol { Text: [.., char last] } => !OperatorEnds.Contains(last, StringComparison.Ordinal),
            _ => false,
        };
    }

    private static MathRow RowAt(MathRow row, ImmutableArray<MathStep> path, int depth)
    {
        MathRow current = row;
        for (int index = 0; index < depth; index++)
        {
            MathStep step = path[index];
            current = ((MathStructure)current[step.Element]).Slots[step.Slot];
        }

        return current;
    }

    private static MathRow Replace(MathRow row, ImmutableArray<MathStep> path, int depth, MathRow replacement)
    {
        if (depth == path.Length)
        {
            return replacement;
        }

        MathStep step = path[depth];
        MathStructure structure = (MathStructure)row[step.Element];
        MathRow slot = Replace(structure.Slots[step.Slot], path, depth + 1, replacement);
        return new MathRow(row.Elements.SetItem(step.Element, structure with { Slots = structure.Slots.SetItem(step.Slot, slot) }));
    }
}
