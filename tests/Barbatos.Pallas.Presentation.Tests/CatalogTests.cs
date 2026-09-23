// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The CATALOG (manual pp. 51-69): what each application's holds, where the manual puts it, and what choosing does.
/// </summary>
public sealed class CatalogTests
{
    private static readonly SyntaxVocabulary Vocabulary = Shell.Session().Engine.Vocabulary;

    public static TheoryData<CalculatorApp> Applications =>
    [
        .. Enum.GetValues<CalculatorApp>().Where(app => app != CalculatorApp.MathBox),
    ];

    [Theory]
    [MemberData(nameof(Applications))]
    public void EveryEntryOfAnApplicationsCatalogTypesWhatThatApplicationReads(CalculatorApp app)
    {
        List<string> unread = [];
        foreach (CatalogItem item in CalculatorCatalog.For(Vocabulary, app).SelectMany(section => section.Items))
        {
            string text = MathLinearWriter.Write(InputCommandRouter.Apply(MathDocument.Empty, item.Action).Document);
            if (NoToken(text, app) && NoToken(text + "1", app))
            {
                unread.Add($"{item.Label} types '{text}'");
            }
        }

        unread.Should().BeEmpty("the CATALOG of {0} lists only what {0} reads", app);
    }

    [Theory]
    [MemberData(nameof(Applications))]
    public void EveryGroupHasSomethingInItAndEveryEntryIsListedOnce(CalculatorApp app)
    {
        ImmutableArray<CatalogSection> sections = CalculatorCatalog.For(Vocabulary, app);

        sections.Should().NotBeEmpty();
        sections.Should().AllSatisfy(section => section.Items.Should().NotBeEmpty("an empty group is not shown"));
        sections.Select(section => section.Group).Should().BeInAscendingOrder("the groups are in the calculator's order");
        sections.SelectMany(section => section.Items).Select(item => item.Text).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void TheManualsGroupsHoldWhatTheManualPutsInThem()
    {
        ImmutableArray<CatalogSection> sections = CalculatorCatalog.For(Vocabulary, CalculatorApp.Calculate);

        Texts(sections, CatalogGroup.Trigonometry).Should().Contain(["sin(", "cosh(", "tan⁻¹("]);
        Texts(sections, CatalogGroup.FunctionAnalysis).Should().Contain(["d/dx(", "∫(", "Σ(", "÷R"]);
        Texts(sections, CatalogGroup.Probability).Should().Contain(["!", "P", "C", "Ran#", "RanInt#("]);
        Texts(sections, CatalogGroup.NumericCalc).Should().Contain(["GCD(", "LCM(", "Rnd(", "Intg("]);
        Texts(sections, CatalogGroup.AngleCoordinates).Should().Contain(["°", "Pol(", "Rec("]);
        Texts(sections, CatalogGroup.AtomicTable).Should().Equal("AtWt(");
        Texts(sections, CatalogGroup.ScientificConstants).Should().Contain(["@h", "@c", "@N_A"]);
        Texts(sections, CatalogGroup.UnitConversions).Should().Contain("cm▶in");
        Texts(sections, CatalogGroup.EngineerSymbols).Should().Contain(["_k", "_M"]);
        Texts(sections, CatalogGroup.Relations).Should().Contain(["=", "≠", "≤"]);
        sections.Should().NotContain(section => section.Group == CatalogGroup.Application, "Calculate has no names only it has");
        sections.Single(section => section.Group == CatalogGroup.Trigonometry).NameKey.Should().Be("catalog.Trigonometry");
    }

    [Fact]
    public void ANewNameIsPlacedInAGroupRatherThanLeftInOther()
    {
        // Other holds what the manual itself puts there (p. 69). A function added to the vocabulary without a place
        // in the CATALOG's table lands here and fails this, rather than sitting in the wrong group unnoticed.
        ImmutableArray<CatalogSection> sections = CalculatorCatalog.For(Vocabulary, CalculatorApp.Calculate);

        Texts(sections, CatalogGroup.Other).Should().BeSubsetOf(
            ["π", "e", "√(", "ˣ√", "root(", "^", "⌟", "²", "³", "⁻¹", "f(", "g("]);
    }

    [Fact]
    public void TheListShowsWhatTheCalculatorsMenuShows()
    {
        ImmutableArray<CatalogItem> items = [.. CalculatorCatalog.For(Vocabulary, CalculatorApp.Calculate).SelectMany(section => section.Items)];

        items.Single(item => item.Text == "C").Label.Should().Be("nCr");
        items.Single(item => item.Text == "P").Label.Should().Be("nPr");
        items.Single(item => item.Text == "@h").Label.Should().Be("h", "the @ is how the line tells a constant from a variable, not its name");
        items.Single(item => item.Text == "_k").Label.Should().Be("k");
        items.Single(item => item.Text == "sin(").Label.Should().Be("sin(");
    }

    [Fact]
    public void BaseNsCatalogHasOnlyWhatBaseNHas()
    {
        ImmutableArray<CatalogSection> sections = CalculatorCatalog.For(Vocabulary, CalculatorApp.BaseN);

        sections[0].Group.Should().Be(CatalogGroup.Application);
        sections[0].NameKey.Should().Be("app.base-n", "the application's own group is named after it");
        Texts(sections, CatalogGroup.Application).Should().Contain(["and", "or", "xor", "xnor", "Not(", "Neg(", "d", "h", "b", "o"]);
        sections.Select(section => section.Group).Should().NotContain(
            [CatalogGroup.Trigonometry, CatalogGroup.ScientificConstants, CatalogGroup.UnitConversions, CatalogGroup.EngineerSymbols],
            "the CATALOG commands of pp. 51-69 are not in Base-N (p. 51)");
        sections.SelectMany(section => section.Items).Should().NotContain(item => item.Text == "C", "C is a digit in Base-N");
    }

    [Theory]
    [InlineData(CalculatorApp.Statistics, "x̄", "σx", "Σx²", "Q1", "▶t", "P(")]
    [InlineData(CalculatorApp.Complex, "i", "∠", "Conjg(", "ReP(")]
    [InlineData(CalculatorApp.Matrix, "MatA", "MatAns", "Det(", "Trn(")]
    [InlineData(CalculatorApp.Vector, "VctA", "•", "UnitV(")]
    [InlineData(CalculatorApp.Spreadsheet, "Sum(", "Mean(")]
    public void AnApplicationsOwnNamesComeFirst(CalculatorApp app, params string[] names)
    {
        ImmutableArray<CatalogSection> sections = CalculatorCatalog.For(Vocabulary, app);

        sections[0].Group.Should().Be(CatalogGroup.Application);
        sections[0].Items.Select(item => item.Text).Should().Contain(names);
    }

    [Theory]
    [InlineData("∫(", MathTemplateKind.Integral)]
    [InlineData("√(", MathTemplateKind.SquareRoot)]
    [InlineData("Abs(", MathTemplateKind.Abs)]
    [InlineData("d/dx(", MathTemplateKind.Derivative)]
    [InlineData("Σ(", MathTemplateKind.Sum)]
    [InlineData("Π(", MathTemplateKind.Product)]
    [InlineData("ˣ√", MathTemplateKind.Root)]
    [InlineData("^", MathTemplateKind.Power)]
    [InlineData("⌟", MathTemplateKind.Fraction)]
    public void ANameTheLineHasAStructureForPutsTheStructureThere(string text, MathTemplateKind kind)
    {
        CalculatorCatalog.ActionOf(text).Should().Be(new InsertTemplate(kind), "it is edited as if it had been typed on its key");
    }

    [Fact]
    public void AnyOtherNamePutsItselfThere()
    {
        CalculatorCatalog.ActionOf("sinh(").Should().Be(new InsertSymbol("sinh("));
        CalculatorCatalog.ActionOf("@h").Should().Be(new InsertSymbol("@h"));
    }

    [Fact]
    public void ASearchLooksInEveryGroupAtOnce()
    {
        CatalogViewModel catalog = new(Vocabulary);
        catalog.Items.Should().Equal(catalog.Sections[0].Items, "with nothing searched for, the chosen group is shown");

        catalog.Search = "sinh";

        catalog.Items.Select(item => item.Text).Should().BeEquivalentTo(["sinh(", "sinh⁻¹("]);

        catalog.Search = "  ";

        catalog.Items.Should().Equal(catalog.Section!.Items, "a search of nothing but spaces is no search");
    }

    [Fact]
    public void ASearchMatchesWhatTheListShowsAndWhatTheLineSpells()
    {
        CatalogViewModel catalog = new(Vocabulary) { Search = "ncr" };

        catalog.Items.Select(item => item.Label).Should().Contain("nCr", "the label, whatever case it is typed in");

        catalog.Search = "@N_A";

        catalog.Items.Select(item => item.Text).Should().Contain("@N_A", "the spelling on the line");
    }

    [Fact]
    public void ChoosingAGroupShowsItsEntries()
    {
        CatalogViewModel catalog = new(Vocabulary);
        List<string?> changed = catalog.Changes();
        CatalogSection trigonometry = catalog.Sections.Single(section => section.Group == CatalogGroup.Trigonometry);

        catalog.Section = trigonometry;

        catalog.Items.Should().Equal(trigonometry.Items);
        changed.Should().Contain(nameof(CatalogViewModel.Items));
    }

    [Fact]
    public void TheCatalogFollowsItsApplication()
    {
        CatalogViewModel catalog = new(Vocabulary) { Search = "sin" };

        catalog.App = CalculatorApp.Statistics;

        catalog.Sections[0].Group.Should().Be(CatalogGroup.Application);
        catalog.Section.Should().Be(catalog.Sections[0]);
        catalog.Search.Should().BeEmpty("another application's CATALOG is opened afresh");
    }

    [Fact]
    public void ChoosingNothingDoesNothing()
    {
        CatalogViewModel catalog = new(Vocabulary);
        List<KeyAction> chosen = [];
        catalog.Chosen += (_, action) => chosen.Add(action);

        catalog.Choose(null);

        chosen.Should().BeEmpty();
    }

    [Fact]
    public void TheCatalogKeyOpensItAndAnEntryGoesOnTheLine()
    {
        CalculateViewModel screen = new(Shell.Session());

        screen.Input.Press(KeyId.Catalog);
        screen.Menu.Should().Be(CalculatorMenu.Catalog);

        screen.Catalog.Choose(screen.Catalog.Sections.SelectMany(section => section.Items).First(item => item.Text == "sinh("));

        screen.Input.Linear.Should().Be("sinh(");
        screen.Menu.Should().Be(CalculatorMenu.None, "a chosen entry closes the menu, as on the calculator");
        screen.Input.CanUndo.Should().BeTrue("what the CATALOG typed is undone as a key is");
    }

    [Fact]
    public void AStructureFromTheCatalogIsEditedAsIfItsKeyHadBeenPressed()
    {
        CalculateViewModel screen = new(Shell.Session());

        screen.Catalog.Choose(screen.Catalog.Sections.SelectMany(section => section.Items).First(item => item.Text == "∫("));

        screen.Input.Document.Root[0].Should().BeOfType<MathStructure>().Which.Kind.Should().Be(MathTemplateKind.Integral);
    }

    [Fact]
    public void AMenuClosesWithoutChoosingAndWhenTheApplicationChanges()
    {
        CalculateViewModel screen = new(Shell.Session());
        screen.Input.Press(KeyId.Recall);
        screen.Menu.Should().Be(CalculatorMenu.Recall);

        screen.CloseMenu();

        screen.Menu.Should().Be(CalculatorMenu.None);
        screen.Input.IsEmpty.Should().BeTrue();

        screen.Input.Press(KeyId.Catalog);
        screen.Refresh();

        screen.Menu.Should().Be(CalculatorMenu.None, "a menu belongs to the application it was opened in");
    }

    [Fact]
    public void FormatShowsTheAnswerTheWayItIsChosen()
    {
        // 1 ÷ 3 is 1⌟3; FORMAT, Decimal is its decimal (p. 42).
        CalculateViewModel screen = new(Shell.Session());
        Type(screen, KeyId.One, KeyId.Divide, KeyId.Three, KeyId.Execute);

        screen.Input.Press(KeyId.Shift);
        screen.Input.Press(KeyId.SwapForm);
        screen.Menu.Should().Be(CalculatorMenu.Format, "FORMAT is S⇔D after Shift");
        screen.Formats.Should().Contain([FormatTarget.DecimalValue, FormatTarget.PrimeFactor, FormatTarget.Sexagesimal]);

        screen.ChooseFormat(FormatTarget.DecimalValue);

        screen.Display!.Text.Should().Be("0.3333333333");
        screen.Menu.Should().Be(CalculatorMenu.None);
    }

    [Fact]
    public void RclListsTheVariablesAndWhatTheyHold()
    {
        CalculatorSession session = Shell.Session();
        session.SetVariable(MemoryVariable.B, Value.FromDecimal(7));
        CalculateViewModel screen = new(session);

        screen.Input.Press(KeyId.Recall);

        screen.Variables.Select(line => line.Name).Should().Equal("A", "B", "C", "D", "E", "F", "x", "y", "z");
        screen.Variables.Single(line => line.Name == "B").Text.Should().Be("7");

        screen.ChooseVariable(screen.Variables.Single(line => line.Name == "B"));

        screen.Input.Linear.Should().Be("B");
        screen.Menu.Should().Be(CalculatorMenu.None);
    }

    [Fact]
    public void ChoosingNoVariableDoesNothing()
    {
        CalculateViewModel screen = new(Shell.Session());
        screen.Input.Press(KeyId.Recall);

        screen.ChooseVariable(null);

        screen.Menu.Should().Be(CalculatorMenu.Recall);
        screen.Input.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void InBaseNTheVariablesAreXYAndZ()
    {
        CalculateViewModel screen = new(Shell.Session(CalculatorApp.BaseN));

        screen.Variables.Select(line => line.Name).Should().Equal("x", "y", "z");
    }

    [Fact]
    public void StoPutsTheAnswerInTheVariableWhoseKeyComesNext()
    {
        // 5 × 2 = 10, then STO and the key of A (p. 30).
        CalculatorSession session = Shell.Session();
        CalculateViewModel screen = new(session);
        Type(screen, KeyId.Five, KeyId.Multiply, KeyId.Two, KeyId.Execute);
        List<string?> changed = screen.Changes();

        screen.Input.Press(KeyId.Shift);
        screen.Input.Press(KeyId.Recall);
        screen.Input.Mode.Should().Be(KeyMode.Store, "STO waits for the variable");

        screen.Input.Press(KeyId.One);

        session.GetVariable(MemoryVariable.A).Should().Be(Value.FromDecimal(10));
        screen.Notice.Should().Be("Ans→A");
        screen.Input.Mode.Should().Be(KeyMode.Primary);
        screen.Input.Linear.Should().Be("5×2", "the key that named the variable typed nothing");
        changed.Should().Contain(nameof(CalculateViewModel.Variables));
        screen.Variables.Single(line => line.Name == "A").Text.Should().Be("10");
    }

    [Fact]
    public void StoIntoXYAndZ()
    {
        CalculatorSession session = Shell.Session();
        CalculateViewModel screen = new(session);
        Type(screen, KeyId.Four, KeyId.Execute, KeyId.Shift, KeyId.Recall, KeyId.Nine);

        session.GetVariable(MemoryVariable.Z).Should().Be(Value.FromDecimal(4));
        screen.Notice.Should().Be("Ans→z");
    }

    [Fact]
    public void InBaseNStoStoresNothingInADigit()
    {
        CalculatorSession session = Shell.Session(CalculatorApp.BaseN);
        CalculateViewModel screen = new(session);
        Type(screen, KeyId.Five, KeyId.Execute);

        Type(screen, KeyId.Shift, KeyId.Recall, KeyId.One);

        screen.Notice.Should().BeNull("A is a digit in Base-N, not a variable (assumption U12)");
        session.GetVariable(MemoryVariable.A).Should().Be(Value.Zero);

        Type(screen, KeyId.Shift, KeyId.Recall, KeyId.Seven);

        // A Base-N answer is a Base-N value, which is not the decimal 5 even where it shows as 5.
        screen.Variables.Single(line => line.Name == "x").Text.Should().Be("5");
        screen.Notice.Should().Be("Ans→x");
    }

    [Fact]
    public void AKeyWithNoVariableOnItEndsStoWithoutStoringAnything()
    {
        CalculatorSession session = Shell.Session();
        CalculateViewModel screen = new(session);
        Type(screen, KeyId.Four, KeyId.Execute);

        Type(screen, KeyId.Shift, KeyId.Recall, KeyId.Add);

        screen.Notice.Should().BeNull();
        screen.Input.Mode.Should().Be(KeyMode.Primary);
        screen.Input.Linear.Should().Be("4", "+ is not a variable, and after STO it types nothing either");
    }

    [Fact]
    public void TheNoticeOfAStoreGoesWithTheNextCalculation()
    {
        CalculateViewModel screen = new(Shell.Session());
        Type(screen, KeyId.Four, KeyId.Execute, KeyId.Shift, KeyId.Recall, KeyId.One);
        screen.Notice.Should().NotBeNull();

        Type(screen, KeyId.ClearAll, KeyId.Two, KeyId.Execute);

        screen.Notice.Should().BeNull();
    }

    [Fact]
    public void AStoredVariableIsReadBackByTheNextCalculation()
    {
        CalculateViewModel screen = new(Shell.Session());
        Type(screen, KeyId.Four, KeyId.Execute, KeyId.Shift, KeyId.Recall, KeyId.One, KeyId.ClearAll);

        Type(screen, KeyId.Alpha, KeyId.One, KeyId.Multiply, KeyId.Three, KeyId.Execute);

        screen.Display!.Text.Should().Be("12", "A holds the 4 that was stored in it");
    }

    private static IEnumerable<string> Texts(ImmutableArray<CatalogSection> sections, CatalogGroup group) =>
        sections.Single(section => section.Group == group).Items.Select(item => item.Text);

    private static void Type(CalculateViewModel screen, params KeyId[] keys)
    {
        foreach (KeyId key in keys)
        {
            screen.Input.Press(key);
        }
    }

    private static bool NoToken(string text, CalculatorApp app) =>
        ExpressionParser.Parse(text, new SyntaxContext(app, AllowRelations: true)).Diagnostic is { Code: SyntaxErrorCode.UnexpectedCharacter };
}
