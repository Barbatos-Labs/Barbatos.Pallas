// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Globalization;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Everything an engine knows by name, frozen when the engine is built: the vocabulary, plugin functions and data sets.
/// </summary>
internal sealed class EngineCatalog
{
    public EngineCatalog(
        SyntaxVocabulary vocabulary,
        IEnumerable<IMathFunction> functions,
        IEnumerable<ConstantSet> constantSets,
        IEnumerable<UnitSet> unitSets,
        AtomicWeightTable? atomicWeights)
    {
        Vocabulary = vocabulary;
        Functions = functions.ToFrozenDictionary(function => function.Signature.Name, StringComparer.Ordinal);

        // A later set replaces the constants of an earlier one with the same symbol.
        Dictionary<string, (ScientificConstant Constant, Value Value)> constants = new(StringComparer.Ordinal);
        foreach (ScientificConstant constant in constantSets.SelectMany(set => set.Constants))
        {
            constants[constant.Symbol] = (constant, ToValue(constant.Value));
        }

        Constants = constants.ToFrozenDictionary(StringComparer.Ordinal);

        Dictionary<string, UnitConversion> units = new(StringComparer.Ordinal);
        foreach (UnitConversion conversion in unitSets.SelectMany(set => set.Conversions))
        {
            units[conversion.Command] = conversion;
        }

        Units = units.ToFrozenDictionary(StringComparer.Ordinal);
        AtomicWeights = atomicWeights;
    }

    public SyntaxVocabulary Vocabulary { get; }

    public FrozenDictionary<string, IMathFunction> Functions { get; }

    public FrozenDictionary<string, (ScientificConstant Constant, Value Value)> Constants { get; }

    public FrozenDictionary<string, UnitConversion> Units { get; }

    public AtomicWeightTable? AtomicWeights { get; }

    /// <summary>Turns a scaled decimal into a value with the precision rule: <see cref="decimal"/> when that keeps every digit.</summary>
    public static Value ToValue(ScaledDecimal number)
    {
        decimal value = number.Mantissa;
        try
        {
            for (int i = 0; i < number.Exponent; i++)
            {
                value *= 10m;
            }

            for (int i = 0; i > number.Exponent; i--)
            {
                decimal next = value / 10m;
                if (next * 10m != value)
                {
                    return FromText(number);
                }

                value = next;
            }
        }
        catch (OverflowException)
        {
            return FromText(number);
        }

        return value != 0m && Math.Abs(value) < Value.SmallestPreciseDecimal ? FromText(number) : Value.FromDecimal(value);
    }

    private static Value FromText(ScaledDecimal number)
    {
        string text = string.Create(CultureInfo.InvariantCulture, $"{number.Mantissa}E{number.Exponent}");
        return Value.FromApproximation(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture), form: null);
    }
}
