// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>A probability simulation of the Math Box application (manual pp. 147-153).</summary>
public enum SimulationKind
{
    /// <summary>Dice Roll: one, two or three dice, each landing on 1 to 6 (p. 147).</summary>
    DiceRoll = 0,

    /// <summary>Coin Toss: one, two or three coins, each landing heads or tails (p. 150).</summary>
    CoinToss = 1,
}
