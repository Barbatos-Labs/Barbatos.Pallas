// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The circle an angle of the Circle application is drawn on (manual pp. 157-159).</summary>
public enum CircleKind
{
    /// <summary>Unit Circle: the whole circle of radius 1; an angle between -10000 and 10000 in any unit.</summary>
    UnitCircle = 0,

    /// <summary>Half Circle: its upper half; an angle from 0 to 180°, π or 200 gradians.</summary>
    HalfCircle = 1,
}
