// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Turns a syntax tree into a bound tree: resolves names, checks arities and applications, turns literals into values and
/// differentiates d/dx bodies.
/// </summary>
/// <remarks>
/// The binder stops at the first error, as the parser does, and reports it with the span of the offending node.
/// </remarks>
internal sealed class Binder
{
    private static readonly FrozenDictionary<string, (Operation Operation, int MinimumArity, int MaximumArity)> Functions =
        new Dictionary<string, (Operation, int, int)>(StringComparer.Ordinal)
        {
            ["sin("] = (Operation.Sin, 1, 1),
            ["cos("] = (Operation.Cos, 1, 1),
            ["tan("] = (Operation.Tan, 1, 1),
            ["sin⁻¹("] = (Operation.Asin, 1, 1),
            ["cos⁻¹("] = (Operation.Acos, 1, 1),
            ["tan⁻¹("] = (Operation.Atan, 1, 1),
            ["sinh("] = (Operation.Sinh, 1, 1),
            ["cosh("] = (Operation.Cosh, 1, 1),
            ["tanh("] = (Operation.Tanh, 1, 1),
            ["sinh⁻¹("] = (Operation.Asinh, 1, 1),
            ["cosh⁻¹("] = (Operation.Acosh, 1, 1),
            ["tanh⁻¹("] = (Operation.Atanh, 1, 1),
            ["log("] = (Operation.Log10, 1, 2),
            ["ln("] = (Operation.Ln, 1, 1),
            ["√("] = (Operation.SquareRoot, 1, 1),
            ["Abs("] = (Operation.Absolute, 1, 1),
            ["Int("] = (Operation.IntegerPart, 1, 1),
            ["Intg("] = (Operation.LargestInteger, 1, 1),
            ["Rnd("] = (Operation.Round, 1, 1),
            ["GCD("] = (Operation.GreatestCommonDivisor, 2, 2),
            ["LCM("] = (Operation.LeastCommonMultiple, 2, 2),
            ["RanInt#("] = (Operation.RandomInteger, 2, 2),
            ["AtWt("] = (Operation.AtomicWeight, 1, 1),
            ["Conjg("] = (Operation.Conjugate, 1, 1),
            ["Arg("] = (Operation.Argument, 1, 1),
            ["ReP("] = (Operation.RealPart, 1, 1),
            ["ImP("] = (Operation.ImaginaryPart, 1, 1),
            ["Not("] = (Operation.Not, 1, 1),
            ["Neg("] = (Operation.Neg, 1, 1),
            ["Det("] = (Operation.Determinant, 1, 1),
            ["Trn("] = (Operation.Transpose, 1, 1),
            ["Identity("] = (Operation.Identity, 1, 1),
            ["Angle("] = (Operation.VectorAngle, 2, 2),
            ["UnitV("] = (Operation.UnitVector, 1, 1),
            ["P("] = (Operation.NormalP, 1, 1),
            ["Q("] = (Operation.NormalQ, 1, 1),
            ["R("] = (Operation.NormalR, 1, 1),
        }.ToFrozenDictionary(StringComparer.Ordinal);

