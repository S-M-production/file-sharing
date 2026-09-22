using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets;

class Client
{
    public static async Task Write()
    {
        IPAddress _address = new IPAddress(new byte[] {150,230,32,189});
        _address = new IPAddress(new byte[] {127,0,0,1});
        int _port = 5000;
        
        IPEndPoint endPoint = new IPEndPoint(_address, _port);
        UdpClient udpClient = new UdpClient(_port);
        udpClient.Connect(endPoint);
        byte[] message = System.Text.Encoding.UTF8.GetBytes("Hello, World!");

        // Func<Task> listener = async () =>
        // {
        //     Console.WriteLine("Hello World");
        //     Task<UdpReceiveResult> message = udpClient.ReceiveAsync();
        //     await message;
        //     Console.WriteLine(message.Result.Buffer);
        // };
        // TaskCompletionSource incomingMessageTask = new TaskCompletionSource(listener.Invoke());

        while (true)
        {
            await udpClient.SendAsync(message);
            await Task.Delay(100);
            // if (incomingMessageTask.Task.IsCompleted)
            // {
            //     break;
            // }
        }
    }
}

