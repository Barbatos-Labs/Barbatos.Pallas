// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// Calculate, end to end: what is typed is calculated, what fails says where, and what was calculated comes back.
/// </summary>
public sealed class CalculateViewModelTests
{
    private static CalculateViewModel Screen(CalculatorApp app = CalculatorApp.Calculate) => new(Shell.Session(app));

    private static CalculateViewModel Typed(string symbols, CalculatorApp app = CalculatorApp.Calculate)
    {
        CalculateViewModel screen = Screen(app);
        screen.Input.Set(MathDocumentReader.Read(symbols, app));
        return screen;
    }

    [Fact]
    public void ANewScreenHasNothingOnIt()
    {
        CalculateViewModel screen = Screen();

        screen.Input.IsEmpty.Should().BeTrue();
        screen.Calculation.Should().BeNull();
        screen.Display.Should().BeNull();
        screen.HasResult.Should().BeFalse();
        screen.HasError.Should().BeFalse();
        screen.ErrorKey.Should().BeNull();
        screen.History.Should().BeEmpty();
    }

    [Fact]
    public void WhatIsTypedIsCalculated()
    {
        CalculateViewModel screen = Typed("4×sin(30)×(30+10×3)");

        screen.Execute();

        screen.HasResult.Should().BeTrue();
        screen.Display!.Text.Should().Be("120", "the manual's own first example (p. 28)");
        screen.Calculation!.Result.ToDecimal().Should().Be(120m);
        screen.HasError.Should().BeFalse();
    }

    [Fact]
    public void TheEqualsKeyCalculates()
    {
        CalculateViewModel screen = Screen();
        foreach (KeyId key in new[] { KeyId.One, KeyId.Add, KeyId.Two, KeyId.Execute })
        {
            screen.Input.Press(key);
        }

        screen.Display!.Text.Should().Be("3");
    }

    [Fact]
    public void AnEmptyLineCalculatesNothing()
    {
        CalculateViewModel screen = Screen();

        screen.Execute();

        screen.Calculation.Should().BeNull();
        screen.History.Should().BeEmpty();
    }

    [Fact]
    public void WhatCannotBeCalculatedPutsTheCursorWhereItWentWrong()
    {
        CalculateViewModel screen = Typed("14÷0×2");

        screen.Execute();

        screen.HasError.Should().BeTrue();
        screen.ErrorKey.Should().Be("error.MathError", "the core names the error; the screen finds its words");
        screen.Calculation!.Error!.Value.Kind.Should().Be(CalcErrorKind.MathError);
        screen.Input.Linear.Should().Be("14÷0×2", "what was typed stays where it is");
        screen.Calculation!.Error!.Value.Span.Start.Should().Be(0, "the division that failed starts the line");
        screen.Input.Document.Cursor.Index.Should().Be(0, "the cursor waits at the start of what could not be calculated");
    }

    [Fact]
    public void TheCursorFindsTheStructureThatFailed()
    {
        // The engine reports the span of the root, which starts after the 1 and the plus.
        CalculateViewModel screen = Typed("1+√(0-9)");

        screen.Execute();

        screen.HasError.Should().BeTrue();
        screen.Calculation!.Error!.Value.Span.Start.Should().Be(2);
        screen.Input.Document.Cursor.Index.Should().Be(2, "which is the root itself, the third thing on the line");
        screen.Input.Document.Cursor.Depth.Should().Be(0);
    }

    [Fact]
    public void TheNextKeystrokeCorrectsWhatFailed()
    {
        CalculateViewModel screen = Typed("2+×3");
        screen.Execute();

        screen.HasError.Should().BeTrue();
        screen.ErrorKey.Should().Be("error.SyntaxError");
        screen.Input.Document.Cursor.Index.Should().Be(screen.Calculation!.Error!.Value.Span.Start, "the cursor is where the engine stopped reading");

        screen.Input.Press(KeyId.Delete);
        screen.Execute();

        screen.Input.Linear.Should().Be("2×3", "the cursor was before the times, so one Delete took the plus and left a calculation");
        screen.Display!.Text.Should().Be("6");
    }

    [Fact]
    public void EveryCalculationIsRemembered()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2×3"));
        screen.Execute();

