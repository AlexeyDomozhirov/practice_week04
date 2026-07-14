namespace task19;

using System;

public class TestCommand : IRepeatableCommand
{
    private readonly int _id;
    private int _counter;
    private readonly int _maxExecutions;

    public bool IsFinished => _counter >= _maxExecutions;

    public TestCommand(int id, int maxExecutions = 3)
    {
        _id = id;
        _maxExecutions = maxExecutions;
    }

    public void Execute()
    {
        _counter++;
        Console.WriteLine($"Поток {_id} вызов {_counter}");
    }
}
