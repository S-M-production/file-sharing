using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class Server
{
    private const int Port = 13000;
    private readonly TcpListener _listener;
    private readonly Dictionary<(string Role, string Uuid), (IPEndPoint PublicEndpoint, TcpClient Connection)> _registrations = new();

    public Server()
    {
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start();
        Console.WriteLine($"TCP coordination server listening on port {Port}...");
    }

    public async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleRegistrationAsync(tcpClient, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleRegistrationAsync(TcpClient tcpClient, CancellationToken cancellationToken)
    {
        using var client = tcpClient;
        var remoteEp = (IPEndPoint)client.Client.RemoteEndPoint!;

        try
        {
            using var ns = client.GetStream();
            // Read a line with registration data
            var buffer = new byte[4096];
            int read = await ns.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (read <= 0) return;

            string payload = Encoding.UTF8.GetString(buffer, 0, read).Trim();
            // Expected format: ROLE|UUID|listenPort|message
            if (!TryParseRegistration(payload, out var role, out var uuid, out var listenPort, out var message))
            {
                return;
            }

            var publicEndpoint = new IPEndPoint(remoteEp.Address, listenPort);
            var key = (role, uuid);
            lock (_registrations)
            {
                _registrations[key] = (publicEndpoint, client);
            }

            Console.WriteLine($"Registered {role}/{uuid} from {remoteEp.Address}:{remoteEp.Port} listening:{listenPort}");

            string oppositeRole = role == "A" ? "B" : "A";
            (IPEndPoint PublicEndpoint, TcpClient Connection) peer;
            bool hasPeer;

            lock (_registrations)
            {
                hasPeer = _registrations.TryGetValue((oppositeRole, uuid), out peer);
            }

            if (hasPeer)
            {
                // Send peer info to both sides. If the stored TcpClient for peer is already disposed this may fail.
                try
                {
                    await SendPeerAsync(client, peer.PublicEndpoint, cancellationToken);
                }
                catch { }

                try
                {
                    await SendPeerAsync(peer.Connection, publicEndpoint, cancellationToken);
                }
                catch { }

                Console.WriteLine($"Paired {uuid}: {role}={publicEndpoint}, {oppositeRole}={peer.PublicEndpoint}");
            }

            // Keep connection alive until cancelled or closed by client
            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration handler error: {ex.Message}");
        }
        finally
        {
            // cleanup registrations that reference this connection
            lock (_registrations)
            {
                var toRemove = _registrations.Where(kv => kv.Value.Connection == client).Select(kv => kv.Key).ToList();
                foreach (var k in toRemove) _registrations.Remove(k);
            }
        }
    }

    private static bool TryParseRegistration(string payload, out string role, out string uuid, out int listenPort, out string message)
    {
        role = string.Empty;
        uuid = string.Empty;
        listenPort = 0;
        message = string.Empty;

        var parts = payload.Split('|', 4);
        if (parts.Length < 3) return false;
        role = parts[0].Trim().ToUpperInvariant();
        uuid = parts[1].Trim();
        if (!int.TryParse(parts[2], out listenPort)) return false;
        message = parts.Length >= 4 ? parts[3] : string.Empty;
        if (role != "A" && role != "B") return false;
        if (!Guid.TryParse(uuid, out _)) return false;
        return true;
    }

    private static async Task SendPeerAsync(TcpClient client, IPEndPoint peerEndpoint, CancellationToken cancellationToken)
    {
        if (!client.Connected) return;
        var ns = client.GetStream();
        string payload = $"PEER|{peerEndpoint.Address}:{peerEndpoint.Port}\n";
        var bytes = Encoding.UTF8.GetBytes(payload);
        await ns.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        await ns.FlushAsync(cancellationToken);
    }
}
