// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Windows;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf;

/// <summary>
/// Where the window was and how big it was, kept in the preferences so the calculator opens where it was closed.
/// </summary>
/// <remarks>
/// <para>
/// The bounds are one string under <see cref="Key"/>, written in the invariant culture - a decimal comma in a
/// Vietnamese Windows would otherwise be a stored place no other build reads. A maximized window keeps the bounds it
/// had before, which is where it goes back to; a minimized one opens as it was before it was minimized.
/// </para>
/// <para>
/// A place is used only while enough of it is still on the screens (<see cref="Fit"/>): a window stored on a monitor
/// that is no longer plugged in would otherwise open where nobody can reach it. The screens are
/// <see cref="SystemParameters"/>' virtual screen, the rectangle round every monitor, which WPF offers without
/// WinForms; a gap between two monitors of different sizes is inside it, and the title bar being reachable is what is
/// checked for that reason.
/// </para>
/// </remarks>
public static class WindowPlacement
{
    /// <summary>The name the bounds are stored under.</summary>
    public const string Key = "window";

    /// <summary>How much of the title bar must be on a screen for the place to be used, in device-independent pixels.</summary>
    public const double Reachable = 120;

    /// <summary>The height of the strip at the top of the window that counts as its title bar.</summary>
    public const double TitleBar = 32;

    /// <summary>Writes a place as the text that is stored.</summary>
    /// <param name="bounds">The bounds of the window in its normal state.</param>
    /// <param name="maximized">Whether the window was maximized.</param>
    /// <returns>The text, in the invariant culture.</returns>
    public static string Write(Rect bounds, bool maximized) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{bounds.Left:R};{bounds.Top:R};{bounds.Width:R};{bounds.Height:R};{(maximized ? 1 : 0)}");

    /// <summary>Reads back a place that was stored.</summary>
    /// <param name="text">The stored text.</param>
    /// <param name="bounds">The bounds of the window in its normal state.</param>
    /// <param name="maximized">Whether the window was maximized.</param>
    /// <returns><see langword="true"/> when the text is a place with a positive size.</returns>
    public static bool TryRead(string? text, out Rect bounds, out bool maximized)
    {
        bounds = Rect.Empty;
        maximized = false;
        string[] parts = (text ?? string.Empty).Split(';');
        if (parts.Length != 5
            || !TryNumber(parts[0], out double left)
            || !TryNumber(parts[1], out double top)
            || !TryNumber(parts[2], out double width)
            || !TryNumber(parts[3], out double height)
            || width <= 0
            || height <= 0
            || parts[4] is not ("0" or "1"))
        {
            return false;
        }

        bounds = new Rect(left, top, width, height);
        maximized = parts[4] == "1";
        return true;
    }

    /// <summary>Returns the bounds a stored place gives on the screens there are now.</summary>
    /// <param name="saved">The stored bounds.</param>
    /// <param name="screen">The rectangle round every screen.</param>
    /// <returns>
    /// The stored bounds, no larger than the screens; or <see langword="null"/> when too little of the title bar would
    /// be on them, and the window is better centred.
    /// </returns>
    public static Rect? Fit(Rect saved, Rect screen)
    {
        if (saved.IsEmpty || screen.IsEmpty)
        {
            return null;
        }

        // The whole height of the title bar, and enough of its width to take hold of.
        Rect title = new(saved.Left, saved.Top, saved.Width, Math.Min(TitleBar, saved.Height));
        title.Intersect(screen);
        if (title.IsEmpty || title.Height < Math.Min(TitleBar, saved.Height) || title.Width < Math.Min(Reachable, saved.Width))
        {
            return null;
        }

        return new Rect(saved.Left, saved.Top, Math.Min(saved.Width, screen.Width), Math.Min(saved.Height, screen.Height));
    }

    /// <summary>Puts a window where it was when it was last closed, before it is shown.</summary>
    /// <param name="window">The window, not yet shown.</param>
    /// <param name="preferences">The preferences the place was stored in.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static void Restore(Window window, IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(preferences);
        if (!TryRead(preferences.Get(Key, string.Empty), out Rect saved, out bool maximized)
            || Fit(saved, Screen()) is not { } bounds)
        {
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = bounds.Left;
        window.Top = bounds.Top;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        if (maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    /// <summary>Stores where a window is, as it closes.</summary>
    /// <param name="window">The window.</param>
    /// <param name="preferences">The preferences to store the place in.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public static void Keep(Window window, IPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(preferences);

        // A window that was never shown or never placed has no bounds (NaN, or an empty restore rectangle) and keeps
        // what was stored before.
        Rect bounds = window.WindowState == WindowState.Normal
            ? new Rect(window.Left, window.Top, window.Width, window.Height)
            : window.RestoreBounds;
        if (TryRead(Write(bounds, maximized: false), out _, out _))
        {
            preferences.Set(Key, Write(bounds, window.WindowState == WindowState.Maximized));
        }
    }

    private static Rect Screen() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    private static bool TryNumber(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value);
}
