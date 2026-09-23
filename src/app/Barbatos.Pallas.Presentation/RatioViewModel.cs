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
/// The Ratio screen (manual pp. 133-134): A:B = X:D or A:B = C:X, and the X that makes it true.
/// </summary>
public sealed partial class RatioViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that solves.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public RatioViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Grid = new ValueGridViewModel(session, 1, 3);
    }

    /// <summary>Gets the two forms a ratio can take.</summary>
    public ImmutableArray<RatioForm> Forms { get; } = [.. Enum.GetValues<RatioForm>()];

    /// <summary>Gets the three values that are known: A, B and the third one.</summary>
    public ValueGridViewModel Grid { get; }

    /// <summary>Gets or sets which form the ratio takes.</summary>
    [ObservableProperty]
    private RatioForm _form = RatioForm.XInSecondRatio;

    /// <summary>Gets what the last solving came to, or <see langword="null"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSolution))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    [NotifyPropertyChangedFor(nameof(ErrorKey))]
    private Calculation? _solution;

    /// <summary>Gets whether X was found.</summary>
    public bool HasSolution => Solution is { Succeeded: true };

    /// <summary>Gets whether the last solving failed.</summary>
    public bool HasError => Solution is { Succeeded: false };

    /// <summary>Gets the localization key of the error, or <see langword="null"/>.</summary>
    public string? ErrorKey => Solution?.Error is { } error ? "error." + error.Kind : null;

    /// <summary>Solves for X.</summary>
    [RelayCommand]
    public void Solve() => Solution = _session.SolveRatio(Form, Grid[0, 0].Value, Grid[0, 1].Value, Grid[0, 2].Value);

    /// <summary>Empties the values and what was solved.</summary>
    [RelayCommand]
    public void Clear()
    {
        Grid.Clear();
        Solution = null;
    }
}
