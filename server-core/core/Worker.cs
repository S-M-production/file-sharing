using System.Net;
using System.Net.Sockets;
using System.Text;
using format.core;
using Microsoft.Extensions.Logging;
using network_core.core;
using server_core.logic;
using server_core.middleware;

namespace server_core.core;

/// <summary>
/// Class that owns a single connection
/// </summary>
/// <param name="tcpClient">Connection to client</param>
/// <param name="logger">Logger created at the start of program</param>
/// <param name="connections">List of all connections</param>
public class Worker
{
    private IPAddress _clientAddress;
    private int _clientPort;
    /// <summary>
    /// Connection object created at creation of worker, houses logic for communication
    /// </summary>
    public readonly Connection Connection;
    private readonly Middleware _middleware;
    private TcpClient _tcpClient;
    private ILogger _logger;
    private readonly UserList _connections;
    public Worker(TcpClient tcpClient, ILogger logger, UserList connections)
    {
        _tcpClient = tcpClient;
        _logger = logger;
        this._connections = connections;
        _middleware = new Middleware();
        Connection= new Connection(tcpClient,logger,_middleware);
        var temp = (tcpClient.Client.RemoteEndPoint as IPEndPoint)!;
        _clientAddress = temp.Address.MapToIPv4();
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
        Connection.Start();
        
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
        
        _clientAddress = clientInfo.Address;
        _clientPort = clientInfo.Port;
        
        _logger.LogInformation("Worker handling client {}:{}",_clientAddress,_clientPort);
        _connections.Connections.TryAdd($"{_clientAddress}:{_clientPort}",this);
    }
}
