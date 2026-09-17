// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// One parse of one text. Failures set <see cref="_error"/> and return <see langword="null"/> up the call chain.
/// </summary>
internal sealed class Parser
{
    private readonly string _text;
    private readonly SyntaxContext _context;
    private readonly Token[] _tokens;
    private int _position;
    private int _depth;
    private SyntaxDiagnostic? _error;

    public Parser(string text, SyntaxContext context, SyntaxVocabulary vocabulary)
    {
        _text = text;
        _context = context;

        List<Token> tokens = [];
        ExpressionLexer lexer = new(text, context, vocabulary);
        Token token;
        do
        {
            token = lexer.Next();
            tokens.Add(token);
        }
        while (token.Kind != TokenKind.End);

        _tokens = [.. tokens];
    }

    private Token Current => _tokens[_position];

    private Token Previous => _tokens[_position - 1];

    public ParseResult Parse()
    {
        SyntaxNode? root = Current.Kind == TokenKind.End
            ? Fail(SyntaxErrorCode.EmptyExpression, Current.Span)
            : ParseRelations();

        if (root is not null && Current.Kind != TokenKind.End)
        {
            root = FailAt(Current);
        }

        return new ParseResult(_text, root, root is null ? _error : null);
    }

    private static bool IsOperandStart(Token token)
    {
        return token.Kind switch
        {
            TokenKind.Number or TokenKind.BasePrefix or TokenKind.CellReference => true,
            TokenKind.Symbol => token.Symbol!.Kind is SymbolKind.Function or SymbolKind.Constant or SymbolKind.ScientificConstant
                or SymbolKind.Variable or SymbolKind.Memory or SymbolKind.MatrixVariable or SymbolKind.VectorVariable
                or SymbolKind.StatisticsVariable or SymbolKind.OpenParenthesis,
            _ => false,
        };
    }

    private static bool Is(Token token, SymbolKind kind) => token.Kind == TokenKind.Symbol && token.Symbol!.Kind == kind;

    private static SourceSpan Covering(SyntaxNode start, SourceSpan end) => SourceSpan.Covering(start.Span, end);

    private SyntaxNode? ParseRelations()
    {
        SyntaxNode? first = ParseExpression(Precedence.Relation);
        if (first is null || !Is(Current, SymbolKind.RelationOperator))
        {
            return first;
        }

        if (!_context.AllowRelations)
        {
            return Fail(SyntaxErrorCode.RelationNotAllowed, Current.Span);
        }

        ImmutableArray<SyntaxNode>.Builder operands = ImmutableArray.CreateBuilder<SyntaxNode>();
        ImmutableArray<RelationOperator>.Builder operators = ImmutableArray.CreateBuilder<RelationOperator>();
        operands.Add(first);

        // Manual p. 75: every inequality must point the same way, and ≠ does not combine with an inequality.
        bool less = false;
        bool greater = false;
        bool notEqual = false;
        while (Is(Current, SymbolKind.RelationOperator))
        {
            Token token = Advance();
            RelationOperator relation = token.Symbol!.RelationOperator!.Value;
            switch (relation)
            {
                case RelationOperator.NotEqual:
                    if (less || greater)
                    {
                        return Fail(SyntaxErrorCode.NotEqualWithInequality, token.Span);
                    }

                    notEqual = true;
                    break;
                case RelationOperator.Less or RelationOperator.LessOrEqual:
                    if (greater || notEqual)
                    {
                        return Fail(greater ? SyntaxErrorCode.MixedRelationDirections : SyntaxErrorCode.NotEqualWithInequality, token.Span);
                    }

                    less = true;
                    break;
                case RelationOperator.Greater or RelationOperator.GreaterOrEqual:
                    if (less || notEqual)
                    {
                        return Fail(less ? SyntaxErrorCode.MixedRelationDirections : SyntaxErrorCode.NotEqualWithInequality, token.Span);
                    }

                    greater = true;
                    break;
            }

            operators.Add(relation);
            SyntaxNode? operand = ParseExpression(Precedence.Relation);
            if (operand is null)
            {
                return null;
            }

            operands.Add(operand);
        }

        return new RelationChain(operands.ToImmutable(), operators.ToImmutable(), Covering(first, operands[^1].Span));
    }

