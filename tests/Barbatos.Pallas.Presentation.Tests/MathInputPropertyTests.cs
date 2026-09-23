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
/// <remarks>
/// A sequence is typed in one application, on that application's keypad: the keys of Matrix pressed on the Calculate
/// line would be ignored, which would make the sample weaker without saying so.
/// </remarks>
public sealed class MathInputPropertyTests
{
    private static readonly ImmutableArray<CalculatorApp> Apps = [.. Enum.GetValues<CalculatorApp>().Where(app => app != CalculatorApp.MathBox)];

    private static readonly Gen<(CalculatorApp App, KeyId[] Keys)> Sequences =
        Gen.Int[0, Apps.Length - 1].SelectMany(appIndex =>
        {
            CalculatorApp app = Apps[appIndex];
            ImmutableArray<KeyId> keys = [.. Keypad.RowsFor(app).SelectMany(row => row)];
            return Gen.Int[0, keys.Length - 1].Array[1, 40].Select(indexes => (app, indexes.Select(index => keys[index]).ToArray()));
        });

    [Fact]
    public void AnySequenceOfKeysLeavesALineThatCanBeWrittenAndDrawn()
    {
        Sequences.Sample(
            sample =>
            {
                MathInputViewModel input = Typed(sample);

                // Neither writer may throw, whatever state the line is in: the screen is drawn on every keystroke.
                return input.Linear is not null && input.Latex.Length > 0;
            },
            iter: 500);
    }

    [Fact]
    public void WhatIsWrittenIsReadBackAsTheSameCalculation()
    {
        Sequences.Sample(
            sample =>
            {
                string linear = Typed(sample).Linear;
                string again = MathLinearWriter.Write(MathDocumentReader.Read(linear, sample.App));

                // Reading closes a bracket the user left open, which the calculator does as well (manual p. 28), so
                // the text may change; what it says may not.
                return Same(linear, again, sample.App);
            },
            iter: 500);
    }

    [Fact]
    public void ReadingBackASecondTimeChangesNothing()
    {
        Sequences.Sample(
            sample =>
            {
                string once = MathLinearWriter.Write(MathDocumentReader.Read(Typed(sample).Linear, sample.App));
                string twice = MathLinearWriter.Write(MathDocumentReader.Read(once, sample.App));

                return twice == once;
            },
            iter: 500);
    }

    [Fact]
    public void TheCursorCanBePutAtAnyPlaceInTheText()
    {
        Sequences.Sample(
            sample =>
            {
                MathDocument document = Typed(sample).Document;
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
            sample =>
            {
                MathInputViewModel input = Typed(sample);
                while (input.CanUndo)
                {
                    input.Undo();
                }

                return input.IsEmpty;
            },
            iter: 500);
    }

    private static bool Same(string left, string right, CalculatorApp app)
    {
        SyntaxContext context = new(app, AllowRelations: true);
        ParseResult first = ExpressionParser.Parse(left, context);
        ParseResult second = ExpressionParser.Parse(right, context);
        if (first.Root is null || second.Root is null)
        {
            return first.Root is null && second.Root is null;
        }

        return SyntaxEquivalence.AreEquivalent(first.Root, second.Root);
    }

    private static MathInputViewModel Typed((CalculatorApp App, KeyId[] Keys) sample)
    {
        MathInputViewModel input = new() { App = sample.App };
        foreach (KeyId key in sample.Keys)
        {
            input.Press(key);
        }

        return input;
    }
}
