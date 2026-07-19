using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using server_core.logic;

namespace server_core.core;
/// <summary>
/// Listens for connections and spawns workers for each connection
/// </summary>
public class Listener(IPAddress address, int port, ILogger logger)
{
    private readonly TcpListener _tcpListener = new(address, port);
    /// <summary>
    /// Starts listening and spawns a worker per client connection
    /// </summary>
    public async Task Run()
    {
        _tcpListener.Start();

        logger.LogInformation("Started listening on {Address}:{Port}", address, port);

        var connections = new ThreadSafeHasset();
        
        while(true)
        {
            var client = await _tcpListener.AcceptTcpClientAsync();
            
            logger.LogInformation(
                "Client connected: {Client}",
                client.Client.RemoteEndPoint);

            Worker worker = new Worker(client,logger, connections);
            _ = worker.Run();
        }
    }
}
