namespace task18;

public class LongRunningCommand : IRepeatableCommand
{
    private readonly string _name;
    private readonly int _totalSteps;
    private int _currentStep;

    public bool IsFinished => _currentStep >= _totalSteps;

    public LongRunningCommand(string name, int totalSteps)
    {
        _name = name;
        _totalSteps = totalSteps;
        _currentStep = 0;
    }

    public void Execute()
    {
        _currentStep++;
        Console.WriteLine($"[Long] {_name} step {_currentStep}/{_totalSteps}");
        Thread.Sleep(20);
    }
}
