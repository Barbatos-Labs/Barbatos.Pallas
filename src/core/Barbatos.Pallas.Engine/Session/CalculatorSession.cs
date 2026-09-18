// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A calculator in use: the application, settings, memory (Ans, PreAns, A-F, x, y, z), defined functions and history.
/// </summary>
/// <remarks>
/// A session is not thread-safe; the engine it comes from is. Memory and defined functions persist across applications,
/// as on the reference calculator (manual pp. 36-40, 70-72); PreAns exists only in Calculate.
/// </remarks>
public sealed class CalculatorSession
{
    private readonly PallasEngine _engine;
    private readonly Random _random;
    private readonly Value[] _variables = [.. Enumerable.Repeat(Value.Zero, 9)];
    private readonly Dictionary<DefinedFunction, SyntaxNode> _definitions = [];
    private readonly List<Calculation> _history = [];
    private CalculatorSettings _settings = CalculatorSettings.Initial;

    internal CalculatorSession(PallasEngine engine, CalculatorApp app, CalculatorProfile profile, int? randomSeed)
    {
        RequireSupported(app);
        _engine = engine;
        App = app;
        Profile = profile;
        Budget = engine.Budget;
        _random = randomSeed is { } seed ? new Random(seed) : new Random();
    }

    /// <summary>Gets the engine the session belongs to.</summary>
    public PallasEngine Engine => _engine;

    /// <summary>Gets the current application.</summary>
    public CalculatorApp App { get; private set; }

    /// <summary>Gets the profile.</summary>
    public CalculatorProfile Profile { get; }

    /// <summary>Gets or sets the settings.</summary>
    public CalculatorSettings Settings
    {
        get => _settings;
        set => _settings = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the budget of each calculation.</summary>
    public EngineBudget Budget { get; set; }

    /// <summary>Gets the last result (manual p. 36).</summary>
    public Value Ans { get; private set; } = Value.Zero;

    /// <summary>Gets the result before the last one; Calculate only (p. 37).</summary>
    public Value PreAns { get; private set; } = Value.Zero;

    /// <summary>Gets the calculations since the history was last cleared, oldest first.</summary>
    public IReadOnlyList<Calculation> History => _history;

    /// <summary>Returns the value of a variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The value; initially 0.</returns>
    public Value GetVariable(MemoryVariable variable) => _variables[(int)Checked(variable)];

    /// <summary>Stores a value in a variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value.</param>
    public void SetVariable(MemoryVariable variable, Value value)
    {
        _variables[(int)Checked(variable)] = value;
    }

    /// <summary>Stores Ans in a variable: STO after a calculation (p. 38).</summary>
    /// <param name="variable">The variable.</param>
    public void Store(MemoryVariable variable) => SetVariable(variable, Ans);

    /// <summary>Defines f(x) or g(x) (p. 70).</summary>
    /// <param name="function">The function.</param>
    /// <param name="expression">The body in Canonical Linear Syntax, in x.</param>
    /// <returns><see langword="null"/>, or the syntax error of the body.</returns>
    public CalcError? Define(DefinedFunction function, string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ParseResult parsed = ExpressionParser.Parse(expression, SyntaxContext.Calculate, _engine.Vocabulary);
        if (!parsed.Succeeded)
        {
            return ToError(parsed.Diagnostic!.Value);
        }

        if (Binder.Check(parsed.Root, _engine.Catalog, _settings) is { } error)
        {
            return error;
        }

        _definitions[function] = parsed.Root;
        return null;
    }

    /// <summary>Removes the definition of f(x) or g(x).</summary>
    /// <param name="function">The function.</param>
    public void Undefine(DefinedFunction function) => _definitions.Remove(function);

    /// <summary>Changes the application. Leaving Calculate clears PreAns; the history is cleared (pp. 35, 37).</summary>
    /// <param name="app">The application.</param>
    /// <exception cref="NotSupportedException"><paramref name="app"/> is an application whose engine is not implemented yet.</exception>
    public void SwitchApp(CalculatorApp app)
    {
        RequireSupported(app);
        if (App == CalculatorApp.Calculate && app != CalculatorApp.Calculate)
        {
            PreAns = Value.Zero;
        }

        if (App != app)
        {
            _history.Clear();
        }

        App = app;
        _settings = _settings with { Verify = false };
    }

    /// <summary>Clears the history.</summary>
    public void ClearHistory() => _history.Clear();

    /// <summary>Calculates an input in the current application.</summary>
    /// <param name="input">The input in Canonical Linear Syntax.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The calculation, with its result or error.</returns>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public Calculation Calculate(string input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        Calculation calculation = Run(input, cancellationToken);
        _history.Add(calculation);
        return calculation;
    }

