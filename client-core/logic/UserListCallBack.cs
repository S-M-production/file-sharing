using System.Net;
using format.core;
using client_core.middleware;


namespace client_core.router.logic;

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