// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Displays calculation results with the calculator's settings and FORMAT conversions (manual pp. 22-25, 42-50).
/// </summary>
internal static class ResultFormatter
{
    public static FormattedResult? Format(Calculation calculation, CalculatorSettings settings, FormatTarget? target)
    {
        if (!calculation.Succeeded)
        {
            return null;
        }

        CalculatorApp app = calculation.App;
        CalculatorProfile profile = calculation.Profile;
        string separator = settings.DecimalMark == DecimalMark.Comma ? "; " : ", ";
        switch (calculation.Kind)
        {
            case CalculationKind.Verify:
                return Plain(calculation.IsTrue == true ? "True" : "False");
            case CalculationKind.Remainder:
                return Plain(Display(calculation.Result, app, settings, profile, null) + separator + "R=" + Display(calculation.Second!.Value, app, settings, profile, null));
            case CalculationKind.Polar:
                return Plain("r=" + Display(calculation.Result, app, settings, profile, null) + separator + "θ=" + Display(calculation.Second!.Value, app, settings, profile, null));
            case CalculationKind.Rectangular:
                return Plain("x=" + Display(calculation.Result, app, settings, profile, null) + separator + "y=" + Display(calculation.Second!.Value, app, settings, profile, null));
        }

        string? text = FormatValue(calculation.Result, app, settings, profile, target, calculation.Hints);
        if (text is null)
        {
            return null;
        }

        return calculation.Result.IsComposite
            ? new FormattedResult(text, CompositeLatex(calculation.Result, app, settings))
            : new FormattedResult(text, Latex(text, calculation.Result, app, settings));
    }

    /// <summary>Formats a value, or returns <see langword="null"/> when the FORMAT conversion does not apply to it.</summary>
    public static string? FormatValue(Value value, CalculatorApp app, CalculatorSettings settings, CalculatorProfile profile, FormatTarget? target, DisplayHints hints = DisplayHints.None)
    {
        string? text = value.Kind switch
        {
            ValueKind.BaseN => target is null ? FormatBaseN(value.ToInt32(), settings.BaseMode) : null,
            ValueKind.Complex => FormatComplex(value.ToComplex(), settings, profile, target),
            ValueKind.Matrix or ValueKind.Vector => target is null or FormatTarget.Standard or FormatTarget.DecimalValue
                ? FormatComposite(value, settings)
                : null,
            _ => FormatReal(value, settings, profile, target, hints),
        };

        return text is null ? null : NumberText.Localize(text, settings);
    }

