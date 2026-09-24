// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>The Result Type menu of a simulation: which of its two screens shows it (manual p. 148).</summary>
public enum SimulationResultView
{
    /// <summary>List: every attempt, in order.</summary>
    List = 0,

    /// <summary>Relative Freq: how often each outcome came up.</summary>
    RelativeFrequency = 1,
}
