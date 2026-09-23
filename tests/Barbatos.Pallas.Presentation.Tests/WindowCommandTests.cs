// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// What the window's own shortcuts reach - the line on the screen, the answer to copy, a line to paste - and the
/// language, which is the application's and not the calculator's.
/// </summary>
public sealed class WindowCommandTests
{
    private static string RouteOf(CalculatorApp app) => CalculatorApps.Of(app).Route;

    [Fact]
    public void EveryScreenWithALineHandsItToTheShortcuts()
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.LineOf(RouteOf(CalculatorApp.Calculate)).Should().BeSameAs(shell.Calculate);
        shell.LineOf(RouteOf(CalculatorApp.Complex)).Should().BeSameAs(shell.Calculate);
        shell.LineOf(RouteOf(CalculatorApp.BaseN)).Should().BeSameAs(shell.BaseN.Calculate);
        shell.LineOf(RouteOf(CalculatorApp.Matrix)).Should().BeSameAs(shell.Matrix.Calculate);
        shell.LineOf(RouteOf(CalculatorApp.Vector)).Should().BeSameAs(shell.Vector.Calculate);
        shell.LineOf(RouteOf(CalculatorApp.Statistics)).Should().BeSameAs(shell.Statistics.Calculate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/settings")]
    [InlineData("/nowhere")]
    public void AScreenWithoutAnApplicationHasNoLine(string? route)
    {
        Shell.Create().LineOf(route).Should().BeNull();
    }

    [Theory]
    [InlineData(CalculatorApp.Distribution)]
    [InlineData(CalculatorApp.Equation)]
    [InlineData(CalculatorApp.Inequality)]
    [InlineData(CalculatorApp.Ratio)]
    [InlineData(CalculatorApp.Table)]
    [InlineData(CalculatorApp.Spreadsheet)]
    [InlineData(CalculatorApp.MathBox)]
    public void AFormHasNoLine(CalculatorApp app)
    {
        // Its fields are text boxes, whose own Ctrl+Z and Ctrl+V those keys are.
        Shell.Create().LineOf(RouteOf(app)).Should().BeNull();
    }

    [Fact]
    public void TheAnswerIsWhatTheScreenShows()
    {
        CalculateViewModel line = Shell.Create().Calculate;
        line.Answer.Should().BeNull("nothing has been calculated");

        line.Paste("1÷4");
        line.Execute();
        string fraction = line.Answer!;
        line.ToggleDecimal();

        fraction.Should().Be(line.Calculation!.Display.Text);
        line.Answer.Should().Be("0.25", "copying takes the form on the screen");
    }

    [Fact]
    public void AnErrorIsNoAnswer()
    {
        CalculateViewModel line = Shell.Create().Calculate;

        line.Paste("1÷0");
        line.Execute();

        line.HasError.Should().BeTrue();
        line.Answer.Should().BeNull();
    }

    [Fact]
    public void PastedTextIsReadAsTheCalculationItSpells()
    {
        CalculateViewModel line = Shell.Create().Calculate;

        line.Paste("  √(2)×3\r\n").Should().BeTrue();

        line.Input.Linear.Should().Be("√(2)×3");
        line.Input.Document.Root[0].Should().BeOfType<MathStructure>().Which.Kind.Should().Be(MathTemplateKind.SquareRoot);
    }

    [Fact]
    public void OnlyTheFirstLineIsPasted()
    {
        CalculateViewModel line = Shell.Create().Calculate;

        line.Paste("2+3\n4+5\n6").Should().BeTrue();
        line.Execute();

        line.Input.Linear.Should().Be("2+3");
        line.Answer.Should().Be("5");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n2+3")]
    public void NothingToPasteLeavesTheLineAsItIs(string? text)
    {
        CalculateViewModel line = Shell.Create().Calculate;
        line.Paste("7");

        line.Paste(text).Should().BeFalse();

        line.Input.Linear.Should().Be("7");
    }

    [Fact]
    public void APasteIsTakenBackLikeAnyEdit()
    {
        CalculateViewModel line = Shell.Create().Calculate;
        line.Input.Press(KeyId.One);

        line.Paste("2+3");
        line.Input.Undo();

        line.Input.Linear.Should().Be("1");
        line.Input.Redo();
        line.Input.Linear.Should().Be("2+3");
    }

    [Fact]
    public void WhatIsPastedIsReadForTheApplicationOfTheLine()
    {
        // "and" is an operator of Base-N alone (pp. 129-132).
        CalculatorShellViewModel shell = Shell.Create();
        shell.Calculate.Paste("6 and 3");
        shell.Calculate.Execute();
        shell.Open(CalculatorApps.Of(CalculatorApp.BaseN));
        CalculateViewModel line = shell.BaseN.Calculate;

        line.Paste("6 and 3");
        line.Execute();

        shell.Calculate.HasError.Should().BeTrue();
        line.Answer.Should().Be("2");
    }

    [Fact]
    public void TheLanguageFollowsWindowsUntilOneIsChosen()
    {
        CalculatorShellViewModel shell = Shell.Create();
        List<string?> changed = shell.Changes();

        shell.Language.Should().Be(CalculatorShellViewModel.SystemLanguage);
        shell.Language = "vi-VN";

        changed.Should().Equal([nameof(CalculatorShellViewModel.Language)]);
        CalculatorShellViewModel.Languages.Should().Equal(CalculatorShellViewModel.SystemLanguage, "en-US", "vi-VN");
    }

    [Fact]
    public void ResettingTheCalculatorLeavesTheLanguageAlone()
    {
        CalculatorShellViewModel shell = Shell.Create(out ISessionStore store);
        shell.Language = "en-US";
        shell.Settings.AngleUnit = AngleUnit.Radian;

        shell.Settings.Reset();
        shell.Save();
        CalculatorShellViewModel next = new(Shell.Session(), store);
        next.Load();

        shell.Language.Should().Be("en-US");
        next.Language.Should().Be(CalculatorShellViewModel.SystemLanguage, "the host keeps the language, not the session");
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("vi-VN", "vi-VN")]
    [InlineData(CalculatorShellViewModel.SystemLanguage, CalculatorShellViewModel.SystemLanguage)]
    [InlineData(null, CalculatorShellViewModel.SystemLanguage)]
    [InlineData("", CalculatorShellViewModel.SystemLanguage)]
    [InlineData("EN-US", CalculatorShellViewModel.SystemLanguage)]
    [InlineData("fr-FR", CalculatorShellViewModel.SystemLanguage)]
    public void AStoredLanguageIsOneTheApplicationSpeaks(string? stored, string language)
    {
        CalculatorShellViewModel.KnownLanguage(stored).Should().Be(language);
    }
}
