// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Engine;

/// <summary>The statistic variables of the Statistics application (manual pp. 90-91), calculated from its data.</summary>
internal enum Statistic
{
    Count,
    SumX,
    SumY,
    SumX2,
    SumY2,
    SumXY,
    SumX3,
    SumX2Y,
    SumX4,
    MeanX,
    MeanY,
    PopulationVarianceX,
    PopulationVarianceY,
    PopulationDeviationX,
    PopulationDeviationY,
    SampleVarianceX,
    SampleVarianceY,
    SampleDeviationX,
    SampleDeviationY,
    MinX,
    MaxX,
    MinY,
    MaxY,
    FirstQuartile,
    Median,
    ThirdQuartile,

    /// <summary>The regression coefficient a.</summary>
    A,

    /// <summary>The regression coefficient b.</summary>
    B,

    /// <summary>The regression coefficient c of the quadratic regression.</summary>
    C,

    /// <summary>The correlation coefficient r of the regressions other than the quadratic.</summary>
    R,
}
