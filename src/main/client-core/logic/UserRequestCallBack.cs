using System.Threading.Tasks;
using format.core;
using client_core.middleware;

namespace client_core.logic;

/// <summary>
/// Handles incoming peer connection requests and exposes a completion source for waiting consumers.
/// </summary>
public class UserRequestCallBack
{
    /// <summary>
    /// Gets the task that completes when a connection request message is received.
    /// </summary>
    public TaskCompletionSource<ProtocolMessage> _awaitingMessage { get; }

    /// <summary>
    /// Gets or sets an optional callback invoked immediately when a new inbound request is received.
    /// </summary>
    public Action<ProtocolMessage>? OnIncomingRequest { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRequestCallBack"/> class.
    /// </summary>
    public UserRequestCallBack()
    {
        _awaitingMessage = new TaskCompletionSource<ProtocolMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Processes an incoming connection request message and notifies any waiting listeners.
    /// </summary>
    /// <param name="incomingMessage">The request message received from another peer.</param>
    /// <returns>Always returns <c>null</c> because request handling is completed via the awaiting task callback.</returns>
    public ProtocolMessage? UserRequestCall(ProtocolMessage incomingMessage)
    {
        string senderAddress = System.Text.Encoding.UTF8.GetString(incomingMessage.Body);

        Console.WriteLine("Incoming connection request from: {0}", senderAddress);

        _awaitingMessage!.TrySetResult(incomingMessage);
        OnIncomingRequest?.Invoke(incomingMessage);

        return null!;
    }
}