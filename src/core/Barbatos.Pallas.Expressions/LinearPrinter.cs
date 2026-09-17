// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Text;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Writes a syntax tree as Canonical Linear Syntax.
/// </summary>
/// <remarks>
/// <para>The output uses canonical spellings only (<c>×</c> for an input <c>*</c>) and parses back to an equivalent tree
/// (<see cref="SyntaxEquivalence"/>) in the same context. To guarantee that, the printer:</para>
/// <list type="bullet">
/// <item><description>adds parentheses where the priority levels require them, and keeps those the tree holds;</description></item>
/// <item><description>closes every parenthesis, including one omitted at the end of the input;</description></item>
/// <item><description>writes every sexagesimal part, a missing one as <c>0</c>;</description></item>
/// <item><description>parenthesizes where juxtaposition would read differently: <c>2(3)</c> rather than <c>23</c>,
/// <c>2(-3)</c> rather than a subtraction, <c>(2C)3</c> rather than a combination;</description></item>
/// <item><description>inserts a space where two tokens would otherwise run together, as a number before a parenthesized
/// factor would turn into a recurring decimal: <c>2.5 (3)</c>.</description></item>
/// </list>
/// </remarks>
public static class LinearPrinter
{
    /// <summary>Prints a tree.</summary>
    /// <param name="node">The tree.</param>
    /// <param name="context">The context the text will be read in, which decides how tokens run together.</param>
    /// <param name="vocabulary">The vocabulary the text will be read with; <see cref="SyntaxVocabulary.Standard"/> when omitted.</param>
    /// <returns>The expression in Canonical Linear Syntax.</returns>
    public static string Print(SyntaxNode node, SyntaxContext context, SyntaxVocabulary? vocabulary = null)
    {
        ArgumentNullException.ThrowIfNull(node);

        Writer writer = new();
        writer.Write(node, 0);
        return writer.Finish(context, vocabulary ?? SyntaxVocabulary.Standard);
    }

    private enum Piece
    {
        Other,
        Number,
        Minus,
        VariableC,
    }

    private sealed class Writer
    {
        private static readonly FrozenDictionary<BinaryOperator, string> BinarySpellings = SyntaxVocabulary.Standard.Symbols
            .Where(symbol => symbol.BinaryOperator is not null && ReferenceEquals(symbol, symbol.Canonical))
            .Select(symbol => KeyValuePair.Create(symbol.BinaryOperator!.Value, symbol.Text))
            .Append(KeyValuePair.Create(BinaryOperator.Combination, "C"))
            .ToFrozenDictionary();

        private static readonly FrozenDictionary<PostfixOperator, string> PostfixSpellings = SyntaxVocabulary.Standard.Symbols
            .Where(symbol => symbol.PostfixOperator is not null && ReferenceEquals(symbol, symbol.Canonical))
            .ToFrozenDictionary(symbol => symbol.PostfixOperator!.Value, symbol => symbol.Text);

        private static readonly FrozenDictionary<RelationOperator, string> RelationSpellings = SyntaxVocabulary.Standard.Symbols
            .Where(symbol => symbol.RelationOperator is not null && ReferenceEquals(symbol, symbol.Canonical))
            .ToFrozenDictionary(symbol => symbol.RelationOperator!.Value, symbol => symbol.Text);

        private readonly StringBuilder _text = new();
        private readonly List<(int Start, Piece Kind)> _pieces = [];

