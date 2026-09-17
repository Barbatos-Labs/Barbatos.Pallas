// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// What a <see cref="SyntaxSymbol"/> is.
/// </summary>
/// <remarks>
/// The kinds up to <see cref="UnitConversion"/> are names, which a vocabulary may add (see
/// <see cref="SyntaxVocabulary.With"/>). The remaining kinds are the fixed grammar.
/// </remarks>
public enum SymbolKind
{
    /// <summary>A function written with parentheses; its text ends with <c>(</c>, as in <c>sin(</c>.</summary>
    Function = 0,

    /// <summary>A mathematical constant or nullary command: <c>π</c>, <c>e</c>, <c>i</c>, <c>Ran#</c>.</summary>
    Constant = 1,

    /// <summary>A scientific constant, written with <c>@</c>: <c>@h</c>, <c>@N_A</c>.</summary>
    ScientificConstant = 2,

    /// <summary>A variable: <c>A</c>-<c>F</c>, <c>x</c>, <c>y</c>, <c>z</c>.</summary>
    Variable = 3,

    /// <summary>An answer memory: <c>Ans</c>, <c>PreAns</c>.</summary>
    Memory = 4,

    /// <summary>A matrix variable: <c>MatA</c>-<c>MatD</c>, <c>MatAns</c>.</summary>
    MatrixVariable = 5,

    /// <summary>A vector variable: <c>VctA</c>-<c>VctD</c>, <c>VctAns</c>.</summary>
    VectorVariable = 6,

    /// <summary>A statistic value: <c>x̄</c>, <c>σx</c>, <c>Σx²</c>, <c>n</c>.</summary>
    StatisticsVariable = 7,

    /// <summary>An engineering symbol written after a value, with an underscore: <c>_k</c>, <c>_μ</c>.</summary>
    EngineeringSymbol = 8,

    /// <summary>A unit conversion command written after a value: <c>cm▶in</c>.</summary>
    UnitConversion = 9,

    /// <summary>An operator between two operands: <c>+</c>, <c>×</c>, <c>⌟</c>, <c>P</c>.</summary>
    BinaryOperator = 10,

    /// <summary>An operator after its operand: <c>²</c>, <c>!</c>, <c>°</c>.</summary>
    PostfixOperator = 11,

    /// <summary>A relational operator: <c>=</c>, <c>≤</c>.</summary>
    RelationOperator = 12,

    /// <summary>A minute or second mark in degrees-minutes-seconds: <c>′</c>, <c>″</c>.</summary>
    SexagesimalMark = 13,

    /// <summary><c>(</c>.</summary>
    OpenParenthesis = 14,

    /// <summary><c>)</c>.</summary>
    CloseParenthesis = 15,

    /// <summary><c>,</c>, between function arguments.</summary>
    Comma = 16,

    /// <summary><c>:</c>, in a Spreadsheet cell range.</summary>
    Colon = 17,
}
