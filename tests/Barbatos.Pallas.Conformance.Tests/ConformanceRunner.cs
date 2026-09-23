// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Text.Json;
using Barbatos.Pallas.Data;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using Barbatos.Pallas.Spreadsheet;

namespace Barbatos.Pallas.Conformance.Tests;

/// <summary>
/// Runs one conformance case against a calculator session and checks every expectation it carries.
/// </summary>
/// <remarks>
/// An expectation member the runner does not know fails the case: a case must never pass because part of what it
/// expects was ignored.
/// </remarks>
internal static class ConformanceRunner
{
    // Property cases evaluate a random function this many times.
    private const int PropertySamples = 2000;

    // The regression menu of p. 86, as the cases name its items.
    private static readonly Dictionary<string, RegressionModel> Regressions = new(StringComparer.Ordinal)
    {
        ["y=a+bx"] = RegressionModel.Linear,
        ["y=a+bx+cx²"] = RegressionModel.Quadratic,
        ["y=a+b·ln(x)"] = RegressionModel.Logarithmic,
        ["y=a·e^(bx)"] = RegressionModel.ExponentialE,
        ["y=a·b^x"] = RegressionModel.ExponentialAB,
        ["y=a·x^b"] = RegressionModel.Power,
        ["y=a+b/x"] = RegressionModel.Inverse,
    };

    // The relations of the Inequality menu (p. 124).
    private static readonly Dictionary<string, RelationOperator> Relations = new(StringComparer.Ordinal)
    {
        [">"] = RelationOperator.Greater,
        ["<"] = RelationOperator.Less,
        ["≥"] = RelationOperator.GreaterOrEqual,
        ["≤"] = RelationOperator.LessOrEqual,
    };

    private static readonly Lazy<PallasEngine> Engine = new(() => PallasEngineBuilder.CreateDefault()
        .AddConstantSet(ConstantSets.Codata2022)
        .AddUnitSet(UnitSets.NistSp811)
        .AddAtomicWeights(AtomicWeightTables.Ciaaw)
        .Build());

    public static void Run(ConformanceCase conformanceCase)
    {
        CalculatorSession session = Engine.Value.CreateSession(
            Enum.Parse<CalculatorApp>(conformanceCase.App),
            Enum.Parse<CalculatorProfile>(conformanceCase.Profile),
            randomSeed: 880);
        session.Settings = SettingsOf(conformanceCase.Settings);

        if (conformanceCase.Given is { ValueKind: JsonValueKind.Object } given)
        {
            foreach ((string name, DefinedFunction function) in (ReadOnlySpan<(string, DefinedFunction)>)[("f", DefinedFunction.F), ("g", DefinedFunction.G)])
            {
                if (given.TryGetProperty(name, out JsonElement body))
                {
                    session.Define(function, body.GetString()!).Should().BeNull("{0}: {1}(x) = {2} must define", conformanceCase, name, body.GetString());
                }
            }

            if (given.TryGetProperty("matrices", out JsonElement matrices))
            {
                foreach (JsonProperty matrix in matrices.EnumerateObject())
                {
                    session.SetMatrix(Enum.Parse<MatrixVariable>(matrix.Name), MatrixOf(matrix.Value));
                }
            }

            if (given.TryGetProperty("vectors", out JsonElement vectors))
            {
                foreach (JsonProperty vector in vectors.EnumerateObject())
                {
                    session.SetVector(Enum.Parse<VectorVariable>(vector.Name), new VectorValue([.. vector.Value.EnumerateArray().Select(element => Parse(element.GetString()!))]));
                }
            }
        }

        switch (conformanceCase.Kind)
        {
            case "expression":
                Check(conformanceCase, session, session.Calculate(conformanceCase.Input!), conformanceCase.Expect!.Value);
                break;
            case "calc":
                Dictionary<MemoryVariable, Value> values = [];
                foreach (JsonProperty value in conformanceCase.Given!.Value.GetProperty("calcValues").EnumerateObject())
                {
                    values[Variable(value.Name)] = Parse(value.Value.GetString()!);
                }

                Check(conformanceCase, session, session.Calculate(conformanceCase.Input!, values), conformanceCase.Expect!.Value);
                break;
            case "sequence":
                RunSequence(conformanceCase, session);
                break;
            case "property":
                CheckProperty(conformanceCase, session);
                break;
            case "statistics":
                RunStatistics(conformanceCase, session);
                break;
            case "distribution":
                RunDistribution(conformanceCase, session);
                break;
            case "simultaneous":
                RunSimultaneous(conformanceCase, session);
                break;
            case "polynomial":
                RunPolynomial(conformanceCase, session);
                break;
            case "solver":
                RunSolver(conformanceCase, session);
                break;
            case "inequality":
                RunInequality(conformanceCase, session);
                break;
            case "ratio":
                RunRatio(conformanceCase, session);
                break;
            case "mathbox":
                RunMathBox(conformanceCase, session);
                break;
            case "spreadsheet":
                RunSpreadsheet(conformanceCase, session);
                break;
            case "table":
                RunTable(conformanceCase, session);
                break;
            default:
                Assert.Fail($"{conformanceCase}: the runner does not know the kind '{conformanceCase.Kind}'.");
                break;
        }
    }

