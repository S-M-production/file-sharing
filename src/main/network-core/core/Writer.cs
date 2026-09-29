using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using format.core;
using Microsoft.Extensions.Logging;
using network_core.call_back;

namespace network_core.core;

public class Writer
{
    /// <summary>
    /// Thread-safe unbounded queue for serializing and sending messages in order to the client.
    /// </summary>
    private readonly Channel<CallBackTask> _taskQueue = Channel.CreateUnbounded<CallBackTask>();
    /// <summary>
    /// High-priority unbounded queue that bypasses normal message queue ordering for urgent messages.
    /// </summary>
    private readonly Channel<CallBackTask> _priorityTaskQueue = Channel.CreateUnbounded<CallBackTask>();
    /// <summary>
    /// Logger instance for recording connection events, errors, and diagnostics.
    /// </summary>
    private readonly ILogger _logger;
    /// <summary>
    /// Completion source that signals when the async write loop has finished processing all queued messages.
    /// </summary>
    private readonly TaskCompletionSource _isWriterCompleted = new TaskCompletionSource();
    /// <summary>
    /// Network stream for reading and writing data over the TCP connection.
    /// </summary>
    private readonly NetworkStream _networkStream;
    /// <summary>
    /// Maximum timeout in seconds for individual write operations before raising a TimeoutException.
    /// </summary>
    private readonly int _awaitTime;
    /// <summary>
    /// The IPv4 address of the connected client.
    /// </summary>
    private readonly IPAddress _clientAddress;
    /// <summary>
    /// The port number of the connected client.
    /// </summary>
    private readonly int _clientPort;

    /// <summary>
    /// Initializes a new instance of the <see cref="Writer"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for recording connection events, errors, and diagnostics.</param>
    /// <param name="networkStream">The network stream to use for writing data.</param>
    /// <param name="clientAddress">The IPv4 address of the connected client.</param>
    /// <param name="clientPort">The port number of the connected client.</param>
    /// <param name="awaitTime">The maximum timeout in seconds for individual write operations before raising a TimeoutException.</param>
    public Writer(ILogger logger, NetworkStream networkStream, IPAddress clientAddress, int clientPort, int awaitTime)
    {
        this._logger = logger;
        this._networkStream = networkStream;
        this._clientAddress = clientAddress;
        this._clientPort = clientPort;
        _awaitTime = awaitTime;
    }
    
    /// <summary>
    /// Puts message into a ordered queue that will serialize messages one at a time 
    /// </summary>
    /// <param name="protocolMessage">Message that needs to be sent</param>
    /// <param name="priority">If true, message is queued in priority queue instead of normal queue.</param>
    /// <returns>TaskCompletionSource that completes when the message has been written.</returns>
    public TaskCompletionSource<bool> AddTask(ProtocolMessage protocolMessage, bool priority = false)
    {
        TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        var task = new CallBackTask(protocolMessage, tcs);
        if (priority)
            _priorityTaskQueue.Writer.TryWrite(task);
        else
            _taskQueue.Writer.TryWrite(task);
        return tcs;
    }
    /// <summary>
    /// Way to end the queue
    /// </summary>
    /// <returns>True if the task queue was successfully completed.</returns>
    public bool CompleteQueue()
    {
        _priorityTaskQueue.Writer.TryComplete();
        return _taskQueue.Writer.TryComplete();
    }

    /// <summary>
    /// Waits for the async write loop to complete all pending operations.
    /// </summary>
    public async Task CompleteCallBack()
    {
        await _isWriterCompleted.Task;
        
    }
    
    /// <summary>
    /// Starting up async writing loop, this loop will take a message at a time out of the queue and serialize it. Should only be ran once
    /// </summary>
    /// <returns>A task that completes when the write loop finishes processing all queued messages.</returns>
    async Task StartAsyncWriteLoop()
    {
        CallBackTask? call;
        while ((call = await TryReadNextTask()) != null)
        {
            byte[] buffer = ProtocolSerializer.Serialize(call.ProtocolMessage);
            try
            {
                await _networkStream.WriteAsync(buffer, 0, buffer.Length).WaitAsync(TimeSpan.FromSeconds(_awaitTime));
            }
            catch (Exception e) when (e is IOException || e is TimeoutException)
            {
                _logger.LogError(e.Message);
                call.SetCompletionSource(false);
                continue;
            }catch (Exception e)
            {
                _logger.LogError(e.Message);
                call.SetCompletionSource(false);
                continue;
            }
            _logger.LogInformation("Wrote: {0} to {1}:{2}",ProtocolSerializer.ReadableSerialize(call.ProtocolMessage),_clientAddress,_clientPort);
            call.SetCompletionSource();
        }
        _isWriterCompleted.TrySetResult();
    }

    /// <summary>
    /// Attempts to read the next task from either priority or normal queue, waiting if neither has data.
    /// </summary>
    /// <returns>The next CallBackTask from priority queue if available, otherwise from normal queue, or null if both queues are completed.</returns>
    private async Task<CallBackTask?> TryReadNextTask()
    {
        while (true)
        {
            if (_priorityTaskQueue.Reader.TryRead(out CallBackTask? priorityTask))
                return priorityTask;
            if (_taskQueue.Reader.TryRead(out CallBackTask? task))
                return task;

            if (_priorityTaskQueue.Reader.Completion.IsCompleted && _taskQueue.Reader.Completion.IsCompleted)
                return null;

            await Task.WhenAny(
                _priorityTaskQueue.Reader.WaitToReadAsync().AsTask(),
                _taskQueue.Reader.WaitToReadAsync().AsTask());
        }
    }
}