using System.Net;
using Microsoft.Extensions.Logging;
using server_core.core;

namespace server_core;

public class Program
{
    private static IPAddress? _address;
    private static int _port;
    /// <summary>
    /// Entrypoint for server
    /// </summary>
    /// <param name="args">takes args "'ip' 'port'"</param>
    public static async Task Main(string[] args)
    {
        using var factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger<Program>();

        if (args.Length != 2)
        {
            logger.LogError("\nPlease input \'ip adress\' \'port\'");
            return;
        }

        try
        {
            _address = IPAddress.Parse(args[0]);
            _port = int.Parse(args[1]);    
        }catch (Exception e){
            logger.LogError("\nPlease input \'ip address\' \'port\'");
            return;
        }

        logger.LogInformation("Inputs are valid");
    
        var listener = new Listener(_address,_port, logger);
        _ = listener.Run();
        await Task.Delay(Timeout.Infinite);
    }
}