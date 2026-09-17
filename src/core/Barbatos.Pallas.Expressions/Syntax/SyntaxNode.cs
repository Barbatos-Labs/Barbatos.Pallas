// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// A node of an immutable syntax tree.
/// </summary>
/// <remarks>
/// The set of node types is closed (the constructor is not accessible outside this assembly), so a <c>switch</c> over
/// them is complete. Nodes have reference identity; compare trees with <see cref="SyntaxEquivalence"/>. Numbers stay
/// text: turning <c>0.1</c> into a <c>decimal</c>, or <c>6.62607015E-34</c> into a <c>double</c>, is the engine's
/// decision (docs/PRECISION.md §3).
/// </remarks>
public abstract class SyntaxNode
{
    private protected SyntaxNode(SourceSpan span)
    {
        Span = span;
    }

    /// <summary>Gets where the node is in the parsed text; <c>default</c> for a node that was not parsed.</summary>
    public SourceSpan Span { get; }

    /// <summary>Returns the node in Canonical Linear Syntax, as read in Calculate.</summary>
    /// <returns>The printed expression.</returns>
    public override string ToString() => LinearPrinter.Print(this, new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true));

    private protected static T NotNull<T>(T? value, string parameterName)
        where T : class
    {
        return value ?? throw new ArgumentNullException(parameterName);
    }
}
