using System.Net;
using System.Net.Sockets;
using System.Text;

public class Server{
    public static void listen(){
        using var udp = new UdpClient(5000);

            Console.WriteLine("Listening on UDP port 5000...");

            while (true)
            {
                var result = await udp.ReceiveAsync();

                string message = Encoding.UTF8.GetString(result.Buffer);

                Console.WriteLine(
                    $"[{result.RemoteEndPoint}] {message}"
                );
            }
    }

}
