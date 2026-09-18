// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Engine;

namespace Barbatos.Pallas.Data;

/// <summary>
/// The atomic weight tables Pallas ships.
/// </summary>
public static class AtomicWeightTables
{
    /// <summary>
    /// The standard atomic weights of the 118 elements, as <c>AtWt(</c> returns them (manual p. 67).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Transcribed from CIAAW, https://ciaaw.org/atomic-weights.htm and https://ciaaw.org/abridged-atomic-weights.htm,
    /// retrieved 18 Sep 2026. The calculator ships the IUPAC 2019 values; Pallas ships the newest (deviation D3).
    /// </para>
    /// <para>
    /// An element whose standard atomic weight is an interval, such as hydrogen's [1.00784, 1.00811], carries its
    /// abridged value, the single number chemistry calculates with. An element without a standard atomic weight carries
    /// the mass number of its longest-lived isotope (https://ciaaw.org/radioactive-elements.htm), which the periodic
    /// table shows in brackets.
    /// </para>
    /// </remarks>
    public static AtomicWeightTable Ciaaw { get; } = new(
        "CIAAW 2021",
        [
        new AtomicWeight(1, "H", 1.0080m, false),
        new AtomicWeight(2, "He", 4.002602m, false),
        new AtomicWeight(3, "Li", 6.94m, false),
        new AtomicWeight(4, "Be", 9.0121831m, false),
        new AtomicWeight(5, "B", 10.81m, false),
        new AtomicWeight(6, "C", 12.011m, false),
        new AtomicWeight(7, "N", 14.007m, false),
        new AtomicWeight(8, "O", 15.999m, false),
        new AtomicWeight(9, "F", 18.998403162m, false),
        new AtomicWeight(10, "Ne", 20.1797m, false),
        new AtomicWeight(11, "Na", 22.98976928m, false),
        new AtomicWeight(12, "Mg", 24.305m, false),
        new AtomicWeight(13, "Al", 26.9815384m, false),
        new AtomicWeight(14, "Si", 28.085m, false),
        new AtomicWeight(15, "P", 30.973761998m, false),
        new AtomicWeight(16, "S", 32.06m, false),
        new AtomicWeight(17, "Cl", 35.45m, false),
        new AtomicWeight(18, "Ar", 39.95m, false),
        new AtomicWeight(19, "K", 39.0983m, false),
        new AtomicWeight(20, "Ca", 40.078m, false),
        new AtomicWeight(21, "Sc", 44.955907m, false),
        new AtomicWeight(22, "Ti", 47.867m, false),
        new AtomicWeight(23, "V", 50.9415m, false),
        new AtomicWeight(24, "Cr", 51.9961m, false),
        new AtomicWeight(25, "Mn", 54.938043m, false),
        new AtomicWeight(26, "Fe", 55.845m, false),
        new AtomicWeight(27, "Co", 58.933194m, false),
        new AtomicWeight(28, "Ni", 58.6934m, false),
        new AtomicWeight(29, "Cu", 63.546m, false),
        new AtomicWeight(30, "Zn", 65.38m, false),
        new AtomicWeight(31, "Ga", 69.723m, false),
        new AtomicWeight(32, "Ge", 72.630m, false),
        new AtomicWeight(33, "As", 74.921595m, false),
        new AtomicWeight(34, "Se", 78.971m, false),
        new AtomicWeight(35, "Br", 79.904m, false),
        new AtomicWeight(36, "Kr", 83.798m, false),
        new AtomicWeight(37, "Rb", 85.4678m, false),
        new AtomicWeight(38, "Sr", 87.62m, false),
        new AtomicWeight(39, "Y", 88.905838m, false),
        new AtomicWeight(40, "Zr", 91.222m, false),
        new AtomicWeight(41, "Nb", 92.90637m, false),
        new AtomicWeight(42, "Mo", 95.95m, false),
        new AtomicWeight(43, "Tc", 97m, true),
        new AtomicWeight(44, "Ru", 101.07m, false),
        new AtomicWeight(45, "Rh", 102.90549m, false),
        new AtomicWeight(46, "Pd", 106.42m, false),
        new AtomicWeight(47, "Ag", 107.8682m, false),
        new AtomicWeight(48, "Cd", 112.414m, false),
        new AtomicWeight(49, "In", 114.818m, false),
        new AtomicWeight(50, "Sn", 118.710m, false),
        new AtomicWeight(51, "Sb", 121.760m, false),
        new AtomicWeight(52, "Te", 127.60m, false),
        new AtomicWeight(53, "I", 126.90447m, false),
        new AtomicWeight(54, "Xe", 131.293m, false),
        new AtomicWeight(55, "Cs", 132.90545196m, false),
        new AtomicWeight(56, "Ba", 137.327m, false),
        new AtomicWeight(57, "La", 138.90547m, false),
        new AtomicWeight(58, "Ce", 140.116m, false),
        new AtomicWeight(59, "Pr", 140.90766m, false),
        new AtomicWeight(60, "Nd", 144.242m, false),
        new AtomicWeight(61, "Pm", 145m, true),
        new AtomicWeight(62, "Sm", 150.36m, false),
        new AtomicWeight(63, "Eu", 151.964m, false),
        new AtomicWeight(64, "Gd", 157.249m, false),
        new AtomicWeight(65, "Tb", 158.925354m, false),
        new AtomicWeight(66, "Dy", 162.500m, false),
        new AtomicWeight(67, "Ho", 164.930329m, false),
        new AtomicWeight(68, "Er", 167.259m, false),
        new AtomicWeight(69, "Tm", 168.934219m, false),
        new AtomicWeight(70, "Yb", 173.045m, false),
        new AtomicWeight(71, "Lu", 174.96669m, false),
        new AtomicWeight(72, "Hf", 178.486m, false),
        new AtomicWeight(73, "Ta", 180.94788m, false),
        new AtomicWeight(74, "W", 183.84m, false),
        new AtomicWeight(75, "Re", 186.207m, false),
        new AtomicWeight(76, "Os", 190.23m, false),
        new AtomicWeight(77, "Ir", 192.217m, false),
        new AtomicWeight(78, "Pt", 195.084m, false),
        new AtomicWeight(79, "Au", 196.966570m, false),
        new AtomicWeight(80, "Hg", 200.592m, false),
        new AtomicWeight(81, "Tl", 204.38m, false),
        new AtomicWeight(82, "Pb", 207.2m, false),
        new AtomicWeight(83, "Bi", 208.98040m, false),
        new AtomicWeight(84, "Po", 209m, true),
        new AtomicWeight(85, "At", 210m, true),
        new AtomicWeight(86, "Rn", 222m, true),
        new AtomicWeight(87, "Fr", 223m, true),
        new AtomicWeight(88, "Ra", 226m, true),
        new AtomicWeight(89, "Ac", 227m, true),
        new AtomicWeight(90, "Th", 232.0377m, false),
        new AtomicWeight(91, "Pa", 231.03588m, false),
        new AtomicWeight(92, "U", 238.02891m, false),
        new AtomicWeight(93, "Np", 237m, true),
        new AtomicWeight(94, "Pu", 244m, true),
        new AtomicWeight(95, "Am", 243m, true),
        new AtomicWeight(96, "Cm", 247m, true),
        new AtomicWeight(97, "Bk", 247m, true),
        new AtomicWeight(98, "Cf", 251m, true),
        new AtomicWeight(99, "Es", 252m, true),
        new AtomicWeight(100, "Fm", 257m, true),
        new AtomicWeight(101, "Md", 258m, true),
        new AtomicWeight(102, "No", 259m, true),
        new AtomicWeight(103, "Lr", 262m, true),
        new AtomicWeight(104, "Rf", 267m, true),
        new AtomicWeight(105, "Db", 268m, true),
        new AtomicWeight(106, "Sg", 269m, true),
        new AtomicWeight(107, "Bh", 270m, true),
        new AtomicWeight(108, "Hs", 269m, true),
        new AtomicWeight(109, "Mt", 277m, true),
        new AtomicWeight(110, "Ds", 281m, true),
        new AtomicWeight(111, "Rg", 282m, true),
        new AtomicWeight(112, "Cn", 285m, true),
        new AtomicWeight(113, "Nh", 285m, true),
        new AtomicWeight(114, "Fl", 289m, true),
        new AtomicWeight(115, "Mc", 288m, true),
        new AtomicWeight(116, "Lv", 291m, true),
        new AtomicWeight(117, "Ts", 294m, true),
        new AtomicWeight(118, "Og", 294m, true),
        ]);
}
