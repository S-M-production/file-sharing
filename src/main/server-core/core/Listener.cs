using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using network_core.core;
using server_core.logic;
using server_core.middleware;

namespace server_core.core;
/// <summary>
/// Listens for incoming TCP connections and spawns worker threads to handle each client.
/// </summary>
public class Listener(IPAddress address, int port, ILogger logger)
{
    /// <summary>
    /// TCP listener bound to the specified address and port
    /// </summary>
    private readonly TcpListener _tcpListener = new(address, port);
    /// <summary>
    /// Publisher for broadcasting user list updates to all connected clients
    /// </summary>
    private readonly Publisher _publisher = new(logger);
    
    /// <summary>
    /// Starts listening and spawns a worker per client connection
    /// </summary>
    public async Task Run()
    {
        _tcpListener.Start();

        logger.LogInformation("Started listening on {Address}:{Port}", address, port);

        var connections = new UserList(_publisher);
        _publisher.Start();
        while(true)
        {
            var client = await _tcpListener.AcceptTcpClientAsync();
            
            logger.LogInformation(
                "Client connected: {Client}",
                client.Client.RemoteEndPoint);
            Worker worker = new Worker(client,logger, connections);
            worker.Run();
            _publisher.AddWorker(worker);
        }
    }
}
