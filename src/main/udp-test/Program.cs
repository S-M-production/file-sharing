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
                var server = new Server();
                await server.ListenAsync(cancellationSource.Token);
                break;
            }

            case "c":
            {
                if (args.Length < 4)
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

                string message = args.Length >= 5 ? args[4] : $"hello-from-{role.ToUpperInvariant()}";

                await Client.WriteAsync(role, uuid, clientPort, message, cancellationSource.Token);
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
        Console.WriteLine("  udp-test s");
        Console.WriteLine("  udp-test c <a|b> <uuid> <clientPort> [message]");
    }
}

