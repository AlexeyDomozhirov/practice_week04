namespace task17;

using System;

public class HardStopCommand : ICommand
{
    private readonly ServerThread _server;

    public HardStopCommand(ServerThread server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public void Execute()
    {
        _server.RequestHardStop();
    }
}