    // The statistic variables (pp. 90-91); "min(x)" is one name, as on the calculator's menu.
    private static readonly FrozenDictionary<string, Statistic> Statistics = new Dictionary<string, Statistic>(StringComparer.Ordinal)
    {
        ["n"] = Statistic.Count,
        ["Σx"] = Statistic.SumX,
        ["Σy"] = Statistic.SumY,
        ["Σx²"] = Statistic.SumX2,
        ["Σy²"] = Statistic.SumY2,
        ["Σxy"] = Statistic.SumXY,
        ["Σx³"] = Statistic.SumX3,
        ["Σx²y"] = Statistic.SumX2Y,
        ["Σx⁴"] = Statistic.SumX4,
        ["x̄"] = Statistic.MeanX,
        ["ȳ"] = Statistic.MeanY,
        ["σ²x"] = Statistic.PopulationVarianceX,
        ["σ²y"] = Statistic.PopulationVarianceY,
        ["σx"] = Statistic.PopulationDeviationX,
        ["σy"] = Statistic.PopulationDeviationY,
        ["s²x"] = Statistic.SampleVarianceX,
        ["s²y"] = Statistic.SampleVarianceY,
        ["sx"] = Statistic.SampleDeviationX,
        ["sy"] = Statistic.SampleDeviationY,
        ["min(x)"] = Statistic.MinX,
        ["max(x)"] = Statistic.MaxX,
        ["min(y)"] = Statistic.MinY,
        ["max(y)"] = Statistic.MaxY,
        ["Q1"] = Statistic.FirstQuartile,
        ["Med"] = Statistic.Median,
        ["Q3"] = Statistic.ThirdQuartile,
        ["a"] = Statistic.A,
        ["b"] = Statistic.B,
        ["c"] = Statistic.C,
        ["r"] = Statistic.R,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, MemorySlot> Memories = new Dictionary<string, MemorySlot>(StringComparer.Ordinal)
    {
        ["A"] = MemorySlot.A,
        ["B"] = MemorySlot.B,
        ["C"] = MemorySlot.C,
        ["D"] = MemorySlot.D,
        ["E"] = MemorySlot.E,
        ["F"] = MemorySlot.F,
        ["x"] = MemorySlot.X,
        ["y"] = MemorySlot.Y,
        ["z"] = MemorySlot.Z,
        ["Ans"] = MemorySlot.Ans,
        ["PreAns"] = MemorySlot.PreAns,
        ["MatA"] = MemorySlot.MatA,
        ["MatB"] = MemorySlot.MatB,
        ["MatC"] = MemorySlot.MatC,
        ["MatD"] = MemorySlot.MatD,
        ["MatAns"] = MemorySlot.MatAns,
        ["VctA"] = MemorySlot.VctA,
        ["VctB"] = MemorySlot.VctB,
        ["VctC"] = MemorySlot.VctC,
        ["VctD"] = MemorySlot.VctD,
        ["VctAns"] = MemorySlot.VctAns,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly FrozenDictionary<string, int> EngineeringExponents = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        ["_m"] = -3,
        ["_μ"] = -6,
        ["_n"] = -9,
        ["_p"] = -12,
        ["_f"] = -15,
        ["_k"] = 3,
        ["_M"] = 6,
        ["_G"] = 9,
        ["_T"] = 12,
        ["_P"] = 15,
        ["_E"] = 18,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    // ÷R, Pol( and Rec( (pp. 56, 62), and the Func Analysis commands (pp. 51-55), exist only in these applications.
    private static readonly CalculatorApp[] RemainderApps = [CalculatorApp.Calculate, CalculatorApp.Statistics, CalculatorApp.Matrix, CalculatorApp.Vector];

    // Verify is available in Calculate, Table, Equation and Complex (p. 73).
    private static readonly CalculatorApp[] VerifyApps = [CalculatorApp.Calculate, CalculatorApp.Table, CalculatorApp.Equation, CalculatorApp.Complex];

    private static readonly CalculatorApp[] CalculusApps =
    [
        CalculatorApp.Calculate, CalculatorApp.Statistics, CalculatorApp.Distribution, CalculatorApp.Spreadsheet, CalculatorApp.Table,
        CalculatorApp.Equation, CalculatorApp.Inequality, CalculatorApp.Matrix, CalculatorApp.Vector, CalculatorApp.Ratio,
    ];

    private readonly EngineCatalog _catalog;
    private readonly CalculatorApp _app;
    private readonly CalculatorSettings _settings;
    private readonly IReadOnlyDictionary<DefinedFunction, SyntaxNode> _definitions;
    private readonly StatisticsSetup _statistics;
    private readonly List<(string Name, int Slot)> _scopes = [];
    private readonly List<DefinedFunction> _calling = [];
    private SourceSpan? _spanOverride;
    private CalcError? _error;

    private Binder(EngineCatalog catalog, CalculatorApp app, CalculatorSettings settings, IReadOnlyDictionary<DefinedFunction, SyntaxNode> definitions, StatisticsSetup statistics)
    {
        _catalog = catalog;
        _app = app;
        _settings = settings;
        _definitions = definitions;
        _statistics = statistics;
    }

    /// <summary>Gets the number of local slots the bound tree uses.</summary>
    public int SlotCount { get; private set; }

    /// <summary>Binds a parsed input.</summary>
    /// <returns>The statement, or <see langword="null"/> with <paramref name="error"/> set.</returns>
    public static BoundStatement? Bind(
        SyntaxNode root,
        EngineCatalog catalog,
        CalculatorApp app,
        CalculatorSettings settings,
        IReadOnlyDictionary<DefinedFunction, SyntaxNode> definitions,
        StatisticsSetup statistics,
        out int slotCount,
        out CalcError? error)
    {
        Binder binder = new(catalog, app, settings, definitions, statistics);
        BoundStatement? statement = binder.BindStatement(root);
        slotCount = binder.SlotCount;
        error = statement is null ? binder._error : null;
        return statement;
    }

    /// <summary>Binds the body of f(x) or g(x) on its own, to check it when it is defined.</summary>
    public static CalcError? Check(SyntaxNode body, EngineCatalog catalog, CalculatorSettings settings)
    {
        Binder binder = new(catalog, CalculatorApp.Calculate, settings, new Dictionary<DefinedFunction, SyntaxNode>(), default);
        binder._scopes.Add(("x", binder.SlotCount++));
        return binder.BindNode(body) is null && binder._error is { Kind: not CalcErrorKind.NotDefined } error ? error : null;
    }

    private BoundStatement? BindStatement(SyntaxNode root)
    {
        if (root is RelationChain chain)
        {
            return RequireApp(root.Span, VerifyApps) && BindOperands(chain.Operands) is { } operands
                ? new BoundStatement(StatementKind.Verify, operands, chain.Operators, root.Span)
                : null;
        }

        if (_settings.Verify && _app is CalculatorApp.Calculate or CalculatorApp.Complex)
        {
            return Fail<BoundStatement>(CalcErrorKind.NoOperator, root.Span);
        }

        if (root is BinaryExpression { Operator: BinaryOperator.DivideWithRemainder } remainder && _app != CalculatorApp.BaseN)
        {
            return RequireApp(root.Span, RemainderApps) && BindOperands([remainder.Left, remainder.Right]) is { } operands
                ? new BoundStatement(StatementKind.Remainder, operands, [], root.Span)
                : null;
        }

        if (root is FunctionCall { Function.Text: "Pol(" or "Rec(" } coordinates)
        {
            if (!RequireApp(root.Span, RemainderApps) || !RequireArity(coordinates, 2, 2))
            {
                return null;
            }

            StatementKind kind = coordinates.Function.Text == "Pol(" ? StatementKind.ToPolar : StatementKind.ToRectangular;
            return BindOperands(coordinates.Arguments) is { } operands ? new BoundStatement(kind, operands, [], root.Span) : null;
        }

        return BindNode(root) is { } expression ? new BoundStatement(StatementKind.Expression, [expression], [], root.Span) : null;
    }

    private BoundNode? BindNode(SyntaxNode node)
    {
        if (_app == CalculatorApp.BaseN && !IsBaseNSyntax(node))
        {
            return Fail(CalcErrorKind.SyntaxError, node.Span);
        }

        return node switch
        {
            NumberLiteral literal => BindNumber(literal),
            BaseLiteral literal => BindBaseLiteral(literal.Digits.Text, literal.Base, literal.Span),
            NameReference name => BindName(name),
            NegationExpression negation => BindNegation(negation),
            PostfixExpression postfix => BindPostfix(postfix),
            BinaryExpression binary => BindBinary(binary),
            MixedFractionExpression mixed => BindCall(Operation.MixedFraction, mixed.Span, mixed.Whole, mixed.Numerator, mixed.Denominator),
            SexagesimalExpression sexagesimal => BindSexagesimal(sexagesimal),
            SuffixCommandExpression suffix => BindSuffix(suffix),
            FunctionCall call => BindFunction(call),
            ParenthesizedExpression parenthesized => BindNode(parenthesized.Inner),
            CellReference cell => BindCell(cell),
            _ => Fail(CalcErrorKind.SyntaxError, node.Span),
        };
    }

    /// <summary>One cell of the Spreadsheet application; a range belongs to Min(, Max(, Mean( or Sum( and nowhere else.</summary>
    private BoundNode? BindCell(CellReference cell)
    {
        SourceSpan span = SpanOf(cell);
        return CellAddress.TryParse(cell.Text, out CellAddress address)
            ? new BoundCells(new CellSelection(address, address, RangeAggregate.Cell), span)
            : Fail(CalcErrorKind.SyntaxError, span);
    }

    /// <summary>A range command of p. 105: its one argument is a range of cells, such as <c>Sum(A1:A3)</c>.</summary>
    private BoundNode? BindRange(FunctionCall call, RangeAggregate aggregate)
    {
        SourceSpan span = SpanOf(call);
        if (call.Arguments.Length != 1 || call.Arguments[0] is not CellRange range)
        {
            return Fail(CalcErrorKind.SyntaxError, span);
        }

        return CellAddress.TryParse(range.Start.Text, out CellAddress start) && CellAddress.TryParse(range.End.Text, out CellAddress end)
            ? new BoundCells(new CellSelection(start, end, aggregate), span)
            : Fail(CalcErrorKind.SyntaxError, span);
    }

    private BoundNode? BindNumber(NumberLiteral literal)
    {
        SourceSpan span = SpanOf(literal);
        if (_app == CalculatorApp.BaseN)
        {
            return BindBaseLiteral(literal.Text, _settings.BaseMode, span);
        }

        return TryParseNumber(literal.Text, out Value value) ? new BoundConstant(value, span) : Fail(CalcErrorKind.MathError, span);
    }

    /// <summary>Reads a number literal with the precision rule (invariant I2): into <see cref="decimal"/>, never through <see cref="double"/>, unless decimal would lose digits.</summary>
    internal static bool TryParseNumber(string text, out Value value)
    {
        int open = text.IndexOf('(', StringComparison.Ordinal);
        if (open >= 0)
        {
            return TryParseRecurring(text, open, out value);
        }

        // More than 28 significant digits is still a decimal, rounded by decimal.TryParse, and no longer exact.
        int significantDigits = text.TrimStart('0', '.').Count(char.IsAsciiDigit);
        if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number)
            && (number == 0m ? significantDigits == 0 : Math.Abs(number) >= Value.SmallestPreciseDecimal))
        {
            value = Value.FromDecimal(number, isExact: significantDigits <= 28, form: null);
            return true;
        }

        double approximation = double.Parse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        value = Value.FromApproximation(approximation, form: null);
        return double.IsFinite(approximation);
    }

