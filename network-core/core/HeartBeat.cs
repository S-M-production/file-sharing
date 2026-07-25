using format.core;

namespace client_core.core;

public class HeartBeat
{
    private const double HeartBeatInterval = 10; //Seconds
    private readonly Connection _connection;
    private Task _heartBeatTask;
    /// <summary>
    /// Sets up heartbeat loop
    /// </summary>
    /// <param name="connection">Connection object representing a connection to valid server</param>
    public HeartBeat(Connection connection)
    {
        this._connection = connection;
        _heartBeatTask = HeartBeatLoop();
    }
    /// <summary>
    /// Sends a heartbeat Ping message every few HeartBeatInterval seconds
    /// </summary>
    private async Task HeartBeatLoop()
    {
        while (true)
        {
            await Task.Delay((int)(HeartBeatInterval*1000));
            await _connection.AddTask(new ProtocolMessage(MessageType.Ping));
        }
    }
}