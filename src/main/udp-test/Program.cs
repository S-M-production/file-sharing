using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets;

class Program
{
    static async Task Main(String [] args)
    {
        if (args.Length != 1)
        {
            return;
        }

        if (args[0] == "c")
        {
            await Client.Write();
        }
        else if (args[0] == "s")
        {
            await Server.listen();
        }
    }
}

