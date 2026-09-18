// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The atomic weight of one element, as <c>AtWt(n)</c> returns it.
/// </summary>
/// <param name="AtomicNumber">The atomic number, 1 to 118.</param>
/// <param name="Symbol">The chemical symbol: <c>Sc</c>.</param>
/// <param name="Weight">
/// The standard atomic weight, or its conventional value for an element given as an interval; for an element without a
/// standard atomic weight, the mass number of its longest-lived isotope.
/// </param>
/// <param name="IsMassNumber">Whether <paramref name="Weight"/> is a mass number, shown in brackets on the periodic table.</param>
public sealed record AtomicWeight(int AtomicNumber, string Symbol, decimal Weight, bool IsMassNumber);
