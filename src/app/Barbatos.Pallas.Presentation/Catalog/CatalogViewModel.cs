// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The CATALOG as a screen shows it: a group to look in, a search across all of them, and what choosing does.
/// </summary>
/// <remarks>
/// A search looks in every group at once, because the user who types <c>sinh</c> should not have to know it is under
/// Hyperbolic/Trig; with nothing typed, the entries are those of the group that is chosen.
/// </remarks>
public sealed partial class CatalogViewModel : ObservableObject
{
    private readonly SyntaxVocabulary _vocabulary;

    /// <summary>Creates the CATALOG over what the engine reads.</summary>
    /// <param name="vocabulary">The vocabulary.</param>
    /// <param name="app">The application whose CATALOG it is.</param>
    /// <exception cref="ArgumentNullException"><paramref name="vocabulary"/> is <see langword="null"/>.</exception>
    public CatalogViewModel(SyntaxVocabulary vocabulary, CalculatorApp app = CalculatorApp.Calculate)
    {
        ArgumentNullException.ThrowIfNull(vocabulary);
        _vocabulary = vocabulary;
        _app = app;
        _sections = CalculatorCatalog.For(vocabulary, app);

        // Every application reads some name the CATALOG lists, so there is always a first group to open on.
        _section = _sections[0];
    }

    /// <summary>Raised when an entry is chosen, with what it puts on the line.</summary>
    public event EventHandler<KeyAction>? Chosen;

    /// <summary>Gets or sets whose CATALOG it is.</summary>
    [ObservableProperty]
    private CalculatorApp _app;

    /// <summary>Gets the groups, in the calculator's order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Items))]
    private ImmutableArray<CatalogSection> _sections;

    /// <summary>Gets or sets the group whose entries are shown while nothing is searched for.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Items))]
    private CatalogSection? _section;

    /// <summary>Gets or sets what is searched for; empty shows the chosen group.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Items))]
    private string _search = string.Empty;

    /// <summary>Gets the entries shown: those of the group, or those of every group that match the search.</summary>
    public ImmutableArray<CatalogItem> Items
    {
        get
        {
            string search = Search.Trim();
            if (search.Length == 0)
            {
                return Section?.Items ?? [];
            }

            return
            [
                .. Sections.SelectMany(section => section.Items)
                    .Where(item => item.Label.Contains(search, StringComparison.OrdinalIgnoreCase)
                        || item.Text.Contains(search, StringComparison.OrdinalIgnoreCase)),
            ];
        }
    }

    /// <summary>Chooses an entry, which puts it on the line.</summary>
    /// <param name="item">The entry; <see langword="null"/> does nothing.</param>
    [RelayCommand]
    public void Choose(CatalogItem? item)
    {
        if (item is null)
        {
            return;
        }

        Chosen?.Invoke(this, item.Action);
    }

    partial void OnAppChanged(CalculatorApp value)
    {
        Sections = CalculatorCatalog.For(_vocabulary, value);
        Section = Sections[0];
        Search = string.Empty;
    }
}
