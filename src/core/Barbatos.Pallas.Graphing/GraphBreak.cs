// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Graphing;

/// <summary>Where a curve is not drawn through, and why.</summary>
/// <param name="X">Where it breaks, to the resolution the sampler narrowed it to.</param>
/// <param name="Kind">An asymptote or a jump.</param>
public readonly record struct GraphBreak(double X, GraphBreakKind Kind);

/// <summary>Why a curve breaks.</summary>
public enum GraphBreakKind
{
    /// <summary>The values grow without bound towards the break: tan x at 90°, 1÷x at 0.</summary>
    Asymptote,

    /// <summary>The values jump by a finite step: Int(x) at an integer.</summary>
    Jump,
}
