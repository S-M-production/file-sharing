using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using network_core.core;
using router_core.core;
using server_core.logic;
using server_core.middleware;

namespace server_core.core;

/// <summary>
/// Manages a single client connection, handling message routing, heartbeat, and lifecycle.
/// </summary>
public class Worker
{
    /// <summary>
    /// IPv4 address of the connected client for identification
    /// </summary>
    public IPAddress ClientAddress {get; private set;}
    /// <summary>
    /// Port number of the connected client for identification
    /// </summary>
    public int _clientPort {get; private set;}
    /// <summary>
    /// Connection handler managing read/write operations for this client
    /// </summary>
    public readonly Connection Connection;
    /// <summary>
    /// Middleware processing pipeline for incoming messages from this client
    /// </summary>
    private readonly Middleware _middleware;
    /// <summary>
    /// TCP client socket for communication with the connected client
    /// </summary>
    private TcpClient _tcpClient;
    /// <summary>
    /// Logger instance for recording worker events and errors
    /// </summary>
    private ILogger _logger;
    /// <summary>
    /// Task reference for the disposal cleanup loop
    /// </summary>
    private Task disposeAwaitLoopTask;
    /// <summary>
    /// Concurrent dictionary of all connected users across all workers
    /// </summary>
    private readonly UserList _connections;
    /// <summary>
    /// Router map for dispatching messages to registered handlers
    /// </summary>
    private readonly RouterMap _router = new();
    /// <summary>
    /// Heartbeat manager for keeping the connection alive and detecting disconnects
    /// </summary>
    private readonly HeartBeat _heartBeat;
    /// <summary>
    /// Task reference for the heartbeat loop
    /// </summary>
    private Task _heartBeatLoop = null!;
    /// <summary>
    /// Completion source signaling when worker should begin graceful shutdown
    /// </summary>
    private readonly TaskCompletionSource _cancellationToken = new();
    
    /// <summary>
    /// Initializes a Worker with a TCP client and sets up message routing and heartbeat.
    /// </summary>
    /// <param name="tcpClient">Connection to client</param>
    /// <param name="logger">Logger created at the start of program</param>
    /// <param name="connections">List of all connections</param>
    public Worker(TcpClient tcpClient, ILogger logger, UserList connections)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        this._connections = connections;
        _router.AddRoute(format.core.MessageType.Disconnect, new UserRemoval(_connections).Remove);
        _middleware = new Middleware(connections, tcpClient.Client.RemoteEndPoint as IPEndPoint);
        Connection= new Connection(tcpClient,logger,_middleware, _router);
        _heartBeat = new HeartBeat(Connection,logger, _cancellationToken);
        var temp = (tcpClient.Client.RemoteEndPoint as IPEndPoint)!;
        ClientAddress = temp.Address.MapToIPv4();
        _clientPort = temp.Port;
    }
     

    /// <summary>
    /// Registers user connection, starts heartbeat and connection loops.
    /// </summary>
    public void Run()
    {
        _logger.LogInformation($"Worker {ClientAddress}:{_clientPort} running...");
        RegisterUserConnection();
        _heartBeat.StartHeartBeatLoop();
        _heartBeatLoop = _heartBeat.HeartBeatLoopTask;
        Connection.Start();
        disposeAwaitLoopTask = DisposeAwaitLoop();
    }

    /// <summary>
    /// Awaits cancellation signal then gracefully shuts down the connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DisposeAwaitLoop()
    {
        await _cancellationToken.Task;
        _logger.LogInformation("Removing {}:{}", ClientAddress,_clientPort);
        RemoveRegisteredUser();
        await Connection.GracefulStop();
        _logger.LogInformation("Gracefully stopped {}:{}", ClientAddress,_clientPort);
    }
    
    /// <summary>
    /// Removes the registered user from the connections list by endpoint key.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private void RemoveRegisteredUser()
    {
        _connections.TryRemove($"{ClientAddress}:{_clientPort}", out _);
    }
    
    /// <summary>
    /// Registers the client connection in the concurrent user list for discovery.
    /// </summary>
    private void RegisterUserConnection()
    {
        IPEndPoint? clientInfo = _tcpClient.Client.RemoteEndPoint as IPEndPoint;

        if (clientInfo?.Address == null || clientInfo.Port <= 0)
        {
            _logger.LogWarning("Client connection has no endpoint information. Closing connection.");
            _tcpClient.Close();
            return;
        }
        
        ClientAddress = clientInfo.Address;
        _clientPort = clientInfo.Port;
        
        _logger.LogInformation("Worker handling client {}:{}",ClientAddress,_clientPort);
        _connections.TryAdd($"{ClientAddress}:{_clientPort}",this);
    }
}
