// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>
/// The Number Line application of Math Box: up to three expressions, each drawn as a part of the x axis, and the
/// View-Window they are drawn in (manual pp. 153-157).
/// </summary>
/// <remarks>
/// Nothing here depends on a session - a number line calculates nothing - so it is a set of functions rather than a
/// form of <see cref="CalculatorSession"/>. Which three expressions are registered, and clearing them when the angle
/// unit changes (p. 155), is the application's.
/// </remarks>
public static class NumberLine
{
    /// <summary>The most expressions a number line registers: axes A, B and C (p. 153).</summary>
    public const int MaximumAxes = 3;

    // p. 153: -1×10¹⁰ ≤ a ≤ 1×10¹⁰, and the same for b; p. 157: the same for the center, and 1×10⁻¹⁰ ≤ Scale ≤ 1×10¹⁰.
    private const decimal Bound = 10_000_000_000m;
    private const decimal SmallestScale = 0.0000000001m;

    /// <summary>Registers an expression.</summary>
    /// <param name="form">The form.</param>
    /// <param name="a">a.</param>
    /// <param name="b">b, for <see cref="NumberLineForm.Between"/> and the three other forms with two bounds; otherwise <see langword="null"/>.</param>
    /// <returns>The axis, or its Range ERROR when a or b is beyond ±10¹⁰ or a is not below b (pp. 153, 165).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="form"/> is not a defined value.</exception>
    /// <exception cref="ArgumentException"><paramref name="b"/> is given to a form with one bound, or missing from a form with two.</exception>
    /// <remarks>
    /// The manual names 10&lt;x≤5 as out of range; a = b is taken as out of range too, for every form with two bounds,
    /// since it is either an empty set or a single point that x=a already draws (assumption U28 of docs/CONFORMANCE.md).
    /// </remarks>
    public static NumberLineAxis Define(NumberLineForm form, Value a, Value? b = null)
    {
        if (!Enum.IsDefined(form))
        {
            throw new ArgumentOutOfRangeException(nameof(form), form, "Not a defined NumberLineForm value.");
        }

        bool twoBounds = form >= NumberLineForm.Between;
        if (twoBounds != b.HasValue)
        {
            throw new ArgumentException(twoBounds ? $"The form {form} has two bounds, a and b." : $"The form {form} has one bound, a.", nameof(b));
        }

        bool inRange = InRange(a) && (b is not { } upper || (InRange(upper) && a.ToDecimalNearest() < upper.ToDecimalNearest()));
        return new NumberLineAxis(form, a, b, inRange ? null : new CalcError(CalcErrorKind.RangeError, default));
    }

    /// <summary>Sets the View-Window by hand (p. 156).</summary>
    /// <param name="center">The value in the middle of the axis, within ±10¹⁰.</param>
    /// <param name="scale">The distance between two ticks, from 10⁻¹⁰ to 10¹⁰.</param>
    /// <returns>The view, or its Range ERROR (p. 165).</returns>
    public static NumberLineView View(Value center, Value scale)
    {
        bool inRange = InRange(center) && InRange(scale) && scale.ToDecimalNearest() >= SmallestScale;
        return inRange
            ? new NumberLineView(center.ToDecimalNearest(), scale.ToDecimalNearest())
            : new NumberLineView(0m, 1m, new CalcError(CalcErrorKind.RangeError, default));
    }

    /// <summary>The View-Window the calculator sets when the expressions are drawn, before any is set by hand (p. 156).</summary>
    /// <param name="axes">The registered expressions; one that failed is left out.</param>
    /// <returns>A view in which every bound of every expression is on the axis.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="axes"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The manual gives one example and not the rule: x≤-1.5, x&gt;-1.0 and -2.0&lt;x≤-0.5 are shown with Scale 0.2 and
    /// Center -1.2. The rule taken is the one that example follows (assumption U29): the smallest scale of 1, 2 or 5 times
    /// a power of ten whose eight ticks span the bounds, and the center the middle of the bounds rounded to a tick.
    /// A single bound spans the larger of its distance from 0 and 1; no bound at all is the view of 0 with Scale 1.
    /// </remarks>
    public static NumberLineView Fit(IReadOnlyList<NumberLineAxis> axes)
    {
        ArgumentNullException.ThrowIfNull(axes);
        decimal[] bounds =
        [
            .. axes.Where(axis => axis.Succeeded)
                .SelectMany(axis => (Value?[])[axis.Lower, axis.Upper])
                .OfType<Value>()
                .Select(bound => bound.ToDecimalNearest()),
        ];

        if (bounds.Length == 0)
        {
            return new NumberLineView(0m, 1m);
        }

        decimal lowest = bounds.Min();
        decimal highest = bounds.Max();
        decimal span = highest > lowest ? highest - lowest : Math.Max(Math.Abs(lowest), 1m);
        decimal scale = NiceScale(span / NumberLineView.Ticks);
        decimal center = Math.Round((lowest + highest) / 2m / scale, 0, MidpointRounding.AwayFromZero) * scale;
        return new NumberLineView(center, scale);
    }

    // Compared as a double first: a value far beyond ±10¹⁰ may be a double that no decimal holds.
    private static bool InRange(Value value) =>
        value.IsReal && Math.Abs(value.ToDouble()) <= (double)Bound && Math.Abs(value.ToDecimalNearest()) <= Bound;

    private static decimal NiceScale(decimal least)
    {
        // 1, 2 and 5 times 10⁻¹⁰, 10⁻⁹, … 10¹⁰; bounds within ±10¹⁰ never need more than 5×10⁹.
        for (decimal decade = SmallestScale; ; decade *= 10m)
        {
            foreach (decimal step in (ReadOnlySpan<decimal>)[1m, 2m, 5m])
            {
                if (step * decade >= least)
                {
                    return step * decade;
                }
            }
        }
    }
}
