// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// One root of a polynomial: its real part and, for a complex root, its imaginary part (manual pp. 116-118).
/// </summary>
/// <remarks>
/// The two parts are separate values because the calculator displays them in exact form, as <c>-3⌟4+√(23)⌟4i</c> on
/// p. 118: a complex <see cref="Value"/> holds two <see cref="double"/> numbers and cannot carry those forms. The
/// application writes the root as the real part, the sign of the imaginary part, its magnitude and <c>i</c>.
/// </remarks>
public sealed class PolynomialRoot
{
    internal PolynomialRoot(Calculation real, Calculation? imaginary)
    {
        Real = real;
        Imaginary = imaginary;
    }

    /// <summary>Gets the real part.</summary>
    public Calculation Real { get; }

    /// <summary>Gets the imaginary part, or <see langword="null"/> for a real root.</summary>
    public Calculation? Imaginary { get; }

    /// <summary>Gets whether the root is a real number.</summary>
    public bool IsReal => Imaginary is null;
}
