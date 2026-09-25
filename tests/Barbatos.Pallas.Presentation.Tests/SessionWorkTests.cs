// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;

namespace Barbatos.Pallas.Presentation.Tests;

/// <summary>
/// The work of a session: what its screens calculate, one after another, off the window's thread in the application
/// and where it is asked elsewhere; stopped by AC.
/// </summary>
public sealed class SessionWorkTests
{
    [Fact]
    public void TheImmediateWorkIsDoneBeforeStartReturns()
    {
        SessionWork work = SessionWork.Immediate;
        int applied = 0;
        bool lent = false;
        void Lent(object? sender, EventArgs e) => lent = true;
        work.Lending += Lent;

        work.Start(_ => 41, result => applied = result + 1);
        work.Lending -= Lent;

        applied.Should().Be(42);
        work.IsOffThread.Should().BeFalse();
        work.IsBusy.Should().BeFalse();
        work.Completion.IsCompleted.Should().BeTrue();
        lent.Should().BeFalse("the session never leaves the thread that asks");
    }

    [Fact]
    public void AWorkRunsOffTheThreadAndItsResultIsAppliedOnIt() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        int thread = Environment.CurrentManagedThreadId;
        int ranOn = thread;
        int appliedOn = -1;

        work.Start(_ => ranOn = Environment.CurrentManagedThreadId, _ => appliedOn = Environment.CurrentManagedThreadId);
        work.IsBusy.Should().BeTrue("the session is lent from the moment the work is started");
        appliedOn.Should().Be(-1, "nothing is applied before the thread that asked is free");

