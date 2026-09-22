// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;

namespace Barbatos.Pallas.Engine;

/// <summary>
/// An immutable vector of real values, as the Vector application holds in VctA-VctD and VctAns (manual pp. 139-145).
/// </summary>
/// <remarks>
/// Each element is a <see cref="Value"/>: the length of (3, 4) is exactly 5, and that of (1, 1) is √2 with its exact form.
/// The dimension limits belong to the profile and are checked where vectors enter a session.
/// </remarks>
public sealed class VectorValue : IEquatable<VectorValue>
{
    private readonly ImmutableArray<Value> _elements;

    /// <summary>Initializes a vector.</summary>
    /// <param name="elements">The elements; real numbers only.</param>
    /// <exception cref="ArgumentException">The vector is empty, or an element is not a real number.</exception>
    public VectorValue(params ReadOnlySpan<Value> elements)
    {
        if (elements.IsEmpty)
        {
            throw new ArgumentException("A vector has at least one element.", nameof(elements));
        }

        foreach (Value element in elements)
        {
            if (!element.IsReal)
            {
                throw new ArgumentException("A vector holds real numbers only.", nameof(elements));
            }
        }

        _elements = [.. elements];
    }

    private VectorValue(ImmutableArray<Value> elements)
    {
        _elements = elements;
    }

    /// <summary>Gets the number of elements.</summary>
    public int Dimension => _elements.Length;

    /// <summary>Gets the elements.</summary>
    public ImmutableArray<Value> Elements => _elements;

    /// <summary>Gets an element.</summary>
    /// <param name="index">The index, from 0.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the vector.</exception>
    public Value this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Dimension);
            return _elements[index];
        }
    }

    /// <inheritdoc/>
    public bool Equals(VectorValue? other) => other is not null && _elements.SequenceEqual(other._elements);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as VectorValue);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        foreach (Value element in _elements)
        {
            hash.Add(element);
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns the elements in the invariant culture, for diagnostics.</summary>
    /// <returns>The text, as <c>[1, 2]</c>.</returns>
    public override string ToString() => "[" + string.Join(", ", _elements.Select(element => element.ToString())) + "]";

    /// <summary>Creates a vector from elements already known to be real, without copying.</summary>
    internal static VectorValue FromElements(ImmutableArray<Value> elements) => new(elements);
}
