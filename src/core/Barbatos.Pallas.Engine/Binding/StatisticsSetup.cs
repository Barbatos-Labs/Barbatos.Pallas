// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>What the Statistics application offers an input: its statistic variables and commands depend on both (pp. 90-91).</summary>
/// <param name="TwoVariable">Whether the data are (x, y) pairs.</param>
/// <param name="Regression">The regression type chosen for two-variable data.</param>
internal readonly record struct StatisticsSetup(bool TwoVariable, RegressionModel Regression);
