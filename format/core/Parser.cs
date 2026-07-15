using System.Net.Sockets;
using System.Text;

namespace format.core
{
    public class Parser
    {
        private readonly NetworkStream networkStream;
        private const int signatureSize = 4;
        private const int messageTypeSize = 4;
        private const int LengthSize = 4;
        private const int HeaderSize = signatureSize + messageTypeSize + LengthSize;

        private static readonly byte[] ExpectedSignature = Encoding.ASCII.GetBytes("CNSR");

        public Parser(NetworkStream networkStream)
        {
            this.networkStream = networkStream;
        }

        public async Task<ProtocolMessage> Parse()
        {
            byte[] header = await ReadExact(HeaderSize);

            byte[] signature = new byte[signatureSize];
            Array.Copy(header, 0, signature, 0, signatureSize);
            
            if (!signature.SequenceEqual(ExpectedSignature))
            {
                throw new IOException(
                    $"Invalid protocol ID '{Encoding.ASCII.GetString(signature)}'. Expected 'CNSR'."
                );
            }
            MessageType messageType = (MessageType) BitConverter.ToInt32(header, signatureSize);
            int length = BitConverter.ToInt32(header, signatureSize + messageTypeSize);

            if (length < 0)
            {
                throw new IOException("Invalid negative body length.");
            }

            byte[] body = await ReadExact(length);

            return new ProtocolMessage(messageType, body);
        }

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
}