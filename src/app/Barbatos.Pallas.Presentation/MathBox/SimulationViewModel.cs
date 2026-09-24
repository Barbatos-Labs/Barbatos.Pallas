// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using System.Globalization;
using Barbatos.Pallas.Engine;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Dice Roll or Coin Toss screen of Math Box: the parameters, the attempts, and the List and Relative Freq screens
/// of what came up (manual pp. 147-153).
/// </summary>
/// <remarks>
/// The simulation is the engine's (<see cref="CalculatorSession.Simulate"/>); this holds what the screen shows of it.
/// Changing a parameter clears the result, as going back to the parameter screen does on the calculator (p. 149).
/// </remarks>
public sealed partial class SimulationViewModel : ObservableObject
{
    /// <summary>A coin that came up heads, as the calculator draws it (p. 151).</summary>
    public const string Heads = "●";

    /// <summary>A coin that came up tails.</summary>
    public const string Tails = "○";

    private readonly CalculatorSession _session;

    /// <summary>Creates the screen of one simulation over a session.</summary>
    /// <param name="session">The session that simulates, in the Math Box application.</param>
    /// <param name="kind">Dice or coins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public SimulationViewModel(CalculatorSession session, SimulationKind kind)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Kind = kind;
        Attempts = new ValueGridViewModel(session, 1, 1);

