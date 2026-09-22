// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A calculator in use: the application, settings, memory (Ans, PreAns, A-F, x, y, z, MatA-MatD, VctA-VctD), defined
/// functions, statistics data and history.
/// </summary>
/// <remarks>
/// A session is not thread-safe; the engine it comes from is. Memory and defined functions persist across applications,
/// as on the reference calculator (manual pp. 36-40, 70-72, 135); PreAns exists only in Calculate, MatAns only in Matrix
/// and VctAns only in Vector (pp. 137, 144). The statistics data stay until they are replaced.
/// </remarks>
public sealed class CalculatorSession
{
    private readonly PallasEngine _engine;
    private readonly Random _random;
    private readonly Value[] _variables = [.. Enumerable.Repeat(Value.Zero, 9)];
    private readonly MatrixValue?[] _matrices = new MatrixValue?[4];
    private readonly VectorValue?[] _vectors = new VectorValue?[4];
    private readonly Dictionary<DefinedFunction, SyntaxNode> _definitions = [];
    private readonly List<Calculation> _history = [];
    private CalculatorSettings _settings = CalculatorSettings.Initial;
    private RegressionModel _regression;
    private StatisticsCalculator? _statistics;
    private bool _storing = true;

    // The distribution types by the names of the menu of p. 96.
    private static readonly string[] DistributionNames = ["Binomial PD", "Binomial CD", "Normal PD", "Normal CD", "Inverse Normal", "Poisson PD", "Poisson CD"];

    // The Equation application solves 2 to 4 unknowns and polynomials of degree 2 to 4 (p. 114), in both profiles: the
    // calculator names the unknowns x, y, z and t, and the exact factoring of a polynomial is designed for that degree.
    private const int MaxUnknowns = 4;

    // Newton settles a simple root in a few dozen steps; beyond this the calculator offers to continue (p. 121).
    private const int NewtonSteps = 200;

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

    /// <summary>
    /// Gets or sets where a cell reference in a Spreadsheet formula reads its value (manual pp. 102, 105).
    /// </summary>
    /// <remarks>
    /// The grid, its dependencies and its recalculation belong to the application (Barbatos.Pallas.Spreadsheet), which
    /// sets this before it evaluates a cell. While it is <see langword="null"/> every cell reads as 0, which is what an
    /// empty cell is (assumption U26).
    /// </remarks>
    public Func<CellAddress, EvalResult>? CellValues { get; set; }

    /// <summary>Gets the last result (manual p. 36).</summary>
    public Value Ans { get; private set; } = Value.Zero;

    /// <summary>Gets the result before the last one; Calculate only (p. 37).</summary>
    public Value PreAns { get; private set; } = Value.Zero;

    /// <summary>Gets the last matrix result of the Matrix application, or <see langword="null"/> (p. 136).</summary>
    public MatrixValue? MatAns { get; private set; }

    /// <summary>Gets the last vector result of the Vector application, or <see langword="null"/> (p. 144).</summary>
    public VectorValue? VctAns { get; private set; }

    /// <summary>Gets the data of the Statistics application; initially <see cref="StatisticsData.Empty"/>.</summary>
    public StatisticsData StatisticsData { get; private set; } = StatisticsData.Empty;

