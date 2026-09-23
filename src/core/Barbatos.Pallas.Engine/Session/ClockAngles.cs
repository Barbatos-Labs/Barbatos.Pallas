// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The Clock screen of the Circle application: an hour, and the two angles between the hands (manual pp. 158, 161).</summary>
/// <param name="Hour">The hour on the clock, 1 to 12; the minute hand stays on 12.</param>
/// <param name="Smaller">θ1, the smaller angle between the hour hand and the minute hand, in the angle unit of the session.</param>
/// <param name="Larger">θ2, the larger one: a full turn less θ1.</param>
/// <remarks>
/// The angles are calculations, so they are shown as results are: at 3:00, 90 and 270 in degrees, and π⌟2 and 3⌟2π in
/// radians under MathO (p. 161). At 12:00 they are 0 and a full turn, and at 6:00 they are equal (assumption U31).
/// </remarks>
public sealed record ClockAngles(int Hour, Calculation Smaller, Calculation Larger);
