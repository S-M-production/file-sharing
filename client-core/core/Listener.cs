using System.Net;
using System.Net.Sockets;
using client_core.router;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core.core;
/// <summary>
/// Class that listens to one single valid connection and initiates request pipeline
/// </summary>
public class Listener
{
    private readonly ILogger _logger;
    private readonly IPAddress _clientAddress;
    private readonly int _clientPort;
    private readonly NetworkStream _stream;
    private readonly Parser _parser;
    private Connection _connection;
    private RouterMap _routerMap;

    /// <summary>
    /// Creates NetworkStream and  saves logger, IP, and port
    /// </summary>
    /// <param name="tcpClient">TcpClient of connection to valid server</param>
    /// <param name="logger">The logger passed down from initial project creation</param>
    /// <param name="connection">A connection object for writing to client</param>
    /// <exception cref="IOException">When an improper TcpClient is inputted, one that doesn't return IP:PORT</exception>
    public Listener(TcpClient tcpClient, ILogger logger,Connection connection)
    {
        this._logger = logger;
        this._connection = connection;
        
        IPEndPoint? clientInfo = tcpClient.Client.RemoteEndPoint as IPEndPoint;
        if (clientInfo == null) throw new IOException("Improper Connection???");
        
        _clientAddress = clientInfo.Address.MapToIPv4();
        _clientPort = clientInfo.Port;

        _stream = tcpClient.GetStream();
        _parser = new Parser(_stream);
        _routerMap = new RouterMap();
    }
    /// <summary>
    /// Runs a listening loop 
    /// </summary>
    /// <remarks>
    /// Async loop that waits for a request from server
    /// Parses it
    /// If issue happens while parsing then its disconnected
    /// Message is passed through middle ware, and response is created and sent to connection
    /// </remarks>
    public async Task Run()
    {
        while (true)
        {
            ProtocolMessage message;
            try
            {
                message = await _parser.Parse();
            }
            catch (IOException e)
            {
                _logger.LogTrace("Issue handling... Disconnecting {Address}:{Port} \n{Error}",_clientAddress,_clientPort,e.StackTrace);
                _stream?.Close();
                return;
            }
        
            _logger.LogInformation("Got message: {} {}:{}",ProtocolSerializer.ReadableSerialize(message),_clientAddress,_clientPort);
        
            //TODO: Create routing layer and create middleware
            ProtocolMessage? response = middleware.Middleware.GetResponse(message,_routerMap);
        
            if (response == null)  continue;
        
            await _stream.WriteAsync(ProtocolSerializer.Serialize(response));  
        }
        
    }
}