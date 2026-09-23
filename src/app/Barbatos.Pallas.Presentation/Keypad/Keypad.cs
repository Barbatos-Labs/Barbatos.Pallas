// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The keypad as data: what every key does, and where the keys sit.
/// </summary>
/// <remarks>
/// <para>
/// The table is the keypad. A screen draws <see cref="Rows"/>, the keyboard maps onto the same
/// <see cref="KeyId"/> values (<see cref="KeyboardMap"/>), and <see cref="InputCommandRouter"/> is the only place
/// that turns a key into an edit, so the on-screen keypad and the keyboard cannot behave differently.
/// </para>
/// <para>
/// What a key types is Canonical Linear Syntax, never a display form: the key for a sine types <c>sin(</c>, which is
/// what the parser reads and what the LaTeX writer draws as sin.
/// </para>
/// </remarks>
public static class Keypad
{
    // The exponent key is three edits: a times, a ten, and a power that takes the ten into its base.
    private static readonly KeyAction Exponent = new InsertSequence(
    [
        new InsertSymbol("×"),
        new InsertSymbol("1"),
        new InsertSymbol("0"),
        new InsertTemplate(MathTemplateKind.Power),
    ]);

    /// <summary>Gets every key, in no particular order.</summary>
    public static ImmutableArray<KeyDefinition> Keys { get; } =
    [
        Command(KeyId.Shift, "SHIFT", KeyCommand.Shift),
        Command(KeyId.Alpha, "ALPHA", KeyCommand.Alpha),
        Command(KeyId.Home, "HOME", KeyCommand.Home),
        Command(KeyId.Settings, "SETUP", KeyCommand.Settings),
        Command(KeyId.Up, "▲", KeyCommand.MoveUp),
        Command(KeyId.Down, "▼", KeyCommand.MoveDown),
        Command(KeyId.Left, "◀", KeyCommand.MoveLeft),
        Command(KeyId.Right, "▶", KeyCommand.MoveRight),
        Command(KeyId.Delete, "DEL", KeyCommand.Delete),
        new KeyDefinition(KeyId.ClearAll, "AC", new RunCommand(KeyCommand.ClearAll), new RunCommand(KeyCommand.Undo), "UNDO"),
        new KeyDefinition(KeyId.Execute, "=", new RunCommand(KeyCommand.Execute), new RunCommand(KeyCommand.Redo), "REDO"),

        new KeyDefinition(KeyId.Zero, "0", new InsertSymbol("0")),
        Digit(KeyId.One, "1", "A"),
        Digit(KeyId.Two, "2", "B"),
        Digit(KeyId.Three, "3", "C"),
        Digit(KeyId.Four, "4", "D"),
        Digit(KeyId.Five, "5", "E"),
        Digit(KeyId.Six, "6", "F"),
        Digit(KeyId.Seven, "7", "x"),
        Digit(KeyId.Eight, "8", "y"),
        Digit(KeyId.Nine, "9", "z"),
        new KeyDefinition(KeyId.Point, ".", new InsertSymbol(".")),
        new KeyDefinition(KeyId.Exponent, "×10ˣ", Exponent, new InsertSymbol("π"), "π"),
        new KeyDefinition(KeyId.Negate, "(−)", new InsertSymbol("−")),

        new KeyDefinition(KeyId.Add, "+", new InsertSymbol("+")),
        new KeyDefinition(KeyId.Subtract, "−", new InsertSymbol("−")),
        new KeyDefinition(KeyId.Multiply, "×", new InsertSymbol("×"), new InsertSymbol("P"), "nPr"),
        new KeyDefinition(KeyId.Divide, "÷", new InsertSymbol("÷"), new InsertSymbol("C"), "nCr"),
        new KeyDefinition(KeyId.OpenBracket, "(", new InsertTemplate(MathTemplateKind.Parentheses), new InsertTemplate(MathTemplateKind.Abs), "|x|"),
        new KeyDefinition(KeyId.CloseBracket, ")", new InsertSymbol(")"), new InsertSymbol(","), ","),

        new KeyDefinition(KeyId.Fraction, "▭⌟▭", new InsertTemplate(MathTemplateKind.Fraction), new InsertTemplate(MathTemplateKind.MixedFraction), "▭▭⌟▭"),
        new KeyDefinition(KeyId.Square, "x²", new InsertSymbol("²"), new InsertSymbol("³"), "x³"),
        new KeyDefinition(KeyId.Power, "x▪", new InsertTemplate(MathTemplateKind.Power), new InsertSymbol("⁻¹"), "x⁻¹"),
        new KeyDefinition(KeyId.SquareRoot, "√▭", new InsertTemplate(MathTemplateKind.SquareRoot), new InsertTemplate(MathTemplateKind.Root), "ˣ√▭"),
        new KeyDefinition(KeyId.Log, "log", new InsertSymbol("log("), new InsertTemplate(MathTemplateKind.LogBase), "logₐb"),
        new KeyDefinition(KeyId.Ln, "ln", new InsertSymbol("ln("), new InsertSymbol("e"), "e"),
        new KeyDefinition(KeyId.Sin, "sin", new InsertSymbol("sin("), new InsertSymbol("sin⁻¹("), "sin⁻¹"),
        new KeyDefinition(KeyId.Cos, "cos", new InsertSymbol("cos("), new InsertSymbol("cos⁻¹("), "cos⁻¹"),
        new KeyDefinition(KeyId.Tan, "tan", new InsertSymbol("tan("), new InsertSymbol("tan⁻¹("), "tan⁻¹"),
        new KeyDefinition(KeyId.Answer, "Ans", new InsertSymbol("Ans"), new InsertSymbol("PreAns"), "PreAns"),
        new KeyDefinition(KeyId.Degree, "°", new InsertSymbol("°"), new InsertSymbol("!"), "x!"),
        new KeyDefinition(KeyId.Percent, "%", new InsertSymbol("%"), new InsertSymbol("Ran#"), "Ran#"),
        new KeyDefinition(KeyId.Integral, "∫", new InsertTemplate(MathTemplateKind.Integral), new InsertTemplate(MathTemplateKind.Derivative), "d/dx"),
        new KeyDefinition(KeyId.Sum, "Σ", new InsertTemplate(MathTemplateKind.Sum), new InsertTemplate(MathTemplateKind.Product), "Π"),
    ];

