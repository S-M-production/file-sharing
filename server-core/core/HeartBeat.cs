using format.core;
using network_core.core;

namespace server_core.core;

public class HeartBeat
{
    private const double HeartBeatInterval = 1; //Seconds
    private readonly Connection _connection;
    /// <summary>
    /// Task for the loop in case if its ever needed
    /// </summary>
    public Task HeartBeatLoopTask{get; private set;}
    /// <summary>
    /// Sets up heartbeat loop
    /// </summary>
    /// <param name="connection">Connection object representing a connection to valid server</param>
    public HeartBeat(Connection connection)
    {
        this._connection = connection;
    }
    /// <summary>
    /// Sends a heartbeat Ping message every few HeartBeatInterval seconds
    /// </summary>
    public void StartHeartBeatLoop()
    {
        HeartBeatLoopTask = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay((int)(HeartBeatInterval*1000));
                _connection.AddTask(new ProtocolMessage(MessageType.Ping));
            }
        });
    }
}