namespace format.core;
/// <summary>
/// Enums representing the types of messages sent in the protocol
/// </summary>
public enum MessageType : int
{
    Connect, //Contains body, username
    RequestUserList,
    ConnectToUser,
    Ping,
    // Server → Client
    ConnectedToServer,
    UserList,
    PortNegotiation,
    StartUdpPunch ,
    Pong
}