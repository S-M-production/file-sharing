using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;
using router_core.core;
using router_core.middleware;
using network_core.call_back;

namespace network_core.core;
//TODO: Extract Writing out of this and create a Writer class
/// <summary>
/// Class that houses the reader and writer for a connection
/// </summary>
/// <remarks>
/// Connection object shouldnt be created, rather it should be recieved from the Connector class and used as Connector validates the server
/// To write to connection you add a ProtocolMessage object to the queue through AddTask and the rest is handled
/// </remarks>
public class Connection
{
    /// <summary>
    /// Network stream for reading and writing data over the TCP connection.
    /// </summary>
    private readonly NetworkStream _networkStream;
    /// <summary>
    /// Thread-safe unbounded queue for serializing and sending messages in order to the client.
    /// </summary>
    private readonly Channel<CallBackTask> _taskQueue = Channel.CreateUnbounded<CallBackTask>();
    /// <summary>
    /// High-priority unbounded queue that bypasses normal message queue ordering for urgent messages.
    /// </summary>
    private readonly Channel<CallBackTask> _priorityTaskQueue = Channel.CreateUnbounded<CallBackTask>();
    /// <summary>
    /// Task reference for the async write loop that continuously sends queued messages.
    /// </summary>
    private Task _asyncWriteLoopTask = null!;
    /// <summary>
    /// Listener instance responsible for reading and processing incoming messages from the client.
    /// </summary>
    private readonly Listener _listener;
    /// <summary>
    /// Task reference for the listener's async read loop.
    /// </summary>
    private Task _listenerTask = null!;
    /// <summary>
    /// The underlying TCP client connection to the remote peer.
    /// </summary>
    private readonly TcpClient _client;
    /// <summary>
    /// Logger instance for recording connection events, errors, and diagnostics.
    /// </summary>
    private readonly ILogger _logger;
    /// <summary>
    /// The IPv4 address of the connected client.
    /// </summary>
    public readonly IPAddress ClientAddress;
    /// <summary>
    /// The port number of the connected client.
    /// </summary>
    public readonly int ClientPort;
    /// <summary>
    /// Middleware pipeline for processing incoming and outgoing messages.
    /// </summary>
    public IMiddleware Middleware { get; }
    /// <summary>
    /// Router map defining how messages are routed to handlers based on message type.
    /// </summary>
    public RouterMap RouterMap { get; } 
    /// <summary>
    /// Flag indicating whether the connection's read/write loops have been started.
    /// </summary>
    private bool _isReadWriteStarted = false;
    /// <summary>
    /// Flag indicating whether the connection shutdown process has been initiated.
    /// </summary>
    private bool _isReadWriteEnded = false;
    /// <summary>
    /// Completion source that signals when the async write loop has finished processing all queued messages.
    /// </summary>
    private readonly TaskCompletionSource _isWriterCompleted = new TaskCompletionSource();
    /// <summary>
    /// Cancellation token source for signaling graceful shutdown to all async operations.
    /// </summary>
    private CancellationTokenSource CancellationTokenSource { get; } = new();
    /// <summary>
    /// Maximum timeout in seconds for individual write operations before raising a TimeoutException.
    /// </summary>
    private int _awaitTime = 1;
    /// <summary>
    /// Sets up listening and writing loop for the connection
    /// </summary>
    /// <param name="client">TcpClient connection, Ideally should be a server connection that is validated through Connector</param>
    /// <param name="logger">ILogger that is passed down into here</param>
    /// <param name="middleware">Middleware provided by core utilizing this class</param>
    /// <param name="routerMap">Router map the connection will use</param>
    /// TODO: Use .NET DI for logger
    public Connection(TcpClient client,ILogger logger,IMiddleware middleware, RouterMap routerMap)
    {
        IPEndPoint clientInfo = (client.Client.RemoteEndPoint as IPEndPoint)!;
        this.RouterMap = routerMap;
        ClientAddress = clientInfo.Address.MapToIPv4();
        ClientPort = clientInfo.Port;
        this._client = client;
        this.Middleware = middleware;
        _networkStream = client.GetStream();
        _listener = new Listener(client,logger,this,RouterMap,middleware,CancellationTokenSource);
        this._logger = logger;
    }
    /// <summary>
    /// Starts up async write and read loops
    /// </summary>
    /// <returns>True if the connection was successfully started, false if already started</returns>
    public bool Start()
    {
        if (_isReadWriteStarted) return false;
        _isReadWriteStarted = true;
        _asyncWriteLoopTask = StartAsyncWriteLoop();
        _listenerTask = _listener.Run();
        return true;
    }

