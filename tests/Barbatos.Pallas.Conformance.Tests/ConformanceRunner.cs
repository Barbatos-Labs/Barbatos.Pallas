// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text.Json;
using Barbatos.Pallas.Data;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Runs one conformance case against a calculator session and checks every expectation it carries.
/// </summary>
/// <remarks>
/// An expectation member the runner does not know fails the case: a case must never pass because part of what it
/// expects was ignored.
/// </remarks>
internal static class ConformanceRunner
{
    // Property cases evaluate a random function this many times.
    private const int PropertySamples = 2000;

    private static readonly Lazy<PallasEngine> Engine = new(() => PallasEngineBuilder.CreateDefault()
        .AddConstantSet(ConstantSets.Codata2022)
        .AddUnitSet(UnitSets.NistSp811)
        .AddAtomicWeights(AtomicWeightTables.Ciaaw)
        .Build());

    public static void Run(ConformanceCase conformanceCase)
    {
        CalculatorSession session = Engine.Value.CreateSession(
            Enum.Parse<CalculatorApp>(conformanceCase.App),
            Enum.Parse<CalculatorProfile>(conformanceCase.Profile),
            randomSeed: 880);
        session.Settings = SettingsOf(conformanceCase.Settings);

        if (conformanceCase.Given is { ValueKind: JsonValueKind.Object } given)
        {
            foreach ((string name, DefinedFunction function) in (ReadOnlySpan<(string, DefinedFunction)>)[("f", DefinedFunction.F), ("g", DefinedFunction.G)])
            {
                if (given.TryGetProperty(name, out JsonElement body))
                {
                    session.Define(function, body.GetString()!).Should().BeNull("{0}: {1}(x) = {2} must define", conformanceCase, name, body.GetString());
                }
            }
        }

        switch (conformanceCase.Kind)
        {
            case "expression":
                Check(conformanceCase, session, session.Calculate(conformanceCase.Input!), conformanceCase.Expect!.Value);
                break;
            case "calc":
                Dictionary<MemoryVariable, Value> values = [];
                foreach (JsonProperty value in conformanceCase.Given!.Value.GetProperty("calcValues").EnumerateObject())
                {
                    values[Variable(value.Name)] = Parse(value.Value.GetString()!);
                }

                Check(conformanceCase, session, session.Calculate(conformanceCase.Input!, values), conformanceCase.Expect!.Value);
                break;
            case "sequence":
                RunSequence(conformanceCase, session);
                break;
            case "property":
                CheckProperty(conformanceCase, session);
                break;
            default:
                Assert.Fail($"{conformanceCase}: the runner does not know the kind '{conformanceCase.Kind}'.");
                break;
        }
    }

    private static void RunSequence(ConformanceCase conformanceCase, CalculatorSession session)
    {
        Calculation? last = null;
        foreach (ConformanceStep step in conformanceCase.Steps!)
        {
            if (step.Input is not null)
            {
                last = session.Calculate(step.Input);
            }

            if (step.Store is not null)
            {
                session.Store(Variable(step.Store));
            }

            if (step.SwitchBaseMode is not null)
            {
                session.Settings = session.Settings with { BaseMode = BaseModeOf(step.SwitchBaseMode) };
            }

            last.Should().NotBeNull("{0}: a step needs a calculation before it", conformanceCase);
            Check(conformanceCase, session, last!, step.Expect!.Value);
        }
    }

    private static void Check(ConformanceCase conformanceCase, CalculatorSession session, Calculation calculation, JsonElement expect)
    {
        if (expect.TryGetProperty("error", out JsonElement error))
        {
            calculation.Error.Should().NotBeNull("{0}: '{1}' should fail with {2}", conformanceCase, calculation.Input, error.GetString());
            calculation.Error!.Value.Kind.ToString().Should().Be(error.GetString(), "{0}: '{1}'", conformanceCase, calculation.Input);
        }
        else
        {
            calculation.Error.Should().BeNull("{0}: '{1}' should succeed", conformanceCase, calculation.Input);
        }

        foreach (JsonProperty member in expect.EnumerateObject())
        {
            string expected = member.Value.ValueKind == JsonValueKind.String ? member.Value.GetString()! : member.Value.ToString();
            string because = $"{conformanceCase}: {member.Name} of '{calculation.Input}'";
            switch (member.Name)
            {
                case "error":
                case "equivalentTo":
                    // Checked above, and by ConformanceSyntaxTests.
                    break;
                case "result":
                    // The session's current settings: a Base-N step may have switched the number mode since.
                    session.Format(calculation)!.Text.Should().Be(expected, because);
                    break;
                case "verify":
                    calculation.IsTrue.Should().Be(expected == "True", because);
                    break;
                case "variables":
                    foreach (JsonProperty variable in member.Value.EnumerateObject())
                    {
                        Value value = variable.Name == "Ans" ? session.Ans : session.GetVariable(Variable(variable.Name));
                        VariableText(value, session).Should().Be(variable.Value.GetString(), "{0}: variable {1}", because, variable.Name);
                    }

                    break;
                case "quotient":
                    VariableText(calculation.Result, session).Should().Be(expected, because);
                    break;
                case "remainder":
                    VariableText(calculation.Second!.Value, session).Should().Be(expected, because);
                    break;
                case "value":
                    VariableText(calculation.Result, session).Should().Be(expected, because);
                    break;
                case "decimalValue":
                    calculation.Result.ToInt32().ToString(CultureInfo.InvariantCulture).Should().Be(expected, because);
                    break;
                case "engShiftRight":
                    session.FormatEngineering(calculation, shift: -1).Should().Be(expected, because);
                    break;
                default:
                    FormatTarget target = TargetOf(member.Name, because);
                    session.Format(calculation, target)?.Text.Should().Be(expected, because);
                    session.Format(calculation, target).Should().NotBeNull(because);
                    break;
            }
        }
    }

