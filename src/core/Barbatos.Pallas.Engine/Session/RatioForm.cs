// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// Which ratio the Ratio application solves for X (manual p. 145).
/// </summary>
public enum RatioForm
{
    /// <summary>A:B = X:D, where X is A·D/B.</summary>
    XInSecondRatio = 0,

    /// <summary>A:B = C:X, where X is B·C/A.</summary>
    XLastInSecondRatio = 1,
}
