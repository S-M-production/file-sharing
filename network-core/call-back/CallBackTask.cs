using format.core;

namespace network_core.call_back;

public record CallBackTask(ProtocolMessage ProtocolMessage, TaskCompletionSource TaskCompletionSource)
{
    public void Completed()
    {
        TaskCompletionSource.TrySetResult();
    }
}