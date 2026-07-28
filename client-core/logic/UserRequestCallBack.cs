using System.Threading.Tasks;
using format.core;
using client_core.middleware;


namespace client_core.logic;

public class UserRequestCallBack
{
    public TaskCompletionSource<ProtocolMessage> _awaitingMessage { get; }

    public UserRequestCallBack()
    {
        _awaitingMessage = new TaskCompletionSource<ProtocolMessage>();
    }
    
    public ProtocolMessage? UserRequestCall(ProtocolMessage incomingMessage)
    {
        _awaitingMessage!.SetResult(incomingMessage);
        return null!;
    }
}