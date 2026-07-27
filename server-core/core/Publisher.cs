using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;

namespace server_core.core;

/// <summary>
/// pub class for pub-sub
/// </summary>
/// <param name="logger">Logger ig</param>
public class Publisher(ILogger logger)
{
    private readonly HashSet<Worker> _workers = new();
    private readonly Channel<ProtocolMessage> _taskQueue = Channel.CreateUnbounded<ProtocolMessage>();
    private readonly Lock _lock = new();
    private Task _writeLoop;
    private bool IsStarted = false;
    
    /// <summary>
    /// Adding a worker to the set of workers
    /// </summary>
    /// <param name="worker">The worker that was created</param>
    /// <returns>T/F if the worker could or couldn't be added</returns>
    public bool AddWorker(Worker worker)
    {
        lock (_lock)
        {
            logger.LogInformation("Adding worker to publisher {0}:{1}",worker.ClientAddress.MapToIPv4().ToString(),worker._clientPort);
            return _workers.Add(worker);
        }
    }
    
    /// <summary>
    /// Adding a message the queue
    /// </summary>
    /// <param name="message">Message to be broadcasted</param>
    /// <returns>T/F if it could be added</returns>
    /// TODO: Make extra logic so i can broadcast to select people or all
    public bool AddMessage(ProtocolMessage message)
    {
        logger.LogInformation("Added message to pub queue: {0}",ProtocolSerializer.ReadableSerialize(message));
        return _taskQueue.Writer.TryWrite(message);
    }
    /// <summary>
    /// Start up AsyncWriteLoop when needed, IDK why I added
    /// </summary>
    public void Start()
    {
        if (!IsStarted)
        {
             _writeLoop = StartAsyncWriteLoop();
        }
    }
    /// <summary>
    /// Loop that goes through each packet and broadcasts it to all people
    /// </summary>
    private async Task StartAsyncWriteLoop()
    {
        await foreach (ProtocolMessage packet in _taskQueue.Reader.ReadAllAsync())
        {
            lock (_lock)
            {
                logger.LogInformation("Pub: {0} to all",ProtocolSerializer.ReadableSerialize(packet));
                foreach (var worker in _workers)
                {
                    worker.Connection.AddTask(packet);
                }
            }
        }
    }
    
    
}