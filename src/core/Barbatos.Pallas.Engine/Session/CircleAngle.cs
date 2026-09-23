// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>An angle of the Unit Circle or Half Circle screen and its trigonometric values (manual pp. 157, 160).</summary>
/// <remarks>
/// The values are calculations of the session, in its angle unit and with its Input/Output setting, so they are shown
/// as every other result is: sin 45° as √(2)⌟2 under MathO (p. 160). A value that does not exist - tan 90° - is that
/// calculation's Math ERROR, while the angle itself is still drawn (assumption U30 of docs/CONFORMANCE.md).
/// </remarks>
public sealed class CircleAngle
{
    internal CircleAngle(CircleKind kind, Value angle, Calculation? sine, Calculation? cosine, Calculation? tangent, CalcError? error)
    {
        Kind = kind;
        Angle = angle;
        Sine = sine;
        Cosine = cosine;
        Tangent = tangent;
        Error = error;
    }

    /// <summary>Gets the circle the angle is drawn on.</summary>
    public CircleKind Kind { get; }

    /// <summary>Gets the angle, in the angle unit of the session.</summary>
    public Value Angle { get; }

    /// <summary>Gets sin θ, or <see langword="null"/> when the angle is out of range.</summary>
    public Calculation? Sine { get; }

    /// <summary>Gets cos θ, or <see langword="null"/> when the angle is out of range.</summary>
    public Calculation? Cosine { get; }

    /// <summary>Gets tan θ, or <see langword="null"/> when the angle is out of range.</summary>
    public Calculation? Tangent { get; }

    /// <summary>Gets the Range ERROR of an angle outside the range of its circle (p. 159); otherwise <see langword="null"/>.</summary>
    public CalcError? Error { get; }

    /// <summary>Gets whether the angle is drawn.</summary>
    public bool Succeeded => Error is null;
}
