using format;
using stun_server.format;
using stun_server.logic;

namespace stun_server.middleware;

public static class middleware
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