    /// <summary>Reads <c>a.b(c)</c> as the exact fraction <c>a.b + c / (10^|b| × (10^|c| − 1))</c>, divided once in <see cref="decimal"/>.</summary>
    private static bool TryParseRecurring(string text, int open, out Value value)
    {
        value = default;
        int point = text.IndexOf('.', StringComparison.Ordinal);
        string whole = text[..point];
        string fixedDigits = text[(point + 1)..open];
        string recurring = text[(open + 1)..^1];
        if (whole.Length + fixedDigits.Length + recurring.Length > 28)
        {
            return false;
        }

        BigInteger fixedScale = BigInteger.Pow(10, fixedDigits.Length);
        BigInteger recurringScale = BigInteger.Pow(10, recurring.Length) - 1;
        BigInteger prefix = BigInteger.Parse(whole.Length == 0 ? "0" : whole, CultureInfo.InvariantCulture) * fixedScale
            + (fixedDigits.Length == 0 ? BigInteger.Zero : BigInteger.Parse(fixedDigits, CultureInfo.InvariantCulture));
        BigInteger numerator = (prefix * recurringScale) + BigInteger.Parse(recurring, CultureInfo.InvariantCulture);
        BigInteger denominator = fixedScale * recurringScale;
        BigInteger divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;

        // At most 28 digits: the numerator and the denominator, below 10²⁸, both fit decimal.
        value = Value.FromDecimal((decimal)numerator / (decimal)denominator);
        return true;
    }

