// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Frozen;
using System.Text;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Writes a syntax tree as LaTeX math, for copying and exporting.
/// </summary>
/// <remarks>
/// The output is meant to typeset the way the calculator displays the expression: fractions as <c>\frac</c>, roots as
/// <c>\sqrt</c>, <c>Σ(x+1,1,5)</c> as a summation. It is not read back; Canonical Linear Syntax is the text form Pallas
/// parses. Calculator-specific commands (<c>▶t</c>, unit conversions, cell references) are written in <c>\mathrm</c>
/// and typeset plainly.
/// </remarks>
public static class LatexPrinter
{
    private static readonly FrozenDictionary<string, string> NamedFunctions = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["sin("] = @"\sin",
        ["cos("] = @"\cos",
        ["tan("] = @"\tan",
        ["sin⁻¹("] = @"\sin^{-1}",
        ["cos⁻¹("] = @"\cos^{-1}",
        ["tan⁻¹("] = @"\tan^{-1}",
        ["sinh("] = @"\sinh",
        ["cosh("] = @"\cosh",
        ["tanh("] = @"\tanh",
        ["sinh⁻¹("] = @"\sinh^{-1}",
        ["cosh⁻¹("] = @"\cosh^{-1}",
        ["tanh⁻¹("] = @"\tanh^{-1}",
        ["ln("] = @"\ln",
        ["log("] = @"\log",
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly char[] TextOnly = ['#', '$', ' '];

    private static readonly FrozenDictionary<char, string> Characters = new Dictionary<char, string>
    {
        ['π'] = @"\pi ",
        ['ħ'] = @"\hbar ",
        ['ε'] = @"\varepsilon ",
        ['μ'] = @"\mu ",
        ['Φ'] = @"\Phi ",
        ['α'] = @"\alpha ",
        ['λ'] = @"\lambda ",
        ['γ'] = @"\gamma ",
        ['τ'] = @"\tau ",
        ['σ'] = @"\sigma ",
        ['Σ'] = @"\Sigma ",
        ['∞'] = @"\infty ",
        ['²'] = "^{2}",
        ['³'] = "^{3}",
        ['⁴'] = "^{4}",
        ['₁'] = "_{1}",
        ['₅'] = "_{5}",
        ['°'] = @"^{\circ}",
        ['·'] = @"\cdot ",
        ['▶'] = @"\blacktriangleright ",
        ['#'] = @"\#",
        ['$'] = @"\$",
        ['%'] = @"\%",
        [' '] = @"\;",
        ['\u0302'] = string.Empty,
        ['\u0304'] = string.Empty,
    }.ToFrozenDictionary();

    /// <summary>Prints a tree.</summary>
    /// <param name="node">The tree.</param>
    /// <returns>LaTeX math-mode source, without surrounding delimiters.</returns>
    public static string Print(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        StringBuilder latex = new();
        Write(latex, node, 0);
        return latex.ToString();
    }