    /// <summary>Parses operators binding tighter than <paramref name="minimum"/>, left to right within a level.</summary>
    private SyntaxNode? ParseExpression(int minimum)
    {
        if (_depth >= ExpressionParser.MaxNestingDepth || !RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            return Fail(SyntaxErrorCode.NestingTooDeep, Current.Span);
        }

        _depth++;
        try
        {
            return ParseOperators(ParseOperand(), minimum);
        }
        finally
        {
            _depth--;
        }
    }

    private enum Step
    {
        NotAnOperator,
        BindsTooLoosely,
        Applied,
    }

    private SyntaxNode? ParseOperators(SyntaxNode? left, int minimum)
    {
        // How many ⌟-separated parts the fraction in `left` has, when this loop built it: 2 for a⌟b, 3 for a⌟b⌟c.
        int fractionParts = 0;
        while (left is not null)
        {
            SyntaxNode operand = left;
            Token token = Current;
            Step step = token.Kind == TokenKind.Symbol ? ApplyOperator(token, minimum, ref left, ref fractionParts) : Step.NotAnOperator;
            if (step == Step.Applied)
            {
                continue;
            }

            if (step == Step.BindsTooLoosely || !IsOperandStart(token) || Precedence.Implicit <= minimum)
            {
                return left;
            }

            // Multiplication with the sign omitted: the token starts another operand.
            if (Previous.Kind == TokenKind.Number && token.Kind == TokenKind.Number)
            {
                return Fail(SyntaxErrorCode.AdjacentNumbers, token.Span);
            }

            SyntaxNode? factor = ParseExpression(Precedence.Implicit);
            left = factor is null ? null : new BinaryExpression(BinaryOperator.ImplicitMultiply, operand, factor, Covering(operand, factor.Span));
            fractionParts = 0;
        }

        return null;
    }

    /// <summary>
    /// Applies the operator <paramref name="token"/> stands for to <paramref name="left"/>, which becomes
    /// <see langword="null"/> when its right operand fails to parse.
    /// </summary>
    private Step ApplyOperator(Token token, int minimum, ref SyntaxNode? left, ref int fractionParts)
    {
        SyntaxSymbol symbol = token.Symbol!;
        int precedence = symbol.Kind switch
        {
            SymbolKind.PostfixOperator => Precedence.Of(symbol.PostfixOperator!.Value),
            SymbolKind.EngineeringSymbol => Precedence.Postfix,
            SymbolKind.UnitConversion => Precedence.Conversion,
            SymbolKind.BinaryOperator => Precedence.Of(symbol.BinaryOperator!.Value),

            // Decision of 17 Sep 2026: C between two operands is the combination operator, not the variable C.
            SymbolKind.Variable when symbol.Canonical.Text == "C" && IsOperandStart(Peek(1)) => Precedence.Combinatorial,
            _ => 0,
        };

        if (precedence == 0)
        {
            return Step.NotAnOperator;
        }

        if (precedence <= minimum)
        {
            return Step.BindsTooLoosely;
        }

        Advance();
        SyntaxNode operand = left!;
        if (symbol.Kind == SymbolKind.PostfixOperator)
        {
            left = symbol.PostfixOperator == PostfixOperator.Degrees
                ? ParseDegrees(operand, token)
                : new PostfixExpression(symbol.PostfixOperator!.Value, operand, Covering(operand, token.Span));
            fractionParts = 0;
            return Step.Applied;
        }

        if (symbol.Kind is SymbolKind.EngineeringSymbol or SymbolKind.UnitConversion)
        {
            left = new SuffixCommandExpression(operand, symbol, Covering(operand, token.Span));
            fractionParts = 0;
            return Step.Applied;
        }

        BinaryOperator binaryOperator = symbol.BinaryOperator ?? BinaryOperator.Combination;
        SyntaxNode? right = ParseExpression(precedence);
        if (right is null)
        {
            left = null;
            return Step.Applied;
        }

        SourceSpan span = Covering(operand, right.Span);
        if (binaryOperator != BinaryOperator.Fraction)
        {
            left = new BinaryExpression(binaryOperator, operand, right, span);
            fractionParts = 0;
        }
        else if (fractionParts == 2)
        {
            BinaryExpression fraction = (BinaryExpression)operand;
            left = new MixedFractionExpression(fraction.Left, fraction.Right, right, span);
            fractionParts = 3;
        }
        else if (fractionParts == 3)
        {
            left = Fail(SyntaxErrorCode.TooManyFractionParts, token.Span);
        }
        else
        {
            left = new BinaryExpression(BinaryOperator.Fraction, operand, right, span);
            fractionParts = 2;
        }

        return Step.Applied;
    }

