namespace task17;

using System;

public class SoftStopCommand : ICommand
{
    private readonly ServerThread _server;

    public SoftStopCommand(ServerThread server)
    {
        _server = server ?? throw new ArgumentNullException(nameof(server));
    }

    public void Execute()
    {
        _server.RequestSoftStop();
    }
}
