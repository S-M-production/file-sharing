using System.Collections.Concurrent;
using System.Data;
using System.Text;
using System.Text.Json;
using format.core;
using server_core.core;

namespace server_core.logic;
/// <summary>
/// class to house dictionary relation ip:port->worker object
/// </summary>
/// <param name="publisher">Pub for pub-sub</param>
public class UserList(Publisher publisher)
{
    /// <summary>
    /// All the connections related from ip:port->worker object
    /// </summary>
    /// <remarks>
    /// Designed this way so one worker can feed a request into another worker to send a message
    /// </remarks>
    private readonly ConcurrentDictionary<string, Worker> _connections = new();
    /// <summary>
    /// Wrapper for TryGetValue of ConcurrentDictionary
    /// </summary>
    public bool TryGetValue(String input, out Worker worker)
    {
        return _connections.TryGetValue(input, out worker);
    }
    /// <summary>
    /// Wrapper for TryRemove of ConcurrentDictionary
    /// </summary>
    public bool TryRemove(String input, out Worker worker)
    {
        publisher.AddMessage(new ProtocolMessage(MessageType.RemoveUserFromList, input));
        return _connections.TryRemove(input, out worker);
    }
    /// <summary>
    /// Wrapper for TryAdd of ConcurrentDictionary
    /// </summary>
    public bool TryAdd(String input,Worker worker)
    {
        publisher.AddMessage(new ProtocolMessage(MessageType.AddUserToList, input));
        return _connections.TryAdd(input, worker);
    }

    /// <summary>
    /// Converts the Concurrent dict, to only key values, then to list, parses into JSON and encodes into utf-8 bytes
    /// </summary>
    /// <returns>Returns byte[] of JSON array of keys</returns>
    public byte[] Serialize(string requestingUser = "")
    {
        var temp = _connections.Keys.ToList();
        temp.Remove(requestingUser);
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(temp));
    }
}