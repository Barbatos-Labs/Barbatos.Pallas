// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The hardware keyboard, mapped onto the keys of the calculator.
/// </summary>
/// <remarks>
/// A keyboard key is named the way the host names it - <c>D7</c>, <c>NumPad7</c>, <c>OemPlus</c>, <c>Enter</c> -
/// which is a string here so that the presentation layer needs no UI framework to hold the table. The keyboard
/// reaches the same <see cref="KeyId"/> values as the keypad, so a calculation typed on either is the same
/// calculation.
/// </remarks>
public static class KeyboardMap
{
    private static readonly FrozenDictionary<string, KeyId> Plain = new Dictionary<string, KeyId>(StringComparer.OrdinalIgnoreCase)
    {
        ["D0"] = KeyId.Zero,
        ["D1"] = KeyId.One,
        ["D2"] = KeyId.Two,
        ["D3"] = KeyId.Three,
        ["D4"] = KeyId.Four,
        ["D5"] = KeyId.Five,
        ["D6"] = KeyId.Six,
        ["D7"] = KeyId.Seven,
        ["D8"] = KeyId.Eight,
        ["D9"] = KeyId.Nine,
        ["NumPad0"] = KeyId.Zero,
        ["NumPad1"] = KeyId.One,
        ["NumPad2"] = KeyId.Two,
        ["NumPad3"] = KeyId.Three,
        ["NumPad4"] = KeyId.Four,
        ["NumPad5"] = KeyId.Five,
        ["NumPad6"] = KeyId.Six,
        ["NumPad7"] = KeyId.Seven,
        ["NumPad8"] = KeyId.Eight,
        ["NumPad9"] = KeyId.Nine,
        ["Decimal"] = KeyId.Point,
        ["OemPeriod"] = KeyId.Point,
        ["Add"] = KeyId.Add,
        ["OemPlus"] = KeyId.Add,
        ["Subtract"] = KeyId.Subtract,
        ["OemMinus"] = KeyId.Subtract,
        ["Multiply"] = KeyId.Multiply,
        ["Divide"] = KeyId.Divide,
        ["OemQuestion"] = KeyId.Divide,
        ["Enter"] = KeyId.Execute,
        ["Return"] = KeyId.Execute,
        ["Back"] = KeyId.Delete,
        ["Delete"] = KeyId.Delete,
        ["Escape"] = KeyId.ClearAll,
        ["Left"] = KeyId.Left,
        ["Right"] = KeyId.Right,
        ["Up"] = KeyId.Up,
        ["Down"] = KeyId.Down,
        ["Home"] = KeyId.Home,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, KeyId> Shifted = new Dictionary<string, KeyId>(StringComparer.OrdinalIgnoreCase)
    {
        // The characters a shifted keyboard produces, where the calculator has a key of its own for them.
        ["D8"] = KeyId.Multiply,
        ["D9"] = KeyId.OpenBracket,
        ["D0"] = KeyId.CloseBracket,
        ["D5"] = KeyId.Percent,
        ["D6"] = KeyId.Power,
        ["OemPlus"] = KeyId.Add,
        ["OemQuestion"] = KeyId.Fraction,
        ["D1"] = KeyId.Degree,
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the key a keyboard key stands for.</summary>
    /// <param name="key">The name the host gives the keyboard key.</param>
    /// <param name="shift">Whether a shift key was held.</param>
    /// <returns>The key, or <see cref="KeyId.None"/> when the keyboard key is not one of the calculator's.</returns>
    public static KeyId Find(string? key, bool shift = false)
    {
        if (key is null)
        {
            return KeyId.None;
        }

        if (shift && Shifted.TryGetValue(key, out KeyId shifted))
        {
            return shifted;
        }

        return Plain.GetValueOrDefault(key, KeyId.None);
    }
}
