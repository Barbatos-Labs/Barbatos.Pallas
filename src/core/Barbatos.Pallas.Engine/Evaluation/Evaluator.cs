// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Runs a compiled program on an explicit stack.
/// </summary>
/// <remarks>
/// Only calculus nests evaluations, one level per Σ, Π, ∫ or d/dx written inside another; the parser bounds that nesting
/// at 128. Every other operation is a loop over instructions.
/// </remarks>
internal sealed class Evaluator
{
    // 10⁻²²: the smallest tol the manual recommends (pp. 52-53).
    private const double SmallestTolerance = 1e-22;

    // ∫ refines to 10⁻¹⁴ relative, and answers while the estimate is within 10⁻⁹ or a looser tol (decision of
    // 18 Sep 2026): a tighter tol is more than double can promise for a difficult integrand, and was a Time Out.
    private const double IntegralTarget = 1e-14;
    private const double IntegralAcceptable = 1e-9;

    private readonly CompiledProgram _program;
    private readonly EvaluationContext _context;
    private readonly Func<MemorySlot, EvalResult> _memory;
    private readonly Value[] _locals;
    private readonly Value[]?[] _stacks;

    public Evaluator(CompiledProgram program, EvaluationContext context, Func<MemorySlot, EvalResult> memory)
    {
        _program = program;
        _context = context;
        _memory = memory;
        _locals = new Value[program.SlotCount];
        _stacks = new Value[program.Segments.Length][];
    }

    /// <summary>Gets the span of the instruction that failed, after <see cref="Run"/> returned an error.</summary>
    public SourceSpan ErrorSpan { get; private set; }

    public EvalResult Run(int segmentIndex)
    {
        Segment segment = _program.Segments[segmentIndex];
        Value[] stack = _stacks[segmentIndex] ??= new Value[segment.MaxStack];
        int top = 0;
        foreach (Instruction instruction in segment.Code)
        {
            EvalResult result;
            switch (instruction.Code)
            {
                case OpCode.Push:
                    stack[top++] = _program.Constants[instruction.A];
                    continue;
                case OpCode.LoadLocal:
                    stack[top++] = _locals[instruction.A];
                    continue;
                case OpCode.StoreLocal:
                    _locals[instruction.A] = stack[--top];
                    continue;
                case OpCode.Load:
                    result = _memory((MemorySlot)instruction.A);
                    break;
                case OpCode.Call:
                    result = Operations.Evaluate((Operation)instruction.A, stack.AsSpan(top - instruction.B, instruction.B), _context);
                    top -= instruction.B;
                    break;
                case OpCode.CallPlugin:
                    result = InvokePlugin(_program.Plugins[instruction.A], stack.AsSpan(top - instruction.B, instruction.B));
                    top -= instruction.B;
                    break;
                case OpCode.Calculus:
                    SourceSpan span = instruction.Span;
                    result = Calculus((CalculusKind)instruction.A, stack.AsSpan(top - instruction.B, instruction.B), instruction.C, instruction.D, ref span);
                    top -= instruction.B;
                    if (!result.Succeeded)
                    {
                        ErrorSpan = span;
                        return result;
                    }

                    stack[top++] = result.Value;
                    continue;
                default:
                    result = EvalResult.Failure((CalcErrorKind)instruction.A);
                    break;
            }

            if (!result.Succeeded)
            {
                ErrorSpan = instruction.Span;
                return result;
            }

            stack[top++] = result.Value;
        }

        return stack[0];
    }

    private EvalResult InvokePlugin(IMathFunction function, ReadOnlySpan<Value> arguments)
    {
        EvalResult result = function.Invoke(arguments, _context);
        if (!result.Succeeded || result.Value.Kind is ValueKind.BaseN or ValueKind.DecimalReal)
        {
            return result;
        }

        // A plugin's double or complex result is held to the same range and precision rule as a built-in one; a
        // complex number with no imaginary part comes back real.
        return ValueMath.Complex(result.Value.ToComplex(), _context);
    }

