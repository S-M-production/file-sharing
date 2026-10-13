using System.Diagnostics;
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
    /// Creates a connection to another computer, can be a user or a server and returns a connection object if it could be validated 
    /// </summary>
    /// <param name="ip">IP address</param>
    /// <param name="port">Port number</param>
    /// <param name="logger">ILogger that was created at initialization</param>
    /// <param name="middleware">Middleware created by program utilizing connector</param>
    /// <returns>Returns a connection object when connection could be validated</returns>
    public static async Task<Connection?> Connect(string ip, int port, ILogger logger, IMiddleware middleware, RouterMap routerMap, ConnectionType connectionType = ConnectionType.Server)
    {
        switch(connectionType)
        {
            case ConnectionType.Server:
                return await ConnectToServer(ip, port, logger, middleware, routerMap);
            case ConnectionType.Tcp:
                return await ConnectToUserTcp(ip, port, logger, middleware, routerMap);
            case ConnectionType.Udp:
                break;
            case ConnectionType.Relay:
                break;
        }

        return null;
    }
    /// <summary>
    /// Creates a connection to a user and validates the user, returns a Connection object if it could be validated 
    /// </summary>
    /// <param name="ip">IP address</param>
    /// <param name="port">Port number</param>
    /// <param name="logger">ILogger that was created at initialization</param>
    /// <param name="middleware">Middleware created by program utilizing connector</param>
    /// <param name="routerMap">RouterMap that was created at initialization</param>
    /// <returns>Returns a connection object when connection could be validated</returns>
    private static async Task<Connection?> ConnectToUserTcp(string ip, int port, ILogger logger, IMiddleware middleware, RouterMap routerMap)
    {
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout)); 
        CancellationToken ct = cts.Token;
        
        var client = new TcpClient();
        await client.ConnectAsync(ip, port,ct);
        
        var stream = client.GetStream();
        
        byte[] request = ProtocolSerializer.Serialize(MessageType.ConnectToUser,"");
        
        await stream.WriteAsync(request, 0, request.Length);
        await stream.FlushAsync();
        
        Parser parser = new Parser(stream);
        ProtocolMessage response = await parser.Parse();
        //TODO: Replace the CancelationTokenSource with DI
        if (response.MessageType == MessageType.ConnectedToUser) return new Connection(client, logger,middleware, routerMap);
        logger.LogError($"Failed to connect to {ip}:{port}, {ProtocolSerializer.ReadableSerialize(response)}");
        return null;
    }

    /// <summary>
    /// Creates a connection to the server and validates the server, returns a Connection object if it could be validated 
    /// </summary>
    /// <param name="ip">IP address</param>
    /// <param name="port">Port number</param>
    /// <param name="logger">ILogger that was created at initialization</param>
    /// <param name="middleware">Middleware created by program utilizing connector</param>
    /// <param name="routerMap">RouterMap that was created at initialization</param>
    /// <returns>Returns a connection object when connection could be validated</returns>
    private static async Task<Connection?> ConnectToServer(string ip, int port, ILogger logger, IMiddleware middleware, RouterMap routerMap)
    {
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout)); 
        CancellationToken ct = cts.Token;
        
        var client = new TcpClient();
        await client.ConnectAsync(ip, port,ct);
        
        var stream = client.GetStream();
        
        byte[] request = ProtocolSerializer.Serialize(MessageType.ConnectToServer,"");
        
        await stream.WriteAsync(request, 0, request.Length);
        await stream.FlushAsync();
        
        Parser parser = new Parser(stream);
        ProtocolMessage response = await parser.Parse();
        //TODO: Replace the CancelationTokenSource with DI
        if (response.MessageType == MessageType.ConnectedToServer) return new Connection(client, logger,middleware, routerMap);
        logger.LogError($"Failed to connect to {ip}:{port}, {ProtocolSerializer.ReadableSerialize(response)}");
        return null;
    }
    
}