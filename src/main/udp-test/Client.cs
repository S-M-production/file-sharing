using System;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Client
{
    // Use localhost by default for easier local testing. Change if you run server elsewhere.
    private const string DefaultServerAddress = "127.0.0.1";
    private const int DefaultServerPort = 5000;

    public static async Task WriteAsync(string roleText, string uuid, int localPort, string message, CancellationToken cancellationToken)
    {
        if (!TryParseRole(roleText, out var role))
        {
            Console.WriteLine("Client role must be A or B.");
            return;
        }

        if (string.IsNullOrWhiteSpace(uuid) || !Guid.TryParse(uuid, out _))
        {
            Console.WriteLine("UUID is required and must be a valid GUID.");
            return;
        }

        if (localPort is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            Console.WriteLine($"Client port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}.");
            return;
        }

        var serverEndpoint = new IPEndPoint(IPAddress.Parse(DefaultServerAddress), DefaultServerPort);

        // 1) Register with server using a TCP connection bound to the localPort so the server sees the correct source port
        IPEndPoint? peerEndpoint = null;
        try
        {
            using (var regSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                regSocket.Bind(new IPEndPoint(IPAddress.Any, localPort));
                await regSocket.ConnectAsync(serverEndpoint);
                using var ns = new NetworkStream(regSocket, ownsSocket: false);

                string registration = $"{role}|{uuid}|{localPort}|{message}\n";
                var bytes = Encoding.UTF8.GetBytes(registration);
                await ns.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await ns.FlushAsync(cancellationToken);

                // Wait for server to respond with PEER|ip:port\n (or until cancelled)
                var readBuf = new byte[1024];
                int read = await ns.ReadAsync(readBuf, 0, readBuf.Length, cancellationToken);
                if (read > 0)
                {
                    string resp = Encoding.UTF8.GetString(readBuf, 0, read).Trim();
                    if (TryParsePeerEndpoint(resp, out var parsed))
                    {
                        peerEndpoint = parsed;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration failed: {ex.Message}");
            return;
        }

        if (peerEndpoint == null)
        {
            Console.WriteLine("No peer endpoint received from server.");
            return;
        }

        Console.WriteLine($"Peer discovered for {role}/{uuid}: {peerEndpoint}");

        // 2) Try to establish direct TCP connection: start a listener and concurrently attempt outgoing connects
        var listener = new TcpListener(IPAddress.Any, localPort);
        try
        {
            listener.Start();
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Failed to start listener on {localPort}: {ex.Message}");
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var acceptTask = Task.Run(async () =>
        {
            try
            {
                var accepted = await listener.AcceptTcpClientAsync(cts.Token);
                return accepted;
            }
            catch { return null; }
        }, cts.Token);

        var connectTask = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    var tc = new TcpClient();
                    var connectTaskInner = tc.ConnectAsync(peerEndpoint.Address, peerEndpoint.Port);
                    var completed = await Task.WhenAny(connectTaskInner, Task.Delay(1000, cts.Token));
                    if (completed == connectTaskInner)
                    {
                        if (tc.Connected) return tc;
                    }
                    tc.Dispose();
                }
                catch { }

                await Task.Delay(100, cts.Token);
            }

            return null;
        }, cts.Token);

        // Wait for whichever completes first
        TcpClient? connection = null;
        try
        {
            var finished = await Task.WhenAny(acceptTask, connectTask);
            if (finished == acceptTask)
            {
                connection = acceptTask.Result;
            }
            else
            {
                connection = connectTask.Result;
            }

            if (connection == null)
            {
                Console.WriteLine("Failed to establish direct connection to peer.");
                return;
            }

            cts.Cancel(); // stop the other task

            // Send one message and print it once
            using var ns = connection.GetStream();
            var payload = Encoding.UTF8.GetBytes(message + "\n");
            await ns.WriteAsync(payload, 0, payload.Length, CancellationToken.None);
            await ns.FlushAsync(CancellationToken.None);

            Console.WriteLine($"Connected to peer {connection.Client.RemoteEndPoint}");

            // Read one message from peer and print
            var rbuf = new byte[1024];
            try
            {
                int r = await ns.ReadAsync(rbuf, 0, rbuf.Length, CancellationToken.None);
                if (r > 0)
                {
                    var recv = Encoding.UTF8.GetString(rbuf, 0, r).Trim();
                    Console.WriteLine(recv);
                }
            }
            catch { }
        }
        finally
        {
            try { listener.Stop(); } catch { }
        }
    }

    private static bool TryParseRole(string roleText, out string role)
    {
        role = roleText.Trim().ToUpperInvariant();
        return role is "A" or "B";
    }

    private static bool TryParsePeerEndpoint(string payload, out IPEndPoint peerEndpoint)
    {
        peerEndpoint = default!;
        if (!payload.StartsWith("PEER|", StringComparison.OrdinalIgnoreCase)) return false;
        string endpointText = payload.Substring("PEER|".Length).Trim();
        // expected ip:port
        var parts = endpointText.Split(':');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out var ip)) return false;
        if (!int.TryParse(parts[1], out var port)) return false;
        peerEndpoint = new IPEndPoint(ip, port);
        return true;
    }
}

