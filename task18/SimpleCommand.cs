namespace task18;

public class SimpleCommand : ICommand
{
    private readonly string _name;
    public SimpleCommand(string name) => _name = name;
    public void Execute() => Console.WriteLine($"[Simple] {_name} executed.");
}
