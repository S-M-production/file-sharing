using System.Net;
using System.Net.Sockets;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core.core
{

    public class Listener(TcpClient tcpClient, ILogger logger)
    {
        private byte[]? _body;
        private IPAddress clientAddress;
        private int clientPort;
        public async Task Run()
        {
            IPEndPoint clientInfo = tcpClient.Client.RemoteEndPoint as IPEndPoint;
            clientAddress = clientInfo.Address.MapToIPv4();
            clientPort = clientInfo.Port;
            
            await using var stream = tcpClient.GetStream();
            
            ProtocolMessage message;
            var parser = new Parser(stream);

            while (true)
            {
                try
                {
                    message = await parser.Parse();
                }catch (IOException e)
                {
                    logger.LogWarning("Issue handling... disconnection {}:{}",clientAddress,clientPort);
                    logger.LogTrace(e.StackTrace);
                    return;
                }
            
                logger.LogInformation("Got message: {} {}:{}",ProtocolSerializer.ReadableSerialize(message),clientAddress,clientPort);
            
                //TODO: Create routing layer and create middleware
                ProtocolMessage? response = middleware.Middleware.GetResponse(message);
            
                if (response == null)  continue;
            
                await stream.WriteAsync(ProtocolSerializer.Serialize(response));  
            }
            
        }
    }
}