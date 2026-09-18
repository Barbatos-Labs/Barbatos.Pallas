// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The built-in operations of a bound expression, each evaluated by <see cref="Operations.Evaluate"/>.
/// </summary>
internal enum Operation
{
    // Arithmetic
    Negate,
    Add,
    Subtract,
    Multiply,
    Divide,
    Power,
    Root,
    Square,
    Cube,
    Reciprocal,
    Percent,
    MixedFraction,

    /// <summary>÷R inside a larger expression: only the quotient passes on (assumption U13).</summary>
    RemainderQuotient,

    // Probability and numeric functions
    Factorial,
    Permutation,
    Combination,
    Random,
    RandomInteger,
    GreatestCommonDivisor,
    LeastCommonMultiple,
    Absolute,
    IntegerPart,
    LargestInteger,
    Round,

    // Powers, roots, logarithms
    SquareRoot,
    Exp,
    Log10,
    LogBase,
    Ln,

    // Angles and trigonometry
    DegreesAngle,
    RadiansAngle,
    GradiansAngle,
    Sexagesimal,
    Sin,
    Cos,
    Tan,
    Asin,
    Acos,
    Atan,
    Sinh,
    Cosh,
    Tanh,
    Asinh,
    Acosh,
    Atanh,

    // Complex
    Polar,
    Conjugate,
    Argument,
    RealPart,
    ImaginaryPart,

    // Base-N
    And,
    Or,
    Xor,
    Xnor,
    Not,
    Neg,

    // Data
    AtomicWeight,

    // Derivative helpers: 0 where the operand is not at a jump of Int, Intg or Rnd, Math ERROR at a jump.
    IntegerPartSlope,
    LargestIntegerSlope,
    RoundSlope,
}
