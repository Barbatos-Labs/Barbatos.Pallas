// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

namespace Barbatos.Pallas.Presentation;

/// <summary>
/// One kind of work a graph does off the window's thread - sampling its curves, naming its points, reading it at the
/// pointer - of which only the latest counts: starting it again cancels the one before, whose result is never shown.
/// </summary>
/// <remarks>
/// A curve of ∫(Abs(sin(10x)),0,x) took 3.8 s to sample at 800 pixels and 5.8 s to zoom (24 Sep 2026, Release), all
/// of it on the window's thread, and dragging the edge of the window samples it again at every size it passes
/// through. The work runs on the thread pool instead, and its result is applied where it was started from: on the
/// window's thread in the application, through its <see cref="SynchronizationContext"/>.
/// </remarks>
internal sealed class GraphWork(Action changed)
{
    private CancellationTokenSource? _current;

    /// <summary>Gets the work started, completed once the latest has applied its result and every one before it has stopped.</summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

    /// <summary>Gets whether the latest work has yet to apply its result.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Starts the work, cancelling the one before.</summary>
    /// <typeparam name="T">What the work comes to.</typeparam>
    /// <param name="work">The work, which is to stop when its token is cancelled.</param>
    /// <param name="apply">What is done with its result, where the work was started from.</param>
    public void Start<T>(Func<CancellationToken, T> work, Action<T> apply)
    {
        _current?.Cancel();
        CancellationTokenSource source = new();
        _current = source;
        IsRunning = true;
        changed();

        // A cancelled work stops at its next calculation, and what it comes to is dropped; waiting for it too means
        // that once the completion is done, nothing more changes. Only a work still running is waited for, so the
        // chain is never longer than what runs at once.
        Task running = Run(work, apply, source.Token);
        Completion = Completion.IsCompleted ? running : Task.WhenAll(Completion, running);
    }

    /// <summary>Cancels the latest work, if it is still running.</summary>
    public void Cancel()
    {
        _current?.Cancel();
        _current = null;
        IsRunning = false;
        changed();
    }

    private async Task Run<T>(Func<CancellationToken, T> work, Action<T> apply, CancellationToken token)
    {
        // Assigned before the try, so that a mutant of what is in it still compiles and Stryker tests this method: a
        // local assigned only there made every one of them a compile error, and the whole method went untested.
        T result = default!;
        try
        {
            result = await Task.Run(() => work(token), token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            // The work was cancelled, which its token says below.
        }

        // Cancelled, while the work was done or after, before its result came back: a later work has taken its place.
        if (!token.IsCancellationRequested)
        {
            IsRunning = false;
            apply(result);
            changed();
        }
    }
}
