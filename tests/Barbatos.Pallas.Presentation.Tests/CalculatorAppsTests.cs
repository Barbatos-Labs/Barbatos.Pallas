// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The application registry: the home screen, the router table and the application menu all read it, so it has to
/// agree with the engine about which applications exist and which of them this build can open.
/// </summary>
public sealed class CalculatorAppsTests
{
    [Fact]
    public void EveryApplicationOfTheEngineIsListedOnce()
    {
        IEnumerable<CalculatorApp> listed = CalculatorApps.All.Select(app => app.App);

        listed.Should().BeEquivalentTo(Enum.GetValues<CalculatorApp>(), "the home screen shows the calculator's applications, all of them");
        CalculatorApps.All.Select(app => app.App).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void TheHomeScreenStartsWithCalculate()
    {
        CalculatorApps.All[0].App.Should().Be(CalculatorApp.Calculate, "the calculator opens on Calculate");
        CalculatorApps.All[0].Route.Should().Be("/calculate");
        CalculatorApps.All[0].NameKey.Should().Be("app.calculate", "the name is localized in the app, never in the core");
    }

    [Fact]
    public void EveryRouteAndNameKeyIsItsOwn()
    {
        CalculatorApps.All.Select(app => app.Route).Should().OnlyHaveUniqueItems();
        CalculatorApps.All.Select(app => app.NameKey).Should().OnlyHaveUniqueItems();
        CalculatorApps.All.Should().OnlyContain(app => app.Route.StartsWith('/') && app.NameKey.StartsWith("app.", StringComparison.Ordinal));
    }

    [Fact]
    public void WhatThisBuildCanOpenIsWhatTheEngineCanOpen()
    {
        CalculatorSession session = Shell.Session();

        foreach (CalculatorAppInfo app in CalculatorApps.All)
        {
            bool opens = true;
            try
            {
                session.SwitchApp(app.App);
            }
            catch (NotSupportedException)
            {
                opens = false;
            }

            app.IsAvailable.Should().Be(opens, "the home screen disables '{0}' if and only if the engine refuses it", app.Route);
        }
    }

    [Fact]
    public void MathBoxIsListedAndDisabledUntilPhaseSix()
    {
        CalculatorAppInfo mathBox = CalculatorApps.Of(CalculatorApp.MathBox);

        mathBox.IsAvailable.Should().BeFalse();
        CalculatorApps.Available.Should().NotContain(mathBox).And.HaveCount(CalculatorApps.All.Length - 1);
        CalculatorApps.Available.Should().OnlyContain(app => app.IsAvailable);
    }

    [Theory]
    [InlineData("/statistics", CalculatorApp.Statistics)]
    [InlineData("statistics", CalculatorApp.Statistics)]
    [InlineData("/STATISTICS", CalculatorApp.Statistics)]
    [InlineData("/base-n", CalculatorApp.BaseN)]
    [InlineData("/math-box", CalculatorApp.MathBox)]
    public void ARouteFindsItsApplication(string route, CalculatorApp expected)
    {
        CalculatorApps.ByRoute(route).Should().Be(CalculatorApps.Of(expected));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/graphing")]
    [InlineData("/calculate/2")]
    public void ARouteNothingAnswersToIsNull(string? route)
    {
        CalculatorApps.ByRoute(route).Should().BeNull();
    }

    [Fact]
    public void AnApplicationTheEnumHasNotIsRefused()
    {
        Action act = () => CalculatorApps.Of((CalculatorApp)(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
