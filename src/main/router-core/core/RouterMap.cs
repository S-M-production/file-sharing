using System.Collections.Concurrent;
using format.core;

namespace router_core.core;

/// <summary>
/// Routing map for router
/// </summary>
public class RouterMap
{
    /// <summary>
    /// Concurrent dictionary mapping message types to their corresponding message handlers.
    /// </summary>
    private readonly ConcurrentDictionary<MessageType,HandleWrap> _map = new ConcurrentDictionary<MessageType, HandleWrap>();

    /// <summary>
    /// Adding a route to the dict
    /// </summary>
    /// <param name="type">The type the listener should respond to</param>
    /// <param name="messageHandler">The delegate that should be called</param>
    /// <param name="cap">The max amount of times the delegate can run before it is deleted</param>
    /// <param name="overwrite">If the current existing route should be overwritten or not</param>
    /// <returns>True if it could write it in, false if it couldn't</returns>
    public bool AddRoute(MessageType type, MessageHandler messageHandler,int cap = -1, bool overwrite = false)
    {
        if (_map.TryGetValue(type, out HandleWrap? existing))
        {
            if (!ValidateReplacement(type, existing, overwrite)) return false;

            _map[type] = new HandleWrap(messageHandler,cap);
            return true;
        }

        return _map.TryAdd(type, new HandleWrap(messageHandler,cap));
    }
    /// <summary>
    /// Validates whether replacement can happen or not
    /// </summary>
    /// <param name="type">Type of message</param>
    /// <param name="existing">Takes HandleWrap that should be replaced</param>
    /// <param name="overwrite">If the exception should be written over without acknowledging expiration</param>
    /// <returns>True if the route can be replaced, false otherwise.</returns>
    /// <exception cref="Exception">Throws the exception if there is remaining uses left</exception>
    private bool ValidateReplacement(MessageType type, HandleWrap existing, bool overwrite)
    {
        if (overwrite)
            return true;

        if (existing.IsExpired)
            return true;

        return false;
    }

    /// <summary>
    /// Retrieves a route handler for the specified message type, decrementing its usage count.
    /// </summary>
    /// <param name="type">Type the route is assigned to</param>
    /// <param name="handle">The message handler that is returned if found and available</param>
    /// <returns>True if the route was found and successfully retrieved, false if not found or expired.</returns>
    /// <exception cref="Exception">Thrown if the route handler cannot be used due to capacity limits.</exception>
    public bool GetRoute(MessageType type, out MessageHandler? handle)
    {
        if (!_map.TryGetValue(type, out HandleWrap? wrap))
        {
            handle = null;
            return false;
        }
        if (wrap.TryUse(out handle)) return true;
        
        _map.TryRemove(type, out _);
        return false;

    }
    
}