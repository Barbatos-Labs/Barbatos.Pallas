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

    /// <summary>Turns the result between its exact form and its decimal (S⇔D, manual p. 42).</summary>
    SwapForm,

    // Base-N (manual pp. 51-56). The hexadecimal digits are keys of their own there, because the variables A to F
    // do not exist in Base-N (assumption U12).

    /// <summary>The hexadecimal digit A.</summary>
    HexA,

    /// <summary>The hexadecimal digit B.</summary>
    HexB,

    /// <summary>The hexadecimal digit C.</summary>
    HexC,

    /// <summary>The hexadecimal digit D.</summary>
    HexD,

    /// <summary>The hexadecimal digit E.</summary>
    HexE,

    /// <summary>The hexadecimal digit F.</summary>
    HexF,

    /// <summary>The logic operator <c>and</c>, and <c>or</c> after Shift.</summary>
    LogicAnd,

    /// <summary>The logic operator <c>xor</c>, and <c>xnor</c> after Shift.</summary>
    LogicXor,

    /// <summary>The one's complement <c>Not(</c>, and the two's complement <c>Neg(</c> after Shift.</summary>
    LogicNot,

    // Matrix and Vector (manual pp. 135-139).

    /// <summary>MatA, and MatAns after Shift.</summary>
    MatrixA,

    /// <summary>MatB.</summary>
    MatrixB,

    /// <summary>MatC.</summary>
    MatrixC,

    /// <summary>MatD.</summary>
    MatrixD,

    /// <summary>The determinant, and the transpose after Shift.</summary>
    Determinant,

    /// <summary>The identity matrix.</summary>
    Identity,

    /// <summary>VctA, and VctAns after Shift.</summary>
    VectorA,

    /// <summary>VctB.</summary>
    VectorB,

    /// <summary>VctC.</summary>
    VectorC,

    /// <summary>VctD.</summary>
    VectorD,

    /// <summary>The dot product.</summary>
    DotProduct,

    /// <summary>The angle between two vectors, and the unit vector after Shift.</summary>
    VectorAngle,

    // Complex (manual pp. 129-134).

    /// <summary>The imaginary unit.</summary>
    Imaginary,

    /// <summary>The polar mark ∠, with the real and imaginary parts on its other meanings.</summary>
    Polar,

    /// <summary>The conjugate, and the argument after Shift.</summary>
    Conjugate,

    // Statistics (manual pp. 90-95). The rest of the statistic variables are reached through the CATALOG.

    /// <summary>The number of data, with the sums of x and y on its other meanings.</summary>
    DataCount,

    /// <summary>The mean of x, with σx and sx on its other meanings.</summary>
    MeanX,

    /// <summary>The mean of y, with σy and sy on its other meanings.</summary>
    MeanY,

    /// <summary>The estimate x̂, with ŷ and ▶t on its other meanings.</summary>
    EstimateX,

    /// <summary>The normal probability P(, with Q( and R( on its other meanings.</summary>
    Probability,
}
