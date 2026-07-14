namespace task19;

using System;
using System.Collections.Concurrent;

public class CommandProcessingThread : IDisposable
{
    private readonly BlockingCollection<ICommand> _queue;
    private readonly IScheduler _scheduler;
    private readonly CancellationTokenSource _cts;
    private Thread _worker = null!;
    
    private int _isStarted = 0;
    private int _isStopped = 0;

    public CommandProcessingThread(int boundedCapacity, IScheduler scheduler)
    {
        if (boundedCapacity <= 0) 
            throw new ArgumentOutOfRangeException(nameof(boundedCapacity));

        _queue = new BlockingCollection<ICommand>(new ConcurrentQueue<ICommand>(), boundedCapacity);
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _cts = new CancellationTokenSource();
    }

    public void AddCommand(ICommand command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        
        if (_queue.IsAddingCompleted)
        {
            throw new InvalidOperationException("Cannot add commands. The processing thread has been stopped.");
        }

        _queue.Add(command);
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _isStarted, 1) == 1)
        {
            throw new InvalidOperationException("The thread is already running.");
        }

        _worker = new Thread(WorkLoop) { IsBackground = true, Name = "CommandWorker" };
        _worker.Start();
    }

    public void HardStop()
    {
        if (Interlocked.Exchange(ref _isStopped, 1) == 1 || Volatile.Read(ref _isStarted) == 0)
            return;
    
        _cts.Cancel();
        _scheduler.Clear();
    
        try
        {
            _queue.Add(new StopCommand());
        }
        catch (InvalidOperationException) { }
    
        _queue.CompleteAdding();
    
        _worker?.Join();
    
        Interlocked.Exchange(ref _isStarted, 0);
    }

    private void WorkLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_scheduler.HasCommand())
            {
                ICommand cmd = _scheduler.Select();
                ExecuteCommand(cmd);

                if (cmd is IRepeatableCommand repeatable && !repeatable.IsFinished)
                    _scheduler.Add(cmd);

                try
                {
                    if (_queue.TryTake(out ICommand? newCmd, 0))
                        ExecuteAndMaybeSchedule(newCmd);
                }
                catch (ObjectDisposedException) { break; }
            }
            else
            {
                ICommand newCmd;
                try
                {
                    newCmd = _queue.Take(_cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch (InvalidOperationException) { break; }

                if (newCmd is StopCommand) break;
                ExecuteAndMaybeSchedule(newCmd);
            }
        }
    }

    private void ExecuteAndMaybeSchedule(ICommand? cmd)
    {
        if (cmd == null || cmd is StopCommand) return; 

        ExecuteCommand(cmd);
        if (cmd is IRepeatableCommand repeatable && !repeatable.IsFinished)
            _scheduler.Add(cmd);
    }

    private void ExecuteCommand(ICommand cmd)
    {
        try { cmd.Execute(); }
        catch (Exception ex) { Console.WriteLine($"Command failed: {ex.Message}"); }
    }

    public void Dispose()
    {
        HardStop();
        
        _cts.Dispose();
        _queue.Dispose();
    }

    private class StopCommand : ICommand { public void Execute() { } }
}
