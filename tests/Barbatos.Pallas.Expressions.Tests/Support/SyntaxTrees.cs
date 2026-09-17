// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using CsCheck;

namespace Barbatos.Pallas.Expressions.Tests.Support;

/// <summary>
/// CsCheck generators of syntax trees that can occur in an application: only symbols available there, numbers the
/// lexer reads there, and a relation chain only at the root.
/// </summary>
internal static class SyntaxTrees
{
    private const int Depth = 4;

    public static Gen<SyntaxNode> For(SyntaxContext context)
    {
        CalculatorApp app = context.App;
        Gen<SyntaxNode> leaf = Leaf(app);

        Gen<SyntaxNode> tree = leaf;
        for (int depth = 1; depth <= Depth; depth++)
        {
            tree = Gen.Frequency((2, leaf), (3, Composite(app, tree)));
        }

        return context.AllowRelations ? Gen.Frequency((3, tree), (1, Chain(tree))) : tree;
    }

    private static Gen<SyntaxNode> Leaf(CalculatorApp app)
    {
        SyntaxSymbol[] names =
        [
            .. Available(app).Where(symbol => symbol.Kind is SymbolKind.Constant or SymbolKind.ScientificConstant or SymbolKind.Variable
                or SymbolKind.Memory or SymbolKind.MatrixVariable or SymbolKind.VectorVariable or SymbolKind.StatisticsVariable)

                // In Base-N, A-F are digits; the variables cannot be typed.
                .Where(symbol => app != CalculatorApp.BaseN || symbol.Text is not ("A" or "B" or "C" or "D" or "E" or "F")),
        ];

        List<(int, IGen<SyntaxNode>)> leaves =
        [
            (3, Number(app).Select(text => (SyntaxNode)new NumberLiteral(text))),
            (2, Gen.OneOfConst(names).Select(symbol => (SyntaxNode)new NameReference(symbol))),
        ];

        if (app == CalculatorApp.BaseN)
        {
            leaves.Add((1, Gen.Select(Gen.OneOfConst(NumberBase.Dec, NumberBase.Hex, NumberBase.Bin, NumberBase.Oct), Number(app),
                (numberBase, digits) => (SyntaxNode)new BaseLiteral(numberBase, new NumberLiteral(digits)))));
        }

        if (app == CalculatorApp.Spreadsheet)
        {
            Gen<string> cells = Gen.OneOfConst("A1", "$B$2", "C$3", "$D4", "E45");
            leaves.Add((1, cells.Select(cell => (SyntaxNode)new CellReference(cell))));
            leaves.Add((1, Gen.Select(cells, cells, (start, end) => (SyntaxNode)new CellRange(new CellReference(start), new CellReference(end)))));
        }

        return Gen.Frequency([.. leaves]);
    }

