using format.core;
using server_core.logic;

namespace server_core.middleware;

public static class Middleware
{
    public static ProtocolMessage? GetResponse(ProtocolMessage message, ThreadSafeHasset connections)
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