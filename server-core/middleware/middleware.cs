using format.core;
using router_core.middleware;
using server_core.logic;

namespace server_core.middleware;

/// <inheritdoc />
public class Middleware:IMiddleware
{
    /// <summary>
    /// Has checks for:
    /// RequestUserList, replies with UserList 
    /// Connect, replies with ConnectedToServer
    /// Ping, replies with pong
    /// </summary>
    /// <param name="message">Incoming message</param>
    /// <param name="connections">Outgoing message</param>
    /// <returns></returns>
    public static ProtocolMessage? GetResponse(ProtocolMessage message, UserList connections)
    {
        if (message.MessageType == MessageType.RequestUserList)
            return new ProtocolMessage(MessageType.UserList, connections.Serialize());
        
        if (message.MessageType == MessageType.Connect) return new ProtocolMessage(MessageType.ConnectedToServer);

        if (message.MessageType == MessageType.Ping)
        {
            return new ProtocolMessage(MessageType.Pong);
        }

        return null;
    }
}