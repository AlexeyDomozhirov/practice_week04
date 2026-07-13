namespace task17;

using System;
using System.Collections.Concurrent;
using System.Threading;

public class ServerThread
{
    private readonly BlockingCollection<ICommand> _queue = new();
    private readonly CancellationTokenSource _hardStopCts = new();
    private Thread? _thread;

    public event Action<Exception, ICommand>? ExceptionHandler;

    public void Start()
    {
        if (_thread != null)
            throw new InvalidOperationException("Server thread is already running.");

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "ServerThread"
        };
        _thread.Start();
    }

    public void Enqueue(ICommand command)
    {
        _queue.Add(command);
    }

    public void Join()
    {
        _thread?.Join();
    }

    internal void RequestHardStop()
    {
        EnsureCalledFromServerThread();
        _hardStopCts.Cancel();
    }

    internal void RequestSoftStop()
    {
        EnsureCalledFromServerThread();
        _queue.CompleteAdding();
    }

    private void EnsureCalledFromServerThread()
    {
        if (Thread.CurrentThread != _thread)
            throw new InvalidOperationException(
                "HardStop / SoftStop must be called from the server thread.");
    }

    private void Run()
    {
        try
        {
            foreach (var command in _queue.GetConsumingEnumerable(_hardStopCts.Token))
            {
                ExecuteCommand(command);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _hardStopCts.Dispose();
        }
    }

    private void ExecuteCommand(ICommand command)
    {
        try
        {
            command.Execute();
        }
        catch (Exception ex)
        {
            ExceptionHandler?.Invoke(ex, command);
        }
    }
}
