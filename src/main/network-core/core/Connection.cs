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
    /// Listener instance responsible for reading and processing incoming messages from the client.
    /// </summary>
    private readonly Listener _listener;
    /// <summary>
    /// Task reference for the listener's async read loop.
    /// </summary>
    private Task _listenerTask = null!;
    /// <summary>
    /// The writer instance for sending messages over the connection.
    /// </summary>
    public readonly Writer Writer;
    private Task _asyncWriteLoopTask = null!;
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
        Writer = new Writer(logger, client.GetStream(), clientInfo.Address.MapToIPv4(), clientInfo.Port, _awaitTime);
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
        _asyncWriteLoopTask = Writer.StartAsyncWriteLoop();
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
        _ = Writer.AddTask(new ProtocolMessage(MessageType.Disconnect));
        Writer.CompleteQueue();
        await _listenerTask;
        await _asyncWriteLoopTask;
        _client.Close();
        return true;
    }
}