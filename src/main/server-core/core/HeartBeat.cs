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
    private readonly TaskCompletionSource _cancellationToken;
    private TaskCompletionSource _pongCallBack = null!;
    private int _failedSend = 0;
    private int _failedSendCap = 3;

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
    public HeartBeat(Connection connection,ILogger logger, TaskCompletionSource cancellationToken)
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
                
                var taskCompletionSource = await _connection.AddTask(
                    new ProtocolMessage(MessageType.Ping), priority: true).Task;
                
                if (!taskCompletionSource)
                {
                    _logger.LogError($"Failed to send pong call to heartbeat.... retry no. {++_failedSend}");
                    if (_failedSend >= _failedSendCap)
                    {
                        _logger.LogError($"Failed to send pong call to heartbeat {_failedSendCap} times");
                        _cancellationToken.SetResult();
                        break;
                    }
                    continue;
                }
                
                Task pongTask = _pongCallBack.Task;
                try
                {
                    await pongTask.WaitAsync(TimeSpan.FromSeconds(HeartBeatInterval));
                }
                catch (TimeoutException e)
                {
                    _logger.LogError("Notifying disrupted Heartbeat");
                    _cancellationToken.SetResult();
                    break;
                }
                _logger.LogInformation($"Sent heartbeat to {_connection.ClientAddress}:{_connection.ClientPort}");
            }
        });
    }
}