using System.Net.Sockets;
using format.core;
using Microsoft.Extensions.Logging;
using router_core.core;
using router_core.middleware;

namespace network_core.core;
/// <summary>
/// Class has static objests as connector doesnt require object lifecycle as it only server purpose of validating and returning a connection to a server
/// </summary>
public static class Connector
{
    /// <summary>
    /// Amount of time the Connector will wait for connection to be created before ending it and returning a exception
    /// </summary>
    private const int Timeout = 1;

    /// <summary>
    /// Creates a connection to the server and validates the server, returns a Connection object if it could be validated 
    /// </summary>
    /// <param name="server">IP address</param>
    /// <param name="port">Port number</param>
    /// <param name="logger">ILogger that was created at initialization</param>
    /// <param name="middleware">Middleware created by program utilizing connector</param>
    /// <returns>Returns a connection object when a validated connection is estabalished</returns>
    /// <exception cref="TimeoutException">When connection takes over more time than timeout was set to, this is thrown</exception>
    /// <exception cref="Exception">Returns exception when server couldn't be validated</exception>
    public static async Task<Connection?> Connect(string server, int port, ILogger logger, IMiddleware middleware)
    {
        Task<Connection?> result = ConnectToServer(server, port, logger, middleware);
        await Task.Delay(Timeout*1000);
        if(!result.IsCompleted) throw new TimeoutException("Timeout waiting for connection");
        return result.Result;
    }
    /// <summary>
    /// Logic to connect to server is housed here 
    /// </summary>
    /// <param name="server"></param>
    /// <param name="port"></param>
    /// <param name="logger"></param>
    /// <returns>Returns a connection object when connection could be validated</returns>
    private static async Task<Connection?> ConnectToServer(string server, int port, ILogger logger, IMiddleware middleware)
    {
        var client = new TcpClient();
        await client.ConnectAsync(server, port);
        
        var stream = client.GetStream();
        
        byte[] request = ProtocolSerializer.Serialize(MessageType.Connect,"");
        
        await stream.WriteAsync(request, 0, request.Length);
        await stream.FlushAsync();
        
        Parser parser = new Parser(stream);
        ProtocolMessage response = await parser.Parse();
        
        if (response.MessageType == MessageType.ConnectedToServer) return new Connection(client, logger,middleware, new RouterMap());
        logger.LogError($"Failed to connect to {server}:{port}, {ProtocolSerializer.ReadableSerialize(response)}");
        return null;
    }
    
}