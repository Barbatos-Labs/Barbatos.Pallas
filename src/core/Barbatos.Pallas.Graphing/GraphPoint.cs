// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Graphing;

/// <summary>A point of a curve, in the units of the plane: where it is drawn, not a value the calculator shows.</summary>
/// <param name="X">Its x.</param>
/// <param name="Y">Its y.</param>
public readonly record struct GraphPoint(double X, double Y);
