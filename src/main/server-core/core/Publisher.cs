using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;

namespace server_core.core;

/// <summary>Publisher for broadcasting <see cref="ProtocolMessage"/> instances to connected workers.</summary>
/// <param name="logger">Logger instance used for internal logging.</param>
public class Publisher(ILogger logger)
{
    /// <summary>Set of connected workers subscribed to receive messages.</summary>
    private readonly HashSet<Worker> _workers = new();

    /// <summary>Unbounded channel used as the queue for outgoing <see cref="ProtocolMessage"/> items.</summary>
    private readonly Channel<ProtocolMessage> _taskQueue = Channel.CreateUnbounded<ProtocolMessage>();

    /// <summary>Lock object used to synchronize access to shared resources.</summary>
    private readonly Lock _lock = new();

    /// <summary>Background task running the write loop that dispatches messages.</summary>
    private Task _writeLoop;

    /// <summary>Indicates whether the publisher has started its write loop.</summary>
    private bool IsStarted = false;
    
    /// <summary>Adds a worker to the publisher's subscriber set.</summary>
    /// <param name="worker">Worker to add.</param>
    /// <returns>True if the worker was added; false if it was already present.</returns>
    public bool AddWorker(Worker worker)
    {
        lock (_lock)
        {
            logger.LogInformation("Adding worker to publisher {0}:{1}",worker.ClientAddress.MapToIPv4().ToString(),worker._clientPort);
            return _workers.Add(worker);
        }
    }
    
    /// <summary>Enqueues a message for broadcast to subscribers.</summary>
    /// <param name="message">ProtocolMessage to enqueue.</param>
    /// <returns>True if the message was written to the queue; otherwise false.</returns>
    // TODO: Make extra logic so i can broadcast to select people or all
    public bool AddMessage(ProtocolMessage message)
    {
        logger.LogInformation("Added message to pub queue: {0}",ProtocolSerializer.ReadableSerialize(message));
        return _taskQueue.Writer.TryWrite(message);
    }
    /// <summary>Starts the background write loop if not already running.</summary>
    public void Start()
    {
        if (!IsStarted)
        {
             _writeLoop = StartAsyncWriteLoop();
        }
    }
    /// <summary>Asynchronously reads messages from the queue and broadcasts them to workers.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous loop.</returns>
    private async Task StartAsyncWriteLoop()
    {
        await foreach (ProtocolMessage packet in _taskQueue.Reader.ReadAllAsync())
        {
            lock (_lock)
            {
                logger.LogInformation("Pub: {0} to all",ProtocolSerializer.ReadableSerialize(packet));
                foreach (var worker in _workers)
                {
                    var temp = worker.ClientAddress.MapToIPv4().ToString() + ":" + worker._clientPort;
                    if (System.Text.Encoding.UTF8.GetString(packet.Body) == temp) continue;
                    worker.Connection.AddTask(packet);
                }
            }
        }
    }
    
    
}