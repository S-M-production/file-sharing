using format.core;

namespace network_core.call_back;

/// <summary>
/// Represents a queued message task with an associated completion source for tracking write status.
/// </summary>
/// <param name="ProtocolMessage">The protocol message to be serialized and sent.</param>
/// <param name="TaskCompletionSource">Completion source that signals when the message has been written or failed.</param>
public record CallBackTask(ProtocolMessage ProtocolMessage, TaskCompletionSource<bool> TaskCompletionSource)
{
    /// <summary>
    /// Sets the completion result of the task, signaling whether the message write succeeded.
    /// </summary>
    /// <param name="value">True if message was successfully written, false otherwise. Defaults to true.</param>
    public void SetCompletionSource(bool value = true)
    {
        TaskCompletionSource.TrySetResult(value);
    }
}