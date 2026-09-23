// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// A Dice Roll or Coin Toss of the Math Box application: every attempt, and what the List and Relative Freq screens
/// show of them (manual pp. 147-153).
/// </summary>
public sealed class Simulation
{
    /// <summary>The fewest dice or coins a simulation throws.</summary>
    public const int MinimumCount = 1;

    /// <summary>The most dice or coins a simulation throws.</summary>
    public const int MaximumCount = 3;

    /// <summary>The most attempts a simulation makes (p. 148).</summary>
    public const int MaximumAttempts = 250;

    private Simulation(SimulationKind kind, int count, ImmutableArray<ImmutableArray<int>> attempts, CalcError? error)
    {
        Kind = kind;
        Count = count;
        Attempts = attempts;
        Error = error;
    }

    /// <summary>Gets whether the simulation threw dice or coins.</summary>
    public SimulationKind Kind { get; }

    /// <summary>Gets how many dice or coins each attempt threw.</summary>
    public int Count { get; }

    /// <summary>
    /// Gets every attempt, in order, as the List screen shows them: the face of each die (1 to 6), or each coin (1 for
    /// heads, 0 for tails), in the columns A, B and C (pp. 149, 153).
    /// </summary>
    public ImmutableArray<ImmutableArray<int>> Attempts { get; }

    /// <summary>Gets the Range ERROR when the number of attempts was not a whole number from 1 to 250 (p. 164).</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the simulation ran.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Returns the Sum column of an attempt: its one die, or the dice added up; for coins, how many were heads.</summary>
    /// <param name="attempt">The attempt, counted from 0.</param>
    /// <returns>The sum.</returns>
    public int Sum(int attempt) => Attempts[attempt].Sum();

    /// <summary>Returns the Diff column of an attempt of two dice: how far apart they landed (p. 149).</summary>
    /// <param name="attempt">The attempt, counted from 0.</param>
    /// <returns>The difference, 0 to 5.</returns>
    /// <exception cref="InvalidOperationException">The simulation did not throw two dice.</exception>
    public int Difference(int attempt)
    {
        if (Kind != SimulationKind.DiceRoll || Count != 2)
        {
            throw new InvalidOperationException("Only a roll of two dice has a difference (p. 149).");
        }

        return Math.Abs(Attempts[attempt][0] - Attempts[attempt][1]);
    }

    /// <summary>Counts the attempts by their outcome, as the Relative Freq screen does (pp. 150, 153).</summary>
    /// <param name="tally">
    /// What to count: the sum or the difference of dice, or the heads of coins. One coin counts heads too, so its two
    /// rows are tails (0) and heads (1).
    /// </param>
    /// <returns>One row per possible outcome, the smallest first, with its frequency and relative frequency.</returns>
    /// <exception cref="ArgumentException"><paramref name="tally"/> does not apply to what was thrown.</exception>
    /// <exception cref="InvalidOperationException">The simulation did not run.</exception>
    /// <remarks>
    /// A relative frequency is the decimal quotient, exact wherever a decimal has one - every frequency of 100 or 250
    /// attempts. The calculator shows it as a decimal, 0.184 rather than 23⌟125, under every Input/Output setting, so an
    /// application formats it with decimal output (assumption U27 of docs/CONFORMANCE.md).
    /// </remarks>
    public ImmutableArray<SimulationFrequency> Frequencies(SimulationTally tally)
    {
        if (!Succeeded)
        {
            throw new InvalidOperationException("A simulation that did not run has no frequencies.");
        }

        (int lowest, int highest, Func<int, int> outcome) = (Kind, tally) switch
        {
            (SimulationKind.DiceRoll, SimulationTally.Sum) => (Count, 6 * Count, (Func<int, int>)Sum),
            (SimulationKind.DiceRoll, SimulationTally.Difference) when Count == 2 => (0, 5, Difference),
            (SimulationKind.CoinToss, SimulationTally.Heads) => (0, Count, Sum),
            _ => throw new ArgumentException($"A {Kind} of {Count} is not counted by {tally}.", nameof(tally)),
        };

        int[] frequencies = new int[highest - lowest + 1];
        for (int attempt = 0; attempt < Attempts.Length; attempt++)
        {
            frequencies[outcome(attempt) - lowest]++;
        }

        return
        [
            .. frequencies.Select((frequency, index) =>
                new SimulationFrequency(lowest + index, frequency, Value.FromDecimal((decimal)frequency / Attempts.Length))),
        ];
    }

    /// <summary>Throws the dice or coins of every attempt.</summary>
    internal static Simulation Run(SimulationKind kind, int count, int attempts, Random random)
    {
        int faces = kind == SimulationKind.DiceRoll ? 6 : 2;
        int lowest = kind == SimulationKind.DiceRoll ? 1 : 0;
        ImmutableArray<int>[] thrown = new ImmutableArray<int>[attempts];
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            int[] one = new int[count];
            for (int piece = 0; piece < count; piece++)
            {
                one[piece] = lowest + random.Next(faces);
            }

            thrown[attempt] = [.. one];
        }

        return new Simulation(kind, count, [.. thrown], null);
    }

    /// <summary>The simulation that could not run.</summary>
    internal static Simulation Failed(SimulationKind kind, int count) =>
        new(kind, count, [], new CalcError(CalcErrorKind.RangeError, default));
}
