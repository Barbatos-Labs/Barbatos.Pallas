// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using WpfMath;
using WpfMath.Parsers;
using XamlMath;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// Draws LaTeX the way the application does, so a test can say whether the screen would show it.
/// </summary>
/// <remarks>
/// Parsing alone is not enough: an empty formula parses and then fails to draw. Each formula is therefore rendered,
/// which is what the application asks WpfMath for.
/// </remarks>
internal static class Formula
{
    /// <summary>Renders LaTeX, and throws what WpfMath throws when it cannot.</summary>
    /// <param name="latex">The formula.</param>
    public static void Render(string latex)
    {
        TexFormula formula = WpfTeXFormulaParser.Instance.Parse(latex);
        _ = formula.RenderToPng(20d, 0d, 0d, "Arial");
    }

    /// <summary>Returns what went wrong when WpfMath drew the formula, or <see langword="null"/>.</summary>
    /// <param name="latex">The formula.</param>
    /// <returns>The message, or <see langword="null"/> when it was drawn.</returns>
    public static string? Fault(string latex)
    {
        try
        {
            Render(latex);
            return null;
        }
        catch (Exception exception)
        {
            return exception.Message;
        }
    }
}