        screen.History.Should().HaveCount(2);
        screen.History[0].Input.Should().Be("2×3", "newest first");
        screen.History[1].Input.Should().Be("1+1");
    }

    [Fact]
    public void WhatWasCalculatedComesBackOnTheLine()
    {
        CalculateViewModel screen = Typed("1⌟2+1⌟3");
        screen.Execute();
        screen.Input.Clear();

        screen.RecallPrevious().Should().BeTrue();

        screen.Input.Linear.Should().Be("1⌟2+1⌟3");
        screen.Input.Document.Root.Count.Should().Be(3, "as a fraction, a plus and a fraction, not as characters");
        screen.Display!.Text.Should().Be("5⌟6", "with what it came to");
    }

    [Fact]
    public void TheHistoryIsWalkedInBothDirections()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2+2"));
        screen.Execute();

        screen.RecallPrevious().Should().BeTrue();
        screen.Input.Linear.Should().Be("2+2");
        screen.RecallPrevious().Should().BeTrue();
        screen.Input.Linear.Should().Be("1+1");
        screen.RecallPrevious().Should().BeFalse("there is nothing older");

        screen.RecallNext().Should().BeTrue();
        screen.Input.Linear.Should().Be("2+2");
        screen.RecallNext().Should().BeFalse("and nothing newer than the newest");
    }

    [Fact]
    public void TheUpKeyReachesTheHistoryOnlyWhenTheCursorCannotMove()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2⌟3"));
        screen.Input.Press(KeyId.Left);

        screen.Input.Press(KeyId.Up);
        screen.Input.Linear.Should().Be("2⌟3", "up from the denominator is the numerator, not the history");
        screen.Input.Document.Cursor.Path[0].Slot.Should().Be(0);

        screen.Input.Press(KeyId.Up);
        screen.Input.Linear.Should().Be("1+1", "and up from there, where the cursor cannot go, is what was calculated before");
    }

    [Fact]
    public void ARecalledCalculationIsCalculatedAgain()
    {
        CalculateViewModel screen = Typed("2+3");
        screen.Execute();
        screen.RecallPrevious();

        screen.Execute();

        screen.Display!.Text.Should().Be("5");
        screen.History.Should().HaveCount(2, "calculating it again is a calculation of its own");
    }

    [Fact]
    public void TheResultTurnsBetweenItsFormsWithOneKey()
    {
        CalculateViewModel screen = Typed("1⌟2");
        screen.Execute();

        screen.Display!.Text.Should().Be("1⌟2");
        screen.ToggleDecimal().Should().BeTrue();
        screen.Display!.Text.Should().Be("0.5");
        screen.ToggleDecimal().Should().BeTrue();
        screen.Display!.Text.Should().Be("1⌟2", "and back again");
    }

    [Theory]
    [InlineData("13⌟4", FormatTarget.MixedFraction, "3⌟1⌟4")]
    [InlineData("1⌟3", FormatTarget.RecurringDecimal, "0.(3)")]
    [InlineData("1234", FormatTarget.Engineering, "1.234×10^3")]
    public void TheResultIsShownAnotherWay(string input, FormatTarget target, string expected)
    {
        CalculateViewModel screen = Typed(input);
        screen.Execute();

        screen.Format(target).Should().BeTrue();

        screen.Display!.Text.Should().Be(expected);
    }

    [Fact]
    public void AResultThatHasNoOtherFormStaysAsItIs()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        string shown = screen.Display!.Text;

        screen.Format(FormatTarget.Polar).Should().BeFalse("a real number has no polar form");

        screen.Display!.Text.Should().Be(shown);
    }

    [Fact]
    public void NothingIsFormattedBeforeThereIsAResult()
    {
        CalculateViewModel screen = Screen();

        screen.Format(FormatTarget.DecimalValue).Should().BeFalse();
        screen.ToggleDecimal().Should().BeFalse();
        screen.RecallPrevious().Should().BeFalse();
        screen.RecallNext().Should().BeFalse();
    }

    [Fact]
    public void ClearTakesTheLineAndTheResultAway()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();

        screen.Clear();

        screen.Input.IsEmpty.Should().BeTrue();
        screen.Calculation.Should().BeNull();
        screen.HasResult.Should().BeFalse();
        screen.History.Should().HaveCount(1, "what was calculated is still what was calculated");
    }

    [Fact]
    public void TheScreenSaysWhenItHasSomethingNewToShow()
    {
        CalculateViewModel screen = Typed("1+1");
        List<string?> changed = screen.Changes();

        screen.Execute();

        changed.Should().Contain([
            nameof(CalculateViewModel.Calculation),
            nameof(CalculateViewModel.Display),
            nameof(CalculateViewModel.HasResult),
            nameof(CalculateViewModel.HasError),
            nameof(CalculateViewModel.ErrorKey),
            nameof(CalculateViewModel.History)]);
    }

    [Fact]
    public void AResultIsDrawnAsMathematicsAndAnErrorIsNot()
    {
        CalculateViewModel screen = Typed("1⌟2");
        screen.Execute();

        screen.HasResult.Should().BeTrue();
        screen.Display!.Text.Should().Be("1⌟2");
        screen.Display!.Latex.Should().Be(@"\frac{1}{2}", "the screen draws the result, not only its text");

        screen.Input.Set(MathDocumentReader.Read("1÷0"));
        screen.Execute();

        screen.HasResult.Should().BeFalse("there is nothing to draw and nothing to show but the error");
        screen.HasError.Should().BeTrue();
        screen.Display!.Latex.Should().BeEmpty();
    }

    [Fact]
    public void TheDownKeyWalksBackTowardsWhatWasJustCalculated()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2+2"));
        screen.Execute();
        screen.Input.Clear();

        screen.Input.Press(KeyId.Up);
        screen.Input.Press(KeyId.Up);
        screen.Input.Linear.Should().Be("1+1");

        screen.Input.Press(KeyId.Down);

        screen.Input.Linear.Should().Be("2+2", "down is the way back to the newest");
    }

    [Fact]
    public void RecallStartsAtTheNewestAfterEveryCalculation()
    {
        CalculateViewModel screen = Typed("1+1");
        screen.Execute();
        screen.Input.Set(MathDocumentReader.Read("2+2"));
        screen.Execute();

        screen.RecallPrevious().Should().BeTrue();

        screen.Input.Linear.Should().Be("2+2", "the one just calculated, not the one before it");
    }

    [Fact]
    public void ASessionIsRequired()
    {
        Action act = () => _ = new CalculateViewModel(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ACalculationIsRequiredToRecallOne()
    {
        CalculateViewModel screen = Screen();
        Action act = () => screen.Recall(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
