// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// Each application has the keys of what it reads, and no others.
/// </summary>
/// <remarks>
/// M4 shipped one keypad for every application, and nothing noticed: the Base-N screen offered √, sin and ∫, every
/// one of which the engine then answered with a Syntax ERROR, and the Matrix screen had no way to type MatA at all
/// (found by driving the application, 23 Sep 2026). These are the tests that would have seen both.
/// </remarks>
public sealed class ApplicationKeypadTests
{
    public static TheoryData<CalculatorApp> Applications =>
    [
        .. Enum.GetValues<CalculatorApp>().Where(app => app != CalculatorApp.MathBox),
    ];

    [Theory]
    [MemberData(nameof(Applications))]
    public void EveryKeyOfAnApplicationTypesWhatThatApplicationReads(CalculatorApp app)
    {
        List<string> unread = [];
        foreach (KeyId id in Keypad.RowsFor(app).SelectMany(row => row))
        {
            foreach (KeyMode mode in Enum.GetValues<KeyMode>())
            {
                if (Keypad.Of(id, app).In(mode) is not { } action || Typed(action) is not { Length: > 0 } text)
                {
                    continue;
                }

                // A key of an unfinished calculation - sin( with nothing in it - is a Syntax ERROR the user corrects
                // by typing on. A character the application has no token for is one no amount of typing corrects.
                if (Unread(text, app))
                {
                    unread.Add($"{id} ({mode}) types '{text}'");
                }
            }
        }

        unread.Should().BeEmpty("every key on the keypad of {0} types something {0} reads", app);
    }

    [Fact]
    public void AKeyAnApplicationCannotReadIsSeenAsOne()
    {
        // The check above is only as good as what it looks for: the square root of Calculate, typed in Base-N, is
        // exactly what M4 offered there.
        string root = Typed(Keypad.Of(KeyId.SquareRoot).Primary)!;
        string sine = Typed(Keypad.Of(KeyId.Sin).Primary)!;

        Unread(root, CalculatorApp.BaseN).Should().BeTrue();
        Unread(sine, CalculatorApp.BaseN).Should().BeTrue();
        Unread("MatA", CalculatorApp.Calculate).Should().BeTrue("MatA is a name only Matrix has");
        Unread(root, CalculatorApp.Calculate).Should().BeFalse();
    }

    [Fact]
    public void BaseNOffersNoKeyTheManualTakesAway()
    {
        ImmutableArray<KeyId> keys = [.. Keypad.RowsFor(CalculatorApp.BaseN).SelectMany(row => row)];

        keys.Should().NotContain(
            [KeyId.SquareRoot, KeyId.Sin, KeyId.Cos, KeyId.Tan, KeyId.Log, KeyId.Ln, KeyId.Integral, KeyId.Sum,
             KeyId.Fraction, KeyId.Power, KeyId.Square, KeyId.Percent, KeyId.Degree, KeyId.Point, KeyId.Exponent,
             KeyId.SwapForm],
            "the CATALOG commands, the point and the exponent are not in Base-N (p. 51, assumption U12)");
        keys.Should().Contain(
            [KeyId.HexA, KeyId.HexB, KeyId.HexC, KeyId.HexD, KeyId.HexE, KeyId.HexF, KeyId.LogicAnd, KeyId.LogicXor, KeyId.LogicNot],
            "the hexadecimal digits and the logic operators are what Base-N has instead");
    }

    [Fact]
    public void InBaseNAKeyIsOnlyWhatItIs()
    {
        Keypad.Of(KeyId.OpenBracket, CalculatorApp.BaseN).Shift.Should().BeNull("|x| is a CATALOG command (p. 51)");
        Keypad.Of(KeyId.CloseBracket, CalculatorApp.BaseN).Shift.Should().BeNull("no function of Base-N takes two arguments");
        Keypad.Of(KeyId.Multiply, CalculatorApp.BaseN).Shift.Should().BeNull("nPr is not in Base-N");

        // nCr types C, which Base-N reads without complaint - as the digit twelve. No parser would catch that key.
        Keypad.Of(KeyId.Divide, CalculatorApp.BaseN).Shift.Should().BeNull("C is a digit in Base-N, not combinations");

        Keypad.Of(KeyId.OpenBracket, CalculatorApp.Calculate).Shift.Should().NotBeNull();
        Keypad.Of(KeyId.Divide, CalculatorApp.Calculate).ShiftGlyph.Should().Be("nCr");
        Keypad.Of(KeyId.Seven, CalculatorApp.BaseN).Should().BeSameAs(Keypad.Of(KeyId.Seven));
    }

