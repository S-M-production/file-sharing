using format.core;

namespace client_core.logic;

/// <summary>
/// Callback handler for receiving and processing user list messages from the server.
/// </summary>
public class UserListCallBack
{
    /// <summary>
    /// Completion source that is set when a user list message is received.
    /// </summary>
    public TaskCompletionSource<ProtocolMessage> _awaitingMessage { get; }

    /// <summary>
    /// Initializes a new instance of the UserListCallBack class with an empty completion source.
    /// </summary>
    public UserListCallBack()
    {
        _awaitingMessage = new TaskCompletionSource<ProtocolMessage>();
    }
    
    /// <summary>
    /// Handles incoming user list messages by setting the result on the awaiting task.
    /// </summary>
    /// <param name="incomingMessage">The user list message received from the server.</param>
    /// <returns>Always returns null as this handler completes an awaiting task rather than generating a response.</returns>
    public ProtocolMessage? UserListCall(ProtocolMessage incomingMessage)
    {
        _awaitingMessage!.SetResult(incomingMessage);
        return null!;
    }
}