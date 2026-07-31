using format.core;
using Microsoft.Extensions.Logging;
using network_core.core;

namespace server_core.core;

/// <summary>
/// Manages the heartbeat of a connection, the keep alive logic
/// </summary>
public class HeartBeat
{
    private const double HeartBeatInterval = 1; //Seconds
    private readonly Connection _connection;
    private readonly ILogger _logger;

    /// <summary>
    /// Task for the loop in case if its ever needed
    /// </summary>
    public Task HeartBeatLoopTask{get; private set;} = null!;

    /// <summary>
    /// Sets up heartbeat loop
    /// </summary>
    /// <param name="connection">Connection object representing a connection to valid server</param>
    /// <param name="logger">Logger created at start of program</param>
    public HeartBeat(Connection connection,ILogger logger)
    {
        this._connection = connection;
        _logger = logger;
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
                TaskCompletionSource taskCompletionSource = _connection.AddTask(new ProtocolMessage(MessageType.Ping));
                await taskCompletionSource.Task;
                _logger.LogInformation($"Sent heartbeat to {_connection.ClientAddress}:{_connection.ClientPort}");
            }
        });
    }
}