    /// <summary>Gets or sets the regression type of two-variable statistics (p. 86); initially y = a + bx.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is not a defined <see cref="RegressionModel"/>.</exception>
    public RegressionModel Regression
    {
        get => _regression;
        set
        {
            _regression = Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "Not a defined RegressionModel value.");
            _statistics = null;
        }
    }

    /// <summary>Gets the calculations since the history was last cleared, oldest first.</summary>
    public IReadOnlyList<Calculation> History => _history;

    /// <summary>Returns the value of a variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The value; initially 0.</returns>
    public Value GetVariable(MemoryVariable variable) => _variables[(int)Checked(variable)];

    /// <summary>Stores a value in a variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="value">The value: a number, not a matrix or a vector.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is a matrix or a vector.</exception>
    public void SetVariable(MemoryVariable variable, Value value)
    {
        if (value.IsComposite)
        {
            throw new ArgumentException("A variable holds a number; matrices and vectors have their own variables.", nameof(value));
        }

        _variables[(int)Checked(variable)] = value;
    }

    /// <summary>Returns a matrix variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The matrix, or <see langword="null"/> when none is stored ("None", p. 134).</returns>
    public MatrixValue? GetMatrix(MatrixVariable variable) => _matrices[(int)Checked(variable)];

    /// <summary>Stores a matrix in a matrix variable, or clears it.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="matrix">The matrix, or <see langword="null"/> to clear the variable.</param>
    /// <exception cref="ArgumentOutOfRangeException">The matrix is larger than the profile allows: 4×4 in <see cref="CalculatorProfile.Standard"/> (p. 132).</exception>
    public void SetMatrix(MatrixVariable variable, MatrixValue? matrix)
    {
        int index = (int)Checked(variable);
        int limit = CompositeMath.Limit(Profile, vector: false);
        if (matrix is not null && (matrix.Rows > limit || matrix.Columns > limit))
        {
            throw new ArgumentOutOfRangeException(nameof(matrix), $"{matrix.Rows}×{matrix.Columns}", $"The {Profile} profile holds matrices up to {limit}×{limit}.");
        }

        _matrices[index] = matrix;
    }

    /// <summary>Returns a vector variable.</summary>
    /// <param name="variable">The variable.</param>
    /// <returns>The vector, or <see langword="null"/> when none is stored ("None", p. 141).</returns>
    public VectorValue? GetVector(VectorVariable variable) => _vectors[(int)Checked(variable)];

    /// <summary>Stores a vector in a vector variable, or clears it.</summary>
    /// <param name="variable">The variable.</param>
    /// <param name="vector">The vector, or <see langword="null"/> to clear the variable.</param>
    /// <exception cref="ArgumentOutOfRangeException">The dimension is outside the profile's: 2 or 3 in <see cref="CalculatorProfile.Standard"/> (p. 139).</exception>
    public void SetVector(VectorVariable variable, VectorValue? vector)
    {
        int index = (int)Checked(variable);
        int limit = CompositeMath.Limit(Profile, vector: true);
        if (vector is not null && (vector.Dimension < 2 || vector.Dimension > limit))
        {
            throw new ArgumentOutOfRangeException(nameof(vector), vector.Dimension, $"The {Profile} profile holds vectors of 2 to {limit} dimensions.");
        }

        _vectors[index] = vector;
    }

    /// <summary>Replaces the data of the Statistics application (pp. 80-83).</summary>
    /// <param name="data">The data.</param>
    /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The data have more rows than the profile allows: 160, 80 or 53 for one, two or three columns in
    /// <see cref="CalculatorProfile.Standard"/> (p. 80).
    /// </exception>
    public void SetStatisticsData(StatisticsData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        int limit = StatisticsCalculator.RowLimit(Profile, data.Columns);
        if (data.Rows > limit)
        {
            throw new ArgumentOutOfRangeException(nameof(data), data.Rows, $"The {Profile} profile holds {limit} rows of {data.Columns} columns.");
        }

        StatisticsData = data;
        _statistics = null;
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

    /// <summary>
    /// Changes the application. Leaving Calculate clears PreAns, leaving Matrix clears MatAns and leaving Vector clears
    /// VctAns; the history is cleared (pp. 35, 37, 137, 144).
    /// </summary>
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
            // Only Matrix makes matrices and only Vector makes vectors, so leaving either clears its answer (pp. 135, 144).
            _history.Clear();
            MatAns = null;
            VctAns = null;
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

    /// <summary>Calculates an input without storing anything: neither in Ans nor in the history.</summary>
    /// <param name="input">The input, in Canonical Linear Syntax.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The calculation, with its result or its error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    /// <remarks>
    /// How an application calculates on the user's behalf rather than at their command: a cell of the Spreadsheet
    /// application, a row of a number table. The calculator leaves Ans alone there (pp. 102, 109).
    /// </remarks>
    public Calculation Evaluate(string input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        _storing = false;
        try
        {
            return Run(input, cancellationToken);
        }
        finally
        {
            _storing = true;
        }
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

    /// <summary>Calculates a distribution with one x, the Variable input method (manual pp. 97, 100); the result goes to Ans.</summary>
    /// <param name="kind">The calculation type.</param>
    /// <param name="parameters">The parameters; the type reads the ones it needs (p. 98), x from <see cref="DistributionParameters.X"/>.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The calculation, with its probability, density or x, or its error.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Distribution application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public Calculation CalculateDistribution(DistributionKind kind, DistributionParameters parameters, CancellationToken cancellationToken = default)
    {
        RequireDistribution(kind, parameters);
        Calculation calculation = Distribution(kind, parameters, parameters.X, cancellationToken);
        if (calculation.Succeeded)
        {
            UpdateAnswers(calculation.Result);
        }

        return calculation;
    }

    /// <summary>
    /// Calculates a binomial or Poisson distribution for each x of a list, the List input method (pp. 96-99). A value
    /// outside the domain is an error of its own row; Ans is unchanged.
    /// </summary>
    /// <param name="kind">A binomial or Poisson calculation type.</param>
    /// <param name="parameters">The parameters; <see cref="DistributionParameters.X"/> is not read.</param>
    /// <param name="list">The x values: at most 45 in <see cref="CalculatorProfile.Standard"/> (p. 98).</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>One calculation per x, in order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="parameters"/> or <paramref name="list"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="kind"/> is a normal type, which takes one x only (p. 97).</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not defined, or the list is longer than the profile allows.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Distribution application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public IReadOnlyList<Calculation> CalculateDistribution(DistributionKind kind, DistributionParameters parameters, IReadOnlyList<Value> list, CancellationToken cancellationToken = default)
    {
        RequireDistribution(kind, parameters);
        ArgumentNullException.ThrowIfNull(list);
        if (kind is DistributionKind.NormalPD or DistributionKind.NormalCD or DistributionKind.InverseNormal)
        {
            throw new ArgumentException("Normal PD, Normal CD and Inverse Normal take one x (the Variable input method).", nameof(kind));
        }

        int limit = Profile == CalculatorProfile.Standard ? 45 : StatisticsCalculator.ExtendedRowLimit;
        if (list.Count > limit)
        {
            throw new ArgumentOutOfRangeException(nameof(list), list.Count, $"The {Profile} profile takes {limit} values of x.");
        }

        return [.. list.Select(x => Distribution(kind, parameters, x, cancellationToken))];
    }

    /// <summary>Solves a system of 2 to 4 linear equations (manual pp. 114-116).</summary>
    /// <param name="augmented">
    /// The rows of the system, each the coefficients of the unknowns and then the constant: n rows of n + 1 values.
    /// </param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The value of each unknown, or that the system has no solution or infinitely many.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="augmented"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="augmented"/> is not 2 to 4 rows of one more value than there are rows.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Equation application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    /// <remarks>The solution stays in the result: the application stores what the user chooses (assumption U25).</remarks>
    public SimultaneousSolution SolveSimultaneous(Value[,] augmented, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(augmented);
        RequireApp(CalculatorApp.Equation);
        int unknowns = augmented.GetLength(0);
        if (unknowns is < 2 or > MaxUnknowns || augmented.GetLength(1) != unknowns + 1)
        {
            throw new ArgumentException($"A system has 2 to {MaxUnknowns} equations, each with one more value than there are unknowns.", nameof(augmented));
        }

        return EquationSolver.Simultaneous(augmented, Context(cancellationToken), Line);
    }

    /// <summary>Solves a polynomial equation of degree 2 to 4 (manual pp. 116-119).</summary>
    /// <param name="coefficients">The coefficients from the highest degree down: a, b, c for a·x² + b·x + c.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The roots and the local extrema, or that there is no real root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are not 3 to 5 coefficients.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Equation application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public PolynomialSolution SolvePolynomial(IReadOnlyList<Value> coefficients, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        RequireApp(CalculatorApp.Equation);
        if (coefficients.Count is < 3 or > MaxUnknowns + 1)
        {
            throw new ArgumentException($"A polynomial of degree 2 to {MaxUnknowns} has 3 to {MaxUnknowns + 1} coefficients.", nameof(coefficients));
        }

        return EquationSolver.Polynomial(coefficients, Context(cancellationToken), Line);
    }

    /// <summary>Solves a polynomial inequality of degree 2 to 4 (manual pp. 124-125).</summary>
    /// <param name="coefficients">The coefficients from the highest degree down.</param>
    /// <param name="relation">How the polynomial compares with 0.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>The intervals that satisfy it, or that none or every real number does.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="coefficients"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">There are not 3 to 5 coefficients.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="relation"/> is not one of the four inequalities.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Inequality application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public InequalitySolution SolveInequality(IReadOnlyList<Value> coefficients, RelationOperator relation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(coefficients);
        RequireApp(CalculatorApp.Inequality);
        if (coefficients.Count is < 3 or > MaxUnknowns + 1)
        {
            throw new ArgumentException($"An inequality of degree 2 to {MaxUnknowns} has 3 to {MaxUnknowns + 1} coefficients.", nameof(coefficients));
        }

        if (relation is not (RelationOperator.Less or RelationOperator.Greater or RelationOperator.LessOrEqual or RelationOperator.GreaterOrEqual))
        {
            throw new ArgumentOutOfRangeException(nameof(relation), relation, "The Inequality application compares with >, <, ≥ or ≤ (p. 124).");
        }

        return EquationSolver.Inequality(coefficients, relation, Context(cancellationToken), Line);
    }

    /// <summary>Solves an equation for one variable, by Newton's method from an initial value (manual pp. 120-121).</summary>
    /// <param name="input">The equation in Canonical Linear Syntax, as <c>left=right</c> or as an expression that stands for <c>expression=0</c>.</param>
    /// <param name="variable">The variable to solve for; its value becomes the solution.</param>
    /// <param name="initialValue">Where the iteration starts.</param>
    /// <param name="cancellationToken">Cancels a long calculation.</param>
    /// <returns>
    /// The solution, with Left − Right in <see cref="Calculation.Second"/>; a Variable ERROR when the equation does not
    /// use the variable, and Cannot Solve when the iteration does not settle (p. 165).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="variable"/> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Equation application.</exception>
    /// <exception cref="OperationCanceledException">The calculation was cancelled.</exception>
    public Calculation SolveEquation(string input, MemoryVariable variable, Value initialValue, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        MemorySlot slot = (MemorySlot)(int)Checked(variable);
        RequireApp(CalculatorApp.Equation);
        return Newton(input, slot, initialValue, cancellationToken);
    }

    /// <summary>Solves a ratio for X (manual pp. 145-146); the result goes to Ans.</summary>
    /// <param name="form">Which of the two ratios has X last.</param>
    /// <param name="a">A, the first value.</param>
    /// <param name="b">B, the second value.</param>
    /// <param name="other">D of A:B = X:D, or C of A:B = C:X.</param>
    /// <returns>The value of X, or a Math ERROR when a value is 0 (p. 146).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not a defined value.</exception>
    /// <exception cref="InvalidOperationException">The session is not in the Ratio application.</exception>
    public Calculation SolveRatio(RatioForm form, Value a, Value b, Value other)
    {
        if (!Enum.IsDefined(form))
        {
            throw new ArgumentOutOfRangeException(nameof(form), form, "Not a defined RatioForm value.");
        }

        RequireApp(CalculatorApp.Ratio);
        string input = form == RatioForm.XInSecondRatio ? "A:B=X:D" : "A:B=C:X";
        EvaluationContext context = Context(CancellationToken.None);

        // Every value is read, so a 0 anywhere is a Math ERROR, not only a division by 0 (p. 146).
        EvalResult product = a.IsReal && b.IsReal && other.IsReal && !a.IsZero && !b.IsZero && !other.IsZero
            ? ValueMath.Multiply(form == RatioForm.XInSecondRatio ? a : b, other, context)
            : ValueMath.MathError;
        EvalResult x = product.Succeeded ? ValueMath.Divide(product.Value, form == RatioForm.XInSecondRatio ? b : a, context) : product;
        if (!x.Succeeded)
        {
            return Failed(input, new CalcError(x.Error!.Value, default));
        }

        UpdateAnswers(x.Value);
        return new Calculation(input, App, _settings, Profile, CalculationKind.Value, x.Value, null, null, null, []);
    }

    /// <summary>Takes everything the session holds, as text an application can store (manual pp. 36-40).</summary>
    /// <returns>The snapshot: the application, the settings, the memory, the defined functions and the statistics data.</returns>
    /// <remarks>The history is not part of it; an application that keeps a history across runs keeps it itself.</remarks>
    public SessionSnapshot Capture()
    {
        return new SessionSnapshot
        {
            App = App,
            Settings = _settings,
            Regression = _regression,
            Variables = [.. _variables.Select(value => ValueText.Write(value, Profile))],
            Ans = ValueText.Write(Ans, Profile),
            PreAns = ValueText.Write(PreAns, Profile),
            Matrices = [.. _matrices.Select(Captured)],
            Vectors = [.. _vectors.Select(Captured)],
            FunctionF = _definitions.TryGetValue(DefinedFunction.F, out SyntaxNode? f) ? f.ToString() : null,
            FunctionG = _definitions.TryGetValue(DefinedFunction.G, out SyntaxNode? g) ? g.ToString() : null,
            StatisticsX = [.. StatisticsData.X.Select(value => ValueText.Write(value, Profile))],
            StatisticsY = StatisticsData.IsTwoVariable ? [.. StatisticsData.Y.Select(value => ValueText.Write(value, Profile))] : [],
            StatisticsFrequencies = StatisticsData.HasFrequencies
                ? [.. StatisticsData.Frequencies.Select(value => ValueText.Write(value, Profile))]
                : [],
        };
    }

    /// <summary>Puts back what <see cref="Capture"/> took, over whatever the session holds now.</summary>
    /// <param name="snapshot">The snapshot.</param>
    /// <exception cref="ArgumentNullException"><paramref name="snapshot"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The snapshot was written by a later version of the format.</exception>
    /// <exception cref="NotSupportedException">The snapshot is of an application this engine does not have.</exception>
    /// <remarks>
    /// Anything the snapshot cannot say - a value whose text is not one, a definition that no longer parses - is left
    /// as the session had it, so a snapshot that has aged badly loses what it cannot carry and nothing more.
    /// </remarks>
    public void Restore(SessionSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Version > SessionSnapshot.CurrentVersion)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.Version, $"This engine reads snapshots up to version {SessionSnapshot.CurrentVersion}.");
        }

        RequireSupported(snapshot.App);

        // The values are read in Calculate, where every spelling of the vocabulary is available, and the application
        // the snapshot names is entered once they are in.
        App = CalculatorApp.Calculate;
        _settings = snapshot.Settings;
        Regression = snapshot.Regression;
        for (int i = 0; i < _variables.Length && i < snapshot.Variables.Length; i++)
        {
            _variables[i] = ValueText.Read(snapshot.Variables[i], this) ?? _variables[i];
        }

        Ans = ValueText.Read(snapshot.Ans, this) ?? Ans;
        PreAns = ValueText.Read(snapshot.PreAns, this) ?? PreAns;
        for (int i = 0; i < _matrices.Length && i < snapshot.Matrices.Length; i++)
        {
            _matrices[i] = Restored(snapshot.Matrices[i]);
        }

        for (int i = 0; i < _vectors.Length && i < snapshot.Vectors.Length; i++)
        {
            _vectors[i] = RestoredVector(snapshot.Vectors[i]);
        }

        RestoreDefinition(DefinedFunction.F, snapshot.FunctionF);
        RestoreDefinition(DefinedFunction.G, snapshot.FunctionG);
        SetStatisticsData(new StatisticsData(
            snapshot.StatisticsX.Select(text => ValueText.Read(text, this) ?? Value.Zero),
            snapshot.StatisticsY.IsEmpty ? null : snapshot.StatisticsY.Select(text => ValueText.Read(text, this) ?? Value.Zero),
            snapshot.StatisticsFrequencies.IsEmpty ? null : snapshot.StatisticsFrequencies.Select(text => ValueText.Read(text, this) ?? Value.Zero)));

        App = snapshot.App;
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

        StatisticsSetup statistics = new(StatisticsData.IsTwoVariable, _regression);
        BoundStatement? statement = Binder.Bind(parsed.Root, _engine.Catalog, app, settings, _definitions, statistics, out int slotCount, out CalcError? bindError);
        if (statement is null)
        {
            return Failed(input, bindError!.Value);
        }

        CompiledProgram program = Compiler.Compile(statement, slotCount);
        EvaluationContext context = new(_engine.Catalog, app, settings, Profile, Budget, _random, cancellationToken, CellValues);
        Evaluator evaluator = new(program, context, LoadMemory, statistic => LoadStatistic(statistic, context));
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

        // The quotient goes to E and Ans, the remainder to F (p. 56), unless nothing of this calculation is stored.
        Store(MemoryVariable.E, division.Quotient);
        Store(MemoryVariable.F, division.Remainder);
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
        Store(MemoryVariable.X, a.Value);
        Store(MemoryVariable.Y, b.Value);
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
        if (slot >= MemorySlot.MatA)
        {
            // p. 163: a matrix or vector used before it is defined is Not Defined.
            object? composite = slot switch
            {
                MemorySlot.MatAns => MatAns,
                MemorySlot.VctAns => VctAns,
                <= MemorySlot.MatD => _matrices[slot - MemorySlot.MatA],
                _ => _vectors[slot - MemorySlot.VctA],
            };

            return composite switch
            {
                MatrixValue matrix => Value.FromMatrix(matrix),
                VectorValue vector => Value.FromVector(vector),
                _ => EvalResult.Failure(CalcErrorKind.NotDefined),
            };
        }

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

    private Calculation Distribution(DistributionKind kind, DistributionParameters parameters, Value x, CancellationToken cancellationToken)
    {
        // The values the type reads must be real: a complex number cannot be used outside Complex (p. 163).
        Value[] inputs = kind switch
        {
            DistributionKind.BinomialPD or DistributionKind.BinomialCD => [x, parameters.Trials, parameters.Probability],
            DistributionKind.NormalPD => [x, parameters.Mean, parameters.StandardDeviation],
            DistributionKind.NormalCD => [parameters.Lower, parameters.Upper, parameters.Mean, parameters.StandardDeviation],
            DistributionKind.InverseNormal => [parameters.Area, parameters.Mean, parameters.StandardDeviation],
            _ => [x, parameters.Lambda],
        };
        EvaluationContext context = new(_engine.Catalog, App, _settings, Profile, Budget, _random, cancellationToken, CellValues);
        EvalResult result = inputs.All(input => input.IsReal) ? DistributionMath.Evaluate(kind, parameters, x, context) : ValueMath.MathError;
        string input = DistributionNames[(int)kind];
        return result.Succeeded
            ? new Calculation(input, App, _settings, Profile, CalculationKind.Value, result.Value, null, null, null, [], DisplayHints.DecimalResult)
            : new Calculation(input, App, _settings, Profile, CalculationKind.Value, default, null, null, new CalcError(result.Error!.Value, default), []);
    }

    /// <summary>Newton's method on Left − Right, with the slope from a central difference, as the calculator solves (p. 121).</summary>
    private Calculation Newton(string input, MemorySlot slot, Value initialValue, CancellationToken cancellationToken)
    {
        ParseResult parsed = ExpressionParser.Parse(input, new SyntaxContext(App, AllowRelations: true), _engine.Vocabulary);
        if (!parsed.Succeeded)
        {
            return Failed(input, ToError(parsed.Diagnostic!.Value));
        }

        StatisticsSetup statistics = new(StatisticsData.IsTwoVariable, _regression);
        BoundStatement? statement = Binder.Bind(parsed.Root, _engine.Catalog, App, _settings, _definitions, statistics, out int slotCount, out CalcError? bindError);
        if (statement is null)
        {
            return Failed(input, bindError!.Value);
        }

        // An equation is "left=right", or an expression, which stands for "expression=0"; a longer chain is not one.
        bool equation = statement.Kind == StatementKind.Verify;
        bool single = equation && statement.Relations.Length == 1 && statement.Relations[0] == RelationOperator.Equal;
        if (statement.Kind != StatementKind.Expression && !single)
        {
            return Failed(input, new CalcError(CalcErrorKind.SyntaxError, statement.Span));
        }

        Value current = initialValue;
        bool usesVariable = false;
        EvaluationContext context = Context(cancellationToken);
        Evaluator evaluator = new(
            Compiler.Compile(statement, slotCount),
            context,
            memory =>
            {
                usesVariable |= memory == slot;
                return memory == slot ? current : LoadMemory(memory);
            },
            statistic => LoadStatistic(statistic, context));

        EvalResult Difference(Value at)
        {
            current = at;
            EvalResult left = evaluator.Run(0);
            EvalResult right = equation ? (left.Succeeded ? evaluator.Run(1) : left) : EvalResult.Success(Value.Zero);
            return left.Succeeded && right.Succeeded ? ValueMath.Subtract(left.Value, right.Value, context) : left.Succeeded ? right : left;
        }

        Value solution = initialValue;
        EvalResult value = Difference(solution);
        if (!usesVariable)
        {
            // p. 165: an expression without the variable to solve for is a Variable ERROR.
            return Failed(input, new CalcError(CalcErrorKind.VariableError, SpanOfInput(input)));
        }

        bool settled = value.Succeeded && value.Value.IsZero;
        for (int iteration = 0; !settled; iteration++)
        {
            if (!value.Succeeded)
            {
                return Failed(input, new CalcError(value.Error!.Value, evaluator.ErrorSpan));
            }

            if (iteration >= NewtonSteps || !context.TryIterate())
            {
                return Failed(input, new CalcError(iteration >= NewtonSteps ? CalcErrorKind.CannotSolve : CalcErrorKind.TimeOut, SpanOfInput(input)));
            }

            ValueChain chain = new();
            Value width = chain.Step(ValueMath.Real(Math.Max(Math.Abs(solution.ToDouble()) * 1e-10d, 1e-95d), null, context));
            Value ahead = chain.Step(Difference(chain.Step(ValueMath.Add(solution, width, context))));
            Value behind = chain.Step(Difference(chain.Step(ValueMath.Subtract(solution, width, context))));
            Value rise = chain.Step(ValueMath.Subtract(ahead, behind, context));
            Value slope = chain.Step(ValueMath.Divide(rise, chain.Step(ValueMath.Add(width, width, context)), context));
            Value step = chain.Step(slope.IsZero ? EvalResult.Failure(CalcErrorKind.CannotSolve) : ValueMath.Divide(value.Value, slope, context));
            Value next = chain.Step(ValueMath.Subtract(solution, step, context));
            if (!chain.Succeeded)
            {
                return Failed(input, new CalcError(chain.Error!.Value.Kind, SpanOfInput(input)));
            }

            // Settled once the step no longer reaches the digits the solution is held to (assumption U25).
            settled = step.IsZero || Math.Abs(step.ToDouble()) <= 1e-20d * Math.Abs(next.ToDouble());
            solution = next;
            value = Difference(solution);
            settled |= value.Succeeded && value.Value.IsZero;
        }

        if (!value.Succeeded)
        {
            return Failed(input, new CalcError(value.Error!.Value, evaluator.ErrorSpan));
        }

        // The solution becomes the variable's value, as it does on the calculator's Solver screen (p. 121).
        SetVariable((MemoryVariable)(int)slot, solution);
        return new Calculation(input, App, _settings, Profile, CalculationKind.Solution, solution, value.Value, null, null, [.. context.Integrals]);
    }

    /// <summary>A matrix or a vector as a snapshot of its size and its entries.</summary>
    private MatrixSnapshot? Captured(MatrixValue? matrix)
    {
        if (matrix is null)
        {
            return null;
        }

        List<string> entries = [];
        for (int row = 0; row < matrix.Rows; row++)
        {
            for (int column = 0; column < matrix.Columns; column++)
            {
                entries.Add(ValueText.Write(matrix[row, column], Profile));
            }
        }

        return new MatrixSnapshot(matrix.Rows, matrix.Columns, [.. entries]);
    }

    /// <summary>A vector as a snapshot of one row.</summary>
    private MatrixSnapshot? Captured(VectorValue? vector)
    {
        return vector is null
            ? null
            : new MatrixSnapshot(1, vector.Dimension, [.. vector.Elements.Select(element => ValueText.Write(element, Profile))]);
    }

    /// <summary>The vector a snapshot of one row holds, or <see langword="null"/>.</summary>
    private VectorValue? RestoredVector(MatrixSnapshot? snapshot)
    {
        return snapshot is null || snapshot.Rows != 1 || snapshot.Columns != snapshot.Entries.Length
            ? null
            : new VectorValue([.. snapshot.Entries.Select(entry => ValueText.Read(entry, this) ?? Value.Zero)]);
    }

    /// <summary>The matrix a snapshot holds, or <see langword="null"/> when it holds none or cannot be read.</summary>
    private MatrixValue? Restored(MatrixSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.Rows < 1 || snapshot.Columns < 1 || snapshot.Entries.Length != snapshot.Rows * snapshot.Columns)
        {
            return null;
        }

        Value[,] entries = new Value[snapshot.Rows, snapshot.Columns];
        for (int row = 0; row < snapshot.Rows; row++)
        {
            for (int column = 0; column < snapshot.Columns; column++)
            {
                entries[row, column] = ValueText.Read(snapshot.Entries[(row * snapshot.Columns) + column], this) ?? Value.Zero;
            }
        }

        return new MatrixValue(entries);
    }

    /// <summary>Defines f(x) or g(x) again, or leaves it undefined where the snapshot has none or one that no longer parses.</summary>
    private void RestoreDefinition(DefinedFunction function, string? body)
    {
        _definitions.Remove(function);
        if (body is not null)
        {
            _ = Define(function, body);
        }
    }

    private Calculation Line(string name, Value value)
    {
        return new Calculation(name, App, _settings, Profile, CalculationKind.Value, value, null, null, null, []);
    }

    private EvaluationContext Context(CancellationToken cancellationToken)
    {
        return new EvaluationContext(_engine.Catalog, App, _settings, Profile, Budget, _random, cancellationToken, CellValues);
    }

    private void RequireApp(CalculatorApp app)
    {
        if (App != app)
        {
            throw new InvalidOperationException($"That calculation belongs to the {app} application; the session is in {App}.");
        }
    }

    private void RequireDistribution(DistributionKind kind, DistributionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not a defined DistributionKind value.");
        }

        if (App != CalculatorApp.Distribution)
        {
            throw new InvalidOperationException($"Distribution calculations belong to the Distribution application; the session is in {App}.");
        }
    }

    private EvalResult LoadStatistic(Statistic statistic, EvaluationContext context)
    {
        return (_statistics ??= new StatisticsCalculator(StatisticsData, _regression)).Evaluate(statistic, context);
    }

    /// <summary>Stores a value a calculation produced by itself, which <see cref="Evaluate"/> does not.</summary>
    private void Store(MemoryVariable variable, Value value)
    {
        if (_storing)
        {
            _variables[(int)variable] = value;
        }
    }

    private void UpdateAnswers(Value answer)
    {
        if (!_storing)
        {
            return;
        }

        // A matrix or vector result goes to MatAns or VctAns, and Ans keeps the last number (pp. 136, 144).
        if (answer.Kind == ValueKind.Matrix)
        {
            MatAns = answer.ToMatrix();
            return;
        }

        if (answer.Kind == ValueKind.Vector)
        {
            VctAns = answer.ToVector();
            return;
        }

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

    private static MatrixVariable Checked(MatrixVariable variable)
    {
        return Enum.IsDefined(variable) ? variable : throw new ArgumentOutOfRangeException(nameof(variable), variable, "Not a defined MatrixVariable value.");
    }

    private static VectorVariable Checked(VectorVariable variable)
    {
        return Enum.IsDefined(variable) ? variable : throw new ArgumentOutOfRangeException(nameof(variable), variable, "Not a defined VectorVariable value.");
    }

    private static void RequireSupported(CalculatorApp app)
    {
        if (app == CalculatorApp.MathBox)
        {
            throw new NotSupportedException($"The {app} application is implemented in Phase 6; every other application is supported.");
        }
    }
}
