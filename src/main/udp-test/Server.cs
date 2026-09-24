using System.Net;
using System.Net.Sockets;
using System.Text;

public sealed class Server
{
    private const int Port = 5000;
    private readonly UdpClient _udpClient;
    private readonly Dictionary<(string Role, string Uuid), IPEndPoint> _endpoints = new();
    private readonly HashSet<string> _reportedPairs = new();

    public Server()
    {
        _udpClient = new UdpClient(Port);
        Console.WriteLine($"UDP hole-punching server listening on port {Port}...");
    }

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        using var cancelRegistration = cancellationToken.Register(() => _udpClient.Close());

        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;

            try
            {
                result = await _udpClient.ReceiveAsync();
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string payload = Encoding.UTF8.GetString(result.Buffer);

            if (!TryParseAnnouncement(payload, out var role, out var uuid, out var message))
            {
                continue;
            }

            _endpoints[(role, uuid)] = result.RemoteEndPoint;

            string oppositeRole = role == "A" ? "B" : "A";
            if (_endpoints.TryGetValue((oppositeRole, uuid), out var peerEndpoint))
            {
                if (_reportedPairs.Add(uuid))
                {
                    Console.WriteLine($"Pair established for {uuid}: {role}={result.RemoteEndPoint}, {oppositeRole}={peerEndpoint}");
                }

                await SendPeerEndpointAsync(result.RemoteEndPoint, peerEndpoint);
                await SendPeerEndpointAsync(peerEndpoint, result.RemoteEndPoint);
            }
        }
    }

    private static bool TryParseAnnouncement(string payload, out string role, out string uuid, out string message)
    {
        role = string.Empty;
        uuid = string.Empty;
        message = string.Empty;

        if (payload.Length < 1 + 36)
        {
            return false;
        }

        char roleCharacter = char.ToUpperInvariant(payload[0]);
        if (roleCharacter is not ('A' or 'B'))
        {
            return false;
        }

        role = roleCharacter.ToString();
        uuid = payload.Substring(1, 36);
        message = payload.Length > 37 ? payload.Substring(37) : string.Empty;

        return Guid.TryParse(uuid, out _);
    }

    private async Task SendPeerEndpointAsync(IPEndPoint clientEndpoint, IPEndPoint peerEndpoint)
    {
        string payload = $"PEER|{peerEndpoint}";
        byte[] response = Encoding.UTF8.GetBytes(payload);
        await _udpClient.SendAsync(response, response.Length, clientEndpoint);
    }
}
