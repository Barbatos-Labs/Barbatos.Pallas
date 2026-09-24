// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// Runs a test the way the window runs a screen: on one thread, where what a screen's work posts back is run in turn,
/// between the test's own steps and never during one.
/// </summary>
/// <remarks>
/// Without it, a result of the graph's work is applied on the thread pool while the test goes on, and whether a result
/// that was superseded shows up is a race rather than what the screen does.
/// </remarks>
internal static class OneThread
{
    /// <summary>Runs the body to its end, and whatever it posts back on the way.</summary>
    public static void Run(Func<Task> body)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        Loop loop = new();
        SynchronizationContext.SetSynchronizationContext(loop);
        try
        {
            Task task = body();
            _ = task.ContinueWith(_ => loop.Complete(), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
            loop.Pump();
            task.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class Loop : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _posted = [];

        public override void Post(SendOrPostCallback d, object? state)
        {
            // A work the test has left behind may finish after it: there is nothing left to run what it posts.
            try
            {
                _posted.Add((d, state));
            }
            catch (InvalidOperationException)
            {
            }
        }

        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

        public void Complete() => _posted.CompleteAdding();

        public void Pump()
        {
            foreach ((SendOrPostCallback callback, object? state) in _posted.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }
}
