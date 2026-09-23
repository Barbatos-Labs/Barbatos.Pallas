// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One key: what it does, and what is written on it.
/// </summary>
/// <param name="Id">Which key.</param>
/// <param name="Glyph">What is written on the key face.</param>
/// <param name="Primary">What the key does.</param>
/// <param name="Shift">What it does in the shift mode, or <see langword="null"/>.</param>
/// <param name="ShiftGlyph">What is written above the key for the shift mode, or <see langword="null"/>.</param>
/// <param name="Alpha">What it does in the alpha mode, or <see langword="null"/>.</param>
/// <param name="AlphaGlyph">What is written above the key for the alpha mode, or <see langword="null"/>.</param>
/// <remarks>
/// A glyph is the mathematics itself (7, ×, sin, √), which is the same in every language and is therefore not
/// localized; a key whose face is a word carries its own label in the host instead.
/// </remarks>
public sealed record KeyDefinition(
    KeyId Id,
    string Glyph,
    KeyAction Primary,
    KeyAction? Shift = null,
    string? ShiftGlyph = null,
    KeyAction? Alpha = null,
    string? AlphaGlyph = null)
{
    /// <summary>Returns what the key does in a mode.</summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The action, or <see langword="null"/> when the key does nothing in that mode.</returns>
    public KeyAction? In(KeyMode mode)
    {
        return mode switch
        {
            KeyMode.Shift => Shift,
            KeyMode.Alpha => Alpha,
            _ => Primary,
        };
    }

    /// <summary>Returns what is written on the key for a mode.</summary>
    /// <param name="mode">The mode.</param>
    /// <returns>The glyph, or <see langword="null"/> when the key does nothing in that mode.</returns>
    public string? GlyphIn(KeyMode mode)
    {
        return mode switch
        {
            KeyMode.Shift => ShiftGlyph,
            KeyMode.Alpha => AlphaGlyph,
            _ => Glyph,
        };
    }
}

/// <summary>
/// Which meaning of a key is reached: the key itself, or its second or third (manual p. 18).
/// </summary>
public enum KeyMode
{
    /// <summary>The key itself.</summary>
    Primary = 0,

    /// <summary>After Shift.</summary>
    Shift,

    /// <summary>After Alpha.</summary>
    Alpha,
}
