// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Text.RegularExpressions;

namespace Barbatos.Pallas.Architecture.Tests;

/// <summary>
/// The API reference of each package describes its public surface, all of it and nothing else.
/// </summary>
/// <remarks>
/// The reference is written by hand (maintainer, 24 Sep 2026), so nothing keeps it with the code but this: every type
/// and member a library declares in its <c>PublicAPI.Shipped.txt</c> and <c>PublicAPI.Unshipped.txt</c> - which the
/// build keeps with the code - is looked up in the <c>API-REFERENCE.md</c> of the package that carries the library. A
/// type needs its <c>### `Name` Kind</c> section; a property, a field or an enum value its name in backticks there; a
/// method, a constructor, an indexer or an operator its signature with every parameter named, overload by overload. A
/// section for a type that is no longer public fails too. What the compiler gives every record - value equality,
/// <c>ToString</c>, <c>Deconstruct</c> - and parameterless constructors need not be described.
/// </remarks>
public sealed partial class ApiReferenceTests
{
    private static readonly HashSet<string> Undescribed = ["Equals", "GetHashCode", "ToString", "Deconstruct", "<Clone>$", "EqualityContract", "PrintMembers", "operator ==", "operator !="];

    private static readonly string[] Modifiers = ["static ", "override ", "abstract ", "virtual ", "const ", "readonly ", "sealed ", "new "];

    public static TheoryData<string> Packages => [.. ArchitectureMap.PublishedPackages];

