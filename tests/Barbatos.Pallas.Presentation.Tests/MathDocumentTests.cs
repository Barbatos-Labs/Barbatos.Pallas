// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The structural input: what a keystroke puts on the screen, and where the cursor goes next.
/// </summary>
public sealed class MathDocumentTests
{
    private static MathDocument Typed(string digits) => Typed(MathDocument.Empty, digits);

    private static MathDocument Typed(MathDocument document, string digits)
    {
        foreach (char character in digits)
        {
            document = document.Insert(character.ToString());
        }

        return document;
    }

    [Fact]
    public void AnEmptyInputHoldsNothingAndTheCursorIsAtItsStart()
    {
        MathDocument.Empty.IsEmpty.Should().BeTrue();
        MathDocument.Empty.Root.Count.Should().Be(0);
        MathDocument.Empty.Cursor.Should().Be(MathCursor.Start);
        MathLinearWriter.Write(MathDocument.Empty).Should().BeEmpty();
    }

    [Fact]
    public void EachKeystrokeIsOneSymbol()
    {
        MathDocument document = Typed("12+3");

        document.Root.Count.Should().Be(4, "a number is deleted one digit at a time");
        document.Cursor.Index.Should().Be(4);
        MathLinearWriter.Write(document).Should().Be("12+3");
    }

    [Fact]
    public void AWholeNameIsOneSymbol()
    {
        MathDocument document = MathDocument.Empty.Insert("sin(").Insert("3").Insert("0").Insert(")");

        document.Root.Count.Should().Be(4);
        MathLinearWriter.Write(document).Should().Be("sin(30)");
    }

    [Fact]
    public void AStructureTakesTheNumberBeforeIt()
    {
        MathDocument document = Typed("12").Insert(MathTemplateKind.Fraction).Insert("5");

        MathLinearWriter.Write(document).Should().Be("12⌟5", "twelve over five, not twelve beside a fraction");
        document.Root.Count.Should().Be(1, "the whole number went into the numerator, not its last digit");
        document.Cursor.Depth.Should().Be(1);
        document.Cursor.Path[0].Slot.Should().Be(1, "the cursor goes on to the denominator");
    }

    [Fact]
    public void AStructureAfterAnOperatorTakesNothing()
    {
        MathDocument document = Typed("1+").Insert(MathTemplateKind.Fraction).Insert("2");

        MathLinearWriter.Write(document).Should().Be("1+2⌟");
        document.Cursor.Path[0].Slot.Should().Be(0, "an empty fraction starts in its numerator");
    }

    [Fact]
    public void APowerTakesWhatItRaises()
    {
        MathDocument document = Typed("2").Insert(MathTemplateKind.Power).Insert("1").Insert("0");

        MathLinearWriter.Write(document).Should().Be("2^(10)");
    }

    [Fact]
    public void ARootTakesItsIndex()
    {
        MathDocument document = Typed("3").Insert(MathTemplateKind.Root).Insert("8");

        MathLinearWriter.Write(document).Should().Be("3ˣ√(8)");
    }

    [Fact]
    public void AStructureTakesTheStructureBeforeIt()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveToEnd().Insert(MathTemplateKind.Power).Insert("3");