        await work.Completion;
        work.IsBusy.Should().BeFalse();
        ranOn.Should().NotBe(thread);
        appliedOn.Should().Be(thread);
    });

    [Fact]
    public void TheWorksOfASessionRunOneAfterAnother() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        using ManualResetEventSlim gate = new();
        ConcurrentQueue<string> steps = new();
        int lent = 0;
        work.Lending += (_, _) => lent++;

        work.Start(
            token =>
            {
                gate.Wait(token);
                steps.Enqueue("first works");
                return 1;
            },
            _ => steps.Enqueue("first applied"));
        work.Start(
            _ =>
            {
                steps.Enqueue("second works");
                return 2;
            },
            _ => steps.Enqueue("second applied"));
        gate.Set();
        await work.Completion;

        steps.Should().Equal("first works", "first applied", "second works", "second applied");
        lent.Should().Be(2, "the session is lent to each work in turn");
    });

    [Fact]
    public void BusyIsSaidOnceWhenTheFirstWorkStartsAndOnceWhenTheLastIsDone() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        List<string?> changes = work.Changes();

        work.Start(_ => 1, _ => { });
        changes.Should().Equal(nameof(SessionWork.IsBusy));
        await work.Completion;
        changes.Should().Equal(nameof(SessionWork.IsBusy), nameof(SessionWork.IsBusy));

        changes.Clear();
        work.Start(_ => 1, _ => { });
        work.Start(_ => 2, _ => { });
        changes.Should().Equal(nameof(SessionWork.IsBusy));
        await work.Completion;
        changes.Should().Equal(nameof(SessionWork.IsBusy), nameof(SessionWork.IsBusy));
    });

    [Fact]
    public void AWorkThatWaitedIsLentAndAppliedOnTheThreadThatAskedToo() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        using ManualResetEventSlim gate = new();
        int thread = Environment.CurrentManagedThreadId;
        ConcurrentBag<int> threads = [];
        work.Lending += (_, _) => threads.Add(Environment.CurrentManagedThreadId);

        work.Start(
            token =>
            {
                gate.Wait(token);
                return 1;
            },
            _ => threads.Add(Environment.CurrentManagedThreadId));
        work.Start(_ => 2, _ => threads.Add(Environment.CurrentManagedThreadId));
        gate.Set();
        await work.Completion;

        threads.Should().HaveCount(4).And.OnlyContain(id => id == thread, "the second waited for the first on the thread that asked for both");
    });

    [Fact]
    public void ACStopsTheWorkAndDropsThoseWaitingForIt() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        using ManualResetEventSlim never = new();
        bool applied = false;
        bool waitingRan = false;

        work.Start(
            token =>
            {
                never.Wait(token);
                return 1;
            },
            _ => applied = true);
        work.Start(_ => waitingRan = true, _ => applied = true);
        work.Cancel();
        await work.Completion;

        applied.Should().BeFalse();
        waitingRan.Should().BeFalse("a work waiting for the session when AC was pressed is dropped");
        work.IsBusy.Should().BeFalse();

        int after = 0;
        work.Start(_ => 7, result => after = result);
        await work.Completion;
        after.Should().Be(7, "a work started after AC runs");
    });

    [Fact]
    public void AWorkStoppedAfterItWasDoneAppliesNothing() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        using ManualResetEventSlim done = new();
        bool applied = false;

        work.Start(
            _ =>
            {
                done.Set();
                return 1;
            },
            _ => applied = true);
        done.Wait();
        work.Cancel();
        await work.Completion;

        applied.Should().BeFalse("its result had not come back to the thread that asked when AC was pressed");
    });

    [Fact]
    public void AWorkThatComesToNothingIsAppliedLikeAnyOther() => OneThread.Run(async () =>
    {
        using SessionWork work = new(offThread: true);
        using ManualResetEventSlim never = new();
        bool ran = false;
        bool applied = false;

        work.Start(_ => ran = true, () => applied = true);
        await work.Completion;
        (ran, applied).Should().Be((true, true));

        work.Start(token => never.Wait(token), () => applied = false);
        work.Cancel();
        await work.Completion;
        applied.Should().BeTrue("the second was stopped before it was applied");
    });

    [Fact]
    public void AFaultOfAWorkIsThrownOnTheThreadThatAskedAndTheWorksAfterItGoOn()
    {
        List<Exception> faults = [];
        int after = 0;

        OneThread.Run(
            async () =>
            {
                using SessionWork work = new(offThread: true);
                work.Start<int>(_ => throw new InvalidOperationException("engine fault"), _ => { });
                work.Start(_ => 3, result => after = result);
                await work.Completion;
                work.IsBusy.Should().BeFalse();
            },
            faults);

        faults.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Be("engine fault");
        after.Should().Be(3, "the works after a fault go on");
    }

    [Fact]
    public void AWorkOffTheThreadNeedsSomewhereToApplyItsResult()
    {
        using SessionWork work = new(offThread: true);
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            work.Invoking(w => w.Start(_ => 1, _ => { })).Should().Throw<InvalidOperationException>();
            work.IsBusy.Should().BeFalse("a work that could not start does not hold the session");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Fact]
    public void AWorkAndWhatAppliesItAreRequired()
    {
        SessionWork work = SessionWork.Immediate;

        work.Invoking(w => w.Start<int>(null!, _ => { })).Should().Throw<ArgumentNullException>().WithParameterName("work");
        work.Invoking(w => w.Start(_ => 1, null!)).Should().Throw<ArgumentNullException>().WithParameterName("apply");
        work.Invoking(w => w.Start(null!, () => { })).Should().Throw<ArgumentNullException>().WithParameterName("work");
        work.Invoking(w => w.Start(_ => { }, null!)).Should().Throw<ArgumentNullException>().WithParameterName("apply");
    }

    [Fact]
    public void DisposingTheWorkStopsTheOneThatHasTheSession() => OneThread.Run(async () =>
    {
        SessionWork work = new(offThread: true);
        using ManualResetEventSlim never = new();
        bool applied = false;

        work.Start(
            token =>
            {
                never.Wait(token);
                return 1;
            },
            _ => applied = true);
        work.Dispose();
        await work.Completion;

        applied.Should().BeFalse();
        work.IsBusy.Should().BeFalse();
    });
}
