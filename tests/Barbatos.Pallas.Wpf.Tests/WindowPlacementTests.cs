// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>
/// The window opens where it was closed, as long as it can still be reached there.
/// </summary>
public sealed class WindowPlacementTests
{
    private static readonly Rect Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void APlaceIsReadBackAsItWasWritten()
    {
        Rect bounds = new(120.5, 64, 470, 880.25);

        WindowPlacement.TryRead(WindowPlacement.Write(bounds, maximized: true), out Rect read, out bool maximized)
            .Should().BeTrue();

        read.Should().Be(bounds);
        maximized.Should().BeTrue();
    }

    [Fact]
    public void APlaceIsWrittenTheSameWayInEveryCulture()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("vi-VN");

            WindowPlacement.Write(new Rect(10.5, 20, 470, 880), maximized: false).Should().Be("10.5;20;470;880;0");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("10;20;470;880")]
    [InlineData("10;20;470;880;0;1")]
    [InlineData("10,5;20;470;880;0")]
    [InlineData("10;20;0;880;0")]
    [InlineData("10;20;470;-1;0")]
    [InlineData("NaN;20;470;880;0")]
    [InlineData("10;Infinity;470;880;0")]
    [InlineData("10;20;470;880;2")]
    [InlineData("10;20;470;880;yes")]
    public void WhatIsNotAPlaceIsNotRead(string? text)
    {
        WindowPlacement.TryRead(text, out Rect bounds, out bool maximized).Should().BeFalse();

        bounds.Should().Be(Rect.Empty);
        maximized.Should().BeFalse();
    }

    [Fact]
    public void APlaceOnTheScreenIsKept()
    {
        Rect saved = new(100, 50, 470, 880);

        WindowPlacement.Fit(saved, Screen).Should().Be(saved);
    }

    [Fact]
    public void AWindowLargerThanTheScreensIsMadeToFit()
    {
        WindowPlacement.Fit(new Rect(0, 0, 2500, 1400), Screen).Should().Be(new Rect(0, 0, 1920, 1080));
    }

    [Theory]
    [InlineData(2000, 100)] // on a monitor to the right that is gone
    [InlineData(-600, 100)] // on a monitor to the left that is gone
    [InlineData(1850, 100)] // only 70 of the title bar on the screen
    [InlineData(-400, 100)] // only 70 of the title bar on the screen, on the left
    [InlineData(100, -10)] // the top of the title bar above the screen
    [InlineData(100, 1060)] // the title bar below the bottom of the screen
    public void APlaceNobodyCanReachIsNotUsed(double left, double top)
    {
        WindowPlacement.Fit(new Rect(left, top, 470, 880), Screen).Should().BeNull();
    }

    [Fact]
    public void AWindowWithEnoughOfItsTitleBarOnTheScreenStaysWhereItIs()
    {
        Rect saved = new(1920 - WindowPlacement.Reachable, 1080 - WindowPlacement.TitleBar, 470, 880);

        WindowPlacement.Fit(saved, Screen).Should().Be(new Rect(saved.Left, saved.Top, 470, 880));
    }

    [Fact]
    public void AScreenOnTheLeftOfTheMainOneHasNegativeCoordinates()
    {
        Rect screens = new(-1920, 0, 3840, 1080);
        Rect saved = new(-1500, 100, 470, 880);

        WindowPlacement.Fit(saved, screens).Should().Be(saved);
    }

    [Fact]
    public void NoScreensIsNoPlace()
    {
        WindowPlacement.Fit(new Rect(0, 0, 470, 880), Rect.Empty).Should().BeNull();
        WindowPlacement.Fit(Rect.Empty, Screen).Should().BeNull();
    }

    [Fact]
    public void AWindowGoesBackWhereItWas()
    {
        OnStaThread(() =>
        {
            Rect screens = VirtualScreen();
            FakePreferences preferences = new();
            Rect saved = new(screens.Left + 40, screens.Top + 30, 450, 600);
            preferences.Set(WindowPlacement.Key, WindowPlacement.Write(saved, maximized: true));
            Window window = new();

            WindowPlacement.Restore(window, preferences);

            window.WindowStartupLocation.Should().Be(WindowStartupLocation.Manual);
            new Rect(window.Left, window.Top, window.Width, window.Height).Should().Be(saved);
            window.WindowState.Should().Be(WindowState.Maximized);
        });
    }

    [Fact]
    public void AWindowWithNothingStoredOrNowhereToGoIsLeftAsItIs()
    {
        OnStaThread(() =>
        {
            Rect screens = VirtualScreen();
            FakePreferences gone = new();
            gone.Set(WindowPlacement.Key, WindowPlacement.Write(new Rect(screens.Right + 1000, 0, 450, 600), false));

            foreach (FakePreferences preferences in (FakePreferences[])[new(), gone])
            {
                Window window = new() { WindowStartupLocation = WindowStartupLocation.CenterScreen };

                WindowPlacement.Restore(window, preferences);

                window.WindowStartupLocation.Should().Be(WindowStartupLocation.CenterScreen);
                double.IsNaN(window.Left).Should().BeTrue();
                window.WindowState.Should().Be(WindowState.Normal);
            }
        });
    }

    [Fact]
    public void AWindowThatClosesIsStoredWhereItIs()
    {
        OnStaThread(() =>
        {
            FakePreferences preferences = new();
            Window window = new() { Left = 100, Top = 50, Width = 470, Height = 880 };

            WindowPlacement.Keep(window, preferences);

            preferences.Values[WindowPlacement.Key].Should().Be("100;50;470;880;0");
        });
    }

    [Fact]
    public void AWindowThatWasNeverPlacedStoresNothing()
    {
        OnStaThread(() =>
        {
            FakePreferences preferences = new();
            preferences.Set(WindowPlacement.Key, "100;50;470;880;0");

            // Never shown: no place of its own, and a maximized window's restore bounds are empty until it is.
            WindowPlacement.Keep(new Window(), preferences);
            WindowPlacement.Keep(new Window { WindowState = WindowState.Maximized }, preferences);

            preferences.Values[WindowPlacement.Key].Should().Be("100;50;470;880;0");
        });
    }

    [Fact]
    public void ArgumentsAreRequired()
    {
        OnStaThread(() =>
        {
            Window window = new();
            FakePreferences preferences = new();

            ((Action)(() => WindowPlacement.Restore(null!, preferences))).Should().Throw<ArgumentNullException>()
                .WithParameterName("window");
            ((Action)(() => WindowPlacement.Restore(window, null!))).Should().Throw<ArgumentNullException>()
                .WithParameterName("preferences");
            ((Action)(() => WindowPlacement.Keep(null!, preferences))).Should().Throw<ArgumentNullException>()
                .WithParameterName("window");
            ((Action)(() => WindowPlacement.Keep(window, null!))).Should().Throw<ArgumentNullException>()
                .WithParameterName("preferences");
        });
    }

    private static Rect VirtualScreen() => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    /// <summary>Runs the body on a thread WPF will build windows on, and hands back what it threw.</summary>
    private static void OnStaThread(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                body();
            }
            catch (Exception error)
            {
                failure = ExceptionDispatchInfo.Capture(error);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
