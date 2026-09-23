// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// What kind of key a key is: the calculator groups them by colour, and so does the screen.
/// </summary>
/// <remarks>
/// The group is part of the keypad table rather than of the host's styling, for the same reason the table itself is
/// C#: a key belongs to a group the way it belongs to a row, and a host that draws the keypad should not have to
/// keep a second list of which key is a digit.
/// </remarks>
public enum KeyGroup
{
    /// <summary>A function, a bracket or a template: what a calculation is built from.</summary>
    Function = 0,

    /// <summary>A digit, the point, or the sign of a number.</summary>
    Digit,

    /// <summary>The four operations.</summary>
    Operator,

    /// <summary>Shift and Alpha: the keys that give the next key another meaning (p. 18).</summary>
    Modifier,

    /// <summary>The cursor keys, and the keys that leave the screen.</summary>
    Control,

    /// <summary>Delete and AC: the keys that take something away.</summary>
    Clear,

    /// <summary>The key that calculates.</summary>
    Execute,
}
