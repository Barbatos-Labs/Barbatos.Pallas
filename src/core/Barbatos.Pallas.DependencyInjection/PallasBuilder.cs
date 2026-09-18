// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.DependencyInjection;

/// <summary>
/// Collects what <see cref="ServiceCollectionExtensions.AddPallas"/> adds to the engine before it is built.
/// </summary>
/// <remarks>
/// The engine is built once, when it is first resolved, so everything registered on the builder is in place by then,
/// whatever order the registrations were written in.
/// </remarks>
internal sealed class PallasBuilder(IServiceCollection services) : IPallasBuilder
{
    public IServiceCollection Services { get; } = services;

    public List<IMathFunction> Functions { get; } = [];

    public List<ConstantSet> ConstantSets { get; } = [];

    public List<UnitSet> UnitSets { get; } = [];

    public AtomicWeightTable? AtomicWeights { get; private set; }

    public IPallasBuilder AddFunction(IMathFunction mathFunction)
    {
        ArgumentNullException.ThrowIfNull(mathFunction);
        Functions.Add(mathFunction);
        return this;
    }

    public IPallasBuilder AddFunction<TFunction>()
        where TFunction : IMathFunction, new()
    {
        return AddFunction(new TFunction());
    }

    public IPallasBuilder AddConstantSet(ConstantSet constants)
    {
        ArgumentNullException.ThrowIfNull(constants);
        ConstantSets.Add(constants);
        return this;
    }

    public IPallasBuilder AddUnitSet(UnitSet units)
    {
        ArgumentNullException.ThrowIfNull(units);
        UnitSets.Add(units);
        return this;
    }

    public IPallasBuilder AddAtomicWeights(AtomicWeightTable atomicWeights)
    {
        ArgumentNullException.ThrowIfNull(atomicWeights);
        AtomicWeights = atomicWeights;
        return this;
    }
}