    private BoundNode? BindBaseLiteral(string digits, NumberBase numberBase, SourceSpan span)
    {
        int radix = (int)numberBase;
        ulong accumulated = 0;
        foreach (char digit in digits)
        {
            int digitValue = char.IsAsciiDigit(digit) ? digit - '0' : char.ToUpperInvariant(digit) - 'A' + 10;
            if (digitValue >= radix || digitValue < 0)
            {
                return Fail(CalcErrorKind.SyntaxError, span);
            }

            accumulated = (accumulated * (ulong)radix) + (ulong)digitValue;
            if (accumulated > uint.MaxValue)
            {
                return Fail(CalcErrorKind.MathError, span);
            }
        }

        // A decimal literal is a signed value; the other bases write the 32-bit pattern, so FFFFFFFF is −1 (p. 130).
        if (numberBase == NumberBase.Dec && accumulated > int.MaxValue)
        {
            return Fail(CalcErrorKind.MathError, span);
        }

        return new BoundConstant(Value.FromBaseN(unchecked((int)(uint)accumulated)), span);
    }

    private BoundNode? BindName(NameReference name)
    {
        SyntaxSymbol symbol = name.Symbol;
        SourceSpan span = SpanOf(name);
        switch (symbol.Kind)
        {
            case SymbolKind.Constant:
                return symbol.Text switch
                {
                    "π" => new BoundConstant(Value.Pi, span),
                    "e" => new BoundConstant(Value.E, span),
                    "i" => new BoundConstant(Value.CreateComplex(0d, 1d), span),
                    _ => new BoundCall(Operation.Random, [], span),
                };
            case SymbolKind.Variable:
                for (int i = _scopes.Count - 1; i >= 0; i--)
                {
                    if (_scopes[i].Name == symbol.Text)
                    {
                        return new BoundLocal(_scopes[i].Slot, span);
                    }
                }

                return new BoundMemory(Memories[symbol.Text], span);
            case SymbolKind.Memory:
                return symbol.Text == "PreAns" && _app != CalculatorApp.Calculate
                    ? Fail(CalcErrorKind.SyntaxError, span)
                    : new BoundMemory(Memories[symbol.Text], span);
            case SymbolKind.MatrixVariable or SymbolKind.VectorVariable:
                // The vocabulary offers these names only in the Matrix and Vector applications.
                return new BoundMemory(Memories[symbol.Text], span);
            case SymbolKind.StatisticsVariable:
                // The vocabulary offers these names only in the Statistics application.
                Statistic statistic = Statistics[symbol.Text];
                return Offers(statistic) ? new BoundStatistic(statistic, span) : Fail(CalcErrorKind.SyntaxError, span);
            case SymbolKind.ScientificConstant:
                return _catalog.Constants.TryGetValue(symbol.Text, out (ScientificConstant Constant, Value Value) constant)
                    ? new BoundConstant(constant.Value, span)
                    : Fail(CalcErrorKind.NotDefined, span);
            default:
                return Fail(CalcErrorKind.SyntaxError, span);
        }
    }

