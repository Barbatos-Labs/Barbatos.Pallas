// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Runtime.ExceptionServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// The calculations a session is asked for, one after another: in the application, off the window's thread, with the
/// session lent to each until it is done; elsewhere, where they are asked.
/// </summary>
/// <remarks>
/// <para>
/// A table of thirteen integrals held the window for 1.9 s (24 Sep 2026), and a calculation may run for as long as
/// its budget allows, ten seconds by default: the window could neither be moved nor redrawn meanwhile, and nothing
/// could stop it. Off the window's thread, the window stays the user's, and <see cref="Cancel"/> stops the calculation
/// at its next step (the AC key, assumption U32).
/// </para>
/// <para>
/// A session is used from one thread at a time. So the works of a session run one after another, each applying its
/// result where it was started from - the window's thread, through its <see cref="SynchronizationContext"/> - before
/// the next one starts; and while one runs, nothing else touches the session: the window takes no input while
/// <see cref="IsBusy"/>, and a save writes the session as it was before the work (<see cref="Lending"/>).
/// </para>
/// </remarks>
public sealed partial class SessionWork : ObservableObject, IDisposable
{
    /// <summary>How a work's result comes back: on the context it was started from, and never before Start has returned.</summary>
    internal const ConfigureAwaitOptions Back = ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.ForceYielding;

    private CancellationTokenSource _cancellation = new();
    private int _waiting;

    /// <summary>Creates the work of a session.</summary>
    /// <param name="offThread">
    /// Whether a work runs off the thread that asks for it, as it does in the application; otherwise each is done where
    /// it is asked, before <see cref="Start{T}"/> returns, which is what <see cref="Immediate"/> does.
    /// </param>
    public SessionWork(bool offThread)
    {
        IsOffThread = offThread;
    }

    /// <summary>Gets the work that does each work where it is asked: that of a screen made without one, and of tests.</summary>
    /// <remarks>It is never busy and holds nothing of any session, so every screen can share it; it is not disposed.</remarks>
    public static SessionWork Immediate { get; } = new(offThread: false);

    /// <summary>Gets whether a work runs off the thread that asks for it.</summary>
    public bool IsOffThread { get; }

    /// <summary>Gets whether a work has the session, or is waiting for it: from when it is started until its result is applied.</summary>
    public bool IsBusy => _waiting > 0;

    /// <summary>Gets the works started, completed once every one of them has applied its result or stopped.</summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

    /// <summary>Raised on the thread that started a work, just before the session is lent to it.</summary>
    /// <remarks>What wants the session while the work has it - a save - takes what it needs from it here.</remarks>
    public event EventHandler? Lending;

    /// <summary>Starts a work on the session, after those started before it.</summary>
    /// <typeparam name="T">What the work comes to.</typeparam>
    /// <param name="work">The work, which reads what it needs and is to stop when its token is cancelled.</param>
    /// <param name="apply">What is done with its result, on the thread that started it; not done when it is cancelled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="work"/> or <paramref name="apply"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// The work runs off the thread, and the thread that starts it has no <see cref="SynchronizationContext"/> to
    /// apply its result on.
    /// </exception>
    public void Start<T>(Func<CancellationToken, T> work, Action<T> apply)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(apply);
        if (!IsOffThread)
        {
            apply(work(CancellationToken.None));
            return;
        }

        SynchronizationContext context = SynchronizationContext.Current
            ?? throw new InvalidOperationException("A work off the thread applies its result where it was started, which needs a SynchronizationContext.");
        _waiting++;
        if (_waiting == 1)
        {
            OnPropertyChanged(nameof(IsBusy));
        }

        Completion = Run(Completion, work, apply, context, _cancellation.Token);
    }

    /// <summary>Starts a work on the session that comes to nothing but what it does to it, after those started before it.</summary>
    /// <param name="work">The work, which is to stop when its token is cancelled.</param>
    /// <param name="apply">What is done once it is done, on the thread that started it; not done when it is cancelled.</param>
    /// <exception cref="ArgumentNullException"><paramref name="work"/> or <paramref name="apply"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">As <see cref="Start{T}"/>.</exception>
    public void Start(Action<CancellationToken> work, Action apply)
    {
        ArgumentNullException.ThrowIfNull(work);
        ArgumentNullException.ThrowIfNull(apply);
        Start<object?>(
            token =>
            {
                work(token);
                return null;
            },
            _ => apply());
    }

    /// <summary>Stops the work that has the session at its next step, and drops those waiting for it.</summary>
    /// <remarks>
    /// The session is back once the work has stopped, which a calculation does at its next step: the engine looks at
    /// its token as often as it looks at its budget. What the work had done to the session stays done; what it would
    /// have applied is not.
    /// </remarks>
    [RelayCommand]
    public void Cancel()
    {
        _cancellation.Cancel();
        _cancellation = new CancellationTokenSource();
    }

    /// <summary>Stops the work that has the session, as the application ends.</summary>
    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private async Task Run<T>(Task before, Func<CancellationToken, T> work, Action<T> apply, SynchronizationContext context, CancellationToken token)
    {
        await before.ConfigureAwait(true);
        try
        {
            if (!token.IsCancellationRequested)
            {
                Lending?.Invoke(this, EventArgs.Empty);

                // Always back through the context, even when the work was done before it was awaited: a work of a few
                // microseconds was applied inside Start, before the command that started it had returned, in six runs
                // of the tests out of fifteen (25 Sep 2026); none of twenty since.
                T result = await Task.Run(() => work(token), token).ConfigureAwait(Back);

                // Stopped once it was done, before its result came back: nothing is applied either.
                if (!token.IsCancellationRequested)
                {
                    apply(result);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Stopped: nothing is applied.
        }
        catch (Exception exception)
        {
            // A fault of the engine or of what applies its result is a fault of the application: it is thrown again
            // on the window's thread, where the application reports what nothing caught, and the works after it go on.
            ExceptionDispatchInfo fault = ExceptionDispatchInfo.Capture(exception);
            context.Post(_ => fault.Throw(), null);
        }
        finally
        {
            _waiting--;
            if (_waiting == 0)
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }
}
