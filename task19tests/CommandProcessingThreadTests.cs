namespace task19tests;

using System.Threading;
using Xunit;
using task19;

using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

public class CommandProcessingThreadTests
{
    [Fact]
    public void TestCommand_ExecutesThreeTimes_AndFinishes()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);
        var cmd = new TestCommand(1, maxExecutions: 3);
        scheduler.Add(cmd);

        processor.Start();
        Thread.Sleep(200);

        Assert.True(cmd.IsFinished);
        Assert.False(scheduler.HasCommand());
        processor.HardStop();
    }

    [Fact]
    public void HardStop_InterruptsRemainingCommands()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);

        var cmd1 = new TestCommand(1, maxExecutions: 100);
        var cmd2 = new TestCommand(2, maxExecutions: 100);
        scheduler.Add(cmd1);
        scheduler.Add(cmd2);

        processor.Start();
        processor.HardStop();

        Assert.False(cmd1.IsFinished);
        Assert.False(cmd2.IsFinished);
        Assert.False(scheduler.HasCommand());
    }

    [Fact]
    public void FiveTestCommands_EachExecutesExactlyThreeTimes()
    {
        var scheduler = new RoundRobinScheduler();
        var processor = new CommandProcessingThread(boundedCapacity: 10, scheduler);
        var commands = new List<TestCommand>();
        for (int i = 1; i <= 5; i++)
        {
            var cmd = new TestCommand(i, maxExecutions: 3);
            commands.Add(cmd);
            scheduler.Add(cmd);
        }

        processor.Start();
        while (scheduler.HasCommand())
        {
            Thread.Sleep(5);
        }
        processor.HardStop();

        foreach (var cmd in commands)
            Assert.True(cmd.IsFinished);
    }
}
