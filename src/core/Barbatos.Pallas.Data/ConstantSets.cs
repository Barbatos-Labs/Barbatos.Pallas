// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Data;

/// <summary>
/// The sets of scientific constants Pallas ships.
/// </summary>
public static class ConstantSets
{
    /// <summary>
    /// The 47 constants of the reference calculator's CATALOG (manual pp. 64-65), with the values of the 2022 CODATA adjustment.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transcribed from the complete listing of the 2022 CODATA adjustment, https://physics.nist.gov/cuu/Constants/Table/allascii.txt,
    /// retrieved 18 Sep 2026. The calculator ships CODATA 2018; Pallas ships the newest set (deviation D2).
    /// </para>
    /// <para>
    /// A value is a <see cref="ScaledDecimal"/> so that every published digit survives without <see cref="double"/>
    /// (docs/PRECISION.md §7): the Planck constant is 6.62607015 × 10⁻³⁴, which <see cref="decimal"/> alone would round to 0.
    /// A zero uncertainty means the value is exact by definition in the 2019 SI. The names are the Canonical Linear Syntax
    /// spellings of docs/LINEAR-SYNTAX.md §2; <c>@t</c>, which the manual lists without a value, is the zero of the Celsius
    /// scale, 273.15 K.
    /// </para>
    /// </remarks>
    public static ConstantSet Codata2022 { get; } = new(
        "CODATA 2022",
        [
        new ScientificConstant("@h", new(6.62607015m, -34), default, "J Hz^-1"),   // Planck constant
        new ScientificConstant("@ħ", new(1.054571817m, -34), default, "J s"),   // reduced Planck constant
        new ScientificConstant("@c", new(299792458m, 0), default, "m s^-1"),   // speed of light in vacuum
        new ScientificConstant("@ε_0", new(8.8541878188m, -12), new(0.0000000014m, -12), "F m^-1"),   // vacuum electric permittivity
        new ScientificConstant("@μ_0", new(1.25663706127m, -6), new(0.00000000020m, -6), "N A^-2"),   // vacuum mag. permeability
        new ScientificConstant("@Z_0", new(376.730313412m, 0), new(0.000000059m, 0), "ohm"),   // characteristic impedance of vacuum
        new ScientificConstant("@G", new(6.67430m, -11), new(0.00015m, -11), "m^3 kg^-1 s^-2"),   // Newtonian constant of gravitation
        new ScientificConstant("@l_P", new(1.616255m, -35), new(0.000018m, -35), "m"),   // Planck length
        new ScientificConstant("@t_P", new(5.391247m, -44), new(0.000060m, -44), "s"),   // Planck time
        new ScientificConstant("@μ_N", new(5.0507837393m, -27), new(0.0000000016m, -27), "J T^-1"),   // nuclear magneton
        new ScientificConstant("@μ_B", new(9.2740100657m, -24), new(0.0000000029m, -24), "J T^-1"),   // Bohr magneton
        new ScientificConstant("@e", new(1.602176634m, -19), default, "C"),   // elementary charge
        new ScientificConstant("@Φ_0", new(2.067833848m, -15), default, "Wb"),   // mag. flux quantum
        new ScientificConstant("@G_0", new(7.748091729m, -5), default, "S"),   // conductance quantum
        new ScientificConstant("@K_J", new(483597.8484m, 9), default, "Hz V^-1"),   // Josephson constant
        new ScientificConstant("@R_K", new(25812.80745m, 0), default, "ohm"),   // von Klitzing constant
        new ScientificConstant("@m_p", new(1.67262192595m, -27), new(0.00000000052m, -27), "kg"),   // proton mass
        new ScientificConstant("@m_n", new(1.67492750056m, -27), new(0.00000000085m, -27), "kg"),   // neutron mass
        new ScientificConstant("@m_e", new(9.1093837139m, -31), new(0.0000000028m, -31), "kg"),   // electron mass
        new ScientificConstant("@m_μ", new(1.883531627m, -28), new(0.000000042m, -28), "kg"),   // muon mass
        new ScientificConstant("@a_0", new(5.29177210544m, -11), new(0.00000000082m, -11), "m"),   // Bohr radius
        new ScientificConstant("@α", new(7.2973525643m, -3), new(0.0000000011m, -3), ""),   // fine-structure constant
        new ScientificConstant("@r_e", new(2.8179403205m, -15), new(0.0000000013m, -15), "m"),   // classical electron radius
        new ScientificConstant("@λ_C", new(2.42631023538m, -12), new(0.00000000076m, -12), "m"),   // Compton wavelength
        new ScientificConstant("@γ_p", new(2.6752218708m, 8), new(0.0000000011m, 8), "s^-1 T^-1"),   // proton gyromag. ratio
        new ScientificConstant("@λ_Cp", new(1.32140985360m, -15), new(0.00000000041m, -15), "m"),   // proton Compton wavelength
        new ScientificConstant("@λ_Cn", new(1.31959090382m, -15), new(0.00000000067m, -15), "m"),   // neutron Compton wavelength
        new ScientificConstant("@R_∞", new(10973731.568157m, 0), new(0.000012m, 0), "m^-1"),   // Rydberg constant
        new ScientificConstant("@μ_p", new(1.41060679545m, -26), new(0.00000000060m, -26), "J T^-1"),   // proton mag. mom.
        new ScientificConstant("@μ_e", new(-9.2847646917m, -24), new(0.0000000029m, -24), "J T^-1"),   // electron mag. mom.
        new ScientificConstant("@μ_n", new(-9.6623653m, -27), new(0.0000023m, -27), "J T^-1"),   // neutron mag. mom.
        new ScientificConstant("@μ_μ", new(-4.49044830m, -26), new(0.00000010m, -26), "J T^-1"),   // muon mag. mom.
        new ScientificConstant("@m_τ", new(3.16754m, -27), new(0.00021m, -27), "kg"),   // tau mass
        new ScientificConstant("@m_u", new(1.66053906892m, -27), new(0.00000000052m, -27), "kg"),   // atomic mass constant
        new ScientificConstant("@F", new(96485.33212m, 0), default, "C mol^-1"),   // Faraday constant
        new ScientificConstant("@N_A", new(6.02214076m, 23), default, "mol^-1"),   // Avogadro constant
        new ScientificConstant("@k", new(1.380649m, -23), default, "J K^-1"),   // Boltzmann constant
        new ScientificConstant("@V_m", new(22.41396954m, -3), default, "m^3 mol^-1"),   // molar volume of ideal gas (273.15 K, 101.325 kPa)
        new ScientificConstant("@R", new(8.314462618m, 0), default, "J mol^-1 K^-1"),   // molar gas constant
        new ScientificConstant("@c_1", new(3.741771852m, -16), default, "W m^2"),   // first radiation constant
        new ScientificConstant("@c_2", new(1.438776877m, -2), default, "m K"),   // second radiation constant
        new ScientificConstant("@σ", new(5.670374419m, -8), default, "W m^-2 K^-4"),   // Stefan-Boltzmann constant
        new ScientificConstant("@g_n", new(9.80665m, 0), default, "m s^-2"),   // standard acceleration of gravity
        new ScientificConstant("@atm", new(101325m, 0), default, "Pa"),   // standard atmosphere
        new ScientificConstant("@R_K-90", new(25812.807m, 0), default, "ohm"),   // conventional value of von Klitzing constant
        new ScientificConstant("@K_J-90", new(483597.9m, 9), default, "Hz V^-1"),   // conventional value of Josephson constant
        new ScientificConstant("@t", new(273.15m, 0), default, "K"),   // zero of the Celsius scale
        ]);
}
