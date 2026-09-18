// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.DependencyInjection;

/// <summary>
/// Adds functions and data sets to the engine being registered.
/// </summary>
public interface IPallasBuilder
{
    /// <summary>Gets the service collection the engine is registered in.</summary>
    IServiceCollection Services { get; }

    /// <summary>Adds a plugin function.</summary>
    /// <param name="mathFunction">The function.</param>
    /// <returns>This builder.</returns>
    IPallasBuilder AddFunction(IMathFunction mathFunction);

    /// <summary>Adds a plugin function, created without reflection so the engine stays AOT-compatible.</summary>
    /// <typeparam name="TFunction">The function type, with a parameterless constructor.</typeparam>
    /// <returns>This builder.</returns>
    IPallasBuilder AddFunction<TFunction>()
        where TFunction : IMathFunction, new();

    /// <summary>Adds a set of scientific constants.</summary>
    /// <param name="constants">The set.</param>
    /// <returns>This builder.</returns>
    IPallasBuilder AddConstantSet(ConstantSet constants);

    /// <summary>Adds a set of unit conversions.</summary>
    /// <param name="units">The set.</param>
    /// <returns>This builder.</returns>
    IPallasBuilder AddUnitSet(UnitSet units);

    /// <summary>Sets the atomic weights.</summary>
    /// <param name="atomicWeights">The table.</param>
    /// <returns>This builder.</returns>
    IPallasBuilder AddAtomicWeights(AtomicWeightTable atomicWeights);
}
