// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// The plugin API of docs/ARCHITECTURE.md §6: a function added to the engine is parsed, bound and evaluated like a
/// built-in one.
/// </summary>
public sealed class PluginTests
{
    [Fact]
    public void APluginFunction_CanBeCalled()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new BeamDeflection()).Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("beam(2,3)").Display.Text.Should().Be("6");
        session.Calculate("beam(2,3)+1").Display.Text.Should().Be("7");
    }

    [Fact]
    public void APluginFunction_ReportsItsOwnErrors()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new BeamDeflection()).Build();
        CalculatorSession session = engine.CreateSession();

        session.Calculate("beam(-1,3)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
    }

    [Fact]
    public void APluginFunction_IsHeldToTheProfilesRange()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new Huge()).Build();

        engine.CreateSession().Calculate("huge(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        engine.CreateSession(profile: CalculatorProfile.Extended).Calculate("huge(1)").Error.Should().BeNull();
    }

    [Fact]
    public void ThePluginsArity_IsCheckedBeforeItIsCalled()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new BeamDeflection()).Build();

        engine.CreateSession().Calculate("beam(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.SyntaxError);
    }

    [Fact]
    public void APluginName_JoinsTheVocabulary()
    {
        PallasEngine engine = PallasEngineBuilder.CreateDefault().AddFunction(new BeamDeflection()).Build();

        engine.Vocabulary.Symbols.Should().Contain(symbol => symbol.Text == "beam(");
        SyntaxVocabulary.Standard.Symbols.Should().NotContain(symbol => symbol.Text == "beam(");
    }

    [Fact]
    public void APluginCannotTakeTheNameOfABuiltIn()
    {
        PallasEngineBuilder builder = PallasEngineBuilder.CreateDefault().AddFunction(new Duplicate());

        Action act = () => builder.Build();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TheBuilderRejectsNullsAndEmptyBudgets()
    {
        PallasEngineBuilder builder = PallasEngineBuilder.CreateDefault();

        Action function = () => builder.AddFunction(null!);
        Action constants = () => builder.AddConstantSet(null!);
        Action units = () => builder.AddUnitSet(null!);
        Action weights = () => builder.AddAtomicWeights(null!);
        Action budget = () => builder.WithBudget(new EngineBudget(0, TimeSpan.FromSeconds(1)));

        function.Should().Throw<ArgumentNullException>();
        constants.Should().Throw<ArgumentNullException>();
        units.Should().Throw<ArgumentNullException>();
        weights.Should().Throw<ArgumentNullException>();
        budget.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ASignatureValidatesItsName()
    {
        Action badName = () => _ = new FunctionSignature("beam", 1, 1).Name;
        Action badArity = () => _ = new FunctionSignature("beam(", 0, 1).Name;
        Action badRange = () => _ = new FunctionSignature("beam(", 2, 1).Name;

        badName.Should().Throw<ArgumentException>();
        badArity.Should().Throw<ArgumentOutOfRangeException>();
        badRange.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed class BeamDeflection : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("beam(", 2, 2);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context)
        {
            if (arguments[0].ToDouble() < 0d)
            {
                return EvalResult.Failure(CalcErrorKind.MathError);
            }

            return Value.FromDecimal(arguments[0].ToDecimal() * arguments[1].ToDecimal());
        }
    }

    private sealed class Huge : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("huge(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context) => Value.FromDouble(1e200);
    }

    private sealed class Duplicate : IMathFunction
    {
        public FunctionSignature Signature { get; } = new("sin(", 1, 1);

        public EvalResult Invoke(ReadOnlySpan<Value> arguments, EvaluationContext context) => Value.Zero;
    }
}
