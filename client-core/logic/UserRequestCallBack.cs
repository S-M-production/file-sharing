using System.Threading.Tasks;
using format.core;
using client_core.middleware;


namespace client_core.logic;

public class UserRequestCallBack
{
    public TaskCompletionSource<ProtocolMessage> _awaitingMessage { get; }

    // Optional UI notifier. UI can set this to be informed immediately when a request arrives.
    public Action<ProtocolMessage>? OnIncomingRequest { get; set; }

    public UserRequestCallBack()
    {
        _awaitingMessage = new TaskCompletionSource<ProtocolMessage>();
    }
    
    public ProtocolMessage? UserRequestCall(ProtocolMessage incomingMessage)
    {
        string senderAddress = System.Text.Encoding.UTF8.GetString(incomingMessage.Body);

        Console.WriteLine("Incoming connection request from: {0}", senderAddress);
        
        _awaitingMessage!.SetResult(incomingMessage);
        OnIncomingRequest?.Invoke(incomingMessage);

        return null!;
    }
}