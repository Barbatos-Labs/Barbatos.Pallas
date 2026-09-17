// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using Barbatos.Pallas.Architecture.Tests.FloatingPoint.Fixtures;

namespace Barbatos.Pallas.Architecture.Tests.FloatingPoint;

/// <summary>
/// Tests the test: the scanner is run against deliberately bad fixtures before anyone trusts it to call the
/// core assemblies clean.
/// </summary>
public sealed class FloatingPointScannerTests
{
    private static readonly Lazy<IReadOnlyList<FloatingPointUsage>> ThisAssembly =
        new(() => FloatingPointScanner.Scan(typeof(FloatingPointScannerTests).Assembly.Location));

    public static TheoryData<string, string> Fixtures => new()
    {
        { typeof(DoubleField).FullName!, "Field" },
        { typeof(DoubleParameter).FullName!, "Parameter" },
        { typeof(SingleReturn).FullName!, "ReturnType" },
        { typeof(DoubleLocal).FullName!, "Opcode" },
        { typeof(BoxedLiteral).FullName!, "Opcode" },
        { typeof(MathSqrtCall).FullName!, "MemberReference" },
        { typeof(MathFCall).FullName!, "MemberReference" },
        { typeof(ComplexLocal).FullName!, "Local" },
        { typeof(NullableDoubleField).FullName!, "Field" },
        { typeof(GenericOverDouble).FullName!, "MemberReference" },
        { typeof(BigIntegerToDouble).FullName!, "MemberReference" },
    };

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Scanner_FindsFloatingPointUsage(string fixtureType, string expectedKind)
    {
        IEnumerable<FloatingPointUsage> found = ThisAssembly.Value.Where(usage => BelongsTo(usage, fixtureType));

        found.Should().Contain(usage => usage.Kind == expectedKind,
            "the scanner must report a {0} usage in {1}; found: {2}", expectedKind, fixtureType, string.Join("; ", found));
    }

    [Fact]
    public void Scanner_DoesNotReportDecimalOrIntegerArithmetic()
    {
        string fixtureType = typeof(ExactArithmetic).FullName!;

        ThisAssembly.Value.Where(usage => BelongsTo(usage, fixtureType)).Should().BeEmpty(
            "decimal, BigInteger and the decimal and integer overloads of System.Math are exact and must stay usable");
    }

    private static bool BelongsTo(FloatingPointUsage usage, string typeName)
    {
        return usage.Location == typeName
            || usage.Location.StartsWith(typeName + ".", StringComparison.Ordinal)
            || usage.Location.StartsWith(typeName + "::", StringComparison.Ordinal)
            || usage.Location.StartsWith(typeName + "+", StringComparison.Ordinal);
    }
}
