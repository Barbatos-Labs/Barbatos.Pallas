# Barbatos.Pallas.Expressions

Lexer, Pratt parser and abstract syntax tree for calculator expressions, following the operator precedence of a reference scientific calculator, with implicit multiplication, precise error spans, and linear and LaTeX printers.

> **Status: 1.0.** The API below is tested on .NET 8, 9 and 10, and it follows semantic versioning: an incompatible
> change waits for 2.0.
>
> **Not a package of its own.** It ships inside [Barbatos.Pallas.Engine](https://www.nuget.org/packages/Barbatos.Pallas.Engine),
> which carries its assembly: reference that package to use it.

Expressions are written in Canonical Linear Syntax, the calculator's LineI notation as plain Unicode text
([docs/LINEAR-SYNTAX.md](https://github.com/Barbatos-Labs/Barbatos.Pallas/blob/main/docs/LINEAR-SYNTAX.md)). The
library reads and writes expressions; it does not evaluate them, and it keeps numbers as the text that was typed.

## Parse

```csharp
using Barbatos.Pallas.Expressions;

ParseResult result = ExpressionParser.Parse("6÷2(1+2)", SyntaxContext.Calculate);

if (result.Succeeded)
{
    SyntaxNode tree = result.Root;             // 6 ÷ (2 × (1+2)): an omitted × binds tighter than ÷ (manual p. 29)
}
else
{
    SyntaxDiagnostic error = result.Diagnostic!.Value;
    // error.Code is a SyntaxErrorCode; error.Span is where the calculator would put the cursor.
}
```

Parsing never throws for bad input. It stops at the first error:

```csharp
ExpressionParser.Parse("2+", SyntaxContext.Calculate).Diagnostic;
// SyntaxDiagnostic { Code = MissingOperand, Span = SourceSpan { Start = 2, Length = 0, End = 2 } }
```

## The application decides what text means

```csharp
ExpressionParser.Parse("1F+b101", new SyntaxContext(CalculatorApp.BaseN));          // hexadecimal 1F plus binary 101
ExpressionParser.Parse("Sum(A1:B3)", new SyntaxContext(CalculatorApp.Spreadsheet)); // a cell range
ExpressionParser.Parse("2∠45", new SyntaxContext(CalculatorApp.Complex));           // polar form

// Relations are read only when Verify is on (manual p. 73).
ExpressionParser.Parse("1≤1<1+1", new SyntaxContext(CalculatorApp.Calculate, AllowRelations: true));
```

## Print

```csharp
SyntaxNode tree = ExpressionParser.Parse("sqrt(2)*pi", SyntaxContext.Calculate).Root!;

LinearPrinter.Print(tree, SyntaxContext.Calculate);   // "√(2)×π" - canonical spellings; parses back to the same tree
LatexPrinter.Print(tree);                              // @"\sqrt{2}\times \pi "
```

Compare trees by structure with `SyntaxEquivalence.AreEquivalent`, which looks through parentheses and ignores spans.

An editor that draws a line a symbol at a time, before it is a tree, draws a name the way a tree draws it:

```csharp
SyntaxSymbol avogadro = SyntaxVocabulary.Standard.Symbols.First(symbol => symbol.Text == "@N_A");

LatexPrinter.Print(avogadro);                          // "N_{A}" - as in a printed calculation
```

## Add names

A plugin function needs its name in the vocabulary, so that `beam(` is read as one token:

```csharp
SyntaxSymbol beam = SyntaxSymbol.CreateName("beam(", SymbolKind.Function);
SyntaxVocabulary vocabulary = SyntaxVocabulary.Standard.With(beam);

ExpressionParser.Parse("2beam(3,4)", SyntaxContext.Calculate, vocabulary);
```

## Guarantees

- Lexing allocates nothing.
- Every printed tree parses back to an equivalent tree, checked with generated trees in seven application contexts.
- Arbitrary text never makes the parser throw or hang; nesting deeper than 128 levels is reported as a Stack ERROR.
- The library has no dependencies and uses no binary floating point.

Part of [Barbatos.Pallas](https://github.com/Barbatos-Labs/Barbatos.Pallas), a precise scientific calculation
engine for .NET 8, 9 and 10 built on decimal, double and BigInteger.
