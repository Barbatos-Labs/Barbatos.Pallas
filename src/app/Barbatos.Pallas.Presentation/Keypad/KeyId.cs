// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// A key of the calculator: one per place on the keypad, whatever the key does in each of its shifts.
/// </summary>
/// <remarks>
/// The identifier is the place, not the meaning, because one key carries two or three of them (manual p. 18): the
/// key that types a sine types an inverse sine after Shift. A hardware key maps onto the same identifiers, so the
/// keyboard and the keypad cannot behave differently.
/// </remarks>
public enum KeyId
{
    /// <summary>Not a key.</summary>
    None = 0,

    /// <summary>The shift mode, which gives every key its second meaning.</summary>
    Shift,

    /// <summary>The alpha mode, which gives every key its variable.</summary>
    Alpha,

    /// <summary>Back to the home screen.</summary>
    Home,

    /// <summary>The settings screen.</summary>
    Settings,

    /// <summary>Move the cursor up, to the slot above.</summary>
    Up,

    /// <summary>Move the cursor down.</summary>
    Down,

    /// <summary>Move the cursor left.</summary>
    Left,

    /// <summary>Move the cursor right.</summary>
    Right,

    /// <summary>Delete what is before the cursor.</summary>
    Delete,

    /// <summary>Clear the input; Shift undoes instead.</summary>
    ClearAll,

    /// <summary>Calculate; Shift repeats the last answer.</summary>
    Execute,

    /// <summary>0, and the last answer after Shift.</summary>
    Zero,

    /// <summary>1, and the variable A.</summary>
    One,

    /// <summary>2, and the variable B.</summary>
    Two,

    /// <summary>3, and the variable C.</summary>
    Three,

    /// <summary>4, and the variable D.</summary>
    Four,

    /// <summary>5, and the variable E.</summary>
    Five,

    /// <summary>6, and the variable F.</summary>
    Six,

    /// <summary>7, and the variable x.</summary>
    Seven,

    /// <summary>8, and the variable y.</summary>
    Eight,

    /// <summary>9, and the variable z.</summary>
    Nine,

    /// <summary>The decimal mark.</summary>
    Point,

    /// <summary>×10ˣ, and π after Shift.</summary>
    Exponent,

    /// <summary>The sign of a number, (−).</summary>
    Negate,

    /// <summary>Plus.</summary>
    Add,

    /// <summary>Minus.</summary>
    Subtract,

    /// <summary>Times, and the permutation nPr after Shift.</summary>
    Multiply,

    /// <summary>Divide, and the combination nCr after Shift.</summary>
    Divide,

    /// <summary>An opening bracket, and the absolute value after Shift.</summary>
    OpenBracket,

    /// <summary>A closing bracket, and the list separator after Shift.</summary>
    CloseBracket,

    /// <summary>A fraction, and a mixed fraction after Shift.</summary>
    Fraction,

    /// <summary>The square, and the cube after Shift.</summary>
    Square,

    /// <summary>A power, and the reciprocal after Shift.</summary>
    Power,

    /// <summary>A square root, and a root of any index after Shift.</summary>
    SquareRoot,

    /// <summary>The logarithm to base ten, and to a base of its own after Shift.</summary>
    Log,

    /// <summary>The natural logarithm, and e after Shift.</summary>
    Ln,

    /// <summary>Sine, and its inverse after Shift.</summary>
    Sin,

    /// <summary>Cosine, and its inverse after Shift.</summary>
    Cos,

    /// <summary>Tangent, and its inverse after Shift.</summary>
    Tan,

    /// <summary>The last answer, and the one before it after Shift.</summary>
    Answer,

    /// <summary>The degree unit, and the factorial after Shift.</summary>
    Degree,

    /// <summary>The percent, and a random number after Shift.</summary>
    Percent,

    /// <summary>An integral, and a derivative after Shift.</summary>
    Integral,

    /// <summary>A sum, and a product after Shift.</summary>
    Sum,
}
