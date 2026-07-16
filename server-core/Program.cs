using System.Net;
using Microsoft.Extensions.Logging;
using server_core.core;

namespace server_core;

public class Program
{
    static IPAddress? address;
    static Int32 port;
    public static void Main(string[] args)
    {
        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger<Program>();

        if (args.Length != 2)
        {
            logger.LogError("\nPlease input \'ip adress\' \'port\'");
            return;
        }

        try
        {
            address = IPAddress.Parse(args[0]);
            port = Int32.Parse(args[1]);    
        }catch (Exception e){
            logger.LogError("\nPlease input \'ip adress\' \'port\'");
            return;
        }

        logger.LogInformation("Inputs are valid");
    
        Listener listener = new Listener(address,port, logger);
        Task listenerInstance = listener.Run();
        while(true){continue;}
    }
}