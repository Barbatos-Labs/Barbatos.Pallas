// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Graphing.Tests;

/// <summary>
/// The region of the plane a graph shows.
/// </summary>
public sealed class GraphViewportTests
{
    [Fact]
    public void AViewportHasItsBoundsAndItsSize()
    {
        GraphViewport view = new(-2d, 6d, -1d, 3d);

        (view.Left, view.Right, view.Bottom, view.Top).Should().Be((-2d, 6d, -1d, 3d));
        (view.Width, view.Height).Should().Be((8d, 4d));
        view.Should().Be(new GraphViewport(-2d, 6d, -1d, 3d));
    }

    [Theory]
    [InlineData(double.NaN, 1d, 0d, 1d, "left")]
    [InlineData(0d, double.PositiveInfinity, 0d, 1d, "right")]
    [InlineData(0d, 1d, double.NegativeInfinity, 1d, "bottom")]
    [InlineData(0d, 1d, 0d, double.NaN, "top")]
    [InlineData(1d, 1d, 0d, 1d, "right")]
    [InlineData(2d, 1d, 0d, 1d, "right")]
    [InlineData(0d, 1d, 1d, 1d, "top")]
    [InlineData(0d, 1d, 1d, 0d, "top")]
    [InlineData(-1.7e308, 1.7e308, 0d, 1d, "right")]
    [InlineData(0d, 1d, -1.7e308, 1.7e308, "top")]
    public void AViewportIsAFiniteRegionThatIsNotEmpty(double left, double right, double bottom, double top, string parameter)
    {
        ((Action)(() => _ = new GraphViewport(left, right, bottom, top))).Should().Throw<ArgumentOutOfRangeException>().WithParameterName(parameter);
    }
}
