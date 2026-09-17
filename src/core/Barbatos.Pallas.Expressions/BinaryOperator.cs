// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// An operator between two operands, with its priority level on the reference calculator (manual p. 168; 1 binds tightest).
/// </summary>
public enum BinaryOperator
{
    /// <summary><c>a+b</c>, level 11.</summary>
    Add = 0,

    /// <summary><c>a-b</c>, level 11.</summary>
    Subtract = 1,

    /// <summary><c>a×b</c>, level 10.</summary>
    Multiply = 2,

    /// <summary><c>a÷b</c>, level 10.</summary>
    Divide = 3,

    /// <summary><c>a÷Rb</c>, division with remainder, level 10.</summary>
    DivideWithRemainder = 4,

    /// <summary><c>ab</c>, multiplication with the sign omitted, level 7: <c>6÷2π</c> is <c>6÷(2π)</c>.</summary>
    ImplicitMultiply = 5,

    /// <summary><c>a^b</c>, level 3.</summary>
    Power = 6,

    /// <summary><c>nˣ√(x)</c>, the n-th root of x, level 3. The left operand is the index.</summary>
    Root = 7,

    /// <summary><c>a⌟b</c>, level 4.</summary>
    Fraction = 8,

    /// <summary><c>nPr</c>, level 8.</summary>
    Permutation = 9,

    /// <summary><c>nCr</c>, level 8.</summary>
    Combination = 10,

    /// <summary><c>r∠θ</c>, a complex number in polar form, level 8.</summary>
    Polar = 11,

    /// <summary><c>a•b</c>, the dot product, level 9.</summary>
    DotProduct = 12,

    /// <summary><c>a and b</c>, level 12.</summary>
    And = 13,

    /// <summary><c>a or b</c>, level 13.</summary>
    Or = 14,

    /// <summary><c>a xor b</c>, level 13.</summary>
    Xor = 15,

    /// <summary><c>a xnor b</c>, level 13.</summary>
    Xnor = 16,
}
