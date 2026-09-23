// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;
using CsCheck;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// What holds for any sequence of keys, not only the ones a test thought of.
/// </summary>
public sealed class MathInputPropertyTests
{
    private static readonly ImmutableArray<KeyId> Keys = [.. Keypad.Keys.Select(key => key.Id)];

    private static readonly Gen<KeyId[]> Sequences = Gen.Int[0, Keys.Length - 1].Array[1, 40].Select(indexes => indexes.Select(index => Keys[index]).ToArray());

    [Fact]
    public void AnySequenceOfKeysLeavesALineThatCanBeWrittenAndDrawn()
    {
        Sequences.Sample(
            keys =>
            {
                MathInputViewModel input = Typed(keys);

                // Neither writer may throw, whatever state the line is in: the screen is drawn on every keystroke.
                return input.Linear is not null && input.Latex.Length > 0;
            },
            iter: 500);
    }

    [Fact]
    public void WhatIsWrittenIsReadBackAsTheSameCalculation()
    {
        Sequences.Sample(
            keys =>
            {
                string linear = Typed(keys).Linear;
                string again = MathLinearWriter.Write(MathDocumentReader.Read(linear));

                // Reading closes a bracket the user left open, which the calculator does as well (manual p. 28), so
                // the text may change; what it says may not.
                return Same(linear, again);
            },
            iter: 500);
    }

    [Fact]
    public void ReadingBackASecondTimeChangesNothing()
    {
        Sequences.Sample(
            keys =>
            {
                string once = MathLinearWriter.Write(MathDocumentReader.Read(Typed(keys).Linear));
                string twice = MathLinearWriter.Write(MathDocumentReader.Read(once));

                return twice == once;
            },
            iter: 500);
    }

    [Fact]
    public void TheCursorCanBePutAtAnyPlaceInTheText()
    {
        Sequences.Sample(
            keys =>
            {
                MathDocument document = Typed(keys).Document;
                string linear = MathLinearWriter.Write(document);

                for (int offset = 0; offset <= linear.Length + 1; offset++)
                {
                    MathDocument moved = document.MoveTo(offset);
                    if (MathLinearWriter.Write(moved) != linear || MathLatexWriter.Write(moved).Length == 0)
                    {
                        return false;
                    }
                }

                return true;
            },
            iter: 500);
    }

    [Fact]
    public void EveryKeyCanBeUndoneOneAtATime()
    {
        Sequences.Sample(
            keys =>
            {
                MathInputViewModel input = Typed(keys);
                while (input.CanUndo)
                {
                    input.Undo();
                }

                return input.IsEmpty;
            },
            iter: 500);
    }

    private static bool Same(string left, string right)
    {
        ParseResult first = ExpressionParser.Parse(left, SyntaxContext.Calculate);
        ParseResult second = ExpressionParser.Parse(right, SyntaxContext.Calculate);
        if (first.Root is null || second.Root is null)
        {
            return first.Root is null && second.Root is null;
        }

        return SyntaxEquivalence.AreEquivalent(first.Root, second.Root);
    }

    private static MathInputViewModel Typed(KeyId[] keys)
    {
        MathInputViewModel input = new();
        foreach (KeyId key in keys)
        {
            input.Press(key);
        }

        return input;
    }
}
