using System.Net;
using format.core;
using System.Text;
using System.Text.Json;

namespace client_core.middleware;

/// <summary>
/// Utility class for deserializing and parsing UserList information from protocol messages.
/// </summary>
public static class UserInfoList
{
    /// <summary>
    /// Extracts and parses a list of user IP endpoints from a protocol message's JSON body.
    /// </summary>
    /// <param name="protocolMessage">The protocol message containing JSON-encoded user addresses in format "IP:Port".</param>
    /// <param name="userInfos">The list of parsed IPEndPoint objects extracted from the message.</param>
    /// <returns>True if the message body was successfully deserialized and parsed; false if the JSON is invalid or null.</returns>
    public static bool GetUserInfoList(ProtocolMessage protocolMessage,  out List<IPEndPoint> userInfos)
    {
        List<string>? addresses = JsonSerializer.Deserialize<List<string>>(Encoding.UTF8.GetString(protocolMessage.Body));
        userInfos = new List<IPEndPoint>();
        if (addresses is null) return false;
        foreach (string address in addresses)
        {
            var temp = address.Split(':');
            userInfos.Add(new IPEndPoint(IPAddress.Parse(temp[0]), int.Parse(temp[1])));
        }
        return true;
    }
   
}