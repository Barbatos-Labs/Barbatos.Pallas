// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests.Support;

/// <summary>
/// Short ways to run one calculation, so a test reads like the manual's examples.
/// </summary>
internal static class Calculator
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().Build();

    /// <summary>Creates a session with the settings a test needs.</summary>
    public static CalculatorSession Session(
        CalculatorApp app = CalculatorApp.Calculate,
        CalculatorProfile profile = CalculatorProfile.Standard,
        Func<CalculatorSettings, CalculatorSettings>? settings = null,
        int? randomSeed = 880)
    {
        CalculatorSession session = Engine.CreateSession(app, profile, randomSeed);
        if (settings is not null)
        {
            session.Settings = settings(session.Settings);
        }

        return session;
    }

    /// <summary>Returns the display of one calculation.</summary>
    public static string Display(string input, CalculatorApp app = CalculatorApp.Calculate, Func<CalculatorSettings, CalculatorSettings>? settings = null)
    {
        Calculation calculation = Session(app, settings: settings).Calculate(input);
        calculation.Error.Should().BeNull("'{0}' should compute", input);
        return calculation.Display.Text;
    }

    /// <summary>Returns the value of one calculation.</summary>
    public static Value Evaluate(string input, CalculatorApp app = CalculatorApp.Calculate, Func<CalculatorSettings, CalculatorSettings>? settings = null)
    {
        Calculation calculation = Session(app, settings: settings).Calculate(input);
        calculation.Error.Should().BeNull("'{0}' should compute", input);
        return calculation.Result;
    }

    /// <summary>Returns the error of one calculation.</summary>
    public static CalcError Error(string input, CalculatorApp app = CalculatorApp.Calculate, Func<CalculatorSettings, CalculatorSettings>? settings = null)
    {
        Calculation calculation = Session(app, settings: settings).Calculate(input);
        calculation.Error.Should().NotBeNull("'{0}' should fail", input);
        return calculation.Error!.Value;
    }
}
