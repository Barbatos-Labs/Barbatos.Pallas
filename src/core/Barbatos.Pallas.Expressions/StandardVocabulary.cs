// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The symbols of <see cref="SyntaxVocabulary.Standard"/>, transcribed from the reference calculator's user's guide
/// (docs/CALCULATOR-CATALOG.md) with the spellings of docs/LINEAR-SYNTAX.md.
/// </summary>
internal static class StandardVocabulary
{
    /// <summary>The 47 scientific constants of the CATALOG (pp. 64-65), each written with <c>@</c>.</summary>
    private static readonly string[] ScientificConstants =
    [
        "h", "ħ", "c", "ε_0", "μ_0", "Z_0", "G", "l_P", "t_P",
        "μ_N", "μ_B", "e", "Φ_0", "G_0", "K_J", "R_K",
        "m_p", "m_n", "m_e", "m_μ", "a_0", "α", "r_e", "λ_C", "γ_p", "λ_Cp", "λ_Cn", "R_∞", "μ_p", "μ_e", "μ_n", "μ_μ", "m_τ",
        "m_u", "F", "N_A", "k", "V_m", "R", "c_1", "c_2", "σ",
        "g_n", "atm", "R_K-90", "K_J-90",
        "t",
    ];

    /// <summary>The 40 unit conversion commands (p. 66), in pairs.</summary>
    private static readonly (string From, string To)[] UnitPairs =
    [
        ("in", "cm"), ("ft", "m"), ("yd", "m"), ("mile", "km"), ("n mile", "m"), ("pc", "km"),
        ("acre", "m²"),
        ("gal(US)", "L"), ("gal(UK)", "L"),
        ("oz", "g"), ("lb", "kg"),
        ("km/h", "m/s"),
        ("atm", "Pa"), ("mmHg", "Pa"), ("kgf/cm²", "Pa"), ("lbf/in²", "kPa"),
        ("kgf·m", "J"), ("J", "cal₁₅"),
        ("hp", "kW"),
        ("°F", "°C"),
    ];

    /// <summary>ASCII spellings of the Greek letters and signs used in scientific constant names.</summary>
    private static readonly (string Unicode, string Ascii)[] GreekLetters =
    [
        ("ħ", "hbar"), ("ε", "epsilon"), ("μ", "mu"), ("Φ", "Phi"), ("α", "alpha"), ("λ", "lambda"), ("γ", "gamma"),
        ("τ", "tau"), ("σ", "sigma"), ("∞", "inf"),
    ];

    public static List<SyntaxSymbol> Create()
    {
        List<SyntaxSymbol> symbols = [];

        AddGrammar(symbols);
        AddFunctions(symbols);
        AddValues(symbols);
        AddScientificConstants(symbols);
        AddUnitConversions(symbols);

        return symbols;
    }

