// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Differentiates a bound tree with respect to one local variable, for d/dx.
/// </summary>
/// <remarks>
/// <para>
/// The derivative is another bound tree, evaluated like any expression, so it is exact where the operations are:
/// <c>d/dx(x³, 0.1)</c> is exactly 0.03 in <see cref="decimal"/>, and <c>d/dx(sin(x), π÷2)</c> is <c>cos(π/2)</c>, exactly 0.
/// The calculator differentiates numerically with a tolerance (p. 52); Pallas refuses where there is no derivative, as
/// deviation D5 records: the derivative of <c>Abs(x)</c> is <c>x÷Abs(x)</c>, a division by zero at 0.
/// </para>
/// <para>
/// Subtrees that do not depend on the variable have derivative 0, whatever they contain, so <c>d/dx(x×Ran#, 1)</c> is
/// defined. A function without a derivative (<c>x!</c>, <c>GCD(</c>, a plugin) that depends on the variable becomes a
/// Math ERROR, raised only if that part of the derivative is evaluated.
/// </para>
/// </remarks>
internal sealed class Differentiator
{
    private readonly AngleUnit _angleUnit;

    private Differentiator(AngleUnit angleUnit)
    {
        _angleUnit = angleUnit;
    }

    public static BoundNode Differentiate(BoundNode body, int slot, AngleUnit angleUnit, ref int nextSlot)
    {
        _ = nextSlot;
        return new Differentiator(angleUnit).Derive(body, slot);
    }

    private BoundNode Derive(BoundNode node, int slot)
    {
        SourceSpan span = node.Span;
        switch (node)
        {
            case BoundLocal local:
                return local.Slot == slot ? Constant(1m, span) : Constant(0m, span);
            case BoundConstant or BoundMemory or BoundStatistic or BoundCells:
                return Constant(0m, span);
            case BoundFailure:
                return node;
            case BoundLet let:
            {
                // d/dx f(v) = f'(v)·v', plus the derivative of f's body in x itself when it has one.
                BoundNode direct = new BoundLet(let.Slot, let.Value, Derive(let.Body, slot), span);
                BoundNode chain = Multiply(new BoundLet(let.Slot, let.Value, Derive(let.Body, let.Slot), span), Derive(let.Value, slot), span);
                return Add(Simplified(direct), chain, span);
            }

            case BoundPluginCall plugin:
                return plugin.Arguments.Any(argument => DependsOn(argument, slot)) ? new BoundFailure(CalcErrorKind.MathError, span) : Constant(0m, span);
            case BoundCalculus calculus:
                return DeriveCalculus(calculus, slot);
            case BoundCall call:
                return DeriveCall(call, slot);
            default:
                return new BoundFailure(CalcErrorKind.MathError, span);
        }
    }

    private BoundNode DeriveCalculus(BoundCalculus calculus, int slot)
    {
        SourceSpan span = calculus.Span;
        if (!calculus.Arguments.Any(argument => DependsOn(argument, slot)))
        {
            return Constant(0m, span);
        }

        switch (calculus.Kind)
        {
            case CalculusKind.Integral:
            {
                // Leibniz: d/dx ∫(f, a(x), b(x)) = f(b)·b' − f(a)·a'.
                BoundNode lower = calculus.Arguments[0];
                BoundNode upper = calculus.Arguments[1];
                BoundNode atUpper = Multiply(new BoundLet(calculus.Slot, upper, calculus.Body, span), Derive(upper, slot), span);
                BoundNode atLower = Multiply(new BoundLet(calculus.Slot, lower, calculus.Body, span), Derive(lower, slot), span);
                return Subtract(atUpper, atLower, span);
            }

            case CalculusKind.Derivative:
            {
                // d/dx g'(p(x)) = g''(p)·p'.
                BoundNode point = calculus.Arguments[0];
                BoundNode second = Derive(calculus.Derivative!, calculus.Slot);
                return Multiply(new BoundLet(calculus.Slot, point, second, span), Derive(point, slot), span);
            }

            default:
                // Σ and Π have integer bounds: no derivative in them.
                return new BoundFailure(CalcErrorKind.MathError, span);
        }
    }

