using System.Net.Sockets;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core.core;

public class Connector
{
    TcpClient client;
    ILogger logger;

    public Connector(string server, int port, ILogger logger)
    {
        client = new TcpClient(server, port);
        this.logger = logger;
    }

    public async Task<Connection?> Connect()
    {
        var stream = client.GetStream();
        byte[] request = ProtocolSerializer.Serialize(MessageType.Connect,"");
        await stream.WriteAsync(request, 0, request.Length);
        await stream.FlushAsync();
        Parser parser = new Parser(stream);
        ProtocolMessage response = await parser.Parse();
        if (response.MessageType == MessageType.ConnectedToServer) return new Connection(client, logger);
        return null;
    }
}