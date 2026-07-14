namespace task18;

public interface IRepeatableCommand : ICommand
{
    bool IsFinished { get; }
}
