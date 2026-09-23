// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.IO;
using System.Text.Json;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// What <see cref="LatexPrinter"/> prints, WpfMath draws. The printer is display-only and never read back, so this
/// is the only thing that makes its output true.
/// </summary>
/// <remarks>
/// Three commands cost an afternoon on 23 Sep 2026, all of them silent until something tried to draw them:
/// <c>\operatorname</c> does not exist in WpfMath, <c>\#</c> and <c>\$</c> are commands only <c>\text</c> carries,
/// and <c>\mathbin</c> and <c>\quad</c> are missing as well. Hence this file.
/// </remarks>
public sealed class LatexRenderingTests
{
    public static TheoryData<string, CalculatorApp> ManualInputs()
    {
        TheoryData<string, CalculatorApp> data = [];
        foreach ((string input, CalculatorApp app) in ConformanceInputs())
        {
            data.Add(input, app);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ManualInputs))]
    public void EveryInputOfTheManualIsDrawn(string input, CalculatorApp app)
    {
        ParseResult result = ExpressionParser.Parse(input, new SyntaxContext(app, AllowRelations: true));
        if (result.Root is null)
        {
            return;
        }

        string latex = LatexPrinter.Print(result.Root);

        Formula.Fault(latex).Should().BeNull("'{0}' prints as '{1}'", input, latex);
    }

    [Theory]
    // One input per branch of the printer that the manual's own examples do not reach.
    [InlineData(CalculatorApp.Calculate, "GCD(28,35)+LCM(9,15)+Int(3.7)+Intg(-2.5)+Rnd(1.23)")]
    [InlineData(CalculatorApp.Calculate, "RanInt#(1,6)+Ran#")]
    [InlineData(CalculatorApp.Calculate, "Abs(-3)+Pol(1,1)+Rec(2,30)+AtWt(6)")]
    [InlineData(CalculatorApp.Calculate, "1⌟2+3⌟1⌟4+√(2)+3ˣ√(8)+2^(10)+5²+4³+2⁻¹")]
    [InlineData(CalculatorApp.Calculate, "log(2,16)+ln(e)+log(100)+sin⁻¹(0.5)+cosh(1)+tanh⁻¹(0.5)")]
    [InlineData(CalculatorApp.Calculate, "d/dx(x³,0.1)+∫(x²,0,1)+Σ(x+1,1,5)+Π(x,1,5)")]
    [InlineData(CalculatorApp.Calculate, "5!+150×20%+10P4+10C4+7÷R2")]
    [InlineData(CalculatorApp.Calculate, "2°20′30″+sin(30°)+(π÷2)ʳ+100ᵍ")]
    [InlineData(CalculatorApp.Calculate, "3.(021)+0.1(6)")]
    [InlineData(CalculatorApp.Calculate, "@h+@N_A+@ε_0+@R_K-90+@hbar+@R_inf")]
    [InlineData(CalculatorApp.Calculate, "999_k+25_μ+3_E")]
    [InlineData(CalculatorApp.Calculate, "5cm▶in+2kgf·m▶J+5n mile▶m+2J▶cal₁₅")]
    [InlineData(CalculatorApp.Calculate, "Ans+PreAns+A+B+C+D+E+F+x+y+z+π+e")]
    [InlineData(CalculatorApp.Calculate, "f(2)+g(3)")]
    [InlineData(CalculatorApp.Complex, "2+3i+Conjg(1+i)+Arg(i)+ReP(1+i)+ImP(1+i)+2∠45")]
    [InlineData(CalculatorApp.BaseN, "d10+h1F+b1010+o17")]
    [InlineData(CalculatorApp.BaseN, "1010 and 1100 or h1F xor b1 xnor d9")]
    [InlineData(CalculatorApp.BaseN, "Not(1010)+Neg(1)")]
    [InlineData(CalculatorApp.Matrix, "Det(MatA)+Trn(MatB)×MatC+Identity(2)+MatAns")]
    [InlineData(CalculatorApp.Vector, "VctA•VctB+Angle(VctA,VctB)+UnitV(VctC)+VctAns")]
    [InlineData(CalculatorApp.Statistics, "x̄+ȳ+σ²x+σx+s²x+sx+n+Σx+Σx²+Σxy+min(x)+max(x)+Q1+Med+Q3")]
    [InlineData(CalculatorApp.Statistics, "5.5ŷ+3x̂+2x̂₁+1x̂₂+P(1)+Q(1)+R(1)+30▶t")]
    [InlineData(CalculatorApp.Spreadsheet, "Sum($A$1:B2)+Min(A1:A5)+Max(B1:B2)+Mean(C1:C3)+A$1+$B2")]
    [InlineData(CalculatorApp.Calculate, "1≤1<1+1")]
    [InlineData(CalculatorApp.Calculate, "5>4≥4")]
    [InlineData(CalculatorApp.Calculate, "1+1=2")]
    [InlineData(CalculatorApp.Calculate, "3≠2")]
    public void EveryPieceOfTheSyntaxIsDrawn(CalculatorApp app, string input)
    {
        ParseResult result = ExpressionParser.Parse(input, new SyntaxContext(app, AllowRelations: true));

        result.Root.Should().NotBeNull("'{0}' should parse", input);
        string latex = LatexPrinter.Print(result.Root!);
        Formula.Fault(latex).Should().BeNull("'{0}' prints as '{1}'", input, latex);
    }

    [Theory]
    // The commands that are missing, so the day they arrive nobody wonders why the printer avoids them.
    [InlineData(@"\operatorname{GCD}")]
    [InlineData(@"\mathbin{+}")]
    [InlineData(@"\quad")]
    [InlineData(@"\textcolor{red}{x}")]
    [InlineData(@"\rule{1pt}{1pt}")]
    [InlineData(@"\phantom{x}")]
    [InlineData(@"\#")]
    [InlineData(@"\$")]
    public void WhatWpfMathHasNot(string latex)
    {
        Formula.Fault(latex).Should().NotBeNull("the printer works around '{0}'", latex);
    }

    [Theory]
    // And what it does have, which is what the printer uses instead.
    [InlineData(@"\mathrm{GCD}")]
    [InlineData(@"\text{Ran\#}")]
    [InlineData(@"\text{\$A\$1}")]
    [InlineData(@"1\;\mathrm{and}\;2")]
    [InlineData(@"\square")]
    [InlineData(@"\color{red}{|}")]
    public void WhatWpfMathHas(string latex)
    {
        Formula.Fault(latex).Should().BeNull();
    }

    private static IEnumerable<(string Input, CalculatorApp App)> ConformanceInputs()
    {
        foreach (string file in Directory.GetFiles(Path.Combine(RepositoryRoot(), "tests", "Barbatos.Pallas.Conformance.Tests", "Data", "calculator"), "*.json"))
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
            if (!document.RootElement.TryGetProperty("cases", out JsonElement cases))
            {
                continue;
            }

            foreach (JsonElement one in cases.EnumerateArray())
            {
                if (!one.TryGetProperty("input", out JsonElement input) || input.GetString() is not { Length: > 0 } text)
                {
                    continue;
                }

                CalculatorApp app = one.TryGetProperty("app", out JsonElement name) && Enum.TryParse(name.GetString(), out CalculatorApp parsed)
                    ? parsed
                    : CalculatorApp.Calculate;
                yield return (text, app);
            }
        }
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Barbatos.Pallas.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Could not find Barbatos.Pallas.slnx above " + AppContext.BaseDirectory);
    }
}
