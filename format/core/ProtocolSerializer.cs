using System.Text;

namespace format.core;

public class ProtocolSerializer
{
    private const string sig = "CNSR";
    
    public static byte[] Serialize(MessageType type, string message)
    {
        return  BuildMessage(type,System.Text.Encoding.UTF8.GetBytes(message)); 
    }
    
    public static byte[]Serialize(MessageType type, byte[] message)
    {
        return BuildMessage(type,message); 
    }

    public static byte[] Serialize(ProtocolMessage protocolMessage)
    {
        return BuildMessage(protocolMessage.MessageType, protocolMessage.Body);
    }

    public static String ReadableSerialize(ProtocolMessage protocolMessage)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("CNSR");
        sb.Append(" ");
        sb.Append(protocolMessage.MessageType.ToString());
        
        if (protocolMessage.Length != 0)
        {
            sb.Append(" ");
            sb.Append(protocolMessage.Length);
            sb.Append(" ");
            sb.Append(System.Text.Encoding.UTF8.GetString(protocolMessage.Body));    
        }
        
        return sb.ToString();
    }
    
    // Converts a MessageType into a 4-byte header
    private static byte[] BuildHeader(MessageType type,Int32 messageLength) // eventually it would make sense to have it as a private class instead of a public class
    {
        // Implementation goes here
        byte[] sigByte = System.Text.Encoding.UTF8.GetBytes(sig);
        byte[] messageTypeBytes = BitConverter.GetBytes((Int32)type);
        byte[] lengthBytes = BitConverter.GetBytes((Int32)messageLength);

        List<Byte> buffer = new List<Byte>();
        buffer.AddRange(sigByte);
        buffer.AddRange(messageTypeBytes);
        buffer.AddRange(lengthBytes);
        
        return buffer.ToArray();
    }
    // Combines header + payload into one message
    private static byte[] BuildMessage(MessageType type, byte[] message)
    {
        // Implementation goes here
        byte[] byteHeader = BuildHeader(type,message.Length);
        byte[] byteMessage = new byte[byteHeader.Length + message.Length];

        Array.Copy(byteHeader, 0, byteMessage, 0, byteHeader.Length);
        Array.Copy(message, 0, byteMessage, byteHeader.Length, message.Length);

        return byteMessage;
    }
    
}