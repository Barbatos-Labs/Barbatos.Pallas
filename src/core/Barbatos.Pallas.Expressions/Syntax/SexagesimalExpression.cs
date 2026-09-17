// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// Degrees-minutes-seconds: <c>2°20′30″</c>.
/// </summary>
/// <remarks>
/// On the calculator the °′″ key and the degree unit <c>°</c> are different keys: in Radian mode, <c>sin(30°)</c> is the
/// sine of 30 degrees, while <c>30°0′0″</c> is just the number 30. In text, <c>°</c> followed by minutes or seconds is
/// sexagesimal; <c>°</c> alone is the unit (<see cref="PostfixOperator.Degrees"/>). Minutes and seconds are plain numbers,
/// which lets the parser decide with one token of lookahead. Printers write all three parts, a missing one as 0.
/// </remarks>
public sealed class SexagesimalExpression : SyntaxNode
{
    /// <summary>Initializes a sexagesimal value.</summary>
    /// <param name="degrees">The degrees.</param>
    /// <param name="minutes">The minutes, or <see langword="null"/> when not written.</param>
    /// <param name="seconds">The seconds, or <see langword="null"/> when not written.</param>
    /// <param name="span">Where the value is.</param>
    /// <exception cref="ArgumentException">Both <paramref name="minutes"/> and <paramref name="seconds"/> are <see langword="null"/>; that is a degree unit.</exception>
    public SexagesimalExpression(SyntaxNode degrees, NumberLiteral? minutes, NumberLiteral? seconds, SourceSpan span = default)
        : base(span)
    {
        Degrees = NotNull(degrees, nameof(degrees));
        if (minutes is null && seconds is null)
        {
            throw new ArgumentException("A sexagesimal value has minutes or seconds; degrees alone are PostfixOperator.Degrees.", nameof(minutes));
        }

        Minutes = minutes;
        Seconds = seconds;
    }

    /// <summary>Gets the degrees.</summary>
    public SyntaxNode Degrees { get; }

    /// <summary>Gets the minutes, or <see langword="null"/>.</summary>
    public NumberLiteral? Minutes { get; }

    /// <summary>Gets the seconds, or <see langword="null"/>.</summary>
    public NumberLiteral? Seconds { get; }
}
