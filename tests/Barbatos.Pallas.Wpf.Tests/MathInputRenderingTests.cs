// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// Whatever the keypad can type, the screen can draw: every key, every template, and the cursor wherever it stands.
/// </summary>
/// <remarks>
/// This is what keeps the keypad and the LaTeX writer in step. A new key with a spelling the writer does not know
/// would otherwise show up as an empty display in the running application, and nowhere else.
/// </remarks>
public sealed class MathInputRenderingTests
{
    public static TheoryData<string> Symbols()
    {
        TheoryData<string> data = [];
        foreach (string text in Keypad.Keys
            .SelectMany(key => new[] { key.Primary, key.Shift, key.Alpha })
            .SelectMany(Flatten)
            .OfType<InsertSymbol>()
            .Select(symbol => symbol.Text)
            .Distinct(System.StringComparer.Ordinal))
        {
            data.Add(text);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Symbols))]
    public void EverySymbolAKeyTypesIsDrawn(string text)
    {
        MathDocument document = MathDocument.Empty.Insert(text);

        string latex = MathLatexWriter.Write(document, caret: false);

        Formula.Fault(latex).Should().BeNull("'{0}' draws as '{1}'", text, latex);
    }

    public static TheoryData<CalculatorApp> Applications =>
    [
        .. Enum.GetValues<CalculatorApp>().Where(app => app != CalculatorApp.MathBox),
    ];

    [Theory]
    [MemberData(nameof(Applications))]
    public void EveryEntryOfEveryCatalogIsDrawnOnTheLine(CalculatorApp app)
    {
        // The CATALOG types what no key does - every CODATA constant, every unit conversion - so what it puts on the
        // line is drawn here as well as what the keys put there.
        SyntaxVocabulary vocabulary = PallasEngineBuilder.CreateDefault().Build().Vocabulary;
        List<string> faults = [];
        foreach (CatalogItem item in CalculatorCatalog.For(vocabulary, app).SelectMany(section => section.Items))
        {
            MathDocument document = InputCommandRouter.Apply(MathDocument.Empty, item.Action).Document;
            string latex = MathLatexWriter.Write(document, caret: false, app);
            if (Formula.Fault(latex) is { } fault)
            {
                faults.Add($"{item.Label} draws as '{latex}': {fault}");
            }
        }

        faults.Should().BeEmpty();
    }

    [Theory]
    [InlineData(MathTemplateKind.Parentheses)]
    [InlineData(MathTemplateKind.Fraction)]
    [InlineData(MathTemplateKind.MixedFraction)]
    [InlineData(MathTemplateKind.SquareRoot)]
    [InlineData(MathTemplateKind.Root)]
    [InlineData(MathTemplateKind.Power)]
    [InlineData(MathTemplateKind.Abs)]
    [InlineData(MathTemplateKind.LogBase)]
    [InlineData(MathTemplateKind.Integral)]
    [InlineData(MathTemplateKind.Sum)]
    [InlineData(MathTemplateKind.Product)]
    [InlineData(MathTemplateKind.Derivative)]
    public void EveryTemplateIsDrawnEmptyAndFilled(MathTemplateKind kind)
    {
        MathDocument empty = MathDocument.Empty.Insert(kind);
        MathDocument filled = empty.Insert("2");

        Formula.Fault(MathLatexWriter.Write(empty, caret: false)).Should().BeNull();
        Formula.Fault(MathLatexWriter.Write(filled, caret: false)).Should().BeNull();
        Formula.Fault(MathLatexWriter.Write(filled)).Should().BeNull("the cursor is part of the formula");
    }

    [Fact]
    public void TheCursorIsDrawnWhereverItStands()
    {
        // A fraction inside a power inside a root: the cursor is walked through every place it can be.
        MathDocument document = MathDocument.Empty
            .Insert(MathTemplateKind.SquareRoot)
            .Insert("2")
            .Insert(MathTemplateKind.Power)
            .Insert(MathTemplateKind.Fraction)
            .Insert("3");

        for (MathDocument walked = document.MoveToStart(); ; walked = walked.MoveRight())
        {
            Formula.Fault(MathLatexWriter.Write(walked)).Should().BeNull();
            if (walked.Cursor.Depth == 0 && walked.Cursor.Index == walked.Root.Count)
            {
                break;
            }
        }
    }

    [Fact]
    public void AnEmptyLineIsDrawn()
    {
        Formula.Fault(MathLatexWriter.Write(MathDocument.Empty)).Should().BeNull();
        Formula.Fault(MathLatexWriter.Write(MathDocument.Empty, caret: false)).Should().BeNull();
    }

    [Fact]
    public void AWholeCalculationIsDrawn()
    {
        MathInputViewModel input = new();
        foreach (KeyId key in new[] { KeyId.One, KeyId.Add, KeyId.Two, KeyId.Fraction, KeyId.Three, KeyId.Right, KeyId.Multiply, KeyId.Sin, KeyId.Three, KeyId.Zero, KeyId.Degree, KeyId.CloseBracket, KeyId.Exponent, KeyId.Two })
        {
            input.Press(key);
        }

        input.Linear.Should().Be("1+2⌟3×sin(30°)×10^(2)");
        Formula.Fault(input.Latex).Should().BeNull(input.Latex);
    }

    private static IEnumerable<KeyAction?> Flatten(KeyAction? action)
    {
        if (action is InsertSequence sequence)
        {
            return sequence.Actions;
        }

        return [action];
    }
}
