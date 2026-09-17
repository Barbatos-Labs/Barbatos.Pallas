// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Diagnostics.CodeAnalysis;

namespace Barbatos.Pallas.Expressions;

/// <summary>
/// The outcome of parsing: a syntax tree, or the first error.
/// </summary>
public sealed class ParseResult
{
    internal ParseResult(string text, SyntaxNode? root, SyntaxDiagnostic? diagnostic)
    {
        Text = text;
        Root = root;
        Diagnostic = diagnostic;
    }

    /// <summary>Gets the text that was parsed, after Unicode normalization; spans index into it.</summary>
    public string Text { get; }

    /// <summary>Gets the syntax tree, or <see langword="null"/> when parsing failed.</summary>
    public SyntaxNode? Root { get; }

    /// <summary>Gets the first error, or <see langword="null"/> when parsing succeeded.</summary>
    public SyntaxDiagnostic? Diagnostic { get; }

    /// <summary>Gets a value indicating whether <see cref="Root"/> holds a tree.</summary>
    [MemberNotNullWhen(true, nameof(Root))]
    public bool Succeeded => Root is not null;
}
