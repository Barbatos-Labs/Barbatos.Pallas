// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Data;

/// <summary>
/// The sets of unit conversions Pallas ships.
/// </summary>
public static class UnitSets
{
    // Definitions used below, each exact: 1 in = 2.54 cm, 1 lb = 0.45359237 kg, g_n = 9.80665 m/s².
    private const decimal PoundForce = 4.4482216152605m;        // 0.45359237 × 9.80665 N
    private const decimal Horsepower = 745.69987158227022m;     // 550 × 0.3048 × PoundForce W

    /// <summary>
    /// The 40 unit conversion commands of the reference calculator's CATALOG (manual p. 66), with the exact definitions of
    /// NIST Special Publication 811 (decision of 17 Sep 2026, deviation D4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each conversion carries a multiplier and a divisor rather than one factor, so an exact definition stays exact in
    /// <see cref="decimal"/>: <c>5cm▶in</c> is 5 ÷ 2.54, which the calculator displays as 1.968503937 and Pallas can also
    /// show as 250⌟127.
    /// </para>
    /// <para>
    /// Two factors are not exact decimals, and are rounded to the digits their definitions give: the parsec, defined as
    /// 648000/π astronomical units, and the 15 °C calorie, an empirical value (4.1855 J).
    /// </para>
    /// </remarks>
    public static UnitSet NistSp811 { get; } = new(
        "NIST SP 811",
        [
            // Length
            new UnitConversion("in▶cm", 2.54m),
            new UnitConversion("cm▶in", 1m, 2.54m),
            new UnitConversion("ft▶m", 0.3048m),
            new UnitConversion("m▶ft", 1m, 0.3048m),
            new UnitConversion("yd▶m", 0.9144m),
            new UnitConversion("m▶yd", 1m, 0.9144m),
            new UnitConversion("mile▶km", 1.609344m),
            new UnitConversion("km▶mile", 1m, 1.609344m),
            new UnitConversion("n mile▶m", 1852m),
            new UnitConversion("m▶n mile", 1m, 1852m),
            new UnitConversion("pc▶km", 30856775814913.673m),
            new UnitConversion("km▶pc", 1m, 30856775814913.673m),

            // Area: 1 acre = 4840 yd²
            new UnitConversion("acre▶m²", 4046.8564224m),
            new UnitConversion("m²▶acre", 1m, 4046.8564224m),

            // Volume
            new UnitConversion("gal(US)▶L", 3.785411784m),
            new UnitConversion("L▶gal(US)", 1m, 3.785411784m),
            new UnitConversion("gal(UK)▶L", 4.54609m),
            new UnitConversion("L▶gal(UK)", 1m, 4.54609m),

            // Mass: 1 oz = 1/16 lb
            new UnitConversion("oz▶g", 28.349523125m),
            new UnitConversion("g▶oz", 1m, 28.349523125m),
            new UnitConversion("lb▶kg", 0.45359237m),
            new UnitConversion("kg▶lb", 1m, 0.45359237m),

            // Velocity
            new UnitConversion("km/h▶m/s", 1m, 3.6m),
            new UnitConversion("m/s▶km/h", 3.6m),

            // Pressure: 1 mmHg = 13.5951 g/cm³ × g_n; 1 kgf/cm² = 9.80665 N/cm²; 1 lbf/in² = PoundForce / 0.00064516 m²
            new UnitConversion("atm▶Pa", 101325m),
            new UnitConversion("Pa▶atm", 1m, 101325m),
            new UnitConversion("mmHg▶Pa", 133.322387415m),
            new UnitConversion("Pa▶mmHg", 1m, 133.322387415m),
            new UnitConversion("kgf/cm²▶Pa", 98066.5m),
            new UnitConversion("Pa▶kgf/cm²", 1m, 98066.5m),
            new UnitConversion("lbf/in²▶kPa", PoundForce, 0.64516m),
            new UnitConversion("kPa▶lbf/in²", 0.64516m, PoundForce),

            // Energy: 1 kgf·m = 9.80665 J; the 15 °C calorie is 4.1855 J
            new UnitConversion("kgf·m▶J", 9.80665m),
            new UnitConversion("J▶kgf·m", 1m, 9.80665m),
            new UnitConversion("J▶cal₁₅", 1m, 4.1855m),
            new UnitConversion("cal₁₅▶J", 4.1855m),

            // Power: 1 hp = 550 ft·lbf/s
            new UnitConversion("hp▶kW", Horsepower, 1000m),
            new UnitConversion("kW▶hp", 1000m, Horsepower),

            // Temperature
            new UnitConversion("°F▶°C", 5m, 9m, offsetBefore: -32m),
            new UnitConversion("°C▶°F", 9m, 5m, offsetAfter: 32m),
        ]);
}