        // Five attempts is what the parameter screen offers first (p. 147).
        Attempts[0, 0].Text = "5";
    }

    /// <summary>Gets whether the screen throws dice or coins.</summary>
    public SimulationKind Kind { get; }

    /// <summary>Gets the numbers of dice or coins the screen offers: 1, 2 or 3.</summary>
    public ImmutableArray<int> Counts { get; } = [1, 2, 3];

    /// <summary>Gets the Same Result settings: Off and the three presets.</summary>
    public ImmutableArray<SameResult> SameResults { get; } = [.. Enum.GetValues<SameResult>()];

    /// <summary>Gets the two result screens.</summary>
    public ImmutableArray<SimulationResultView> Views { get; } = [.. Enum.GetValues<SimulationResultView>()];

    /// <summary>Gets the number of attempts, as typed: a whole number from 1 to 250.</summary>
    public ValueGridViewModel Attempts { get; }

    /// <summary>Gets or sets how many dice or coins each attempt throws.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tallies))]
    private int _count = 1;

    /// <summary>Gets or sets the Same Result setting; initially Off.</summary>
    [ObservableProperty]
    private SameResult _sameResult;

    /// <summary>Gets or sets which result screen is shown.</summary>
    [ObservableProperty]
    private SimulationResultView _view;

    /// <summary>Gets or sets what the Relative Freq screen counts.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Frequencies))]
    [NotifyPropertyChangedFor(nameof(OutcomeHeading))]
    private SimulationTally _tally = SimulationTally.Sum;

    /// <summary>Gets what the last execution came to, or <see langword="null"/> before one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    [NotifyPropertyChangedFor(nameof(ErrorKey))]
    [NotifyPropertyChangedFor(nameof(Columns))]
    [NotifyPropertyChangedFor(nameof(Rows))]
    [NotifyPropertyChangedFor(nameof(Frequencies))]
    [NotifyPropertyChangedFor(nameof(OutcomeHeading))]
    private Simulation? _result;

    /// <summary>Gets or sets the row of the Relative Freq screen whose value a store takes (p. 149).</summary>
    [ObservableProperty]
    private SimulationFrequencyRow? _selected;

    /// <summary>Gets the variable the selected relative frequency was last stored in, or <see langword="null"/>.</summary>
    [ObservableProperty]
    private MemoryVariable? _stored;

    /// <summary>Gets whether there are results to show.</summary>
    public bool HasResult => Result is { Succeeded: true };

    /// <summary>Gets the localization key of the error: the attempts could not be read, or were out of range.</summary>
    public string? ErrorKey => Attempts[0, 0].ErrorKey ?? (Result?.Error is { } error ? "error." + error.Kind : null);

    /// <summary>Gets what the Relative Freq screen can count: the sum, and for two dice the difference; for coins, heads.</summary>
    public ImmutableArray<SimulationTally> Tallies => Kind == SimulationKind.CoinToss
        ? [SimulationTally.Heads]
        : Count == 2 ? [SimulationTally.Sum, SimulationTally.Difference] : [SimulationTally.Sum];

    /// <summary>Gets the headings of the List screen: A, B and C, then Sum and Diff for dice or the heads of coins (pp. 149, 153).</summary>
    public ImmutableArray<string> Columns
    {
        get
        {
            // A new count clears the result, so the one on the screen is always the count it was thrown with.
            int count = Count;
            IEnumerable<string> pieces = ((string[])["A", "B", "C"]).Take(count);
            return Kind == SimulationKind.DiceRoll
                ? [.. pieces, .. count >= 2 ? (string[])["Sum"] : [], .. count == 2 ? (string[])["Diff"] : []]
                : [.. pieces, .. count >= 2 ? (string[])[Heads] : []];
        }
    }

    /// <summary>Gets every attempt, as the List screen shows it.</summary>
    public ImmutableArray<SimulationRow> Rows
    {
        get
        {
            if (Result is not { Succeeded: true } simulation)
            {
                return [];
            }

            return
            [
                .. simulation.Attempts.Select((attempt, index) => new SimulationRow(index + 1, [.. Cells(simulation, attempt, index)])),
            ];
        }
    }

    /// <summary>Gets the heading of the outcome column of the Relative Freq screen: Sum, Diff or Side.</summary>
    public string OutcomeHeading => Kind == SimulationKind.CoinToss ? "Side" : Tally == SimulationTally.Difference ? "Diff" : "Sum";

    /// <summary>Gets the rows of the Relative Freq screen, each relative frequency as a decimal (assumption U27).</summary>
    public ImmutableArray<SimulationFrequencyRow> Frequencies
    {
        get
        {
            if (Result is not { Succeeded: true } simulation || !Tallies.Contains(Tally))
            {
                return [];
            }

            CalculatorSettings decimals = DecimalOutput(_session.Settings);
            return
            [
                .. simulation.Frequencies(Tally).Select(row => new SimulationFrequencyRow(
                    Outcome(simulation, row.Outcome),
                    row.Frequency,
                    PallasEngine.Format(row.RelativeFrequency, decimals, _session.Profile)?.Text ?? string.Empty,
                    row.RelativeFrequency)),
            ];
        }
    }

    /// <summary>Gets the variables a relative frequency can be stored in.</summary>
    public ImmutableArray<MemoryVariable> Variables { get; } = [.. Enum.GetValues<MemoryVariable>()];

    /// <summary>Runs the simulation with the parameters on the screen.</summary>
    [RelayCommand]
    public void Execute()
    {
        // A cell that is not a number holds zero, which the engine refuses as a Range ERROR; ErrorKey names the
        // cell's own error first.
        Stored = null;
        Result = _session.Simulate(Kind, Count, Attempts[0, 0].Value, SameResult);
        Tally = Tallies[0];
        Selected = Frequencies.FirstOrDefault();
    }

    /// <summary>Stores the selected relative frequency in a variable, as [A=] &gt; [Store] does (p. 149).</summary>
    /// <param name="variable">The variable.</param>
    [RelayCommand]
    public void StoreIn(MemoryVariable variable)
    {
        if (Selected is { } row)
        {
            _session.SetVariable(variable, row.Value);
            Stored = variable;
        }
    }

    /// <summary>Clears the result, back to the parameter screen.</summary>
    [RelayCommand]
    public void Clear()
    {
        Result = null;
        Selected = null;
        Stored = null;
    }

    /// <summary>Takes the settings back into account: the relative frequencies are written in the number format in effect.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(Frequencies));
        Selected = Frequencies.FirstOrDefault(row => row.Outcome == Selected?.Outcome);
    }

    /// <summary>The same settings, with the output a decimal: a relative frequency is shown as one (p. 150).</summary>
    internal static CalculatorSettings DecimalOutput(CalculatorSettings settings) => settings with
    {
        InputOutput = settings.InputOutput switch
        {
            InputOutput.MathIMathO => InputOutput.MathIDecimalO,
            InputOutput.LineILineO => InputOutput.LineIDecimalO,
            _ => settings.InputOutput,
        },
    };

    partial void OnCountChanged(int value)
    {
        Tally = Tallies[0];
        Clear();
    }

    partial void OnSameResultChanged(SameResult value) => Clear();

    // The rows are another list: a row of the sums is not what a store takes once the differences are shown.
    partial void OnTallyChanged(SimulationTally value) => Selected = Frequencies.FirstOrDefault();

    private IEnumerable<string> Cells(Simulation simulation, ImmutableArray<int> attempt, int index)
    {
        if (Kind == SimulationKind.CoinToss)
        {
            foreach (int side in attempt)
            {
                yield return side == 1 ? Heads : Tails;
            }

            if (simulation.Count >= 2)
            {
                yield return simulation.Sum(index).ToString(CultureInfo.InvariantCulture);
            }

            yield break;
        }

        foreach (int face in attempt)
        {
            yield return face.ToString(CultureInfo.InvariantCulture);
        }

        if (simulation.Count >= 2)
        {
            yield return simulation.Sum(index).ToString(CultureInfo.InvariantCulture);
        }

        if (simulation.Count == 2)
        {
            yield return simulation.Difference(index).ToString(CultureInfo.InvariantCulture);
        }
    }

    private static string Outcome(Simulation simulation, int outcome) =>
        simulation.Kind == SimulationKind.DiceRoll ? outcome.ToString(CultureInfo.InvariantCulture)
        : simulation.Count == 1 ? (outcome == 1 ? Heads : Tails)
        : Heads + "×" + outcome.ToString(CultureInfo.InvariantCulture);
}

/// <summary>One attempt of the List screen.</summary>
/// <param name="Number">Its number, from 1.</param>
/// <param name="Cells">What each column shows.</param>
public sealed record SimulationRow(int Number, ImmutableArray<string> Cells);

/// <summary>One row of the Relative Freq screen.</summary>
/// <param name="Outcome">The sum, the difference or the heads, as the calculator writes it (●×2).</param>
/// <param name="Frequency">How often it came up.</param>
/// <param name="RelativeFrequency">The relative frequency as the screen shows it, a decimal.</param>
/// <param name="Value">The relative frequency, which a store puts in a variable.</param>
public sealed record SimulationFrequencyRow(string Outcome, int Frequency, string RelativeFrequency, Value Value);
