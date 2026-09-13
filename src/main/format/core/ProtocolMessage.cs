namespace format.core;

/// <summary>
/// Way to store protocol message
/// </summary>
/// <param name="MessageType">Type of message</param>
/// <param name="Body">The payload in bytes</param>
public record ProtocolMessage(MessageType MessageType, byte[] Body)
{
    /// <summary>
    /// Setting length automatically by counting bodys length
    /// </summary>
    public int Length => Body.Length;
    
    /// <summary>
    /// Setting protocol message using message type and no body 
    /// </summary>
    /// <param name="messageType">yk what it is</param>
    public ProtocolMessage(MessageType messageType)
        : this(messageType, [])
    {
    }

    /// <summary>
    /// Setting protocol message using message type and string body 
    /// </summary>
    /// <param name="messageType">yk what it is</param>
    /// <param name="body">yk what it is</param>
    public ProtocolMessage(MessageType messageType, String body)
        : this(messageType, System.Text.Encoding.UTF8.GetBytes(body))
    {
    }
}    