    /// <summary>Stores values in variables, then calculates: CALC (p. 40).</summary>
    /// <param name="input">The input in Canonical Linear Syntax.</param>
    /// <param name="values">The variable values.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The calculation.</returns>
    public Calculation Calculate(string input, IReadOnlyDictionary<MemoryVariable, Value> values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach ((MemoryVariable variable, Value value) in values)
        {
            SetVariable(variable, value);
        }

        return Calculate(input, cancellationToken);
    }

    /// <summary>Displays a calculation with the current settings, converted by the FORMAT menu.</summary>
    /// <param name="calculation">The calculation.</param>
    /// <param name="target">The conversion, or <see langword="null"/> for the display of the settings.</param>
    /// <returns>The display, or <see langword="null"/> when the conversion does not apply to the result.</returns>
    public FormattedResult? Format(Calculation calculation, FormatTarget? target = null)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        return ResultFormatter.Format(calculation, _settings, target);
    }

    /// <summary>Displays a result in engineering notation shifted by steps of three digits, as the ◀ and ▶ keys do in ENG mode (p. 48).</summary>
    /// <param name="calculation">The calculation.</param>
    /// <param name="shift">−1 moves the exponent down by 3 (1.024M to 1024k), +1 up.</param>
    /// <returns>The display, or <see langword="null"/> for a result that is not a real number.</returns>
    public string? FormatEngineering(Calculation calculation, int shift)
    {
        ArgumentNullException.ThrowIfNull(calculation);
        return calculation.Succeeded && calculation.Kind == CalculationKind.Value && calculation.Result.IsReal
            ? NumberText.Localize(ExactDisplay.Engineering(calculation.Result, _settings, shift), _settings)
            : null;
    }

    private Calculation Run(string input, CancellationToken cancellationToken)
    {
        CalculatorApp app = App;
        CalculatorSettings settings = _settings;
        ParseResult parsed = ExpressionParser.Parse(input, new SyntaxContext(app, AllowRelations: settings.Verify), _engine.Vocabulary);
        if (!parsed.Succeeded)
        {
            return Failed(input, ToError(parsed.Diagnostic!.Value));
        }

        BoundStatement? statement = Binder.Bind(parsed.Root, _engine.Catalog, app, settings, _definitions, out int slotCount, out CalcError? bindError);
        if (statement is null)
        {
            return Failed(input, bindError!.Value);
        }

        CompiledProgram program = Compiler.Compile(statement, slotCount);
        EvaluationContext context = new(_engine.Catalog, app, settings, Profile, Budget, _random, cancellationToken);
        Evaluator evaluator = new(program, context, LoadMemory);
        DisplayHints hints = DisplayHintReader.Read(parsed.Root);

        switch (statement.Kind)
        {
            case StatementKind.Verify:
                return Verify(input, statement, evaluator, context);
            case StatementKind.Remainder:
                return Remainder(input, evaluator, context, hints);
            case StatementKind.ToPolar or StatementKind.ToRectangular:
                return Coordinates(input, statement.Kind, evaluator, context);
        }

        EvalResult result = evaluator.Run(0);
        if (!result.Succeeded)
        {
            return Failed(input, new CalcError(result.Error!.Value, evaluator.ErrorSpan));
        }

        UpdateAnswers(result.Value);
        return new Calculation(input, app, settings, Profile, CalculationKind.Value, result.Value, null, null, null, [.. context.Integrals], hints);
    }

    private Calculation Verify(string input, BoundStatement statement, Evaluator evaluator, EvaluationContext context)
    {
        Value[] operands = new Value[statement.Operands.Length];
        for (int i = 0; i < operands.Length; i++)
        {
            EvalResult operand = evaluator.Run(i);
            if (!operand.Succeeded)
            {
                return Failed(input, new CalcError(operand.Error!.Value, evaluator.ErrorSpan));
            }

            operands[i] = operand.Value;
        }

        // Every link of the chain must hold: 2+3=5≠2+5=8 is False because 7 = 8 is (p. 75).
        bool isTrue = true;
        for (int i = 0; i < statement.Relations.Length; i++)
        {
            RelationOperator relation = statement.Relations[i];
            Value left = operands[i];
            Value right = operands[i + 1];
            if (left.Kind == ValueKind.Complex || right.Kind == ValueKind.Complex)
            {
                // An inequality of complex numbers cannot be verified (p. 128).
                if (relation is not (RelationOperator.Equal or RelationOperator.NotEqual))
                {
                    return Failed(input, new CalcError(CalcErrorKind.MathError, statement.Span));
                }

                bool equal = ValueMath.CompareReal(Value.FromApproximation(left.ToComplex().Real, null), Value.FromApproximation(right.ToComplex().Real, null)) == 0
                    && ValueMath.CompareReal(Value.FromApproximation(left.ToComplex().Imaginary, null), Value.FromApproximation(right.ToComplex().Imaginary, null)) == 0;
                isTrue &= relation == RelationOperator.Equal ? equal : !equal;
                continue;
            }

            int comparison = ValueMath.CompareReal(left, right);
            isTrue &= relation switch
            {
                RelationOperator.Equal => comparison == 0,
                RelationOperator.NotEqual => comparison != 0,
                RelationOperator.Less => comparison < 0,
                RelationOperator.Greater => comparison > 0,
                RelationOperator.LessOrEqual => comparison <= 0,
                _ => comparison >= 0,
            };
        }

        // Verify stores 1 in Ans when true and 0 when false (p. 76).
        Value answer = isTrue ? Value.One : Value.Zero;
        UpdateAnswers(answer);
        return new Calculation(input, App, context.Settings, Profile, CalculationKind.Verify, answer, null, isTrue, null, [.. context.Integrals]);
    }

    private Calculation Remainder(string input, Evaluator evaluator, EvaluationContext context, DisplayHints hints)
    {
        EvalResult dividend = evaluator.Run(0);
        EvalResult divisor = dividend.Succeeded ? evaluator.Run(1) : dividend;
        if (!divisor.Succeeded)
        {
            return Failed(input, new CalcError(divisor.Error!.Value, evaluator.ErrorSpan));
        }

        if (Operations.DivisionWithRemainder(dividend.Value, divisor.Value, context) is not { } division)
        {
            // The calculator divides normally where ÷R does not apply (p. 56).
            EvalResult quotient = ValueMath.Divide(dividend.Value, divisor.Value, context);
            if (!quotient.Succeeded)
            {
                return Failed(input, new CalcError(quotient.Error!.Value, SpanOfInput(input)));
            }

            UpdateAnswers(quotient.Value);
            return new Calculation(input, App, context.Settings, Profile, CalculationKind.Value, quotient.Value, null, null, null, [.. context.Integrals], hints);
        }

        // The quotient goes to E and Ans, the remainder to F (p. 56).
        _variables[(int)MemoryVariable.E] = division.Quotient;
        _variables[(int)MemoryVariable.F] = division.Remainder;
        UpdateAnswers(division.Quotient);
        return new Calculation(input, App, context.Settings, Profile, CalculationKind.Remainder, division.Quotient, division.Remainder, null, null, [.. context.Integrals]);
    }

    private Calculation Coordinates(string input, StatementKind kind, Evaluator evaluator, EvaluationContext context)
    {
        EvalResult first = evaluator.Run(0);
        EvalResult second = first.Succeeded ? evaluator.Run(1) : first;
        if (!second.Succeeded)
        {
            return Failed(input, new CalcError(second.Error!.Value, evaluator.ErrorSpan));
        }

        (EvalResult a, EvalResult b) = kind == StatementKind.ToPolar
            ? ToPolar(first.Value, second.Value, context)
            : ToRectangular(first.Value, second.Value, context);
        if (!a.Succeeded || !b.Succeeded)
        {
            return Failed(input, new CalcError(a.Error ?? b.Error!.Value, SpanOfInput(input)));
        }

        // The two results go to x and y (p. 62); Ans takes the first (assumption U14).
        _variables[(int)MemoryVariable.X] = a.Value;
        _variables[(int)MemoryVariable.Y] = b.Value;
        UpdateAnswers(a.Value);
        CalculationKind calculationKind = kind == StatementKind.ToPolar ? CalculationKind.Polar : CalculationKind.Rectangular;
        return new Calculation(input, App, context.Settings, Profile, calculationKind, a.Value, b.Value, null, null, [.. context.Integrals]);
    }

    private static (EvalResult R, EvalResult Theta) ToPolar(Value x, Value y, EvaluationContext context)
    {
        if (!x.IsReal || !y.IsReal)
        {
            return (ValueMath.MathError, ValueMath.MathError);
        }

        // r = √(x² + y²) with Value arithmetic, so exact forms carry: Pol(√(2),√(2)) has r exactly 2.
        EvalResult xx = ValueMath.Multiply(x, x, context);
        EvalResult yy = ValueMath.Multiply(y, y, context);
        EvalResult sum = xx.Succeeded && yy.Succeeded ? ValueMath.Add(xx.Value, yy.Value, context) : ValueMath.MathError;
        EvalResult r = sum.Succeeded ? ValueMath.SquareRoot(sum.Value, context) : sum;
        // Atan2(0, 0) is 0, the angle Pol(0,0) has.
        EvalResult theta = ValueMath.Real(Trigonometry.Atan2(y.ToDouble(), x.ToDouble(), context.AngleUnit), null, context);
        return (r, theta);
    }

    private static (EvalResult X, EvalResult Y) ToRectangular(Value r, Value theta, EvaluationContext context)
    {
        // 0 ≤ r (p. 171).
        if (!r.IsReal || !theta.IsReal || r.ToDouble() < 0d)
        {
            return (ValueMath.MathError, ValueMath.MathError);
        }

        // cos and sin share their domain (p. 170): both succeed or both fail.
        EvalResult cosine = Operations.Evaluate(Operation.Cos, [theta], context);
        EvalResult sine = Operations.Evaluate(Operation.Sin, [theta], context);
        return cosine.Succeeded
            ? (ValueMath.Multiply(r, cosine.Value, context), ValueMath.Multiply(r, sine.Value, context))
            : (cosine, cosine);
    }

    private EvalResult LoadMemory(MemorySlot slot)
    {
        Value value = slot switch
        {
            MemorySlot.Ans => Ans,
            MemorySlot.PreAns => PreAns,
            _ => _variables[(int)slot],
        };

        switch (App)
        {
            case CalculatorApp.BaseN:
                // Base-N drops fractional parts (p. 130) and holds 32 bits.
                if (value.Kind == ValueKind.BaseN)
                {
                    return value;
                }

                if (!value.IsReal)
                {
                    return ValueMath.MathError;
                }

                double truncated = Math.Truncate(value.ToDouble());
                return truncated is >= int.MinValue and <= int.MaxValue ? Value.FromBaseN((int)truncated) : ValueMath.MathError;
            case CalculatorApp.Complex:
                return value.Kind == ValueKind.BaseN ? Value.FromDecimal(value.ToInt32()) : value;
            default:
                // A variable holding a complex number cannot be used outside Complex (p. 163).
                return value.Kind switch
                {
                    ValueKind.Complex => ValueMath.MathError,
                    ValueKind.BaseN => Value.FromDecimal(value.ToInt32()),
                    _ => value,
                };
        }
    }

    private void UpdateAnswers(Value answer)
    {
        if (App == CalculatorApp.Calculate)
        {
            PreAns = Ans;
        }

        Ans = answer;
    }

    private Calculation Failed(string input, CalcError error)
    {
        return new Calculation(input, App, _settings, Profile, CalculationKind.Value, default, null, null, error, []);
    }

    private static CalcError ToError(SyntaxDiagnostic diagnostic)
    {
        CalcErrorKind kind = diagnostic.Code == SyntaxErrorCode.NestingTooDeep ? CalcErrorKind.StackError : CalcErrorKind.SyntaxError;
        return new CalcError(kind, diagnostic.Span, diagnostic.Code);
    }

    private static SourceSpan SpanOfInput(string input) => new(0, input.Length);

    private static MemoryVariable Checked(MemoryVariable variable)
    {
        return Enum.IsDefined(variable) ? variable : throw new ArgumentOutOfRangeException(nameof(variable), variable, "Not a defined MemoryVariable value.");
    }

    private static void RequireSupported(CalculatorApp app)
    {
        if (app is not (CalculatorApp.Calculate or CalculatorApp.Complex or CalculatorApp.BaseN))
        {
            throw new NotSupportedException($"The {app} application is implemented in Phase 4 or later; sessions support Calculate, Complex and BaseN.");
        }
    }
}