    private static void Write(StringBuilder latex, SyntaxNode node, int minimum)
    {
        if (Precedence.Of(node) < minimum)
        {
            WriteParenthesized(latex, node);
            return;
        }

        switch (node)
        {
            case NumberLiteral number:
                WriteNumber(latex, number.Text);
                break;
            case BaseLiteral literal:
                latex.Append(@"\mathrm{").Append(literal.Base switch
                {
                    NumberBase.Dec => "d",
                    NumberBase.Hex => "h",
                    NumberBase.Bin => "b",
                    _ => "o",
                }).Append(literal.Digits.Text).Append('}');
                break;
            case NameReference name:
                WriteName(latex, name.Symbol);
                break;
            case CellReference cell:
                AppendUpright(latex, cell.Text);
                break;
            case CellRange range:
                AppendUpright(latex, range.Start.Text + ":" + range.End.Text);
                break;
            case NegationExpression negation:
                latex.Append('-');
                Write(latex, negation.Operand, negation.Operand is NegationExpression ? 0 : Precedence.Prefix + 1);
                break;
            case PostfixExpression postfix:
                WritePostfix(latex, postfix);
                break;
            case SuffixCommandExpression suffix:
                Write(latex, suffix.Operand, Precedence.Of(suffix));
                latex.Append(@"\,");
                AppendRoman(latex, suffix.Command.Kind == SymbolKind.EngineeringSymbol ? suffix.Command.Text[1..] : suffix.Command.Text);
                break;
            case BinaryExpression binary:
                WriteBinary(latex, binary);
                break;
            case MixedFractionExpression mixed:
                Write(latex, mixed.Whole, Precedence.Fraction + 1);
                latex.Append(@"\frac{");
                Write(latex, mixed.Numerator, 0);
                latex.Append("}{");
                Write(latex, mixed.Denominator, 0);
                latex.Append('}');
                break;
            case SexagesimalExpression sexagesimal:
                Write(latex, sexagesimal.Degrees, Precedence.Postfix);
                latex.Append(@"^{\circ}").Append(sexagesimal.Minutes?.Text ?? "0").Append('\'')
                    .Append(sexagesimal.Seconds?.Text ?? "0").Append(@"''");
                break;
            case FunctionCall call:
                WriteFunction(latex, call);
                break;
            case ParenthesizedExpression parenthesized:
                WriteParenthesized(latex, parenthesized.Inner);
                break;
            default:
                // The node types are closed; the one left is a relation chain.
                RelationChain chain = (RelationChain)node;
                Write(latex, chain.Operands[0], Precedence.Relation + 1);
                for (int index = 0; index < chain.Operators.Length; index++)
                {
                    latex.Append(chain.Operators[index] switch
                    {
                        RelationOperator.Equal => "=",
                        RelationOperator.NotEqual => @"\neq ",
                        RelationOperator.Less => "<",
                        RelationOperator.Greater => ">",
                        RelationOperator.LessOrEqual => @"\leq ",
                        _ => @"\geq ",
                    });
                    Write(latex, chain.Operands[index + 1], Precedence.Relation + 1);
                }

                break;
        }
    }

    private static void WriteNumber(StringBuilder latex, string text)
    {
        int open = text.IndexOf('(', StringComparison.Ordinal);
        if (open < 0)
        {
            latex.Append(text);
            return;
        }

        // 3.(021) is 3.021021…; the recurring digits carry a bar.
        latex.Append(text, 0, open).Append(@"\overline{").Append(text, open + 1, text.Length - open - 2).Append('}');
    }

    private static void WritePostfix(StringBuilder latex, PostfixExpression postfix)
    {
        switch (postfix.Operator)
        {
            case PostfixOperator.EstimateX or PostfixOperator.EstimateY or PostfixOperator.EstimateX1 or PostfixOperator.EstimateX2:
                Write(latex, postfix.Operand, Precedence.Conversion);
                latex.Append(@"\,").Append(postfix.Operator switch
                {
                    PostfixOperator.EstimateX => @"\hat{x}",
                    PostfixOperator.EstimateY => @"\hat{y}",
                    PostfixOperator.EstimateX1 => @"\hat{x}_{1}",
                    _ => @"\hat{x}_{2}",
                });
                return;
            case PostfixOperator.Factorial or PostfixOperator.Percent or PostfixOperator.StandardizedVariate:
                Write(latex, postfix.Operand, Precedence.Postfix);
                latex.Append(postfix.Operator switch
                {
                    PostfixOperator.Factorial => "!",
                    PostfixOperator.Percent => @"\%",
                    _ => @"\blacktriangleright t",
                });
                return;
            default:
                // A superscript sits on its base: an operand that is itself a power or postfix needs braces, not
                // parentheses, to keep x²³ from reading as x²·³.
                latex.Append('{');
                Write(latex, postfix.Operand, Precedence.Postfix + 1);
                latex.Append("}^{").Append(postfix.Operator switch
                {
                    PostfixOperator.Square => "2",
                    PostfixOperator.Cube => "3",
                    PostfixOperator.Reciprocal => "-1",
                    PostfixOperator.Degrees => @"\circ",
                    PostfixOperator.Radians => @"\mathrm{r}",
                    _ => @"\mathrm{g}",
                }).Append('}');
                return;
        }
    }

