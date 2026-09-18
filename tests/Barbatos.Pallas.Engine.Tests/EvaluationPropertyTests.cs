// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Expressions.Tests.Support;
using CsCheck;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// Properties of evaluation over generated trees and arbitrary text: an answer or a named error, never an exception,
/// never a hang.
/// </summary>
/// <remarks>
/// The generators are those of Phase 2, so the trees are exactly what the parser can produce in each application.
/// A small budget keeps a generated Σ from running for a million terms. CsCheck runs the samples on several threads and
/// a session is not thread-safe, so every sample has its own: one shared session failed a run in the 21-assembly suite
/// on 18 Sep 2026.
/// </remarks>
public sealed class EvaluationPropertyTests
{
    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().Build();

    private static readonly EngineBudget SmallBudget = new(2_000, TimeSpan.FromMilliseconds(250));

    private static readonly char[] Alphabet =
    [
        .. "0123456789.()+-×÷^,⌟√!%°′″ˣ²³⁻¹ ",
        .. "ABCDEFxyzeiπnPCd/R",
    ];

    public static TheoryData<CalculatorApp> Applications => [CalculatorApp.Calculate, CalculatorApp.Complex, CalculatorApp.BaseN];

    [Theory]
    [MemberData(nameof(Applications))]
    public void AnyTree_EvaluatesToAValueOrAnError(CalculatorApp app)
    {
        SyntaxContext context = new(app);

        SyntaxTrees.For(context).Sample(
            tree =>
            {
                Calculation calculation = Session(app, verify: false).Calculate(LinearPrinter.Print(tree, context));
                return calculation.Succeeded ? calculation.Display.Text.Length > 0 : Enum.IsDefined(calculation.Error!.Value.Kind);
            },
            iter: 2_000,
            print: tree => $"{app}: \"{LinearPrinter.Print(tree, context)}\"");
    }

    [Fact]
    public void AnyVerifiableTree_IsTrueOrFalse()
    {
        SyntaxContext context = new(CalculatorApp.Calculate, AllowRelations: true);

        SyntaxTrees.For(context).Sample(
            tree =>
            {
                Calculation calculation = Session(CalculatorApp.Calculate, verify: true).Calculate(LinearPrinter.Print(tree, context));
                return calculation.Succeeded ? calculation.IsTrue is not null : Enum.IsDefined(calculation.Error!.Value.Kind);
            },
            iter: 2_000,
            print: tree => $"Verify: \"{LinearPrinter.Print(tree, context)}\"");
    }

    [Theory]
    [MemberData(nameof(Applications))]
    public void ArbitraryText_NeverThrows(CalculatorApp app)
    {
        Gen.OneOfConst(Alphabet).Array[0, 40].Select(characters => new string(characters)).Sample(
            text =>
            {
                Calculation calculation = Session(app, verify: false).Calculate(text);
                if (calculation.Succeeded)
                {
                    return true;
                }

                CalcError error = calculation.Error!.Value;
                return error.Span.Start >= 0 && error.Span.End <= text.Length;
            },
            iter: 10_000,
            print: text => $"{app}: \"{text}\"");
    }

    [Fact]
    public void EveryResultCanBeDisplayedAndReparsed()
    {
        // A displayed result is Canonical Linear Syntax, so it can be typed back in; the value must not change.
        SyntaxContext context = SyntaxContext.Calculate;

        SyntaxTrees.For(context).Sample(
            tree =>
            {
                CalculatorSession session = Session(CalculatorApp.Calculate, verify: false);
                Calculation calculation = session.Calculate(LinearPrinter.Print(tree, context));
                if (!calculation.Succeeded || calculation.Kind != CalculationKind.Value || !calculation.Result.IsReal)
                {
                    return true;
                }

                string text = session.Format(calculation, FormatTarget.DecimalValue)!.Text;
                Calculation again = session.Calculate(text);
                return again.Succeeded && ValueMathIsClose(calculation.Result, again.Result);
            },
            iter: 1_000,
            print: tree => $"\"{LinearPrinter.Print(tree, context)}\"");
    }

    private static bool ValueMathIsClose(Value left, Value right)
    {
        // The display keeps 10 significant digits, so the reparsed value agrees to about that.
        double a = left.ToDouble();
        double b = right.ToDouble();
        return Math.Abs(a - b) <= 1e-9 * Math.Max(Math.Abs(a), Math.Abs(b));
    }

    private static CalculatorSession Session(CalculatorApp app, bool verify)
    {
        CalculatorSession session = Engine.CreateSession(app, randomSeed: 880);
        session.Budget = SmallBudget;
        session.Settings = session.Settings with { Verify = verify };
        return session;
    }
}
