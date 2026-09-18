// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Data.Tests;

/// <summary>
/// The NIST SP 811 conversions: the exact definitions, and a round trip for every command.
/// </summary>
public sealed class UnitSetTests
{
    private static readonly UnitSet Units = UnitSets.NistSp811;

    private static readonly PallasEngine Engine = PallasEngineBuilder.CreateDefault().AddUnitSet(Units).Build();

    public static TheoryData<string> Commands => [.. Units.Conversions.Select(conversion => conversion.Command)];

    [Fact]
    public void TheSetCoversTheCalculatorsCatalogExactly()
    {
        string[] vocabulary =
        [
            .. SyntaxVocabulary.Standard.Symbols
                .Where(symbol => symbol.Kind == SymbolKind.UnitConversion && ReferenceEquals(symbol, symbol.Canonical))
                .Select(symbol => symbol.Text)
                .Order(StringComparer.Ordinal),
        ];

        Units.Conversions.Select(conversion => conversion.Command).Order(StringComparer.Ordinal).Should().Equal(vocabulary);
        Units.Conversions.Should().HaveCount(40);
    }

    [Theory]
    [MemberData(nameof(Commands))]
    public void EveryCommand_ConvertsAndConvertsBack(string command)
    {
        // The two directions of a pair are inverses: converting 7 and back must return 7.
        string reverse = command.Split('▶') is [string from, string to] ? to + "▶" + from : throw new InvalidOperationException(command);
        CalculatorSession session = Engine.CreateSession(profile: CalculatorProfile.Extended);

        Calculation forward = session.Calculate("7" + command);
        forward.Error.Should().BeNull("'{0}' should convert", command);

        Calculation back = session.Calculate("Ans" + reverse);
        back.Error.Should().BeNull("'{0}' should convert back", reverse);
        back.Result.ToDouble().Should().BeApproximately(7d, 1e-12);
    }

    [Theory]
    // The definitions that are exact: the result is a decimal quotient, not a float.
    [InlineData("1in▶cm", "2.54")]
    [InlineData("5cm▶in", "1.968503937")]
    [InlineData("1ft▶m", "0.3048")]
    [InlineData("1mile▶km", "1.609344")]
    [InlineData("1lb▶kg", "0.45359237")]
    [InlineData("1oz▶g", "28.34952313")]
    [InlineData("1acre▶m²", "4046.856422")]
    [InlineData("1gal(US)▶L", "3.785411784")]
    [InlineData("1gal(UK)▶L", "4.54609")]
    [InlineData("1atm▶Pa", "101325")]
    [InlineData("1kgf·m▶J", "9.80665")]
    [InlineData("1n mile▶m", "1852")]
    [InlineData("36km/h▶m/s", "10")]
    [InlineData("1hp▶kW", "0.7456998716")]
    [InlineData("1mmHg▶Pa", "133.3223874")]
    [InlineData("1lbf/in²▶kPa", "6.894757293")]
    public void ExactDefinitions_GiveExactResults(string input, string expected)
    {
        // In LineI/LineO the calculator shows the decimal rather than the fraction the exact quotient also is (p. 66).
        Decimals().Calculate(input).Display.Text.Should().Be(expected);
    }

    [Theory]
    [InlineData("32°F▶°C", "0")]
    [InlineData("212°F▶°C", "100")]
    [InlineData("-40°F▶°C", "-40")]
    [InlineData("100°C▶°F", "212")]
    [InlineData("37°C▶°F", "98.6")]
    public void Temperatures_ConvertWithTheirOffset(string input, string expected)
    {
        Decimals().Calculate(input).Display.Text.Should().Be(expected);
    }

    private static CalculatorSession Decimals()
    {
        CalculatorSession session = Engine.CreateSession();
        session.Settings = session.Settings with { InputOutput = InputOutput.LineILineO };
        return session;
    }

    [Fact]
    public void ExactConversions_StayExactValues()
    {
        // 5 ÷ 2.54 is a decimal quotient, so the engine can still show it as the fraction 250⌟127.
        CalculatorSession session = Engine.CreateSession();

        session.Calculate("5cm▶in").Result.IsExact.Should().BeTrue();
        session.Format(session.Calculate("5cm▶in"), FormatTarget.ImproperFraction)!.Text.Should().Be("250⌟127");
    }

    [Fact]
    public void AConversionValidatesItsArguments()
    {
        Action badName = () => _ = new UnitConversion("cm to in", 1m).Command;
        Action zeroDivisor = () => _ = new UnitConversion("cm▶in", 1m, 0m).Command;

        badName.Should().Throw<ArgumentException>();
        zeroDivisor.Should().Throw<ArgumentOutOfRangeException>();
    }
}
