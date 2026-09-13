using format.core;

namespace network_core.call_back;

public record CallBackTask(ProtocolMessage ProtocolMessage, TaskCompletionSource<bool> TaskCompletionSource)
{
    public void SetCompletionSource(bool value = true)
    {
        TaskCompletionSource.TrySetResult(value);
    }
}