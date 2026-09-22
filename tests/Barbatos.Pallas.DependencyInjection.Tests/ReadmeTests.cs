// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.DependencyInjection.Tests;

/// <summary>
/// The examples of the package README, run as written: a README that drifts from the API is a defect.
/// </summary>
public sealed class ReadmeTests
{
    [Fact]
    public void OneCall()
    {
        ServiceCollection services = new();

        services.AddPallas();

        using ServiceProvider provider = services.BuildServiceProvider();
        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();
        session.Calculate("2⌟3+1⌟1⌟2").Display.Text.Should().Be("13⌟6");
    }

    [Fact]
    public void TheOptionsAndTheBuilder()
    {
        ServiceCollection services = new();
        ConstantSet myConstants = new("Mine", [new ScientificConstant("@h", new(1m, 0), default, "J Hz^-1")]);
        UnitSet myUnits = new("Mine", [new UnitConversion("in▶cm", 3m)]);

        services.AddPallas(options =>
                {
                    options.Profile = CalculatorProfile.Extended;
                    options.App = CalculatorApp.Complex;
                    options.Budget = new EngineBudget(MaxIterations: 10_000_000, Timeout: TimeSpan.FromSeconds(5));
                    options.IncludeReferenceData = true;
                })
                .AddFunction<BeamDeflection>()
                .AddConstantSet(myConstants)
                .AddUnitSet(myUnits);

        using ServiceProvider provider = services.BuildServiceProvider();
        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.Profile.Should().Be(CalculatorProfile.Extended);
        session.App.Should().Be(CalculatorApp.Complex);
        session.Calculate("@h").Display.Text.Should().Be("1", "a later set replaces constants with the same symbol");
        session.Calculate("beam(3,4)").Display.Text.Should().Be("12");
    }

    /// <summary>The plugin function of the README, which needs a parameterless constructor and no reflection.</summary>
    private sealed class BeamDeflection : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("beam(", 2, 2);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            return Value.FromDecimal(arguments[0].ToDecimal() * arguments[1].ToDecimal());
        }
    }
}
