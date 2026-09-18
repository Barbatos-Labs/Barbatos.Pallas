// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Configures and builds a <see cref="PallasEngine"/>: plugin functions, data sets and the budget.
/// </summary>
/// <remarks>
/// The engine has the reference calculator's functions but no reference data: scientific constants, unit conversions and atomic
/// weights come from data sets, such as those of Barbatos.Pallas.Data, added here (decision of 18 Sep 2026).
/// </remarks>
public sealed class PallasEngineBuilder
{
    private readonly List<IMathFunction> _functions = [];
    private readonly List<ConstantSet> _constantSets = [];
    private readonly List<UnitSet> _unitSets = [];
    private AtomicWeightTable? _atomicWeights;
    private EngineBudget _budget = EngineBudget.Default;

    private PallasEngineBuilder()
    {
    }

    /// <summary>Returns a builder for an engine with the reference calculator's functions.</summary>
    /// <returns>The builder.</returns>
    public static PallasEngineBuilder CreateDefault() => new();

    /// <summary>Adds a plugin function.</summary>
    /// <param name="function">The function; its name must not be taken.</param>
    /// <returns>This builder.</returns>
    public PallasEngineBuilder AddFunction(IMathFunction function)
    {
        ArgumentNullException.ThrowIfNull(function);
        _functions.Add(function);
        return this;
    }

    /// <summary>Adds a set of scientific constants; a later set replaces constants with the same symbol.</summary>
    /// <param name="constants">The set.</param>
    /// <returns>This builder.</returns>
    public PallasEngineBuilder AddConstantSet(ConstantSet constants)
    {
        ArgumentNullException.ThrowIfNull(constants);
        _constantSets.Add(constants);
        return this;
    }

    /// <summary>Adds a set of unit conversions; a later set replaces conversions with the same command.</summary>
    /// <param name="units">The set.</param>
    /// <returns>This builder.</returns>
    public PallasEngineBuilder AddUnitSet(UnitSet units)
    {
        ArgumentNullException.ThrowIfNull(units);
        _unitSets.Add(units);
        return this;
    }

    /// <summary>Sets the atomic weights <c>AtWt(</c> returns.</summary>
    /// <param name="atomicWeights">The table.</param>
    /// <returns>This builder.</returns>
    public PallasEngineBuilder AddAtomicWeights(AtomicWeightTable atomicWeights)
    {
        ArgumentNullException.ThrowIfNull(atomicWeights);
        _atomicWeights = atomicWeights;
        return this;
    }

    /// <summary>Sets the default budget of each calculation.</summary>
    /// <param name="budget">The budget.</param>
    /// <returns>This builder.</returns>
    public PallasEngineBuilder WithBudget(EngineBudget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(budget.MaxIterations);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(budget.Timeout, TimeSpan.Zero);
        _budget = budget;
        return this;
    }

    /// <summary>Builds the engine.</summary>
    /// <returns>The engine, immutable and safe to share between threads.</returns>
    /// <exception cref="ArgumentException">Two plugin functions have the same name, or one has the name of a built-in symbol.</exception>
    public PallasEngine Build()
    {
        SyntaxVocabulary vocabulary = _functions.Count == 0
            ? SyntaxVocabulary.Standard
            : SyntaxVocabulary.Standard.With(_functions.Select(function => function.Signature.Symbol));
        EngineCatalog catalog = new(vocabulary, _functions, _constantSets, _unitSets, _atomicWeights);
        return new PallasEngine(catalog, _budget);
    }
}