    private BoundNode DeriveCall(BoundCall call, int slot)
    {
        SourceSpan span = call.Span;
        ImmutableArray<BoundNode> arguments = call.Arguments;
        if (!arguments.Any(argument => DependsOn(argument, slot)))
        {
            return Constant(0m, span);
        }

        BoundNode u = arguments[0];
        BoundNode du = Derive(u, slot);
        switch (call.Operation)
        {
            case Operation.Negate:
                return Negate(du, span);
            case Operation.Add:
                return Add(du, Derive(arguments[1], slot), span);
            case Operation.Subtract:
                return Subtract(du, Derive(arguments[1], slot), span);
            case Operation.Multiply:
                return Add(Multiply(du, arguments[1], span), Multiply(u, Derive(arguments[1], slot), span), span);
            case Operation.Divide:
            {
                BoundNode v = arguments[1];
                BoundNode dv = Derive(v, slot);
                return IsZero(dv)
                    ? Divide(du, v, span)
                    : Divide(Subtract(Multiply(du, v, span), Multiply(u, dv, span), span), Call(Operation.Square, span, v), span);
            }

            case Operation.Power:
                return DerivePower(u, arguments[1], du, Derive(arguments[1], slot), span);
            case Operation.Root:
            {
                // ⁿ√v = v^(1/n); with a constant index its derivative is ⁿ√v·v'/(n·v).
                BoundNode index = u;
                BoundNode radicand = arguments[1];
                BoundNode dRadicand = Derive(radicand, slot);
                BoundNode root = call;
                BoundNode byRadicand = Divide(Multiply(root, dRadicand, span), Multiply(index, radicand, span), span);
                if (IsZero(du))
                {
                    return byRadicand;
                }

                BoundNode byIndex = Divide(Multiply(Multiply(root, Call(Operation.Ln, span, radicand), span), du, span), Call(Operation.Square, span, index), span);
                return Subtract(byRadicand, byIndex, span);
            }

            case Operation.Square:
                return Multiply(Multiply(Constant(2m, span), u, span), du, span);
            case Operation.Cube:
                return Multiply(Multiply(Constant(3m, span), Call(Operation.Square, span, u), span), du, span);
            case Operation.Reciprocal:
                return Negate(Divide(du, Call(Operation.Square, span, u), span), span);
            case Operation.Percent:
                return Divide(du, Constant(100m, span), span);
            case Operation.MixedFraction:
                return DependsOn(arguments[1], slot) || DependsOn(arguments[2], slot) ? new BoundFailure(CalcErrorKind.MathError, span) : du;
            case Operation.Sexagesimal:
            case Operation.DegreesAngle:
            case Operation.RadiansAngle:
            case Operation.GradiansAngle:
                // Linear in the angle: the conversion factor applies to the derivative too.
                return call.Operation == Operation.Sexagesimal ? du : Call(call.Operation, span, du);
            case Operation.Absolute:
                return Divide(Multiply(du, u, span), call, span);
            case Operation.IntegerPart:
                return Call(Operation.IntegerPartSlope, span, u);
            case Operation.LargestInteger:
                return Call(Operation.LargestIntegerSlope, span, u);
            case Operation.Round:
                return Call(Operation.RoundSlope, span, u);
            case Operation.IntegerPartSlope:
            case Operation.LargestIntegerSlope:
            case Operation.RoundSlope:
                return call;
            case Operation.SquareRoot:
                return Divide(du, Multiply(Constant(2m, span), call, span), span);
            case Operation.Exp:
                return Multiply(call, du, span);
            case Operation.Ln:
                return Divide(du, u, span);
            case Operation.Log10:
                return Divide(du, Multiply(u, Call(Operation.Ln, span, Constant(10m, span)), span), span);
            case Operation.LogBase:
                return Derive(Divide(Call(Operation.Ln, span, arguments[1]), Call(Operation.Ln, span, u), span), slot);
            case Operation.Sin:
                return Multiply(Multiply(Call(Operation.Cos, span, u), du, span), AngleScale(span), span);
            case Operation.Cos:
                return Negate(Multiply(Multiply(Call(Operation.Sin, span, u), du, span), AngleScale(span), span), span);
            case Operation.Tan:
                return Divide(Multiply(du, AngleScale(span), span), Call(Operation.Square, span, Call(Operation.Cos, span, u)), span);
            case Operation.Asin:
                return Divide(du, Multiply(AngleScale(span), Call(Operation.SquareRoot, span, Subtract(Constant(1m, span), Call(Operation.Square, span, u), span)), span), span);
            case Operation.Acos:
                return Negate(Divide(du, Multiply(AngleScale(span), Call(Operation.SquareRoot, span, Subtract(Constant(1m, span), Call(Operation.Square, span, u), span)), span), span), span);
            case Operation.Atan:
                return Divide(du, Multiply(AngleScale(span), Add(Constant(1m, span), Call(Operation.Square, span, u), span), span), span);
            case Operation.Sinh:
                return Multiply(Call(Operation.Cosh, span, u), du, span);
            case Operation.Cosh:
                return Multiply(Call(Operation.Sinh, span, u), du, span);
            case Operation.Tanh:
                return Divide(du, Call(Operation.Square, span, Call(Operation.Cosh, span, u)), span);
            case Operation.Asinh:
                return Divide(du, Call(Operation.SquareRoot, span, Add(Call(Operation.Square, span, u), Constant(1m, span), span)), span);
            case Operation.Acosh:
                return Divide(du, Call(Operation.SquareRoot, span, Subtract(Call(Operation.Square, span, u), Constant(1m, span), span)), span);
            case Operation.Atanh:
                return Divide(du, Subtract(Constant(1m, span), Call(Operation.Square, span, u), span), span);
            default:
                // x!, nPr, nCr, GCD, LCM, random numbers, ÷R, atomic weights, complex and Base-N operations.
                return new BoundFailure(CalcErrorKind.MathError, span);
        }
    }

