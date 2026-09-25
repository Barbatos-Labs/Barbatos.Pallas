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
/// The Vector screen (manual pp. 144-149): the four vectors, the one being edited, and calculations on them.
/// </summary>
/// <remarks>The same shape as the Matrix screen, with one row: a vector of two or three elements (p. 144).</remarks>
public sealed partial class VectorViewModel : ObservableObject
{
    private readonly CalculatorSession _session;

    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session that holds the vectors.</param>
    /// <param name="history">The session's history, shared by its screens; <see langword="null"/> for this run's alone.</param>
    /// <param name="work">The session's work, shared by its screens; <see langword="null"/> to calculate where asked.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public VectorViewModel(CalculatorSession session, SessionHistory? history = null, SessionWork? work = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        Calculate = new CalculateViewModel(session, history, work);
        Grid = new ValueGridViewModel(session, 1, 3);
        Load();
    }

    /// <summary>Gets the vectors there are (VctA to VctD).</summary>
    public ImmutableArray<VectorVariable> Names { get; } = [.. Enum.GetValues<VectorVariable>()];

    /// <summary>Gets the line calculations on the vectors are typed on.</summary>
    public CalculateViewModel Calculate { get; }

    /// <summary>Gets the elements of the vector being edited.</summary>
    public ValueGridViewModel Grid { get; }

    /// <summary>Gets or sets which vector is being edited.</summary>
    [ObservableProperty]
    private VectorVariable _name = VectorVariable.VctA;

    /// <summary>Gets whether the vector being edited is stored in the session.</summary>
    public bool IsDefined => _session.GetVector(Name) is not null;

    /// <summary>Gets the last vector a calculation produced, or <see langword="null"/> (VctAns, p. 145).</summary>
    public VectorValue? Answer => _session.VctAns;

    /// <summary>Stores what the grid holds as the vector being edited.</summary>
    [RelayCommand]
    public void Store()
    {
        _session.SetVector(Name, new VectorValue([.. Enumerable.Range(0, Grid.Columns).Select(index => Grid[0, index].Value)]));
        OnPropertyChanged(nameof(IsDefined));
    }

    /// <summary>Takes the vector being edited out of the session.</summary>
    [RelayCommand]
    public void Delete()
    {
        _session.SetVector(Name, null);
        Grid.Clear();
        OnPropertyChanged(nameof(IsDefined));
    }

    /// <summary>Puts the last vector a calculation produced into the grid.</summary>
    /// <returns><see langword="true"/> when there was one.</returns>
    public bool TakeAnswer()
    {
        if (_session.VctAns is not { } answer)
        {
            return false;
        }

        Grid.Resize(1, answer.Dimension);
        for (int index = 0; index < answer.Dimension; index++)
        {
            Grid[0, index].Set(answer[index]);
        }

        return true;
    }

    /// <summary>Changes how many elements the vector being edited has (two or three, p. 144).</summary>
    /// <param name="dimension">The dimension.</param>
    /// <returns><see langword="true"/> when a vector of that dimension can be held.</returns>
    public bool Resize(int dimension)
    {
        if (dimension is not (2 or 3))
        {
            return false;
        }

        Grid.Resize(1, dimension);
        return true;
    }

    /// <summary>The last answer, as a command a button can be bound to.</summary>
    [RelayCommand]
    private void UseAnswer() => TakeAnswer();

    partial void OnNameChanged(VectorVariable value) => Load();

    private void Load()
    {
        if (_session.GetVector(Name) is { } vector)
        {
            Grid.Resize(1, vector.Dimension);
            for (int index = 0; index < vector.Dimension; index++)
            {
                Grid[0, index].Set(vector[index]);
            }
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