    private SyntaxNode? ParseOperand()
    {
        Token token = Current;
        switch (token.Kind)
        {
            case TokenKind.Number:
                Advance();
                return new NumberLiteral(Text(token), token.Span);

            case TokenKind.BasePrefix:
                Advance();
                Token digits = Current;
                if (digits.Kind != TokenKind.Number)
                {
                    return Fail(SyntaxErrorCode.MissingOperand, digits.Span);
                }

                Advance();
                NumberBase numberBase = _text[token.Span.Start] switch
                {
                    'd' => NumberBase.Dec,
                    'h' => NumberBase.Hex,
                    'b' => NumberBase.Bin,
                    _ => NumberBase.Oct,
                };
                return new BaseLiteral(numberBase, new NumberLiteral(Text(digits), digits.Span), SourceSpan.Covering(token.Span, digits.Span));

            case TokenKind.CellReference:
                Advance();
                CellReference start = new(Text(token), token.Span);
                if (!Is(Current, SymbolKind.Colon))
                {
                    return start;
                }

                Advance();
                Token end = Current;
                if (end.Kind != TokenKind.CellReference)
                {
                    return Fail(SyntaxErrorCode.MissingOperand, end.Span);
                }

                Advance();
                return new CellRange(start, new CellReference(Text(end), end.Span), SourceSpan.Covering(token.Span, end.Span));

            case TokenKind.Symbol:
                return ParseSymbolOperand(token, token.Symbol!);

            case TokenKind.Invalid:
                return Fail(SyntaxErrorCode.UnexpectedCharacter, token.Span);

            default:
                return Fail(SyntaxErrorCode.MissingOperand, token.Span);
        }
    }

    private SyntaxNode? ParseSymbolOperand(Token token, SyntaxSymbol symbol)
    {
        switch (symbol.Kind)
        {
            case SymbolKind.Constant or SymbolKind.ScientificConstant or SymbolKind.Variable or SymbolKind.Memory
                or SymbolKind.MatrixVariable or SymbolKind.VectorVariable or SymbolKind.StatisticsVariable:
                Advance();
                return new NameReference(symbol, token.Span);

            case SymbolKind.BinaryOperator when symbol.BinaryOperator == BinaryOperator.Subtract:
                Advance();
                SyntaxNode? operand = ParseExpression(Precedence.Prefix);
                return operand is null ? null : new NegationExpression(operand, SourceSpan.Covering(token.Span, operand.Span));

            case SymbolKind.OpenParenthesis:
                Advance();
                SyntaxNode? inner = ParseExpression(Precedence.Relation);
                if (inner is null || !TryClose(out SourceSpan close))
                {
                    return null;
                }

                return new ParenthesizedExpression(inner, SourceSpan.Covering(token.Span, close));

            case SymbolKind.Function:
                return ParseFunctionCall(token, symbol);

            default:
                return Fail(SyntaxErrorCode.MissingOperand, token.Span);
        }
    }

