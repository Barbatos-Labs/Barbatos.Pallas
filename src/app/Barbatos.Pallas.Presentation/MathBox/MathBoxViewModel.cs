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
/// The Math Box screen: its menu of four tools, and each tool's own screen (manual pp. 146-161).
/// </summary>
/// <remarks>
/// Every tool is built with the screen and kept, so what was typed in one is still there after another was used, as
/// the other screens of the shell keep theirs. Math Box has no line of its own: what the calculator types is a
/// parameter, a bound or an angle, each a cell calculated through the session.
/// </remarks>
public sealed partial class MathBoxViewModel : ObservableObject
{
    /// <summary>Creates the screen over a session.</summary>
    /// <param name="session">The session every tool works on.</param>
    /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null"/>.</exception>
    public MathBoxViewModel(CalculatorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        Dice = new SimulationViewModel(session, SimulationKind.DiceRoll);
        Coins = new SimulationViewModel(session, SimulationKind.CoinToss);
        NumberLine = new NumberLineViewModel(session);
        Circle = new CircleViewModel(session);
    }

    /// <summary>Gets the tools of the menu, in its order.</summary>
    public ImmutableArray<MathBoxTool> Tools { get; } = [.. Enum.GetValues<MathBoxTool>()];

    /// <summary>Gets the Dice Roll screen.</summary>
    public SimulationViewModel Dice { get; }

    /// <summary>Gets the Coin Toss screen.</summary>
    public SimulationViewModel Coins { get; }

    /// <summary>Gets the Number Line screen.</summary>
    public NumberLineViewModel NumberLine { get; }

    /// <summary>Gets the Circle screen.</summary>
    public CircleViewModel Circle { get; }

    /// <summary>Gets the tool that is open, or <see langword="null"/> while the menu is shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMenu))]
    [NotifyPropertyChangedFor(nameof(Current))]
    private MathBoxTool? _tool;

    /// <summary>Gets whether the menu is shown.</summary>
    public bool IsMenu => Tool is null;

    /// <summary>Gets the screen of the open tool, or <see langword="null"/> while the menu is shown.</summary>
    public ObservableObject? Current => Tool switch
    {
        MathBoxTool.DiceRoll => Dice,
        MathBoxTool.CoinToss => Coins,
        MathBoxTool.NumberLine => NumberLine,
        MathBoxTool.Circle => Circle,
        _ => null,
    };

    /// <summary>Opens a tool from the menu.</summary>
    /// <param name="tool">The tool.</param>
    [RelayCommand]
    public void Open(MathBoxTool tool) => Tool = tool;

    /// <summary>Back to the menu, as ↩ does on a parameter screen (p. 149).</summary>
    [RelayCommand]
    public void Back() => Tool = null;

    /// <summary>Takes the settings back into account, as the application is opened again.</summary>
    public void Refresh()
    {
        NumberLine.Refresh();
        Circle.Refresh();
        Dice.Refresh();
        Coins.Refresh();
    }
}
