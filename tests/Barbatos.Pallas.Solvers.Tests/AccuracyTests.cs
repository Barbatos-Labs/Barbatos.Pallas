// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Globalization;
using System.Numerics;
using PeterO.Numbers;

namespace Barbatos.Pallas.Solvers.Tests;

/// <summary>
/// The accuracy of the iterated roots, against 50-digit references from PeterO.Numbers, a library that shares no code
/// with Pallas (the exit criterion of Phase 4 in docs/ARCHITECTURE.md §11).
/// </summary>
/// <remarks>
/// Every reference here is built from the closed form of the root - a square root, a cube root, a cosine - or by
/// bisecting the polynomial at 50 digits, never from the iteration being measured. The polynomials are those a
/// calculator solves: degree 2 to 4, with roots of moderate size and none of them repeated, where a root is
/// well conditioned and the accuracy of <see cref="double"/> is what the iteration can reach.
/// </remarks>
public sealed class AccuracyTests
{
    private static readonly EContext Wide = EContext.ForPrecision(50);

    /// <summary>The relative error a simple, well-conditioned root of these degrees must stay inside.</summary>
    private const double Tolerance = 1e-14;

    [Fact]
    public void TheRootsOfAQuadratic()
    {
        // x² − 2: ±√2, whose reference is PeterO's square root.
        EDecimal root = EDecimal.FromInt32(2).Sqrt(Wide);

        Errors([-2d, 0d, 1d], [root, root.Negate()]).Should().OnlyContain(error => error < Tolerance);
    }

    [Fact]
    public void TheRootsOfAGoldenQuadratic()
    {
        // x² − x − 1: (1 ± √5)/2.
        EDecimal five = EDecimal.FromInt32(5).Sqrt(Wide);
        EDecimal half = EDecimal.FromInt32(2);

        Errors([-1d, -1d, 1d], [five.Add(EDecimal.One).Divide(half, Wide), EDecimal.One.Subtract(five).Divide(half, Wide)])
            .Should().OnlyContain(error => error < Tolerance);
    }

    [Fact]
    public void TheRealRootOfACubic()
    {
        // x³ − 2: the real root is 2^(1/3), and the other two are that times (−1 ± √3 i)/2.
        EDecimal cube = EDecimal.FromInt32(2).Pow(EDecimal.One.Divide(EDecimal.FromInt32(3), Wide), Wide);

        Errors([-2d, 0d, 0d, 1d], [cube]).Should().OnlyContain(error => error < Tolerance);
    }

    [Fact]
    public void TheRootsOfAQuarticOfSurds()
    {
        // x⁴ − 10x² + 1 = (x² − 2·√2·x − 1)(x² + 2·√2·x − 1)… its roots are ±√2 ± √3.
        EDecimal two = EDecimal.FromInt32(2).Sqrt(Wide);
        EDecimal three = EDecimal.FromInt32(3).Sqrt(Wide);
        EDecimal[] roots =
        [
            two.Add(three),
            two.Subtract(three),
            three.Subtract(two),
            two.Add(three).Negate(),
        ];

        Errors([1d, 0d, -10d, 0d, 1d], roots).Should().OnlyContain(error => error < Tolerance);
    }

    [Fact]
    public void TheRootsOfANestedQuartic()
    {
        // x⁴ − 4x² + 2: ±√(2 ± √2), the cosines of the eighth roots of unity doubled.
        EDecimal two = EDecimal.FromInt32(2);
        EDecimal outer = two.Add(two.Sqrt(Wide)).Sqrt(Wide);
        EDecimal inner = two.Subtract(two.Sqrt(Wide)).Sqrt(Wide);

        Errors([2d, 0d, -4d, 0d, 1d], [outer, inner, inner.Negate(), outer.Negate()])
            .Should().OnlyContain(error => error < Tolerance);
    }

