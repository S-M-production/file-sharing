using format.core;
using Microsoft.Extensions.Logging;
using network_core.core;

namespace server_core.core;

/// <summary>Manages the heartbeat (keep-alive) logic for a connection.</summary>
public class HeartBeat
{
    /// <summary>Interval between heartbeat pings in seconds.</summary>
    private const double HeartBeatInterval = 1; //Seconds

    /// <summary>Underlying connection used to send/receive heartbeat messages.</summary>
    private readonly Connection _connection;

    /// <summary>Logger for recording heartbeat events.</summary>
    private readonly ILogger _logger;

    /// <summary>TaskCompletionSource that is completed to signal cancellation/detected disconnect.</summary>
    private readonly TaskCompletionSource _cancellationToken;

    /// <summary>TaskCompletionSource that completes when a Pong is received.</summary>
    private TaskCompletionSource _pongCallBack = null!;

    /// <summary>Number of consecutive failed send attempts.</summary>
    private int _failedSend = 0;

    /// <summary>Maximum allowed failed send attempts before signaling cancellation.</summary>
    private int _failedSendCap = 3;

    /// <summary>Task representing the running heartbeat loop (if started).</summary>
    public Task HeartBeatLoopTask{get; private set;} = null!;

    /// <summary>Creates a new <see cref="HeartBeat"/> for the given connection.</summary>
    /// <param name="connection">Connection to monitor.</param>
    /// <param name="logger">Logger to record events.</param>
    /// <param name="cancellationToken">TaskCompletionSource that will be set when a fatal disconnect is detected.</param>
    public HeartBeat(Connection connection,ILogger logger, TaskCompletionSource cancellationToken)
    {
        this._connection = connection;
        _logger = logger;
        _cancellationToken = cancellationToken;
    }
    /// <summary>Starts the heartbeat loop which periodically sends Ping messages and waits for Pong responses.</summary>
    /// <returns>None. The loop runs on a background task assigned to <see cref="HeartBeatLoopTask"/>.</returns>
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