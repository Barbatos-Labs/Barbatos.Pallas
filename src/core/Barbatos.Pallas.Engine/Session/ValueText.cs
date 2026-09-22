// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A <see cref="Value"/> as text that reads back as the same value, for <see cref="SessionSnapshot"/>.
/// </summary>
/// <remarks>
/// <para>
/// A tag says what the text is, because a number alone cannot say whether it is exact, whether it is held as
/// <see cref="double"/>, or what it is a Base-N value of:
/// </para>
/// <list type="table">
/// <item><term><c>e:</c></term><description>an expression in Canonical Linear Syntax, evaluated again on the way back. A
/// value that carries an exact form is written this way - <c>e:√(2)</c>, <c>e:3⌟4</c> - so the form comes back with
/// it.</description></item>
/// <item><term><c>d:</c></term><description>an exact <see cref="decimal"/>, with every digit it has.</description></item>
/// <item><term><c>a:</c></term><description>a <see cref="decimal"/> that is an approximation.</description></item>
/// <item><term><c>f:</c></term><description>a <see cref="double"/>, in the shortest text that reads back as it.</description></item>
/// <item><term><c>c:</c></term><description>a complex number, its two parts the same way.</description></item>
/// <item><term><c>n:</c></term><description>a Base-N value, as a 32-bit integer.</description></item>
/// </list>
/// <para>
/// Nothing is rounded: a decimal keeps all of its digits and a double its round-trip text, so a snapshot restores the
/// value that was captured, not what a display would have shown of it.
/// </para>
/// </remarks>
internal static class ValueText
{
    /// <summary>The text a value is written as.</summary>
    public static string Write(Value value, CalculatorProfile profile)
    {
        // A value with an exact form is written as the form: reading it back computes the value and the form together.
        if (value.Form is not null
            && ResultFormatter.FormatValue(value, CalculatorApp.Calculate, CalculatorSettings.Initial, profile, target: null) is { } form)
        {
            return "e:" + form;
        }

        return value.Kind switch
        {
            ValueKind.DecimalReal => (value.IsExact ? "d:" : "a:") + value.ToDecimal().ToString(CultureInfo.InvariantCulture),
            ValueKind.DoubleReal => "f:" + value.ToDouble().ToString("R", CultureInfo.InvariantCulture),
            ValueKind.Complex => string.Create(
                CultureInfo.InvariantCulture,
                $"c:{value.ToComplex().Real.ToString("R", CultureInfo.InvariantCulture)},{value.ToComplex().Imaginary.ToString("R", CultureInfo.InvariantCulture)}"),
            _ => "n:" + value.ToInt32().ToString(CultureInfo.InvariantCulture),
        };
    }

    /// <summary>The value a text was written from, or <see langword="null"/> when the text is not one.</summary>
    public static Value? Read(string? text, CalculatorSession session)
    {
        if (text is null || text.Length < 2 || text[1] != ':')
        {
            return null;
        }

        string rest = text[2..];
        switch (text[0])
        {
            case 'e':
                // The expression is calculated in the application it belongs to: i and ∠ are the Complex application's.
                Calculation calculation = session.Evaluate(rest);
                return calculation.Succeeded ? calculation.Result : null;
            case 'd':
            case 'a':
                return decimal.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number)
                    ? Value.FromDecimal(number, isExact: text[0] == 'd', form: null)
                    : null;
            case 'f':
                return double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture, out double approximation)
                    ? Value.FromApproximation(approximation, form: null)
                    : null;
            case 'c':
                string[] parts = rest.Split(',');
                return parts.Length == 2
                    && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double real)
                    && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double imaginary)
                    ? Value.CreateComplex(real, imaginary)
                    : null;
            case 'n':
                return int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture, out int baseN)
                    ? Value.FromBaseN(baseN)
                    : null;
            default:
                return null;
        }
    }
}
