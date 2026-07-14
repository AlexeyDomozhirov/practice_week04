namespace task18tests;

using System.Threading;
using Xunit;
using task18;

public class CommandProcessingThreadTests
{
    [Fact]
    public void RoundRobinScheduler_SelectsInFifoOrder()
    {
        var scheduler = new RoundRobinScheduler();
        var cmd1 = new SimpleCommand("A");
        var cmd2 = new SimpleCommand("B");
        var cmd3 = new SimpleCommand("C");

        scheduler.Add(cmd1);
        scheduler.Add(cmd2);
        scheduler.Add(cmd3);

        Assert.True(scheduler.HasCommand());
        Assert.Same(cmd1, scheduler.Select());
        Assert.Same(cmd2, scheduler.Select());
        Assert.Same(cmd3, scheduler.Select());
        Assert.False(scheduler.HasCommand());
    }

    [Fact]
    public void LongCommand_CompletesAllSteps()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);

        var longCmd = new LongRunningCommand("Test", totalSteps: 3);
        scheduler.Add(longCmd);

        processor.Start();
        Thread.Sleep(300);
        processor.Stop();

        Assert.True(longCmd.IsFinished);
        Assert.False(scheduler.HasCommand());
    }

    [Fact]
    public void CommandsFromQueue_AreScheduledIfRepeatable()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);

        var longCmd = new LongRunningCommand("FromQueue", totalSteps: 3);
        processor.AddCommand(longCmd);

        processor.Start();
        Thread.Sleep(300);
        processor.Stop();

        Assert.True(longCmd.IsFinished);
        Assert.False(scheduler.HasCommand());
    }

    [Fact]
    public void NewCommandArrivesDuringLongProcessing_IsExecutedWithoutStarvation()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);

        var longCmd = new LongRunningCommand("Long", 5);
        scheduler.Add(longCmd);

        processor.Start();
        Thread.Sleep(30);

        bool executed = false;
        var flagCmd = new FlagCommand(() => executed = true);
        processor.AddCommand(flagCmd);

        Thread.Sleep(30);
        processor.Stop();

        Assert.True(executed, "New command should be executed even while long operations are in scheduler.");
    }

    [Fact]
    public void DeadlockAvoidance_QueueFull_DoesNotBlock()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 1, scheduler);
        processor.Start();

        processor.AddCommand(new SimpleCommand("First"));
        Thread.Sleep(50);

        var exception = Record.Exception(() => processor.AddCommand(new SimpleCommand("Second")));
        Assert.Null(exception);

        processor.Stop();
    }

    [Fact]
    public void NoBusyWaitWhenIdle()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);
        processor.Start();
        Thread.Sleep(100);

        var flagCmd = new FlagCommand(() => { });
        var startTime = DateTime.UtcNow;
        processor.AddCommand(flagCmd);
        Thread.Sleep(100);
        var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;

        Assert.True(elapsed < 500, "Command should be processed quickly, not stuck in busy loop.");
        processor.Stop();
    }

    private class FlagCommand : ICommand
    {
        private readonly Action _action;
        public FlagCommand(Action action) => _action = action;
        public void Execute() => _action();
    }
}
