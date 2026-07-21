using System.Net;
using client_core.router;
using format.core;
using System.Text;
using System.Text.Json;

namespace client_core.middleware;

public class UserInfoList
{
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