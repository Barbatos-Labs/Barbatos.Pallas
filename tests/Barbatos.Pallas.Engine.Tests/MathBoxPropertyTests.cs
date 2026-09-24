// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine.Tests.Support;
using Barbatos.Pallas.Expressions;
using Barbatos.Pallas.Numerics;
using CsCheck;

namespace Barbatos.Pallas.Engine.Tests;

/// <summary>
/// What holds for every throw, every hour and every set of bounds of Math Box, not only for the manual's examples.
/// </summary>
public sealed class MathBoxPropertyTests
{
    private static readonly Gen<(SimulationKind Kind, int Count, int Attempts, SameResult Preset, int Seed)> Throws =
        Gen.Select(
            Gen.OneOfConst(SimulationKind.DiceRoll, SimulationKind.CoinToss),
            Gen.Int[Simulation.MinimumCount, Simulation.MaximumCount],
            Gen.Int[1, Simulation.MaximumAttempts],
            Gen.OneOfConst(Enum.GetValues<SameResult>()),
            Gen.Int);

    [Fact]
    public void AnyThrowIsOfItsFacesAndCountedOnceOnItsScreen()
    {
        Throws.Sample(
            sample =>
            {
                CalculatorSession session = Calculator.Session(CalculatorApp.MathBox, randomSeed: sample.Seed);
                Simulation simulation = session.Simulate(sample.Kind, sample.Count, Value.FromDecimal(sample.Attempts), sample.Preset);
                int highest = sample.Kind == SimulationKind.DiceRoll ? 6 : 1;
                int lowest = sample.Kind == SimulationKind.DiceRoll ? 1 : 0;
                bool thrown = simulation.Succeeded
                    && simulation.Attempts.Length == sample.Attempts
                    && simulation.Attempts.All(attempt => attempt.Length == sample.Count && attempt.All(face => face >= lowest && face <= highest));

                // Each tally the screen offers counts every attempt once, in rows from its lowest outcome to its highest,
                // and a relative frequency is the frequency over the attempts, exactly.
                SimulationTally[] tallies = sample.Kind == SimulationKind.CoinToss
                    ? [SimulationTally.Heads]
                    : sample.Count == 2 ? [SimulationTally.Sum, SimulationTally.Difference] : [SimulationTally.Sum];
                return thrown && tallies.All(tally =>
                {
                    SimulationFrequency[] rows = [.. simulation.Frequencies(tally)];
                    return rows.Sum(row => row.Frequency) == sample.Attempts
                        && rows.Zip(rows.Skip(1)).All(pair => pair.Second.Outcome == pair.First.Outcome + 1)
                        && rows.All(row => row.RelativeFrequency.ToDecimal() == row.Frequency / (decimal)sample.Attempts);
                });
            },
            iter: 300);
    }

    [Fact]
    public void APresetGivesTheSameThrowsWhateverTheSessionsOwnRandomNumbers()
    {
        Throws.Where(sample => sample.Preset != SameResult.Off).Sample(
            sample =>
            {
                Simulation one = Calculator.Session(CalculatorApp.MathBox, randomSeed: sample.Seed)
                    .Simulate(sample.Kind, sample.Count, Value.FromDecimal(sample.Attempts), sample.Preset);
                Simulation other = Calculator.Session(CalculatorApp.MathBox, randomSeed: sample.Seed + 1)
                    .Simulate(sample.Kind, sample.Count, Value.FromDecimal(sample.Attempts), sample.Preset);
                return one.Attempts.Select(attempt => string.Join(",", attempt)).SequenceEqual(other.Attempts.Select(attempt => string.Join(",", attempt)));
            },
            iter: 100);
    }

    [Theory]
    [InlineData(AngleUnit.Degree, "360")]
    [InlineData(AngleUnit.Gradian, "400")]
    [InlineData(AngleUnit.Radian, "2π")]
    public void TheHandsOfTheClockMakeAWholeTurnAtEveryHour(AngleUnit unit, string turn)
    {
        CalculatorSession session = Calculator.Session(CalculatorApp.MathBox, settings: settings => settings with { AngleUnit = unit });
        for (int hour = 1; hour <= 12; hour++)
        {
            ClockAngles clock = session.Clock(hour);
            Calculation sum = session.Evaluate(clock.Smaller.Input + "+" + clock.Larger.Input);

            clock.Smaller.Result.ToDouble().Should().BeLessThanOrEqualTo(clock.Larger.Result.ToDouble(), "at {0}:00", hour);
            sum.Display.Text.Should().Be(turn, "the two angles at {0}:00 make a whole turn", hour);
        }
    }

    [Fact]
    public void AViewFitsAnyBoundsWithinTheRange()
    {
        // Bounds of up to ten digits either side of the point, within ±10¹⁰: the fitted view shows every one, its scale
        // is 1, 2 or 5 times a power of ten, and its center is on a tick.
        Gen<decimal> bound = Gen.Select(Gen.Long[-9_999_999_999L, 9_999_999_999L], Gen.Int[0, 10], (digits, places) => digits / Power(places));
        bound.Array[1, 6].Sample(
            bounds =>
            {
                NumberLineView view = NumberLine.Fit([.. bounds.Select(value => NumberLine.Define(NumberLineForm.Equal, Value.FromDecimal(value)))]);
                decimal leading = view.Scale / Power((int)Math.Floor(Math.Log10((double)view.Scale)));
                return view.Error is null
                    && bounds.All(value => value >= view.Minimum && value <= view.Maximum)
                    && leading is 1m or 2m or 5m
                    && view.Center / view.Scale == decimal.Truncate(view.Center / view.Scale);
            },
            iter: 500);
    }

    // 10 to a power, exactly, as a decimal: 10⁻³ is 0.001 where the double 1e-3 is not.
    private static decimal Power(int places) =>
        places >= 0 ? Enumerable.Repeat(10m, places).Aggregate(1m, (product, ten) => product * ten) : 1m / Power(-places);
}
