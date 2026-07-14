using System.Collections.Concurrent;

namespace task18;

public class CommandProcessingThread
{
    private readonly BlockingCollection<ICommand> _queue;
    private readonly IScheduler _scheduler;
    private readonly CancellationTokenSource _cts;
    private Thread _worker;

    public CommandProcessingThread(int boundedCapacity, IScheduler scheduler)
    {
        _queue = new BlockingCollection<ICommand>(new ConcurrentQueue<ICommand>(), boundedCapacity);
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _cts = new CancellationTokenSource();
    }

    public void AddCommand(ICommand command) => _queue.Add(command);

    public void Start()
    {
        _worker = new Thread(WorkLoop) { IsBackground = true, Name = "CommandWorker" };
        _worker.Start();
    }

    public void Stop()
    {
        _cts.Cancel();
        _queue.Add(new StopCommand());
        _worker?.Join();
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

                if (_queue.TryTake(out ICommand newCmd, 0))
                    ExecuteAndMaybeSchedule(newCmd);
            }
            else
            {
                ICommand newCmd;
                try
                {
                    newCmd = _queue.Take(_cts.Token);
                }
                catch (OperationCanceledException) { break; }

                if (newCmd is StopCommand) break;
                ExecuteAndMaybeSchedule(newCmd);
            }
        }
    }

    private void ExecuteAndMaybeSchedule(ICommand cmd)
    {
        ExecuteCommand(cmd);
        if (cmd is IRepeatableCommand repeatable && !repeatable.IsFinished)
            _scheduler.Add(cmd);
    }

    private void ExecuteCommand(ICommand cmd)
    {
        try { cmd.Execute(); }
        catch (Exception ex) { Console.WriteLine($"Command failed: {ex.Message}"); }
    }

    private class StopCommand : ICommand { public void Execute() { } }
}
