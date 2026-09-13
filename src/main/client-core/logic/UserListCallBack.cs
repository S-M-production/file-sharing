using format.core;

namespace client_core.logic;

public class UserListCallBack
{
    public TaskCompletionSource<ProtocolMessage> _awaitingMessage { get; }

    public UserListCallBack()
    {
        _awaitingMessage = new TaskCompletionSource<ProtocolMessage>();
    }
    
    public ProtocolMessage? UserListCall(ProtocolMessage incomingMessage)
    {
        _awaitingMessage!.SetResult(incomingMessage);
        return null!;
    }
}