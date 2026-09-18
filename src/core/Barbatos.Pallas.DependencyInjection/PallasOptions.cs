// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.DependencyInjection;

/// <summary>
/// The options of <see cref="ServiceCollectionExtensions.AddPallas"/>.
/// </summary>
public sealed class PallasOptions
{
    /// <summary>Gets or sets the profile new sessions are created with; the calculator's limits by default.</summary>
    public CalculatorProfile Profile { get; set; } = CalculatorProfile.Standard;

    /// <summary>Gets or sets the application new sessions start in; Calculate by default.</summary>
    public CalculatorApp App { get; set; } = CalculatorApp.Calculate;

    /// <summary>Gets or sets what one calculation may spend.</summary>
    public EngineBudget Budget { get; set; } = EngineBudget.Default;

    /// <summary>
    /// Gets or sets whether the reference data of Barbatos.Pallas.Data is registered: the CODATA constants, the
    /// NIST SP 811 unit conversions and the CIAAW atomic weights. On by default.
    /// </summary>
    public bool IncludeReferenceData { get; set; } = true;

    /// <summary>Validates the options, as the service provider does when the engine is first resolved.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A budget limit is not positive.</exception>
    /// <exception cref="ArgumentException">The profile or application is not a defined value.</exception>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Budget);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Budget.MaxIterations);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Budget.Timeout, TimeSpan.Zero);

        if (!Enum.IsDefined(Profile))
        {
            throw new ArgumentException($"{Profile} is not a defined CalculatorProfile.", nameof(Profile));
        }

        if (!Enum.IsDefined(App))
        {
            throw new ArgumentException($"{App} is not a defined CalculatorApp.", nameof(App));
        }
    }
}
