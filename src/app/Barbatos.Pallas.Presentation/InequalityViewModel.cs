// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Inequality screen (manual pp. 121-126): a polynomial, a relation, and the x it holds for.
/// </summary>
/// <remarks>
/// The answer is intervals rather than values, and the engine writes them as text as well - the manual's own
/// <c>x &lt; -1, 3 &lt; x</c> - so the screen shows what the calculator would show and lists the intervals besides.
/// </remarks>
public sealed partial class InequalityViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that solves.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public InequalityViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Grid = new ValueGridViewModel(session, 1, 3);
    }

    /// <summary>Gets the relations to choose from (p. 121).</summary>
    public ImmutableArray<RelationOperator> Relations { get; } =
    [
        RelationOperator.Greater,
        RelationOperator.Less,
        RelationOperator.GreaterOrEqual,
        RelationOperator.LessOrEqual,
    ];

    /// <summary>Gets the coefficients, highest power first.</summary>
    public ValueGridViewModel Grid { get; }

    /// <summary>Gets or sets the degree of the polynomial; two to four.</summary>
    [ObservableProperty]
    private int _degree = 2;

    /// <summary>Gets or sets which way the inequality runs.</summary>
    [ObservableProperty]
    private RelationOperator _relation = RelationOperator.Greater;

    /// <summary>Gets what the last solving came to, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSolution))]
    private SolutionOutcome? _outcome;

    /// <summary>Gets the answer as the calculator writes it.</summary>
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>Gets the intervals of the answer.</summary>
    [ObservableProperty]
    private ImmutableArray<SolutionLine> _intervals = [];

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets whether there is something to show.</summary>
    public bool HasSolution => Outcome is not null;

    /// <summary>Solves the inequality the grid and the relation stand for.</summary>
    [RelayCommand]
    public void Solve()
    {
        InequalitySolution solution = _session.SolveInequality([.. Enumerable.Range(0, Grid.Columns).Select(column => Grid[0, column].Value)], Relation);

        Outcome = solution.Outcome;
        ErrorKey = solution.Error is { } error ? "error." + error.Kind : null;
        Text = solution.Succeeded ? solution.Text : string.Empty;
        Intervals = solution.Succeeded
            ? [.. solution.Intervals.Select((interval, index) => new SolutionLine((index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), interval.Text))]
            : [];
    }

    /// <summary>Empties the coefficients and what was solved.</summary>
    [RelayCommand]
    public void Clear()
    {
        Grid.Clear();
        Outcome = null;
        Text = string.Empty;
        Intervals = [];
        ErrorKey = null;
    }

    partial void OnDegreeChanged(int value)
    {
        int degree = Math.Clamp(value, 2, 4);
        if (degree != value)
        {
            Degree = degree;
            return;
        }

        Grid.Resize(1, degree + 1);
        Clear();
    }
}
