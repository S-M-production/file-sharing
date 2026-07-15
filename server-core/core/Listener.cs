using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using stun_server.logic;

namespace core
{
    public class Listener
    {
        private readonly IPAddress address;
        private readonly int port;
        private readonly TcpListener tcpListener;
        private readonly ILogger logger;

        public Listener(IPAddress address, int port, ILogger logger)
        {
            this.address = address;
            this.port = port;
            this.logger = logger;
            this.tcpListener = new TcpListener(address, port);
        }

        public async Task Run()
        {
            tcpListener.Start();

            logger.LogInformation("Started listening on {Address}:{Port}", address, port);

            var connections = new ThreadSafeHasset();
            
            while(true)
            {
                var client = await tcpListener.AcceptTcpClientAsync();
                
                logger.LogInformation(
                    "Client connected: {Client}",
                    client.Client.RemoteEndPoint);

                Worker worker = new Worker(client,logger, connections);
                Task workerInstance = worker.Run();
            }
        }
    }
}