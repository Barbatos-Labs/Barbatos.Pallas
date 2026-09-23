// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The structures the math input can hold: what the calculator draws as more than a line of characters
/// (manual pp. 26-31).
/// </summary>
/// <remarks>
/// Every one of them is a template with slots the user moves between, which is what makes the input structural
/// rather than a string: a fraction has a numerator and a denominator, not a <c>⌟</c> somewhere in the text.
/// </remarks>
public enum MathTemplateKind
{
    /// <summary>( x ).</summary>
    Parentheses = 0,

    /// <summary>A fraction: numerator over denominator.</summary>
    Fraction = 1,

    /// <summary>A mixed fraction: a whole part, a numerator and a denominator.</summary>
    MixedFraction = 2,

    /// <summary>√x.</summary>
    SquareRoot = 3,

    /// <summary>ⁿ√x: an index and a radicand.</summary>
    Root = 4,

    /// <summary>x^n: a base and an exponent.</summary>
    Power = 5,

    /// <summary>|x|.</summary>
    Abs = 6,

    /// <summary>logₐb: a base and an argument.</summary>
    LogBase = 7,

    /// <summary>∫ f dx from a to b.</summary>
    Integral = 8,

    /// <summary>Σ f from x = a to b.</summary>
    Sum = 9,

    /// <summary>Π f from x = a to b.</summary>
    Product = 10,

    /// <summary>d/dx f at x = a.</summary>
    Derivative = 11,
}
