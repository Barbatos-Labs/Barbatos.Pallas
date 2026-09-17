// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics;

/// <summary>
/// Recognizes the fraction a <see cref="decimal"/> value stands for, so results can be displayed as fractions without a
/// dedicated rational number type.
/// </summary>
public static class Fractions
{
    /// <summary>
    /// Finds the simplest fraction within <paramref name="tolerance"/> of <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value, for example <c>2.1666666666666666666666666667</c> (the decimal result of 13 ÷ 6).</param>
    /// <param name="maxDenominator">The largest denominator to accept; at least 1.</param>
    /// <param name="tolerance">The largest acceptable absolute difference between the fraction and <paramref name="value"/>; not negative.</param>
    /// <param name="numerator">The numerator, carrying the sign, when the method returns <see langword="true"/>.</param>
    /// <param name="denominator">The positive denominator, when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if such a fraction exists; the fraction is in lowest terms.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDenominator"/> is less than 1 or <paramref name="tolerance"/> is negative.</exception>
    /// <remarks>
    /// <para>
    /// The convergents of the continued fraction of <paramref name="value"/> are the best rational approximations for their
    /// denominators, so the first convergent within the tolerance is the simplest fraction there is.
    /// </para>
    /// <para>
    /// The tolerance reflects how precise the value is. A <see cref="decimal"/> quotient is exact to about 10⁻²⁷, so
    /// <c>1m / 3m * 3m</c> (0.9999999999999999999999999999) is recognized as 1 with a tolerance of 10⁻²⁵. A value converted
    /// from <see cref="double"/> carries 15 significant digits and needs a correspondingly larger tolerance.
    /// </para>
    /// </remarks>
    public static bool TryFromDecimal(decimal value, long maxDenominator, decimal tolerance, out long numerator, out long denominator)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDenominator, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(tolerance);

        numerator = 0;
        denominator = 1;

        decimal target = Math.Abs(value);
        decimal remainder = target;
        long previousNumerator = 0;
        long previousDenominator = 1;
        long currentNumerator = 1;
        long currentDenominator = 0;

        // Terminates: after the first term every coefficient is at least 1, so denominators strictly increase until they
        // pass maxDenominator or overflow long; or the remainder becomes an integer. Written as for (;;) rather than
        // while (true): mutation testing replaces the literal true with false, which does not compile, and Stryker then
        // skips every mutant in the method.
        for (;;)
        {
            decimal wholePart = Math.Floor(remainder);
            if (wholePart > long.MaxValue)
            {
                return false;
            }

            long coefficient = (long)wholePart;
            long nextNumerator;
            long nextDenominator;
            try
            {
                nextNumerator = checked((coefficient * currentNumerator) + previousNumerator);
                nextDenominator = checked((coefficient * currentDenominator) + previousDenominator);
            }
            catch (OverflowException)
            {
                return false;
            }

            if (nextDenominator > maxDenominator)
            {
                return false;
            }

            (previousNumerator, currentNumerator) = (currentNumerator, nextNumerator);
            (previousDenominator, currentDenominator) = (currentDenominator, nextDenominator);

            if (Math.Abs(((decimal)currentNumerator / currentDenominator) - target) <= tolerance)
            {
                numerator = value < 0m ? -currentNumerator : currentNumerator;
                denominator = currentDenominator;
                return true;
            }

            // Defensive: a remainder that is an integer ends the expansion, and its convergent normally matched above.
            // A decimal quotient that rounds to an integer is that integer exactly, and 9.2 million searched inputs
            // (tolerance 0, unbounded denominators, values next to small fractions) never reached this line.
            decimal fraction = remainder - wholePart;
            if (fraction == 0m)
            {
                return false;
            }

            remainder = 1m / fraction;
        }
    }
}
