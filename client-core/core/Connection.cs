using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core.core;
/// <summary>
/// Class that houses the reader and writer for a connection
/// </summary>
/// <remarks>
/// Connection object shouldnt be created, rather it should be recieved from the Connector class and used as Connector validates the server
/// To write to connection you add a ProtocolMessage object to the queue through AddTask and the rest is handled
/// </remarks>
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
    /// <summary>
    /// Sets up listening and writing loop for the connection
    /// </summary>
    /// <param name="client">TcpClient connection, Ideally should be a server connection that is validated through Connector</param>
    /// <param name="logger">ILogger that is passed down into here</param>
    /// TODO: Use .NET DI for logger
    public Connection(TcpClient client,ILogger logger)
    {
        IPEndPoint clientInfo = client.Client.RemoteEndPoint as IPEndPoint;
        clientAddress = clientInfo.Address.MapToIPv4();
        clientPort = clientInfo.Port;
        
        this.client = client;
        networkStream = client.GetStream();
        listenerTask = new Listener(client,logger,this).Run();
        asyncLoopTask = StartAsyncWriteLoop();
        heartBeat = new HeartBeat(this);
        this.logger = logger;
    }
    /// <summary>
    /// Puts message into a ordered queue that will serialize messages one at a time 
    /// </summary>
    /// <param name="protocolMessage">Message that needs to be sent</param>
    public async Task AddTask(ProtocolMessage protocolMessage)
    {
        await taskQueue.Writer.WriteAsync(protocolMessage);
    }
    /// <summary>
    /// Way to end the queue
    /// </summary>
    public async Task CompleteQueue()
    {
        taskQueue.Writer.Complete();
    }
    /// <summary>
    /// Starting up async writing loop, this loop will take a message at a time out of the queue and serialize it. Should only be ran once
    /// </summary>
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