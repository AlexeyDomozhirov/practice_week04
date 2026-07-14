namespace task19;

public interface IRepeatableCommand : ICommand
{
    bool IsFinished { get; }
}
