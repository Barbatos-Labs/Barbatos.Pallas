// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// An operator written after its operand.
/// </summary>
public enum PostfixOperator
{
    /// <summary><c>x²</c>, level 3.</summary>
    Square = 0,

    /// <summary><c>x³</c>, level 3.</summary>
    Cube = 1,

    /// <summary><c>x⁻¹</c>, level 3.</summary>
    Reciprocal = 2,

    /// <summary><c>x!</c>, level 3.</summary>
    Factorial = 3,

    /// <summary><c>x%</c>, level 3.</summary>
    Percent = 4,

    /// <summary><c>x°</c>, an angle in degrees, level 3. Followed by minutes or seconds it is a <see cref="SexagesimalExpression"/> instead.</summary>
    Degrees = 5,

    /// <summary><c>xʳ</c>, an angle in radians, level 3.</summary>
    Radians = 6,

    /// <summary><c>xᵍ</c>, an angle in gradians, level 3.</summary>
    Gradians = 7,

    /// <summary><c>x▶t</c>, the standardized variate in Statistics, level 3.</summary>
    StandardizedVariate = 8,

    /// <summary><c>yx̂</c>, the regression estimate of x, level 6.</summary>
    EstimateX = 9,

    /// <summary><c>xŷ</c>, the regression estimate of y, level 6.</summary>
    EstimateY = 10,

    /// <summary><c>yx̂₁</c>, the first quadratic-regression estimate of x, level 6.</summary>
    EstimateX1 = 11,

    /// <summary><c>yx̂₂</c>, the second quadratic-regression estimate of x, level 6.</summary>
    EstimateX2 = 12,
}
