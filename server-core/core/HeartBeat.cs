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
    private readonly CancellationTokenSource _cancellationToken;
    private TaskCompletionSource _pongCallBack;
    /// <summary>
    /// Task for the loop in case if its ever needed
    /// </summary>
    public Task HeartBeatLoopTask{get; private set;} = null!;

    /// <summary>
    /// Sets up heartbeat loop
    /// </summary>
    /// <param name="connection">Connection object representing a connection to valid server</param>
    /// <param name="logger">Logger created at start of program</param>
    /// <param name="cancellationToken">If the heartbeat detects client wrongfully disconnects, this will be set</param>
    public HeartBeat(Connection connection,ILogger logger, CancellationTokenSource cancellationToken)
    {
        this._connection = connection;
        _logger = logger;
        _cancellationToken = cancellationToken;
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
                _pongCallBack = new TaskCompletionSource();

                await Task.Delay((int)(HeartBeatInterval*1000));
                
                _connection.RouterMap.AddRoute(MessageType.Pong,
                    messageHandler: (message) =>
                    {
                        _pongCallBack.SetResult();
                        return null;
                    }, 
                    1);
                
                TaskCompletionSource taskCompletionSource = _connection.AddTask(new ProtocolMessage(MessageType.Ping));
                await taskCompletionSource.Task;
                
                Task pongTask = _pongCallBack.Task;
                try
                {
                    await pongTask.WaitAsync(TimeSpan.FromSeconds(HeartBeatInterval));
                }
                catch (TimeoutException e)
                {
                    await _cancellationToken.CancelAsync();
                    break;
                }
                _logger.LogInformation($"Sent heartbeat to {_connection.ClientAddress}:{_connection.ClientPort}");
            }
        });
    }
}