    private static BoundNode DerivePower(BoundNode u, BoundNode v, BoundNode du, BoundNode dv, SourceSpan span)
    {
        BoundNode power = Call(Operation.Power, span, u, v);
        if (IsZero(dv))
        {
            // v·u^(v−1)·u'
            BoundNode lowered = Call(Operation.Power, span, u, Subtract(v, Constant(1m, span), span));
            return Multiply(Multiply(v, lowered, span), du, span);
        }

        BoundNode byExponent = Multiply(Call(Operation.Ln, span, u), dv, span);
        if (IsZero(du))
        {
            return Multiply(power, byExponent, span);
        }

        // u^v·(v'·ln u + v·u'/u)
        return Multiply(power, Add(byExponent, Divide(Multiply(v, du, span), u, span), span), span);
    }

    /// <summary>The derivative of an angle in the current unit with respect to radians: π/180 for degrees, π/200 for gradians.</summary>
    private BoundNode AngleScale(SourceSpan span)
    {
        return _angleUnit switch
        {
            AngleUnit.Degree => Divide(new BoundConstant(Value.Pi, span), Constant(180m, span), span),
            AngleUnit.Gradian => Divide(new BoundConstant(Value.Pi, span), Constant(200m, span), span),
            _ => Constant(1m, span),
        };
    }

    private static bool DependsOn(BoundNode node, int slot)
    {
        return node switch
        {
            BoundLocal local => local.Slot == slot,
            BoundCall call => call.Arguments.Any(argument => DependsOn(argument, slot)),
            BoundPluginCall plugin => plugin.Arguments.Any(argument => DependsOn(argument, slot)),
            BoundLet let => DependsOn(let.Value, slot) || (let.Slot != slot && DependsOn(let.Body, slot)),
            BoundCalculus calculus => calculus.Arguments.Any(argument => DependsOn(argument, slot))
                || (calculus.Slot != slot && (DependsOn(calculus.Body, slot) || (calculus.Derivative is not null && DependsOn(calculus.Derivative, slot)))),
            _ => false,
        };
    }

    private static BoundNode Simplified(BoundNode let) => let is BoundLet { Body: BoundConstant constant } && IsZero(constant) ? constant : let;

    private static bool IsZero(BoundNode node) => node is BoundConstant { Value: { IsExact: true, IsZero: true } };

    private static bool IsOne(BoundNode node) => node is BoundConstant constant && constant.Value.IsExact && constant.Value.Kind == ValueKind.DecimalReal && constant.Value.ToDecimal() == 1m;

    private static BoundConstant Constant(decimal value, SourceSpan span) => new(Value.FromDecimal(value), span);

    private static BoundCall Call(Operation operation, SourceSpan span, params ImmutableArray<BoundNode> arguments) => new(operation, arguments, span);

    private static BoundNode Add(BoundNode left, BoundNode right, SourceSpan span)
    {
        return IsZero(left) ? right : IsZero(right) ? left : Call(Operation.Add, span, left, right);
    }

    private static BoundNode Subtract(BoundNode left, BoundNode right, SourceSpan span)
    {
        return IsZero(right) ? left : IsZero(left) ? Negate(right, span) : Call(Operation.Subtract, span, left, right);
    }

    private static BoundNode Negate(BoundNode operand, SourceSpan span)
    {
        return IsZero(operand) ? operand : Call(Operation.Negate, span, operand);
    }

    private static BoundNode Multiply(BoundNode left, BoundNode right, SourceSpan span)
    {
        if (IsZero(left) || IsOne(right))
        {
            return left;
        }

        return IsZero(right) || IsOne(left) ? right : Call(Operation.Multiply, span, left, right);
    }

    private static BoundNode Divide(BoundNode left, BoundNode right, SourceSpan span)
    {
        return IsZero(left) || IsOne(right) ? left : Call(Operation.Divide, span, left, right);
    }
}