    [Theory]
    [InlineData(CalculatorApp.Matrix, KeyId.MatrixA)]
    [InlineData(CalculatorApp.Matrix, KeyId.Determinant)]
    [InlineData(CalculatorApp.Vector, KeyId.VectorA)]
    [InlineData(CalculatorApp.Vector, KeyId.DotProduct)]
    [InlineData(CalculatorApp.Complex, KeyId.Imaginary)]
    [InlineData(CalculatorApp.Complex, KeyId.Polar)]
    [InlineData(CalculatorApp.Statistics, KeyId.MeanX)]
    [InlineData(CalculatorApp.Statistics, KeyId.Probability)]
    [InlineData(CalculatorApp.BaseN, KeyId.HexA)]
    [InlineData(CalculatorApp.BaseN, KeyId.LogicAnd)]
    public void ANameAnApplicationHasIsOnItsKeypadAndNoOther(CalculatorApp owner, KeyId id)
    {
        foreach (CalculatorApp app in Enum.GetValues<CalculatorApp>())
        {
            Keypad.Has(app, id).Should().Be(app == owner, "{0} is a key of {1} and of nothing else", id, owner);
        }
    }

    [Fact]
    public void AnApplicationWithNamesOfItsOwnKeepsEveryKeyOfCalculate()
    {
        ImmutableArray<KeyId> calculate = [.. Keypad.Rows.SelectMany(row => row)];

        foreach (CalculatorApp app in (CalculatorApp[])[CalculatorApp.Statistics, CalculatorApp.Complex, CalculatorApp.Matrix, CalculatorApp.Vector])
        {
            ImmutableArray<ImmutableArray<KeyId>> rows = Keypad.RowsFor(app);

            rows.SelectMany(row => row).Should().Contain(calculate, "{0} calculates expressions as Calculate does", app);
            rows.Should().HaveCount(Keypad.Rows.Length + 1, "{0} adds one row of its own", app);
            rows[2].Should().NotIntersectWith(calculate, "the row of its own is under the cursor keys");
        }
    }

    [Fact]
    public void AKeyTheApplicationHasNotDoesNothingThereWhereverItComesFrom()
    {
        MathInputViewModel baseN = new() { App = CalculatorApp.BaseN };
        MathInputViewModel calculate = new();

        baseN.Press(KeyId.SquareRoot);
        baseN.Press(KeyId.Sin);
        baseN.Press(KeyId.Point);
        calculate.Press(KeyId.MatrixA);
        calculate.Press(KeyId.HexA);

        baseN.IsEmpty.Should().BeTrue("the keyboard reaches the keypad through the same table, so a key Base-N has not is not pressed");
        calculate.IsEmpty.Should().BeTrue();
        baseN.CanUndo.Should().BeFalse("nothing was typed, so there is nothing to take back");
    }

    [Fact]
    public void BaseNTypesItsOwnKeys()
    {
        MathInputViewModel input = new() { App = CalculatorApp.BaseN };

        foreach (KeyId key in (KeyId[])[KeyId.HexF, KeyId.LogicAnd, KeyId.HexA, KeyId.One])
        {
            input.Press(key);
        }

        input.Linear.Should().Be("FandA1");
    }

    [Fact]
    public void TheLineChangesItsKeysWithItsApplication()
    {
        MathInputViewModel input = new();
        List<string?> changed = input.Changes();

        input.App = CalculatorApp.Matrix;

        changed.Should().Contain(nameof(MathInputViewModel.Rows));
        input.Rows.SelectMany(row => row).Select(key => key.Id).Should().Contain(KeyId.MatrixA);
        input.Rows[2].Select(key => key.Id).Should().StartWith([KeyId.MatrixA]);
    }

