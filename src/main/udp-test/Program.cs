using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static async Task Main(string[] args)
    {
        using var cancellationSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "s":
            {
                int serverPort = 13000;
                if (args.Length >= 2 && !int.TryParse(args[1], out serverPort))
                {
                    Console.WriteLine("Server port must be a valid integer.");
                    return;
                }

                var server = new Server(serverPort);
                await server.ListenAsync(cancellationSource.Token);
                break;
            }

            case "c":
            {
                // Expected: c <a|b> <uuid> <clientPort> <serverHost> <serverPort> [message]
                if (args.Length < 6)
                {
                    PrintUsage();
                    return;
                }

                string role = args[1];
                string uuid = args[2];
                if (!int.TryParse(args[3], out int clientPort))
                {
                    Console.WriteLine("Client port must be a valid integer.");
                    return;
                }

                string serverHost = args[4];

                if (!int.TryParse(args[5], out int serverPort))
                {
                    Console.WriteLine("Server port must be a valid integer.");
                    return;
                }

                string message = args.Length >= 7 ? args[6] : $"hello-from-{role.ToUpperInvariant()}";

                await Client.WriteAsync(role, uuid, clientPort, serverHost, serverPort, message, cancellationSource.Token);
                break;
            }

            default:
                PrintUsage();
                break;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  udp-test s [serverPort]");
        Console.WriteLine("  udp-test c <a|b> <uuid> <clientPort> <serverHost> <serverPort> [message]");
    }
}

