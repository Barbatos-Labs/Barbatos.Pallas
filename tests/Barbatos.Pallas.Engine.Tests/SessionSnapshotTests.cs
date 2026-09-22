// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// What a session keeps when an application stores it and starts again (manual pp. 36-40): the snapshot holds the
/// values themselves, not what a display would have shown of them.
/// </summary>
public sealed class SessionSnapshotTests
{
    private static CalculatorSession Session(CalculatorApp app = CalculatorApp.Calculate) => Calculator.Session(app);

    [Fact]
    public void AMemoryComesBackWithEveryDigit()
    {
        CalculatorSession saved = Session();
        saved.SetVariable(MemoryVariable.A, Calculator.Evaluate("1÷3"));
        saved.SetVariable(MemoryVariable.B, Value.FromDouble(1.6e-19));
        saved.Calculate("2+3");

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(1m / 3m, "a decimal keeps all of its digits, not the ten displayed");
        restored.GetVariable(MemoryVariable.B).ToDouble().Should().Be(1.6e-19);
        restored.Ans.ToDecimal().Should().Be(5m);
    }

    [Fact]
    public void AValueKeepsItsExactForm()
    {
        // √2 restored as 1.414213562 would square to 1.999999999, and the display would lose the surd.
        CalculatorSession saved = Session();
        saved.Calculate("√(2)");
        saved.Store(MemoryVariable.C);

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.Calculate("C").Display.Text.Should().Be("√(2)");
        restored.Calculate("C×C").Display.Text.Should().Be("2");
    }

    [Theory]
    // One value of every kind a memory can hold.
    [InlineData(CalculatorApp.Calculate, "0.1+0.2", "3⌟10")]
    [InlineData(CalculatorApp.Calculate, "10√(2)+15×3√(3)", "45√(3)+10√(2)")]
    [InlineData(CalculatorApp.Calculate, "2⌟3", "2⌟3")]
    [InlineData(CalculatorApp.Calculate, "1×10^99", "1×10^99")]
    [InlineData(CalculatorApp.Complex, "2+3i", "2+3i")]
    [InlineData(CalculatorApp.BaseN, "d255", "255")]
    public void EveryKindOfValueSurvivesTheRoundTrip(CalculatorApp app, string input, string display)
    {
        CalculatorSession saved = Session(app);
        saved.Calculate(input);

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.App.Should().Be(app);
        restored.Format(restored.Calculate("Ans"))!.Text.Should().Be(display);
        restored.Format(restored.Calculate("Ans"))!.Text.Should().Be(saved.Format(saved.Calculate("Ans"))!.Text, "the session shows what it showed");
    }

    [Fact]
    public void TheSettingsAndTheApplicationComeBack()
    {
        CalculatorSession saved = Session(CalculatorApp.Statistics);
        saved.Settings = saved.Settings with
        {
            InputOutput = InputOutput.LineILineO,
            AngleUnit = Barbatos.Pallas.Numerics.AngleUnit.Gradian,
            NumberFormat = NumberFormat.Fix(3),
            DecimalMark = DecimalMark.Comma,
            ComplexRoots = false,
        };
        saved.Regression = RegressionModel.Power;

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.App.Should().Be(CalculatorApp.Statistics);
        restored.Settings.Should().Be(saved.Settings);
        restored.Regression.Should().Be(RegressionModel.Power);
    }

    [Fact]
    public void MatricesAndVectorsComeBack()
    {
        CalculatorSession saved = Session(CalculatorApp.Matrix);
        saved.SetMatrix(MatrixVariable.MatA, new MatrixValue(new[,] { { Value.FromDecimal(0.5m), Calculator.Evaluate("√(2)") }, { Value.FromDecimal(3), Value.FromDecimal(4) } }));
        saved.SetVector(VectorVariable.VctB, new VectorValue(Value.FromDecimal(1), Value.FromDecimal(2), Value.FromDecimal(3)));

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.GetMatrix(MatrixVariable.MatA)!.Rows.Should().Be(2);
        restored.GetMatrix(MatrixVariable.MatA)![0, 0].ToDecimal().Should().Be(0.5m);
        restored.GetMatrix(MatrixVariable.MatA)![0, 1].ToDouble().Should().BeApproximately(Math.Sqrt(2d), 1e-15d);
        restored.GetVector(VectorVariable.VctB)!.Dimension.Should().Be(3);
        restored.GetVector(VectorVariable.VctB)![2].ToDecimal().Should().Be(3m);
        restored.GetMatrix(MatrixVariable.MatB).Should().BeNull();
        restored.GetVector(VectorVariable.VctA).Should().BeNull();
    }

