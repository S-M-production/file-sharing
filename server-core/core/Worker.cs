using System.Net.Sockets;
using format;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Net;
using stun_server.format;
using stun_server.logic;
using stun_server.middleware;

namespace core
{

    public class Worker(TcpClient tcpClient, ILogger logger, ThreadSafeHasset connections)
    {
        private byte[]? _body;
        private IPAddress clientAddress;
        private int clientPort;
        public async Task Run()
        {
            RegisterUserConnection();
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
                    connections.Remove($"{clientAddress}:{clientPort}");
                    return;
                }
            
                logger.LogInformation("Got message: {} {}:{}",ProtocolSerializer.ReadableSerialize(message),clientAddress,clientPort);
            
                //TODO: Create routing layer and create middleware
                ProtocolMessage? response = middleware.GetResponse(message, connections);
            
                if (response == null)  return;
            
                await stream.WriteAsync(ProtocolSerializer.Serialize(response));  
                Console.Write("Wrote: ");
                Console.Write(ProtocolSerializer.ReadableSerialize(response));
                Console.WriteLine(" To {0} {1}:{2}",Encoding.UTF8.GetString(message.Body),clientAddress,clientPort);
            }
            
        }

        private void RegisterUserConnection()
        {
            IPEndPoint? clientInfo = tcpClient.Client.RemoteEndPoint as IPEndPoint;

            if (clientInfo?.Address == null || clientInfo.Port <= 0)
            {
                logger.LogWarning("Client connection has no endpoint information. Closing connection.");
                tcpClient.Close();
                return;
            }
            
            clientAddress = clientInfo.Address;
            clientPort = clientInfo.Port;
            
            logger.LogInformation("Worker handling client {}:{}",clientAddress,clientPort);
            connections.Add($"{clientAddress}:{clientPort}");
        }
    }
}