        MathLinearWriter.Write(document).Should().Be("(1⌟2)^(3)", "a fraction raised to a power needs its brackets back");
    }

    [Fact]
    public void TheCursorWalksIntoAStructureAndOutAgain()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveToEnd();

        document.Cursor.Depth.Should().Be(0);
        document = document.MoveLeft();
        document.Cursor.Depth.Should().Be(1, "moving left steps into the denominator");
        document.Cursor.Path[0].Slot.Should().Be(1);
        document.Cursor.Index.Should().Be(1);
        document = document.MoveLeft();
        document.Cursor.Index.Should().Be(0);
        document = document.MoveLeft();
        document.Cursor.Path[0].Slot.Should().Be(0, "and on into the numerator");
        document = document.MoveLeft().MoveLeft();
        document.Cursor.Depth.Should().Be(0, "and out at the front");
        document.Cursor.Index.Should().Be(0);
    }

    [Fact]
    public void TheCursorWalksRightThroughEverySlot()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveToStart();

        document = document.MoveRight();
        document.Cursor.Path[0].Slot.Should().Be(0);
        document = document.MoveRight();
        document.Cursor.Index.Should().Be(1, "past the numerator's own symbol");
        document = document.MoveRight();
        document.Cursor.Path[0].Slot.Should().Be(1);
        document = document.MoveRight().MoveRight();
        document.Cursor.Depth.Should().Be(0);
        document.Cursor.Index.Should().Be(1, "after the fraction");
        document.MoveRight().Should().BeSameAs(document, "there is nothing to the right of the end");
    }

    [Fact]
    public void UpAndDownMoveBetweenTheSlotsThatSitAboveOneAnother()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2");

        document.Cursor.Path[0].Slot.Should().Be(1);
        document = document.MoveUp();
        document.Cursor.Path[0].Slot.Should().Be(0, "up from the denominator is the numerator");
        document.Cursor.Index.Should().Be(1, "at the end of what is there");
        document = document.MoveDown();
        document.Cursor.Path[0].Slot.Should().Be(1);
        document.MoveDown().Should().BeSameAs(document, "there is nothing below a denominator");
    }

    [Fact]
    public void UpAndDownReachTheStructureAround()
    {
        // The cursor is in the numerator of a fraction inside the exponent of a power. Down is the denominator; down
        // again leaves the fraction, which has nothing below, and finds the power's own base.
        MathDocument document = Typed("2").Insert(MathTemplateKind.Power).Insert(MathTemplateKind.Fraction);

        document.Cursor.Depth.Should().Be(2);
        document.MoveDown().Cursor.Path[1].Slot.Should().Be(1, "below a numerator is its denominator");

        MathDocument moved = document.MoveDown().MoveDown();

        moved.Cursor.Depth.Should().Be(1);
        moved.Cursor.Path[0].Slot.Should().Be(0, "below an exponent is its base");
    }

    [Fact]
    public void UpAndDownStopWhereThereIsNothingMore()
    {
        MathDocument fraction = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2");
        MathDocument numerator = fraction.MoveUp();
        MathDocument body = MathDocument.Empty.Insert(MathTemplateKind.Sum).Insert("1");

        numerator.MoveUp().Should().BeSameAs(numerator, "there is nothing above a numerator");
        fraction.MoveDown().Should().BeSameAs(fraction, "and nothing below a denominator");
        body.MoveUp().Should().BeSameAs(body, "the body of a sum is beside its bounds, not under them");
        body.MoveDown().Should().BeSameAs(body);
        MathDocument.Empty.MoveUp().Should().BeSameAs(MathDocument.Empty, "and an empty line has no slots at all");
    }

    [Fact]
    public void UpAndDownReachTheBoundsOfASum()
    {
        MathDocument document = MathDocument.Empty.Insert(MathTemplateKind.Sum).Insert("1").MoveRight();

        document.Cursor.Path[0].Slot.Should().Be(1, "to the right of the body is the lower bound");
        document.MoveUp().Cursor.Path[0].Slot.Should().Be(2, "and above the lower bound is the upper one");
        document.MoveUp().MoveDown().Cursor.Path[0].Slot.Should().Be(1);
    }

    [Fact]
    public void DeleteTakesOneSymbol()
    {
        MathDocument document = Typed("12+3").Backspace();

        MathLinearWriter.Write(document).Should().Be("12+");
        document.Cursor.Index.Should().Be(3);
    }

    [Fact]
    public void DeleteStepsIntoAStructureRatherThanTakingItAway()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveToEnd().Backspace();

        MathLinearWriter.Write(document).Should().Be("1⌟2", "what is on the screen is still on the screen");
        document.Cursor.Depth.Should().Be(1);
        document.Cursor.Path[0].Slot.Should().Be(1);
        document.Cursor.Index.Should().Be(1);
    }

    [Fact]
    public void DeleteTakesAnEmptyStructureAway()
    {
        MathDocument document = Typed("1+").Insert(MathTemplateKind.Fraction);

        document = document.Backspace();

        MathLinearWriter.Write(document).Should().Be("1+");
        document.Cursor.Depth.Should().Be(0);
        document.Cursor.Index.Should().Be(2);
    }

    [Fact]
    public void DeleteAtTheStartOfASlotMovesOut()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction).Insert("2").MoveUp().MoveLeft();

        document = document.Backspace();

        document.Cursor.Depth.Should().Be(0);
        MathLinearWriter.Write(document).Should().Be("1⌟2", "nothing was deleted, because nothing was in front of the cursor");
    }

    [Fact]
    public void OnlyTheTemplatesThatShouldTakeSomethingTakeIt()
    {
        MathDocument root = Typed("12").Insert(MathTemplateKind.SquareRoot).Insert("9");
        MathDocument brackets = Typed("12").Insert(MathTemplateKind.Parentheses).Insert("9");
        MathDocument sum = Typed("12").Insert(MathTemplateKind.Sum).Insert("9");

        MathLinearWriter.Write(root).Should().Be("12√(9)", "a square root opens empty beside what was typed");
        MathLinearWriter.Write(brackets).Should().Be("12(9)");
        MathLinearWriter.Write(sum).Should().Be("12Σ(9,,)");
    }

    [Fact]
    public void DeleteStepsIntoTheStructureItIsBehind()
    {
        MathDocument document = Typed("1+");
        document = Typed(document.Insert(MathTemplateKind.Fraction), "2").MoveToEnd();

        document = document.Backspace();
        document = document.Insert("5");

        MathLinearWriter.Write(document).Should().Be("1+2⌟5", "the cursor went into the denominator of that fraction");
    }

    [Fact]
    public void DeleteAtTheStartOfASlotGoesToTheSlotBefore()
    {
        MathDocument document = Typed("1").Insert(MathTemplateKind.Fraction);
        document = Typed(document, "2").MoveLeft();

        document = document.Backspace();

        document.Cursor.Depth.Should().Be(1, "the cursor is still in the fraction");
        document.Cursor.Path[0].Slot.Should().Be(0, "at the end of the numerator");
        document.Cursor.Index.Should().Be(1);
        MathLinearWriter.Write(document.Insert("0")).Should().Be("10⌟2");
    }

    [Fact]
    public void DeleteAtTheStartOfTheInputDoesNothing()
    {
        MathDocument document = Typed("12").MoveToStart();

        document.Backspace().Should().BeSameAs(document);
        MathDocument.Empty.Backspace().Should().BeSameAs(MathDocument.Empty);
    }

    [Fact]
    public void TheCursorCanBePutAtEitherEnd()
    {
        MathDocument document = Typed("1+2");

        MathDocument start = document.MoveToStart();

        start.Cursor.Should().Be(MathCursor.Start);
        start.MoveToEnd().Cursor.Index.Should().Be(3);
        start.MoveLeft().Should().BeSameAs(start, "there is nothing to the left of the start");
    }

    [Fact]
    public void EveryTemplateCanBeTypedIntoAndPrinted()
    {
        foreach (MathTemplateKind kind in Enum.GetValues<MathTemplateKind>())
        {
            MathDocument document = MathDocument.Empty.Insert(kind);

            document.Root.Count.Should().Be(1);
            MathTemplates.SlotCount(kind).Should().BeInRange(1, 3);
            document = document.Insert("1");
            MathLinearWriter.Write(document).Should().NotBeEmpty();
            MathLatexWriter.Write(document).Should().NotBeEmpty();
        }
    }

    [Fact]
    public void ATemplateThatDoesNotExistIsRefused()
    {
        Action slots = () => MathTemplates.SlotCount((MathTemplateKind)99);
        Action create = () => MathTemplates.Create((MathTemplateKind)99);
        Action insert = () => MathDocument.Empty.Insert((MathTemplateKind)99);

        slots.Should().Throw<ArgumentOutOfRangeException>();
        create.Should().Throw<ArgumentOutOfRangeException>();
        insert.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ASymbolWithoutTextIsRefused()
    {
        Action empty = () => MathDocument.Empty.Insert(string.Empty);
        Action none = () => MathDocument.Empty.Insert((string)null!);

        empty.Should().Throw<ArgumentException>();
        none.Should().Throw<ArgumentNullException>();
    }
}
