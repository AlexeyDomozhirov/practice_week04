namespace task19;

public class RoundRobinScheduler : IScheduler
{
    private readonly Queue<ICommand> _commands = new Queue<ICommand>();

    public bool HasCommand() => _commands.Count > 0;
    public ICommand Select() => _commands.Dequeue();
    public void Add(ICommand cmd) => _commands.Enqueue(cmd);
    public void Clear() => _commands.Clear();
}
