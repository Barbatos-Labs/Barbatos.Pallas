// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The stored format of a session: what one build writes, every later build reads, and what it cannot read leaves
/// the calculator as a new one.
/// </summary>
public sealed class SessionSnapshotJsonTests
{
    private static CalculatorSession Used()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.Statistics);
        session.Settings = session.Settings with
        {
            InputOutput = InputOutput.LineILineO,
            AngleUnit = AngleUnit.Gradian,
            NumberFormat = NumberFormat.Fix(3),
            EngineerSymbol = true,
            FractionResult = FractionResult.Mixed,
            ComplexResult = ComplexResult.Polar,
            DecimalMark = DecimalMark.Comma,
            DigitSeparator = true,
            ComplexRoots = false,
        };
        session.Regression = RegressionModel.Power;
        session.SetVariable(MemoryVariable.A, Value.FromDecimal(1m / 3m));
        session.SetVariable(MemoryVariable.B, Value.FromDouble(1.6e-19));
        session.Define(DefinedFunction.F, "x²+1⌟2");
        session.SetMatrix(MatrixVariable.MatB, new MatrixValue(new[,] { { Value.One, Value.FromDecimal(2) }, { Value.FromDecimal(3), Value.FromDecimal(4) } }));
        session.SetVector(VectorVariable.VctD, new VectorValue(Value.One, Value.FromDecimal(2), Value.FromDecimal(3)));
        session.SetStatisticsData(new StatisticsData(
            [Value.One, Value.FromDecimal(2)],
            [Value.FromDecimal(3), Value.FromDecimal(4)],
            [Value.FromDecimal(5), Value.FromDecimal(6)]));
        return session;
    }

    [Fact]
    public void ASnapshotIsWrittenAndReadBackAsItWas()
    {
        SessionSnapshot snapshot = Used().Capture();

        SessionSnapshot? read = SessionSnapshotJson.Read(SessionSnapshotJson.Write(snapshot));

        read.Should().BeEquivalentTo(snapshot);
    }

    [Fact]
    public void AStoredSessionCalculatesWhatTheStoredOneDid()
    {
        CalculatorSession saved = Used();
        saved.Calculate("√(2)");

        CalculatorSession restored = Shell.Session();
        restored.Restore(SessionSnapshotJson.Read(SessionSnapshotJson.Write(saved.Capture()))!);

        restored.App.Should().Be(CalculatorApp.Statistics);
        restored.Settings.Should().Be(saved.Settings);
        restored.Regression.Should().Be(RegressionModel.Power);
        restored.GetVariable(MemoryVariable.A).ToDecimal().Should().Be(1m / 3m, "text keeps every digit, not the ten displayed");
        restored.GetVariable(MemoryVariable.B).ToDouble().Should().Be(1.6e-19);
        restored.GetMatrix(MatrixVariable.MatB)![1, 1].ToDecimal().Should().Be(4m);
        restored.GetVector(VectorVariable.VctD)!.Dimension.Should().Be(3);
        restored.Calculate("Ans×Ans").Display.Text.Should().Be("2,000", "√2 comes back as a surd, with the stored decimal mark");
        restored.Calculate("f(2)").Result.ToDecimal().Should().Be(4.5m);
        restored.Calculate("Σxy").Result.ToDecimal().Should().Be(63m);
    }

    [Fact]
    public void TheStoredTextSaysWhatItHoldsInWordsAndNumbers()
    {
        // The format is a contract: names and enum spellings are what a later build will read.
        CalculatorSession session = Shell.Session(CalculatorApp.Complex);
        session.Settings = session.Settings with { NumberFormat = NumberFormat.Fix(3), AngleUnit = AngleUnit.Radian };
        session.Calculate("√(2)");

        string json = SessionSnapshotJson.Write(session.Capture());

        json.Should().Contain("\"version\":1")
            .And.Contain("\"app\":\"Complex\"")
            .And.Contain("\"angleUnit\":\"Radian\"")
            .And.Contain("\"numberFormat\":\"Fix3\"")
            .And.Contain("\"ans\":");
        SessionSnapshotJson.Read(json)!.Ans.Should().Be("e:√(2)", "a value is the engine's own text, whatever JSON escapes on the way");
    }

    [Theory]
    [InlineData("Norm1")]
    [InlineData("Norm2")]
    [InlineData("Fix0")]
    [InlineData("Fix9")]
    [InlineData("Sci1")]
    [InlineData("Sci10")]
    public void EveryNumberFormatIsOneWord(string word)
    {
        CalculatorSession session = Shell.Session();
        SettingsViewModel settings = new(session);
        int digit = word.IndexOfAny(['0', '1', '2', '3', '4', '5', '6', '7', '8', '9']);
        settings.TrySetNumberFormat(Enum.Parse<NumberFormatKind>(word[..digit]), int.Parse(word[digit..], CultureInfo.InvariantCulture));

        string json = SessionSnapshotJson.Write(session.Capture());

        json.Should().Contain("\"numberFormat\":\"" + word + "\"");
        SessionSnapshotJson.Read(json)!.Settings.NumberFormat.Should().Be(session.Settings.NumberFormat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[1,2,3]")]
    [InlineData("null")]
    public void TextThatIsNotASessionIsNoSession(string? json)
    {
        SessionSnapshotJson.Read(json).Should().BeNull();
    }

    [Fact]
    public void ASessionOfALaterVersionIsReadAndLeftToTheEngineToRefuse()
    {
        SessionSnapshot later = Shell.Session().Capture() with { Version = SessionSnapshot.CurrentVersion + 1 };

        SessionSnapshot? read = SessionSnapshotJson.Read(SessionSnapshotJson.Write(later));

        read!.Version.Should().Be(SessionSnapshot.CurrentVersion + 1, "the version rule belongs to Restore, not to the reader");
    }

    [Fact]
    public void AnEmptyObjectIsANewCalculator()
    {
        SessionSnapshot? read = SessionSnapshotJson.Read("{}");

        read.Should().NotBeNull();
        read!.Version.Should().Be(0);
        read.App.Should().Be(CalculatorApp.Calculate);
        read.Settings.Should().Be(CalculatorSettings.Initial);
        read.Regression.Should().Be(RegressionModel.Linear);
        read.Ans.Should().BeEmpty();
        read.PreAns.Should().BeEmpty();
        read.FunctionF.Should().BeNull();
        read.Variables.Should().BeEmpty();
        read.Matrices.Should().BeEmpty();
        read.Vectors.Should().BeEmpty();
        read.StatisticsX.Should().BeEmpty();
        read.StatisticsY.Should().BeEmpty();
        read.StatisticsFrequencies.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{\"app\":\"Graphing\"}")]
    [InlineData("{\"app\":\"3\"}")]
    [InlineData("{\"app\":\"93\"}")]
    [InlineData("{\"app\":null}")]
    [InlineData("{\"app\":\"\"}")]
    public void AnApplicationThisBuildDoesNotKnowIsCalculate(string json)
    {
        SessionSnapshotJson.Read(json)!.App.Should().Be(CalculatorApp.Calculate, "a stored name is text, and text can say anything");
    }

    [Theory]
    [InlineData("{\"settings\":{\"angleUnit\":\"Turns\"}}")]
    [InlineData("{\"settings\":{\"numberFormat\":\"Fix12\"}}")]
    [InlineData("{\"settings\":{\"numberFormat\":\"Bits2\"}}")]
    [InlineData("{\"settings\":{\"numberFormat\":\"Fix\"}}")]
    [InlineData("{\"settings\":{\"numberFormat\":\"3\"}}")]
    [InlineData("{\"settings\":{\"numberFormat\":\"\"}}")]
    [InlineData("{\"settings\":{\"angleUnit\":\"\"}}")]
    [InlineData("{\"settings\":{}}")]
    [InlineData("{\"settings\":null}")]
    public void ASettingThisBuildDoesNotKnowIsTheOneACalculatorStartsWith(string json)
    {
        SessionSnapshot read = SessionSnapshotJson.Read(json)!;

        read.Settings.AngleUnit.Should().Be(AngleUnit.Degree);
        read.Settings.NumberFormat.Should().Be(NumberFormat.Norm1);
        read.Settings.ComplexRoots.Should().BeTrue("a flag that is not stored is the flag a calculator starts with");
    }

    [Fact]
    public void AMatrixThatIsNotThereStaysNotThere()
    {
        SessionSnapshot snapshot = Shell.Session(CalculatorApp.Matrix).Capture();

        SessionSnapshot read = SessionSnapshotJson.Read(SessionSnapshotJson.Write(snapshot))!;

        read.Matrices.Should().HaveCount(4).And.OnlyContain(matrix => matrix == null);
        read.Vectors.Should().HaveCount(4).And.OnlyContain(vector => vector == null);
    }

    [Fact]
    public void ASnapshotIsRequired()
    {
        Action act = () => SessionSnapshotJson.Write(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