    [Fact]
    public void AKeyShowsWhatItDoesInItsApplication()
    {
        MathInputViewModel input = new() { App = CalculatorApp.BaseN };

        KeyDefinition bracket = input.Rows.SelectMany(row => row).Single(key => key.Id == KeyId.OpenBracket);

        bracket.ShiftGlyph.Should().BeNull("the screen draws the Base-N bracket, which has no |x| on it");
    }

    [Fact]
    public void TheScreenOfAnApplicationHasTheKeysOfThatApplication()
    {
        CalculatorShellViewModel shell = Shell.Create();

        shell.Open(CalculatorApps.Of(CalculatorApp.Matrix));

        shell.Matrix.Calculate.Input.App.Should().Be(CalculatorApp.Matrix);

        shell.Open(CalculatorApps.Of(CalculatorApp.BaseN));

        shell.BaseN.Calculate.Input.App.Should().Be(CalculatorApp.BaseN);
    }

    [Fact]
    public void CalculateAndComplexShareALineButNotTheirKeys()
    {
        CalculatorShellViewModel shell = Shell.Create();
        shell.Calculate.Input.App.Should().Be(CalculatorApp.Calculate);

        shell.Open(CalculatorApps.Of(CalculatorApp.Complex));

        shell.Calculate.Input.App.Should().Be(CalculatorApp.Complex, "the Complex screen is the Calculate screen with the keys of Complex");
        Keypad.Has(shell.Calculate.Input.App, KeyId.Imaginary).Should().BeTrue();

        shell.Open(CalculatorApps.Of(CalculatorApp.Calculate));

        shell.Calculate.Input.App.Should().Be(CalculatorApp.Calculate);
    }

    [Fact]
    public void TheSwapKeyTurnsTheAnswerBetweenItsTwoForms()
    {
        // 1 ÷ 3 is 1⌟3, and S⇔D turns it into its decimal and back (p. 42).
        CalculateViewModel screen = new(Shell.Session());
        screen.Input.Press(KeyId.One);
        screen.Input.Press(KeyId.Divide);
        screen.Input.Press(KeyId.Three);
        screen.Input.Press(KeyId.Execute);
        screen.Display!.Text.Should().Be("1⌟3");

        screen.Input.Press(KeyId.SwapForm);

        screen.Display!.Text.Should().Be("0.3333333333");

        screen.Input.Press(KeyId.SwapForm);

        screen.Display!.Text.Should().Be("1⌟3");
    }

    [Fact]
    public void TheSwapKeyChangesNothingBeforeThereIsAnAnswer()
    {
        CalculateViewModel screen = new(Shell.Session());

        screen.Input.Press(KeyId.SwapForm);

        screen.Display.Should().BeNull();
        screen.Input.IsEmpty.Should().BeTrue("S⇔D is not an edit of the line");
    }

    /// <summary>What a key types on an empty line, as the engine would read it.</summary>
    private static string? Typed(KeyAction action)
    {
        KeyResult result = InputCommandRouter.Apply(MathDocument.Empty, action);
        return result.Request is null ? MathLinearWriter.Write(result.Document) : null;
    }

    /// <summary>Whether an application has no token for something in the text, even with a digit typed after it.</summary>
    /// <remarks>
    /// The digit is for a key that types the start of a token rather than a whole one: the point is not a number
    /// until the digit after it, and <c>.5</c> is. A character the application cannot read stays unreadable.
    /// </remarks>
    private static bool Unread(string text, CalculatorApp app) => HasNoToken(text, app) && HasNoToken(text + "1", app);

    private static bool HasNoToken(string text, CalculatorApp app) =>
        ExpressionParser.Parse(text, new SyntaxContext(app, AllowRelations: true)).Diagnostic is { Code: SyntaxErrorCode.UnexpectedCharacter };
}
