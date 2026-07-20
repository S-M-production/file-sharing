using client_core.router;
using format.core;

namespace client_core.middleware;

/// <summary>
/// Currently does nothing but redirect the request to router
/// </summary>
/// <remarks>
/// In future will contain validation etc.
/// </remarks>
public static class Middleware
{
    /// <summary>
    /// Performs a few middle ware checks (Not yet in place) And calls handle of the route
    /// </summary>
    /// <param name="message">The message received from server</param>
    /// <param name="routerMap">The map the middleware should use</param>
    /// <returns>returns null if it couldn't be retrieved, returns a protocol message when the handle returns something</returns>
    public static ProtocolMessage? GetResponse(ProtocolMessage message,RouterMap routerMap)
    {
        //TODO: Fill out middleware if statement logic
        if(!routerMap.GetRoute(message.MessageType, out var handle)) return null;
        return handle!(message);
    }
}