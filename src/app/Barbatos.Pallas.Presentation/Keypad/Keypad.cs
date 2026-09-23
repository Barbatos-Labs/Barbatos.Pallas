// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;

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
        Command(KeyId.Shift, "SHIFT", KeyCommand.Shift, KeyGroup.Modifier),
        Command(KeyId.Alpha, "ALPHA", KeyCommand.Alpha, KeyGroup.Modifier),
        Command(KeyId.Home, "HOME", KeyCommand.Home),
        Command(KeyId.Settings, "SETUP", KeyCommand.Settings),
        Command(KeyId.Up, "▲", KeyCommand.MoveUp),
        Command(KeyId.Down, "▼", KeyCommand.MoveDown),
        Command(KeyId.Left, "◀", KeyCommand.MoveLeft),
        Command(KeyId.Right, "▶", KeyCommand.MoveRight),
        Command(KeyId.Delete, "DEL", KeyCommand.Delete, KeyGroup.Clear),
        new KeyDefinition(KeyId.ClearAll, "AC", new RunCommand(KeyCommand.ClearAll), new RunCommand(KeyCommand.Undo), "UNDO", Group: KeyGroup.Clear),
        new KeyDefinition(KeyId.Execute, "=", new RunCommand(KeyCommand.Execute), new RunCommand(KeyCommand.Redo), "REDO", Group: KeyGroup.Execute),

        new KeyDefinition(KeyId.Zero, "0", new InsertSymbol("0"), Group: KeyGroup.Digit),
        Digit(KeyId.One, "1", "A"),
        Digit(KeyId.Two, "2", "B"),
        Digit(KeyId.Three, "3", "C"),
        Digit(KeyId.Four, "4", "D"),
        Digit(KeyId.Five, "5", "E"),
        Digit(KeyId.Six, "6", "F"),
        Digit(KeyId.Seven, "7", "x"),
        Digit(KeyId.Eight, "8", "y"),
        Digit(KeyId.Nine, "9", "z"),
        new KeyDefinition(KeyId.Point, ".", new InsertSymbol("."), Group: KeyGroup.Digit),
        new KeyDefinition(KeyId.Exponent, "×10ˣ", Exponent, new InsertSymbol("π"), "π"),
        new KeyDefinition(KeyId.Negate, "(−)", new InsertSymbol("−"), Group: KeyGroup.Digit),

        new KeyDefinition(KeyId.Add, "+", new InsertSymbol("+"), Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.Subtract, "−", new InsertSymbol("−"), Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.Multiply, "×", new InsertSymbol("×"), new InsertSymbol("P"), "nPr", Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.Divide, "÷", new InsertSymbol("÷"), new InsertSymbol("C"), "nCr", Group: KeyGroup.Operator),
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
        Command(KeyId.SwapForm, "S⇔D", KeyCommand.SwapForm),

        // Base-N (pp. 51-56). The hexadecimal digits are keys of their own, because the variables A to F do not
        // exist there: what the key types is read as a digit by the lexer in this application (assumption U12).
        Hex(KeyId.HexA, "A"),
        Hex(KeyId.HexB, "B"),
        Hex(KeyId.HexC, "C"),
        Hex(KeyId.HexD, "D"),
        Hex(KeyId.HexE, "E"),
        Hex(KeyId.HexF, "F"),
        new KeyDefinition(KeyId.LogicAnd, "and", new InsertSymbol("and"), new InsertSymbol("or"), "or", Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.LogicXor, "xor", new InsertSymbol("xor"), new InsertSymbol("xnor"), "xnor", Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.LogicNot, "Not", new InsertSymbol("Not("), new InsertSymbol("Neg("), "Neg"),

        // Matrix and Vector (pp. 135-139).
        new KeyDefinition(KeyId.MatrixA, "MatA", new InsertSymbol("MatA"), new InsertSymbol("MatAns"), "MatAns"),
        new KeyDefinition(KeyId.MatrixB, "MatB", new InsertSymbol("MatB")),
        new KeyDefinition(KeyId.MatrixC, "MatC", new InsertSymbol("MatC")),
        new KeyDefinition(KeyId.MatrixD, "MatD", new InsertSymbol("MatD")),
        new KeyDefinition(KeyId.Determinant, "Det", new InsertSymbol("Det("), new InsertSymbol("Trn("), "Trn"),
        new KeyDefinition(KeyId.Identity, "Iden", new InsertSymbol("Identity(")),
        new KeyDefinition(KeyId.VectorA, "VctA", new InsertSymbol("VctA"), new InsertSymbol("VctAns"), "VctAns"),
        new KeyDefinition(KeyId.VectorB, "VctB", new InsertSymbol("VctB")),
        new KeyDefinition(KeyId.VectorC, "VctC", new InsertSymbol("VctC")),
        new KeyDefinition(KeyId.VectorD, "VctD", new InsertSymbol("VctD")),
        new KeyDefinition(KeyId.DotProduct, "•", new InsertSymbol("•"), Group: KeyGroup.Operator),
        new KeyDefinition(KeyId.VectorAngle, "Angle", new InsertSymbol("Angle("), new InsertSymbol("UnitV("), "UnitV"),

        // Complex (pp. 129-134).
        new KeyDefinition(KeyId.Imaginary, "i", new InsertSymbol("i"), Group: KeyGroup.Digit),
        new KeyDefinition(KeyId.Polar, "∠", new InsertSymbol("∠"), new InsertSymbol("ReP("), "ReP", new InsertSymbol("ImP("), "ImP", KeyGroup.Operator),
        new KeyDefinition(KeyId.Conjugate, "Conjg", new InsertSymbol("Conjg("), new InsertSymbol("Arg("), "Arg"),

        // Statistics (pp. 90-95). The statistic variables these five keys do not carry are reached through the
        // CATALOG, as they are on the calculator's own menu.
        new KeyDefinition(KeyId.DataCount, "n", new InsertSymbol("n"), new InsertSymbol("Σx"), "Σx", new InsertSymbol("Σy"), "Σy"),
        new KeyDefinition(KeyId.MeanX, "x̄", new InsertSymbol("x̄"), new InsertSymbol("σx"), "σx", new InsertSymbol("sx"), "sx"),
        new KeyDefinition(KeyId.MeanY, "ȳ", new InsertSymbol("ȳ"), new InsertSymbol("σy"), "σy", new InsertSymbol("sy"), "sy"),
        new KeyDefinition(KeyId.EstimateX, "x̂", new InsertSymbol("x̂"), new InsertSymbol("ŷ"), "ŷ", new InsertSymbol("▶t"), "▶t"),
        new KeyDefinition(KeyId.Probability, "P(", new InsertSymbol("P("), new InsertSymbol("Q("), "Q(", new InsertSymbol("R("), "R("),
    ];

    // Every application's keypad has the cursor rows above and the number rows below; what differs is between them.
    // These are declared before the tables built from them: static fields are initialized in the order they appear.
    private static readonly ImmutableArray<ImmutableArray<KeyId>> CursorRows =
    [
        [KeyId.Shift, KeyId.Alpha, KeyId.Up, KeyId.Home, KeyId.Settings],
        [KeyId.Left, KeyId.Down, KeyId.Right, KeyId.Delete, KeyId.ClearAll],
    ];

    private static readonly ImmutableArray<ImmutableArray<KeyId>> FunctionRows =
    [
        [KeyId.Fraction, KeyId.Square, KeyId.Power, KeyId.SquareRoot, KeyId.Log],
        [KeyId.Ln, KeyId.Sin, KeyId.Cos, KeyId.Tan, KeyId.Degree],
        [KeyId.Integral, KeyId.Sum, KeyId.Percent, KeyId.OpenBracket, KeyId.CloseBracket],
    ];

    private static readonly ImmutableArray<ImmutableArray<KeyId>> NumberRows =
    [
        [KeyId.Seven, KeyId.Eight, KeyId.Nine, KeyId.Multiply, KeyId.Divide],
        [KeyId.Four, KeyId.Five, KeyId.Six, KeyId.Add, KeyId.Subtract],
        [KeyId.One, KeyId.Two, KeyId.Three, KeyId.Negate, KeyId.Answer],
        [KeyId.Zero, KeyId.Point, KeyId.Exponent, KeyId.SwapForm, KeyId.Execute],
    ];

    // Base-N replaces the function rows rather than adding to them: the CATALOG commands of pp. 51-69 are not there
    // (p. 51), and neither are the point, the exponent or a second form of the result (assumption U12). A key the
    // calculator would refuse is not offered, rather than offered and then answered with a Syntax ERROR.
    private static readonly ImmutableArray<ImmutableArray<KeyId>> BaseNRows =
    [
        .. CursorRows,
        [KeyId.HexA, KeyId.HexB, KeyId.HexC, KeyId.HexD, KeyId.HexE, KeyId.HexF],
        [KeyId.LogicAnd, KeyId.LogicXor, KeyId.LogicNot, KeyId.OpenBracket, KeyId.CloseBracket],
        [KeyId.Seven, KeyId.Eight, KeyId.Nine, KeyId.Multiply, KeyId.Divide],
        [KeyId.Four, KeyId.Five, KeyId.Six, KeyId.Add, KeyId.Subtract],
        [KeyId.One, KeyId.Two, KeyId.Three, KeyId.Negate, KeyId.Answer],
        [KeyId.Zero, KeyId.Execute],
    ];

    // The applications that calculate expressions of their own keep every key of Calculate and add one row of
    // their own, directly under the cursor: the names only they have (pp. 90, 129, 135, 138).
    private static readonly FrozenDictionary<CalculatorApp, ImmutableArray<KeyId>> OwnRows = new Dictionary<CalculatorApp, ImmutableArray<KeyId>>
    {
        [CalculatorApp.Statistics] = [KeyId.DataCount, KeyId.MeanX, KeyId.MeanY, KeyId.EstimateX, KeyId.Probability],
        [CalculatorApp.Complex] = [KeyId.Imaginary, KeyId.Polar, KeyId.Conjugate],
        [CalculatorApp.Matrix] = [KeyId.MatrixA, KeyId.MatrixB, KeyId.MatrixC, KeyId.MatrixD, KeyId.Determinant, KeyId.Identity],
        [CalculatorApp.Vector] = [KeyId.VectorA, KeyId.VectorB, KeyId.VectorC, KeyId.VectorD, KeyId.DotProduct, KeyId.VectorAngle],
    }.ToFrozenDictionary();

    // In Base-N a key is only what it is: |x| and the comma after Shift belong to the CATALOG commands, which Base-N
    // has not (p. 51), and so do nPr and nCr - where nCr would be worse than refused, because C is a digit there and
    // the key would type the number twelve under a legend that says combinations (found by ApplicationKeypadTests).
    private static readonly KeyDefinition BaseNOpenBracket = new(KeyId.OpenBracket, "(", new InsertTemplate(MathTemplateKind.Parentheses));
    private static readonly KeyDefinition BaseNCloseBracket = new(KeyId.CloseBracket, ")", new InsertSymbol(")"));
    private static readonly KeyDefinition BaseNMultiply = new(KeyId.Multiply, "×", new InsertSymbol("×"), Group: KeyGroup.Operator);
    private static readonly KeyDefinition BaseNDivide = new(KeyId.Divide, "÷", new InsertSymbol("÷"), Group: KeyGroup.Operator);

    /// <summary>Gets the keys of the Calculate application row by row, as the screen draws them.</summary>
    public static ImmutableArray<ImmutableArray<KeyId>> Rows { get; } = [.. CursorRows, .. FunctionRows, .. NumberRows];

    private static readonly FrozenDictionary<KeyId, KeyDefinition> ById = Keys.ToFrozenDictionary(key => key.Id);

    /// <summary>Returns the keys of an application row by row, as its screen draws them.</summary>
    /// <param name="app">The application.</param>
    /// <returns>Its rows of keys.</returns>
    /// <remarks>
    /// An application has the keys of what it reads and no others: Base-N has no functions, and only Matrix has
    /// MatA. What a key types is checked against what the parser reads in that application by the tests.
    /// </remarks>
    public static ImmutableArray<ImmutableArray<KeyId>> RowsFor(CalculatorApp app)
    {
        if (app is CalculatorApp.BaseN)
        {
            return BaseNRows;
        }

        return OwnRows.TryGetValue(app, out ImmutableArray<KeyId> own)
            ? [.. CursorRows, own, .. FunctionRows, .. NumberRows]
            : Rows;
    }

    /// <summary>Returns whether an application's keypad has a key.</summary>
    /// <param name="app">The application.</param>
    /// <param name="id">The key.</param>
    /// <returns><see langword="true"/> when the key is on the application's keypad.</returns>
    /// <remarks>The keyboard is filtered through this, so it cannot reach a key the screen does not show.</remarks>
    public static bool Has(CalculatorApp app, KeyId id) => RowsFor(app).Any(row => row.Contains(id));

    /// <summary>Returns what a key does in an application.</summary>
    /// <param name="id">The key.</param>
    /// <param name="app">The application.</param>
    /// <returns>Its definition there.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is not a key of this keypad.</exception>
    public static KeyDefinition Of(KeyId id, CalculatorApp app)
    {
        return (app, id) switch
        {
            (CalculatorApp.BaseN, KeyId.OpenBracket) => BaseNOpenBracket,
            (CalculatorApp.BaseN, KeyId.CloseBracket) => BaseNCloseBracket,
            (CalculatorApp.BaseN, KeyId.Multiply) => BaseNMultiply,
            (CalculatorApp.BaseN, KeyId.Divide) => BaseNDivide,
            _ => Of(id),
        };
    }

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

    private static KeyDefinition Command(KeyId id, string glyph, KeyCommand command, KeyGroup group = KeyGroup.Control) =>
        new(id, glyph, new RunCommand(command), Group: group);

    private static KeyDefinition Digit(KeyId id, string digit, string variable) =>
        new(id, digit, new InsertSymbol(digit), Alpha: new InsertSymbol(variable), AlphaGlyph: variable, Group: KeyGroup.Digit);

    private static KeyDefinition Hex(KeyId id, string digit) =>
        new(id, digit, new InsertSymbol(digit), Group: KeyGroup.Digit);
}