    private static Gen<string> Number(CalculatorApp app)
    {
        return app == CalculatorApp.BaseN
            ? Gen.OneOfConst("0", "1", "10", "101", "1F", "ABC", "7FFFFFFF", "20")
            : Gen.OneOf(
                Gen.OneOfConst("0", "2", "12", "1.25", ".5", "3.", "0.(3)", "1.2(34)", "999999", "20"),
                Gen.Int[0, 99999].Select(value => value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static Gen<SyntaxNode> Composite(CalculatorApp app, Gen<SyntaxNode> sub)
    {
        BinaryOperator[] binary =
        [
            BinaryOperator.Add, BinaryOperator.Subtract, BinaryOperator.Multiply, BinaryOperator.Divide, BinaryOperator.DivideWithRemainder,
            BinaryOperator.ImplicitMultiply, BinaryOperator.Power, BinaryOperator.Root, BinaryOperator.Fraction, BinaryOperator.Permutation,
            .. app switch
            {
                CalculatorApp.Complex => [BinaryOperator.Polar, BinaryOperator.Combination],
                CalculatorApp.Vector => [BinaryOperator.DotProduct, BinaryOperator.Combination],

                // In Base-N, C is a hexadecimal digit, so the combination operator cannot be written.
                CalculatorApp.BaseN => [BinaryOperator.And, BinaryOperator.Or, BinaryOperator.Xor, BinaryOperator.Xnor],
                _ => (BinaryOperator[])[BinaryOperator.Combination],
            },
        ];

        PostfixOperator[] postfix =
        [
            PostfixOperator.Square, PostfixOperator.Cube, PostfixOperator.Reciprocal, PostfixOperator.Factorial, PostfixOperator.Percent,
            PostfixOperator.Degrees, PostfixOperator.Radians, PostfixOperator.Gradians,
            .. app == CalculatorApp.Statistics
                ? (PostfixOperator[])[PostfixOperator.StandardizedVariate, PostfixOperator.EstimateX, PostfixOperator.EstimateY, PostfixOperator.EstimateX1, PostfixOperator.EstimateX2]
                : [],
        ];

        SyntaxSymbol[] commands = [.. Available(app).Where(symbol => symbol.Kind is SymbolKind.EngineeringSymbol or SymbolKind.UnitConversion)];
        SyntaxSymbol[] functions = [.. Available(app).Where(symbol => symbol.Kind == SymbolKind.Function && symbol.Text != "root(")];
        Gen<NumberLiteral?> part = Gen.OneOfConst<NumberLiteral?>(null, new NumberLiteral("0"), new NumberLiteral("20"), new NumberLiteral("10"));

        return Gen.OneOf(
            sub.Select(operand => (SyntaxNode)new NegationExpression(operand)),
            Gen.Select(Gen.OneOfConst(postfix), sub, (op, operand) => (SyntaxNode)new PostfixExpression(op, operand)),
            Gen.Select(Gen.OneOfConst(commands), sub, (command, operand) => (SyntaxNode)new SuffixCommandExpression(operand, command)),
            Gen.Select(Gen.OneOfConst(binary), sub, sub, (op, left, right) => (SyntaxNode)new BinaryExpression(op, left, right)),
            Gen.Select(Gen.OneOfConst(binary), sub, sub, (op, left, right) => (SyntaxNode)new BinaryExpression(op, left, right)),
            Gen.Select(sub, sub, sub, (whole, numerator, denominator) => (SyntaxNode)new MixedFractionExpression(whole, numerator, denominator)),
            Gen.Select(sub, part, part, (degrees, minutes, seconds) => minutes is null && seconds is null
                ? new PostfixExpression(PostfixOperator.Degrees, degrees)
                : (SyntaxNode)new SexagesimalExpression(degrees, minutes, seconds)),
            Gen.Select(Gen.OneOfConst(functions), sub.Array[1, 3], (function, arguments) => (SyntaxNode)new FunctionCall(function, [.. arguments])),
            sub.Select(inner => (SyntaxNode)new ParenthesizedExpression(inner)));
    }

    private static Gen<SyntaxNode> Chain(Gen<SyntaxNode> operand)
    {
        // Every operator of a chain comes from one family, as manual p. 75 requires.
        RelationOperator[][] families =
        [
            [RelationOperator.Equal, RelationOperator.NotEqual],
            [RelationOperator.Equal, RelationOperator.Less, RelationOperator.LessOrEqual],
            [RelationOperator.Equal, RelationOperator.Greater, RelationOperator.GreaterOrEqual],
        ];

        return Gen.Select(Gen.OneOfConst(families), operand.Array[2, 4], Gen.Int[0, 1000].Array[3], (family, operands, picks) =>
            (SyntaxNode)new RelationChain(
                [.. operands],
                [.. Enumerable.Range(0, operands.Length - 1).Select(index => family[picks[index] % family.Length])]));
    }

    private static IEnumerable<SyntaxSymbol> Available(CalculatorApp app)
    {
        return SyntaxVocabulary.Standard.Symbols.Where(symbol => ReferenceEquals(symbol, symbol.Canonical) && symbol.IsAvailableIn(app));
    }

    public static ImmutableArray<SyntaxContext> Contexts { get; } =
    [
        new(CalculatorApp.Calculate, AllowRelations: true),
        new(CalculatorApp.Complex, AllowRelations: true),
        new(CalculatorApp.BaseN),
        new(CalculatorApp.Statistics),
        new(CalculatorApp.Spreadsheet),
        new(CalculatorApp.Matrix),
        new(CalculatorApp.Vector),
    ];
}
