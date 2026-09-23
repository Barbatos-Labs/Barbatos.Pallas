// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Pallas.DependencyInjection.Tests;

/// <summary>
/// <see cref="ServiceCollectionExtensions.AddPallas"/>: what an application gets from one call.
/// </summary>
public sealed class AddPallasTests
{
    [Fact]
    public void AddPallas_RegistersAnEngineAndSessions()
    {
        using ServiceProvider provider = new ServiceCollection().AddPallas().Services.BuildServiceProvider();

        PallasEngine engine = provider.GetRequiredService<PallasEngine>();
        CalculatorSession first = provider.GetRequiredService<CalculatorSession>();
        CalculatorSession second = provider.GetRequiredService<CalculatorSession>();

        engine.Should().BeSameAs(provider.GetRequiredService<PallasEngine>(), "the engine is a singleton");
        first.Should().NotBeSameAs(second, "each session has its own memory");
        first.Calculate("2⌟3+1⌟1⌟2").Display.Text.Should().Be("13⌟6");
    }

    [Theory]
    // Every application of the calculator but Math Box, which arrives in Phase 6.
    [InlineData(CalculatorApp.Calculate, "1+1", "2")]
    [InlineData(CalculatorApp.Complex, "(1+2i)×i", "-2+i")]
    [InlineData(CalculatorApp.BaseN, "d5+d3", "8")]
    [InlineData(CalculatorApp.Matrix, "Identity(2)", "[[1, 0], [0, 1]]")]
    [InlineData(CalculatorApp.Vector, "1+1", "2")]
    [InlineData(CalculatorApp.Statistics, "1+1", "2")]
    [InlineData(CalculatorApp.Distribution, "1+1", "2")]
    [InlineData(CalculatorApp.Equation, "1+1", "2")]
    [InlineData(CalculatorApp.Inequality, "1+1", "2")]
    [InlineData(CalculatorApp.Ratio, "1+1", "2")]
    [InlineData(CalculatorApp.Spreadsheet, "A1+1", "1")]
    [InlineData(CalculatorApp.Table, "1+1", "2")]
    public void ASessionOfEveryApplicationCanBeResolvedAndUsed(CalculatorApp app, string input, string display)
    {
        using ServiceProvider provider = new ServiceCollection().AddPallas(options => options.App = app).Services.BuildServiceProvider();

        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.App.Should().Be(app);
        session.Calculate(input).Display.Text.Should().Be(display);
    }

    [Fact]
    public void ASessionOfMathBox_IsResolved()
    {
        // p. 146: the Math Box application has its engine since Phase 6.
        using ServiceProvider provider = new ServiceCollection().AddPallas(options => options.App = CalculatorApp.MathBox).Services.BuildServiceProvider();

        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.App.Should().Be(CalculatorApp.MathBox);
        session.Clock(3).Smaller.Display.Text.Should().Be("90");
    }

    [Fact]
    public void TheReferenceDataIsIncludedByDefault()
    {
        using ServiceProvider provider = new ServiceCollection().AddPallas().Services.BuildServiceProvider();
        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.Calculate("@c").Display.Text.Should().Be("299792458");
        session.Calculate("AtWt(21)").Display.Text.Should().Be("44.955907");
        session.Settings = session.Settings with { InputOutput = InputOutput.LineILineO };
        session.Calculate("5cm▶in").Display.Text.Should().Be("1.968503937");
    }

    [Fact]
    public void TheReferenceDataCanBeLeftOut()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddPallas(options => options.IncludeReferenceData = false)
            .Services.BuildServiceProvider();

        provider.GetRequiredService<CalculatorSession>().Calculate("@c").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void OptionsChooseTheProfileApplicationAndBudget()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddPallas(options =>
            {
                options.Profile = CalculatorProfile.Extended;
                options.App = CalculatorApp.Complex;
                options.Budget = new EngineBudget(50, TimeSpan.FromSeconds(1));
            })
            .Services.BuildServiceProvider();

        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.App.Should().Be(CalculatorApp.Complex);
        session.Profile.Should().Be(CalculatorProfile.Extended);
        session.Calculate("100!").Error.Should().BeNull("the Extended profile has no factorial limit of 69");

