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
/// The Distribution screen (manual pp. 96-101): which distribution, its parameters, and one or many values of x.
/// </summary>
/// <remarks>
/// Which parameters a distribution takes is the distribution's own business, so the screen asks the engine what to
/// show (<see cref="Parameters"/>) rather than holding a table of its own. A list of x values is calculated in one
/// go, as the calculator's own list mode does.
/// </remarks>
public sealed partial class DistributionViewModel : ObservableObject
{
    private readonly CalculatorSession _session;
    private readonly SessionWork _work;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that calculates.</param>
    /// <param name="work">The session's work, shared by its screens; <see langword="null"/> to calculate where asked.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public DistributionViewModel(CalculatorSession session, SessionWork? work = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _work = work ?? SessionWork.Immediate;
        Values = new ValueGridViewModel(session, 1, 1);
        Arguments = new ValueGridViewModel(session, 1, 2);
    }

    /// <summary>Gets the distributions there are.</summary>
    public ImmutableArray<DistributionKind> Kinds { get; } = [.. Enum.GetValues<DistributionKind>()];

    /// <summary>Gets the x values the screen calculates for, one per row.</summary>
    public ValueGridViewModel Values { get; }

    /// <summary>Gets the parameters of the distribution, in the order <see cref="Parameters"/> names them.</summary>
    public ValueGridViewModel Arguments { get; }

    /// <summary>Gets or sets which distribution is calculated.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Parameters))]
    private DistributionKind _kind = DistributionKind.BinomialPD;

    /// <summary>Gets what the last calculation came to, one result per x.</summary>
    [ObservableProperty]
    private ImmutableArray<SolutionLine> _results = [];

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private string? _errorKey;

    /// <summary>Gets the parameters the chosen distribution takes, as the labels the screen shows.</summary>
    public ImmutableArray<string> Parameters => Kind switch
    {
        DistributionKind.BinomialPD or DistributionKind.BinomialCD => ["distribution.trials", "distribution.probability"],
        DistributionKind.NormalPD => ["distribution.mean", "distribution.deviation"],
        DistributionKind.NormalCD => ["distribution.lower", "distribution.upper", "distribution.mean", "distribution.deviation"],
        DistributionKind.InverseNormal => ["distribution.area", "distribution.mean", "distribution.deviation"],
        _ => ["distribution.lambda"],
    };

    /// <summary>Calculates the distribution for every x on the screen.</summary>
    /// <remarks>
    /// The normal distributions and the inverse normal take one x - the calculator calls it the Variable input
    /// method (p. 96) - and the engine refuses a list for them; the others are calculated for every x at once. A work
    /// of the session: a binomial is exact on BigInteger, and one of many trials runs until the budget says Time Out.
    /// </remarks>
    [RelayCommand]
    public void Execute()
    {
        ErrorKey = null;
        DistributionKind kind = Kind;
        bool single = TakesOneValue;
        DistributionParameters parameters = Read();
        // A distribution that takes one x has one row of them (SetValueCount), so the column is that x.
        ImmutableArray<Value> values = Values.Column(0);
        ImmutableArray<string> shown = [.. Enumerable.Range(0, values.Length).Select(row => Values[row, 0].Display)];
        _work.Start(token => Calculated(kind, single, parameters, values, token), calculations => Show(calculations, shown));
    }

    /// <summary>Gets whether this distribution is calculated for one x rather than for a list of them.</summary>
    public bool TakesOneValue => Kind is DistributionKind.NormalPD or DistributionKind.NormalCD or DistributionKind.InverseNormal;

    /// <summary>Empties the parameters, the values and the results.</summary>
    [RelayCommand]
    public void Clear()
    {
        Values.Clear();
        Arguments.Clear();
        Results = [];
        ErrorKey = null;
    }

    /// <summary>Changes how many x values the screen calculates for.</summary>
    /// <param name="count">How many; at least one, and one exactly where the distribution takes one.</param>
    public void SetValueCount(int count)
    {
        Values.Resize(TakesOneValue ? 1 : Math.Max(1, count), 1);
        Results = [];
    }

    partial void OnKindChanged(DistributionKind value)
    {
        Arguments.Resize(1, Parameters.Length);
        SetValueCount(Values.Rows);
        Results = [];
        ErrorKey = null;
        OnPropertyChanged(nameof(TakesOneValue));
    }

    private IReadOnlyList<Calculation> Calculated(DistributionKind kind, bool single, DistributionParameters parameters, ImmutableArray<Value> values, CancellationToken token) =>
        single
            ? [_session.CalculateDistribution(kind, parameters with { X = values[0] }, token)]
            : _session.CalculateDistribution(kind, parameters, values, token);

    private void Show(IReadOnlyList<Calculation> calculations, ImmutableArray<string> shown)
    {
        // A calculation in error displays no text, which is what its line shows.
        Results = [.. calculations.Select((calculation, index) => new SolutionLine(shown[index], calculation.Display.Text))];
        ErrorKey = calculations.FirstOrDefault(calculation => !calculation.Succeeded)?.Error is { } error ? "error." + error.Kind : null;
    }

    private DistributionParameters Read()
    {
        // The grid is resized to what the distribution asks for whenever the distribution changes, so each of these
        // is a column that is there.
        Value First() => Arguments[0, 0].Value;
        Value Second() => Arguments[0, 1].Value;
        Value Third() => Arguments[0, 2].Value;
        Value Fourth() => Arguments[0, 3].Value;

        return Kind switch
        {
            DistributionKind.BinomialPD or DistributionKind.BinomialCD => new DistributionParameters { Trials = First(), Probability = Second() },
            DistributionKind.NormalPD => new DistributionParameters { Mean = First(), StandardDeviation = Second() },
            DistributionKind.NormalCD => new DistributionParameters { Lower = First(), Upper = Second(), Mean = Third(), StandardDeviation = Fourth() },
            DistributionKind.InverseNormal => new DistributionParameters { Area = First(), Mean = Second(), StandardDeviation = Third() },
            _ => new DistributionParameters { Lambda = First() },
        };
    }
}
