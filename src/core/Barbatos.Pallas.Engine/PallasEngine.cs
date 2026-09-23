// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The calculation engine: an immutable catalog of functions and data from which calculator sessions are created.
/// </summary>
/// <example>
/// <code>
/// PallasEngine engine = PallasEngineBuilder.CreateDefault().Build();
/// CalculatorSession session = engine.CreateSession(CalculatorApp.Calculate);
/// Calculation result = session.Calculate("2⌟3+1⌟1⌟2");   // result.Display.Text is "13⌟6"
/// </code>
/// </example>
public sealed class PallasEngine
{
    internal PallasEngine(EngineCatalog catalog, EngineBudget budget)
    {
        Catalog = catalog;
        Budget = budget;
    }

    /// <summary>Gets the vocabulary: the reference calculator's names and the plugin functions.</summary>
    public SyntaxVocabulary Vocabulary => Catalog.Vocabulary;

    /// <summary>Gets the default budget of a calculation.</summary>
    public EngineBudget Budget { get; }

    internal EngineCatalog Catalog { get; }

    /// <summary>Creates a calculator session: its own memory, history and settings.</summary>
    /// <param name="app">The application; every one of the thirteen has its engine.</param>
    /// <param name="profile">The profile.</param>
    /// <param name="randomSeed">A seed for Ran# and RanInt#, to reproduce a sequence; <see langword="null"/> for an unpredictable one.</param>
    /// <returns>The session.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="app"/> is not a defined value.</exception>
    public CalculatorSession CreateSession(CalculatorApp app = CalculatorApp.Calculate, CalculatorProfile profile = CalculatorProfile.Standard, int? randomSeed = null)
    {
        return new CalculatorSession(this, app, profile, randomSeed);
    }

    /// <summary>Displays a value, such as a variable in the variable list.</summary>
    /// <param name="value">The value.</param>
    /// <param name="settings">The settings to display it with; a Base-N value uses their number mode.</param>
    /// <param name="profile">The profile, which decides the display bounds.</param>
    /// <param name="target">A FORMAT conversion, or <see langword="null"/> for the display of the settings.</param>
    /// <returns>The display, or <see langword="null"/> when the conversion does not apply to the value.</returns>
    public static FormattedResult? Format(Value value, CalculatorSettings settings, CalculatorProfile profile = CalculatorProfile.Standard, FormatTarget? target = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        CalculatorApp app = value.Kind switch
        {
            ValueKind.Complex => CalculatorApp.Complex,
            ValueKind.BaseN => CalculatorApp.BaseN,
            _ => CalculatorApp.Calculate,
        };

        string? text = ResultFormatter.FormatValue(value, app, settings, profile, target);
        return text is null ? null : new FormattedResult(text, ResultFormatter.Latex(text, value, app, settings));
    }
}
