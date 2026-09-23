// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// Which kind of equation the screen is solving (manual pp. 112-120).
/// </summary>
public enum EquationKind
{
    /// <summary>Simultaneous equations in two to four unknowns.</summary>
    Simultaneous = 0,

    /// <summary>A polynomial of degree two to four.</summary>
    Polynomial = 1,
}

/// <summary>
/// The Equation screen (manual pp. 112-120): a grid of coefficients and what the engine makes of them.
/// </summary>
/// <remarks>
/// The screen is the coefficients and nothing else: which equations they stand for is the kind and the size, and the
/// solving itself belongs to the session, which reports an outcome - solved, no solution, infinitely many, or no
/// real roots - rather than a number of special cases.
/// </remarks>
public sealed partial class EquationViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that solves.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public EquationViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Grid = new ValueGridViewModel(session, 2, 3);
    }

    /// <summary>Gets the kinds of equation the screen can solve.</summary>
    public ImmutableArray<EquationKind> Kinds { get; } = [.. Enum.GetValues<EquationKind>()];

    /// <summary>Gets the coefficients: one row per equation, the constant last; one row for a polynomial.</summary>
    public ValueGridViewModel Grid { get; }

    /// <summary>Gets or sets whether the screen solves simultaneous equations or a polynomial.</summary>
    [ObservableProperty]
    private EquationKind _kind = EquationKind.Simultaneous;

    /// <summary>Gets or sets how many unknowns, or the degree of the polynomial; two to four.</summary>
    [ObservableProperty]
    private int _size = 2;

    /// <summary>Gets what the last solving came to, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSolution))]
    private SolutionOutcome? _outcome;

    /// <summary>Gets the solutions, as the screen lists them.</summary>
    [ObservableProperty]
    private ImmutableArray<SolutionLine> _solutions = [];

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets whether there is something to show.</summary>
    public bool HasSolution => Outcome is not null;

    /// <summary>Solves what the grid holds.</summary>
    [RelayCommand]
    public void Solve()
    {
        Solutions = [];
        ErrorKey = null;
        if (Kind is EquationKind.Simultaneous)
        {
            SolveSimultaneous();
            return;
        }

        SolvePolynomial();
    }

    /// <summary>Empties the coefficients and what was solved.</summary>
    [RelayCommand]
    public void Clear()
    {
        Grid.Clear();
        Solutions = [];
        Outcome = null;
        ErrorKey = null;
    }

    partial void OnKindChanged(EquationKind value) => Resize();

    partial void OnSizeChanged(int value) => Resize();

    private void Resize()
    {
        int size = Math.Clamp(Size, 2, 4);
        if (size != Size)
        {
            Size = size;
            return;
        }

        // Simultaneous equations are a row each with the constant last; a polynomial is one row of coefficients.
        Grid.Resize(Kind is EquationKind.Simultaneous ? size : 1, size + 1);
        Solutions = [];
        Outcome = null;
        ErrorKey = null;
    }

    private void SolveSimultaneous()
    {
        SimultaneousSolution solution = _session.SolveSimultaneous(Grid.ToArray());
        Outcome = solution.Outcome;
        ErrorKey = Key(solution.Error);
        if (!solution.Succeeded || solution.Outcome is not SolutionOutcome.Solved)
        {
            return;
        }

        Solutions = [.. solution.Unknowns.Select((unknown, index) => new SolutionLine(Unknown(index), unknown.Display.Text))];
    }

    private void SolvePolynomial()
    {
        PolynomialSolution solution = _session.SolvePolynomial([.. Enumerable.Range(0, Grid.Columns).Select(column => Grid[0, column].Value)]);
        Outcome = solution.Outcome;
        ErrorKey = Key(solution.Error);
        if (!solution.Succeeded)
        {
            return;
        }

        List<SolutionLine> lines = [];
        for (int index = 0; index < solution.Roots.Length; index++)
        {
            PolynomialRoot root = solution.Roots[index];
            string name = "x" + Subscript(index + 1);
            lines.Add(new SolutionLine(name, root.IsReal ? root.Real.Display.Text : root.Real.Display.Text + (root.Imaginary!.Result.ToDouble() < 0d ? "-" : "+") + root.Imaginary.Display.Text.TrimStart('-') + "i"));
        }

        foreach (PolynomialExtremum extremum in solution.Extrema)
        {
            lines.Add(new SolutionLine(extremum.Kind.ToString(), extremum.X.Display.Text + ", " + extremum.Y.Display.Text));
        }

        Solutions = [.. lines];
    }

    private static string Key(CalcError? error) => error is { } fault ? "error." + fault.Kind : string.Empty;

    private static string Unknown(int index) => index switch
    {
        0 => "x",
        1 => "y",
        2 => "z",
        _ => "t",
    };

    private static string Subscript(int number) => number switch
    {
        1 => "₁",
        2 => "₂",
        3 => "₃",
        _ => "₄",
    };
}

/// <summary>
/// One line of what an application solved: what it is called, and what it came to.
/// </summary>
/// <param name="Name">The name, such as <c>x</c> or <c>x₁</c>.</param>
/// <param name="Text">The value, as the calculator shows it.</param>
public sealed record SolutionLine(string Name, string Text);
