using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using network_core.core;
using router_core.core;
using server_core.logic;
using server_core.middleware;

namespace server_core.core;

/// <summary>
/// Class that owns a single connection
/// </summary>
public class Worker
{
    /// <summary>
    /// Be able to distinguish workers using this
    /// </summary>
    public IPAddress ClientAddress {get; private set;}
    /// <summary>
    /// Be able to distinguish workers using this
    /// </summary>
    public int _clientPort {get; private set;}
    /// <summary>
    /// Connection object created at creation of worker, houses logic for communication
    /// </summary>
    public readonly Connection Connection;
    private readonly Middleware _middleware;
    private TcpClient _tcpClient;
    private ILogger _logger;
    private readonly UserList _connections;
    private readonly RouterMap _router;
    
    /// <summary>Constructor</summary>
    /// <param name="tcpClient">Connection to client</param>
    /// <param name="logger">Logger created at the start of program</param>
    /// <param name="connections">List of all connections</param>
    public Worker(TcpClient tcpClient, ILogger logger, UserList connections)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        _router = new ();
        _router.AddRoute(format.core.MessageType.Disconnect, new UserRemoval(_connections).Remove);
        this._connections = connections;
        _middleware = new Middleware(connections, (tcpClient.Client.RemoteEndPoint as IPEndPoint)!);
        Connection= new Connection(tcpClient,logger,_middleware, _router);
        var temp = (tcpClient.Client.RemoteEndPoint as IPEndPoint)!;
        ClientAddress = temp.Address.MapToIPv4();
        _clientPort = temp.Port;
    }
     

    /// <summary>
    /// Registers users connection
    /// Then actively listens and forwards responses to middleware
    /// Then responds with what middleware responded with
    /// </summary>
    public void Run()
    {
        RegisterUserConnection();
        Connection.Start(false);
        
    }
    /// <summary>
    /// Registers the users connection in the concurrent hashset
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