    /// <summary>
    /// Gracefully stops the connection
    /// </summary>
    /// <returns>True if the connection was successfully stopped, false if already stopped.</returns>
    public async Task<bool> GracefulStop()
    {
        if (_isReadWriteEnded) return false;
        _logger.LogInformation($"Gracefully stopping connection to {ClientAddress}:{ClientPort}");
        _isReadWriteEnded = true;
        await CancellationTokenSource.CancelAsync();
        _ = AddTask(new ProtocolMessage(MessageType.Disconnect));
        CompleteQueue();
        await _listenerTask;
        await _asyncWriteLoopTask;
        _client.Close();
        return true;
    }

    /// <summary>
    /// Puts message into a ordered queue that will serialize messages one at a time 
    /// </summary>
    /// <param name="protocolMessage">Message that needs to be sent</param>
    /// <param name="priority">If true, message is queued in priority queue instead of normal queue.</param>
    /// <returns>TaskCompletionSource that completes when the message has been written.</returns>
    public TaskCompletionSource<bool> AddTask(ProtocolMessage protocolMessage, bool priority = false)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        var task = new CallBackTask(protocolMessage, tcs);
        if (priority)
            _priorityTaskQueue.Writer.TryWrite(task);
        else
            _taskQueue.Writer.TryWrite(task);
        return tcs;
    }
    /// <summary>
    /// Way to end the queue
    /// </summary>
    /// <returns>True if the task queue was successfully completed.</returns>
    public bool CompleteQueue()
    {
        _priorityTaskQueue.Writer.TryComplete();
        return _taskQueue.Writer.TryComplete();
    }

    /// <summary>
    /// Waits for the async write loop to complete all pending operations.
    /// </summary>
    public async Task CompleteCallBack()
    {
        await _isWriterCompleted.Task;
        
    }
    
    /// <summary>
    /// Starting up async writing loop, this loop will take a message at a time out of the queue and serialize it. Should only be ran once
    /// </summary>
    /// <returns>A task that completes when the write loop finishes processing all queued messages.</returns>
    async Task StartAsyncWriteLoop()
    {
        CallBackTask? call;
        while ((call = await TryReadNextTask()) != null)
        {
            byte[] buffer = ProtocolSerializer.Serialize(call.ProtocolMessage);
            try
            {
                await _networkStream.WriteAsync(buffer, 0, buffer.Length).WaitAsync(TimeSpan.FromSeconds(_awaitTime));
            }
            catch (Exception e) when (e is IOException || e is TimeoutException)
            {
                _logger.LogError(e.Message);
                call.SetCompletionSource(false);
                continue;
            }catch (Exception e)
            {
                _logger.LogError(e.Message);
                call.SetCompletionSource(false);
                continue;
            }
            _logger.LogInformation("Wrote: {0} to {1}:{2}",ProtocolSerializer.ReadableSerialize(call.ProtocolMessage),ClientAddress,ClientPort);
            call.SetCompletionSource();
        }
        _isWriterCompleted.TrySetResult();
    }

    /// <summary>
    /// Attempts to read the next task from either priority or normal queue, waiting if neither has data.
    /// </summary>
    /// <returns>The next CallBackTask from priority queue if available, otherwise from normal queue, or null if both queues are completed.</returns>
    private async Task<CallBackTask?> TryReadNextTask()
    {
        while (true)
        {
            if (_priorityTaskQueue.Reader.TryRead(out CallBackTask? priorityTask))
                return priorityTask;
            if (_taskQueue.Reader.TryRead(out CallBackTask? task))
                return task;

            if (_priorityTaskQueue.Reader.Completion.IsCompleted && _taskQueue.Reader.Completion.IsCompleted)
                return null;

            await Task.WhenAny(
                _priorityTaskQueue.Reader.WaitToReadAsync().AsTask(),
                _taskQueue.Reader.WaitToReadAsync().AsTask());
        }
    }
    
}