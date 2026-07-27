using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;
using router_core.core;
using router_core.middleware;

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
    public readonly Channel<ProtocolMessage> TaskQueue = Channel.CreateUnbounded<ProtocolMessage>();
    private Task _asyncLoopTask = null!;
    private Listener _listener;
    private Task _listenerTask = null!;
    private readonly TcpClient _client;
    private HeartBeat _heartBeat = null!;
    private readonly ILogger _logger;
    private readonly IPAddress _clientAddress;
    private readonly int _clientPort;
    public IMiddleware Middleware { get; }
    public RouterMap RouterMap { get; } 
    private int Started = 0;
    /// <summary>
    /// Sets up listening and writing loop for the connection
    /// </summary>
    /// <param name="client">TcpClient connection, Ideally should be a server connection that is validated through Connector</param>
    /// <param name="logger">ILogger that is passed down into here</param>
    /// <param name="middleware">Middleware provided by core utilizing this class</param>
    /// TODO: Use .NET DI for logger
    public Connection(TcpClient client,ILogger logger,IMiddleware middleware, RouterMap routerMap)
    {
        IPEndPoint clientInfo = (client.Client.RemoteEndPoint as IPEndPoint)!;
        this.RouterMap = routerMap;
        _clientAddress = clientInfo.Address.MapToIPv4();
        _clientPort = clientInfo.Port;
        this._client = client;
        this.Middleware = middleware;
        _networkStream = client.GetStream();
        _listener = new Listener(client,logger,this,RouterMap,middleware);
        this._logger = logger;
    }

    public void Start(bool heartbeat = true)
    {
        if (Started != 0) return;
        Started = 1;
        _asyncLoopTask = StartAsyncWriteLoop();
        _listenerTask = _listener.Run();
        if (!heartbeat) return;
        _heartBeat = new HeartBeat(this);
    } 
    
    /// <summary>
    /// Puts message into a ordered queue that will serialize messages one at a time 
    /// </summary>
    /// <param name="protocolMessage">Message that needs to be sent</param>
    public async Task AddTask(ProtocolMessage protocolMessage)
    {
        await TaskQueue.Writer.WriteAsync(protocolMessage);
    }
    /// <summary>
    /// Way to end the queue
    /// </summary>
    public Task CompleteQueue()
    {
        try
        {
            TaskQueue.Writer.Complete();
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
    /// <summary>
    /// Starting up async writing loop, this loop will take a message at a time out of the queue and serialize it. Should only be ran once
    /// </summary>
    async Task StartAsyncWriteLoop()
    {
        await foreach (ProtocolMessage packet in TaskQueue.Reader.ReadAllAsync())
        {
            byte[] buffer = ProtocolSerializer.Serialize(packet);
            await _networkStream.WriteAsync(buffer, 0, buffer.Length);   
            _logger.LogInformation("Wrote: {0} to {1}:{2}",ProtocolSerializer.ReadableSerialize(packet),_clientAddress,_clientPort);
        }
    }
    
}