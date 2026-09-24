using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    private const string DefaultServerAddress = "150.230.32.189";
    private const int DefaultServerPort = 5000;

    public static async Task WriteAsync(string roleText, string uuid, int localPort, string message, CancellationToken cancellationToken)
    {
        if (!TryParseRole(roleText, out var role))
        {
            Console.WriteLine("Client role must be A or B.");
            return;
        }

        if (string.IsNullOrWhiteSpace(uuid))
        {
            Console.WriteLine("UUID is required.");
            return;
        }

        if (!Guid.TryParse(uuid, out _))
        {
            Console.WriteLine("UUID must be a valid GUID string.");
            return;
        }

        string payload = BuildAnnouncement(role, uuid, message);
        byte[] announceBytes = Encoding.UTF8.GetBytes(payload);
        byte[] peerBytes = Encoding.UTF8.GetBytes(message);

        if (localPort is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
        {
            Console.WriteLine($"Client port must be between {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}.");
            return;
        }

        var serverEndpoint = new IPEndPoint(IPAddress.Parse(DefaultServerAddress), DefaultServerPort);

        UdpClient udpClient;

        try
        {
            udpClient = new UdpClient(localPort);
        }
        catch (SocketException)
        {
            Console.WriteLine($"Failed to bind client to local port {localPort}. Is it already in use?");
            return;
        }

        using (udpClient)
        {
            udpClient.Connect(serverEndpoint);

            var peerEndpointSource = new TaskCompletionSource<IPEndPoint>(TaskCreationOptions.RunContinuationsAsynchronously);

            var announceTask = AnnounceLoopAsync(udpClient, announceBytes, peerEndpointSource.Task, cancellationToken);
            var receiveTask = ReceiveLoopAsync(udpClient, peerEndpointSource, cancellationToken);

            IPEndPoint peerEndpoint;

            try
            {
                peerEndpoint = await peerEndpointSource.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            udpClient.Connect(peerEndpoint);
            Console.WriteLine($"Peer discovered for {role}/{uuid}: {peerEndpoint}");

            await announceTask;

            var peerSendTask = PeerSendLoopAsync(udpClient, peerBytes, cancellationToken);

            await Task.WhenAll(receiveTask, peerSendTask);
        }
    }

    private static async Task AnnounceLoopAsync(UdpClient udpClient, byte[] payload, Task<IPEndPoint> peerTask, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && !peerTask.IsCompleted)
            {
                await udpClient.SendAsync(payload, payload.Length);
                await Task.Delay(100, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static async Task ReceiveLoopAsync(UdpClient udpClient, TaskCompletionSource<IPEndPoint> peerEndpointSource, CancellationToken cancellationToken)
    {
        bool printedPeerMessage = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await udpClient.ReceiveAsync();
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException)
            {
                continue;
            }

            string payload = Encoding.UTF8.GetString(result.Buffer);

            if (TryParsePeerEndpoint(payload, out var peerEndpoint))
            {
                peerEndpointSource.TrySetResult(peerEndpoint);
                continue;
            }

            if (peerEndpointSource.Task.IsCompleted && !printedPeerMessage && !string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine(payload);
                printedPeerMessage = true;
            }
        }
    }

    private static async Task PeerSendLoopAsync(UdpClient udpClient, byte[] payload, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await udpClient.SendAsync(payload, payload.Length);
                await Task.Delay(100, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static string BuildAnnouncement(string role, string uuid, string message)
    {
        return $"{role.ToUpperInvariant()}{uuid}{message}";
    }

    private static bool TryParseRole(string roleText, out string role)
    {
        role = roleText.Trim().ToUpperInvariant();
        return role is "A" or "B";
    }

    private static bool TryParsePeerEndpoint(string payload, out IPEndPoint peerEndpoint)
    {
        peerEndpoint = default!;

        if (!payload.StartsWith("PEER|", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string endpointText = payload.Substring("PEER|".Length);

        if (!IPEndPoint.TryParse(endpointText, out var parsedPeerEndpoint))
        {
            return false;
        }

        peerEndpoint = parsedPeerEndpoint;
        return true;
    }
}

