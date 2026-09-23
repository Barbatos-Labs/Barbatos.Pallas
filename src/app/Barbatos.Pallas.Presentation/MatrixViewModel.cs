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
/// The Matrix screen (manual pp. 135-143): the four matrices, the one being edited, and calculations on them.
/// </summary>
/// <remarks>
/// A matrix is defined and then used by name in a calculation, so the screen is a grid and the ordinary line of the
/// calculator underneath it: <c>Det(MatA)</c> is typed, not chosen from a menu.
/// </remarks>
public sealed partial class MatrixViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that holds the matrices.</param>
    /// <param name="history">The session's history, shared by its screens; <see langword="null"/> for this run's alone.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public MatrixViewModel(CalculatorSession session, SessionHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Calculate = new CalculateViewModel(session, history);
        Grid = new ValueGridViewModel(session, 2, 2);
        Load();
    }

    /// <summary>Gets the matrices there are (MatA to MatD).</summary>
    public ImmutableArray<MatrixVariable> Names { get; } = [.. Enum.GetValues<MatrixVariable>()];

    /// <summary>Gets the line calculations on the matrices are typed on.</summary>
    public CalculateViewModel Calculate { get; }

    /// <summary>Gets the entries of the matrix being edited.</summary>
    public ValueGridViewModel Grid { get; }

    /// <summary>Gets or sets which matrix is being edited.</summary>
    [ObservableProperty]
    private MatrixVariable _name = MatrixVariable.MatA;

    /// <summary>Gets whether the matrix being edited is stored in the session.</summary>
    public bool IsDefined => _session.GetMatrix(Name) is not null;

    /// <summary>Gets the last matrix a calculation produced, or <see langword="null"/> (MatAns, p. 136).</summary>
    public MatrixValue? Answer => _session.MatAns;

    /// <summary>Stores what the grid holds as the matrix being edited (p. 135).</summary>
    [RelayCommand]
    public void Store()
    {
        _session.SetMatrix(Name, new MatrixValue(Grid.ToArray()));
        OnPropertyChanged(nameof(IsDefined));
    }

    /// <summary>Takes the matrix being edited out of the session (p. 136).</summary>
    [RelayCommand]
    public void Delete()
    {
        _session.SetMatrix(Name, null);
        Grid.Clear();
        OnPropertyChanged(nameof(IsDefined));
    }

    /// <summary>Puts the last matrix a calculation produced into the grid, to carry on from it.</summary>
    /// <returns><see langword="true"/> when there was one.</returns>
    public bool TakeAnswer()
    {
        if (_session.MatAns is not { } answer)
        {
            return false;
        }

        Grid.Set(answer.ToArray());
        return true;
    }

    /// <summary>Changes the size of the matrix being edited (up to 4×4, p. 135).</summary>
    /// <param name="rows">The rows.</param>
    /// <param name="columns">The columns.</param>
    /// <returns><see langword="true"/> when a matrix of that size can be held.</returns>
    public bool Resize(int rows, int columns)
    {
        if (rows is < 1 or > 4 || columns is < 1 or > 4)
        {
            return false;
        }

        Grid.Resize(rows, columns);
        return true;
    }

    /// <summary>The last answer, as a command a button can be bound to.</summary>
    [RelayCommand]
    private void UseAnswer() => TakeAnswer();

    partial void OnNameChanged(MatrixVariable value) => Load();

    private void Load()
    {
        if (_session.GetMatrix(Name) is { } matrix)
        {
            Grid.Set(matrix.ToArray());
        }
        else
        {
            Grid.Clear();
        }

        Calculate.Input.Clear();
        OnPropertyChanged(nameof(IsDefined));
        OnPropertyChanged(nameof(Answer));
    }
}