    /// <summary>Formats a Base-N integer in a number mode: decimal is signed, the other bases show the 32-bit pattern (p. 130).</summary>
    public static string FormatBaseN(int value, NumberBase numberBase)
    {
        return numberBase switch
        {
            NumberBase.Hex => value.ToString("X", CultureInfo.InvariantCulture),
            NumberBase.Bin => Convert.ToString(value, 2),
            NumberBase.Oct => Convert.ToString(value, 8),
            _ => value.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static string Display(Value value, CalculatorApp app, CalculatorSettings settings, CalculatorProfile profile, FormatTarget? target)
    {
        return FormatValue(value, app, settings, profile, target) ?? string.Empty;
    }

    private static string? FormatReal(Value value, CalculatorSettings settings, CalculatorProfile profile, FormatTarget? target, DisplayHints hints)
    {
        if (target is null)
        {
            // A sum of degrees-minutes-seconds is displayed as one (assumption U16); LineO shows a fraction only for
            // fraction input (p. 32: 2⌟3+1⌟1⌟2 is 13⌟6, while p. 47 shows 3.25 as 3.25).
            bool decimalOutput = settings.InputOutput is InputOutput.MathIDecimalO or InputOutput.LineIDecimalO;
            if ((hints & DisplayHints.DecimalResult) != 0)
            {
                // Statistic and distribution results are decimals on every screen of pp. 83-100: x̄ is 5.95, not 119⌟20,
                // and a binomial probability 0.8125, not 13⌟16 (assumption U23).
                return DecimalText(value, settings);
            }

            if ((hints & DisplayHints.Sexagesimal) != 0 && !decimalOutput && ExactDisplay.Sexagesimal(value) is { } sexagesimal)
            {
                return sexagesimal;
            }

            return settings.InputOutput switch
            {
                InputOutput.MathIMathO => ExactDisplay.Standard(value, settings, profile, allowRoots: true) ?? DecimalText(value, settings),
                InputOutput.LineILineO when (hints & DisplayHints.FractionInput) != 0 =>
                    ExactDisplay.Standard(value, settings, profile, allowRoots: false) ?? DecimalText(value, settings),
                _ => DecimalText(value, settings),
            };
        }

        return target switch
        {
            FormatTarget.Standard => ExactDisplay.Standard(value, settings, profile, allowRoots: settings.InputOutput is InputOutput.MathIMathO or InputOutput.MathIDecimalO)
                ?? DecimalText(value, settings),
            FormatTarget.DecimalValue => NumberText.Format(value, settings.NumberFormat),
            FormatTarget.PrimeFactor => ExactDisplay.PrimeFactors(value, profile),
            FormatTarget.RecurringDecimal => ExactDisplay.RecurringDecimal(value, profile),
            FormatTarget.ImproperFraction => ExactDisplay.Fraction(value, profile, mixed: false),
            FormatTarget.MixedFraction => ExactDisplay.Fraction(value, profile, mixed: true),
            FormatTarget.Engineering => ExactDisplay.Engineering(value, settings, shift: 0),
            FormatTarget.Sexagesimal => ExactDisplay.Sexagesimal(value),
            _ => null,
        };
    }

    private static string DecimalText(Value value, CalculatorSettings settings)
    {
        return settings.EngineerSymbol ? ExactDisplay.EngineeringSymbol(value, settings) : NumberText.Format(value, settings.NumberFormat);
    }

    private static string? FormatComplex(Complex value, CalculatorSettings settings, CalculatorProfile profile, FormatTarget? target)
    {
        // The other conversions - prime factors, fractions, ENG, sexagesimal - do not apply to a complex number.
        bool? polar = target switch
        {
            FormatTarget.Polar => true,
            FormatTarget.Rectangular => false,
            null or FormatTarget.Standard or FormatTarget.DecimalValue => settings.ComplexResult == ComplexResult.Polar,
            _ => null,
        };

        if (polar is not { } asPolar)
        {
            return null;
        }

        CalculatorSettings partSettings = target == FormatTarget.DecimalValue ? settings with { InputOutput = InputOutput.MathIDecimalO } : settings;
        if (asPolar)
        {
            string modulus = Part(Complex.Abs(value), partSettings, profile);
            string angle = Part(Trigonometry.Atan2(value.Imaginary, value.Real, settings.AngleUnit), partSettings, profile);
            return modulus + "∠" + angle;
        }

        string real = Part(value.Real, partSettings, profile);
        string imaginary = Part(Math.Abs(value.Imaginary), partSettings, profile);
        string imaginaryText = imaginary == "1" ? "i" : imaginary + "i";
        if (value.Real == 0d)
        {
            return (double.IsNegative(value.Imaginary) ? "-" : string.Empty) + imaginaryText;
        }

        return real + (double.IsNegative(value.Imaginary) ? "-" : "+") + imaginaryText;
    }

    private static string Part(double part, CalculatorSettings settings, CalculatorProfile profile)
    {
        Value value = Value.FromApproximation(part, form: null);
        return settings.InputOutput == InputOutput.MathIMathO
            ? ExactDisplay.Standard(value, settings, profile, allowRoots: true) ?? NumberText.Format(value, settings.NumberFormat)
            : NumberText.Format(value, settings.NumberFormat);
    }

    /// <summary>Writes a matrix as <c>[[3, 0], [1, 1]]</c> and a vector as <c>[0.6, 0.8]</c>, each entry in the number format.</summary>
    /// <remarks>
    /// The calculator shows matrix and vector entries as decimals, never as fractions or surds: UnitV of (3, 4) is (0.6, 0.8)
    /// in MathI/MathO (p. 145; assumption U21).
    /// </remarks>
    private static string FormatComposite(Value value, CalculatorSettings settings)
    {
        string separator = settings.DecimalMark == DecimalMark.Comma ? "; " : ", ";
        if (value.Kind == ValueKind.Vector)
        {
            return "[" + string.Join(separator, value.ToVector().Elements.Select(element => DecimalText(element, settings))) + "]";
        }

        MatrixValue matrix = value.ToMatrix();
        IEnumerable<string> rows = Enumerable.Range(0, matrix.Rows)
            .Select(row => "[" + string.Join(separator, Enumerable.Range(0, matrix.Columns).Select(column => DecimalText(matrix[row, column], settings))) + "]");
        return "[" + string.Join(separator, rows) + "]";
    }

    /// <summary>Writes a matrix, or a vector as a column (as the VctAns screen shows it, p. 144), as a LaTeX pmatrix.</summary>
    private static string CompositeLatex(Value value, CalculatorApp app, CalculatorSettings settings)
    {
        string Entry(Value entry) => Latex(NumberText.Localize(DecimalText(entry, settings), settings), entry, app, settings);

        IEnumerable<string> rows;
        if (value.Kind == ValueKind.Vector)
        {
            rows = value.ToVector().Elements.Select(Entry);
        }
        else
        {
            MatrixValue matrix = value.ToMatrix();
            rows = Enumerable.Range(0, matrix.Rows)
                .Select(row => string.Join("&", Enumerable.Range(0, matrix.Columns).Select(column => Entry(matrix[row, column]))));
        }

        return @"\begin{pmatrix}" + string.Join(@"\\", rows) + @"\end{pmatrix}";
    }

    private static FormattedResult Plain(string text) => new(text, @"\text{" + text + "}");

    internal static string Latex(string text, Value value, CalculatorApp app, CalculatorSettings settings)
    {
        // The output notation is Canonical Linear Syntax for numbers, fractions, roots, π and complex numbers, so the
        // LaTeX printer of Expressions writes it. Engineering symbols and localized marks are written as text.
        if (settings.DecimalMark == DecimalMark.Dot && !settings.DigitSeparator)
        {
            CalculatorApp context = value.Kind == ValueKind.Complex ? CalculatorApp.Complex : app;
            ParseResult parsed = ExpressionParser.Parse(text, new SyntaxContext(context));
            if (parsed.Succeeded)
            {
                return LatexPrinter.Print(parsed.Root);
            }
        }

        return @"\text{" + text + "}";
    }
}