    private static void AddGrammar(List<SyntaxSymbol> symbols)
    {
        Add(symbols, SyntaxSymbol.CreatePunctuation("(", SymbolKind.OpenParenthesis));
        Add(symbols, SyntaxSymbol.CreatePunctuation(")", SymbolKind.CloseParenthesis));
        Add(symbols, SyntaxSymbol.CreatePunctuation(",", SymbolKind.Comma));
        Add(symbols, SyntaxSymbol.CreatePunctuation(":", SymbolKind.Colon, CalculatorApp.Spreadsheet));

        Add(symbols, SyntaxSymbol.CreateBinary("+", BinaryOperator.Add));
        Add(symbols, SyntaxSymbol.CreateBinary("-", BinaryOperator.Subtract), "−");
        Add(symbols, SyntaxSymbol.CreateBinary("×", BinaryOperator.Multiply), "*");
        Add(symbols, SyntaxSymbol.CreateBinary("÷", BinaryOperator.Divide), "/");
        Add(symbols, SyntaxSymbol.CreateBinary("÷R", BinaryOperator.DivideWithRemainder));
        Add(symbols, SyntaxSymbol.CreateBinary("^", BinaryOperator.Power));
        Add(symbols, SyntaxSymbol.CreateBinary("ˣ√", BinaryOperator.Root));
        Add(symbols, SyntaxSymbol.CreateBinary("⌟", BinaryOperator.Fraction));
        Add(symbols, SyntaxSymbol.CreateBinary("P", BinaryOperator.Permutation));
        Add(symbols, SyntaxSymbol.CreateBinary("∠", BinaryOperator.Polar, CalculatorApp.Complex));
        Add(symbols, SyntaxSymbol.CreateBinary("•", BinaryOperator.DotProduct, CalculatorApp.Vector));
        Add(symbols, SyntaxSymbol.CreateBinary("and", BinaryOperator.And, CalculatorApp.BaseN));
        Add(symbols, SyntaxSymbol.CreateBinary("or", BinaryOperator.Or, CalculatorApp.BaseN));
        Add(symbols, SyntaxSymbol.CreateBinary("xor", BinaryOperator.Xor, CalculatorApp.BaseN));
        Add(symbols, SyntaxSymbol.CreateBinary("xnor", BinaryOperator.Xnor, CalculatorApp.BaseN));

        Add(symbols, SyntaxSymbol.CreatePostfix("²", PostfixOperator.Square));
        Add(symbols, SyntaxSymbol.CreatePostfix("³", PostfixOperator.Cube));
        Add(symbols, SyntaxSymbol.CreatePostfix("⁻¹", PostfixOperator.Reciprocal));
        Add(symbols, SyntaxSymbol.CreatePostfix("!", PostfixOperator.Factorial));
        Add(symbols, SyntaxSymbol.CreatePostfix("%", PostfixOperator.Percent));
        Add(symbols, SyntaxSymbol.CreatePostfix("°", PostfixOperator.Degrees));
        Add(symbols, SyntaxSymbol.CreatePostfix("ʳ", PostfixOperator.Radians));
        Add(symbols, SyntaxSymbol.CreatePostfix("ᵍ", PostfixOperator.Gradians));
        Add(symbols, SyntaxSymbol.CreatePostfix("▶t", PostfixOperator.StandardizedVariate, CalculatorApp.Statistics), "->t");
        Add(symbols, SyntaxSymbol.CreatePostfix("x̂", PostfixOperator.EstimateX, CalculatorApp.Statistics));
        Add(symbols, SyntaxSymbol.CreatePostfix("ŷ", PostfixOperator.EstimateY, CalculatorApp.Statistics));
        Add(symbols, SyntaxSymbol.CreatePostfix("x̂₁", PostfixOperator.EstimateX1, CalculatorApp.Statistics));
        Add(symbols, SyntaxSymbol.CreatePostfix("x̂₂", PostfixOperator.EstimateX2, CalculatorApp.Statistics));

        Add(symbols, SyntaxSymbol.CreatePunctuation("′", SymbolKind.SexagesimalMark), "'");
        Add(symbols, SyntaxSymbol.CreatePunctuation("″", SymbolKind.SexagesimalMark), "\"");

        // No ASCII alias for ≠: "!=" would swallow the factorial in "3!=6".
        Add(symbols, SyntaxSymbol.CreateRelation("=", RelationOperator.Equal));
        Add(symbols, SyntaxSymbol.CreateRelation("≠", RelationOperator.NotEqual));
        Add(symbols, SyntaxSymbol.CreateRelation("<", RelationOperator.Less));
        Add(symbols, SyntaxSymbol.CreateRelation(">", RelationOperator.Greater));
        Add(symbols, SyntaxSymbol.CreateRelation("≤", RelationOperator.LessOrEqual), "<=");
        Add(symbols, SyntaxSymbol.CreateRelation("≥", RelationOperator.GreaterOrEqual), ">=");

        foreach (string symbol in (string[])["m", "μ", "n", "p", "f", "k", "M", "G", "T", "P", "E"])
        {
            SyntaxSymbol engineering = SyntaxSymbol.CreateName("_" + symbol, SymbolKind.EngineeringSymbol);
            if (symbol == "μ")
            {
                // "_u" is the ASCII spelling; U+00B5 MICRO SIGN is what many keyboards produce, and Unicode
                // normalization form C does not turn it into U+03BC.
                Add(symbols, engineering, "_u", "_µ");
            }
            else
            {
                Add(symbols, engineering);
            }
        }
    }

