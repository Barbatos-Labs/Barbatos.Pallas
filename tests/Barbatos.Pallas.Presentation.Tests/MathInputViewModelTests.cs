// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The line the user types on: what the keys do to it, what undo takes back, and what it hands to the application.
/// </summary>
public sealed class MathInputViewModelTests
{
    private static MathInputViewModel Pressed(params KeyId[] keys)
    {
        MathInputViewModel input = new();
        foreach (KeyId key in keys)
        {
            input.Press(key);
        }

        return input;
    }

    [Fact]
    public void ANewLineIsEmpty()
    {
        MathInputViewModel input = new();

        input.IsEmpty.Should().BeTrue();
        input.Linear.Should().BeEmpty();
        input.Latex.Should().NotBeEmpty("an empty line still draws a box and a cursor");
        input.Mode.Should().Be(KeyMode.Primary);
        input.CanUndo.Should().BeFalse();
        input.CanRedo.Should().BeFalse();
    }

    [Fact]
    public void TheKeysTypeWhatIsOnThem()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Add, KeyId.Two, KeyId.Multiply, KeyId.Three);

        input.Linear.Should().Be("1+2×3");
        input.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void ShiftGivesTheNextKeyItsSecondMeaningAndNoMore()
    {
        MathInputViewModel input = Pressed(KeyId.Shift);

        input.Mode.Should().Be(KeyMode.Shift);
        input.Press(KeyId.Sin);
        input.Mode.Should().Be(KeyMode.Primary, "a mode lasts for one key");
        input.Press(KeyId.Three);
        input.Press(KeyId.Zero);
        input.Press(KeyId.CloseBracket);

        input.Linear.Should().Be("sin⁻¹(30)");
    }

    [Fact]
    public void ShiftTwiceTurnsItOff()
    {
        MathInputViewModel input = Pressed(KeyId.Shift, KeyId.Shift);

        input.Mode.Should().Be(KeyMode.Primary);
    }

    [Fact]
    public void AlphaTypesTheVariables()
    {
        MathInputViewModel input = Pressed(KeyId.Alpha, KeyId.One, KeyId.Add, KeyId.Alpha, KeyId.Seven);

        input.Linear.Should().Be("A+x");
    }

    [Fact]
    public void AKeyWithoutASecondMeaningTypesNothingInThatMode()
    {
        MathInputViewModel input = Pressed(KeyId.Shift, KeyId.Zero);

        input.Linear.Should().BeEmpty("the zero key has no shift, so the keystroke only ends the mode");
        input.Mode.Should().Be(KeyMode.Primary);
    }

    [Fact]
    public void TheExponentKeyTypesAPowerOfTen()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Exponent, KeyId.Nine, KeyId.Nine);

        input.Linear.Should().Be("1×10^(99)");
    }

    [Fact]
    public void TheFractionKeyTakesWhatWasTyped()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two, KeyId.Fraction, KeyId.Five);

        input.Linear.Should().Be("12⌟5");
    }

    [Fact]
    public void TheCursorKeysMoveWithoutChangingWhatWasTyped()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Fraction, KeyId.Two);
        string before = input.Linear;

        input.Press(KeyId.Up);

        input.Linear.Should().Be(before);
        input.Document.Cursor.Path[0].Slot.Should().Be(0);
        input.Press(KeyId.Left);
        input.Press(KeyId.Right);
        input.Press(KeyId.Down);
        input.Linear.Should().Be(before);
    }

    [Fact]
    public void DeleteAndClearTakeWhatWasTypedAway()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two, KeyId.Delete);

        input.Linear.Should().Be("1");
        input.Press(KeyId.ClearAll);
        input.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void UndoTakesBackWhatWasTypedButNotWhereTheCursorWent()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Add, KeyId.Two);

        input.Press(KeyId.Left);
        input.CanUndo.Should().BeTrue();
        input.Undo();

        input.Linear.Should().Be("1+", "moving the cursor is not an edit");
        input.CanRedo.Should().BeTrue();
        input.Redo();
        input.Linear.Should().Be("1+2");
    }

    [Fact]
    public void UndoIsOnTheClearKeyAndRedoOnTheExecuteKey()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two);

        input.Press(KeyId.Shift);
        input.Press(KeyId.ClearAll);
        input.Linear.Should().Be("1");

        input.Press(KeyId.Shift);
        input.Press(KeyId.Execute);
        input.Linear.Should().Be("12");
    }

    [Theory]
    // Everything that changes what was typed is worth undoing; moving the cursor is not.
    [InlineData(KeyId.Fraction, true)]
    [InlineData(KeyId.Exponent, true)]
    [InlineData(KeyId.Delete, true)]
    [InlineData(KeyId.ClearAll, true)]
    [InlineData(KeyId.Left, false)]
    [InlineData(KeyId.Right, false)]
    [InlineData(KeyId.Up, false)]
    [InlineData(KeyId.Down, false)]
    public void UndoTakesBackTheKeysThatChangedTheLine(KeyId key, bool undoable)
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two);
        string before = input.Linear;

        input.Press(key);
        string after = input.Linear;
        input.Undo();

        if (undoable)
        {
            after.Should().NotBe(before, "{0} changes the line", key);
            input.Linear.Should().Be(before, "undo takes that edit back");
            return;
        }

        after.Should().Be(before, "{0} only moves the cursor", key);
        input.Linear.Should().Be("1", "undo takes back the last edit, which was typing the 2");
    }

    [Fact]
    public void UndoWithNothingToTakeBackDoesNothing()
    {
        MathInputViewModel input = new();

        input.Undo();
        input.Redo();

        input.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TypingAfterAnUndoLeavesNothingToPutBack()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two);
        input.Undo();
        List<string?> changed = input.Changes();

        input.Press(KeyId.Three);

        input.CanRedo.Should().BeFalse();
        changed.Should().Contain(nameof(MathInputViewModel.CanRedo), "a redo button that was lit has to go out");
        input.Linear.Should().Be("13");
    }

    [Fact]
    public void WhatIsPutBackCanBeTakenBackAgain()
    {
        MathInputViewModel input = Pressed(KeyId.One, KeyId.Two);
        input.Undo();
        input.Redo();

        input.Undo();

        input.Linear.Should().Be("1", "redo is an edit that undo takes back, like any other");
        input.CanRedo.Should().BeTrue();
    }

    [Fact]
    public void TheLineIsDrawnWithItsCursor()
    {
        MathInputViewModel input = Pressed(KeyId.One);

        input.Latex.Should().Contain(@"\color{red}{|}", "the screen draws the caret as part of the formula");
    }

    [Fact]
    public void WhatTheLineCannotDoItselfIsHandedOn()
    {
        MathInputViewModel input = Pressed(KeyId.One);
        List<KeyCommand> requests = [];
        input.Requested += (_, command) => requests.Add(command);

        input.Press(KeyId.Execute);
        input.Press(KeyId.Home);
        input.Press(KeyId.Settings);

        requests.Should().Equal(KeyCommand.Execute, KeyCommand.Home, KeyCommand.Settings);
        input.Linear.Should().Be("1", "none of them changes the line");
    }

    [Fact]
    public void AKeyTheKeypadHasNotDoesNothing()
    {
        MathInputViewModel input = Pressed(KeyId.One);

        input.Press(KeyId.None);
        input.Press((KeyId)999);

        input.Linear.Should().Be("1");
    }

    [Fact]
    public void TheLineSaysWhenItChanges()
    {
        MathInputViewModel input = new();
        List<string?> changed = input.Changes();

        input.Press(KeyId.Seven);

        changed.Should().Contain([nameof(MathInputViewModel.Document), nameof(MathInputViewModel.Linear), nameof(MathInputViewModel.Latex), nameof(MathInputViewModel.IsEmpty)]);
    }

    [Fact]
    public void TheLineSaysWhenThereIsSomethingToUndoOrRedo()
    {
        MathInputViewModel input = new();
        List<string?> changed = input.Changes();

        input.Press(KeyId.One);
        changed.Should().Contain(nameof(MathInputViewModel.CanUndo), "a button that undoes has to know");
        changed.Clear();

        input.Undo();
        changed.Should().Contain([nameof(MathInputViewModel.CanUndo), nameof(MathInputViewModel.CanRedo)]);
        changed.Clear();

        input.Redo();
        changed.Should().Contain([nameof(MathInputViewModel.CanUndo), nameof(MathInputViewModel.CanRedo)]);
    }

    [Fact]
    public void ACalculationCanBePutBackOnTheLine()
    {
        MathInputViewModel input = Pressed(KeyId.One);
        MathDocument document = MathDocument.Empty.Insert("2").Insert("+").Insert("3");

        input.Set(document);

        input.Linear.Should().Be("2+3");
        input.Undo();
        input.Linear.Should().Be("1", "what was on the line comes back");
    }

    [Fact]
    public void ADocumentIsRequired()
    {
        MathInputViewModel input = new();
        Action act = () => input.Set(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TheRouterRefusesWhatIsNotAKeyOrAnAction()
    {
        Action document = () => InputCommandRouter.Apply(null!, new InsertSymbol("1"));
        Action action = () => InputCommandRouter.Apply(MathDocument.Empty, null!);
        Action kind = () => InputCommandRouter.Apply(MathDocument.Empty, new UnknownAction());

        document.Should().Throw<ArgumentNullException>();
        action.Should().Throw<ArgumentNullException>();
        kind.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TheRouterSaysWhatEachKindOfKeyDid()
    {
        KeyResult symbol = InputCommandRouter.Apply(MathDocument.Empty, new InsertSymbol("1"));
        KeyResult template = InputCommandRouter.Apply(MathDocument.Empty, new InsertTemplate(MathTemplateKind.Fraction));
        KeyResult sequence = InputCommandRouter.Apply(MathDocument.Empty, new InsertSequence([new InsertSymbol("1"), new InsertSymbol("2")]));
        KeyResult move = InputCommandRouter.Apply(symbol.Document, new RunCommand(KeyCommand.MoveLeft));
        KeyResult execute = InputCommandRouter.Apply(symbol.Document, new RunCommand(KeyCommand.Execute));

        symbol.Edited.Should().BeTrue();
        symbol.Request.Should().BeNull();
        template.Document.Root.Count.Should().Be(1);
        MathLinearWriter.Write(sequence.Document).Should().Be("12");
        move.Edited.Should().BeFalse("moving the cursor is not an edit");
        move.Document.Cursor.Index.Should().Be(0);
        execute.Request.Should().Be(KeyCommand.Execute);
        execute.Document.Should().BeSameAs(symbol.Document);
    }

    private sealed record UnknownAction : KeyAction;
}
