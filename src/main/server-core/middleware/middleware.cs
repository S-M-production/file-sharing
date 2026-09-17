using format.core;
using Microsoft.VisualBasic;
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
    /// <summary>
    /// Concurrent dictionary containing all connected users mapped by their endpoint address
    /// </summary>
    private readonly UserList _userList;

    /// <summary>
    /// The IP endpoint of the current client connection this middleware is handling
    /// </summary>
    private readonly IPEndPoint _ip;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="userList">Pass in userlist here so GetResponse will have the right definition</param>
    /// <param name="ip">IEndpoint of worker that's handling the connection that's using this</param>
    public Middleware(UserList userList, IPEndPoint ip)
    {
        _userList = userList;
        _ip = ip;
    }
    /// TODO: Move responsibility of all this into router, lambas or logic
    /// <summary>
    /// Processes incoming protocol messages and returns appropriate responses based on message type.
    /// </summary>
    /// <param name="message">The incoming protocol message to process</param>
    /// <param name="routerMap">The router map containing registered message handlers</param>
    /// <returns>A ProtocolMessage response to send back to the client, or null if no response should be sent</returns>
    public async Task<ProtocolMessage?> GetResponse(ProtocolMessage message, RouterMap routerMap)
    {
        var tempText = Encoding.UTF8.GetString(message.Body);
        switch (message.MessageType)
        {
            case MessageType.RequestUserList:
                // Serialize the user list excluding the requesting client and send it back
                string user = _ip.Address.MapToIPv4().ToString() +":"+ _ip.Port.ToString();
                return new ProtocolMessage(MessageType.UserList, _userList.Serialize(user));
            
            case MessageType.Connect:
                // Acknowledge successful connection to server
                return new ProtocolMessage(MessageType.ConnectedToServer);
            case MessageType.Ping:
                // Respond to heartbeat ping with pong to keep connection alive
                return new ProtocolMessage(MessageType.Pong);
            
            case MessageType.ConnectToUser:
                // Attempt to retrieve target user; if not found return error, otherwise queue connection info to target
                if (!_userList.TryGetValue(tempText, out Worker worker)) return new ProtocolMessage(MessageType.UserNotFound);
                var response = new ProtocolMessage(MessageType.ConnectToUser,
                    Encoding.UTF8.GetBytes($"{_ip.Address.MapToIPv4()}:{_ip.Port}"));
                worker.Connection.AddTask(response);
                return null;  // Don't send response to requester, they'll contact directly
            
            case MessageType.Disconnect:
                // Route disconnect through registered handler and pass client endpoint info
                if(!routerMap.GetRoute(message.MessageType, out var temp)) return null;
                message = new ProtocolMessage(message.MessageType, Encoding.UTF8.GetBytes(_ip.Address.MapToIPv4().ToString()+":"+ _ip.Port.ToString()));
                return temp!(message);
            
            default:
                // Route to any custom registered handlers for this message type
                if(!routerMap.GetRoute(message.MessageType, out var handle)) return null;
                return handle!(message);
        }
    }
}