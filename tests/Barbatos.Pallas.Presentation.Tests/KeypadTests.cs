// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The keypad as data: every key appears once, does something, and types syntax the parser knows.
/// </summary>
public sealed class KeypadTests
{
    [Fact]
    public void EveryKeyOfTheKeypadIsOnTheScreenExactlyOnce()
    {
        ImmutableArray<KeyId> placed = [.. Keypad.Rows.SelectMany(row => row)];

        placed.Should().OnlyHaveUniqueItems();
        placed.Should().BeEquivalentTo(Keypad.Keys.Select(key => key.Id), "the table is the keypad");
    }

    [Fact]
    public void EveryKeyOfTheEnumIsOnTheKeypad()
    {
        IEnumerable<KeyId> defined = Enum.GetValues<KeyId>().Where(id => id != KeyId.None);

        Keypad.Keys.Select(key => key.Id).Should().BeEquivalentTo(defined);
    }

    [Fact]
    public void EveryKeyHasAFaceAndSomethingToDo()
    {
        foreach (KeyDefinition key in Keypad.Keys)
        {
            key.Glyph.Should().NotBeEmpty("key {0} is drawn", key.Id);
            key.Primary.Should().NotBeNull();
            key.In(KeyMode.Primary).Should().Be(key.Primary);
            key.GlyphIn(KeyMode.Primary).Should().Be(key.Glyph);
            (key.Shift is null).Should().Be(key.ShiftGlyph is null, "key {0} says what its shift does if it does anything", key.Id);
            (key.Alpha is null).Should().Be(key.AlphaGlyph is null, "key {0} says what its alpha does if it does anything", key.Id);
            key.In(KeyMode.Shift).Should().Be(key.Shift);
            key.In(KeyMode.Alpha).Should().Be(key.Alpha);
            key.GlyphIn(KeyMode.Shift).Should().Be(key.ShiftGlyph);
            key.GlyphIn(KeyMode.Alpha).Should().Be(key.AlphaGlyph);
        }
    }

    [Fact]
    public void EverySymbolAKeyTypesIsSyntaxTheParserKnows()
    {
        foreach (string text in Symbols())
        {
            // Around each symbol goes the least that makes it a calculation, so that what is tested is the symbol.
            string input = Around(text);
            ParseResult result = ExpressionParser.Parse(input, SyntaxContext.Calculate);

            result.Root.Should().NotBeNull("'{0}' should be syntax, as '{1}'", text, input);
            MathLinearWriter.Write(MathDocument.Empty.Insert(text)).Should().Be(text, "what a key types is what it types");
        }
    }

    [Fact]
    public void EveryTemplateAKeyInsertsIsATemplateThatExists()
    {
        foreach (KeyAction action in Actions())
        {
            if (action is InsertTemplate template)
            {
                Enum.IsDefined(template.Kind).Should().BeTrue();
                MathTemplates.SlotCount(template.Kind).Should().BeGreaterThan(0);
            }
        }
    }

    [Fact]
    public void AKeyThatIsNotOnTheKeypadIsRefusedOrNotFound()
    {
        Action act = () => Keypad.Of(KeyId.None);

        act.Should().Throw<ArgumentOutOfRangeException>();
        Keypad.Find(KeyId.None).Should().BeNull();
        Keypad.Of(KeyId.Seven).Glyph.Should().Be("7");
    }

    [Theory]
    [InlineData("D7", false, KeyId.Seven)]
    [InlineData("NumPad7", false, KeyId.Seven)]
    [InlineData("OemPlus", false, KeyId.Add)]
    [InlineData("OemPlus", true, KeyId.Add)]
    [InlineData("D8", true, KeyId.Multiply)]
    [InlineData("D9", true, KeyId.OpenBracket)]
    [InlineData("Enter", false, KeyId.Execute)]
    [InlineData("Back", false, KeyId.Delete)]
    [InlineData("Escape", false, KeyId.ClearAll)]
    [InlineData("left", false, KeyId.Left)]
    [InlineData("F13", false, KeyId.None)]
    [InlineData("", false, KeyId.None)]
    [InlineData(null, false, KeyId.None)]
    public void TheKeyboardReachesTheSameKeys(string? key, bool shift, KeyId expected)
    {
        KeyboardMap.Find(key, shift).Should().Be(expected);
    }

    [Fact]
    public void AKeyboardKeyWithoutAShiftMeaningKeepsItsOwn()
    {
        KeyboardMap.Find("D7", shift: true).Should().Be(KeyId.Seven);
        KeyboardMap.Find("D7").Should().Be(KeyId.Seven);
    }

    [Fact]
    public void EveryKeyboardKeyIsAKeyOfTheKeypad()
    {
        foreach (string name in new[] { "D0", "D1", "D5", "D8", "D9", "NumPad0", "Decimal", "Add", "Subtract", "Multiply", "Divide", "Enter", "Back", "Escape", "Left", "Right", "Up", "Down", "Home", "OemPlus", "OemMinus", "OemPeriod", "OemQuestion" })
        {
            foreach (bool shift in new[] { false, true })
            {
                KeyId id = KeyboardMap.Find(name, shift);
                if (id != KeyId.None)
                {
                    Keypad.Find(id).Should().NotBeNull("'{0}' maps onto a key of the keypad", name);
                }
            }
        }
    }

    private static IEnumerable<KeyAction> Actions()
    {
        foreach (KeyDefinition key in Keypad.Keys)
        {
            foreach (KeyAction? action in new[] { key.Primary, key.Shift, key.Alpha })
            {
                if (action is null)
                {
                    continue;
                }

                if (action is InsertSequence sequence)
                {
                    foreach (KeyAction one in sequence.Actions)
                    {
                        yield return one;
                    }

                    continue;
                }

                yield return action;
            }
        }
    }

    private static IEnumerable<string> Symbols() => Actions().OfType<InsertSymbol>().Select(symbol => symbol.Text).Distinct(StringComparer.Ordinal);

    private static string Around(string text)
    {
        return text switch
        {
            ")" => "(1)",
            "," => "log(2,8)",
            "." => ".5",
            "+" or "−" or "×" or "÷" or "P" or "C" => "10" + text + "4",
            "²" or "³" or "⁻¹" or "!" or "%" or "°" => "2" + text,
            _ when text.EndsWith('(') => text + "1)",
            _ => text,
        };
    }
}