    private static void WriteBinary(StringBuilder latex, BinaryExpression binary)
    {
        int precedence = Precedence.Of(binary.Operator);
        switch (binary.Operator)
        {
            case BinaryOperator.Fraction:
                latex.Append(@"\frac{");
                Write(latex, Unwrap(binary.Left), 0);
                latex.Append("}{");
                Write(latex, Unwrap(binary.Right), 0);
                latex.Append('}');
                return;
            case BinaryOperator.Power:
                latex.Append('{');
                Write(latex, binary.Left, precedence + 1);
                latex.Append("}^{");
                Write(latex, Unwrap(binary.Right), 0);
                latex.Append('}');
                return;
            case BinaryOperator.Root:
                latex.Append(@"\sqrt[");
                Write(latex, binary.Left, 0);
                latex.Append("]{");
                Write(latex, Unwrap(binary.Right), 0);
                latex.Append('}');
                return;
            case BinaryOperator.Permutation or BinaryOperator.Combination:
                latex.Append("{}_{");
                Write(latex, binary.Left, 0);
                latex.Append(binary.Operator == BinaryOperator.Permutation ? @"}\mathrm{P}_{" : @"}\mathrm{C}_{");
                Write(latex, binary.Right, 0);
                latex.Append('}');
                return;
        }

        Write(latex, binary.Left, precedence);
        latex.Append(binary.Operator switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => @"\times ",
            BinaryOperator.Divide => @"\div ",
            BinaryOperator.DivideWithRemainder => @"\div_{\mathrm{R}} ",
            BinaryOperator.ImplicitMultiply => StartsWithDigit(binary.Right) ? @"\cdot " : " ",
            BinaryOperator.Polar => @"\angle ",
            BinaryOperator.DotProduct => @"\cdot ",
            BinaryOperator.And => @"\;\mathrm{and}\;",
            BinaryOperator.Or => @"\;\mathrm{or}\;",
            BinaryOperator.Xor => @"\;\mathrm{xor}\;",
            _ => @"\;\mathrm{xnor}\;",
        });
        Write(latex, binary.Right, precedence + 1);
    }

    private static void WriteFunction(StringBuilder latex, FunctionCall call)
    {
        string name = call.Function.Text;
        switch (name)
        {
            case "√(" when call.Arguments.Length == 1:
                latex.Append(@"\sqrt{");
                Write(latex, Unwrap(call.Arguments[0]), 0);
                latex.Append('}');
                return;
            case "Abs(" when call.Arguments.Length == 1:
                latex.Append(@"\left|");
                Write(latex, call.Arguments[0], 0);
                latex.Append(@"\right|");
                return;
            case "log(" when call.Arguments.Length == 2:
                latex.Append(@"\log_{");
                Write(latex, call.Arguments[0], 0);
                latex.Append('}');
                WriteArguments(latex, call.Arguments[1..]);
                return;
            case "Σ(" or "Π(" when call.Arguments.Length == 3:
                latex.Append(name == "Σ(" ? @"\sum_{x=" : @"\prod_{x=");
                Write(latex, call.Arguments[1], 0);
                latex.Append("}^{");
                Write(latex, call.Arguments[2], 0);
                latex.Append("} ");
                Write(latex, call.Arguments[0], Precedence.Implicit + 1);
                return;
            case "∫(" when call.Arguments.Length is 3 or 4:
                latex.Append(@"\int_{");
                Write(latex, call.Arguments[1], 0);
                latex.Append("}^{");
                Write(latex, call.Arguments[2], 0);
                latex.Append("} ");
                Write(latex, call.Arguments[0], Precedence.Implicit + 1);
                latex.Append(@"\,dx");
                return;
            case "d/dx(" when call.Arguments.Length is 2 or 3:
                latex.Append(@"\left.\frac{d}{dx}");
                WriteArguments(latex, call.Arguments[..1]);
                latex.Append(@"\right|_{x=");
                Write(latex, call.Arguments[1], 0);
                latex.Append('}');
                return;
        }

        if (NamedFunctions.TryGetValue(name, out string? named))
        {
            latex.Append(named);
        }
        else if (name.Length == 2)
        {
            // f(, g(, P(, Q(, R(: a one-letter function is written like a variable.
            latex.Append(name[0]);
        }
        else
        {
            AppendUpright(latex, name[..^1]);
        }

        WriteArguments(latex, call.Arguments);
    }

    private static void WriteArguments(StringBuilder latex, IReadOnlyList<SyntaxNode> arguments)
    {
        latex.Append(@"\left(");
        for (int index = 0; index < arguments.Count; index++)
        {
            if (index > 0)
            {
                latex.Append(", ");
            }

            Write(latex, arguments[index], Precedence.Relation + 1);
        }

        latex.Append(@"\right)");
    }

    private static void WriteName(StringBuilder latex, SyntaxSymbol symbol)
    {
        string text = symbol.Text;
        switch (text)
        {
            case "x̄":
                latex.Append(@"\bar{x}");
                return;
            case "ȳ":
                latex.Append(@"\bar{y}");
                return;
            case "Ran#":
                AppendUpright(latex, text);
                return;
        }

        if (symbol.Kind == SymbolKind.ScientificConstant)
        {
            // @N_A → N_{A}; @R_K-90 → R_{K-90}.
            string name = text[1..];
            int underscore = name.IndexOf('_', StringComparison.Ordinal);
            string letters = underscore < 0 ? name : name[..underscore];
            if (letters.Length > 1 && letters.All(char.IsAsciiLetter))
            {
                AppendRoman(latex, letters);
            }
            else
            {
                AppendCharacters(latex, letters);
            }
            if (underscore >= 0)
            {
                latex.Append("_{");
                AppendCharacters(latex, name[(underscore + 1)..]);
                latex.Append('}');
            }

            return;
        }

        if (text.Length > 1 && text.All(char.IsAsciiLetterOrDigit))
        {
            AppendRoman(latex, text);
            return;
        }

        AppendCharacters(latex, text);
    }

    /// <remarks>
    /// WpfMath, the renderer of the desktop application, has no <c>\operatorname</c>, and <c>\#</c> and <c>\$</c> are
    /// commands only <c>\text</c> carries (measured 23 Sep 2026). A name with one of those characters, or with a
    /// space, is therefore written as <c>\text</c> and everything else as <c>\mathrm</c>.
    /// </remarks>
    private static void AppendUpright(StringBuilder latex, string text)
    {
        if (text.IndexOfAny(TextOnly) < 0)
        {
            AppendRoman(latex, text);
            return;
        }

        latex.Append(@"\text{");
        foreach (char character in text)
        {
            if (character is '#' or '$')
            {
                latex.Append('\\');
            }

            latex.Append(character);
        }

        latex.Append('}');
    }

    private static void AppendRoman(StringBuilder latex, string text)
    {
        latex.Append(@"\mathrm{");
        AppendCharacters(latex, text);
        latex.Append('}');
    }

    private static void AppendCharacters(StringBuilder latex, string text)
    {
        // Two subscript digits form one subscript: cal₁₅ is cal_{15}, not cal_{1}_{5}.
        foreach (char character in text.Replace("₁₅", "_{15}", StringComparison.Ordinal))
        {
            if (Characters.TryGetValue(character, out string? replacement))
            {
                latex.Append(replacement);
            }
            else
            {
                latex.Append(character);
            }
        }
    }

    private static void WriteParenthesized(StringBuilder latex, SyntaxNode node)
    {
        latex.Append(@"\left(");
        Write(latex, node, 0);
        latex.Append(@"\right)");
    }

    private static SyntaxNode Unwrap(SyntaxNode node) => node is ParenthesizedExpression parenthesized ? parenthesized.Inner : node;

    private static bool StartsWithDigit(SyntaxNode node)
    {
        return node switch
        {
            NumberLiteral => true,
            BinaryExpression { Operator: not (BinaryOperator.Fraction or BinaryOperator.Root or BinaryOperator.Permutation or BinaryOperator.Combination) } binary => StartsWithDigit(binary.Left),
            PostfixExpression postfix => StartsWithDigit(postfix.Operand),
            SuffixCommandExpression suffix => StartsWithDigit(suffix.Operand),
            MixedFractionExpression mixed => StartsWithDigit(mixed.Whole),
            SexagesimalExpression sexagesimal => StartsWithDigit(sexagesimal.Degrees),
            _ => false,
        };
    }
}
