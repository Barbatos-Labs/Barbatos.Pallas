// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>One row of the Relative Freq screen of a simulation (manual pp. 150, 153).</summary>
/// <param name="Outcome">The sum, the difference or the number of heads the row counts.</param>
/// <param name="Frequency">How many attempts had that outcome (Freq).</param>
/// <param name="RelativeFrequency">The frequency divided by the number of attempts (Rel Fr), a decimal.</param>
public sealed record SimulationFrequency(int Outcome, int Frequency, Value RelativeFrequency);
