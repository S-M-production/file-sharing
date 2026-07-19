using System.Net;
using System.Net.Sockets;
using System.Text;
using format.core;
using Microsoft.Extensions.Logging;
using server_core.logic;

namespace server_core.core;

public class Worker(TcpClient tcpClient, ILogger logger, ThreadSafeHasset connections)
{
    private IPAddress _clientAddress = ((tcpClient.Client.RemoteEndPoint as IPEndPoint)!).Address.MapToIPv4();
    private int _clientPort = ((tcpClient.Client.RemoteEndPoint as IPEndPoint)!).Port;
    /// <summary>
    /// Registers users connection
    /// Then actively listens and forwards responses to middleware
    /// Then responds with what middleware responded with
    /// </summary>
    public async Task Run()
    {
        RegisterUserConnection();
        await using var stream = tcpClient.GetStream();

        var parser = new Parser(stream);

        while (true)
        {
            ProtocolMessage message;
            try
            {
                message = await parser.Parse();
            }catch (IOException e)
            {
                logger.LogWarning("Issue handling... disconnection {}:{}",_clientAddress,_clientPort);
                logger.LogTrace(e.StackTrace);
                connections.Remove($"{_clientAddress}:{_clientPort}");
                return;
            }
        
            logger.LogInformation("Got message: {} {}:{}",ProtocolSerializer.ReadableSerialize(message),_clientAddress,_clientPort);
        
            //TODO: Create routing layer and create middleware
            ProtocolMessage? response = middleware.Middleware.GetResponse(message, connections);
        
            if (response == null)  return;
        
            await stream.WriteAsync(ProtocolSerializer.Serialize(response));  
            Console.Write("Wrote: ");
            Console.Write(ProtocolSerializer.ReadableSerialize(response));
            Console.WriteLine(" To {0} {1}:{2}",Encoding.UTF8.GetString(message.Body),_clientAddress,_clientPort);
        }
        
    }
    /// <summary>
    /// Registers the users connection in the concurrent hashset
    /// </summary>
    private void RegisterUserConnection()
    {
        IPEndPoint? clientInfo = tcpClient.Client.RemoteEndPoint as IPEndPoint;

        if (clientInfo?.Address == null || clientInfo.Port <= 0)
        {
            logger.LogWarning("Client connection has no endpoint information. Closing connection.");
            tcpClient.Close();
            return;
        }
        
        _clientAddress = clientInfo.Address;
        _clientPort = clientInfo.Port;
        
        logger.LogInformation("Worker handling client {}:{}",_clientAddress,_clientPort);
        connections.Add($"{_clientAddress}:{_clientPort}");
    }
}
