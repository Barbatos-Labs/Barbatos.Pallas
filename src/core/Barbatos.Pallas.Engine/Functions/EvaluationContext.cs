// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// What a function may know about the calculation it is evaluated in.
/// </summary>
public sealed class EvaluationContext
{
    // Checking the clock costs about as much as an iteration; once per 1024 iterations keeps Time Out within milliseconds.
    private const int ClockInterval = 1024;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private long _iterations;

    internal EvaluationContext(
        EngineCatalog catalog,
        CalculatorApp app,
        CalculatorSettings settings,
        CalculatorProfile profile,
        EngineBudget budget,
        Random random,
        CancellationToken cancellationToken)
    {
        Catalog = catalog;
        App = app;
        Settings = settings;
        Profile = profile;
        Budget = budget;
        Random = random;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the application the calculation runs in.</summary>
    public CalculatorApp App { get; }

    /// <summary>Gets the settings in effect.</summary>
    public CalculatorSettings Settings { get; }

    /// <summary>Gets the angle unit in effect.</summary>
    public AngleUnit AngleUnit => Settings.AngleUnit;

    /// <summary>Gets the profile, which decides the calculation range and function domains.</summary>
    public CalculatorProfile Profile { get; }

    /// <summary>Gets the token that cancels the calculation.</summary>
    public CancellationToken CancellationToken { get; }

    internal EngineCatalog Catalog { get; }

    internal EngineBudget Budget { get; }

    internal Random Random { get; }

    internal List<IntegralEstimate> Integrals { get; } = [];

    /// <summary>Counts iterations of a Σ, Π or ∫ body against the budget.</summary>
    /// <returns><see langword="true"/> while the budget allows more work.</returns>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    internal bool TryIterate(long count = 1)
    {
        _iterations += count;
        if (_iterations > Budget.MaxIterations)
        {
            return false;
        }

        if (_iterations % ClockInterval < count)
        {
            CancellationToken.ThrowIfCancellationRequested();
            return _stopwatch.Elapsed <= Budget.Timeout;
        }

        return true;
    }
}