    [Fact]
    public void TheDefinedFunctionsAndTheStatisticsDataComeBack()
    {
        CalculatorSession saved = Session(CalculatorApp.Statistics);
        saved.Define(DefinedFunction.F, "x²+1⌟2");
        saved.Define(DefinedFunction.G, "sin(x)");
        saved.SetStatisticsData(new StatisticsData(
            [Value.FromDecimal(1), Value.FromDecimal(2)],
            [Value.FromDecimal(3), Value.FromDecimal(4)],
            [Value.FromDecimal(5), Value.FromDecimal(6)]));

        CalculatorSession restored = Session();
        restored.Restore(saved.Capture());

        restored.Calculate("f(2)").Display.Text.Should().Be("9⌟2");
        restored.Calculate("g(0)").Display.Text.Should().Be("0");
        restored.StatisticsData.Rows.Should().Be(2);
        restored.StatisticsData.IsTwoVariable.Should().BeTrue();
        restored.StatisticsData.HasFrequencies.Should().BeTrue();
        restored.Calculate("Σxy").Display.Text.Should().Be("63", "1×3×5 + 2×4×6");
    }

    [Fact]
    public void ASnapshotOfAnEmptySessionRestoresAnEmptySession()
    {
        CalculatorSession used = Session();
        used.SetVariable(MemoryVariable.A, Value.FromDecimal(42));
        used.Define(DefinedFunction.F, "x");
        used.SetStatisticsData(new StatisticsData([Value.One]));

        used.Restore(Session().Capture());

        used.GetVariable(MemoryVariable.A).Should().Be(Value.Zero);
        used.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
        used.StatisticsData.Rows.Should().Be(0);
    }

    [Fact]
    public void WhatASnapshotCannotSayIsLeftAsItWas()
    {
        // A snapshot from elsewhere may hold text this engine cannot read; the session keeps what it had instead.
        CalculatorSession session = Session();
        session.SetVariable(MemoryVariable.A, Value.FromDecimal(7));
        SessionSnapshot broken = session.Capture() with
        {
            Variables = ["x:nonsense", .. Enumerable.Repeat("d:0", 8)],
            Ans = "not a value",
            FunctionF = "x²+",
            Matrices = [new MatrixSnapshot(2, 2, ["d:1"]), null, null, null],
        };

        session.Restore(broken);

        session.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(7m);
        session.GetMatrix(MatrixVariable.MatA).Should().BeNull();
        session.Calculate("f(1)").Error!.Value.Kind.Should().Be(CalcErrorKind.NotDefined);
    }

    [Fact]
    public void ASnapshotOfALaterVersionIsRefused()
    {
        CalculatorSession session = Session();
        SessionSnapshot later = session.Capture() with { Version = SessionSnapshot.CurrentVersion + 1 };

        session.Invoking(s => s.Restore(later)).Should().Throw<ArgumentOutOfRangeException>();
        session.Invoking(s => s.Restore(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ASnapshotOfAnApplicationThisEngineHasNot_IsRefused()
    {
        CalculatorSession session = Session();
        SessionSnapshot mathBox = session.Capture() with { App = CalculatorApp.MathBox };

        session.Invoking(s => s.Restore(mathBox)).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void ASnapshotIsPlainTextAndSettings()
    {
        // What an application has to store: strings, enums and the settings record - no Value, no engine types.
        CalculatorSession session = Session();
        session.Calculate("√(2)");

        SessionSnapshot snapshot = session.Capture();

        snapshot.Version.Should().Be(1);
        snapshot.Variables.Should().HaveCount(9).And.OnlyContain(text => text.StartsWith("d:", StringComparison.Ordinal));
        snapshot.Ans.Should().Be("e:√(2)");
        snapshot.Matrices.Should().HaveCount(4).And.OnlyContain(matrix => matrix == null);
        snapshot.StatisticsX.Should().BeEmpty();
        snapshot.Should().BeAssignableTo<IEquatable<SessionSnapshot>>("a record an application can compare and copy");
    }
}