    private static void CheckProperty(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement expect = conformanceCase.Expect!.Value;
        decimal low = decimal.Parse(expect.GetProperty("range")[0].GetString()!, CultureInfo.InvariantCulture);
        decimal high = decimal.Parse(expect.GetProperty("range")[1].GetString()!, CultureInfo.InvariantCulture);
        decimal step = decimal.Parse(expect.GetProperty("step").GetString()!, CultureInfo.InvariantCulture);
        HashSet<decimal> seen = [];
        for (int i = 0; i < PropertySamples; i++)
        {
            Calculation calculation = session.Calculate(conformanceCase.Input!);
            calculation.Error.Should().BeNull("{0}", conformanceCase);
            decimal value = calculation.Result.ToDecimal();
            value.Should().BeInRange(low, high, "{0}", conformanceCase);
            (value / step).Should().Be(decimal.Truncate(value / step), "{0}: values are multiples of {1}", conformanceCase, step);
            seen.Add(value);
        }

        // Every value of a small range turns up; a large one is at least well spread.
        long count = (long)((high - low) / step) + 1;
        seen.Count.Should().BeGreaterThanOrEqualTo((int)Math.Min(count, PropertySamples / 4), "{0}: the values must vary", conformanceCase);
    }

    /// <summary>The variable list displays in Norm 1 (p. 38).</summary>
    private static string VariableText(Value value, CalculatorSession session)
    {
        if (value.Kind == ValueKind.BaseN)
        {
            return value.ToInt32().ToString(CultureInfo.InvariantCulture);
        }

        CalculatorSettings settings = CalculatorSettings.Initial with { InputOutput = InputOutput.MathIDecimalO };
        return PallasEngine.Format(value, settings, session.Profile)!.Text;
    }

    private static FormatTarget TargetOf(string member, string because)
    {
        return member switch
        {
            "standard" => FormatTarget.Standard,
            "decimal" => FormatTarget.DecimalValue,
            "primeFactor" => FormatTarget.PrimeFactor,
            "recurringDecimal" => FormatTarget.RecurringDecimal,
            "rectangular" => FormatTarget.Rectangular,
            "polar" => FormatTarget.Polar,
            "improperFraction" => FormatTarget.ImproperFraction,
            "mixedFraction" => FormatTarget.MixedFraction,
            "eng" => FormatTarget.Engineering,
            "sexagesimal" => FormatTarget.Sexagesimal,
            _ => throw new InvalidOperationException($"{because}: the runner does not know the expectation '{member}'."),
        };
    }

    private static CalculatorSettings SettingsOf(Dictionary<string, string> settings)
    {
        CalculatorSettings result = CalculatorSettings.Initial;
        foreach ((string key, string value) in settings)
        {
            result = key switch
            {
                "inputOutput" => result with { InputOutput = Enum.Parse<InputOutput>(value.Replace("/", string.Empty, StringComparison.Ordinal)) },
                "angleUnit" => result with { AngleUnit = Enum.Parse<AngleUnit>(value) },
                "numberFormat" => result with { NumberFormat = NumberFormatOf(value) },
                "engineerSymbol" => result with { EngineerSymbol = value == "On" },
                "fractionResult" => result with { FractionResult = Enum.Parse<FractionResult>(value) },
                "complexResult" => result with { ComplexResult = value == "r∠θ" ? ComplexResult.Polar : ComplexResult.Rectangular },
                "decimalMark" => result with { DecimalMark = Enum.Parse<DecimalMark>(value) },
                "digitSeparator" => result with { DigitSeparator = value == "On" },
                "baseMode" => result with { BaseMode = BaseModeOf(value) },
                "verify" => result with { Verify = value == "On" },
                _ => throw new InvalidOperationException($"The runner does not know the setting '{key}'."),
            };
        }

        return result;
    }

    private static NumberFormat NumberFormatOf(string value)
    {
        int digits = int.Parse(value.AsSpan(value.StartsWith("Norm", StringComparison.Ordinal) ? 4 : 3), CultureInfo.InvariantCulture);
        return value[..3] switch
        {
            "Fix" => NumberFormat.Fix(digits),
            "Sci" => NumberFormat.Sci(digits),
            _ => digits == 1 ? NumberFormat.Norm1 : NumberFormat.Norm2,
        };
    }

    private static NumberBase BaseModeOf(string value)
    {
        return value switch
        {
            "Hexadecimal" => NumberBase.Hex,
            "Binary" => NumberBase.Bin,
            "Octal" => NumberBase.Oct,
            _ => NumberBase.Dec,
        };
    }

    private static MemoryVariable Variable(string name) => Enum.Parse<MemoryVariable>(name, ignoreCase: true);

    private static Value Parse(string text) => Value.FromDecimal(decimal.Parse(text, CultureInfo.InvariantCulture));
}
