namespace format.core;
/// <summary>
/// Enums representing the types of messages sent in the protocol
/// </summary>
public enum MessageType : int
{
    /// <summary>
    /// Request to connect to server, for now contains no body
    /// </summary>
    Connect, 
    /// <summary>
    /// Request to get full user list from server, for now contains no body
    /// </summary>
    RequestUserList,
    /// <summary>
    /// Request to connect to a specific user, contains the user IP:PORT in body
    /// </summary>
    ConnectToUser,
    /// <summary>
    /// Request to server to keep connection alive
    /// </summary>
    Ping,
    // Server → Client
    /// <summary>
    /// Response back to client allowing connection to server, for now contains no body
    /// </summary>
    ConnectedToServer,
    /// <summary>
    /// Response back to client, contains full list of users in its body
    /// </summary>
    UserList,
    /// <summary>
    /// Asks client for what ports they have free to start UDP hole punch, for now contains no body
    /// </summary>
    PortNegotiation,
    /// <summary>
    /// Tells both clients to start UDP hole punching, for now contains no body
    /// </summary>
    StartUdpPunch ,
    /// <summary>
    /// Response back to client from ping, for now contains no body
    /// </summary>
    Pong,
    /// <summary>
    /// For protocol message used within one program, contains body
    /// </summary>
    Internal
}