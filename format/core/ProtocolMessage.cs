namespace format.core;
public record ProtocolMessage(MessageType MessageType, byte[] Body)
{
    public int Length => Body.Length;

    public ProtocolMessage(MessageType messageType)
        : this(messageType, [])
    {
    }
}    
