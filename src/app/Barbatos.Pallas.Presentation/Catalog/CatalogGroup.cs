// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The groups of the CATALOG (manual pp. 51-69), in the order the calculator lists them.
/// </summary>
/// <remarks>
/// The first group is the application's own: the names only it has, such as the statistic variables of Statistics
/// or the logic operators of Base-N, which the calculator puts first in that application's CATALOG.
/// </remarks>
public enum CatalogGroup
{
    /// <summary>The names only the current application has.</summary>
    Application = 0,

    /// <summary>Derivatives, integrals, sums, products, logarithms and ÷R.</summary>
    FunctionAnalysis,

    /// <summary>Percent, factorial, permutations, combinations and the random numbers.</summary>
    Probability,

    /// <summary>GCD, LCM, absolute value, the integer parts and rounding.</summary>
    NumericCalc,

    /// <summary>The angle units, polar and rectangular coordinates, and degrees, minutes and seconds.</summary>
    AngleCoordinates,

    /// <summary>The trigonometric and hyperbolic functions and their inverses.</summary>
    Trigonometry,

    /// <summary>The engineering symbols, from femto to exa.</summary>
    EngineerSymbols,

    /// <summary>The scientific constants of the CODATA set the engine ships.</summary>
    ScientificConstants,

    /// <summary>The unit conversions of NIST SP 811.</summary>
    UnitConversions,

    /// <summary>The atomic weights.</summary>
    AtomicTable,

    /// <summary>The relations, which the line takes while Verify is on (p. 73).</summary>
    Relations,

    /// <summary>What is left: π, e, roots, powers and the defined functions.</summary>
    Other,
}