        session.SwitchApp(CalculatorApp.Calculate);
        session.Calculate("Σ(x,1,1000)").Error!.Value.Kind.Should().Be(CalcErrorKind.TimeOut, "the budget allows 50 iterations");
    }

    [Fact]
    public void APluginFunctionCanBeRegistered()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddPallas()
            .AddFunction<Double>()
            .AddFunction(new Triple())
            .Services.BuildServiceProvider();

        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.Calculate("double(21)").Display.Text.Should().Be("42");
        session.Calculate("triple(14)").Display.Text.Should().Be("42");
    }

    [Fact]
    public void DataSetsCanBeAddedOrReplaced()
    {
        ConstantSet mine = new("Mine", [new ScientificConstant("@h", new(1m, 0), default, "J Hz^-1")]);

        using ServiceProvider provider = new ServiceCollection().AddPallas().AddConstantSet(mine).Services.BuildServiceProvider();

        provider.GetRequiredService<CalculatorSession>().Calculate("@h").Display.Text.Should().Be("1", "a later set replaces an earlier one");
    }

    [Fact]
    public void InvalidOptions_AreReportedWhenTheEngineIsResolved()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddPallas(options => options.Budget = new EngineBudget(0, TimeSpan.FromSeconds(1)))
            .Services.BuildServiceProvider();

        Action act = () => provider.GetRequiredService<PallasEngine>();

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EachBudgetLimitIsNamedWhenItIsInvalid()
    {
        PallasOptions none = new() { Budget = null! };
        PallasOptions noIterations = new() { Budget = new EngineBudget(0, TimeSpan.FromSeconds(1)) };
        PallasOptions noTime = new() { Budget = new EngineBudget(1, TimeSpan.Zero) };

        none.Invoking(options => options.Validate()).Should().Throw<ArgumentNullException>().WithParameterName("Budget");
        noIterations.Invoking(options => options.Validate()).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("Budget.MaxIterations");
        noTime.Invoking(options => options.Validate()).Should().Throw<ArgumentOutOfRangeException>().WithParameterName("Budget.Timeout");
        new PallasOptions().Invoking(options => options.Validate()).Should().NotThrow();
    }

    [Fact]
    public void UnitSetsAndAtomicWeights_ReachTheEngine()
    {
        UnitSet units = new("Mine", [new UnitConversion("in▶cm", 3m)]);
        AtomicWeightTable weights = new("Mine", [new AtomicWeight(1, "H", 2m, IsMassNumber: false)]);

        using ServiceProvider provider = new ServiceCollection()
            .AddPallas(options => options.IncludeReferenceData = false)
            .AddUnitSet(units)
            .AddAtomicWeights(weights)
            .Services.BuildServiceProvider();
        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        session.Calculate("2in▶cm").Display.Text.Should().Be("6");
        session.Calculate("AtWt(1)").Display.Text.Should().Be("2");
    }

    [Fact]
    public void InvalidProfilesAndApplications_AreReportedToo()
    {
        using ServiceProvider profileProvider = new ServiceCollection()
            .AddPallas(options => options.Profile = (CalculatorProfile)42)
            .Services.BuildServiceProvider();
        using ServiceProvider appProvider = new ServiceCollection()
            .AddPallas(options => options.App = (CalculatorApp)42)
            .Services.BuildServiceProvider();

        Action profile = () => profileProvider.GetRequiredService<PallasEngine>();
        Action app = () => appProvider.GetRequiredService<PallasEngine>();

        profile.Should().Throw<ArgumentException>();
        app.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TheBuilderRejectsNulls()
    {
        IPallasBuilder builder = new ServiceCollection().AddPallas();

        Action function = () => builder.AddFunction(null!);
        Action constants = () => builder.AddConstantSet(null!);
        Action units = () => builder.AddUnitSet(null!);
        Action weights = () => builder.AddAtomicWeights(null!);
        Action services = () => ServiceCollectionExtensions.AddPallas(null!);

        function.Should().Throw<ArgumentNullException>();
        constants.Should().Throw<ArgumentNullException>();
        units.Should().Throw<ArgumentNullException>();
        weights.Should().Throw<ArgumentNullException>();
        services.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CallingAddPallasTwice_AddsToOneEngine()
    {
        // A library registers its function, then the host calls AddPallas again with its own options.
        ServiceCollection services = [];
        IPallasBuilder library = services.AddPallas().AddFunction<Double>();
        IPallasBuilder host = services.AddPallas(options => options.App = CalculatorApp.Complex).AddFunction(new Triple());

        using ServiceProvider provider = services.BuildServiceProvider();
        CalculatorSession session = provider.GetRequiredService<CalculatorSession>();

        host.Should().BeSameAs(library);
        session.App.Should().Be(CalculatorApp.Complex, "the options of every call apply");
        session.SwitchApp(CalculatorApp.Calculate);
        session.Calculate("double(21)").Display.Text.Should().Be("42");
        session.Calculate("triple(14)").Display.Text.Should().Be("42", "a function added through the second call is not lost");
        services.Count(descriptor => descriptor.ServiceType == typeof(PallasEngine)).Should().Be(1);
    }

    [Fact]
    public void TheBuilderExposesTheServiceCollection()
    {
        ServiceCollection services = [];

        IPallasBuilder builder = services.AddPallas();

        builder.Services.Should().BeSameAs(services);
    }

    private sealed class Double : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("double(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context) => Value.FromDecimal(arguments[0].ToDecimal() * 2m);
    }

    private sealed class Triple : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("triple(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context) => Value.FromDecimal(arguments[0].ToDecimal() * 3m);
    }
}