    private SyntaxNode? ParseFunctionCall(Token token, SyntaxSymbol function)
    {
        Advance();
        ImmutableArray<SyntaxNode>.Builder arguments = ImmutableArray.CreateBuilder<SyntaxNode>();
        for (;;)
        {
            SyntaxNode? argument = ParseExpression(Precedence.Relation);
            if (argument is null)
            {
                return null;
            }

            arguments.Add(argument);
            if (!Is(Current, SymbolKind.Comma))
            {
                break;
            }

            Advance();
        }

        if (!TryClose(out SourceSpan close))
        {
            return null;
        }

        SourceSpan span = SourceSpan.Covering(token.Span, close);
        if (function.Canonical.Text == "root(")
        {
            return arguments.Count == 2
                ? new BinaryExpression(BinaryOperator.Root, arguments[0], arguments[1], span)
                : Fail(SyntaxErrorCode.InvalidRootArguments, span);
        }

        return new FunctionCall(function, arguments.ToImmutable(), span);
    }

    /// <summary>
    /// After <c>°</c>: a number followed by <c>′</c> is minutes, and a number followed by <c>″</c> is seconds. With neither,
    /// the <c>°</c> is the degree unit.
    /// </summary>
    private SyntaxNode ParseDegrees(SyntaxNode degrees, Token mark)
    {
        NumberLiteral? minutes = TakeSexagesimalPart("′");
        NumberLiteral? seconds = TakeSexagesimalPart("″");

        return minutes is null && seconds is null
            ? new PostfixExpression(PostfixOperator.Degrees, degrees, Covering(degrees, mark.Span))
            : new SexagesimalExpression(degrees, minutes, seconds, Covering(degrees, Previous.Span));
    }

    private NumberLiteral? TakeSexagesimalPart(string markText)
    {
        Token number = Current;
        Token mark = Peek(1);
        if (number.Kind != TokenKind.Number || !Is(mark, SymbolKind.SexagesimalMark) || mark.Symbol!.Canonical.Text != markText)
        {
            return null;
        }

        Advance();
        Advance();
        return new NumberLiteral(Text(number), number.Span);
    }

    /// <summary>Consumes <c>)</c>, or accepts its omission at the end of the text (manual p. 28).</summary>
    private bool TryClose(out SourceSpan close)
    {
        Token token = Current;
        if (Is(token, SymbolKind.CloseParenthesis))
        {
            Advance();
            close = token.Span;
            return true;
        }

        close = token.Span;
        if (token.Kind == TokenKind.End)
        {
            return true;
        }

        FailAt(token);
        return false;
    }

    /// <summary>Reports the token that could not be used where it stands.</summary>
    private SyntaxNode? FailAt(Token token)
    {
        SyntaxErrorCode code = token.Kind switch
        {
            TokenKind.Invalid => SyntaxErrorCode.UnexpectedCharacter,
            TokenKind.Symbol => token.Symbol!.Kind switch
            {
                SymbolKind.CloseParenthesis => SyntaxErrorCode.UnmatchedClosingParenthesis,
                SymbolKind.SexagesimalMark => SyntaxErrorCode.MisplacedSexagesimalMark,
                SymbolKind.RelationOperator => SyntaxErrorCode.RelationNotAllowed,
                _ => SyntaxErrorCode.UnexpectedToken,
            },
            // Defensive: a number, prefix or cell reference left over would have started an implicit multiplication.
            _ => SyntaxErrorCode.UnexpectedToken,
        };

        return Fail(code, token.Span);
    }

    private SyntaxNode? Fail(SyntaxErrorCode code, SourceSpan span)
    {
        _error = new SyntaxDiagnostic(code, span);
        return null;
    }

    private Token Advance()
    {
        Token token = _tokens[_position];
        if (_position < _tokens.Length - 1)
        {
            _position++;
        }

        return token;
    }

    private Token Peek(int offset) => _tokens[Math.Min(_position + offset, _tokens.Length - 1)];

    private string Text(Token token) => _text.Substring(token.Span.Start, token.Span.Length);
}
