using client_core.core;
using format.core;
using Microsoft.Extensions.Logging;

namespace client_core;

public class Program
{
    static async Task Main()
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger<Program>();
        Connector connector;
        try
        {
            connector = new Connector("127.0.0.1", 13000, logger);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to connect to server");
            return;
        }
        Connection? connection = await connector.Connect();
        if (connection == null)
        {
            Console.WriteLine("Not connected");
            return;
        }
        Console.WriteLine("Connected!!!");
        await connection.AddTask(new ProtocolMessage(MessageType.RequestUserList));
        Console.WriteLine("RequestUserList");
        while (true) continue;
    }
}