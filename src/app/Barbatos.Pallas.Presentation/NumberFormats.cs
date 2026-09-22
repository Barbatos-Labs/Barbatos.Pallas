// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The Number Format setting as a kind and a digit count, and as the text a stored session holds.
/// </summary>
/// <remarks>
/// The screen offers the kind and then the digits (manual p. 23), and a stored session holds the pair as one word -
/// <c>Norm1</c>, <c>Fix3</c>, <c>Sci10</c>. Both need the same range check, so it is written once.
/// </remarks>
internal static class NumberFormats
{
    /// <summary>Creates the setting from a kind and a digit count, or fails when the count is outside its range.</summary>
    public static bool TryCreate(NumberFormatKind kind, int digits, out NumberFormat format)
    {
        switch (kind)
        {
            case NumberFormatKind.Norm when digits is 1:
                format = NumberFormat.Norm1;
                return true;
            case NumberFormatKind.Norm when digits is 2:
                format = NumberFormat.Norm2;
                return true;
            case NumberFormatKind.Fix when digits is >= 0 and <= 9:
                format = NumberFormat.Fix(digits);
                return true;
            case NumberFormatKind.Sci when digits is >= 1 and <= 10:
                format = NumberFormat.Sci(digits);
                return true;
            default:
                format = NumberFormat.Norm1;
                return false;
        }
    }

    /// <summary>Writes the setting as one word, as a stored session holds it.</summary>
    public static string Write(NumberFormat format) =>
        format.Kind.ToString() + format.Digits.ToString(CultureInfo.InvariantCulture);

    /// <summary>Reads back what <see cref="Write"/> produced; anything else is not a setting.</summary>
    public static bool TryRead(string? text, out NumberFormat format)
    {
        format = NumberFormat.Norm1;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int digit = 0;
        while (digit < text.Length && !char.IsAsciiDigit(text[digit]))
        {
            digit++;
        }

        return Enum.TryParse(text[..digit], out NumberFormatKind kind)
            && Enum.IsDefined(kind)
            && int.TryParse(text[digit..], NumberStyles.None, CultureInfo.InvariantCulture, out int digits)
            && TryCreate(kind, digits, out format);
    }
}