    private static void AddFunctions(List<SyntaxSymbol> symbols)
    {
        AddFunction(symbols, "sin(");
        AddFunction(symbols, "cos(");
        AddFunction(symbols, "tan(");
        AddFunction(symbols, "sin⁻¹(", [], "asin(");
        AddFunction(symbols, "cos⁻¹(", [], "acos(");
        AddFunction(symbols, "tan⁻¹(", [], "atan(");
        AddFunction(symbols, "sinh(");
        AddFunction(symbols, "cosh(");
        AddFunction(symbols, "tanh(");
        AddFunction(symbols, "sinh⁻¹(", [], "asinh(");
        AddFunction(symbols, "cosh⁻¹(", [], "acosh(");
        AddFunction(symbols, "tanh⁻¹(", [], "atanh(");
        AddFunction(symbols, "log(");
        AddFunction(symbols, "ln(");
        AddFunction(symbols, "√(", [], "sqrt(");
        AddFunction(symbols, "Abs(");
        AddFunction(symbols, "Int(");
        AddFunction(symbols, "Intg(");
        AddFunction(symbols, "Rnd(");
        AddFunction(symbols, "GCD(");
        AddFunction(symbols, "LCM(");
        AddFunction(symbols, "Pol(");
        AddFunction(symbols, "Rec(");
        AddFunction(symbols, "RanInt#(");
        AddFunction(symbols, "AtWt(");
        AddFunction(symbols, "d/dx(", [], "diff(");
        AddFunction(symbols, "∫(", [], "integral(");
        AddFunction(symbols, "Σ(", [], "sum(");
        AddFunction(symbols, "Π(", [], "product(");
        AddFunction(symbols, "f(");
        AddFunction(symbols, "g(");

        // root(n,x) is input-only: the parser turns it into nˣ√(x), which is what printers write.
        AddFunction(symbols, "root(");

        AddFunction(symbols, "Conjg(", [CalculatorApp.Complex]);
        AddFunction(symbols, "Arg(", [CalculatorApp.Complex]);
        AddFunction(symbols, "ReP(", [CalculatorApp.Complex]);
        AddFunction(symbols, "ImP(", [CalculatorApp.Complex]);
        AddFunction(symbols, "Not(", [CalculatorApp.BaseN]);
        AddFunction(symbols, "Neg(", [CalculatorApp.BaseN]);
        AddFunction(symbols, "Det(", [CalculatorApp.Matrix]);
        AddFunction(symbols, "Trn(", [CalculatorApp.Matrix]);
        AddFunction(symbols, "Identity(", [CalculatorApp.Matrix]);
        AddFunction(symbols, "Angle(", [CalculatorApp.Vector]);
        AddFunction(symbols, "UnitV(", [CalculatorApp.Vector]);
        AddFunction(symbols, "P(", [CalculatorApp.Statistics]);
        AddFunction(symbols, "Q(", [CalculatorApp.Statistics]);
        AddFunction(symbols, "R(", [CalculatorApp.Statistics]);
        AddFunction(symbols, "Min(", [CalculatorApp.Spreadsheet]);
        AddFunction(symbols, "Max(", [CalculatorApp.Spreadsheet]);
        AddFunction(symbols, "Mean(", [CalculatorApp.Spreadsheet]);
        AddFunction(symbols, "Sum(", [CalculatorApp.Spreadsheet]);
    }