    private EvalResult Calculus(CalculusKind kind, ReadOnlySpan<Value> bounds, int body, int slot, ref SourceSpan span)
    {
        foreach (Value bound in bounds)
        {
            if (!bound.IsReal)
            {
                return ValueMath.MathError;
            }
        }

        return kind switch
        {
            CalculusKind.Summation or CalculusKind.Product => Series(kind, bounds[0], bounds[1], body, slot, ref span),
            CalculusKind.Integral => Integral(bounds, body, slot, span, ref span),
            _ => Derivative(bounds, body, slot, ref span),
        };
    }

    private EvalResult Series(CalculusKind kind, Value lower, Value upper, int body, int slot, ref SourceSpan span)
    {
        // a and b are integers with −10¹⁰ < a ≤ b < 10¹⁰ (pp. 54-55).
        if (!ValueMath.TryGetInteger(lower, out long a) || !ValueMath.TryGetInteger(upper, out long b)
            || a > b || a <= -10_000_000_000 || b >= 10_000_000_000)
        {
            return ValueMath.MathError;
        }

        Value accumulated = kind == CalculusKind.Summation ? Value.Zero : Value.One;
        for (long x = a; x <= b; x++)
        {
            if (!_context.TryIterate())
            {
                return EvalResult.Failure(CalcErrorKind.TimeOut);
            }

            _locals[slot] = Value.FromDecimal(x);
            EvalResult term = Run(body);
            if (!term.Succeeded)
            {
                span = ErrorSpan;
                return term;
            }

            EvalResult next = kind == CalculusKind.Summation
                ? ValueMath.Add(accumulated, term.Value, _context)
                : ValueMath.Multiply(accumulated, term.Value, _context);
            if (!next.Succeeded)
            {
                return next;
            }

            accumulated = next.Value;
        }

        return accumulated;
    }

    private EvalResult Integral(ReadOnlySpan<Value> bounds, int body, int slot, SourceSpan callSpan, ref SourceSpan span)
    {
        double acceptable = IntegralAcceptable;
        double target = IntegralTarget;
        if (bounds.Length == 3)
        {
            if (!TryGetTolerance(bounds[2], out double tolerance))
            {
                return ValueMath.MathError;
            }

            acceptable = Math.Max(tolerance, IntegralAcceptable);
            target = Math.Max(tolerance, IntegralTarget);
        }

        SourceSpan failure = callSpan;
        bool Integrand(double x, out double value, out CalcErrorKind error)
        {
            value = 0d;
            error = CalcErrorKind.MathError;
            // x lies between two bounds in range, so it is in range too.
            _locals[slot] = Value.FromApproximation(x, form: null);
            EvalResult result = Run(body);
            if (!result.Succeeded || !result.Value.IsReal)
            {
                failure = result.Succeeded ? callSpan : ErrorSpan;
                error = result.Error ?? CalcErrorKind.MathError;
                return false;
            }

            value = result.Value.ToDouble();
            return true;
        }

        (double integral, double estimate, CalcErrorKind? integralError) =
            GaussKronrod.Integrate(Integrand, bounds[0].ToDouble(), bounds[1].ToDouble(), target, acceptable, _context);
        if (integralError is { } kind)
        {
            span = kind == CalcErrorKind.TimeOut ? callSpan : failure;
            return EvalResult.Failure(kind);
        }

        _context.Integrals.Add(new IntegralEstimate(callSpan, integral, estimate));
        return ValueMath.Real(integral, null, _context);
    }

    private EvalResult Derivative(ReadOnlySpan<Value> bounds, int body, int slot, ref SourceSpan span)
    {
        if (bounds.Length == 2 && !TryGetTolerance(bounds[1], out _))
        {
            return ValueMath.MathError;
        }

        // The function must be defined at the point before its derivative means anything: d/dx(ln(x), −1) is a Math ERROR.
        _locals[slot] = bounds[0];
        EvalResult value = Run(body);
        if (!value.Succeeded)
        {
            span = ErrorSpan;
            return value;
        }

        _locals[slot] = bounds[0];
        EvalResult derivative = Run(body + 1);
        if (!derivative.Succeeded)
        {
            span = ErrorSpan;
        }

        return derivative;
    }

    private static bool TryGetTolerance(Value value, out double tolerance)
    {
        tolerance = value.ToDouble();
        return tolerance >= SmallestTolerance;
    }
}
