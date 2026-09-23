// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// One expression of the Number Line application and the part of the x axis it covers (manual pp. 153-155).
/// </summary>
/// <remarks>
/// What the calculator draws: a bound that is part of the set as a filled dot, one that is not as an open circle, and
/// a side without a bound as an arrow to the edge of the view (p. 155). <see cref="Lower"/> and <see cref="Upper"/>
/// carry exactly that: a bound or <see langword="null"/> for the arrow, and whether the bound is included.
/// </remarks>
public sealed class NumberLineAxis
{
    internal NumberLineAxis(NumberLineForm form, Value a, Value? b, CalcError? error)
    {
        Form = form;
        A = a;
        B = b;
        Error = error;
    }

    /// <summary>Gets the form of the expression.</summary>
    public NumberLineForm Form { get; }

    /// <summary>Gets a.</summary>
    public Value A { get; }

    /// <summary>Gets b, for the four forms that have one; otherwise <see langword="null"/>.</summary>
    public Value? B { get; }

    /// <summary>
    /// Gets the Range ERROR of p. 165 when a or b is beyond ±10¹⁰ or a is not below b; otherwise <see langword="null"/>.
    /// </summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the expression is one the calculator draws.</summary>
    public bool Succeeded => Error is null;

    /// <summary>Gets the lower end of the set, or <see langword="null"/> when it goes on to the left.</summary>
    public Value? Lower => Form is NumberLineForm.Less or NumberLineForm.LessOrEqual ? null : A;

    /// <summary>Gets whether <see cref="Lower"/> is part of the set.</summary>
    public bool LowerIncluded => Form is NumberLineForm.Equal or NumberLineForm.GreaterOrEqual or NumberLineForm.FromIncluded or NumberLineForm.BetweenIncluded;

    /// <summary>Gets the upper end of the set, or <see langword="null"/> when it goes on to the right.</summary>
    public Value? Upper => Form switch
    {
        NumberLineForm.Less or NumberLineForm.LessOrEqual or NumberLineForm.Equal => A,
        NumberLineForm.Greater or NumberLineForm.GreaterOrEqual => null,
        _ => B,
    };

    /// <summary>Gets whether <see cref="Upper"/> is part of the set.</summary>
    public bool UpperIncluded => Form is NumberLineForm.LessOrEqual or NumberLineForm.Equal or NumberLineForm.ToIncluded or NumberLineForm.BetweenIncluded;
}
