using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets;

class Client
{
    static void Write()
    {
        IPAddress _address;
        int _port;

        if (args.Length != 2)
        {
            Console.WriteLine("\nPlease input \'ip address\' \'port\'");
            return;
        }

        try
        {
            _address = IPAddress.Parse(args[0]);
            _port = int.Parse(args[1]);
        }
        catch (Exception)
        {
            Console.WriteLine("\nPlease input \'ip address\' \'port\'");
            return;
        }
        IPEndPoint endPoint = new IPEndPoint(_address, _port);
        UdpClient udpClient = new UdpClient(_port);
        udpClient.Connect(endPoint);
        byte[] message = System.Text.Encoding.UTF8.GetBytes("Hello, World!");

        Func<Task> listener = async () =>
        {
            Console.WriteLine("Hello World");
            Task<UdpReceiveResult> message = udpClient.ReceiveAsync();
            await message;
            Console.WriteLine(message.Result.Buffer);
        };
        TaskCompletionSource incomingMessageTask = new TaskCompletionSource(listener.Invoke());

        while (true)
        {
            udpClient.Send(message);
            Task.Delay(100).Wait();
            if (incomingMessageTask.Task.IsCompleted)
            {
                break;
            }
        }
    }
}

