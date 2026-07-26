using format.core;
using router_core.core;
using router_core.middleware;
using server_core.logic;

namespace server_core.middleware;

/// <inheritdoc />
public class Middleware:IMiddleware
{
    private readonly UserList _userList;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userList">Pass in userlist here so GetResponse will have the right definition</param>
    public Middleware(UserList userList)
    {
        _userList = userList;
    }

    /// <summary>
    /// Has checks for:
    /// RequestUserList, replies with UserList 
    /// Connect, replies with ConnectedToServer
    /// Ping, replies with pong
    /// </summary>
    /// <param name="message">Incoming message</param>
    /// <param name="routerMap">Router passed in</param>
    /// <returns>May or maynot return a protocol message, depends on how the middleware is altered</returns>
    public ProtocolMessage? GetResponse(ProtocolMessage message, RouterMap routerMap)
    {
        Console.WriteLine("Got: {0}",message.MessageType);
        if (message.MessageType == MessageType.RequestUserList)
            return new ProtocolMessage(MessageType.UserList, _userList.Serialize());
        
        if (message.MessageType == MessageType.Connect)
        {
            Console.WriteLine($"Returned connected to server!!!");
            return new ProtocolMessage(MessageType.ConnectedToServer);
        }

        if (message.MessageType == MessageType.Ping)
        {
            return new ProtocolMessage(MessageType.Pong);
        }

        return null;
    }
}