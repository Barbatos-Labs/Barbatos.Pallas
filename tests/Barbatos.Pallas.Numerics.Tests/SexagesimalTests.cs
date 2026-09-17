// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Numerics.Tests;

public sealed class SexagesimalTests
{
    [Fact]
    public void ToDegrees_OfTheManualsExamples()
    {
        // Manual p. 49: 1°15′0″ = 1.25, and 2°20′30″ + 0°9′30″ = 2.5.
        Sexagesimal.ToDegrees(1, 15, 0).Should().Be(1.25m);
        (Sexagesimal.ToDegrees(2, 20, 30) + Sexagesimal.ToDegrees(0, 9, 30)).Should().Be(2.5m);
    }

    [Fact]
    public void FromDegrees_SplitsAndCarriesRoundedSeconds()
    {
        Sexagesimal.FromDegrees(1.25m, 0, MidpointRounding.AwayFromZero).Should().Be((false, 1m, 15m, 0m));
        Sexagesimal.FromDegrees(-12.5125m, 0, MidpointRounding.AwayFromZero).Should().Be((true, 12m, 30m, 45m));
        Sexagesimal.FromDegrees(0m, 0, MidpointRounding.AwayFromZero).Should().Be((false, 0m, 0m, 0m));

        // 59.99…″ rounds to 60″, which must carry: 2°30′0″, never 2°29′60″.
        Sexagesimal.FromDegrees(2.4999999999999999999999999999m, 2, MidpointRounding.AwayFromZero).Should().Be((false, 2m, 30m, 0m));
        Sexagesimal.FromDegrees(0.5125m / 3m, 2, MidpointRounding.AwayFromZero).Should().Be((false, 0m, 10m, 15m));
    }

    [Fact]
    public void FromDegrees_RejectsSecondsDecimalsOutsideZeroToTwentyEight()
    {
        Action negative = () => Sexagesimal.FromDegrees(1m, -1, MidpointRounding.AwayFromZero);
        Action tooMany = () => Sexagesimal.FromDegrees(1m, 29, MidpointRounding.AwayFromZero);

        // Math.Round would reject both as well, but naming its own "decimals" parameter.
        negative.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("secondsDecimals");
        tooMany.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("secondsDecimals");
    }
}