    private static void RunSequence(ConformanceCase conformanceCase, CalculatorSession session)
    {
        Calculation? last = null;
        foreach (ConformanceStep step in conformanceCase.Steps!)
        {
            if (step.Input is not null)
            {
                last = session.Calculate(step.Input);
            }

            if (step.Store is not null)
            {
                session.Store(Variable(step.Store));
            }

            if (step.SwitchBaseMode is not null)
            {
                session.Settings = session.Settings with { BaseMode = BaseModeOf(step.SwitchBaseMode) };
            }

            last.Should().NotBeNull("{0}: a step needs a calculation before it", conformanceCase);
            Check(conformanceCase, session, last!, step.Expect!.Value);
        }
    }

    private static void RunStatistics(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        ConformanceCase source = given.TryGetProperty("sameDataAs", out JsonElement same) ? ConformanceCatalog.Find(same.GetString()!) : conformanceCase;
        session.SetStatisticsData(StatisticsDataOf(source));
        if (source.Given!.Value.TryGetProperty("operations", out JsonElement operations))
        {
            foreach (JsonElement operation in operations.EnumerateArray())
            {
                // "Sort x Ascending", "Sort y Descending" (p. 82).
                string[] words = operation.GetString()!.Split(' ');
                StatisticsColumn column = words[1] == "x" ? StatisticsColumn.X : words[1] == "y" ? StatisticsColumn.Y : StatisticsColumn.Frequency;
                session.SetStatisticsData(session.StatisticsData.Sort(column, descending: words[2] == "Descending"));
            }
        }

        if (given.TryGetProperty("regression", out JsonElement regression))
        {
            session.Regression = Regressions[regression.GetString()!];
        }

        List<Calculation> calculations = [];
        if (given.TryGetProperty("inputs", out JsonElement inputs))
        {
            calculations.AddRange(inputs.EnumerateArray().Select(input => session.Calculate(input.GetString()!)));
        }

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "rows":
                    StatisticsData data = session.StatisticsData;
                    string[][] rows = [.. Enumerable.Range(0, data.Rows).Select(row => data.IsTwoVariable ? new[] { data.X[row].ToString(), data.Y[row].ToString() } : [data.X[row].ToString()])];
                    rows.Should().BeEquivalentTo(member.Value.EnumerateArray().Select(row => row.EnumerateArray().Select(value => value.GetString()!).ToArray()), options => options.WithStrictOrdering(), "{0}: the data rows", conformanceCase);
                    break;
                case "values":
                    foreach (JsonProperty value in member.Value.EnumerateObject())
                    {
                        Calculation calculation = calculations.Single(candidate => candidate.Input == value.Name);
                        calculation.Display.Text.Should().Be(value.Value.GetString(), "{0}: '{1}'", conformanceCase, value.Name);
                    }

                    break;
                case "result":
                    calculations[^1].Display.Text.Should().Be(member.Value.GetString(), "{0}: '{1}'", conformanceCase, calculations[^1].Input);
                    break;
                default:
                    // Any other member names a statistic variable, as the Statistics Calc screen shows it (pp. 88-89).
                    session.Calculate(member.Name).Display.Text.Should().Be(member.Value.GetString(), "{0}: {1}", conformanceCase, member.Name);
                    break;
            }
        }
    }

    private static void RunDistribution(ConformanceCase conformanceCase, CalculatorSession session)
    {
        // The menu names of p. 96 and the parameter names of p. 98.
        JsonElement given = conformanceCase.Given!.Value;
        DistributionKind kind = Enum.Parse<DistributionKind>(given.GetProperty("distribution").GetString()!.Replace(" ", string.Empty, StringComparison.Ordinal));
        DistributionParameters parameters = new();
        foreach (JsonProperty parameter in given.EnumerateObject().Where(member => member.Value.ValueKind == JsonValueKind.String))
        {
            Value value = parameter.Name is "distribution" or "inputMethod" ? Value.Zero : Parse(parameter.Value.GetString()!);
            parameters = parameter.Name switch
            {
                "distribution" or "inputMethod" => parameters,
                "x" => parameters with { X = value },
                "N" => parameters with { Trials = value },
                "p" => parameters with { Probability = value },
                "μ" => parameters with { Mean = value },
                "σ" => parameters with { StandardDeviation = value },
                "Lower" => parameters with { Lower = value },
                "Upper" => parameters with { Upper = value },
                "Area" => parameters with { Area = value },
                "λ" => parameters with { Lambda = value },
                _ => throw new InvalidOperationException($"The runner does not know the parameter '{parameter.Name}'."),
            };
        }

        bool list = given.GetProperty("inputMethod").GetString() == "List";
        Calculation[] calculations = list
            ? [.. session.CalculateDistribution(kind, parameters, [.. given.GetProperty("x").EnumerateArray().Select(x => Parse(x.GetString()!))])]
            : [session.CalculateDistribution(kind, parameters)];
        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            string[] expected = member.Value.ValueKind == JsonValueKind.Array ? [.. member.Value.EnumerateArray().Select(value => value.GetString()!)] : [member.Value.GetString()!];
            member.Name.Should().BeOneOf(["P", "p", "xInv"], "{0}: the runner knows P, p and xInv", conformanceCase);
            calculations.Select(calculation => calculation.Display.Text).Should().Equal(expected, "{0}: {1}", conformanceCase, member.Name);
        }
    }

    private static void RunSimultaneous(ConformanceCase conformanceCase, CalculatorSession session)
    {
        // The coefficients of each equation, then its constant, as the calculator's input screen asks for them (p. 114).
        JsonElement given = conformanceCase.Given!.Value;
        JsonElement[] rows = [.. given.GetProperty("coefficients").EnumerateArray()];
        int unknowns = given.GetProperty("unknowns").GetInt32();
        Value[,] augmented = new Value[unknowns, unknowns + 1];
        for (int row = 0; row < unknowns; row++)
        {
            int column = 0;
            foreach (JsonElement entry in rows[row].EnumerateArray())
            {
                augmented[row, column++] = Parse(entry.GetString()!);
            }
        }

        SimultaneousSolution solution = session.SolveSimultaneous(augmented);
        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "solution":
                    foreach (JsonProperty unknown in member.Value.EnumerateObject())
                    {
                        Calculation value = solution.Unknowns.Single(candidate => candidate.Input == unknown.Name);
                        value.Display.Text.Should().Be(unknown.Value.GetString(), "{0}: {1}", conformanceCase, unknown.Name);
                    }

                    break;
                case "message":
                    solution.Outcome.ToString().Should().Be(member.Value.GetString(), "{0}: the message", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static void RunPolynomial(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        Value[] coefficients = [.. given.GetProperty("coefficients").EnumerateArray().Select(coefficient => Parse(coefficient.GetString()!))];
        (coefficients.Length - 1).Should().Be(given.GetProperty("degree").GetInt32(), "{0}: the degree and the coefficients", conformanceCase);

        PolynomialSolution solution = session.SolvePolynomial(coefficients);
        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "roots":
                    solution.Roots.Select(RootText).Should().Equal(
                        member.Value.EnumerateArray().Select(root => root.GetString()!), "{0}: the roots", conformanceCase);
                    break;
                case "extremum":
                    PolynomialExtremum extremum = solution.Extrema.Single();
                    string kind = extremum.Kind == ExtremumKind.Minimum ? "Min" : "Max";
                    kind.Should().Be(member.Value.GetProperty("type").GetString(), "{0}: the kind of extremum", conformanceCase);
                    extremum.X.Display.Text.Should().Be(member.Value.GetProperty("x").GetString(), "{0}: x of the extremum", conformanceCase);
                    extremum.Y.Display.Text.Should().Be(member.Value.GetProperty("y").GetString(), "{0}: y of the extremum", conformanceCase);
                    break;
                case "message":
                    solution.Outcome.ToString().Should().Be(member.Value.GetString(), "{0}: the message", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static void RunSolver(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        if (given.TryGetProperty("variables", out JsonElement variables))
        {
            foreach (JsonProperty variable in variables.EnumerateObject())
            {
                session.SetVariable(Variable(variable.Name), Parse(variable.Value.GetString()!));
            }
        }

        Calculation calculation = session.SolveEquation(
            given.GetProperty("equation").GetString()!,
            Variable(given.GetProperty("solveFor").GetString()!),
            Parse(given.GetProperty("initialValue").GetString()!));

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "error":
                    calculation.Error!.Value.Kind.ToString().Should().Be(member.Value.GetString(), "{0}: the error", conformanceCase);
                    break;
                case "solution":
                    calculation.Error.Should().BeNull("{0}: '{1}' should solve", conformanceCase, calculation.Input);
                    calculation.Display.Text.Should().Be(member.Value.GetString(), "{0}: the solution", conformanceCase);
                    break;
                case "leftMinusRight":
                    PallasEngine.Format(calculation.Second!.Value, session.Settings, session.Profile)!.Text
                        .Should().Be(member.Value.GetString(), "{0}: Left − Right", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static void RunInequality(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        Value[] coefficients = [.. given.GetProperty("coefficients").EnumerateArray().Select(coefficient => Parse(coefficient.GetString()!))];
        (coefficients.Length - 1).Should().Be(given.GetProperty("degree").GetInt32(), "{0}: the degree and the coefficients", conformanceCase);
        InequalitySolution solution = session.SolveInequality(coefficients, Relations[given.GetProperty("relation").GetString()!]);

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "solution":
                    solution.Text.Should().Be(member.Value.GetString(), "{0}: the intervals", conformanceCase);
                    break;
                case "message":
                    solution.Outcome.ToString().Should().Be(member.Value.GetString(), "{0}: the message", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static void RunRatio(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        bool xInSecond = given.GetProperty("form").GetString() == "A:B=X:D";
        Calculation calculation = session.SolveRatio(
            xInSecond ? RatioForm.XInSecondRatio : RatioForm.XLastInSecondRatio,
            Parse(given.GetProperty("A").GetString()!),
            Parse(given.GetProperty("B").GetString()!),
            Parse(given.GetProperty(xInSecond ? "D" : "C").GetString()!));

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "error":
                    calculation.Error!.Value.Kind.ToString().Should().Be(member.Value.GetString(), "{0}: the error", conformanceCase);
                    break;
                case "X":
                    calculation.Error.Should().BeNull("{0}: the ratio should solve", conformanceCase);
                    calculation.Display.Text.Should().Be(member.Value.GetString(), "{0}: X", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static void RunSpreadsheet(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        SpreadsheetGrid grid = new(session);
        foreach (JsonProperty cell in given.GetProperty("cells").EnumerateObject())
        {
            CellAddress address = Cell(conformanceCase, cell.Name);
            string input = cell.Value.GetString()!;
            _ = input.StartsWith('=') ? grid.SetFormula(address, input[1..]) : grid.SetConstant(address, input);
        }

        if (given.TryGetProperty("operations", out JsonElement operations))
        {
            RunSheetOperations(conformanceCase, grid, operations);
        }

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "values":
                    foreach (JsonProperty value in member.Value.EnumerateObject())
                    {
                        SpreadsheetCell? cell = grid[Cell(conformanceCase, value.Name)];
                        cell.Should().NotBeNull("{0}: {1} should have content", conformanceCase, value.Name);
                        PallasEngine.Format(cell!.Value, session.Settings, session.Profile)!.Text
                            .Should().Be(value.Value.GetString(), "{0}: the value of {1}", conformanceCase, value.Name);
                    }

                    break;
                case "formulas":
                    foreach (JsonProperty formula in member.Value.EnumerateObject())
                    {
                        grid[Cell(conformanceCase, formula.Name)]?.Text
                            .Should().Be(formula.Value.GetString(), "{0}: the formula of {1}", conformanceCase, formula.Name);
                    }

                    break;
                case "constants":
                    foreach (JsonElement constant in member.Value.EnumerateArray())
                    {
                        grid[Cell(conformanceCase, constant.GetString()!)]?.IsFormula
                            .Should().BeFalse("{0}: {1} holds a constant", conformanceCase, constant.GetString());
                    }

                    break;
                case "error":
                    grid.Cells.Should().Contain(cell => cell.Error!.Value.Kind.ToString() == member.Value.GetString(), "{0}: the error", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    /// <summary>The operations of a sheet case: "Copy B1", "Paste C3", "Fill Formula =2A1-3 B1:B3", "Fill Value B1×3 C1:C3".</summary>
    private static void RunSheetOperations(ConformanceCase conformanceCase, SpreadsheetGrid grid, JsonElement operations)
    {
        CellAddress copied = default;
        foreach (JsonElement operation in operations.EnumerateArray())
        {
            string[] words = operation.GetString()!.Split(' ');
            switch (words[0])
            {
                case "Copy":
                    copied = Cell(conformanceCase, words[1]);
                    break;
                case "Paste":
                    grid.CopyPaste(copied, Cell(conformanceCase, words[1]));
                    break;
                case "Cut":
                    copied = Cell(conformanceCase, words[1]);
                    break;
                case "Fill":
                    string[] range = words[^1].Split(':');
                    string input = string.Join(' ', words[2..^1]).TrimStart('=');
                    _ = words[1] == "Formula"
                        ? grid.Fill(input, Cell(conformanceCase, range[0]), Cell(conformanceCase, range[1]))
                        : grid.FillValue(input, Cell(conformanceCase, range[0]), Cell(conformanceCase, range[1]));
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the operation '{operation.GetString()}'.");
                    break;
            }
        }
    }

    private static void RunTable(ConformanceCase conformanceCase, CalculatorSession session)
    {
        // The table type names of the menu of p. 109.
        JsonElement given = conformanceCase.Given!.Value;
        TableType type = given.GetProperty("tableType").GetString() switch
        {
            "f(x)/g(x)" => TableType.FunctionsFAndG,
            "f(x)" => TableType.FunctionF,
            _ => TableType.FunctionG,
        };

        NumberTable table = NumberTable.Generate(
            session,
            type,
            Parse(given.GetProperty("start").GetString()!),
            Parse(given.GetProperty("end").GetString()!),
            Parse(given.GetProperty("step").GetString()!));

        foreach (JsonProperty member in conformanceCase.Expect!.Value.EnumerateObject())
        {
            switch (member.Name)
            {
                case "rows":
                    string[][] rows = [.. table.Rows.Select(row => (string[])[.. new[] { row.X, row.F, row.G }.Where(cell => cell is not null).Select(cell => cell!.Display.Text)])];
                    rows.Should().BeEquivalentTo(
                        member.Value.EnumerateArray().Select(row => row.EnumerateArray().Select(cell => cell.GetString()!).ToArray()),
                        options => options.WithStrictOrdering(),
                        "{0}: the rows of the table", conformanceCase);
                    break;
                case "variables":
                    foreach (JsonProperty variable in member.Value.EnumerateObject())
                    {
                        VariableText(session.GetVariable(Variable(variable.Name)), session)
                            .Should().Be(variable.Value.GetString(), "{0}: variable {1}", conformanceCase, variable.Name);
                    }

                    break;
                case "verify":
                    foreach (JsonProperty answer in given.GetProperty("answers").EnumerateObject())
                    {
                        // "f(x) row 1": the column and the row, counted from 1 as the screen numbers them.
                        string[] words = answer.Name.Split(' ');
                        TableFunction function = words[0] == "f(x)" ? TableFunction.F : TableFunction.G;
                        int row = int.Parse(words[^1], CultureInfo.InvariantCulture) - 1;
                        table.Verify(row, function, answer.Value.GetString()!)
                            .Should().Be(member.Value.GetString() == "True", "{0}: {1}", conformanceCase, answer.Name);
                    }

                    break;
                case "error":
                    table.Error!.Value.Kind.ToString().Should().Be(member.Value.GetString(), "{0}: the error", conformanceCase);
                    break;
                default:
                    Assert.Fail($"{conformanceCase}: the runner does not know the expectation '{member.Name}'.");
                    break;
            }
        }
    }

    private static CellAddress Cell(ConformanceCase conformanceCase, string name)
    {
        CellAddress.TryParse(name, out CellAddress address).Should().BeTrue("{0}: '{1}' is a cell", conformanceCase, name);
        return address;
    }

    /// <summary>A root as the calculator writes it: the real part, then the imaginary part with its sign and i (p. 118).</summary>
    private static string RootText(PolynomialRoot root)
    {
        if (root.IsReal)
        {
            return root.Real.Display.Text;
        }

        string imaginary = root.Imaginary!.Display.Text;
        return root.Real.Display.Text + (imaginary.StartsWith('-') ? string.Empty : "+") + imaginary + "i";
    }

    private static StatisticsData StatisticsDataOf(ConformanceCase source)
    {
        JsonElement given = source.Given!.Value;
        bool twoVariable = given.GetProperty("mode").GetString() == "2-Variable";
        bool frequency = source.Settings.TryGetValue("frequency", out string? setting) && setting == "On";
        Value[][] rows = [.. given.GetProperty("rows").EnumerateArray().Select(row => row.EnumerateArray().Select(value => Parse(value.GetString()!)).ToArray())];
        int frequencyColumn = twoVariable ? 2 : 1;
        rows.Should().OnlyContain(row => row.Length == frequencyColumn + (frequency ? 1 : 0), "{0}: every row has x{1}{2}", source, twoVariable ? ", y" : string.Empty, frequency ? " and Freq" : string.Empty);
        return new StatisticsData(
            rows.Select(row => row[0]),
            twoVariable ? rows.Select(row => row[1]) : null,
            frequency ? rows.Select(row => row[frequencyColumn]) : null);
    }

    private static void Check(ConformanceCase conformanceCase, CalculatorSession session, Calculation calculation, JsonElement expect)
    {
        if (expect.TryGetProperty("error", out JsonElement error))
        {
            calculation.Error.Should().NotBeNull("{0}: '{1}' should fail with {2}", conformanceCase, calculation.Input, error.GetString());
            calculation.Error!.Value.Kind.ToString().Should().Be(error.GetString(), "{0}: '{1}'", conformanceCase, calculation.Input);
        }
        else
        {
            calculation.Error.Should().BeNull("{0}: '{1}' should succeed", conformanceCase, calculation.Input);
        }

        foreach (JsonProperty member in expect.EnumerateObject())
        {
            string expected = member.Value.ValueKind == JsonValueKind.String ? member.Value.GetString()! : member.Value.ToString();
            string because = $"{conformanceCase}: {member.Name} of '{calculation.Input}'";
            switch (member.Name)
            {
                case "error":
                case "equivalentTo":
                    // Checked above, and by ConformanceSyntaxTests.
                    break;
                case "result":
                    // The session's current settings: a Base-N step may have switched the number mode since.
                    session.Format(calculation)!.Text.Should().Be(expected, because);
                    break;
                case "verify":
                    calculation.IsTrue.Should().Be(expected == "True", because);
                    break;
                case "variables":
                    foreach (JsonProperty variable in member.Value.EnumerateObject())
                    {
                        Value value = variable.Name == "Ans" ? session.Ans : session.GetVariable(Variable(variable.Name));
                        VariableText(value, session).Should().Be(variable.Value.GetString(), "{0}: variable {1}", because, variable.Name);
                    }

                    break;
                case "quotient":
                    VariableText(calculation.Result, session).Should().Be(expected, because);
                    break;
                case "remainder":
                    VariableText(calculation.Second!.Value, session).Should().Be(expected, because);
                    break;
                case "value":
                    VariableText(calculation.Result, session).Should().Be(expected, because);
                    break;
                case "decimalValue":
                    calculation.Result.ToInt32().ToString(CultureInfo.InvariantCulture).Should().Be(expected, because);
                    break;
                case "engShiftRight":
                    session.FormatEngineering(calculation, shift: -1).Should().Be(expected, because);
                    break;
                case "matrix":
                    // The display writes a matrix row by row, as [[3, 0], [1, 1]].
                    session.Format(calculation)!.Text.Should().Be("[" + string.Join(", ", member.Value.EnumerateArray().Select(ListText)) + "]", because);
                    break;
                case "vector":
                    session.Format(calculation)!.Text.Should().Be(ListText(member.Value), because);
                    break;
                default:
                    FormatTarget target = TargetOf(member.Name, because);
                    session.Format(calculation, target)?.Text.Should().Be(expected, because);
                    session.Format(calculation, target).Should().NotBeNull(because);
                    break;
            }
        }
    }

    private static void CheckProperty(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement expect = conformanceCase.Expect!.Value;
        decimal low = decimal.Parse(expect.GetProperty("range")[0].GetString()!, CultureInfo.InvariantCulture);
        decimal high = decimal.Parse(expect.GetProperty("range")[1].GetString()!, CultureInfo.InvariantCulture);
        decimal step = decimal.Parse(expect.GetProperty("step").GetString()!, CultureInfo.InvariantCulture);
        HashSet<decimal> seen = [];
        for (int i = 0; i < PropertySamples; i++)
        {
            Calculation calculation = session.Calculate(conformanceCase.Input!);
            calculation.Error.Should().BeNull("{0}", conformanceCase);
            decimal value = calculation.Result.ToDecimal();
            value.Should().BeInRange(low, high, "{0}", conformanceCase);
            (value / step).Should().Be(decimal.Truncate(value / step), "{0}: values are multiples of {1}", conformanceCase, step);
            seen.Add(value);
        }

        // Every value of a small range turns up; a large one is at least well spread.
        long count = (long)((high - low) / step) + 1;
        seen.Count.Should().BeGreaterThanOrEqualTo((int)Math.Min(count, PropertySamples / 4), "{0}: the values must vary", conformanceCase);
    }

    private static string ListText(JsonElement list) => "[" + string.Join(", ", list.EnumerateArray().Select(element => element.GetString())) + "]";

    private static MatrixValue MatrixOf(JsonElement rows)
    {
        JsonElement[] list = [.. rows.EnumerateArray()];
        int columns = list[0].GetArrayLength();
        Value[,] entries = new Value[list.Length, columns];
        for (int row = 0; row < list.Length; row++)
        {
            int column = 0;
            foreach (JsonElement entry in list[row].EnumerateArray())
            {
                entries[row, column++] = Parse(entry.GetString()!);
            }
        }

        return new MatrixValue(entries);
    }

    /// <summary>The variable list displays in Norm 1 (p. 38).</summary>
    private static string VariableText(Value value, CalculatorSession session)
    {
        if (value.Kind == ValueKind.BaseN)
        {
            return value.ToInt32().ToString(CultureInfo.InvariantCulture);
        }

        CalculatorSettings settings = CalculatorSettings.Initial with { InputOutput = InputOutput.MathIDecimalO };
        return PallasEngine.Format(value, settings, session.Profile)!.Text;
    }

    private static FormatTarget TargetOf(string member, string because)
    {
        return member switch
        {
            "standard" => FormatTarget.Standard,
            "decimal" => FormatTarget.DecimalValue,
            "primeFactor" => FormatTarget.PrimeFactor,
            "recurringDecimal" => FormatTarget.RecurringDecimal,
            "rectangular" => FormatTarget.Rectangular,
            "polar" => FormatTarget.Polar,
            "improperFraction" => FormatTarget.ImproperFraction,
            "mixedFraction" => FormatTarget.MixedFraction,
            "eng" => FormatTarget.Engineering,
            "sexagesimal" => FormatTarget.Sexagesimal,
            _ => throw new InvalidOperationException($"{because}: the runner does not know the expectation '{member}'."),
        };
    }

    private static CalculatorSettings SettingsOf(Dictionary<string, string> settings)
    {
        CalculatorSettings result = CalculatorSettings.Initial;
        foreach ((string key, string value) in settings)
        {
            result = key switch
            {
                "inputOutput" => result with { InputOutput = Enum.Parse<InputOutput>(value.Replace("/", string.Empty, StringComparison.Ordinal)) },
                "angleUnit" => result with { AngleUnit = Enum.Parse<AngleUnit>(value) },
                "numberFormat" => result with { NumberFormat = NumberFormatOf(value) },
                "engineerSymbol" => result with { EngineerSymbol = value == "On" },
                "fractionResult" => result with { FractionResult = Enum.Parse<FractionResult>(value) },
                "complexResult" => result with { ComplexResult = value == "r∠θ" ? ComplexResult.Polar : ComplexResult.Rectangular },
                "decimalMark" => result with { DecimalMark = Enum.Parse<DecimalMark>(value) },
                "digitSeparator" => result with { DigitSeparator = value == "On" },
                "baseMode" => result with { BaseMode = BaseModeOf(value) },
                "verify" => result with { Verify = value == "On" },
                "complexRoots" => result with { ComplexRoots = value == "On" },
                // The statistics editor's frequency column, which StatisticsDataOf reads.
                "frequency" => result,
                _ => throw new InvalidOperationException($"The runner does not know the setting '{key}'."),
            };
        }

        return result;
    }

    private static NumberFormat NumberFormatOf(string value)
    {
        int digits = int.Parse(value.AsSpan(value.StartsWith("Norm", StringComparison.Ordinal) ? 4 : 3), CultureInfo.InvariantCulture);
        return value[..3] switch
        {
            "Fix" => NumberFormat.Fix(digits),
            "Sci" => NumberFormat.Sci(digits),
            _ => digits == 1 ? NumberFormat.Norm1 : NumberFormat.Norm2,
        };
    }

    private static NumberBase BaseModeOf(string value)
    {
        return value switch
        {
            "Hexadecimal" => NumberBase.Hex,
            "Binary" => NumberBase.Bin,
            "Octal" => NumberBase.Oct,
            _ => NumberBase.Dec,
        };
    }

    private static MemoryVariable Variable(string name) => Enum.Parse<MemoryVariable>(name, ignoreCase: true);

    private static Value Parse(string text) => Value.FromDecimal(decimal.Parse(text, CultureInfo.InvariantCulture));

    // The nine forms of a Number Line expression, as the calculator's list writes them (p. 153).
    private static readonly Dictionary<string, NumberLineForm> NumberLineForms = new(StringComparer.Ordinal)
    {
        ["x<a"] = NumberLineForm.Less,
        ["x≤a"] = NumberLineForm.LessOrEqual,
        ["x=a"] = NumberLineForm.Equal,
        ["x>a"] = NumberLineForm.Greater,
        ["x≥a"] = NumberLineForm.GreaterOrEqual,
        ["a<x<b"] = NumberLineForm.Between,
        ["a≤x<b"] = NumberLineForm.FromIncluded,
        ["a<x≤b"] = NumberLineForm.ToIncluded,
        ["a≤x≤b"] = NumberLineForm.BetweenIncluded,
    };

    /// <summary>
    /// A form of the Math Box application (pp. 146-161): a simulation, one Number Line expression or several, a
    /// View-Window set by hand, an angle on a circle, or an hour on the clock.
    /// </summary>
    private static void RunMathBox(ConformanceCase conformanceCase, CalculatorSession session)
    {
        JsonElement given = conformanceCase.Given!.Value;
        CalcError? error = null;
        NumberLineView? view = null;
        CircleAngle? angle = null;
        ClockAngles? clock = null;

        if (given.TryGetProperty("simulation", out JsonElement simulation))
        {
            bool dice = simulation.GetString() == "Dice Roll";
            int count = given.GetProperty(dice ? "dice" : "coins").GetInt32();
            error = session.Simulate(dice ? SimulationKind.DiceRoll : SimulationKind.CoinToss, count, Parse(given.GetProperty("attempts").GetString()!)).Error;
        }
        else if (given.TryGetProperty("numberLine", out _))
        {
            error = Axis(given).Error;
        }
        else if (given.TryGetProperty("numberLines", out JsonElement lines))
        {
            NumberLineAxis[] axes = [.. lines.EnumerateArray().Select(Axis)];
            axes.Should().OnlyContain(axis => axis.Succeeded, "{0}: every expression registers", conformanceCase);
            view = NumberLine.Fit(axes);
        }
        else if (given.TryGetProperty("viewWindow", out JsonElement window))
        {
            view = NumberLine.View(Parse(window.GetProperty("center").GetString()!), Parse(window.GetProperty("scale").GetString()!));
            error = view.Error;
        }
        else if (given.TryGetProperty("circle", out JsonElement circle))
        {
            CircleKind kind = circle.GetString() == "Unit Circle" ? CircleKind.UnitCircle : CircleKind.HalfCircle;
            angle = session.CircleAngle(kind, Parse(given.GetProperty("angle").GetString()!));
            error = angle.Error;
        }
        else if (given.TryGetProperty("clock", out JsonElement hour))
        {
            clock = session.Clock(hour.GetInt32());
        }
        else
        {
            Assert.Fail($"{conformanceCase}: the runner does not know this Math Box form.");
        }

        JsonElement expect = conformanceCase.Expect!.Value;
        if (expect.TryGetProperty("error", out JsonElement expected))
        {
            error.Should().NotBeNull("{0} should fail with {1}", conformanceCase, expected.GetString());
            error!.Value.Kind.ToString().Should().Be(expected.GetString(), "{0}", conformanceCase);
        }
        else
        {
            error.Should().BeNull("{0} should succeed", conformanceCase);
        }

        foreach (JsonProperty member in expect.EnumerateObject())
        {
            string because = $"{conformanceCase}: {member.Name}";
            switch (member.Name)
            {
                case "error":
                    break;
                case "view":
                    foreach (JsonProperty part in member.Value.EnumerateObject())
                    {
                        decimal actual = part.Name switch
                        {
                            "scale" => view!.Scale,
                            "center" => view!.Center,
                            "minimum" => view!.Minimum,
                            "maximum" => view!.Maximum,
                            _ => throw new InvalidOperationException($"{because}: the view has no '{part.Name}'."),
                        };
                        actual.Should().Be(decimal.Parse(part.Value.GetString()!, CultureInfo.InvariantCulture), "{0} {1}", because, part.Name);
                    }

                    break;
                case "sin":
                    angle!.Sine!.Display.Text.Should().Be(member.Value.GetString(), because);
                    break;
                case "cos":
                    angle!.Cosine!.Display.Text.Should().Be(member.Value.GetString(), because);
                    break;
                case "tan":
                    angle!.Tangent!.Display.Text.Should().Be(member.Value.GetString(), because);
                    break;
                case "angles":
                    ((string[])[clock!.Smaller.Display.Text, clock.Larger.Display.Text]).Should()
                        .Equal(member.Value.EnumerateArray().Select(text => text.GetString()), because);
                    break;
                default:
                    Assert.Fail($"{because}: the runner does not know this expectation.");
                    break;
            }
        }

        static NumberLineAxis Axis(JsonElement expression)
        {
            NumberLineForm form = NumberLineForms[expression.GetProperty("numberLine").GetString()!];
            Value? b = expression.TryGetProperty("b", out JsonElement upper) ? Parse(upper.GetString()!) : null;
            return NumberLine.Define(form, Parse(expression.GetProperty("a").GetString()!), b);
        }
    }
}
