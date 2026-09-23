// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// What each template is made of: how many slots it has, which of them the cursor enters, and how they sit above
/// one another on the screen.
/// </summary>
/// <remarks>
/// The table is here and nowhere else, so a new template is a row here plus a case in each writer, and the editor
/// itself does not grow.
/// </remarks>
public static class MathTemplates
{
    /// <summary>Returns how many slots a template has.</summary>
    /// <param name="kind">The template.</param>
    /// <returns>The number of slots.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public static int SlotCount(MathTemplateKind kind)
    {
        return kind switch
        {
            MathTemplateKind.Parentheses or MathTemplateKind.SquareRoot or MathTemplateKind.Abs => 1,
            MathTemplateKind.Fraction or MathTemplateKind.Root or MathTemplateKind.Power or MathTemplateKind.LogBase or MathTemplateKind.Derivative => 2,
            MathTemplateKind.MixedFraction or MathTemplateKind.Integral or MathTemplateKind.Sum or MathTemplateKind.Product => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a defined MathTemplateKind value."),
        };
    }

    /// <summary>
    /// Returns the slot that takes what was typed before the template, or -1 when the template takes nothing.
    /// </summary>
    /// <param name="kind">The template.</param>
    /// <returns>The slot, or -1.</returns>
    /// <remarks>
    /// Typing 12 and then the fraction key means twelve over something, not twelve beside a fraction; the same holds
    /// for a power and for the index of a root. Everything else opens empty.
    /// </remarks>
    public static int CapturingSlot(MathTemplateKind kind)
    {
        return kind switch
        {
            MathTemplateKind.Fraction or MathTemplateKind.Power => 0,
            MathTemplateKind.Root => 0,
            _ => -1,
        };
    }

    /// <summary>Returns the slot the cursor goes to when the template is inserted.</summary>
    /// <param name="kind">The template.</param>
    /// <param name="captured">Whether something was taken into <see cref="CapturingSlot"/>.</param>
    /// <returns>The slot.</returns>
    public static int EntrySlot(MathTemplateKind kind, bool captured)
    {
        // What was captured is behind the cursor already, so the cursor goes on to the slot after it.
        return captured ? CapturingSlot(kind) + 1 : 0;
    }

    /// <summary>Returns the slots from the top of the screen to the bottom, for the up and down keys.</summary>
    /// <param name="kind">The template.</param>
    /// <returns>The slots that sit above one another; slots reached only by the left and right keys are not in it.</returns>
    public static ImmutableArray<int> VerticalOrder(MathTemplateKind kind)
    {
        return kind switch
        {
            MathTemplateKind.Fraction => [0, 1],
            MathTemplateKind.MixedFraction => [1, 2],
            MathTemplateKind.Power => [1, 0],
            MathTemplateKind.Root => [0, 1],
            MathTemplateKind.LogBase => [1, 0],
            MathTemplateKind.Integral or MathTemplateKind.Sum or MathTemplateKind.Product => [2, 1],
            _ => [],
        };
    }

    /// <summary>Creates an empty structure.</summary>
    /// <param name="kind">The template.</param>
    /// <returns>The structure, with every slot empty.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public static MathStructure Create(MathTemplateKind kind) =>
        new(kind, [.. Enumerable.Repeat(MathRow.Empty, SlotCount(kind))]);
}
