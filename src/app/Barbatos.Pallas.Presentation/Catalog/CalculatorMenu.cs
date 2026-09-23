// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Which menu is on the display, if any.
/// </summary>
/// <remarks>
/// The calculator shows its menus on its own display, over the calculation, rather than in a window of their own:
/// what is chosen goes onto the line that is being typed, so the menu belongs to that line.
/// </remarks>
public enum CalculatorMenu
{
    /// <summary>No menu: the display shows the calculation.</summary>
    None = 0,

    /// <summary>The CATALOG (pp. 51-69).</summary>
    Catalog,

    /// <summary>The FORMAT conversions of the result (pp. 42-50).</summary>
    Format,

    /// <summary>The variables and what they hold (RCL).</summary>
    Recall,
}

/// <summary>
/// One variable of the list RCL shows.
/// </summary>
/// <param name="Name">The variable as the line spells it: <c>A</c> or <c>x</c>.</param>
/// <param name="Text">What it holds, as the calculator shows it.</param>
public sealed record VariableLine(string Name, string Text);