    private BoundNode? BindNegation(NegationExpression negation)
    {
        // −2147483648 is in the decimal Base-N range although 2147483648 alone is not.
        if (_app == CalculatorApp.BaseN && _settings.BaseMode == NumberBase.Dec && negation.Operand is NumberLiteral { Text: "2147483648" })
        {
            return new BoundConstant(Value.FromBaseN(int.MinValue), SpanOf(negation));
        }

        return BindCall(Operation.Negate, negation.Span, negation.Operand);
    }

    private BoundNode? BindPostfix(PostfixExpression postfix)
    {
        Operation? operation = postfix.Operator switch
        {
            PostfixOperator.Square => Operation.Square,
            PostfixOperator.Cube => Operation.Cube,
            PostfixOperator.Reciprocal => Operation.Reciprocal,
            PostfixOperator.Factorial => Operation.Factorial,
            PostfixOperator.Percent => _app == CalculatorApp.Complex ? null : Operation.Percent,
            PostfixOperator.Degrees => Operation.DegreesAngle,
            PostfixOperator.Radians => Operation.RadiansAngle,
            PostfixOperator.Gradians => Operation.GradiansAngle,
            _ => null,
        };

        return operation is { } known ? BindCall(known, postfix.Span, postfix.Operand) : BindStatisticCommand(postfix);
    }

    /// <summary>Whether the data and the regression type offer a statistic variable (p. 90; the asterisks mark one variable).</summary>
    private bool Offers(Statistic statistic)
    {
        return statistic switch
        {
            Statistic.Count or Statistic.SumX or Statistic.SumX2 or Statistic.MeanX or Statistic.PopulationVarianceX
                or Statistic.PopulationDeviationX or Statistic.SampleVarianceX or Statistic.SampleDeviationX
                or Statistic.MinX or Statistic.MaxX => true,
            Statistic.FirstQuartile or Statistic.Median or Statistic.ThirdQuartile => !_statistics.TwoVariable,
            Statistic.C => _statistics.TwoVariable && _statistics.Regression == RegressionModel.Quadratic,
            Statistic.R => _statistics.TwoVariable && _statistics.Regression != RegressionModel.Quadratic,
            _ => _statistics.TwoVariable,
        };
    }

    /// <summary>
    /// ▶t and the estimates x̂, x̂₁, x̂₂ and ŷ (pp. 91-92), as expressions in the statistic variables with the formulas of
    /// pp. 93-95. The operand is evaluated once, into a local slot.
    /// </summary>
    private BoundNode? BindStatisticCommand(PostfixExpression postfix)
    {
        SourceSpan span = SpanOf(postfix);
        bool quadratic = _statistics.Regression == RegressionModel.Quadratic;
        bool offered = postfix.Operator switch
        {
            PostfixOperator.StandardizedVariate => !_statistics.TwoVariable,
            PostfixOperator.EstimateY => _statistics.TwoVariable,
            PostfixOperator.EstimateX => _statistics.TwoVariable && !quadratic,
            _ => _statistics.TwoVariable && quadratic,
        };
        if (!offered)
        {
            return Fail(CalcErrorKind.SyntaxError, span);
        }

        if (BindNode(postfix.Operand) is not { } operand)
        {
            return null;
        }

        int slot = SlotCount++;
        BoundNode v = new BoundLocal(slot, span);
        BoundNode a = new BoundStatistic(Statistic.A, span);
        BoundNode b = new BoundStatistic(Statistic.B, span);
        BoundNode Call(Operation operation, params BoundNode[] arguments) => new BoundCall(operation, [.. arguments], span);

        BoundNode body = postfix.Operator switch
        {
            // t = (x − x̄)/σx.
            PostfixOperator.StandardizedVariate => Call(Operation.Divide, Call(Operation.Subtract, v, new BoundStatistic(Statistic.MeanX, span)), new BoundStatistic(Statistic.PopulationDeviationX, span)),
            PostfixOperator.EstimateY => _statistics.Regression switch
            {
                RegressionModel.Linear => Call(Operation.Add, a, Call(Operation.Multiply, b, v)),
                RegressionModel.Quadratic => Call(Operation.Add, Call(Operation.Add, a, Call(Operation.Multiply, b, v)), Call(Operation.Multiply, new BoundStatistic(Statistic.C, span), Call(Operation.Square, v))),
                RegressionModel.Logarithmic => Call(Operation.Add, a, Call(Operation.Multiply, b, Call(Operation.Ln, v))),
                RegressionModel.ExponentialE => Call(Operation.Multiply, a, Call(Operation.Exp, Call(Operation.Multiply, b, v))),
                RegressionModel.ExponentialAB => Call(Operation.Multiply, a, Call(Operation.Power, b, v)),
                RegressionModel.Power => Call(Operation.Multiply, a, Call(Operation.Power, v, b)),
                _ => Call(Operation.Add, a, Call(Operation.Divide, b, v)),
            },
            PostfixOperator.EstimateX => _statistics.Regression switch
            {
                RegressionModel.Linear => Call(Operation.Divide, Call(Operation.Subtract, v, a), b),
                RegressionModel.Logarithmic => Call(Operation.Exp, Call(Operation.Divide, Call(Operation.Subtract, v, a), b)),
                RegressionModel.ExponentialE => Call(Operation.Divide, Call(Operation.Subtract, Call(Operation.Ln, v), Call(Operation.Ln, a)), b),
                RegressionModel.ExponentialAB => Call(Operation.Divide, Call(Operation.Subtract, Call(Operation.Ln, v), Call(Operation.Ln, a)), Call(Operation.Ln, b)),
                RegressionModel.Power => Call(Operation.Exp, Call(Operation.Divide, Call(Operation.Subtract, Call(Operation.Ln, v), Call(Operation.Ln, a)), b)),
                _ => Call(Operation.Divide, b, Call(Operation.Subtract, v, a)),
            },
            _ => QuadraticRoot(postfix.Operator == PostfixOperator.EstimateX1, v, a, b, span),
        };

        return new BoundLet(slot, operand, body, span);
    }