    /// <summary>Gets the keys row by row, as the screen draws them.</summary>
    public static ImmutableArray<ImmutableArray<KeyId>> Rows { get; } =
    [
        [KeyId.Shift, KeyId.Alpha, KeyId.Up, KeyId.Home, KeyId.Settings],
        [KeyId.Left, KeyId.Down, KeyId.Right, KeyId.Delete, KeyId.ClearAll],
        [KeyId.Fraction, KeyId.Square, KeyId.Power, KeyId.SquareRoot, KeyId.Log],
        [KeyId.Ln, KeyId.Sin, KeyId.Cos, KeyId.Tan, KeyId.Degree],
        [KeyId.Integral, KeyId.Sum, KeyId.Percent, KeyId.OpenBracket, KeyId.CloseBracket],
        [KeyId.Seven, KeyId.Eight, KeyId.Nine, KeyId.Multiply, KeyId.Divide],
        [KeyId.Four, KeyId.Five, KeyId.Six, KeyId.Add, KeyId.Subtract],
        [KeyId.One, KeyId.Two, KeyId.Three, KeyId.Negate, KeyId.Answer],
        [KeyId.Zero, KeyId.Point, KeyId.Exponent, KeyId.Execute],
    ];

    private static readonly FrozenDictionary<KeyId, KeyDefinition> ById = Keys.ToFrozenDictionary(key => key.Id);

    /// <summary>Returns what a key does.</summary>
    /// <param name="id">The key.</param>
    /// <returns>Its definition.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is not a key of this keypad.</exception>
    public static KeyDefinition Of(KeyId id)
    {
        return ById.TryGetValue(id, out KeyDefinition? key)
            ? key
            : throw new ArgumentOutOfRangeException(nameof(id), id, "Not a key of the keypad.");
    }

    /// <summary>Returns what a key does, or <see langword="null"/> when the keypad has no such key.</summary>
    /// <param name="id">The key.</param>
    /// <returns>Its definition, or <see langword="null"/>.</returns>
    public static KeyDefinition? Find(KeyId id) => ById.GetValueOrDefault(id);

    private static KeyDefinition Command(KeyId id, string glyph, KeyCommand command) =>
        new(id, glyph, new RunCommand(command));

    private static KeyDefinition Digit(KeyId id, string digit, string variable) =>
        new(id, digit, new InsertSymbol(digit), Alpha: new InsertSymbol(variable), AlphaGlyph: variable);
}
