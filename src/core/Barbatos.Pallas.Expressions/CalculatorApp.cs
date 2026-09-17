// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The calculator application an expression is entered in.
/// </summary>
/// <remarks>
/// The application changes how text is read. In Base-N, <c>b10</c> is binary 10 and <c>A</c>-<c>F</c> are hexadecimal
/// digits; in Calculate they are variables. In Spreadsheet, <c>A1</c> is a cell. The names match the reference calculator
/// conformance data.
/// </remarks>
public enum CalculatorApp
{
    /// <summary>Calculate: arithmetic, functions, CALC, Verify.</summary>
    Calculate = 0,

    /// <summary>Statistics: statistic variables, regression estimates, normal distribution functions.</summary>
    Statistics = 1,

    /// <summary>Distribution.</summary>
    Distribution = 2,

    /// <summary>Spreadsheet: cell references and ranges.</summary>
    Spreadsheet = 3,

    /// <summary>Table.</summary>
    Table = 4,

    /// <summary>Equation, including the Solver.</summary>
    Equation = 5,

    /// <summary>Inequality.</summary>
    Inequality = 6,

    /// <summary>Complex: the imaginary unit and polar form.</summary>
    Complex = 7,

    /// <summary>Base-N: hexadecimal digits, base prefixes and logic operators.</summary>
    BaseN = 8,

    /// <summary>Matrix.</summary>
    Matrix = 9,

    /// <summary>Vector.</summary>
    Vector = 10,

    /// <summary>Ratio.</summary>
    Ratio = 11,

    /// <summary>Math Box.</summary>
    MathBox = 12,
}
