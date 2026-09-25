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
/// The Statistics screen (manual pp. 74-95): the data, the regression to fit, and the statistics of both.
/// </summary>
/// <remarks>
/// The statistics themselves are names in an ordinary calculation - <c>x̄</c>, <c>Σxy</c>, <c>r</c> - so the screen
/// puts the data into the session and then calculates those names on the line, rather than holding a table of its
/// own. The summary it shows is that same line, asked for one name at a time.
/// </remarks>
public sealed partial class StatisticsViewModel : ObservableObject
{
    private static readonly ImmutableArray<string> OneVariable = ["n", "x̄", "σx", "sx", "Σx", "Σx²", "min(x)", "Q1", "Med", "Q3", "max(x)"];
    private static readonly ImmutableArray<string> TwoVariable = ["n", "x̄", "ȳ", "σx", "σy", "Σxy", "a", "b", "r"];

    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that holds the data and calculates.</param>
    /// <param name="history">The session's history, shared by its screens; <see langword="null"/> for this run's alone.</param>
    /// <param name="work">The session's work, shared by its screens; <see langword="null"/> to calculate where asked.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public StatisticsViewModel(CalculatorSession session, SessionHistory? history = null, SessionWork? work = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Data = new ValueGridViewModel(session, 4, 1);
        Calculate = new CalculateViewModel(session, history, work);
    }

    /// <summary>Gets the regressions there are (p. 84).</summary>
    public ImmutableArray<RegressionModel> Regressions { get; } = [.. Enum.GetValues<RegressionModel>()];

    /// <summary>Gets the data: a column of x, and a column of y and of frequencies where they are shown.</summary>
    public ValueGridViewModel Data { get; }

    /// <summary>Gets the line the statistics are calculated on.</summary>
    public CalculateViewModel Calculate { get; }

    /// <summary>Gets or sets whether the data have a y column (p. 84).</summary>
    [ObservableProperty]
    private bool _isTwoVariable;

    /// <summary>Gets or sets whether the data have a frequency column (p. 76).</summary>
    [ObservableProperty]
    private bool _hasFrequencies;

    /// <summary>Gets or sets which curve is fitted to two-variable data.</summary>
    [ObservableProperty]
    private RegressionModel _regression;

    /// <summary>Gets the statistics of the data as they stand.</summary>
    [ObservableProperty]
    private ImmutableArray<SolutionLine> _summary = [];

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Puts the data of the screen into the session and works out its statistics.</summary>
    [RelayCommand]
    public void Apply()
    {
        ErrorKey = null;
        _session.Regression = Regression;
        // The columns are x, then y where there is one, then the frequencies where there are any.
        int frequencies = IsTwoVariable ? 2 : 1;
        _session.SetStatisticsData(new StatisticsData(
            Data.Column(0),
            IsTwoVariable ? Data.Column(1) : null,
            HasFrequencies ? Data.Column(frequencies) : null));

        List<SolutionLine> summary = [];
        foreach (string name in IsTwoVariable ? TwoVariable : OneVariable)
        {
            // A statistic a set of data has not - a regression of one variable, a quartile of two - is left out
            // rather than shown as an error: the calculator's own menu does not offer it either.
            Calculation calculation = _session.Evaluate(name);
            if (calculation.Succeeded)
            {
                summary.Add(new SolutionLine(name, calculation.Display.Text));
            }
        }

        Summary = [.. summary];
    }

    /// <summary>Adds a row to the data.</summary>
    [RelayCommand]
    public void AddRow() => Data.Resize(Data.Rows + 1, Data.Columns);

    /// <summary>Removes the last row of the data, keeping at least one.</summary>
    [RelayCommand]
    public void RemoveRow()
    {
        if (Data.Rows > 1)
        {
            Data.Resize(Data.Rows - 1, Data.Columns);
        }
    }

    /// <summary>Empties the data and the statistics.</summary>
    [RelayCommand]
    public void Clear()
    {
        Data.Clear();
        _session.SetStatisticsData(StatisticsData.Empty);
        Summary = [];
        ErrorKey = null;
    }

    partial void OnIsTwoVariableChanged(bool value) => Columns(resize: true);

    partial void OnHasFrequenciesChanged(bool value) => Columns(resize: true);

    partial void OnRegressionChanged(RegressionModel value) => _session.Regression = value;

    private int Columns(bool resize = false)
    {
        int columns = 1 + (IsTwoVariable ? 1 : 0) + (HasFrequencies ? 1 : 0);
        if (resize)
        {
            Data.Resize(Data.Rows, columns);
            Summary = [];
        }

        return columns;
    }
}
