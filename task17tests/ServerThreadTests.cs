namespace task17tests;

using task17;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public class ServerThreadTests 
{
    [Fact]
    public void HardStop_StopsImmediately_RemainingCommandsIgnored()
    {
        var server = new ServerThread();
        var executed = new List<int>();
    
        ICommand CreateCommand(int id) => new ActionCommand(() =>
        {
            Thread.Sleep(10);
            lock (executed) executed.Add(id);
        });
    
        server.Enqueue(CreateCommand(1));
        server.Enqueue(CreateCommand(2));
        server.Enqueue(new HardStopCommand(server));
        server.Enqueue(CreateCommand(3));
        server.Enqueue(CreateCommand(4));
    
        server.Start();
    
        server.Join();
    
        Assert.DoesNotContain(3, executed);
        Assert.DoesNotContain(4, executed);
    }
    
    [Fact]
    public void SoftStop_ProcessesAllCommands_ThenStops()
    {
        var server = new ServerThread();
        var executed = new ConcurrentBag<int>();
    
        ICommand CreateCommand(int id) => new ActionCommand(() =>
        {
            executed.Add(id);
        });
    
        server.Enqueue(CreateCommand(1));
        server.Enqueue(CreateCommand(2));
        server.Enqueue(new SoftStopCommand(server));
    
        server.Start();
    
        server.Join();
    
        Assert.Contains(1, executed);
        Assert.Contains(2, executed);
    
        Assert.Throws<InvalidOperationException>(() => server.Enqueue(CreateCommand(99)));
    }
    
    [Fact]
    public void HardStop_Throws_WhenCalledFromForeignThread()
    {
        var server = new ServerThread();
        server.Start();
    
        var hardStop = new HardStopCommand(server);
        var ex = Assert.Throws<InvalidOperationException>(() => hardStop.Execute());
    }
    
    [Fact]
    public void SoftStop_Throws_WhenCalledFromForeignThread()
    {
        var server = new ServerThread();
        server.Start();
    
        var softStop = new SoftStopCommand(server);
        var ex = Assert.Throws<InvalidOperationException>(() => softStop.Execute());
    }
    
    [Fact]
    public void ExceptionInCommand_IsForwardedToHandler()
    {
        var server = new ServerThread();
        Exception? caughtException = null;
        ICommand? caughtCommand = null;
    
        server.ExceptionHandler += (ex, cmd) =>
        {
            caughtException = ex;
            caughtCommand = cmd;
        };
    
        var faultyCommand = new ActionCommand(() => throw new InvalidOperationException("Test error"));
        server.Enqueue(faultyCommand);
        server.Enqueue(new HardStopCommand(server));
    
        server.Start();
        server.Join();
    
        Assert.NotNull(caughtException);
        Assert.IsType<InvalidOperationException>(caughtException);
        Assert.Equal("Test error", caughtException!.Message);
        Assert.Same(faultyCommand, caughtCommand);
    }
    
    private class ActionCommand : ICommand
    {
        private readonly Action _action;
        public ActionCommand(Action action) => _action = action;
        public void Execute() => _action();
    }
}
