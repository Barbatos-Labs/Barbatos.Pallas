// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The conversions of the FORMAT menu (manual pp. 42-50).
/// </summary>
public enum FormatTarget
{
    /// <summary>Standard: fractions, square roots and π where possible.</summary>
    Standard = 0,

    /// <summary>Decimal.</summary>
    DecimalValue = 1,

    /// <summary>Prime factorization of a positive integer of at most 10 digits.</summary>
    PrimeFactor = 2,

    /// <summary>Recurring decimal: <c>3.(3)</c>.</summary>
    RecurringDecimal = 3,

    /// <summary>Rectangular coordinates of a complex number: a+bi.</summary>
    Rectangular = 4,

    /// <summary>Polar coordinates of a complex number: r∠θ.</summary>
    Polar = 5,

    /// <summary>Improper fraction: <c>13⌟4</c>.</summary>
    ImproperFraction = 6,

    /// <summary>Mixed fraction: <c>3⌟1⌟4</c>.</summary>
    MixedFraction = 7,

    /// <summary>Engineering notation, with an exponent that is a multiple of 3.</summary>
    Engineering = 8,

    /// <summary>Degrees, minutes and seconds.</summary>
    Sexagesimal = 9,
}
