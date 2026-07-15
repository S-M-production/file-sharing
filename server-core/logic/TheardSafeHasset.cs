using System.Text;
using System.Text.Json;

namespace stun_server.logic;

public class ThreadSafeHasset
{
    private readonly HashSet<string> _connections = new HashSet<string>();
    private readonly object _lock = new object();

    public bool Add(string ipPort)
    {
        lock (_lock)
        {
            return _connections.Add(ipPort);
        }
    }
    public bool Remove(string ipPort)
    {
        lock (_lock)
        {
            return _connections.Remove(ipPort);
        }
    }
    public bool Contains(string ipPort)
    {
        lock (_lock)
        {
            return _connections.Contains(ipPort);
        }
    }
    public List<string> GetAll()
    {
        lock (_lock)
        {
            return _connections.ToList();
        }
    }

    public byte[] Serialize()
    {
        lock (_lock)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_connections.ToList()));
        }
    }
}