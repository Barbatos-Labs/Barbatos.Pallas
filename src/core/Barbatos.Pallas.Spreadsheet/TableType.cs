// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Spreadsheet;

/// <summary>
/// Which columns a number table has (manual p. 109), which also decides how many rows it may have.
/// </summary>
public enum TableType
{
    /// <summary>f(x) and g(x), the initial setting: up to 30 rows.</summary>
    FunctionsFAndG = 0,

    /// <summary>f(x) alone: up to 45 rows.</summary>
    FunctionF = 1,

    /// <summary>g(x) alone: up to 45 rows.</summary>
    FunctionG = 2,
}

/// <summary>
/// One of the two functions a number table shows (manual p. 109).
/// </summary>
public enum TableFunction
{
    /// <summary>f(x).</summary>
    F = 0,

    /// <summary>g(x).</summary>
    G = 1,
}
