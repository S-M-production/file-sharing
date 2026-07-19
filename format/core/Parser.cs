using System.Net.Sockets;
using System.Text;

namespace format.core;
/// <summary>
/// Class to parse a networks stream for incoming packets that follow custom protocol
/// </summary>
/// <param name="networkStream">Tcp stream of valid connecttion</param>
public class Parser(NetworkStream networkStream)
{
    /// <summary>
    /// Header sizes
    /// </summary>
    private const int SignatureSize = 4;
    private const int MessageTypeSize = 4;
    private const int LengthSize = 4;
    private const int HeaderSize = SignatureSize + MessageTypeSize + LengthSize;
    
    private static readonly byte[] ExpectedSignature = Encoding.ASCII.GetBytes("CNSR");
    /// <summary>
    /// Parses 1 protocol message when called
    /// </summary>
    /// <returns>Returns a ProtocolMessage record containing header and body</returns>
    /// <exception cref="IOException">Throws exception when the protocol isn't followed from incoming side</exception>
    public async Task<ProtocolMessage> Parse()
    {
        byte[] header = await ReadExact(HeaderSize);

        byte[] signature = new byte[SignatureSize];
        Array.Copy(header, 0, signature, 0, SignatureSize);
        
        if (!signature.SequenceEqual(ExpectedSignature))
        {
            throw new IOException(
                $"Invalid protocol ID '{Encoding.ASCII.GetString(signature)}'. Expected 'CNSR'."
            );
        }
        MessageType messageType = (MessageType) BitConverter.ToInt32(header, SignatureSize);
        int length = BitConverter.ToInt32(header, SignatureSize + MessageTypeSize);

        if (length < 0)
        {
            throw new IOException("Invalid negative body length.");
        }

        byte[] body = await ReadExact(length);

        return new ProtocolMessage(messageType, body);
    }
    /// <summary>
    /// Retrieves exactly count amount of bytes from stream
    /// </summary>
    /// <param name="count">Number of bytes that should be read</param>
    /// <returns>Returns byte[] or size count</returns>
    /// <exception cref="IOException">Returns IOException when full message cannot be read</exception>
    private async Task<byte[]> ReadExact(int count)
    {
        byte[] buffer = new byte[count];
        int totalRead = 0;

        while (totalRead < count)
        {
            int bytesRead = await networkStream.ReadAsync(
                buffer,
                totalRead,
                count - totalRead
            );

            if (bytesRead == 0)
            {
                throw new IOException(
                    "Client disconnected before full message was received."
                );
            }

            totalRead += bytesRead;
        }

        return buffer;
    }
}
