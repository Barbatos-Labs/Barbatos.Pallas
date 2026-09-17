// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Why an expression could not be read.
/// </summary>
/// <remarks>
/// Every code is a Syntax ERROR on the calculator except <see cref="NestingTooDeep"/>, which is a Stack ERROR. The engine
/// maps codes to its language-neutral error kinds; the app turns those into text.
/// </remarks>
public enum SyntaxErrorCode
{
    /// <summary>The text is empty or white space.</summary>
    EmptyExpression = 0,

    /// <summary>A character starts no token.</summary>
    UnexpectedCharacter = 1,

    /// <summary>An operand is missing: <c>2+</c>, <c>×3</c>, <c>sin()</c>.</summary>
    MissingOperand = 2,

    /// <summary>A token cannot appear here: a comma outside a function, <c>P</c> without a left operand.</summary>
    UnexpectedToken = 3,

    /// <summary>A <c>)</c> has no matching <c>(</c>.</summary>
    UnmatchedClosingParenthesis = 4,

    /// <summary>Two numbers stand side by side, <c>2 3</c>; implicit multiplication needs something between them.</summary>
    AdjacentNumbers = 5,

    /// <summary>A relational operator appears while relations are not allowed (Verify is off).</summary>
    RelationNotAllowed = 6,

    /// <summary>Relations point both ways, <c>5≤6≥4</c> (manual p. 75).</summary>
    MixedRelationDirections = 7,

    /// <summary><c>≠</c> is combined with <c>&lt; &gt; ≤ ≥</c>, <c>4&lt;6≠8</c> (manual p. 75; assumption U10).</summary>
    NotEqualWithInequality = 8,

    /// <summary>More than three parts separated by <c>⌟</c>.</summary>
    TooManyFractionParts = 9,

    /// <summary>A minute or second mark <c>′ ″</c> without degrees before it.</summary>
    MisplacedSexagesimalMark = 10,

    /// <summary><c>root(</c> does not have exactly two arguments, index and radicand.</summary>
    InvalidRootArguments = 11,

    /// <summary>Parentheses, functions or signs are nested deeper than <see cref="ExpressionParser.MaxNestingDepth"/>: a Stack ERROR.</summary>
    NestingTooDeep = 12,
}