    private static void AddValues(List<SyntaxSymbol> symbols)
    {
        Add(symbols, SyntaxSymbol.CreateName("π", SymbolKind.Constant), "pi");
        Add(symbols, SyntaxSymbol.CreateName("e", SymbolKind.Constant));
        Add(symbols, SyntaxSymbol.CreateName("Ran#", SymbolKind.Constant));
        Add(symbols, SyntaxSymbol.CreateName("i", SymbolKind.Constant, CalculatorApp.Complex));

        // C is also the combination operator when it stands between two operands (docs/LINEAR-SYNTAX.md).
        foreach (string variable in (string[])["A", "B", "C", "D", "E", "F", "x", "y", "z"])
        {
            Add(symbols, SyntaxSymbol.CreateName(variable, SymbolKind.Variable));
        }

        Add(symbols, SyntaxSymbol.CreateName("Ans", SymbolKind.Memory));
        Add(symbols, SyntaxSymbol.CreateName("PreAns", SymbolKind.Memory));

        foreach (string suffix in (string[])["A", "B", "C", "D", "Ans"])
        {
            Add(symbols, SyntaxSymbol.CreateName("Mat" + suffix, SymbolKind.MatrixVariable, CalculatorApp.Matrix));
            Add(symbols, SyntaxSymbol.CreateName("Vct" + suffix, SymbolKind.VectorVariable, CalculatorApp.Vector));
        }

        // Statistic variables (pp. 90-91). "min(x)" is one name, as on the calculator's menu.
        foreach (string statistic in (string[])
            [
                "Σx", "Σy", "Σx²", "Σy²", "Σxy", "Σx³", "Σx²y", "Σx⁴",
                "x̄", "ȳ", "σ²x", "σ²y", "σx", "σy", "s²x", "s²y", "sx", "sy", "n",
                "min(x)", "max(x)", "min(y)", "max(y)", "Q1", "Med", "Q3",
                "a", "b", "c", "r",
            ])
        {
            Add(symbols, SyntaxSymbol.CreateName(statistic, SymbolKind.StatisticsVariable, CalculatorApp.Statistics));
        }
    }

    private static void AddScientificConstants(List<SyntaxSymbol> symbols)
    {
        foreach (string name in ScientificConstants)
        {
            string canonical = "@" + name;
            string ascii = canonical;
            foreach ((string unicode, string letters) in GreekLetters)
            {
                ascii = ascii.Replace(unicode, letters, StringComparison.Ordinal);
            }

            string subscripts = canonical
                .Replace("_0", "₀", StringComparison.Ordinal)
                .Replace("_1", "₁", StringComparison.Ordinal)
                .Replace("_2", "₂", StringComparison.Ordinal)
                .Replace("_∞", "∞", StringComparison.Ordinal);

            Add(symbols, SyntaxSymbol.CreateName(canonical, SymbolKind.ScientificConstant), Distinct(canonical, ascii, subscripts));
        }
    }

    private static void AddUnitConversions(List<SyntaxSymbol> symbols)
    {
        foreach ((string from, string to) in UnitPairs)
        {
            AddUnitConversion(symbols, from + "▶" + to);
            AddUnitConversion(symbols, to + "▶" + from);
        }
    }

    private static void AddUnitConversion(List<SyntaxSymbol> symbols, string canonical)
    {
        string ascii = canonical
            .Replace("▶", "->", StringComparison.Ordinal)
            .Replace("²", "2", StringComparison.Ordinal)
            .Replace("₁₅", "15", StringComparison.Ordinal)
            .Replace("·", ".", StringComparison.Ordinal);

        Add(symbols, SyntaxSymbol.CreateName(canonical, SymbolKind.UnitConversion), Distinct(canonical, ascii));
    }

    private static void AddFunction(List<SyntaxSymbol> symbols, string name, CalculatorApp[]? applications = null, params string[] aliases)
    {
        Add(symbols, SyntaxSymbol.CreateName(name, SymbolKind.Function, applications ?? []), aliases);
    }

    private static void Add(List<SyntaxSymbol> symbols, SyntaxSymbol symbol, params string[] aliases)
    {
        symbols.Add(symbol);
        foreach (string alias in aliases)
        {
            symbols.Add(symbol.CreateAlias(alias));
        }
    }

    private static string[] Distinct(string canonical, params string[] spellings)
    {
        return [.. spellings.Where(spelling => spelling != canonical).Distinct(StringComparer.Ordinal)];
    }
}
