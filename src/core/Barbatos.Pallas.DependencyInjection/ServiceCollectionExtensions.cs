// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Data;
using Barbatos.Pallas.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Barbatos.Pallas.DependencyInjection;

/// <summary>
/// Registers Barbatos.Pallas with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the calculation engine as a singleton and calculator sessions as transients.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes the options: profile, budget, and whether the reference data is included.</param>
    /// <returns>A builder for functions and data sets; the same builder on every call.</returns>
    /// <remarks>
    /// Calling this more than once adds to one engine: each call applies its <paramref name="configure"/> in turn and
    /// returns the builder of the first call, so nothing registered through a later call is lost.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddPallas(options => options.Profile = CalculatorProfile.Extended)
    ///         .AddFunction&lt;BeamDeflection&gt;();
    /// </code>
    /// </example>
    public static IPallasBuilder AddPallas(this IServiceCollection services, Action<PallasOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<PallasOptions> options = services.AddOptions<PallasOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        // A second call (a library and its host both calling AddPallas) must add to the same engine, not to a builder
        // nothing reads: the engine is built once, from the builder registered first.
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(PallasBuilder) && descriptor.ImplementationInstance is PallasBuilder existing)
            {
                return existing;
            }
        }

        PallasBuilder builder = new(services);
        services.AddSingleton(builder);
        services.TryAddSingleton(provider => Build(provider.GetRequiredService<IOptions<PallasOptions>>().Value, builder));
        services.TryAddTransient(provider =>
        {
            PallasOptions resolved = provider.GetRequiredService<IOptions<PallasOptions>>().Value;
            return provider.GetRequiredService<PallasEngine>().CreateSession(resolved.App, resolved.Profile);
        });

        return builder;
    }

    private static PallasEngine Build(PallasOptions options, PallasBuilder builder)
    {
        options.Validate();

        PallasEngineBuilder engine = PallasEngineBuilder.CreateDefault().WithBudget(options.Budget);
        if (options.IncludeReferenceData)
        {
            engine.AddConstantSet(ConstantSets.Codata2022)
                .AddUnitSet(UnitSets.NistSp811)
                .AddAtomicWeights(AtomicWeightTables.Ciaaw);
        }

        foreach (ConstantSet constants in builder.ConstantSets)
        {
            engine.AddConstantSet(constants);
        }

        foreach (UnitSet units in builder.UnitSets)
        {
            engine.AddUnitSet(units);
        }

        if (builder.AtomicWeights is { } table)
        {
            engine.AddAtomicWeights(table);
        }

        foreach (IMathFunction function in builder.Functions)
        {
            engine.AddFunction(function);
        }

        return engine.Build();
    }
}