    /// <summary>x̂₁ or x̂₂ = (−b ± √(b² − 4c(a − y)))/(2c) (p. 94).</summary>
    private static BoundCall QuadraticRoot(bool first, BoundNode y, BoundNode a, BoundNode b, SourceSpan span)
    {
        BoundNode c = new BoundStatistic(Statistic.C, span);
        BoundCall Call(Operation operation, params BoundNode[] arguments) => new(operation, [.. arguments], span);
        BoundConstant Constant(decimal value) => new(Value.FromDecimal(value), span);

        BoundNode root = Call(Operation.SquareRoot, Call(Operation.Subtract, Call(Operation.Square, b), Call(Operation.Multiply, Call(Operation.Multiply, Constant(4m), c), Call(Operation.Subtract, a, y))));
        return Call(Operation.Divide, Call(first ? Operation.Add : Operation.Subtract, Call(Operation.Negate, b), root), Call(Operation.Multiply, Constant(2m), c));
    }

    private BoundNode? BindBinary(BinaryExpression binary)
    {
        // e^x is the exponential function, not a power of the 15-digit e (p. 69).
        if (binary.Operator == BinaryOperator.Power && binary.Left is NameReference { Symbol.Text: "e" })
        {
            return BindCall(Operation.Exp, binary.Span, binary.Right);
        }

        Operation? operation = binary.Operator switch
        {
            BinaryOperator.Add => Operation.Add,
            BinaryOperator.Subtract => Operation.Subtract,
            BinaryOperator.Multiply or BinaryOperator.ImplicitMultiply => Operation.Multiply,
            BinaryOperator.Divide or BinaryOperator.Fraction => Operation.Divide,
            BinaryOperator.DivideWithRemainder => RemainderApps.Contains(_app) ? Operation.RemainderQuotient : null,
            BinaryOperator.Power => Operation.Power,
            BinaryOperator.Root => Operation.Root,
            BinaryOperator.Permutation => Operation.Permutation,
            BinaryOperator.Combination => Operation.Combination,
            BinaryOperator.Polar => Operation.Polar,
            BinaryOperator.DotProduct => Operation.DotProduct,
            BinaryOperator.And => Operation.And,
            BinaryOperator.Or => Operation.Or,
            BinaryOperator.Xor => Operation.Xor,
            BinaryOperator.Xnor => Operation.Xnor,
            _ => null,
        };

        return operation is { } known ? BindCall(known, binary.Span, binary.Left, binary.Right) : Fail(CalcErrorKind.SyntaxError, binary.Span);
    }

    private BoundCall? BindSexagesimal(SexagesimalExpression sexagesimal)
    {
        SourceSpan span = SpanOf(sexagesimal);
        if (BindNode(sexagesimal.Degrees) is not { } degrees)
        {
            return null;
        }

        BoundNode? minutes = sexagesimal.Minutes is null ? new BoundConstant(Value.Zero, span) : BindNumber(sexagesimal.Minutes);
        BoundNode? seconds = sexagesimal.Seconds is null ? new BoundConstant(Value.Zero, span) : BindNumber(sexagesimal.Seconds);
        return minutes is null || seconds is null ? null : new BoundCall(Operation.Sexagesimal, [degrees, minutes, seconds], span);
    }

