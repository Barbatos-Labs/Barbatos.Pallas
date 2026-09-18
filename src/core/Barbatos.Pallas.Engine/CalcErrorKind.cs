// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The errors of the reference calculator (manual pp. 162-166), without their text.
/// </summary>
/// <remarks>
/// The engine is language-neutral: an application turns a kind into English or Vietnamese text. Every error also carries
/// the span where the calculator would place the cursor (<see cref="CalcError"/>).
/// </remarks>
public enum CalcErrorKind
{
    /// <summary>Syntax ERROR: the expression is malformed, or a function has the wrong number of arguments.</summary>
    SyntaxError = 0,

    /// <summary>Math ERROR: a result outside the calculation range, an input outside a function's domain, or division by zero.</summary>
    MathError = 1,

    /// <summary>Stack ERROR: the expression nests deeper than the calculator's stacks.</summary>
    StackError = 2,

    /// <summary>Argument ERROR: an argument of the wrong kind.</summary>
    ArgumentError = 3,

    /// <summary>Dimension ERROR: incompatible matrix or vector sizes.</summary>
    DimensionError = 4,

    /// <summary>Variable ERROR: a Solver expression without a variable.</summary>
    VariableError = 5,

    /// <summary>Cannot Solve: the Solver did not converge.</summary>
    CannotSolve = 6,

    /// <summary>Range ERROR: a table, fill range or Math Box input out of range.</summary>
    RangeError = 7,

    /// <summary>Time Out: a derivative, integral or other iteration did not meet its end condition within the budget.</summary>
    TimeOut = 8,

    /// <summary>Circular ERROR: f(x) and g(x), or spreadsheet cells, refer to each other.</summary>
    CircularError = 9,

    /// <summary>Memory ERROR: the spreadsheet's capacity is exceeded.</summary>
    MemoryError = 10,

    /// <summary>No Operator: Verify on an expression without a relational operator.</summary>
    NoOperator = 11,

    /// <summary>Not Defined: f(x), g(x), a matrix, a vector or a data set used before it is defined.</summary>
    NotDefined = 12,
}