        public void Write(SyntaxNode node, int minimum)
        {
            if (Precedence.Of(node) < minimum)
            {
                WriteParenthesized(node);
                return;
            }

            switch (node)
            {
                case NumberLiteral number:
                    Emit(number.Text, Piece.Number);
                    break;
                case BaseLiteral literal:
                    Emit(literal.Base switch
                    {
                        NumberBase.Dec => "d",
                        NumberBase.Hex => "h",
                        NumberBase.Bin => "b",
                        _ => "o",
                    });
                    Emit(literal.Digits.Text, Piece.Number);
                    break;
                case NameReference name:
                    Emit(name.Symbol.Text, name.Symbol.Kind == SymbolKind.Variable && name.Symbol.Text == "C" ? Piece.VariableC : Piece.Other);
                    break;
                case CellReference cell:
                    Emit(cell.Text);
                    break;
                case CellRange range:
                    Emit(range.Start.Text);
                    Emit(":");
                    Emit(range.End.Text);
                    break;
                case NegationExpression negation:
                    Emit("-", Piece.Minus);
                    Write(negation.Operand, negation.Operand is NegationExpression ? 0 : Precedence.Prefix + 1);
                    break;
                case PostfixExpression postfix:
                    Write(postfix.Operand, Precedence.Of(postfix.Operator));
                    Emit(PostfixSpellings[postfix.Operator]);
                    break;
                case SuffixCommandExpression suffix:
                    Write(suffix.Operand, Precedence.Of(suffix));
                    Emit(suffix.Command.Text);
                    break;
                case BinaryExpression binary:
                    WriteBinary(binary);
                    break;
                case MixedFractionExpression mixed:
                    Write(mixed.Whole, Precedence.Fraction + 1);
                    Emit("⌟");
                    Write(mixed.Numerator, Precedence.Fraction + 1);
                    Emit("⌟");
                    Write(mixed.Denominator, Precedence.Fraction + 1);
                    break;
                case SexagesimalExpression sexagesimal:
                    Write(sexagesimal.Degrees, Precedence.Postfix);
                    Emit("°");
                    Emit(sexagesimal.Minutes?.Text ?? "0", Piece.Number);
                    Emit("′");
                    Emit(sexagesimal.Seconds?.Text ?? "0", Piece.Number);
                    Emit("″");
                    break;
                case FunctionCall call:
                    Emit(call.Function.Text);
                    for (int index = 0; index < call.Arguments.Length; index++)
                    {
                        if (index > 0)
                        {
                            Emit(",");
                        }

                        Write(call.Arguments[index], Precedence.Relation + 1);
                    }

                    Emit(")");
                    break;
                case ParenthesizedExpression parenthesized:
                    WriteParenthesized(parenthesized.Inner);
                    break;
                default:
                    // The node types are closed; the one left is a relation chain.
                    RelationChain chain = (RelationChain)node;
                    Write(chain.Operands[0], Precedence.Relation + 1);
                    for (int index = 0; index < chain.Operators.Length; index++)
                    {
                        Emit(RelationSpellings[chain.Operators[index]]);
                        Write(chain.Operands[index + 1], Precedence.Relation + 1);
                    }

                    break;
            }
        }

        /// <summary>
        /// Returns the text, with a space wherever the lexer would otherwise read two printed tokens as one.
        /// </summary>
        public string Finish(SyntaxContext context, SyntaxVocabulary vocabulary)
        {
            string text = _text.ToString();
            List<int> starts = [.. _pieces.Select(piece => piece.Start)];

            // Each pass splits every lexed token at the first printed boundary inside it, then lexes again: the rest of
            // that token may lex correctly once separated, as "2.5 (3)" does. A space never joins tokens, so every pass
            // makes progress and the loop ends.
            for (int pass = 0; pass <= starts.Count; pass++)
            {
                List<int> splits = [];
                ExpressionLexer lexer = new(text, context, vocabulary);
                int next = 0;
                for (Token token = lexer.Next(); token.Kind != TokenKind.End; token = lexer.Next())
                {
                    while (next < starts.Count && starts[next] <= token.Span.Start)
                    {
                        next++;
                    }

                    if (next < starts.Count && starts[next] < token.Span.End)
                    {
                        splits.Add(starts[next]);
                    }
                }

                if (splits.Count == 0)
                {
                    break;
                }

                text = InsertSpaces(text, splits, starts);
            }

            return text;
        }

