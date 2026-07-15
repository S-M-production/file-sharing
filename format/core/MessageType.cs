namespace format.core;
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