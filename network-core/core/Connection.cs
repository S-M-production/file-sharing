using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;
using router_core.core;
using router_core.middleware;
using network_core.call_back;

namespace network_core.core;
/// <summary>
/// Class that houses the reader and writer for a connection
/// </summary>
/// <remarks>
/// Connection object shouldnt be created, rather it should be recieved from the Connector class and used as Connector validates the server
/// To write to connection you add a ProtocolMessage object to the queue through AddTask and the rest is handled
/// </remarks>
public class Connection
{
    private readonly NetworkStream _networkStream;
    /// <summary>
    /// Thread safe Queue for connection to write a message at a time to client
    /// </summary>
    private readonly Channel<CallBackTask> _taskQueue = Channel.CreateUnbounded<CallBackTask>();
    private readonly Channel<CallBackTask> _priorityTaskQueue = Channel.CreateUnbounded<CallBackTask>();
    private Task _asyncLoopTask = null!;
    private readonly Listener _listener;
    private Task _listenerTask = null!;
    private readonly TcpClient _client;
    private readonly ILogger _logger;
    public readonly IPAddress ClientAddress;
    public readonly int ClientPort;
    public IMiddleware Middleware { get; }
    public RouterMap RouterMap { get; } 
    private bool _started = false;
    private bool _ended = false;
    private readonly TaskCompletionSource _isWriterCompleted = new TaskCompletionSource();
    private CancellationTokenSource CancellationTokenSource { get; } = new();
    private int _awaitTime = 1; ///How long the writeAsync can wait upto
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
    public bool Start()
    {
        if (_started) return false;
        _started = true;
        _asyncLoopTask = StartAsyncWriteLoop();
        _listenerTask = _listener.Run();
        return true;
    }

    /// <summary>
    /// Gracefully stops the connection
    /// </summary>
    public async Task<bool> GracefulStop()
    {
        if (_ended) return false;
        _logger.LogInformation($"Gracefully stopping connection to {ClientAddress}:{ClientPort}");
        _ended = true;
        await CancellationTokenSource.CancelAsync();
        _ = AddTask(new ProtocolMessage(MessageType.Disconnect));
        CompleteQueue();
        await _listenerTask;
        await _asyncLoopTask;
        _client.Close();
        return true;
    }

    /// <summary>
    /// Puts message into a ordered queue that will serialize messages one at a time 
    /// </summary>
    /// <param name="protocolMessage">Message that needs to be sent</param>
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
    public bool CompleteQueue()
    {
        _priorityTaskQueue.Writer.TryComplete();
        return _taskQueue.Writer.TryComplete();
    }

    public async Task CompleteCallBack()
    {
        await _isWriterCompleted.Task;
        
    }
    
    /// <summary>
    /// Starting up async writing loop, this loop will take a message at a time out of the queue and serialize it. Should only be ran once
    /// </summary>
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