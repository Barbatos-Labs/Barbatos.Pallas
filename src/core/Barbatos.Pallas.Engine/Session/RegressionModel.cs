// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The regression types of two-variable statistics (manual pp. 86-87), fitted by least squares (pp. 93-95).</summary>
public enum RegressionModel
{
    /// <summary>y = a + bx.</summary>
    Linear = 0,

    /// <summary>y = a + bx + cx².</summary>
    Quadratic = 1,

    /// <summary>y = a + b·ln(x), fitted to ln x.</summary>
    Logarithmic = 2,

    /// <summary>y = a·e^(bx), fitted to ln y.</summary>
    ExponentialE = 3,

    /// <summary>y = a·b^x, fitted to ln y.</summary>
    ExponentialAB = 4,

    /// <summary>y = a·x^b, fitted to ln x and ln y.</summary>
    Power = 5,

    /// <summary>y = a + b/x, fitted to 1/x.</summary>
    Inverse = 6,
}