    [Fact]
    public void TheRootOfACubicWithNoClosedFormHere()
    {
        // x³ − x − 1, the plastic number: no surd is written for it here, so the reference is 50-digit bisection.
        BigInteger[] polynomial = [-1, -1, 0, 1];
        EDecimal root = Bisect(polynomial, EDecimal.One, EDecimal.FromInt32(2));

        Errors([-1d, -1d, 0d, 1d], [root]).Should().OnlyContain(error => error < Tolerance);
        root.ToString().Should().StartWith("1.3247179572447460259609088544780973407", "the plastic number, whose last digits bisection leaves to the rounding of 50");
    }

    [Fact]
    public void TheImaginaryPartsOfAConjugatePair()
    {
        // x³ − 2: the complex roots are 2^(1/3)·(−1 ± √3 i)/2, so their imaginary parts are ±2^(1/3)·√3/2.
        EDecimal cube = EDecimal.FromInt32(2).Pow(EDecimal.One.Divide(EDecimal.FromInt32(3), Wide), Wide);
        EDecimal imaginary = cube.Multiply(EDecimal.FromInt32(3).Sqrt(Wide)).Divide(EDecimal.FromInt32(2), Wide);

        double[] parts = [.. PolynomialRoots.Find([-2d, 0d, 0d, 1d]).Select(root => root.Imaginary).Where(part => Math.Abs(part) > 0.1d).Order()];

        parts.Should().HaveCount(2);
        Error(parts[1], imaginary).Should().BeLessThan(Tolerance);
        Error(parts[0], imaginary.Negate()).Should().BeLessThan(Tolerance);
    }

    [Fact]
    public void EveryRootOfAPolynomialIsMatchedByAReference()
    {
        // A root that no reference matches would pass the tests above by looking at the wrong root of the pair.
        Complex[] roots = PolynomialRoots.Find([-2d, 0d, 1d]);

        roots.Should().HaveCount(2);
        roots.Select(root => root.Real).Order().Should().BeInAscendingOrder();
        Errors([-2d, 0d, 1d], [EDecimal.FromInt32(2).Sqrt(Wide)]).Should().ContainSingle();
    }

    /// <summary>The relative error of the iterated root nearest each reference.</summary>
    private static double[] Errors(double[] coefficients, EDecimal[] references)
    {
        Complex[] roots = PolynomialRoots.Find(coefficients);
        return [.. references.Select(reference => Error(Nearest(roots, reference), reference))];
    }

    /// <summary>The real part of the root nearest a real reference, which is the root the reference stands for.</summary>
    private static double Nearest(Complex[] roots, EDecimal reference)
    {
        double value = reference.ToDouble();
        return roots.OrderBy(root => Complex.Abs(root - value)).First().Real;
    }

    private static double Error(double value, EDecimal reference)
    {
        EDecimal difference = ERational.FromDouble(value).ToEDecimal(Wide).Subtract(reference).Abs();
        return difference.Divide(reference.Abs(), Wide).ToDouble();
    }

    /// <summary>The root of an integer polynomial between two points of opposite sign, to 50 digits.</summary>
    private static EDecimal Bisect(BigInteger[] coefficients, EDecimal low, EDecimal high)
    {
        int sign = SignAt(coefficients, low);
        for (int step = 0; step < 200; step++)
        {
            EDecimal middle = low.Add(high).Divide(EDecimal.FromInt32(2), Wide);
            if (SignAt(coefficients, middle) == sign)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return low.Add(high).Divide(EDecimal.FromInt32(2), Wide);
    }

    private static int SignAt(BigInteger[] coefficients, EDecimal point)
    {
        EDecimal value = EDecimal.Zero;
        for (int i = coefficients.Length - 1; i >= 0; i--)
        {
            value = value.Multiply(point, Wide).Add(EDecimal.FromString(coefficients[i].ToString(CultureInfo.InvariantCulture)));
        }

        return value.Sign;
    }
}
