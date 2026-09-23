// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Immutable;
using Barbatos.Pallas.Engine;
using Barbatos.Pallas.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The shell of the calculator: which application is open, the session every application shares, and the session that
/// is kept between runs.
/// </summary>
/// <remarks>
/// One session serves every application, as one calculator does: the memories, the defined functions and the settings
/// are the same wherever the user goes (manual pp. 36-40). Opening an application switches the session to it; opening
/// one this build has no engine for changes nothing.
/// </remarks>
public sealed partial class CalculatorShellViewModel : ObservableObject
{
    private readonly ISessionStore _store;
    private BaseNViewModel? _baseN;
    private MatrixViewModel? _matrix;
    private VectorViewModel? _vector;
    private StatisticsViewModel? _statistics;
    private DistributionViewModel? _distribution;
    private EquationViewModel? _equation;
    private InequalityViewModel? _inequality;
    private RatioViewModel? _ratio;
    private TableViewModel? _table;
    private SpreadsheetViewModel? _spreadsheet;

    /// <summary>Creates the shell over a session and the store its snapshot goes to.</summary>
    /// <param name="session">The session every application shares.</param>
    /// <param name="store">Where the session is kept between runs.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public CalculatorShellViewModel(CalculatorSession session, ISessionStore store)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(store);
        Session = session;
        _store = store;
        _currentApp = CalculatorApps.Of(session.App);
        Settings = new SettingsViewModel(session);
        Calculate = Line(new CalculateViewModel(session));
    }

    /// <summary>Gets the session every application of the shell works on.</summary>
    public CalculatorSession Session { get; }

    /// <summary>Gets the settings of the calculator, as the settings screen shows them.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Gets the Calculate screen, which owns the line the user types on; the Complex screen is the same one.</summary>
    public CalculateViewModel Calculate { get; }

    /// <summary>Gets the Base-N screen.</summary>
    public BaseNViewModel BaseN => _baseN ??= Wire(new BaseNViewModel(Session));

    /// <summary>Gets the Matrix screen.</summary>
    public MatrixViewModel Matrix => _matrix ??= Wire(new MatrixViewModel(Session));

    /// <summary>Gets the Vector screen.</summary>
    public VectorViewModel Vector => _vector ??= Wire(new VectorViewModel(Session));

    /// <summary>Gets the Statistics screen.</summary>
    public StatisticsViewModel Statistics => _statistics ??= Wire(new StatisticsViewModel(Session));

    /// <summary>Gets the Distribution screen.</summary>
    public DistributionViewModel Distribution => _distribution ??= new DistributionViewModel(Session);

    /// <summary>Gets the Equation screen.</summary>
    public EquationViewModel Equation => _equation ??= new EquationViewModel(Session);

    /// <summary>Gets the Inequality screen.</summary>
    public InequalityViewModel Inequality => _inequality ??= new InequalityViewModel(Session);

    /// <summary>Gets the Ratio screen.</summary>
    public RatioViewModel Ratio => _ratio ??= new RatioViewModel(Session);

    /// <summary>Gets the Table screen.</summary>
    public TableViewModel Table => _table ??= new TableViewModel(Session);

    /// <summary>Gets the Spreadsheet screen.</summary>
    public SpreadsheetViewModel Spreadsheet => _spreadsheet ??= new SpreadsheetViewModel(Session);

    /// <summary>Raised when a key asks for a screen the shell does not own, with the route of that screen.</summary>
    /// <remarks>The host navigates; the shell knows which application a route belongs to and nothing about windows.</remarks>
    public event EventHandler<string>? NavigationRequested;

    /// <summary>Gets every application, in the order of the home screen.</summary>
    public ImmutableArray<CalculatorAppInfo> Apps { get; } = CalculatorApps.All;

    /// <summary>Gets the application that is open.</summary>
    [ObservableProperty]
    private CalculatorAppInfo _currentApp;

    /// <summary>Opens an application, which switches the session to it.</summary>
    /// <param name="app">The application; one without an engine is not opened.</param>
    [RelayCommand]
    public void Open(CalculatorAppInfo? app)
    {
        if (app is null || !app.IsAvailable)
        {
            return;
        }

        Session.SwitchApp(app.App);
        CurrentApp = app;

        // Switching applications turns Verify off (p. 73), so the settings screen is no longer what it showed, and
        // the line of the calculator now belongs to another application.
        Settings.Refresh();
        Calculate.Refresh();
    }

    /// <summary>Opens the application a route names, if this build has it.</summary>
    /// <param name="route">The route, such as <c>/statistics</c>.</param>
    /// <returns><see langword="true"/> when the application was opened.</returns>
    public bool OpenRoute(string? route)
    {
        CalculatorAppInfo? app = CalculatorApps.ByRoute(route);
        Open(app);
        return app is not null && app.IsAvailable;
    }

    /// <summary>Writes the session to the store, as the application closes.</summary>
    public void Save() => _store.Save(Session.Capture());

    /// <summary>Reads back the session that was stored, if there is one.</summary>
    /// <returns><see langword="true"/> when a session was restored.</returns>
    /// <remarks>
    /// A snapshot that this engine cannot read is left alone rather than reported: a calculator that will not start
    /// because of what it remembers is worse than one that starts empty.
    /// </remarks>
    public bool Load()
    {
        if (_store.Load() is not { } snapshot)
        {
            return false;
        }

        try
        {
            Session.Restore(snapshot);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }

        CurrentApp = CalculatorApps.Of(Session.App);
        Settings.Refresh();
        return true;
    }

    private CalculateViewModel Line(CalculateViewModel line)
    {
        // Every line of the calculator reaches the home screen and the settings the same way.
        line.Input.Requested += OnRequested;
        return line;
    }

    private BaseNViewModel Wire(BaseNViewModel screen)
    {
        Line(screen.Calculate);
        return screen;
    }

    private MatrixViewModel Wire(MatrixViewModel screen)
    {
        Line(screen.Calculate);
        return screen;
    }

    private VectorViewModel Wire(VectorViewModel screen)
    {
        Line(screen.Calculate);
        return screen;
    }

    private StatisticsViewModel Wire(StatisticsViewModel screen)
    {
        Line(screen.Calculate);
        return screen;
    }

    private void OnRequested(object? sender, KeyCommand command)
    {
        switch (command)
        {
            case KeyCommand.Home:
                NavigationRequested?.Invoke(this, "/");
                break;
            case KeyCommand.Settings:
                NavigationRequested?.Invoke(this, "/settings");
                break;
            default:
                break;
        }
    }
}
