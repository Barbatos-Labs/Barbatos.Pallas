// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One step from a row into a slot of one of its structures.
/// </summary>
/// <param name="Element">The structure's place in the row.</param>
/// <param name="Slot">Which slot of it.</param>
public readonly record struct MathStep(int Element, int Slot);

/// <summary>
/// Where the cursor is: the row it is in, and where in that row.
/// </summary>
/// <param name="Path">The steps from the whole input down to the row the cursor is in; empty for the input itself.</param>
/// <param name="Index">How many elements of that row are before the cursor.</param>
public sealed record MathCursor(ImmutableArray<MathStep> Path, int Index)
{
    /// <summary>Gets the cursor of an empty input: at the start of the input itself.</summary>
    public static MathCursor Start { get; } = new([], 0);

    /// <summary>Gets how deep inside the structures the cursor is; 0 in the input itself.</summary>
    public int Depth => Path.Length;
}
