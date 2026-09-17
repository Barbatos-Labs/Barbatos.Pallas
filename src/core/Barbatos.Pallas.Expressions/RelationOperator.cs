// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A relational operator in a Verify expression. Relations bind looser than every other operator.
/// </summary>
public enum RelationOperator
{
    /// <summary><c>=</c>.</summary>
    Equal = 0,

    /// <summary><c>≠</c>.</summary>
    NotEqual = 1,

    /// <summary><c>&lt;</c>.</summary>
    Less = 2,

    /// <summary><c>&gt;</c>.</summary>
    Greater = 3,

    /// <summary><c>≤</c>, also written <c>&lt;=</c>.</summary>
    LessOrEqual = 4,

    /// <summary><c>≥</c>, also written <c>&gt;=</c>.</summary>
    GreaterOrEqual = 5,
}