    private BoundNode? BindSuffix(SuffixCommandExpression suffix)
    {
        SourceSpan span = SpanOf(suffix);
        if (BindNode(suffix.Operand) is not { } operand)
        {
            return null;
        }

        if (suffix.Command.Kind == SymbolKind.EngineeringSymbol)
        {
            return Scale(operand, EngineeringExponents[suffix.Command.Text], span);
        }

        if (!_catalog.Units.TryGetValue(suffix.Command.Text, out UnitConversion? conversion))
        {
            return Fail(CalcErrorKind.NotDefined, span);
        }

        // (value + before) × multiplier ÷ divisor + after, with identity steps left out so exact values stay exact.
        BoundNode result = operand;
        if (conversion.OffsetBefore != 0m)
        {
            result = new BoundCall(Operation.Add, [result, new BoundConstant(Value.FromDecimal(conversion.OffsetBefore), span)], span);
        }

        if (conversion.Multiplier != 1m)
        {
            result = new BoundCall(Operation.Multiply, [result, new BoundConstant(Value.FromDecimal(conversion.Multiplier), span)], span);
        }

        if (conversion.Divisor != 1m)
        {
            result = new BoundCall(Operation.Divide, [result, new BoundConstant(Value.FromDecimal(conversion.Divisor), span)], span);
        }

        if (conversion.OffsetAfter != 0m)
        {
            result = new BoundCall(Operation.Add, [result, new BoundConstant(Value.FromDecimal(conversion.OffsetAfter), span)], span);
        }

        return result;
    }

    private BoundNode? BindFunction(FunctionCall call)
    {
        string name = call.Function.Text;
        SourceSpan span = SpanOf(call);
        switch (name)
        {
            case "d/dx(":
                return BindCalculus(call, CalculusKind.Derivative, 2, 3);
            case "∫(":
                return BindCalculus(call, CalculusKind.Integral, 3, 4);
            case "Σ(":
                return BindCalculus(call, CalculusKind.Summation, 3, 3);
            case "Π(":
                return BindCalculus(call, CalculusKind.Product, 3, 3);
            case "f(":
                return BindDefinedFunction(call, DefinedFunction.F);
            case "g(":
                return BindDefinedFunction(call, DefinedFunction.G);
            case "Pol(" or "Rec(":
                // Only on their own (assumption U14); BindStatement handles that case.
                return Fail(CalcErrorKind.SyntaxError, span);
            case "P(" or "Q(" or "R(" when _statistics.TwoVariable:
                // The normal distribution is offered with one-variable data only (p. 91).
                return Fail(CalcErrorKind.SyntaxError, span);
            case "Min(":
                return BindRange(call, RangeAggregate.Minimum);
            case "Max(":
                return BindRange(call, RangeAggregate.Maximum);
            case "Mean(":
                return BindRange(call, RangeAggregate.Mean);
            case "Sum(":
                return BindRange(call, RangeAggregate.Sum);
        }

        if (Functions.TryGetValue(name, out (Operation Operation, int MinimumArity, int MaximumArity) builtIn))
        {
            if (!RequireArity(call, builtIn.MinimumArity, builtIn.MaximumArity))
            {
                return null;
            }

            Operation operation = builtIn.Operation == Operation.Log10 && call.Arguments.Length == 2 ? Operation.LogBase : builtIn.Operation;
            return BindCall(operation, call.Span, [.. call.Arguments]);
        }

        if (_catalog.Functions.TryGetValue(name, out IMathFunction? plugin))
        {
            return RequireArity(call, plugin.Signature.MinimumArity, plugin.Signature.MaximumArity) && BindOperands(call.Arguments) is { } arguments
                ? new BoundPluginCall(plugin, arguments, span)
                : null;
        }

        return Fail(CalcErrorKind.SyntaxError, span);
    }

    private BoundCalculus? BindCalculus(FunctionCall call, CalculusKind kind, int minimumArity, int maximumArity)
    {
        SourceSpan span = SpanOf(call);
        if (!RequireApp(span, CalculusApps) || !RequireArity(call, minimumArity, maximumArity))
        {
            return null;
        }

        int slot = SlotCount++;
        _scopes.Add(("x", slot));
        BoundNode? body = BindNode(call.Arguments[0]);
        _scopes.RemoveAt(_scopes.Count - 1);
        if (body is null || BindOperands(call.Arguments.RemoveAt(0)) is not { } bounds)
        {
            return null;
        }

        BoundNode? derivative = null;
        if (kind == CalculusKind.Derivative)
        {
            int nextSlot = SlotCount;
            derivative = Differentiator.Differentiate(body, slot, _settings.AngleUnit, ref nextSlot);
            SlotCount = nextSlot;
        }

        return new BoundCalculus(kind, slot, body, derivative, bounds, span);
    }

