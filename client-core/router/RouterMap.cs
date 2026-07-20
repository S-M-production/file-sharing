using System.Collections.Concurrent;
using format.core;

namespace client_core.router;

/// <summary>
/// Routing map for router
/// </summary>
public class RouterMap
{
    private readonly ConcurrentDictionary<MessageType,HandleWrap> _map = new ConcurrentDictionary<MessageType, HandleWrap>();

    /// <summary>
    /// Adding a route to the dict
    /// </summary>
    /// <param name="type">The type the listener should respond to</param>
    /// <param name="handle">The delegate that should be called</param>
    /// <param name="cap">The max amount of times the delegate can run before it is deleted</param>
    /// <param name="overwrite">If the current existing route should be overwritten or not</param>
    /// <returns>True if it could write it in, false if it couldn't</returns>
    public bool AddRoute(MessageType type, Handle handle,int cap = -1, bool overwrite = false)
    {
        if (_map.TryGetValue(type, out HandleWrap? existing))
        {
            ValidateReplacement(type, existing, overwrite);

            _map[type] = new HandleWrap(handle,cap);
            return true;
        }

        return _map.TryAdd(type, new HandleWrap(handle,cap));
    }
    /// <summary>
    /// Validates whether replacement can happen or not
    /// </summary>
    /// <param name="type">Type of message</param>
    /// <param name="existing">Takes HandleWrap that should be replaced</param>
    /// <param name="overwrite">If the exception should be written over without acknowledging expiration</param>
    /// <exception cref="Exception">Throws the exception if there is remaining uses left</exception>
    private void ValidateReplacement(MessageType type, HandleWrap existing, bool overwrite)
    {
        if (overwrite)
            return;

        if (existing.IsExpired)
            return;

        throw new Exception(
            $"Route '{type}' still has {existing.Cap - existing.Used} use(s) remaining.");
    }

    /// <summary>
    /// retrieves a route
    /// </summary>
    /// <param name="type">Type the route is assigned to</param>
    /// <param name="handle">the handle that is returned</param>
    /// <returns>where the operation could or couldnt happen</returns>
    /// <exception cref="Exception"></exception>
    public bool GetRoute(MessageType type, out Handle? handle)
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