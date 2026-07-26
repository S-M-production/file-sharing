using format.core;
using router_core.core;
using router_core.middleware;
using server_core.core;
using server_core.logic;
using System.Net;
using System.Text;

namespace server_core.middleware;

/// <inheritdoc />
public class Middleware:IMiddleware
{
    private readonly UserList _userList;

    private readonly IPEndPoint _ip;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userList">Pass in userlist here so GetResponse will have the right definition</param>
    public Middleware(UserList userList, IPEndPoint ip)
    {
        _userList = userList;
        _ip = ip;
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
    public async Task<ProtocolMessage?> GetResponse(ProtocolMessage message, RouterMap routerMap)
    {
        switch (message.MessageType)
        {
            case MessageType.RequestUserList:
                return new ProtocolMessage(MessageType.UserList, _userList.Serialize());
            case MessageType.Connect:
                return new ProtocolMessage(MessageType.ConnectedToServer);
            case MessageType.Ping:
                return new ProtocolMessage(MessageType.Pong);
            case MessageType.ConnectToUser:
                string tempText = Encoding.UTF8.GetString(message.Body);
                if (!_userList.Connections.TryGetValue(tempText, out Worker worker)) return new ProtocolMessage(MessageType.UserNotFound);
                _ = worker.Connection.AddTask(new ProtocolMessage(MessageType.ConnectToUser, Encoding.UTF8.GetBytes(_ip.Address.MapToIPv4().ToString())));
                return null;
            default:
                return null;
        }
    }
}