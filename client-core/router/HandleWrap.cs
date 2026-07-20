namespace client_core.router;

/// <summary>
/// Wrapper for a handle in RouterMap to add a cap to how many times
/// a certain delegate can be called before it expires.
/// </summary>
public class HandleWrap
{
    public Handle Handle { get; }
    /// <summary>
    /// -1 means unlimited.
    /// </summary>
    public int Cap { get; }

    private int _used;
    /// <summary>
    /// Number of times the delegate has been used.
    /// </summary>
    public int Used => _used;
    
    /// <summary>
    /// Check whether the Handle is expired or not
    /// </summary>
    public bool IsExpired =>
        Cap != -1 && Used >= Cap;

    /// <summary>
    /// Way to call back the handle with checks if you can use it or not
    /// </summary>
    /// <param name="handle">Handle that is given</param>
    /// <returns>Returns true if handle can be used, false if it cant be</returns>
    public bool TryUse(out Handle? handle)
    {
        if (Cap == -1)
        {
            handle = Handle;
            return true;
        }

        if (Interlocked.Increment(ref _used) <= Cap)
        {
            handle = Handle;
            return true;
        }

        handle = null;
        return false;
    }
    
    /// <summary>
    /// Storing a handle with unlimited uses
    /// </summary>
    public HandleWrap(Handle handle)
    {
        Handle = handle;
        Cap = -1;
        _used = 0;
    }
    /// <summary>
    /// Storing a handle with limited uses
    /// </summary>
    /// <param name="cap">How many times you can use it</param>
    public HandleWrap(Handle handle, int cap)
    {
        Handle = handle;
        Cap = cap;
        _used = 0;
    }
    /// <summary>
    /// Storing a handle with limited uses, but you used it a bit already
    /// </summary>
    /// <remarks>
    /// IDK why I made this... maybe will be never used
    /// </remarks>
    /// <param name="handle"></param>
    /// <param name="cap"></param>
    /// <param name="used"></param>
    public HandleWrap(Handle handle, int cap, int used)
    {
        Handle = handle;
        Cap = cap;
        _used = used;
    }
}