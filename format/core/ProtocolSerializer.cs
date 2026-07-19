using System.Text;

namespace format.core;

public static class ProtocolSerializer
{
    private const string Sig = "CNSR";
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
    /// <summary>
    /// Serializes the protocol message into a readable format (String)
    /// </summary>
    /// <param name="protocolMessage">Takes in protocolMessage to parse</param>
    /// <returns>Returns a string with all headers/body in UTF-8</returns>
    public static string ReadableSerialize(ProtocolMessage protocolMessage)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("CNSR");
        sb.Append(" ");
        sb.Append(protocolMessage.MessageType.ToString());

        if (protocolMessage.Length == 0) return sb.ToString();
        
        sb.Append(" ");
        sb.Append(protocolMessage.Length);
        sb.Append(" ");
        sb.Append(System.Text.Encoding.UTF8.GetString(protocolMessage.Body));

        return sb.ToString();
    }
    
    /// <summary>
    /// Creates header from Message type and length of message
    /// </summary>
    /// <param name="type">Type of protocol message</param>
    /// <param name="messageLength">Length of body</param>
    /// <returns>Returns byte[] of header</returns>
    private static byte[] BuildHeader(MessageType type,Int32 messageLength) 
    {

        byte[] sigByte = System.Text.Encoding.UTF8.GetBytes(Sig);
        byte[] messageTypeBytes = BitConverter.GetBytes((Int32)type);
        byte[] lengthBytes = BitConverter.GetBytes((Int32)messageLength);

        List<Byte> buffer = new List<Byte>();
        buffer.AddRange(sigByte);
        buffer.AddRange(messageTypeBytes);
        buffer.AddRange(lengthBytes);
        
        return buffer.ToArray();
    }
    
    /// <summary>
    /// Creates message (byte[]) from type of message and body
    /// </summary>
    /// <param name="type">Type of message from protocol format</param>
    /// <param name="message">Message encoded in UTF-8</param>
    /// <returns>returns byte[] of message</returns>
    private static byte[] BuildMessage(MessageType type, byte[] message)
    {
    
        byte[] byteHeader = BuildHeader(type,message.Length);
        byte[] byteMessage = new byte[byteHeader.Length + message.Length];

        Array.Copy(byteHeader, 0, byteMessage, 0, byteHeader.Length);
        Array.Copy(message, 0, byteMessage, byteHeader.Length, message.Length);

        return byteMessage;
    }
    
}