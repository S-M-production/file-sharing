using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using server_core.core;

namespace server_core.logic;
/// <summary>
/// class to house dictionary relation ip:port->worker object
/// </summary>
public class UserList
{
    /// <summary>
    /// All the connections related from ip:port->worker object
    /// </summary>
    /// <remarks>
    /// Designed this way so one worker can feed a request into another worker to send a message
    /// </remarks>
    public ConcurrentDictionary<string, Worker> Connections {get; } = new();

    /// <summary>
    /// Converts the Concurrent dict, to only key values, then to list, parses into JSON and encodes into utf-8 bytes
    /// </summary>
    /// <returns>Returns byte[] of JSON array of keys</returns>
    public byte[] Serialize(string requestingUser = "")
    {
        var temp = Connections.Keys.ToList();
        temp.Remove(requestingUser);
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(temp));
        
    }
}