        private static string InsertSpaces(string text, List<int> splits, List<int> starts)
        {
            StringBuilder spaced = new(text.Length + splits.Count);
            int copied = 0;
            foreach (int split in splits)
            {
                spaced.Append(text, copied, split - copied).Append(' ');
                copied = split;
            }

            spaced.Append(text, copied, text.Length - copied);

            for (int index = 0; index < starts.Count; index++)
            {
                starts[index] += splits.Count(split => split <= starts[index]);
            }

            return spaced.ToString();
        }

        private void WriteBinary(BinaryExpression binary)
        {
            int precedence = Precedence.Of(binary.Operator);
            switch (binary.Operator)
            {
                case BinaryOperator.ImplicitMultiply:
                    WriteLeftOfJuxtaposition(binary.Left, precedence);
                    WriteRightOfJuxtaposition(binary.Right, precedence + 1, numbersMayNotTouch: true);
                    break;
                case BinaryOperator.Combination:
                    WriteLeftOfJuxtaposition(binary.Left, precedence);
                    Emit("C", Piece.VariableC);
                    WriteRightOfJuxtaposition(binary.Right, precedence + 1, numbersMayNotTouch: false);
                    break;
                case BinaryOperator.Fraction:
                    Write(binary.Left, precedence + 1);
                    Emit("⌟");
                    Write(binary.Right, precedence + 1);
                    break;
                case BinaryOperator.Root:
                    Write(binary.Left, precedence);
                    Emit("ˣ√");
                    if (binary.Right is ParenthesizedExpression)
                    {
                        Write(binary.Right, 0);
                    }
                    else
                    {
                        WriteParenthesized(binary.Right);
                    }

                    break;
                case BinaryOperator.And or BinaryOperator.Or or BinaryOperator.Xor or BinaryOperator.Xnor:
                    Write(binary.Left, precedence);
                    Emit(" ");
                    Emit(BinarySpellings[binary.Operator]);
                    Emit(" ");
                    Write(binary.Right, precedence + 1);
                    break;
                default:
                    Write(binary.Left, precedence);
                    Emit(BinarySpellings[binary.Operator]);
                    Write(binary.Right, precedence + 1);
                    break;
            }
        }

        /// <summary>Writes the left operand of an implicit multiplication or combination; a trailing variable C there would read as the combination operator.</summary>
        private void WriteLeftOfJuxtaposition(SyntaxNode node, int minimum)
        {
            int length = _text.Length;
            int count = _pieces.Count;
            Write(node, minimum);
            if (_pieces.Count - count > 1 && _pieces[^1].Kind == Piece.VariableC)
            {
                Truncate(length, count);
                WriteParenthesized(node);
            }
        }

        /// <summary>Writes the right operand of an implicit multiplication or combination, which must start an operand.</summary>
        private void WriteRightOfJuxtaposition(SyntaxNode node, int minimum, bool numbersMayNotTouch)
        {
            // The left operand was written first, so there is a piece before this one.
            int length = _text.Length;
            int count = _pieces.Count;
            Piece before = _pieces[^1].Kind;
            Write(node, minimum);

            Piece first = _pieces[count].Kind;
            if (first == Piece.Minus || (numbersMayNotTouch && first == Piece.Number && before == Piece.Number))
            {
                Truncate(length, count);
                WriteParenthesized(node);
            }
        }

        private void WriteParenthesized(SyntaxNode node)
        {
            Emit("(");
            Write(node, 0);
            Emit(")");
        }

        private void Emit(string text, Piece kind = Piece.Other)
        {
            if (text != " ")
            {
                _pieces.Add((_text.Length, kind));
            }

            _text.Append(text);
        }

        private void Truncate(int length, int count)
        {
            _text.Length = length;
            _pieces.RemoveRange(count, _pieces.Count - count);
        }
    }
}