    private BoundNode? BindDefinedFunction(FunctionCall call, DefinedFunction function)
    {
        SourceSpan span = SpanOf(call);
        if (!RequireArity(call, 1, 1) || BindNode(call.Arguments[0]) is not { } argument)
        {
            return null;
        }

        if (!_definitions.TryGetValue(function, out SyntaxNode? definition))
        {
            return Fail(CalcErrorKind.NotDefined, span);
        }

        // f(x) = g(x) with g(x) = f(x) is a Circular ERROR when either is used (p. 72).
        if (_calling.Contains(function))
        {
            return Fail(CalcErrorKind.CircularError, span);
        }

        int slot = SlotCount++;
        SourceSpan? outerOverride = _spanOverride;
        List<(string Name, int Slot)> outerScopes = [.. _scopes];
        _scopes.Clear();
        _scopes.Add(("x", slot));
        _calling.Add(function);
        _spanOverride = span;

        BoundNode? body = BindNode(definition);

        _spanOverride = outerOverride;
        _calling.RemoveAt(_calling.Count - 1);
        _scopes.Clear();
        _scopes.AddRange(outerScopes);

        return body is null ? null : new BoundLet(slot, argument, body, span);
    }

    private BoundCall? BindCall(Operation operation, SourceSpan span, params ReadOnlySpan<SyntaxNode> operands)
    {
        ImmutableArray<BoundNode>.Builder arguments = ImmutableArray.CreateBuilder<BoundNode>(operands.Length);
        foreach (SyntaxNode operand in operands)
        {
            if (BindNode(operand) is not { } bound)
            {
                return null;
            }

            arguments.Add(bound);
        }

        return new BoundCall(operation, arguments.MoveToImmutable(), _spanOverride ?? span);
    }

    private ImmutableArray<BoundNode>? BindOperands(IEnumerable<SyntaxNode> operands)
    {
        ImmutableArray<BoundNode>.Builder bound = ImmutableArray.CreateBuilder<BoundNode>();
        foreach (SyntaxNode operand in operands)
        {
            if (BindNode(operand) is not { } node)
            {
                return null;
            }

            bound.Add(node);
        }

        return bound.ToImmutable();
    }

    private static BoundCall Scale(BoundNode operand, int powerOfTen, SourceSpan span)
    {
        decimal factor = 1m;
        for (int i = 0; i < Math.Abs(powerOfTen); i++)
        {
            factor *= 10m;
        }

        Operation operation = powerOfTen >= 0 ? Operation.Multiply : Operation.Divide;
        return new BoundCall(operation, [operand, new BoundConstant(Value.FromDecimal(factor), span)], span);
    }

    private bool RequireArity(FunctionCall call, int minimum, int maximum)
    {
        if (call.Arguments.Length >= minimum && call.Arguments.Length <= maximum)
        {
            return true;
        }

        _error ??= new CalcError(CalcErrorKind.SyntaxError, SpanOf(call));
        return false;
    }

    /// <summary>Checks that the current application may use a command that exists only in some (manual pp. 51-62).</summary>
    private bool RequireApp(SourceSpan span, params ReadOnlySpan<CalculatorApp> applications)
    {
        // A loop rather than Contains: the span overload that takes a bare enum is .NET 10 only.
        foreach (CalculatorApp application in applications)
        {
            if (application == _app)
            {
                return true;
            }
        }

        _error ??= new CalcError(CalcErrorKind.SyntaxError, _spanOverride ?? span);
        return false;
    }

    /// <summary>Base-N has no CATALOG functions (p. 51): numbers, memories, the four operations, logic and parentheses.</summary>
    private static bool IsBaseNSyntax(SyntaxNode node)
    {
        return node switch
        {
            NumberLiteral or BaseLiteral or NegationExpression or ParenthesizedExpression => true,
            NameReference name => name.Symbol.Kind is SymbolKind.Variable or SymbolKind.Memory && name.Symbol.Text != "PreAns",
            BinaryExpression binary => binary.Operator is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply
                or BinaryOperator.ImplicitMultiply or BinaryOperator.Divide or BinaryOperator.And or BinaryOperator.Or
                or BinaryOperator.Xor or BinaryOperator.Xnor,
            FunctionCall call => call.Function.Text is "Not(" or "Neg(",
            _ => false,
        };
    }

    private SourceSpan SpanOf(SyntaxNode node) => _spanOverride ?? node.Span;

    private BoundNode? Fail(CalcErrorKind kind, SourceSpan span) => Fail<BoundNode>(kind, span);

    private T? Fail<T>(CalcErrorKind kind, SourceSpan span)
        where T : class
    {
        _error ??= new CalcError(kind, _spanOverride ?? span);
        return null;
    }
}