    [Theory]
    [MemberData(nameof(Packages))]
    public void EveryDeclaredTypeAndMember_IsInItsPackagesReference(string package)
    {
        Reference reference = Reference.Read(package);
        List<string> missing = [];
        foreach (Declared declared in Declarations(package))
        {
            if (!reference.Sections.TryGetValue(declared.Type, out string? section))
            {
                missing.Add($"{declared.Type}: no section");
                continue;
            }

            if (declared.Member is { } member && !Describes(section, declared.Kind, member, declared.Parameters))
            {
                missing.Add(declared.Kind == DeclaredKind.Callable ? $"{declared.Type}.{member}({string.Join(", ", declared.Parameters)})" : $"{declared.Type}.{member}");
            }
        }

        missing.Should().BeEmpty("{0}/API-REFERENCE.md describes every type and member of the package", package);
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void EveryTypeTheReferenceDescribes_IsDeclared(string package)
    {
        Reference reference = Reference.Read(package);
        HashSet<string> declared = [.. Declarations(package).Select(entry => entry.Type)];

        reference.Sections.Keys.Should().BeSubsetOf(declared, "{0}/API-REFERENCE.md describes no type that is not public", package);
        reference.Duplicates.Should().BeEmpty("a type is described once");
    }

    [Theory]
    [MemberData(nameof(Packages))]
    public void TheReference_ListsEveryNamespaceOfThePackage(string package)
    {
        string text = File.ReadAllText(Reference.PathOf(package));

        foreach (string library in Libraries(package))
        {
            text.Should().Contain($"## `{library}` Namespace", "{0} carries {1}", package, library);
        }
    }

    [Fact]
    public void TheParser_ReadsEveryFormOfEntry()
    {
        // The forms the analyzer writes, one of each, as the reference's check relies on reading them right.
        HashSet<string> types = ["Barbatos.Pallas.Engine.T", "Barbatos.Pallas.Engine.T.Inner", "Barbatos.Pallas.Engine.E"];

        Parse("Barbatos.Pallas.Engine.T", types).Should().Be(new Declared("T", null, DeclaredKind.Type, []));
        Parse("static Barbatos.Pallas.Engine.T.Make(int count, string! name = \"x\") -> Barbatos.Pallas.Engine.T", types)!.Parameters.Should().Equal("count", "name");
        Parse("Barbatos.Pallas.Engine.T.Name.get -> string!", types).Should().Be(new Declared("T", "Name", DeclaredKind.Named, []));
        Parse("Barbatos.Pallas.Engine.T.Inner.Inner(System.Collections.Generic.IReadOnlyList<(int A, int B)>! pairs, out int[]! rest) -> void", types)!
            .Should().Match<Declared>(entry => entry.Type == "T.Inner" && entry.Kind == DeclaredKind.Callable && entry.Member == "Inner");
        Parse("Barbatos.Pallas.Engine.T.this[int row, int column].get -> int", types)!.Parameters.Should().Equal("row", "column");
        Parse("Barbatos.Pallas.Engine.E.Degree = 0 -> Barbatos.Pallas.Engine.E", types).Should().Be(new Declared("E", "Degree", DeclaredKind.Named, []));
        Parse("static Barbatos.Pallas.Engine.T.implicit operator Barbatos.Pallas.Engine.T(int value) -> Barbatos.Pallas.Engine.T", types)!.Member.Should().Be("implicit operator T");
        Parse("~override Barbatos.Pallas.Engine.T.ToString() -> string", types).Should().BeNull("ToString is not described");
        Parse("Barbatos.Pallas.Engine.T.T() -> void", types).Should().BeNull("a parameterless constructor is not described");
        Parse("#nullable enable", types).Should().BeNull();
    }

    private static bool Describes(string section, DeclaredKind kind, string member, string[] parameters)
    {
        if (kind == DeclaredKind.Named)
        {
            return section.Contains($"`{member}`", StringComparison.Ordinal);
        }

        // Each place the member's signature starts; one of them names this overload's parameters, in order.
        string start = "`" + (member == "this" ? "this[" : member + "(");
        for (int at = section.IndexOf(start, StringComparison.Ordinal); at >= 0; at = section.IndexOf(start, at + 1, StringComparison.Ordinal))
        {
            int open = at + start.Length;
            int close = section.IndexOf('`', open);
            if (close > open && Names(section[open..close].TrimEnd(')', ']')).SequenceEqual(parameters))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> Libraries(string package) => [package, .. PackagingRulesTests.Carried(package)];

    private static List<Declared> Declarations(string package)
    {
        List<Declared> declared = [];
        foreach (string library in Libraries(package))
        {
            string directory = Path.GetDirectoryName(RepositoryLayout.ProjectFile(library))!;
            string[] lines = [.. File.ReadLines(Path.Combine(directory, "PublicAPI.Shipped.txt")), .. File.ReadLines(Path.Combine(directory, "PublicAPI.Unshipped.txt"))];
            HashSet<string> types = [.. lines.Select(Unmodified).Where(line => line.Length > 0 && !line.StartsWith('#') && !line.Contains(" -> ", StringComparison.Ordinal))];
            declared.AddRange(lines.Select(line => Parse(line, types)).OfType<Declared>().Select(entry => entry with { Type = entry.Type }));
        }

        return declared;
    }

    /// <summary>Reads one line of an API file: a type, or a member of one; <see langword="null"/> for what is not described.</summary>
    private static Declared? Parse(string line, HashSet<string> types)
    {
        string text = Unmodified(line);
        if (text.Length == 0 || text.StartsWith('#'))
        {
            return null;
        }

        int arrow = text.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrow < 0)
        {
            return new Declared(Simple(text), null, DeclaredKind.Type, []);
        }

        string body = text[..arrow];
        int equals = body.IndexOf(" = ", StringComparison.Ordinal);
        if (equals >= 0 && !body.Contains('(', StringComparison.Ordinal))
        {
            body = body[..equals];
        }

        string[] parameters = [];
        DeclaredKind kind = DeclaredKind.Named;
        int indexer = body.IndexOf(".this[", StringComparison.Ordinal);
        int paren = body.IndexOf('(', StringComparison.Ordinal);
        if (indexer >= 0)
        {
            parameters = Names(body[(indexer + 6)..body.LastIndexOf(']')]);
            body = body[..indexer] + ".this";
            kind = DeclaredKind.Callable;
        }
        else if (paren >= 0)
        {
            parameters = Names(body[(paren + 1)..body.LastIndexOf(')')]);
            body = body[..paren];
            kind = DeclaredKind.Callable;
        }
        else
        {
            body = Accessor().Replace(body, "");
        }

        string type = types.Where(candidate => body.StartsWith(candidate + ".", StringComparison.Ordinal)).MaxBy(candidate => candidate.Length)
            ?? throw new InvalidOperationException("No declared type holds " + line);
        string member = Qualifier().Replace(body[(type.Length + 1)..], "");
        bool parameterless = kind == DeclaredKind.Callable && parameters.Length == 0 && member == Simple(type).Split('.')[^1];
        return Undescribed.Contains(member) || parameterless ? null : new Declared(Simple(type), member, kind, parameters);
    }

    private static string Unmodified(string line)
    {
        string text = line.Trim().TrimStart('~');
        for (string? modifier = Modifiers.FirstOrDefault(text.StartsWith); modifier is not null; modifier = Modifiers.FirstOrDefault(text.StartsWith))
        {
            text = text[modifier.Length..];
        }

        return text;
    }

    // A type is named in the reference as C# code names it in its namespace: Value, not Barbatos.Pallas.Engine.Value.
    private static string Simple(string type) => Qualifier().Replace(type, "");

    /// <summary>The names of a parameter list, as written in an API file or in the reference.</summary>
    private static string[] Names(string list)
    {
        List<string> names = [];
        int depth = 0;
        int start = 0;
        for (int index = 0; index <= list.Length; index++)
        {
            char c = index < list.Length ? list[index] : ',';
            depth += c is '<' or '(' or '[' ? 1 : c is '>' or ')' or ']' ? -1 : 0;
            if (c == ',' && depth == 0)
            {
                string parameter = list[start..index];
                int equals = parameter.IndexOf('=', StringComparison.Ordinal);
                parameter = (equals >= 0 ? parameter[..equals] : parameter).Trim();
                if (parameter.Length > 0)
                {
                    names.Add(Identifier().Match(parameter).Value);
                }

                start = index + 1;
            }
        }

        return [.. names];
    }

    [GeneratedRegex(@"\.(get|set|init)$")]
    private static partial Regex Accessor();

    [GeneratedRegex(@"\b(?:Barbatos\.Pallas\.[A-Za-z]+\.|System\.(?:[A-Za-z]+\.)*)")]
    private static partial Regex Qualifier();

    [GeneratedRegex(@"@?[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex Identifier();

    private enum DeclaredKind
    {
        Type,
        Named,
        Callable,
    }

    private sealed record Declared(string Type, string? Member, DeclaredKind Kind, string[] Parameters)
    {
        public bool Equals(Declared? other) =>
            other is not null && Type == other.Type && Member == other.Member && Kind == other.Kind && Parameters.SequenceEqual(other.Parameters);

        public override int GetHashCode() => HashCode.Combine(Type, Member, Kind);
    }

    /// <summary>A package's API reference, cut into the section of each type it describes.</summary>
    private sealed partial class Reference
    {
        private Reference(Dictionary<string, string> sections, List<string> duplicates)
        {
            Sections = sections;
            Duplicates = duplicates;
        }

        public Dictionary<string, string> Sections { get; }

        public List<string> Duplicates { get; }

        public static string PathOf(string package) =>
            Path.Combine(Path.GetDirectoryName(RepositoryLayout.ProjectFile(package))!, "API-REFERENCE.md");

        public static Reference Read(string package)
        {
            Dictionary<string, string> sections = [];
            List<string> duplicates = [];
            string? current = null;
            List<string> lines = [];
            foreach (string line in File.ReadLines(PathOf(package)).Append("## end"))
            {
                if (line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("### ", StringComparison.Ordinal))
                {
                    if (current is not null && !sections.TryAdd(current, string.Join('\n', lines)))
                    {
                        duplicates.Add(current);
                    }

                    Match heading = TypeHeading().Match(line);
                    current = heading.Success ? heading.Groups["name"].Value : null;
                    lines.Clear();
                    continue;
                }

                lines.Add(line);
            }

            return new Reference(sections, duplicates);
        }

        [GeneratedRegex(@"^### `(?<name>[^`]+)` (Class|Struct|Interface|Enum|Delegate)( \(record\))?$")]
        private static partial Regex TypeHeading();
    }
}
