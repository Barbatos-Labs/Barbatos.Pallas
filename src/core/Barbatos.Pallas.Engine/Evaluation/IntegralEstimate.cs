// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The error estimate of one numerical integration in a calculation (invariant I7 of docs/PRECISION.md).
/// </summary>
/// <param name="Span">The <c>∫(</c> call in the input.</param>
/// <param name="Result">The computed integral, in <see cref="double"/>.</param>
/// <param name="ErrorEstimate">The estimated absolute error of <paramref name="Result"/>.</param>
public readonly record struct IntegralEstimate(SourceSpan Span, double Result, double ErrorEstimate);
