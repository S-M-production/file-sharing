using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core.core;

public class Connection
{
    NetworkStream networkStream;
    public Channel<ProtocolMessage> taskQueue = Channel.CreateUnbounded<ProtocolMessage>();
    Task asyncLoopTask;
    Task listenerTask;
    private readonly TcpClient client;
    private HeartBeat heartBeat;
    private ILogger logger;
    private IPAddress clientAddress;
    private int clientPort;
    public Connection(TcpClient client,ILogger logger)
    {
        IPEndPoint clientInfo = client.Client.RemoteEndPoint as IPEndPoint;
        clientAddress = clientInfo.Address.MapToIPv4();
        clientPort = clientInfo.Port;
        
        this.client = client;
        networkStream = client.GetStream();
        listenerTask = new Listener(client,logger).Run();
        asyncLoopTask = StartAsyncWriteLoop();
        heartBeat = new HeartBeat(this);
        this.logger = logger;
    }
    public async Task AddTask(ProtocolMessage protocolMessage)
    {
        await taskQueue.Writer.WriteAsync(protocolMessage);
    }

    public async Task CompleteQueue()
    {
        taskQueue.Writer.Complete();
    }
    
    async Task StartAsyncWriteLoop()
    {
        await foreach (ProtocolMessage packet in taskQueue.Reader.ReadAllAsync())
        {
            byte[] buffer = ProtocolSerializer.Serialize(packet);
            await networkStream.WriteAsync(buffer, 0, buffer.Length);   
            logger.LogInformation("Wrote: {0} to {1}:{2}",ProtocolSerializer.ReadableSerialize(packet),clientAddress,clientPort);
        }
    }
    
}